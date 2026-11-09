"""Uplift metrics: hand-computed fixtures, causalml cross-check, random baseline (fast, no data)."""

from __future__ import annotations

import numpy as np
import pandas as pd
import pytest
from causalml.metrics import auuc_score, qini_score

from parley_ml.evaluation import uplift as u


def _fixture_scores() -> np.ndarray:
    return np.array([6.0, 5.0, 4.0, 3.0, 2.0, 1.0])


def test_uplift_at_k_matches_hand_values() -> None:
    # Ranked rows (treatment, outcome): (1,1), (0,0), (1,0), (1,1), (0,1), (0,0)
    sample = u.rank_sample(_fixture_scores(), [1, 0, 1, 1, 0, 0], [1, 0, 0, 1, 1, 0])

    assert u.uplift_at_k(sample, 0.5) == pytest.approx(0.5)  # top 3: Y_T=1,N_T=2; Y_C=0,N_C=1
    assert u.uplift_at_k(sample, 2 / 3) == pytest.approx(2.0 / 3.0 - 0.0)  # top 4: Y_T=2,N_T=3; Y_C=0,N_C=1
    assert u.uplift_at_k(sample, 1.0) == pytest.approx(2.0 / 3.0 - 1.0 / 3.0)  # all: Y_T=2,N_T=3; Y_C=1,N_C=3
    assert np.isnan(u.uplift_at_k(sample, 1 / 6))  # top 1 row is treated only: no control


def test_qini_curve_and_coefficient_match_hand_values() -> None:
    # Ranked rows: control rows first among the top two so every prefix has a control.
    sample = u.rank_sample(_fixture_scores(), [1, 0, 1, 1, 0, 0], [1, 0, 0, 1, 1, 0])
    fractions = np.array([0.5, 2 / 3, 1.0])

    np.testing.assert_allclose(u.qini_curve(sample, fractions), [1.0, 2.0, 1.0])
    # Gaps to the random line over the six-point grid (origin included in the mean): sum 2.0, divided by 7.
    assert u.qini_coefficient(sample, points=6) == pytest.approx(2.0 / 7.0)


def test_auuc_matches_hand_value() -> None:
    # Gains uplift(k) * N(k) at k = 1/6 .. 1 are 0 (undefined: no control), 2, 1.5, 8/3, 5/6, 2; their sum 9 is
    # averaged over the six grid points and the origin (divided by 7).
    sample = u.rank_sample(_fixture_scores(), [1, 0, 1, 1, 0, 0], [1, 0, 0, 1, 1, 0])

    assert u.auuc(sample, points=6) == pytest.approx(9.0 / 7.0)


def test_causalml_cross_check_on_random_fixture() -> None:
    rng = np.random.default_rng(3)
    n = 300
    scores = rng.normal(size=n)
    treatment = rng.integers(0, 2, n).astype(float)
    outcome = (rng.random(n) < 0.1 + 0.1 * treatment * (scores > 0)).astype(float)
    frame = pd.DataFrame({"y": outcome, "w": treatment, "model": scores})
    sample = u.rank_sample(scores, treatment, outcome)

    causal_qini = float(qini_score(frame, outcome_col="y", treatment_col="w", normalize=False)["model"])
    causal_auuc = float(auuc_score(frame, outcome_col="y", treatment_col="w", normalize=False)["model"])

    assert u.qini_coefficient(sample, points=n) == pytest.approx(causal_qini, rel=1e-9, abs=1e-9)
    assert u.auuc(sample, points=n) == pytest.approx(causal_auuc, rel=1e-9, abs=1e-9)


def test_separate_relative_auuc_hand_value() -> None:
    # Two treated rows, both responders; two control rows, no responders. Each group is ranked on its own.
    # V(p) = treated responders in the top p fraction / 2 - 0 = 0 for p < 0.5, 0.5 for 0.5 <= p < 1, 1.0 at p = 1.
    # Sum over p = 0.01 .. 1.00: 50 * 0.5 + 1.0 = 26.0.
    scores = np.array([9.0, 8.0, 7.0, 6.0])
    treatment = np.array([1.0, 1.0, 0.0, 0.0])
    outcome = np.array([1.0, 1.0, 0.0, 0.0])

    assert u.separate_relative_auuc(scores, treatment, outcome) == pytest.approx(26.0)


