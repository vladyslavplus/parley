"""Settings for the scoring service: a shared key and an artifact directory from the environment, limits from YAML."""

from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Final

import yaml

ML_ROOT: Final = Path(__file__).resolve().parents[2]
SERVING_CONFIG: Final = ML_ROOT / "configs" / "serving.yaml"
KEY_ENV: Final = "PARLEY_ML_KEY"
ARTIFACTS_ENV: Final = "PARLEY_ML_ARTIFACTS_DIR"
DEFAULT_ARTIFACTS_DIR: Final = ML_ROOT / "artifacts" / "models"
MAX_BATCH_SIZE_LIMIT: Final = 10_000


@dataclass(frozen=True)
class ServingConfig:
    max_batch_size: int


@dataclass(frozen=True)
class ServiceSettings:
    key: str = ""
    artifacts_dir: Path = DEFAULT_ARTIFACTS_DIR
    config: ServingConfig = ServingConfig(max_batch_size=1000)


def load_serving_config(path: Path = SERVING_CONFIG) -> ServingConfig:
    raw: Any = yaml.safe_load(path.read_text(encoding="utf-8"))
    if not isinstance(raw, dict):
        raise ValueError(f"{path} must hold a mapping")
    size = raw.get("max_batch_size")
    if not isinstance(size, int) or isinstance(size, bool) or not 1 <= size <= MAX_BATCH_SIZE_LIMIT:
        raise ValueError(f"max_batch_size must be an integer from 1 to {MAX_BATCH_SIZE_LIMIT}")
    return ServingConfig(max_batch_size=size)


def load_settings(environ: Mapping[str, str], config_path: Path = SERVING_CONFIG) -> ServiceSettings:
    """Read the shared key and artifact directory. The service refuses to start without a key."""
    key = environ.get(KEY_ENV, "")
    if not key.strip():
        raise RuntimeError(f"{KEY_ENV} is not set; the scoring service refuses to start without a shared key")
    artifacts = environ.get(ARTIFACTS_ENV, "").strip()
    return ServiceSettings(
        key=key,
        artifacts_dir=Path(artifacts) if artifacts else DEFAULT_ARTIFACTS_DIR,
        config=load_serving_config(config_path),
    )
