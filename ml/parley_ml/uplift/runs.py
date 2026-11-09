"""Run directories for uplift experiments: ids, resume check, environment, git state, GPU memory and validation metrics.

A run directory is ``<runs_root>/<run_id>/`` with ``config.json``, the fitted boosters (``*.ubj``) and ``metrics.json``.
``metrics.json`` is written last, through a temporary file, so its presence marks a finished run.
"""

from __future__ import annotations

import hashlib
import importlib.metadata as metadata
import json
import platform
import subprocess
import sys
import threading
from pathlib import Path
from typing import Any, Final

import numpy as np

from parley_ml.datasets.checksums import write_json
from parley_ml.evaluation.uplift import auuc, qini_coefficient, rank_sample, separate_relative_auuc, uplift_at_k
from parley_ml.uplift.config import ML_ROOT

SCHEMA_VERSION: Final = 1
METRICS_FILE: Final = "metrics.json"
CONFIG_FILE: Final = "config.json"
ENVIRONMENT_PACKAGES: Final[tuple[str, ...]] = (
    "numpy",
    "pandas",
    "polars",
    "xgboost",
    "causalml",
    "scikit-learn",
    "pyarrow",
)
_CREATE_NO_WINDOW: Final = 0x08000000 if sys.platform == "win32" else 0


def run_config_sha256(family_config_sha256: str, outcome: str, max_depth: int, learning_rate: float) -> str:
    """Hash of everything that defines one run. The family config bytes are part of it."""
    payload = {
        "family_config_sha256": family_config_sha256,
        "outcome": outcome,
        "max_depth": max_depth,
        "learning_rate": learning_rate,
    }
    return hashlib.sha256(json.dumps(payload, sort_keys=True).encode("utf-8")).hexdigest()


def run_id(family: str, outcome: str, config_sha256: str) -> str:
    return f"uplift-{family}-{outcome}-{config_sha256[:12]}"


def finished_metrics(run_dir: Path, config_sha256: str) -> dict[str, Any] | None:
    """Return the metrics of a finished run with the same config hash, or None when the run must be (re)done."""
    path = run_dir / METRICS_FILE
    if not path.exists():
        return None
    metrics = json.loads(path.read_text(encoding="utf-8"))
    if metrics.get("config_sha256") != config_sha256:
        return None
    return metrics


def write_run_config(run_dir: Path, payload: dict[str, Any]) -> None:
    write_json(run_dir / CONFIG_FILE, payload)


def write_metrics(run_dir: Path, payload: dict[str, Any]) -> None:
    temporary = run_dir / (METRICS_FILE + ".tmp")
    write_json(temporary, payload)
    temporary.replace(run_dir / METRICS_FILE)


def validation_metrics(
    scores: np.ndarray,
    treatment: np.ndarray,
    outcome: np.ndarray,
    uplift_k: tuple[float, ...],
) -> dict[str, Any]:
    """Point metrics on one split. Tied scores keep input order here; the final evaluation bootstraps the same functions."""
    sample = rank_sample(scores, treatment, outcome)
    return {
        "rows": int(scores.shape[0]),
        "treated_rows": int(np.sum(treatment == 1)),
        "control_rows": int(np.sum(treatment == 0)),
        "uplift_at_k": {f"{k:.2f}": uplift_at_k(sample, k) for k in uplift_k},
        "qini_coefficient": qini_coefficient(sample),
        "auuc": auuc(sample),
        "separate_relative_auuc": separate_relative_auuc(scores, treatment, outcome),
    }


def environment() -> dict[str, Any]:
    libraries: dict[str, str] = {}
    for name in ENVIRONMENT_PACKAGES:
        try:
            libraries[name] = metadata.version(name)
        except metadata.PackageNotFoundError:
            libraries[name] = "missing"
    identity = {"python": platform.python_version(), "platform": sys.platform, "libraries": libraries}
    digest = hashlib.sha256(json.dumps(identity, sort_keys=True).encode("utf-8")).hexdigest()
    return {**identity, "hash": digest[:12]}


def git_state() -> dict[str, Any]:
    commit = _run(["git", "rev-parse", "HEAD"], cwd=ML_ROOT.parent)
    status = _run(["git", "status", "--porcelain", "--", "ml"], cwd=ML_ROOT.parent)
    return {"commit": commit.strip() if commit else None, "ml_has_uncommitted_changes": bool(status and status.strip())}


def _run(command: list[str], cwd: Path) -> str | None:
    try:
        completed = subprocess.run(
            command, cwd=cwd, capture_output=True, text=True, timeout=60, creationflags=_CREATE_NO_WINDOW, check=False
        )
    except (OSError, subprocess.SubprocessError):
        return None
    return completed.stdout if completed.returncode == 0 else None


def gpu_memory_mib() -> int | None:
    output = _run(["nvidia-smi", "--query-gpu=memory.used", "--format=csv,noheader,nounits"], cwd=ML_ROOT)
    if not output:
        return None
    try:
        return int(output.strip().splitlines()[0])
    except (ValueError, IndexError):
        return None


class GpuMemorySampler:
    """Samples GPU memory use on a background thread and keeps the peak. Hidden process, no console window."""

    def __init__(self, interval_seconds: float = 1.0) -> None:
        self._interval = interval_seconds
        self._stop = threading.Event()
        self._thread = threading.Thread(target=self._loop, daemon=True)
        self.peak_mib: int | None = None

    def __enter__(self) -> "GpuMemorySampler":
        self.peak_mib = gpu_memory_mib()
        self._thread.start()
        return self

    def __exit__(self, *exc_info: object) -> None:
        self._stop.set()
        self._thread.join(timeout=10)

    def _loop(self) -> None:
        while not self._stop.wait(self._interval):
            value = gpu_memory_mib()
            if value is not None and (self.peak_mib is None or value > self.peak_mib):
                self.peak_mib = value


def checksums_payload(checksums: dict[str, str]) -> dict[str, str]:
    return dict(sorted(checksums.items()))
