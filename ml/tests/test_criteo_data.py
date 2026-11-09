"""Criteo artifacts on the local machine: identity, split reproduction, random baseline on the test split.

Slow and data-dependent: needs ml/data/criteo_uplift (run parley_ml.datasets.criteo first). Skipped with a reason
when the data is absent, and deselected from the default run (pytest -m slow).
"""

from __future__ import annotations

import json
from pathlib import Path

import numpy as np
import polars as pl
import pytest
import yaml

from parley_ml.datasets import criteo
from parley_ml.datasets.checksums import sha256_file
from parley_ml.datasets.criteo_split import build_split, stratum_codes
from parley_ml.evaluation import uplift as u

ML_ROOT = Path(__file__).resolve().parents[1]
INGEST_CONFIG = ML_ROOT / "configs" / "criteo_ingest.yaml"
DATA_DIR = ML_ROOT / "data" / "criteo_uplift"


def _config() -> dict:
    return yaml.safe_load(INGEST_CONFIG.read_text(encoding="utf-8"))


@pytest.fixture(scope="module")
def manifest() -> dict:
    path = DATA_DIR / "ingest_manifest.json"
    if not path.exists():
        pytest.skip("Criteo data missing: run parley_ml.datasets.criteo --config configs/criteo_ingest.yaml first")
    return json.loads(path.read_text(encoding="utf-8"))


@pytest.mark.slow
def test_parquet_matches_the_expected_identity(manifest: dict) -> None:
    expected = _config()["expected"]
    parquet = DATA_DIR / manifest["parquet"]["file"]

    assert sha256_file(parquet) == manifest["parquet"]["sha256"]
    frame = pl.read_parquet(parquet)
    assert frame.height == expected["rows"] == 13_979_592
    assert frame.columns == list(criteo.FEATURE_COLUMNS + criteo.LABEL_COLUMNS)
    tolerance = expected["tolerance"]
    assert frame["treatment"].mean() == pytest.approx(expected["treatment_ratio"], abs=tolerance["treatment_ratio"])
    assert frame["visit"].mean() == pytest.approx(expected["visit_rate"], abs=tolerance["visit_rate"])
    assert frame["conversion"].mean() == pytest.approx(expected["conversion_rate"], abs=tolerance["conversion_rate"])


@pytest.mark.slow
def test_split_files_are_reproduced_from_the_parquet(manifest: dict) -> None:
    split_config = yaml.safe_load((ML_ROOT / _config()["split_config"]).read_text(encoding="utf-8"))["split"]
    frame = pl.read_parquet(DATA_DIR / manifest["parquet"]["file"], columns=["treatment", "visit", "conversion"])
    codes = stratum_codes(frame["treatment"].to_numpy(), frame["visit"].to_numpy(), frame["conversion"].to_numpy())

    splits = build_split(codes, split_config["fractions"], int(split_config["seed"]))

    for name, info in manifest["split"]["files"].items():
        saved = np.load(DATA_DIR / "splits" / info["file"])
        np.testing.assert_array_equal(saved, splits[name])
        assert sha256_file(DATA_DIR / "splits" / info["file"]) == info["sha256"]


@pytest.mark.slow
def test_random_baseline_qini_interval_covers_zero_on_the_validation_split(manifest: dict) -> None:
    # Validation split only: the test split is opened solely by the final evaluation, never by the default or slow runs.
    rows = np.load(DATA_DIR / "splits" / manifest["split"]["files"]["validation"]["file"])
    frame = pl.read_parquet(DATA_DIR / manifest["parquet"]["file"]).select(["treatment", "visit"])
    treatment = frame["treatment"].to_numpy()[rows].astype(float)
    visit = frame["visit"].to_numpy()[rows].astype(float)
    rng = np.random.default_rng(20261111)
    random_scores = rng.random(rows.size)
    sample = u.rank_sample(random_scores, treatment, visit)

    draws = u.bootstrap(
        lambda weights: {"qini": u.qini_coefficient(sample, weights=weights)},
        size=rows.size,
        replicates=200,
        seed=20261112,
    )
    low, high = u.interval(draws["qini"])

    assert low < 0.0 < high
