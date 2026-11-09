"""Ingest the UCI Online Retail II workbook into clean, checksummed Parquet.

Run from the ml/ directory:
    uv run --directory ml python -m parley_ml.datasets.online_retail --config configs/online_retail.yaml

Outputs (git-ignored): artifacts/online_retail/{transactions,customers}.parquet and manifest.json.
"""

from __future__ import annotations

import argparse
import json
import shutil
import sys
import urllib.error
import urllib.request
import zipfile
from pathlib import Path
from typing import Any

import pandas as pd
import pyarrow as pa
import pyarrow.parquet as pq
import yaml

from parley_ml.datasets.checksums import sha256_bytes, sha256_file, write_json
from parley_ml.datasets.online_retail_cleaning import (
    CANONICAL_COLUMNS,
    CUSTOMERS_FILE,
    CUSTOMERS_SCHEMA,
    SCHEMA_VERSION,
    TRANSACTIONS_FILE,
    TRANSACTIONS_SCHEMA,
    CleaningRules,
    build_customers,
    clean_lines,
)

_ML_ROOT = Path(__file__).resolve().parents[2]
_MANIFEST_FILE = "manifest.json"
_DOWNLOAD_CHUNK_BYTES = 1024 * 1024


class DatasetIdentityError(ValueError):
    """The workbook does not match the expected Online Retail II identity."""


class DatasetDownloadError(RuntimeError):
    """The archive could not be downloaded from the source."""


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Clean Online Retail II into Parquet.")
    parser.add_argument("--config", required=True, type=Path, help="Path to online_retail.yaml")
    args = parser.parse_args(argv)
    try:
        manifest = run(args.config)
    except (DatasetDownloadError, DatasetIdentityError) as error:
        print(f"online_retail: {error}", file=sys.stderr)
        return 1
    print(json.dumps(manifest["cleaning"], indent=2))
    print(json.dumps(manifest["outputs"], indent=2))
    return 0


def run(config_path: Path) -> dict[str, Any]:
    config_bytes = config_path.read_bytes()
    config = yaml.safe_load(config_bytes)
    dataset = config["dataset"]
    cache_dir = _ML_ROOT / config["paths"]["cache_dir"]
    output_dir = _ML_ROOT / config["paths"]["output_dir"]

    archive = ensure_archive(
        url=dataset["source_url"],
        destination=cache_dir / "online_retail_ii.zip",
        expected_sha256=dataset["archive_sha256"],
        expected_bytes=int(dataset["archive_bytes"]),
        user_agent=dataset["user_agent"],
        timeout_seconds=int(dataset["download_timeout_seconds"]),
    )
    workbook = extract_workbook(archive, cache_dir, dataset["workbook_member"], dataset["workbook_sha256"])

    sheets = pd.read_excel(workbook, sheet_name=None, engine="openpyxl")
    raw = load_canonical_lines(sheets, config["source_columns"])
    _verify_identity(raw, config["expected"])

    rules = CleaningRules.from_config(config["cleaning"], config["country_iso2"])
    lines, report = clean_lines(raw, rules)
    customers = build_customers(lines, report)

    output_dir.mkdir(parents=True, exist_ok=True)
    outputs = {
        TRANSACTIONS_FILE: _write_table(lines, TRANSACTIONS_SCHEMA, output_dir / TRANSACTIONS_FILE),
        CUSTOMERS_FILE: _write_table(customers, CUSTOMERS_SCHEMA, output_dir / CUSTOMERS_FILE),
    }
    manifest = {
        "schema_version": SCHEMA_VERSION,
        "dataset": {key: dataset[key] for key in ("name", "uci_id", "doi", "license", "creator", "source_url")},
        "config_sha256": sha256_bytes(config_bytes),
        "source": {
            "archive_sha256": dataset["archive_sha256"],
            "workbook_member": dataset["workbook_member"],
            "workbook_sha256": dataset["workbook_sha256"],
            "sheets": {name: len(frame) for name, frame in sheets.items()},
        },
        "identity": {
            "raw_rows": len(raw),
            "distinct_customers_raw": int(raw["customer_id"].nunique()),
            "rows_without_customer_id": int(raw["customer_id"].isna().sum()),
        },
        "cleaning": report.as_dict(),
        "clean": {
            "transaction_lines": len(lines),
            "customers": len(customers),
            "invoices_non_cancellation": int(lines.loc[~lines["is_cancellation"], "invoice_no"].nunique()),
        },
        "outputs": outputs,
        "versions": {"pandas": pd.__version__, "pyarrow": pa.__version__},
    }
    write_json(output_dir / _MANIFEST_FILE, manifest)
    return manifest


