"""Integrity check of saved uplift boosters: a replaced booster must fail the frozen-metric comparison."""

from __future__ import annotations

import numpy as np
import xgboost as xgb

from parley_ml.evaluation.uplift import rank_sample
from parley_ml.uplift.final import INTEGRITY_REL_TOL, metric_mismatches, point_metrics

UPLIFT_K = (0.05, 0.1, 0.2, 0.3, 0.5)


def _data(rows: int = 3000, seed: int = 3) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    rng = np.random.default_rng(seed)
    features = rng.normal(size=(rows, 6)).astype(np.float32)
    treatment = rng.integers(0, 2, rows)
    outcome = (rng.random(rows) < 0.1 + 0.1 * treatment * (features[:, 0] > 0)).astype(float)
    return features, treatment.astype(float), outcome


def _scores(booster: xgb.Booster, features: np.ndarray) -> np.ndarray:
    return booster.predict(xgb.DMatrix(features))


def _metrics(scores: np.ndarray, treatment: np.ndarray, outcome: np.ndarray) -> dict[str, float]:
    sample = rank_sample(scores, treatment, outcome)
    return point_metrics(sample, scores, treatment, outcome, UPLIFT_K)


def _booster(depth: int, features: np.ndarray, outcome: np.ndarray, seed: int) -> xgb.Booster:
    params = {"objective": "binary:logistic", "tree_method": "hist", "device": "cpu", "max_depth": depth,
              "eta": 0.1, "seed": seed}
    return xgb.train(params, xgb.DMatrix(features, label=outcome), num_boost_round=20)


def test_the_original_booster_reproduces_its_own_frozen_metrics() -> None:
    features, treatment, outcome = _data()
    booster = _booster(3, features, outcome, seed=1)
    scores = _scores(booster, features)
    frozen = _metrics(scores, treatment, outcome)

    assert metric_mismatches(_metrics(scores, treatment, outcome), frozen) == []


def test_a_replaced_booster_fails_the_frozen_metric_comparison() -> None:
    features, treatment, outcome = _data()
    original = _booster(3, features, outcome, seed=1)
    frozen = _metrics(_scores(original, features), treatment, outcome)

    replaced = _booster(5, features, outcome, seed=2)
    problems = metric_mismatches(_metrics(_scores(replaced, features), treatment, outcome), frozen)

    assert problems, "a different booster must not reproduce the frozen metrics"


def test_tolerance_absorbs_only_floating_point_noise() -> None:
    frozen = {"qini_coefficient": 4000.0, "auuc": 12000.0}
    noise = {"qini_coefficient": 4000.0 * (1 + INTEGRITY_REL_TOL / 10), "auuc": 12000.0}
    shifted = {"qini_coefficient": 4000.0 * (1 + 1e-3), "auuc": 12000.0}

    assert metric_mismatches(noise, frozen) == []
    assert metric_mismatches(shifted, frozen) != []
