"""Ingest the Criteo uplift research dataset (v2.1) into float32 Parquet and write the stratified split.

Run from ml/:
    uv run --directory ml python -m parley_ml.datasets.criteo --config configs/criteo_ingest.yaml

The source archive is a gzip CSV in the Hugging Face dataset repository, pinned to a commit in the config. It is
downloaded into the git-ignored ``data/`` directory, checked against its size and SHA-256, and never committed.

Licence: CC BY-NC-SA 4.0 (non-commercial, share-alike). Results are research output; the model never serves real
contacts.
"""

from __future__ import annotations

import argparse
import json
import sys
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any, Final

import numpy as np
import polars as pl
import yaml

from parley_ml.datasets.checksums import sha256_bytes, sha256_file, write_json
from parley_ml.datasets.criteo_split import (
    SPLIT_NAMES,
    arm_shares,
    build_split,
    check_disjoint_and_complete,
    stratum_codes,
    write_split,
)

_ML_ROOT = Path(__file__).resolve().parents[2]
FEATURE_COLUMNS: Final[tuple[str, ...]] = tuple(f"f{index}" for index in range(12))
TREATMENT: Final = "treatment"
VISIT: Final = "visit"
CONVERSION: Final = "conversion"
EXPOSURE: Final = "exposure"
LABEL_COLUMNS: Final[tuple[str, ...]] = (TREATMENT, VISIT, CONVERSION, EXPOSURE)
SOURCE_COLUMNS: Final[tuple[str, ...]] = FEATURE_COLUMNS + LABEL_COLUMNS
# Exposure is recorded after treatment, so it is never a model input.
MODEL_FEATURE_COLUMNS: Final[tuple[str, ...]] = FEATURE_COLUMNS
PARQUET_COMPRESSION: Final = "zstd"
_DOWNLOAD_CHUNK_BYTES: Final = 1024 * 1024


class DatasetIdentityError(ValueError):
    """The file does not match the expected Criteo identity (size, checksum, rows, rates)."""


class DatasetDownloadError(RuntimeError):
    """The archive could not be downloaded."""


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Ingest the Criteo uplift dataset and write the split.")
    parser.add_argument("--config", required=True, type=Path, help="Path to criteo_ingest.yaml")
    args = parser.parse_args(argv)
    try:
        manifest = run(args.config)
    except (DatasetDownloadError, DatasetIdentityError) as error:
        print(f"criteo: {error}", file=sys.stderr)
        return 1
    print(json.dumps(manifest["rates"], indent=2))
    print(json.dumps(manifest["split"], indent=2))
    return 0


def run(config_path: Path) -> dict[str, Any]:
    config_bytes = config_path.read_bytes()
    config = yaml.safe_load(config_bytes)
    dataset = config["dataset"]
    paths = config["paths"]
    expected = config["expected"]
    cache_dir = _ML_ROOT / paths["cache_dir"]

    archive = ensure_archive(
        url=dataset["source_url"],
        destination=cache_dir / dataset["file_name"],
        expected_sha256=dataset["sha256"],
        expected_bytes=int(dataset["bytes"]),
        user_agent=dataset["user_agent"],
        timeout_seconds=int(dataset["download_timeout_seconds"]),
    )

    raw = pl.read_csv(archive)
    _verify_source(raw, expected)
    frame = raw.select(
        [pl.col(column).cast(pl.Float32) for column in FEATURE_COLUMNS]
        + [pl.col(column).cast(pl.Int8) for column in LABEL_COLUMNS]
    )
    rates = _rates(frame)
    _verify_rates(rates, expected)

    output = _ML_ROOT / paths["parquet"]
    output.parent.mkdir(parents=True, exist_ok=True)
    frame.write_parquet(output, compression=PARQUET_COMPRESSION)

    split_config = yaml.safe_load((_ML_ROOT / config["split_config"]).read_bytes())["split"]
    split_report = _write_split(frame, split_config, _ML_ROOT / paths["split_dir"])

    manifest = {
        "schema_version": 1,
        "dataset": {
            "name": dataset["name"],
            "source_url": dataset["source_url"],
            "source_revision": dataset["source_revision"],
            "file_name": dataset["file_name"],
            "file_sha256": dataset["sha256"],
            "file_bytes": int(dataset["bytes"]),
            "license": dataset["license"],
            "citation": dataset["citation"],
        },
        "config_sha256": sha256_bytes(config_bytes),
        "rows": frame.height,
        "columns": list(frame.columns),
        "rates": rates,
        "parquet": {"file": output.name, "sha256": sha256_file(output), "compression": PARQUET_COMPRESSION},
        "split": split_report,
        "libraries": {"polars": pl.__version__, "numpy": np.__version__},
    }
    write_json(_ML_ROOT / paths["manifest"], manifest)
    return manifest


