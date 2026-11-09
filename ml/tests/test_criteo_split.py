"""Criteo stratified split on synthetic labels: partition, strata, balance, determinism (fast, no data)."""

from __future__ import annotations

import numpy as np
import pytest

from parley_ml.datasets.criteo import MODEL_FEATURE_COLUMNS
from parley_ml.datasets.criteo_split import (
    arm_shares,
    build_split,
    check_disjoint_and_complete,
    stratum_codes,
)

FRACTIONS = {"train": 0.70, "validation": 0.10, "test": 0.20}


def _labels(rows: int, seed: int) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    rng = np.random.default_rng(seed)
    treatment = (rng.random(rows) < 0.85).astype(np.int8)
    visit = (rng.random(rows) < 0.05).astype(np.int8)
    conversion = (rng.random(rows) < 0.003).astype(np.int8)
    return treatment, visit, conversion


def test_split_partitions_every_row_once_with_the_configured_fractions() -> None:
    treatment, visit, conversion = _labels(50_000, seed=1)
    codes = stratum_codes(treatment, visit, conversion)

    splits = build_split(codes, FRACTIONS, seed=7)

    check_disjoint_and_complete(splits, treatment.size)
    assert splits["train"].size == pytest.approx(0.70 * treatment.size, abs=8)
    assert splits["validation"].size == pytest.approx(0.10 * treatment.size, abs=8)
    assert splits["test"].size == pytest.approx(0.20 * treatment.size, abs=8)


def test_split_is_deterministic_for_a_seed_and_differs_across_seeds() -> None:
    treatment, visit, conversion = _labels(20_000, seed=2)
    codes = stratum_codes(treatment, visit, conversion)

    first = build_split(codes, FRACTIONS, seed=7)
    second = build_split(codes, FRACTIONS, seed=7)
    other = build_split(codes, FRACTIONS, seed=8)

    for name in first:
        np.testing.assert_array_equal(first[name], second[name])
    assert not np.array_equal(first["test"], other["test"])


def test_every_stratum_is_represented_in_each_split_in_proportion() -> None:
    treatment, visit, conversion = _labels(80_000, seed=3)
    codes = stratum_codes(treatment, visit, conversion)
    splits = build_split(codes, FRACTIONS, seed=7)

    for code in np.unique(codes):
        stratum_total = int(np.sum(codes == code))
        for name, fraction in FRACTIONS.items():
            in_split = int(np.sum(codes[splits[name]] == code))
            assert in_split == pytest.approx(fraction * stratum_total, abs=1.0)


def test_arm_balance_is_within_rounding_in_every_split() -> None:
    treatment, visit, conversion = _labels(100_000, seed=4)
    codes = stratum_codes(treatment, visit, conversion)
    splits = build_split(codes, FRACTIONS, seed=7)

    overall = float(treatment.mean())
    for share in arm_shares(treatment, splits).values():
        assert abs(share - overall) < 1e-3


def test_fractions_must_name_the_three_splits_and_sum_to_one() -> None:
    codes = np.zeros(10, dtype=np.uint8)

    with pytest.raises(ValueError):
        build_split(codes, {"train": 0.8, "test": 0.2}, seed=1)
    with pytest.raises(ValueError):
        build_split(codes, {"train": 0.7, "validation": 0.1, "test": 0.3}, seed=1)


def test_stratum_codes_follow_treatment_visit_conversion_order() -> None:
    codes = stratum_codes(np.array([1, 1, 0, 0]), np.array([1, 0, 1, 0]), np.array([1, 0, 0, 1]))

    assert codes.tolist() == [7, 4, 2, 1]


def test_exposure_is_never_a_model_feature() -> None:
    assert "exposure" not in MODEL_FEATURE_COLUMNS
    assert "treatment" not in MODEL_FEATURE_COLUMNS
    assert "visit" not in MODEL_FEATURE_COLUMNS
    assert "conversion" not in MODEL_FEATURE_COLUMNS
    assert len(MODEL_FEATURE_COLUMNS) == 12
