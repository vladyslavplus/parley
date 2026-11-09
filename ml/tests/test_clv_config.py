"""CLV fit configuration: the production file parses, and invalid values are rejected (fast)."""

from __future__ import annotations

import copy
from datetime import date
from pathlib import Path
from typing import Any

import pytest
import yaml

from parley_ml.clv.config import load_fit_config, parse_fit_config

ML_ROOT = Path(__file__).resolve().parents[1]
PRODUCTION_CONFIG = ML_ROOT / "configs" / "clv_fit.yaml"


def _raw() -> dict[str, Any]:
    return copy.deepcopy(yaml.safe_load(PRODUCTION_CONFIG.read_text(encoding="utf-8")))


def test_production_config_parses_with_split_dates_and_budget() -> None:
    config = load_fit_config(PRODUCTION_CONFIG, root=ML_ROOT)

    assert config.calibration_end == date(2011, 6, 9)
    assert config.holdout_end == date(2011, 12, 9)
    assert config.input_dir == ML_ROOT / "artifacts" / "online_retail"
    assert config.output_dir == ML_ROOT / "artifacts" / "runs"
    assert config.gamma_gamma.min_frequency == 1
    assert config.checks.restart_scales == (0.5, 2.0)
    assert config.budget_seconds == 600
    assert len(config.config_sha256) == 64


@pytest.mark.parametrize(
    ("section", "key", "value", "message"),
    [
        ("split", "calibration_end", "2011-12-09", "before"),
        ("models", "bg_nbd", None, "bg_nbd"),
        ("checks", "restart_scales", [1.0], "restart_scales"),
        ("checks", "max_relative_gradient", 0, "positive"),
    ],
)
def test_invalid_values_are_rejected(section: str, key: str, value: Any, message: str) -> None:
    raw = _raw()
    if section == "models" and key == "bg_nbd":
        raw["models"]["bg_nbd"] = None
    else:
        raw[section][key] = value

    with pytest.raises(ValueError, match=message):
        parse_fit_config(raw, root=ML_ROOT, config_sha256="0" * 64)


def test_priors_must_be_library_defaults_and_method_map() -> None:
    raw = _raw()
    raw["models"]["gamma_gamma"]["priors"] = "custom"

    with pytest.raises(ValueError, match="library_defaults"):
        parse_fit_config(raw, root=ML_ROOT, config_sha256="0" * 64)
