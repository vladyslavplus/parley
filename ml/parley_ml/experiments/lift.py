"""Campaign lift statistics: reference formulas for two-arm conversion campaigns (attribution and statistics rules of the product).

Pure Python and scipy. The C# calculator must match these definitions exactly; the fixture header in
``tests/fixtures/lift_vectors.json`` repeats them for that purpose.

Definitions (n = arm size, c = conversions, p = c / n):
- Rates: p_t = c_t / n_t, p_h = c_h / n_h. Lift = p_t - p_h.
- Wilson score interval of one proportion at quantile z: centre (p + z^2 / 2n) / (1 + z^2 / n), half width
  z * sqrt(p (1 - p) / n + z^2 / 4n^2) / (1 + z^2 / n). The lower and upper bounds are the interval ends.
- Newcombe-Wilson interval for the difference (Newcombe 1998, method 10, hybrid score): with the Wilson intervals
  (l_t, u_t) and (l_h, u_h) at the same z,
  lower = lift - sqrt((p_t - l_t)^2 + (u_h - p_h)^2),
  upper = lift + sqrt((u_t - p_t)^2 + (p_h - l_h)^2).
- z = Phi^-1(1 - alpha / 2) for the interval and the two-sided test, with alpha = 0.05 unless stated.
- Two-proportion z-test: pooled proportion p_0 = (c_t + c_h) / (n_t + n_h),
  z = lift / sqrt(p_0 (1 - p_0) (1 / n_t + 1 / n_h)), two-sided p-value = 2 (1 - Phi(|z|)).
- Sample ratio mismatch: chi-squared statistic with 1 degree of freedom on the arm counts against expected counts
  n_total * share, where share_treatment = share_treatment_bps / 10000. p-value = P(chi2_1 > statistic).
- Minimum detectable effect (absolute): (z_alpha + z_beta) * sqrt(p (1 - p) (1 / n_t + 1 / n_h)), z_alpha = Phi^-1(1 - alpha / 2),
  z_beta = Phi^-1(power), p = the baseline rate (the holdout rate in the product). Example: n_h = 590, n_t = 5310, p = 0.20
  gives 0.0486 (4.9 percentage points).
- Incremental revenue: (mean_revenue_t - mean_revenue_h) * n_t.

Undefined values (zero denominators) are returned as NaN. Those cases are listed in the fixture with ``null``.
"""

from __future__ import annotations

import math
from dataclasses import dataclass
from typing import Final

from scipy.stats import chi2, norm

DEFAULT_ALPHA: Final = 0.05
DEFAULT_POWER: Final = 0.8
BASIS_POINTS: Final = 10_000


@dataclass(frozen=True)
class ArmCounts:
    conversions: int
    n: int


def _is_undefined(*values: float) -> bool:
    return any(math.isnan(value) for value in values)


def rate(conversions: int, n: int) -> float:
    if n == 0:
        return math.nan
    return conversions / n


def wilson_interval(conversions: int, n: int, z: float) -> tuple[float, float]:
    """Wilson score interval (low, high) for one proportion. NaN bounds when n is 0."""
    if n == 0:
        return math.nan, math.nan
    p = conversions / n
    z2 = z * z
    denominator = 1.0 + z2 / n
    centre = (p + z2 / (2.0 * n)) / denominator
    half = z * math.sqrt(p * (1.0 - p) / n + z2 / (4.0 * n * n)) / denominator
    return centre - half, centre + half


def newcombe_wilson_difference(
    treatment: ArmCounts, holdout: ArmCounts, alpha: float = DEFAULT_ALPHA
) -> tuple[float, float, float]:
    """(lift, low, high) for p_t - p_h. NaN when either arm is empty."""
    z = norm.ppf(1.0 - alpha / 2.0)
    p_t = rate(treatment.conversions, treatment.n)
    p_h = rate(holdout.conversions, holdout.n)
    if _is_undefined(p_t, p_h):
        return math.nan, math.nan, math.nan
    lift = p_t - p_h
    l_t, u_t = wilson_interval(treatment.conversions, treatment.n, z)
    l_h, u_h = wilson_interval(holdout.conversions, holdout.n, z)
    low = lift - math.sqrt((p_t - l_t) ** 2 + (u_h - p_h) ** 2)
    high = lift + math.sqrt((u_t - p_t) ** 2 + (p_h - l_h) ** 2)
    return lift, low, high


def two_proportion_z_p_value(treatment: ArmCounts, holdout: ArmCounts) -> float:
    """Two-sided p-value of the pooled two-proportion z-test. NaN when the pooled variance is zero."""
    n_total = treatment.n + holdout.n
    if n_total == 0:
        return math.nan
    pooled = (treatment.conversions + holdout.conversions) / n_total
    variance = pooled * (1.0 - pooled) * (1.0 / treatment.n + 1.0 / holdout.n) if treatment.n and holdout.n else 0.0
    if variance == 0.0:
        return math.nan
    p_t = rate(treatment.conversions, treatment.n)
    p_h = rate(holdout.conversions, holdout.n)
    z = (p_t - p_h) / math.sqrt(variance)
    return 2.0 * (1.0 - norm.cdf(abs(z)))


def srm_p_value(treatment_n: int, holdout_n: int, share_treatment_bps: int) -> float:
    """Chi-squared p-value (1 df) of the arm counts against share_treatment_bps. NaN when there are no rows."""
    if not 0 < share_treatment_bps < BASIS_POINTS:
        raise ValueError("share_treatment_bps must be strictly between 0 and 10000")
    total = treatment_n + holdout_n
    if total == 0:
        return math.nan
    share = share_treatment_bps / BASIS_POINTS
    expected_t = total * share
    expected_h = total * (1.0 - share)
    statistic = (treatment_n - expected_t) ** 2 / expected_t + (holdout_n - expected_h) ** 2 / expected_h
    return float(chi2.sf(statistic, df=1))


def minimum_detectable_effect(
    baseline_rate: float,
    treatment_n: int,
    holdout_n: int,
    alpha: float = DEFAULT_ALPHA,
    power: float = DEFAULT_POWER,
) -> float:
    """Absolute minimum detectable effect. NaN when either arm is empty."""
    if treatment_n == 0 or holdout_n == 0:
        return math.nan
    z_alpha = norm.ppf(1.0 - alpha / 2.0)
    z_beta = norm.ppf(power)
    return (z_alpha + z_beta) * math.sqrt(baseline_rate * (1.0 - baseline_rate) * (1.0 / treatment_n + 1.0 / holdout_n))


def incremental_revenue(mean_revenue_treatment: float, mean_revenue_holdout: float, treatment_n: int) -> float:
    """(mean revenue per treated recipient - mean revenue per holdout recipient) * treated recipients. NaN if undefined."""
    if treatment_n == 0 or _is_undefined(mean_revenue_treatment, mean_revenue_holdout):
        return math.nan
    return (mean_revenue_treatment - mean_revenue_holdout) * treatment_n
