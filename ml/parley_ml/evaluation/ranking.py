"""Ranking and calibration metrics for customer value predictions.

All functions take the predicted score and the realized holdout value per customer, as 1-D arrays of equal length.
"""

from __future__ import annotations

import math

import numpy as np
import pandas as pd
from scipy.stats import spearmanr


def spearman_correlation(predicted: np.ndarray, actual: np.ndarray) -> float:
    """Spearman rank correlation between predicted scores and realized value."""
    return float(spearmanr(predicted, actual).statistic)


def tie_averaged_values(predicted: np.ndarray, actual: np.ndarray) -> np.ndarray:
    """Each customer's realized value replaced by the mean value of its tie group (customers with equal scores).

    Under a random order inside a tie group, the expected cumulative gain grows linearly through that group at the
    group's mean value. Replacing values by the group mean gives that expected curve exactly, and no longer depends on
    the order of rows. A top-k cut that falls inside a tie group includes that group pro rata.
    """
    values = pd.Series(np.asarray(actual, dtype=float))
    keys = pd.Series(np.asarray(predicted, dtype=float))
    return values.groupby(keys).transform("mean").to_numpy()


def top_fraction_capture(predicted: np.ndarray, actual: np.ndarray, fraction: float) -> float:
    """Share of the realized total held by the top ``fraction`` of customers, ordered by prediction.

    Tie-aware: a cut inside a tie group takes that group's mean value pro rata (see ``tie_averaged_values``), so the
    result does not depend on row order.
    """
    averaged = tie_averaged_values(predicted, actual)
    total = float(np.sum(actual))
    if total == 0.0:
        return float("nan")
    count = max(1, math.ceil(len(averaged) * fraction - 1e-12))
    order = np.argsort(-np.asarray(predicted), kind="stable")
    return float(np.sum(averaged[order[:count]]) / total)


def cumulative_gains_gini(predicted: np.ndarray, actual: np.ndarray) -> float:
    """Gini of the cumulative-gains curve: ``2 * AUC - 1``, where AUC is the area under the curve of realized share
    against customer share, customers sorted by prediction (descending). Random ordering gives about 0. A perfect
    ordering gives the Gini of the realized values themselves, which is 1 only when one customer holds all value.
    The number depends on the population's value distribution, so compare models within one population only.
    Tie-aware, like ``top_fraction_capture``.
    """
    gains = cumulative_gains_curve(predicted, actual)
    x = np.linspace(0.0, 1.0, num=len(gains))
    return float(2.0 * np.trapezoid(gains, x) - 1.0)


def cumulative_gains_curve(predicted: np.ndarray, actual: np.ndarray) -> np.ndarray:
    """Realized share of total value at each customer share, starting at 0 (length ``n + 1``).

    Uses tie-averaged values, so the curve is the same for any row order. The total is over the given customers. If
    the total is zero, the curve is zero.
    """
    averaged = tie_averaged_values(predicted, actual)
    order = np.argsort(-np.asarray(predicted), kind="stable")
    total = float(np.sum(actual))
    cumulative = np.concatenate([[0.0], np.cumsum(averaged[order])])
    if total == 0.0:
        return np.zeros_like(cumulative)
    return cumulative / total


def total_purchase_error(predicted_purchases: np.ndarray, actual_purchases: np.ndarray) -> float:
    """Relative error of the cohort total: ``(sum predicted - sum actual) / sum actual``."""
    actual_total = float(np.sum(actual_purchases))
    if actual_total == 0.0:
        return float("nan")
    return float((np.sum(predicted_purchases) - actual_total) / actual_total)


def mean_absolute_error(predicted: np.ndarray, actual: np.ndarray) -> float:
    """Mean absolute error per customer."""
    return float(np.mean(np.abs(np.asarray(predicted, dtype=float) - np.asarray(actual, dtype=float))))
