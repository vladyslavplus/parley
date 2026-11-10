"""Uplift metrics for randomized experiments: uplift@k, Qini, AUUC, conditional profit, Poisson bootstrap.

Conventions. Rows are ranked by a score, highest first. For a top fraction k of the population:
- N_T(k), N_C(k): treated and control counts (weighted when weights are given);
- Y_T(k), Y_C(k): outcome sums of treated and control rows.

Joint curves (rank treated and control together):
- uplift@k = Y_T/N_T - Y_C/N_C
- Qini(k) = Y_T - Y_C * N_T / N_C (absolute, count-rebalanced)
- AUUC cumulative gain = uplift(k) * (N_T + N_C), integrated over k

Separate curves (rank treated and control each on its own; Devriendt, Guns and Verbeke 2020, Eq. 10 and 16):
- V(p) = R_T(p) / |T| - R_C(p) / |C|, where R_g(p) is the responder count in the top p fraction of group g
- AUUC_separate_relative = sum over p = 0.01, 0.02, ..., 1.00 of V(p)

Integrals over k use the mean of the curve on a uniform grid of G points (right endpoints i/G, i = 1..G), so with
G equal to the row count the joint AUUC matches ``causalml.metrics.auuc_score(normalize=False)`` and the Qini
coefficient matches ``causalml.metrics.qini_score(normalize=False)``.

Weights (for the Poisson bootstrap) replace row counts everywhere; a row with weight 0 is absent.

Ties. Rows with equal scores form one tie group. A cut that falls inside a group takes that group's rows pro rata,
which is the expected value under a random order inside the group. The result does not depend on row order.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Callable, Final

import numpy as np

DEFAULT_GRID_POINTS: Final = 1000
SEPARATE_GRID_POINTS: Final = 100
CI_LOW_PERCENT: Final = 2.5
CI_HIGH_PERCENT: Final = 97.5


@dataclass(frozen=True)
class RankedSample:
    """Rows sorted by score, highest first. Build once per score vector and reuse across bootstrap draws."""

    order: np.ndarray
    treatment: np.ndarray
    outcome: np.ndarray
    group_ends: np.ndarray  # prefix indices (1-based row counts) where each tie group ends; last value is the size

    @property
    def size(self) -> int:
        return int(self.treatment.shape[0])


def rank_sample(scores: np.ndarray, treatment: np.ndarray, outcome: np.ndarray) -> RankedSample:
    """Sort rows by score, descending. Ties keep input order (stable sort), so results are reproducible."""
    scores = np.asarray(scores, dtype=float)
    treatment = np.asarray(treatment, dtype=float)
    outcome = np.asarray(outcome, dtype=float)
    if not (scores.shape == treatment.shape == outcome.shape) or scores.ndim != 1:
        raise ValueError("scores, treatment and outcome must be 1-D arrays of the same length")
    if not np.all(np.isfinite(scores)):
        raise ValueError("scores must be finite")
    if not np.all(np.isin(treatment, (0.0, 1.0))) or not np.all(np.isin(outcome, (0.0, 1.0))):
        raise ValueError("treatment and outcome must be 0 or 1")
    if scores.size == 0:
        raise ValueError("at least one row is required")
    order = np.argsort(-scores, kind="stable")
    group_ends = _group_ends(scores[order])
    return RankedSample(order=order, treatment=treatment[order], outcome=outcome[order], group_ends=group_ends)


def _group_ends(sorted_scores: np.ndarray) -> np.ndarray:
    """Prefix index where each run of equal scores ends (1-based), in descending order. The last entry is the size."""
    size = sorted_scores.shape[0]
    breaks = np.flatnonzero(sorted_scores[1:] != sorted_scores[:-1]) + 1
    return np.concatenate([breaks, [size]]).astype(np.int64)


def _prefix_sums(sample: RankedSample, weights: np.ndarray | None) -> dict[str, np.ndarray]:
    """Prefix sums with a leading zero: index j holds the totals of the first j ranked rows."""
    t = sample.treatment
    y = sample.outcome
    w = np.ones(sample.size) if weights is None else np.asarray(weights, dtype=float)[sample.order]
    arrays = {
        "w": w,
        "nt": w * t,
        "nc": w * (1.0 - t),
        "yt": w * t * y,
        "yc": w * (1.0 - t) * y,
    }
    return {name: np.concatenate([[0.0], np.cumsum(values)]) for name, values in arrays.items()}


def _threshold(values: np.ndarray) -> np.ndarray:
    """Cumulative-weight threshold with a relative tolerance, so that i/n * n (rounded down by floating point) keeps
    row i. Without it, integer prefixes can lose their last row."""
    return values * (1.0 + 1e-12) + 1e-12


def _cumulative_at(
    cum_w: np.ndarray,
    cum_values: np.ndarray,
    group_ends: np.ndarray,
    targets: np.ndarray,
) -> np.ndarray:
    """Cumulative value at each target weight. Targets inside a tie group take that group's rows pro rata.

    ``cum_w`` and ``cum_values`` are prefix sums with a leading zero (length n + 1). ``group_ends`` holds the prefix
    index where each tie group ends. Targets beyond the total are clipped to the total.
    """
    starts = np.concatenate([[0], group_ends[:-1]])
    end_w = cum_w[group_ends]
    start_w = cum_w[starts]
    # The tolerance only decides which group a boundary target belongs to; the share uses the exact target.
    lower = targets * (1.0 - 1e-12) - 1e-12
    group = np.minimum(np.searchsorted(end_w, lower, side="left"), group_ends.shape[0] - 1)
    width = end_w[group] - start_w[group]
    with np.errstate(divide="ignore", invalid="ignore"):
        pro_rata = np.where(width > 0, (targets - start_w[group]) / width, 1.0)
    # A single row is never split: it counts once the target reaches its end, as before ties were handled.
    single_row = (group_ends[group] - starts[group]) == 1
    whole_row = (targets >= end_w[group] - 1e-12 * (1.0 + np.abs(end_w[group]))).astype(float)
    share = np.where(single_row, whole_row, np.clip(pro_rata, 0.0, 1.0))
    share = np.clip(share, 0.0, 1.0)
    first = cum_values[starts[group]]
    last = cum_values[group_ends[group]]
    return first + share * (last - first)


def _joint_counts(sample: RankedSample, weights: np.ndarray | None, fractions: np.ndarray) -> dict[str, np.ndarray]:
    """Weighted totals of the top ``fraction`` of the joint ranking, for each fraction (tie-aware)."""
    sums = _prefix_sums(sample, weights)
    total = sums["w"][-1]
    targets = np.asarray(fractions, dtype=float) * total
    return {
        name: _cumulative_at(sums["w"], values, sample.group_ends, targets)
        for name, values in sums.items()
    }


def _uplift(nt: np.ndarray, nc: np.ndarray, yt: np.ndarray, yc: np.ndarray) -> np.ndarray:
    with np.errstate(divide="ignore", invalid="ignore"):
        return yt / nt - yc / nc


def uplift_at_k(sample: RankedSample, k: float, weights: np.ndarray | None = None) -> float:
    """Uplift of the top fraction ``k``: Y_T/N_T - Y_C/N_C. NaN when either arm is empty."""
    counts = _joint_counts(sample, weights, np.array([k]))
    return float(_uplift(counts["nt"], counts["nc"], counts["yt"], counts["yc"])[0])


def qini_curve(sample: RankedSample, fractions: np.ndarray, weights: np.ndarray | None = None) -> np.ndarray:
    """Absolute Qini values Y_T - Y_C * N_T / N_C at each fraction. NaN where a control count is zero."""
    counts = _joint_counts(sample, weights, fractions)
    with np.errstate(divide="ignore", invalid="ignore"):
        return counts["yt"] - counts["yc"] * counts["nt"] / counts["nc"]


def uniform_grid(points: int) -> np.ndarray:
    """Right endpoints i/G for i = 1..G."""
    if points < 1:
        raise ValueError("grid needs at least one point")
    return np.arange(1, points + 1, dtype=float) / points


def qini_coefficient(
    sample: RankedSample, points: int = DEFAULT_GRID_POINTS, weights: np.ndarray | None = None
) -> float:
    """Area between the model's Qini curve and the random line, averaged over the grid (causalml convention).

    The random line runs from 0 to the Qini value of the whole population. The mean is taken over the ``points``
    grid points and the origin (where both curves are zero), which is the causalml ``qini_score`` convention.
    """
    fractions = uniform_grid(points)
    model = np.nan_to_num(qini_curve(sample, fractions, weights))
    total = qini_curve(sample, np.array([1.0]), weights)[0]
    gap = model - fractions * total
    return float(np.sum(gap) / (points + 1))


def auuc(sample: RankedSample, points: int = DEFAULT_GRID_POINTS, weights: np.ndarray | None = None) -> float:
    """Area under the cumulative uplift curve, uplift(k) * N(k): the grid sum divided by ``points + 1``.

    The origin is part of the average, as in causalml. Equals ``causalml.metrics.auuc_score(normalize=False)`` when
    ``points`` equals the row count and no weights are given.
    """
    fractions = uniform_grid(points)
    counts = _joint_counts(sample, weights, fractions)
    uplift = np.nan_to_num(_uplift(counts["nt"], counts["nc"], counts["yt"], counts["yc"]))
    gain = uplift * (counts["nt"] + counts["nc"])
    return float(np.sum(gain) / (points + 1))


def separate_relative_auuc(
    scores: np.ndarray,
    treatment: np.ndarray,
    outcome: np.ndarray,
    weights: np.ndarray | None = None,
) -> float:
    """AUUC of the separate, relative curve (Devriendt, Guns and Verbeke, Eq. 10 and 16).

    Treated and control rows are each ranked by score within their own group. V(p) is the responder count among the
    top p fraction of treated rows divided by the treated group size, minus the same quantity for control. The
    result sums V(p) over p = 0.01 .. 1.00.
    """
    scores = np.asarray(scores, dtype=float)
    treatment = np.asarray(treatment)
    outcome = np.asarray(outcome, dtype=float)
    w_all = np.ones(scores.shape[0]) if weights is None else np.asarray(weights, dtype=float)
    fractions = uniform_grid(SEPARATE_GRID_POINTS)

    captured: list[np.ndarray] = []
    for arm in (1, 0):
        members = np.flatnonzero(treatment == arm)
        order = members[np.argsort(-scores[members], kind="stable")]
        w = w_all[order]
        y = outcome[order]
        cum_w = np.concatenate([[0.0], np.cumsum(w)])
        cum_wy = np.concatenate([[0.0], np.cumsum(w * y)])
        group_size = cum_w[-1]
        if group_size == 0.0:
            return float("nan")
        group_ends = _group_ends(scores[order])
        values = _cumulative_at(cum_w, cum_wy, group_ends, fractions * group_size)
        captured.append(values / group_size)
    return float(np.sum(captured[0] - captured[1]))


def conditional_profit(
    sample: RankedSample,
    fractions: np.ndarray,
    cost_to_value_ratios: np.ndarray,
    weights: np.ndarray | None = None,
) -> np.ndarray:
    """Profit(k) = N(k) * (uplift(k) * v - c) with v = 1 and c = ratio, shape (ratios, fractions).

    Per unit of value. ``N(k)`` is the weighted count of the top fraction. ``k*`` is chosen on validation with
    :func:`best_fraction` and applied to the test split.
    """
    counts = _joint_counts(sample, weights, fractions)
    uplift = np.nan_to_num(_uplift(counts["nt"], counts["nc"], counts["yt"], counts["yc"]))
    population = counts["nt"] + counts["nc"]
    ratios = np.asarray(cost_to_value_ratios, dtype=float)[:, None]
    return population[None, :] * (uplift[None, :] - ratios)


def best_fraction(profit: np.ndarray, fractions: np.ndarray) -> float:
    """Fraction with the highest profit in a one-dimensional profit curve."""
    return float(fractions[int(np.argmax(profit))])


def poisson_weights(size: int, rng: np.random.Generator) -> np.ndarray:
    """Poisson(1) row weights: the Poisson bootstrap, which resamples rows independently."""
    return rng.poisson(1.0, size).astype(float)


def bootstrap(
    statistic: Callable[[np.ndarray], dict[str, float]],
    size: int,
    replicates: int,
    seed: int,
) -> dict[str, np.ndarray]:
    """Evaluate ``statistic`` on ``replicates`` Poisson weight vectors drawn from one seeded generator.

    ``statistic`` receives the weights and returns named values. Evaluating two models in the same call with the
    same weights gives paired differences.
    """
    rng = np.random.default_rng(seed)
    draws: dict[str, list[float]] = {}
    for _ in range(replicates):
        values = statistic(poisson_weights(size, rng))
        for name, value in values.items():
            draws.setdefault(name, []).append(float(value))
    return {name: np.asarray(values) for name, values in draws.items()}


def interval(values: np.ndarray) -> tuple[float, float]:
    """Percentile interval at 95 %, ignoring non-finite replicates."""
    finite = values[np.isfinite(values)]
    if finite.size == 0:
        return float("nan"), float("nan")
    return float(np.percentile(finite, CI_LOW_PERCENT)), float(np.percentile(finite, CI_HIGH_PERCENT))


def summarize(draws: dict[str, np.ndarray], point: dict[str, float]) -> dict[str, dict[str, Any]]:
    """Point estimate, interval and replicate count for each named statistic."""
    summary: dict[str, dict[str, Any]] = {}
    for name, values in draws.items():
        low, high = interval(values)
        summary[name] = {
            "point": float(point[name]),
            "ci_low": low,
            "ci_high": high,
            "replicates": int(values.shape[0]),
        }
    return summary
