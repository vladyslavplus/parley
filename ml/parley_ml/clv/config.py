"""Configuration of the CLV fit (``configs/clv_fit.yaml``), parsed and validated without model imports."""

from __future__ import annotations

import hashlib
from dataclasses import dataclass
from datetime import date
from pathlib import Path
from typing import Any, Final, Mapping

import yaml

METHOD_MAP: Final = "map"
# Hex characters of the config SHA-256 used in run directory names (fit and evaluation).
RUN_HASH_CHARS: Final = 12
PRIORS_LIBRARY_DEFAULTS: Final = "library_defaults"


@dataclass(frozen=True)
class GammaGammaConfig:
    min_frequency: int
    min_monetary_value: float


@dataclass(frozen=True)
class FitChecksConfig:
    require_optimizer_success: bool
    max_relative_gradient: float
    restart_scales: tuple[float, ...]
    max_relative_objective_gap: float


@dataclass(frozen=True)
class FitConfig:
    dataset_name: str
    input_dir: Path
    output_dir: Path
    calibration_end: date
    holdout_end: date
    gamma_gamma: GammaGammaConfig
    checks: FitChecksConfig
    budget_seconds: float
    config_sha256: str


def load_fit_config(path: Path, root: Path) -> FitConfig:
    """Read the YAML file. Relative paths resolve against ``root`` (the ml/ directory)."""
    payload = path.read_bytes()
    raw = yaml.safe_load(payload)
    if not isinstance(raw, Mapping):
        raise ValueError(f"{path}: the config must be a mapping")
    return parse_fit_config(raw, root=root, config_sha256=hashlib.sha256(payload).hexdigest())


def parse_fit_config(raw: Mapping[str, Any], root: Path, config_sha256: str) -> FitConfig:
    dataset = _mapping(raw, "dataset")
    paths = _mapping(raw, "paths")
    split = _mapping(raw, "split")
    models = _mapping(raw, "models")
    bg = _mapping(models, "bg_nbd")
    gg = _mapping(models, "gamma_gamma")
    checks = _mapping(raw, "checks")

    _require_method(bg, "models.bg_nbd")
    _require_method(gg, "models.gamma_gamma")

    calibration_end = _date(split, "calibration_end", "split")
    holdout_end = _date(split, "holdout_end", "split")
    if not calibration_end < holdout_end:
        raise ValueError("split.calibration_end must be before split.holdout_end")

    min_frequency = _int(gg, "min_frequency", "models.gamma_gamma")
    if min_frequency < 1:
        raise ValueError("models.gamma_gamma.min_frequency must be at least 1 (repeat customers only)")
    min_monetary = _float(gg, "min_monetary_value", "models.gamma_gamma")
    if min_monetary < 0:
        raise ValueError("models.gamma_gamma.min_monetary_value must not be negative")

    max_gradient = _float(checks, "max_relative_gradient", "checks")
    max_gap = _float(checks, "max_relative_objective_gap", "checks")
    if max_gradient <= 0 or max_gap <= 0:
        raise ValueError("checks tolerances must be positive")
    restart_scales = _float_list(checks, "restart_scales", "checks")
    if not restart_scales or any(scale <= 0 or scale == 1.0 for scale in restart_scales):
        raise ValueError("checks.restart_scales must be positive factors other than 1")

    budget = _float(raw, "budget_seconds", "root")
    if budget <= 0:
        raise ValueError("budget_seconds must be positive")

    return FitConfig(
        dataset_name=_str(dataset, "name", "dataset"),
        input_dir=root / _str(paths, "input_dir", "paths"),
        output_dir=root / _str(paths, "output_dir", "paths"),
        calibration_end=calibration_end,
        holdout_end=holdout_end,
        gamma_gamma=GammaGammaConfig(min_frequency=min_frequency, min_monetary_value=min_monetary),
        checks=FitChecksConfig(
            require_optimizer_success=_bool(checks, "require_optimizer_success", "checks"),
            max_relative_gradient=max_gradient,
            restart_scales=tuple(restart_scales),
            max_relative_objective_gap=max_gap,
        ),
        budget_seconds=budget,
        config_sha256=config_sha256,
    )


def _mapping(container: Mapping[str, Any], key: str) -> Mapping[str, Any]:
    value = container.get(key)
    if not isinstance(value, Mapping):
        raise ValueError(f"Config section '{key}' is missing or not a mapping")
    return value


def _require_method(model: Mapping[str, Any], where: str) -> None:
    if model.get("method") != METHOD_MAP:
        raise ValueError(f"{where}.method must be '{METHOD_MAP}'")
    if model.get("priors") != PRIORS_LIBRARY_DEFAULTS:
        raise ValueError(f"{where}.priors must be '{PRIORS_LIBRARY_DEFAULTS}' (recorded in the fit output)")


def _date(section: Mapping[str, Any], key: str, where: str) -> date:
    value = section.get(key)
    if isinstance(value, date):
        return value
    if isinstance(value, str):
        return date.fromisoformat(value)
    raise ValueError(f"{where}.{key} must be an ISO date")


def _str(section: Mapping[str, Any], key: str, where: str) -> str:
    value = section.get(key)
    if not isinstance(value, str) or not value:
        raise ValueError(f"{where}.{key} must be a non-empty string")
    return value


def _int(section: Mapping[str, Any], key: str, where: str) -> int:
    value = section.get(key)
    if isinstance(value, bool) or not isinstance(value, int):
        raise ValueError(f"{where}.{key} must be an integer")
    return value


def _float(section: Mapping[str, Any], key: str, where: str) -> float:
    value = section.get(key)
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError(f"{where}.{key} must be a number")
    return float(value)


def _float_list(section: Mapping[str, Any], key: str, where: str) -> list[float]:
    values = section.get(key)
    if not isinstance(values, list) or any(isinstance(v, bool) or not isinstance(v, (int, float)) for v in values):
        raise ValueError(f"{where}.{key} must be a list of numbers")
    return [float(v) for v in values]


def _bool(section: Mapping[str, Any], key: str, where: str) -> bool:
    value = section.get(key)
    if not isinstance(value, bool):
        raise ValueError(f"{where}.{key} must be true or false")
    return value
