"""End-to-end CLV fit on the clean Online Retail II artifacts. Slow (about 10 s): run with ``-m slow``."""

from __future__ import annotations

import dataclasses
from pathlib import Path

import pytest

from parley_ml.clv.config import load_fit_config
from parley_ml.clv.fit import TRANSACTIONS_FILE, run

ML_ROOT = Path(__file__).resolve().parents[1]
PRODUCTION_CONFIG = ML_ROOT / "configs" / "clv_fit.yaml"


@pytest.mark.slow
def test_fit_on_clean_data_passes_checks_and_counts_reconcile(tmp_path: Path) -> None:
    config = load_fit_config(PRODUCTION_CONFIG, root=ML_ROOT)
    if not (config.input_dir / TRANSACTIONS_FILE).exists():
        pytest.skip("Clean Online Retail II artifacts are missing; run parley_ml.datasets.online_retail first")
    config = dataclasses.replace(config, output_dir=tmp_path)

    result = run(config, PRODUCTION_CONFIG)
    fit = result["fit"]

    assert result["summary"]["all_checks_passed"]
    assert fit["bg_nbd"]["checks"]["passed"]
    assert fit["gamma_gamma"]["checks"]["passed"]
    counts = fit["counts"]
    assert counts["clean_lines"] == counts["clean_lines_read"] == 794_163
    assert counts["daily"]["lines"] == counts["clean_lines"]
    assert counts["split"]["calibration_days"] + counts["split"]["holdout_days"] == counts["daily"]["purchase_days"]
    assert counts["split"]["days_after_holdout_end"] == 0