def ensure_archive(
    url: str,
    destination: Path,
    expected_sha256: str,
    expected_bytes: int,
    user_agent: str,
    timeout_seconds: int,
) -> Path:
    """Return the cached file, downloading it first when missing or corrupt."""
    if destination.exists() and destination.stat().st_size == expected_bytes and sha256_file(destination) == expected_sha256:
        return destination
    destination.parent.mkdir(parents=True, exist_ok=True)
    partial = destination.with_suffix(destination.suffix + ".part")
    _download(url, partial, expected_bytes, user_agent, timeout_seconds)
    if partial.stat().st_size != expected_bytes:
        partial.unlink()
        raise DatasetIdentityError(f"Download size differs from the expected {expected_bytes} bytes")
    if sha256_file(partial) != expected_sha256:
        partial.unlink()
        raise DatasetIdentityError("Download checksum differs from the expected SHA-256")
    partial.replace(destination)
    return destination


def _download(url: str, partial: Path, expected_bytes: int, user_agent: str, timeout_seconds: int) -> None:
    """Copy the response to ``partial``, reading at most one byte more than the expected size."""
    request = urllib.request.Request(url, headers={"User-Agent": user_agent})
    remaining = expected_bytes + 1
    try:
        with urllib.request.urlopen(request, timeout=timeout_seconds) as response, partial.open("wb") as handle:
            while remaining > 0 and (chunk := response.read(min(_DOWNLOAD_CHUNK_BYTES, remaining))):
                handle.write(chunk)
                remaining -= len(chunk)
    except urllib.error.URLError as error:
        partial.unlink(missing_ok=True)
        raise DatasetDownloadError(f"Could not download the Criteo file from {url}: {error.reason}") from error
    except TimeoutError as error:
        partial.unlink(missing_ok=True)
        raise DatasetDownloadError(f"The Criteo download from {url} timed out.") from error


def _verify_source(raw: pl.DataFrame, expected: dict[str, Any]) -> None:
    missing = [column for column in SOURCE_COLUMNS if column not in raw.columns]
    if missing:
        raise DatasetIdentityError(f"Source misses columns: {missing}")
    if raw.height != int(expected["rows"]):
        raise DatasetIdentityError(f"Row count {raw.height} differs from the expected {expected['rows']}")


def _rates(frame: pl.DataFrame) -> dict[str, Any]:
    treated = frame.filter(pl.col(TREATMENT) == 1)
    control = frame.filter(pl.col(TREATMENT) == 0)
    visit_treated = float(treated[VISIT].mean())
    visit_control = float(control[VISIT].mean())
    conversion_treated = float(treated[CONVERSION].mean())
    conversion_control = float(control[CONVERSION].mean())
    return {
        "treatment_ratio": float(frame[TREATMENT].mean()),
        "visit_rate": float(frame[VISIT].mean()),
        "conversion_rate": float(frame[CONVERSION].mean()),
        "exposure_rate": float(frame[EXPOSURE].mean()),
        "by_arm": {
            "visit_rate_treated": visit_treated,
            "visit_rate_control": visit_control,
            "visit_uplift_absolute": visit_treated - visit_control,
            "conversion_rate_treated": conversion_treated,
            "conversion_rate_control": conversion_control,
            "conversion_uplift_absolute": conversion_treated - conversion_control,
            "treated_rows": treated.height,
            "control_rows": control.height,
        },
    }


def _verify_rates(rates: dict[str, Any], expected: dict[str, Any]) -> None:
    checks = {
        "treatment_ratio": (rates["treatment_ratio"], expected["treatment_ratio"]),
        "visit_rate": (rates["visit_rate"], expected["visit_rate"]),
        "conversion_rate": (rates["conversion_rate"], expected["conversion_rate"]),
    }
    for name, (observed, reference) in checks.items():
        tolerance = float(expected["tolerance"][name])
        if abs(observed - float(reference)) > tolerance:
            raise DatasetIdentityError(f"{name} {observed:.6f} differs from {reference} by more than {tolerance}")


def _write_split(frame: pl.DataFrame, split_config: dict[str, Any], directory: Path) -> dict[str, Any]:
    treatment = frame[TREATMENT].to_numpy()
    codes = stratum_codes(treatment, frame[VISIT].to_numpy(), frame[CONVERSION].to_numpy())
    splits = build_split(codes, split_config["fractions"], int(split_config["seed"]))
    check_disjoint_and_complete(splits, frame.height)

    overall = float(np.mean(treatment))
    shares = arm_shares(treatment, splits)
    tolerance = float(split_config["balance_tolerance"])
    balanced = {name: abs(share - overall) <= tolerance for name, share in shares.items()}
    if not all(balanced.values()):
        raise DatasetIdentityError(f"Treatment share out of balance by more than {tolerance}: {shares}")

    files = write_split(splits, directory)
    return {
        "seed": int(split_config["seed"]),
        "fractions": split_config["fractions"],
        "stratify_by": list(split_config["stratify_by"]),
        "treatment_share_overall": overall,
        "treatment_share_by_split": shares,
        "balance_tolerance": tolerance,
        "balanced": balanced,
        "files": files,
        "split_names": list(SPLIT_NAMES),
    }


if __name__ == "__main__":
    raise SystemExit(main())
