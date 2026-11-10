"""Load the Criteo feature matrix and the fit splits (train, validation) with checksum checks.

The test split is never loaded here. ``load_bundle`` refuses any split name outside ``FIT_SPLITS``.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from typing import Any, Final

import numpy as np
import polars as pl

from parley_ml.datasets.checksums import sha256_file
from parley_ml.datasets.criteo import CONVERSION, VISIT
from parley_ml.uplift.config import FIT_SPLITS, FamilyConfig

_LABEL_COLUMNS = {"visit": VISIT, "conversion": CONVERSION}
TEST_SPLIT: Final = "test"
ALL_SPLITS: Final = (*FIT_SPLITS, TEST_SPLIT)


class SplitChecksumError(RuntimeError):
    """A parquet or split file differs from the checksum recorded in the ingest manifest."""


@dataclass(frozen=True)
class SplitArrays:
    name: str
    features: np.ndarray  # float32, shape (rows, features)
    treatment: np.ndarray  # int8
    labels: dict[str, np.ndarray]  # outcome name -> int8

    @property
    def rows(self) -> int:
        return int(self.features.shape[0])

    def outcome(self, name: str) -> np.ndarray:
        return self.labels[name]


@dataclass(frozen=True)
class DataBundle:
    splits: dict[str, SplitArrays]
    checksums: dict[str, str]  # file name -> sha256 verified against the manifest
    subsample: int | None

    def __getitem__(self, name: str) -> SplitArrays:
        return self.splits[name]


def require_fit_splits(names: tuple[str, ...]) -> None:
    """Raise when a split outside the fit splits is requested. The test split waits for the final evaluation."""
    forbidden = [name for name in names if name not in FIT_SPLITS]
    if forbidden:
        raise ValueError(f"Splits {forbidden} are not fit splits; only {list(FIT_SPLITS)} may be loaded here")


def load_bundle(
    config: FamilyConfig,
    subsample: int | None = None,
    names: tuple[str, ...] = FIT_SPLITS,
    allow_test: bool = False,
) -> DataBundle:
    """Load the named splits. ``subsample`` keeps a seeded random subset per split (smoke runs only).

    The test split is loaded only with ``allow_test=True``, which the final evaluation passes after it has written the
    freeze file.
    """
    if TEST_SPLIT in names and not allow_test:
        raise ValueError("The test split is opened only by the final evaluation, after the freeze file is written")
    unknown = [name for name in names if name not in ALL_SPLITS]
    if unknown:
        raise ValueError(f"Unknown splits {unknown}")
    manifest = json.loads(config.manifest.read_text(encoding="utf-8"))
    checksums: dict[str, str] = {}

    parquet_sha = sha256_file(config.parquet)
    if parquet_sha != manifest["parquet"]["sha256"]:
        raise SplitChecksumError(f"{config.parquet} differs from the manifest checksum")
    checksums[config.parquet.name] = parquet_sha

    frame = pl.read_parquet(config.parquet, columns=list(config.features) + [config.treatment, VISIT, CONVERSION])
    matrix = frame.select(list(config.features)).to_numpy().astype(np.float32, copy=False)
    treatment = frame[config.treatment].to_numpy().astype(np.int8, copy=False)
    labels = {name: frame[column].to_numpy().astype(np.int8, copy=False) for name, column in _LABEL_COLUMNS.items()}

    splits: dict[str, SplitArrays] = {}
    for name in names:
        info = manifest["split"]["files"][name]
        path = config.split_dir / info["file"]
        digest = sha256_file(path)
        if digest != info["sha256"]:
            raise SplitChecksumError(f"{path} differs from the manifest checksum")
        checksums[path.name] = digest
        indices = np.load(path)
        if subsample is not None and subsample < indices.shape[0]:
            rng = np.random.default_rng(config.seed)
            indices = np.sort(rng.choice(indices, size=subsample, replace=False))
        splits[name] = SplitArrays(
            name=name,
            features=matrix[indices],
            treatment=treatment[indices],
            labels={outcome: labels[outcome][indices] for outcome in labels},
        )
    return DataBundle(splits=splits, checksums=checksums, subsample=subsample)


def describe_bundle(bundle: DataBundle) -> dict[str, Any]:
    return {
        name: {
            "rows": split.rows,
            "treated_rows": int(np.sum(split.treatment == 1)),
            "control_rows": int(np.sum(split.treatment == 0)),
        }
        for name, split in bundle.splits.items()
    }


def load_test_split(config: FamilyConfig) -> SplitArrays:
    """Load only the test rows, one column at a time, so the full table is never held in memory.

    Checks the parquet and the test index file against the ingest manifest first. Used by the final evaluation only.
    """
    manifest = json.loads(config.manifest.read_text(encoding="utf-8"))
    if sha256_file(config.parquet) != manifest["parquet"]["sha256"]:
        raise SplitChecksumError(f"{config.parquet} differs from the manifest checksum")
    info = manifest["split"]["files"][TEST_SPLIT]
    path = config.split_dir / info["file"]
    if sha256_file(path) != info["sha256"]:
        raise SplitChecksumError(f"{path} differs from the manifest checksum")
    indices = np.load(path)

    features = np.empty((indices.shape[0], len(config.features)), dtype=np.float32)
    for index, column in enumerate(config.features):
        values = pl.read_parquet(config.parquet, columns=[column])[column].to_numpy()
        features[:, index] = values[indices]
        del values
    treatment = pl.read_parquet(config.parquet, columns=[config.treatment])[config.treatment].to_numpy()
    labels = {}
    for name, column in _LABEL_COLUMNS.items():
        values = pl.read_parquet(config.parquet, columns=[column])[column].to_numpy()
        labels[name] = values[indices].astype(np.int8, copy=False)
        del values
    return SplitArrays(
        name=TEST_SPLIT,
        features=features,
        treatment=treatment[indices].astype(np.int8, copy=False),
        labels=labels,
    )