def ensure_archive(
    url: str,
    destination: Path,
    expected_sha256: str,
    expected_bytes: int,
    user_agent: str,
    timeout_seconds: int,
) -> Path:
    """Return the cached archive, downloading it first when missing or corrupt."""
    if destination.exists() and sha256_file(destination) == expected_sha256:
        return destination
    destination.parent.mkdir(parents=True, exist_ok=True)
    partial = destination.with_suffix(".part")
    _download(url, partial, expected_bytes, user_agent, timeout_seconds)
    if partial.stat().st_size != expected_bytes:
        partial.unlink()
        raise DatasetIdentityError(f"Archive size differs from the expected {expected_bytes} bytes")
    if sha256_file(partial) != expected_sha256:
        partial.unlink()
        raise DatasetIdentityError("Archive checksum differs from the expected SHA-256")
    partial.replace(destination)
    return destination


def _download(url: str, partial: Path, expected_bytes: int, user_agent: str, timeout_seconds: int) -> None:
    """Copy the response to ``partial``, reading at most one byte more than the expected size.

    The cap means a server that sends more than the expected archive cannot fill the disk; the size check in the caller
    then rejects the file. Network failures are reported as one clear error, and no partial file is left behind.
    """
    request = urllib.request.Request(url, headers={"User-Agent": user_agent})
    remaining = expected_bytes + 1
    try:
        with urllib.request.urlopen(request, timeout=timeout_seconds) as response, partial.open("wb") as handle:
            while remaining > 0 and (chunk := response.read(min(_DOWNLOAD_CHUNK_BYTES, remaining))):
                handle.write(chunk)
                remaining -= len(chunk)
    except urllib.error.URLError as error:
        partial.unlink(missing_ok=True)
        raise DatasetDownloadError(f"Could not download the Online Retail archive from {url}: {error.reason}") from error
    except TimeoutError as error:
        partial.unlink(missing_ok=True)
        raise DatasetDownloadError(f"The Online Retail archive download from {url} timed out.") from error


def extract_workbook(archive: Path, cache_dir: Path, member: str, expected_sha256: str) -> Path:
    """Extract the workbook to a fixed cache path; the member name is never used as a path."""
    target = cache_dir / member
    if target.exists() and sha256_file(target) == expected_sha256:
        return target
    with zipfile.ZipFile(archive) as bundle, bundle.open(member) as source, target.open("wb") as handle:
        shutil.copyfileobj(source, handle)
    if sha256_file(target) != expected_sha256:
        target.unlink()
        raise DatasetIdentityError("Workbook checksum differs from the expected SHA-256")
    return target


def load_canonical_lines(sheets: dict[str, pd.DataFrame], source_columns: dict[str, str]) -> pd.DataFrame:
    """Rename source columns to canonical names and stack all sheets with their origin."""
    parts: list[pd.DataFrame] = []
    for sheet_name, sheet in sheets.items():
        renamed = sheet.rename(columns={name: canonical for name, canonical in source_columns.items() if name in sheet.columns})
        missing = set(CANONICAL_COLUMNS) - set(renamed.columns) - {"source_sheet"}
        if missing:
            raise DatasetIdentityError(f"Sheet '{sheet_name}' miss columns: {sorted(missing)}")
        parts.append(
            pd.DataFrame(
                {
                    "invoice_no": renamed["invoice_no"].astype("string"),
                    "stock_code": renamed["stock_code"].astype("string"),
                    "description": renamed["description"].astype("string"),
                    "quantity": renamed["quantity"].astype("int64"),
                    "invoice_ts": pd.to_datetime(renamed["invoice_ts"]),
                    "unit_price": renamed["unit_price"].astype("float64"),
                    "customer_id": renamed["customer_id"].astype("Int64"),
                    "country_raw": renamed["country_raw"].astype("string").str.strip(),
                    "source_sheet": sheet_name,
                }
            )
        )
    return pd.concat(parts, ignore_index=True)


def _verify_identity(raw: pd.DataFrame, expected: dict[str, int]) -> None:
    observed = {
        "raw_rows": len(raw),
        "distinct_customers_raw": int(raw["customer_id"].nunique()),
        "rows_without_customer_id": int(raw["customer_id"].isna().sum()),
    }
    mismatches = {key: (observed[key], int(expected[key])) for key in expected if observed[key] != int(expected[key])}
    if mismatches:
        raise DatasetIdentityError(f"Workbook does not match the expected identity (observed, expected): {mismatches}")


def _write_table(frame: pd.DataFrame, schema: pa.Schema, path: Path) -> dict[str, Any]:
    table = pa.Table.from_pandas(frame, schema=schema, preserve_index=False)
    pq.write_table(table, path, compression="zstd")
    return {"rows": table.num_rows, "sha256": sha256_file(path)}


if __name__ == "__main__":
    raise SystemExit(main())
