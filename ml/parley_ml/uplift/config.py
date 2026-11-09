"""Parse the uplift family configs (``configs/uplift_<family>.yaml``) and the grid config (``configs/uplift_grid.yaml``).

Paths in the configs are relative to the ml/ directory.
"""

from __future__ import annotations

import hashlib
import itertools
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Final, Mapping

import yaml

from parley_ml.datasets.criteo import EXPOSURE, FEATURE_COLUMNS, TREATMENT

ML_ROOT: Final = Path(__file__).resolve().parents[2]
FAMILIES: Final[tuple[str, ...]] = ("response", "s", "t")
OUTCOMES: Final[tuple[str, ...]] = ("visit", "conversion")
# The test split is not fit or selected on. The final evaluation step is the only place it is opened.
FIT_SPLITS: Final[tuple[str, ...]] = ("train", "validation")
SELECTION_METRICS: Final[tuple[str, ...]] = ("qini_coefficient", "auuc", "separate_relative_auuc")


@dataclass(frozen=True)
class FamilyConfig:
    family: str
    path: Path
    config_sha256: str
    parquet: Path
    manifest: Path
    split_dir: Path
    features: tuple[str, ...]
    treatment: str
    outcomes: tuple[str, ...]
    seed: int
    xgboost: Mapping[str, Any]
    early_stopping_rounds: int
    max_rounds: int
    uplift_k: tuple[float, ...]


@dataclass(frozen=True)
class Cell:
    max_depth: int
    learning_rate: float


@dataclass(frozen=True)
class GridConfig:
    path: Path
    config_sha256: str
    cells: tuple[Cell, ...]
    families: tuple[Path, ...]
    runs_root: Path
    summary_csv: Path
    summary_markdown: Path
    selection_split: str
    selection_metric: str


def load_family_config(path: Path) -> FamilyConfig:
    payload = path.read_bytes()
    raw = yaml.safe_load(payload)
    if not isinstance(raw, Mapping):
        raise ValueError(f"{path}: the config must be a mapping")

    family = _str(raw, "family", str(path))
    if family not in FAMILIES:
        raise ValueError(f"{path}: family must be one of {FAMILIES}")
    dataset = _mapping(raw, "dataset", str(path))
    fit_splits = tuple(_list(dataset, "fit_splits", "dataset"))
    if fit_splits != FIT_SPLITS:
        raise ValueError(f"dataset.fit_splits must be {list(FIT_SPLITS)}; the test split is opened only in the final evaluation step")

    features = tuple(_list(raw, "features", str(path)))
    if EXPOSURE in features:
        raise ValueError(f"{path}: '{EXPOSURE}' is post-treatment and must not be a feature")
    if features != FEATURE_COLUMNS:
        raise ValueError(f"{path}: features must be {list(FEATURE_COLUMNS)}")
    treatment = _str(raw, "treatment", str(path))
    if treatment != TREATMENT:
        raise ValueError(f"{path}: treatment must be '{TREATMENT}'")

    outcomes = tuple(_list(raw, "outcomes", str(path)))
    if not outcomes or any(outcome not in OUTCOMES for outcome in outcomes):
        raise ValueError(f"{path}: outcomes must be a non-empty subset of {OUTCOMES}")

    xgboost = _mapping(raw, "xgboost", str(path))
    if xgboost.get("objective") != "binary:logistic":
        raise ValueError(f"{path}: xgboost.objective must be binary:logistic")
    if xgboost.get("device") not in ("cuda", "cpu"):
        raise ValueError(f"{path}: xgboost.device must be 'cuda' or 'cpu'")

    early = _int(raw, "early_stopping_rounds", str(path))
    max_rounds = _int(raw, "max_rounds", str(path))
    if early < 1 or max_rounds < early:
        raise ValueError(f"{path}: need 1 <= early_stopping_rounds <= max_rounds")

    evaluation = _mapping(raw, "evaluation", str(path))
    uplift_k = tuple(float(value) for value in _list(evaluation, "uplift_k", "evaluation"))
    if not uplift_k or any(not 0.0 < value <= 1.0 for value in uplift_k):
        raise ValueError(f"{path}: evaluation.uplift_k values must be in (0, 1]")

    return FamilyConfig(
        family=family,
        path=path,
        config_sha256=hashlib.sha256(payload).hexdigest(),
        parquet=ML_ROOT / _str(dataset, "parquet", "dataset"),
        manifest=ML_ROOT / _str(dataset, "manifest", "dataset"),
        split_dir=ML_ROOT / _str(dataset, "split_dir", "dataset"),
        features=features,
        treatment=treatment,
        outcomes=outcomes,
        seed=_int(raw, "seed", str(path)),
        xgboost=dict(xgboost),
        early_stopping_rounds=early,
        max_rounds=max_rounds,
        uplift_k=uplift_k,
    )


def load_grid_config(path: Path) -> GridConfig:
    payload = path.read_bytes()
    raw = yaml.safe_load(payload)
    if not isinstance(raw, Mapping):
        raise ValueError(f"{path}: the config must be a mapping")
    grid = _mapping(raw, "grid", str(path))
    depths = [int(value) for value in _list(grid, "max_depth", "grid")]
    rates = [float(value) for value in _list(grid, "learning_rate", "grid")]
    if not depths or not rates or any(depth < 1 for depth in depths) or any(not 0 < rate <= 1 for rate in rates):
        raise ValueError(f"{path}: grid needs positive max_depth values and learning rates in (0, 1]")
    cells = tuple(Cell(max_depth=depth, learning_rate=rate) for depth, rate in itertools.product(depths, rates))

    selection = _mapping(raw, "selection", str(path))
    split = _str(selection, "split", "selection")
    if split != "validation":
        raise ValueError("selection.split must be 'validation'; winners are never chosen on the test split")
    metric = _str(selection, "metric", "selection")
    if metric not in SELECTION_METRICS:
        raise ValueError(f"selection.metric must be one of {SELECTION_METRICS}")

    return GridConfig(
        path=path,
        config_sha256=hashlib.sha256(payload).hexdigest(),
        cells=cells,
        families=tuple(ML_ROOT / _family_path(item, str(path)) for item in _list(raw, "families", str(path))),
        runs_root=ML_ROOT / _str(raw, "runs_root", str(path)),
        summary_csv=ML_ROOT / _str(raw, "summary_csv", str(path)),
        summary_markdown=ML_ROOT / _str(raw, "summary_markdown", str(path)),
        selection_split=split,
        selection_metric=metric,
    )


def _mapping(container: Mapping[str, Any], key: str, where: str) -> Mapping[str, Any]:
    value = container.get(key)
    if not isinstance(value, Mapping):
        raise ValueError(f"{where}: section '{key}' is missing or not a mapping")
    return value


def _str(container: Mapping[str, Any], key: str, where: str) -> str:
    value = container.get(key)
    if not isinstance(value, str) or not value:
        raise ValueError(f"{where}.{key} must be a non-empty string")
    return value


def _int(container: Mapping[str, Any], key: str, where: str) -> int:
    value = container.get(key)
    if isinstance(value, bool) or not isinstance(value, int):
        raise ValueError(f"{where}.{key} must be an integer")
    return value


def _family_path(value: Any, where: str) -> str:
    if not isinstance(value, str) or not value:
        raise ValueError(f"{where}: each families entry must be a path string")
    return value


def _list(container: Mapping[str, Any], key: str, where: str) -> list[Any]:
    value = container.get(key)
    if not isinstance(value, list):
        raise ValueError(f"{where}.{key} must be a list")
    return value
