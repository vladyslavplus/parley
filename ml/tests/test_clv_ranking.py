"""Ranking and calibration metrics on tiny cases with known answers (fast)."""

from __future__ import annotations

import numpy as np
import pytest

from parley_ml.evaluation.ranking import (
    cumulative_gains_curve,
    cumulative_gains_gini,
    mean_absolute_error,
    spearman_correlation,
    top_fraction_capture,
    total_purchase_error,
)


def test_top_decile_captures_all_value_when_only_the_top_customer_has_value() -> None:
    actual = np.array([0.0] * 9 + [10.0])
    predicted = np.arange(10, dtype=float)

    assert top_fraction_capture(predicted, actual, 0.1) == pytest.approx(1.0)


def test_spearman_is_one_for_identical_order_and_minus_one_for_reverse() -> None:
    actual = np.array([1.0, 4.0, 9.0, 16.0])

    assert spearman_correlation(actual, actual) == pytest.approx(1.0)
    assert spearman_correlation(-actual, actual) == pytest.approx(-1.0)


def test_top_decile_uses_ceiling_and_stable_ties() -> None:
    actual = np.array([1.0, 2.0, 3.0, 4.0])
    predicted = np.array([5.0, 5.0, 1.0, 0.0])

    # Scores 5 tie at the top with values 1 and 2 (mean 1.5 each). The cut at one customer takes 1.5 pro rata.
    assert top_fraction_capture(predicted, actual, 0.25) == pytest.approx(1.5 / 10.0)
    assert top_fraction_capture(predicted, actual, 0.5) == pytest.approx(3.0 / 10.0)


def test_gini_of_perfect_ordering_equals_gini_of_the_values_and_unrelated_scores_give_zero() -> None:
    # Values 1..100 are uniform, so the gains curve is 2x - x^2 and its Gini is 1/3.
    actual = np.arange(1.0, 101.0)

    assert cumulative_gains_gini(actual, actual) == pytest.approx(1.0 / 3.0, abs=0.01)
    # Unrelated scores give roughly no gain over random ordering. Constant scores are not used here because
    # stable ties keep input order, which is an arbitrary ordering rather than a random one.
    unrelated = np.random.default_rng(0).random(100)
    assert abs(cumulative_gains_gini(unrelated, actual)) < 0.1


def test_cumulative_gains_curve_starts_at_zero_and_ends_at_one() -> None:
    curve = cumulative_gains_curve(np.array([3.0, 1.0, 2.0]), np.array([1.0, 1.0, 2.0]))

    assert curve[0] == 0.0
    assert curve[-1] == pytest.approx(1.0)
    assert len(curve) == 4


def test_total_purchase_error_and_mae() -> None:
    assert total_purchase_error(np.array([1.0, 1.0]), np.array([2.0, 2.0])) == pytest.approx(-0.5)
    assert mean_absolute_error(np.array([1.0, 3.0]), np.array([2.0, 2.0])) == pytest.approx(1.0)


def test_tied_scores_give_the_same_capture_and_gini_for_any_row_order() -> None:
    rng = np.random.default_rng(4)
    predicted = rng.integers(0, 5, 200).astype(float)
    actual = rng.integers(0, 4, 200).astype(float)
    order = rng.permutation(200)

    assert top_fraction_capture(predicted, actual, 0.1) == pytest.approx(
        top_fraction_capture(predicted[order], actual[order], 0.1)
    )
    assert cumulative_gains_gini(predicted, actual) == pytest.approx(
        cumulative_gains_gini(predicted[order], actual[order])
    )


def test_top_decile_cut_inside_a_tie_group_takes_the_group_mean_pro_rata() -> None:
    # Four customers tie at the top score with values 0, 0, 0, 8 (mean 2). The top 1/8 of 8 customers is one customer,
    # which takes the tie group's mean of 2 under random order inside the tie: 2 / total 8.
    predicted = np.array([9.0, 9.0, 9.0, 9.0, 1.0, 1.0, 1.0, 1.0])
    actual = np.array([0.0, 0.0, 0.0, 8.0, 0.0, 0.0, 0.0, 0.0])

    assert top_fraction_capture(predicted, actual, 0.125) == pytest.approx(2.0 / 8.0)
