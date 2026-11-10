"""Fast CPU checks for the uplift learners, run records and config guards (no Criteo data needed)."""

from __future__ import annotations

import json
from pathlib import Path

import numpy as np
import pytest

from parley_ml.uplift.config import ML_ROOT, Cell, load_family_config, load_grid_config
from parley_ml.uplift.data import SplitArrays, require_fit_splits
from parley_ml.uplift.learners import LEARNERS, BoosterSettings, causalml_reference
from parley_ml.uplift.runs import finished_metrics, run_config_sha256
from parley_ml.uplift.train_grid import GridRow, select_winners

SEED = 20261112


def _params(max_depth: int = 3) -> dict:
    return {
        "objective": "binary:logistic",
        "eval_metric": "logloss",
        "tree_method": "hist",
        "device": "cpu",
        "max_bin": 256,
        "max_depth": max_depth,
        "eta": 0.1,
        "seed": SEED,
    }


def _synthetic(rows: int, seed: int) -> tuple[SplitArrays, np.ndarray]:
    rng = np.random.default_rng(seed)
    features = rng.normal(size=(rows, 12)).astype(np.float32)
    treatment = rng.integers(0, 2, rows).astype(np.int8)
    base = 1.0 / (1.0 + np.exp(-(0.6 * features[:, 0] - 1.4)))
    effect = 0.15 * (features[:, 1] > 0) * treatment
    outcome = (rng.random(rows) < np.clip(base + effect, 0, 1)).astype(np.int8)
    split = SplitArrays(name="train", features=features, treatment=treatment, labels={"visit": outcome, "conversion": outcome})
    return split, features


@pytest.mark.parametrize("family", ["t", "s"])
def test_native_learners_match_causalml_at_fixed_rounds(family: str) -> None:
    train, _ = _synthetic(3000, seed=1)
    evaluation, eval_features = _synthetic(500, seed=2)
    params = _params()
    rounds = 15
    settings = BoosterSettings(params=params, early_stopping_rounds=None, max_rounds=rounds)

    learner = LEARNERS[family]()
    learner.fit(train, evaluation, "visit", settings)
    ours = learner.score(eval_features)
    reference = causalml_reference(family, train, "visit", eval_features, params, rounds=rounds)

    np.testing.assert_allclose(ours, reference, rtol=1e-5, atol=1e-6)


def test_early_stopping_keeps_rounds_up_to_the_best_iteration() -> None:
    train, _ = _synthetic(3000, seed=3)
    validation, _ = _synthetic(1000, seed=4)
    settings = BoosterSettings(params=_params(max_depth=2), early_stopping_rounds=5, max_rounds=300)

    learner = LEARNERS["response"]()
    rounds = learner.fit(train, validation, "visit", settings)

    kept = rounds["response"]
    assert 1 <= kept <= 300
    assert learner.models["response"].booster.num_boosted_rounds() >= kept


def test_fits_are_reproducible_with_a_fixed_seed() -> None:
    train, _ = _synthetic(2000, seed=5)
    validation, eval_features = _synthetic(400, seed=6)
    settings = BoosterSettings(params=_params(), early_stopping_rounds=10, max_rounds=60)

    first = LEARNERS["t"]()
    first.fit(train, validation, "visit", settings)
    second = LEARNERS["t"]()
    second.fit(train, validation, "visit", settings)

    np.testing.assert_array_equal(first.score(eval_features), second.score(eval_features))


def test_test_split_cannot_be_loaded_before_the_final_evaluation() -> None:
    with pytest.raises(ValueError, match="not fit splits"):
        require_fit_splits(("train", "test"))
    require_fit_splits(("train", "validation"))


def test_exposure_is_rejected_as_a_feature(tmp_path: Path) -> None:
    text = (ML_ROOT / "configs" / "uplift_t.yaml").read_text(encoding="utf-8")
    altered = text.replace("f10, f11]", "f10, f11, exposure]", 1)
    assert altered != text
    path = tmp_path / "uplift_bad.yaml"
    path.write_text(altered, encoding="utf-8", newline="\n")

    with pytest.raises(ValueError, match="post-treatment"):
        load_family_config(path)


def test_finished_run_is_reused_only_for_the_same_config(tmp_path: Path) -> None:
    run_dir = tmp_path / "uplift-t-visit-abc"
    run_dir.mkdir()
    config_sha = run_config_sha256("a" * 64, "visit", 4, 0.05)
    (run_dir / "metrics.json").write_text(json.dumps({"config_sha256": config_sha, "run_id": "x"}), encoding="utf-8")

    assert finished_metrics(run_dir, config_sha) is not None
    assert finished_metrics(run_dir, run_config_sha256("a" * 64, "visit", 6, 0.05)) is None
    assert finished_metrics(tmp_path / "missing", config_sha) is None


def test_grid_has_every_depth_and_rate_and_winners_come_from_validation_metrics() -> None:
    grid = load_grid_config(ML_ROOT / "configs" / "uplift_grid.yaml")
    assert [(cell.max_depth, cell.learning_rate) for cell in grid.cells] == [
        (4, 0.05),
        (4, 0.1),
        (6, 0.05),
        (6, 0.1),
    ]

    def row(depth: int, qini: float) -> GridRow:
        return GridRow(
            family="t",
            outcome="visit",
            max_depth=depth,
            learning_rate=0.05,
            run_id=f"run-{depth}",
            reused=False,
            metrics={"qini_coefficient": qini, "auuc": 0.0, "separate_relative_auuc": 0.0, "uplift_at_k": {}},
            wall_seconds=None,
            gpu_memory_peak_mib=None,
            rounds="",
        )

    winners = select_winners([row(4, 1.0), row(6, 2.0), row(4, 2.0)], "qini_coefficient")
    assert winners[("t", "visit")].run_id == "run-6"
    assert Cell(max_depth=4, learning_rate=0.05) in grid.cells
