"""Closed-form CLV functions for the scoring service: BG/NBD and Gamma-Gamma with saved MAP parameters.

Pure numpy and scipy, no PyMC, so the serving image stays small. Formulas follow Fader, Hardie and Lee (2005)
for BG/NBD and Fader and Hardie (2013) for Gamma-Gamma, in the form used by PyMC-Marketing 1.3.0. A test compares
the results with PyMC-Marketing on a fixed customer set (``tests/test_clv_closed_form.py``).

Inputs are the RFM summary columns: ``frequency`` (repeat purchase days, an integer), ``recency`` (days between first
and last purchase), ``tenure`` (days between first purchase and the end of observation), and the horizon in days.

Per-item validity. ``validate_items`` returns a boolean mask and a reason per item. The scoring service rejects
invalid items one by one (``rejected`` in the response) and scores the rest. The functions here return NaN for
rejected items and for items whose closed form is not finite. A bad batch never raises for one item. Only the
horizon is a request-level value, and an invalid horizon raises ``ValueError``.

Numerical domain. The Gauss hypergeometric function overflows where the repeat count is close to the tenure and the
horizon is 10 years (``MAX_HORIZON_DAYS``). The direct form is used first; when it is not finite, the Pfaff transform
``2F1(A, B; C; z) = (1 - z)^-A 2F1(A, C - B; C; z / (z - 1))`` is used. Items that are still not finite are returned
as NaN with reason ``numeric_domain``. Checked on a grid of frequencies up to 10,000 days, tenures up to 36,500 days,
and horizons from 1 to 3,650 days (against mpmath during development): the only non-finite results were at horizon
3,650 with frequency between 365 and 1,000 and tenure equal to frequency.

Gamma-Gamma. Customers with no repeat purchases, or with a mean repeat value at or below zero (net refunds), get the
population mean spend ``v * p / (q - 1)``. The Gamma-Gamma model is fitted only on customers with a positive mean repeat
value, so the individual term does not apply to the others.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Final

import numpy as np
from scipy.special import expit, hyp2f1

MAX_TENURE_DAYS: Final = 36_500
MAX_HORIZON_DAYS: Final = 3_650

REASON_FREQUENCY_NOT_INTEGER: Final = "frequency_not_integer"
REASON_FREQUENCY_RANGE: Final = "frequency_out_of_range"
REASON_RECENCY_RANGE: Final = "recency_out_of_range"
REASON_TENURE_RANGE: Final = "tenure_out_of_range"
REASON_NUMERIC_DOMAIN: Final = "numeric_domain"


@dataclass(frozen=True)
class BetaGeoParameters:
    alpha: float
    r: float
    a: float
    b: float

    def __post_init__(self) -> None:
        if min(self.alpha, self.r, self.a, self.b) <= 0:
            raise ValueError("BG/NBD parameters must be positive")
        if self.a == 1.0:
            raise ValueError("BG/NBD parameter a must not equal 1")
        if self.a + self.b <= 1.0:
            raise ValueError("BG/NBD requires a + b > 1 so that a + b + x - 1 is positive for every frequency x")


@dataclass(frozen=True)
class GammaGammaParameters:
    p: float
    q: float
    v: float

    def __post_init__(self) -> None:
        if min(self.p, self.v) <= 0:
            raise ValueError("Gamma-Gamma parameters p and v must be positive")
        if self.q <= 1.0:
            raise ValueError("Gamma-Gamma requires q > 1 for a finite population mean")


@dataclass(frozen=True)
class ItemValidity:
    """Per-item validity: ``valid[i]`` is False when item i is rejected, and ``reasons[i]`` says why."""

    valid: np.ndarray
    reasons: tuple[str | None, ...]


def validate_items(frequency: np.ndarray, recency: np.ndarray, tenure: np.ndarray) -> ItemValidity:
    """Check each item's RFM inputs. Items with a reason are invalid; the rest can be scored.

    Rules: frequency is a non-negative integer no larger than tenure (repeat purchase days cannot exceed the days since
    the first purchase); recency is between 0 and tenure; tenure is at most ``MAX_TENURE_DAYS``.
    """
    x = np.asarray(frequency, dtype=float)
    t_x = np.asarray(recency, dtype=float)
    big_t = np.asarray(tenure, dtype=float)
    with np.errstate(invalid="ignore"):
        not_integer = ~np.isfinite(x) | (np.mod(x, 1.0) != 0.0)
        frequency_range = ~(x >= 0) | (x > big_t)
        tenure_range = ~(big_t >= 0) | (big_t > MAX_TENURE_DAYS)
        recency_range = ~((t_x >= 0) & (t_x <= big_t))

    reasons: list[str | None] = []
    for index in range(x.shape[0]):
        if tenure_range[index]:
            reasons.append(REASON_TENURE_RANGE)
        elif recency_range[index]:
            reasons.append(REASON_RECENCY_RANGE)
        elif not_integer[index]:
            reasons.append(REASON_FREQUENCY_NOT_INTEGER)
        elif frequency_range[index]:
            reasons.append(REASON_FREQUENCY_RANGE)
        else:
            reasons.append(None)
    valid = np.array([reason is None for reason in reasons], dtype=bool)
    return ItemValidity(valid=valid, reasons=tuple(reasons))


def validate_horizon(horizon: float) -> None:
    """The horizon is one value per request. Raises ``ValueError`` outside 0..``MAX_HORIZON_DAYS``."""
    if not 0 <= horizon <= MAX_HORIZON_DAYS:
        raise ValueError(f"horizon must be between 0 and {MAX_HORIZON_DAYS} days")


def expected_purchases(
    params: BetaGeoParameters,
    frequency: np.ndarray,
    recency: np.ndarray,
    tenure: np.ndarray,
    horizon: float,
) -> np.ndarray:
    """Expected number of purchase days in the next ``horizon`` days (Fader et al. 2005, eq. 10).

    NaN for items rejected by ``validate_items`` and for items whose closed form is not finite.
    """
    validate_horizon(horizon)
    validity = validate_items(frequency, recency, tenure)
    out = np.full(validity.valid.shape[0], np.nan)
    if not validity.valid.any():
        return out
    x = np.asarray(frequency, dtype=float)[validity.valid]
    t_x = np.asarray(recency, dtype=float)[validity.valid]
    big_t = np.asarray(tenure, dtype=float)[validity.valid]
    t = float(horizon)
    alpha, r, a, b = params.alpha, params.r, params.a, params.b
    c = a + b + x - 1.0
    shape = r + x

    with np.errstate(all="ignore"):
        ratio = (alpha + big_t) / (alpha + big_t + t)
        z = t / (alpha + big_t + t)
        direct = ratio**shape * hyp2f1(shape, b + x, c, z)
        pfaff = hyp2f1(shape, a - 1.0, c, -t / (alpha + big_t))
        tail = np.where(np.isfinite(direct), direct, pfaff)
        bracket = 1.0 - tail
        numerator = c / (a - 1.0) * bracket

        repeat = x > 0
        safe_x = np.where(repeat, x, 1.0)
        repeat_term = (a / (b + safe_x - 1.0)) * ((alpha + big_t) / (alpha + t_x)) ** (r + safe_x)
        denominator = 1.0 + np.where(repeat, repeat_term, 0.0)
        values = numerator / denominator
    out[validity.valid] = np.where(np.isfinite(values), values, np.nan)
    return out


def probability_alive(
    params: BetaGeoParameters,
    frequency: np.ndarray,
    recency: np.ndarray,
    tenure: np.ndarray,
) -> np.ndarray:
    """Probability that the customer is still active at the end of observation (Fader, Hardie and Lee 2008).

    NaN for items rejected by ``validate_items``.
    """
    validity = validate_items(frequency, recency, tenure)
    out = np.full(validity.valid.shape[0], np.nan)
    if not validity.valid.any():
        return out
    x = np.asarray(frequency, dtype=float)[validity.valid]
    t_x = np.asarray(recency, dtype=float)[validity.valid]
    big_t = np.asarray(tenure, dtype=float)[validity.valid]
    alpha, r, a, b = params.alpha, params.r, params.a, params.b

    with np.errstate(all="ignore"):
        safe_x = np.maximum(x, 1.0)
        log_odds_term = (r + x) * np.log((alpha + big_t) / (alpha + t_x)) + np.log(a / (b + safe_x - 1.0))
        out[validity.valid] = np.where(x == 0, 1.0, expit(-log_odds_term))
    return out


def expected_spend(
    params: GammaGammaParameters,
    frequency: np.ndarray,
    mean_repeat_value: np.ndarray,
) -> np.ndarray:
    """Expected average value of the next purchase (Fader and Hardie 2013, eq. 5).

    The individual term applies only where ``frequency`` is positive and ``mean_repeat_value`` is positive. Everywhere
    else the population mean ``v * p / (q - 1)`` is returned (see the module docstring). NaN for non-integer or
    negative frequency, and for a non-finite mean value.
    """
    x = np.asarray(frequency, dtype=float)
    m = np.asarray(mean_repeat_value, dtype=float)
    p, q, v = params.p, params.q, params.v
    population_mean = v * p / (q - 1.0)
    with np.errstate(all="ignore"):
        individual = (x > 0) & (m > 0)
        safe_x = np.where(individual, x, 0.0)
        safe_m = np.where(individual, m, 0.0)
        weight = np.where(individual, p * safe_x / (p * safe_x + q - 1.0), 0.0)
        values = (1.0 - weight) * population_mean + weight * safe_m
    invalid = ~np.isfinite(x) | (x < 0) | (np.mod(np.where(np.isfinite(x), x, 0.0), 1.0) != 0.0) | ~np.isfinite(m)
    return np.where(invalid, np.nan, values)


def expected_value_over_horizon(
    transactions: BetaGeoParameters,
    spend: GammaGammaParameters,
    frequency: np.ndarray,
    recency: np.ndarray,
    tenure: np.ndarray,
    mean_repeat_value: np.ndarray,
    horizon: float,
) -> np.ndarray:
    """Expected undiscounted value over the horizon: expected purchases times expected spend."""
    purchases = expected_purchases(transactions, frequency, recency, tenure, horizon)
    return purchases * expected_spend(spend, frequency, mean_repeat_value)


