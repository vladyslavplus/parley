"""End-to-end evaluation on the clean artifacts. Slow (about 10 s with the PyMC-free path): run with ``-m slow``."""

from __future__ import annotations

import dataclasses
from pathlib import Path

import numpy as np
import pytest

from parley_ml.clv.evaluate import load_eval_config, run
from parley_ml.clv.fit import TRANSACTIONS_FILE

ML_ROOT = Path(__file__).resolve().parents[1]
EVAL_CONFIG = ML_ROOT / "configs" / "clv_eval.yaml"


@pytest.mark.slow
def test_evaluation_excludes_first_seen_customers_and_produces_finite_metrics(tmp_path: Path) -> None:
    config = load_eval_config(EVAL_CONFIG, root=ML_ROOT)
    if not (config.input_dir / TRANSACTIONS_FILE).exists():
        pytest.skip("Clean Online Retail II artifacts are missing; run parley_ml.datasets.online_retail first")
    config = dataclasses.replace(
        config,
        results_dir=tmp_path / "results",
        run_root=tmp_path / "runs",
        artifact_root=tmp_path / "models",
    )

    summary = run(config)["summary"]

    assert summary["exclusion"]["customers_first_seen_in_holdout"] == 908
    assert summary["population"]["all_customers"] == 4944
    assert summary["horizon_days"] == 183
    for population in ("all_customers", "repeat_buyers"):
        assert all(np.isfinite(value) for value in summary["metrics"][population].values())
    assert (tmp_path / "results" / "clv" / "metrics.csv").exists()
    assert (tmp_path / "models").exists()
