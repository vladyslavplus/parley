"""Deterministic stratified split of the Criteo uplift rows (pure numpy).

Strata are the 8 combinations of treatment x visit x conversion. Within each stratum the row indices are shuffled
with one seeded generator and cut into train, validation and test by the configured fractions. The same input and
seed give the same index arrays, byte for byte.
"""

from __future__ import annotations

from collections.abc import Mapping
from pathlib import Path
from typing import Final

import numpy as np

SPLIT_NAMES: Final[tuple[str, ...]] = ("train", "validation", "test")
_FRACTION_TOLERANCE: Final = 1e-9


def stratum_codes(treatment: np.ndarray, visit: np.ndarray, conversion: np.ndarray) -> np.ndarray:
    """Code 0..7 per row: 4 * treatment + 2 * visit + conversion."""
    t = np.asarray(treatment).astype(np.uint8)
    v = np.asarray(visit).astype(np.uint8)
    c = np.asarray(conversion).astype(np.uint8)
    return (4 * t + 2 * v + c).astype(np.uint8)


def build_split(codes: np.ndarray, fractions: Mapping[str, float], seed: int) -> dict[str, np.ndarray]:
    """Return sorted row indices per split name. Counts per stratum are rounded from the fractions."""
    if set(fractions) != set(SPLIT_NAMES):
        raise ValueError(f"fractions must name exactly {sorted(SPLIT_NAMES)}")
    if abs(sum(fractions.values()) - 1.0) > _FRACTION_TOLERANCE or min(fractions.values()) < 0:
        raise ValueError("fractions must be non-negative and sum to 1")

    rng = np.random.default_rng(seed)
    parts: dict[str, list[np.ndarray]] = {name: [] for name in SPLIT_NAMES}
    for code in np.unique(codes):
        members = np.flatnonzero(codes == code)
        shuffled = rng.permutation(members)
        n_train = round(members.size * fractions["train"])
        n_validation = round(members.size * fractions["validation"])
        parts["train"].append(shuffled[:n_train])
        parts["validation"].append(shuffled[n_train : n_train + n_validation])
        parts["test"].append(shuffled[n_train + n_validation :])
    return {name: np.sort(np.concatenate(chunks)).astype(np.int64) for name, chunks in parts.items()}


def check_disjoint_and_complete(splits: Mapping[str, np.ndarray], total_rows: int) -> None:
    """Every row appears in exactly one split."""
    joined = np.concatenate([splits[name] for name in SPLIT_NAMES])
    if joined.size != total_rows or np.unique(joined).size != total_rows:
        raise ValueError("splits must partition the rows: each row in exactly one split")


def arm_shares(treatment: np.ndarray, splits: Mapping[str, np.ndarray]) -> dict[str, float]:
    """Treatment share per split."""
    return {name: float(np.mean(np.asarray(treatment)[splits[name]])) for name in SPLIT_NAMES}


def write_split(splits: Mapping[str, np.ndarray], directory: Path) -> dict[str, dict[str, object]]:
    """Save each split as a .npy file of int64 row indices. Returns rows and SHA-256 per split."""
    from parley_ml.datasets.checksums import sha256_file

    directory.mkdir(parents=True, exist_ok=True)
    report: dict[str, dict[str, object]] = {}
    for name in SPLIT_NAMES:
        path = directory / f"{name}_indices.npy"
        np.save(path, splits[name])
        report[name] = {"file": path.name, "rows": int(splits[name].size), "sha256": sha256_file(path)}
    return report