def test_conditional_profit_and_best_fraction() -> None:
    sample = u.rank_sample(_fixture_scores(), [1, 0, 1, 1, 0, 0], [1, 0, 0, 1, 1, 0])
    fractions = np.array([0.5, 1.0])
    profit = u.conditional_profit(sample, fractions, np.array([0.1]))

    # N(0.5) = 3, uplift 0.5 -> 3 * (0.5 - 0.1) = 1.2 ; N(1) = 6, uplift 1/3 -> 6 * (1/3 - 0.1) = 1.4
    np.testing.assert_allclose(profit[0], [1.2, 1.4])
    assert u.best_fraction(profit[0], fractions) == 1.0


def test_random_baseline_qini_interval_covers_zero() -> None:
    rng = np.random.default_rng(11)
    n = 20_000
    scores = rng.random(n)
    treatment = (rng.random(n) < 0.85).astype(float)
    outcome = (rng.random(n) < 0.02).astype(float)

    def statistic(weights: np.ndarray) -> dict[str, float]:
        sample = u.rank_sample(scores, treatment, outcome)
        return {"qini": u.qini_coefficient(sample, points=200, weights=weights)}

    draws = u.bootstrap(statistic, size=n, replicates=200, seed=5)
    low, high = u.interval(draws["qini"])

    assert low < 0.0 < high


def test_bootstrap_is_reproducible_for_a_seed() -> None:
    rng = np.random.default_rng(2)
    scores = rng.normal(size=500)
    treatment = rng.integers(0, 2, 500).astype(float)
    outcome = rng.integers(0, 2, 500).astype(float)
    sample = u.rank_sample(scores, treatment, outcome)

    def statistic(weights: np.ndarray) -> dict[str, float]:
        return {"auuc": u.auuc(sample, points=100, weights=weights)}

    first = u.bootstrap(statistic, size=500, replicates=20, seed=9)["auuc"]
    second = u.bootstrap(statistic, size=500, replicates=20, seed=9)["auuc"]

    np.testing.assert_array_equal(first, second)


@pytest.mark.parametrize(
    ("scores", "treatment", "outcome"),
    [
        (np.array([1.0, 2.0]), np.array([1.0, 2.0]), np.array([0.0, 1.0])),
        (np.array([1.0, np.nan]), np.array([1.0, 0.0]), np.array([0.0, 1.0])),
        (np.array([1.0]), np.array([1.0, 0.0]), np.array([0.0])),
    ],
)
def test_invalid_inputs_are_rejected(scores: np.ndarray, treatment: np.ndarray, outcome: np.ndarray) -> None:
    with pytest.raises(ValueError):
        u.rank_sample(scores, treatment, outcome)


def test_tied_scores_give_the_same_metrics_for_any_row_order() -> None:
    rng = np.random.default_rng(7)
    n = 400
    scores = rng.integers(0, 5, n).astype(float)
    treatment = rng.integers(0, 2, n).astype(float)
    outcome = rng.integers(0, 2, n).astype(float)
    order = rng.permutation(n)
    fractions = u.uniform_grid(50)

    a = u.rank_sample(scores, treatment, outcome)
    b = u.rank_sample(scores[order], treatment[order], outcome[order])

    assert u.uplift_at_k(a, 0.3) == pytest.approx(u.uplift_at_k(b, 0.3))
    assert u.qini_coefficient(a, points=50) == pytest.approx(u.qini_coefficient(b, points=50))
    assert u.auuc(a, points=50) == pytest.approx(u.auuc(b, points=50))
    assert u.separate_relative_auuc(scores, treatment, outcome) == pytest.approx(
        u.separate_relative_auuc(scores[order], treatment[order], outcome[order])
    )
    np.testing.assert_allclose(
        u.conditional_profit(a, fractions, [0.01, 0.02]), u.conditional_profit(b, fractions, [0.01, 0.02])
    )


def test_a_fully_tied_population_has_the_overall_uplift_at_every_fraction() -> None:
    # One tie group: a top-k cut takes k of the group pro rata, so every fraction sees the overall uplift.
    treatment = np.array([1.0, 1.0, 1.0, 0.0, 0.0, 0.0])
    outcome = np.array([1.0, 0.0, 1.0, 0.0, 1.0, 0.0])
    sample = u.rank_sample(np.zeros(6), treatment, outcome)
    overall = 2.0 / 3.0 - 1.0 / 3.0

    for k in (0.1, 0.5, 1.0):
        assert u.uplift_at_k(sample, k) == pytest.approx(overall)
