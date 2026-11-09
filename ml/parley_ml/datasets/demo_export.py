"""Export the cleaned Online Retail II data as demo seed files for the CRM.

Run from the ml/ directory:
    uv run --directory ml python -m parley_ml.datasets.demo_export --config configs/demo_export.yaml

Reads the Parquet written by parley_ml.datasets.online_retail, verifies it against that
run's manifest, and writes gzip CSV files (fixed gzip mtime, so output is reproducible):
- contacts.csv.gz: one row per customer id, with the mapped country only.
- orders.csv.gz: one row per invoice (cancellations included, negative totals).
No names, emails or other personal data leave this step. The CRM seed generates them.
"""

from __future__ import annotations

import argparse
import csv
import gzip
import io
import json
from pathlib import Path
from typing import Any

import numpy as np
import pandas as pd
import yaml

from parley_ml.datasets.checksums import sha256_bytes, sha256_file, write_json
from parley_ml.datasets.online_retail_cleaning import CUSTOMERS_FILE, TRANSACTIONS_FILE

_ML_ROOT = Path(__file__).resolve().parents[2]
SOURCE_MANIFEST_FILE = "manifest.json"

CONTACT_COLUMNS: tuple[str, ...] = ("customer_id", "country_iso2")
ORDER_COLUMNS: tuple[str, ...] = (
    "invoice_no",
    "customer_id",
    "ordered_at",
    "total_amount",
    "currency",
    "line_count",
    "is_cancellation",
)


class DemoExportError(ValueError):
    """The input data does not match the source manifest or the export contract."""


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Export demo seed CSV files.")
    parser.add_argument("--config", required=True, type=Path, help="Path to demo_export.yaml")
    args = parser.parse_args(argv)
    manifest = run(args.config)
    print(json.dumps(manifest["outputs"], indent=2))
    return 0


def run(config_path: Path) -> dict[str, Any]:
    config_bytes = config_path.read_bytes()
    config = yaml.safe_load(config_bytes)
    input_dir = _ML_ROOT / config["paths"]["input_dir"]
    output_dir = _ML_ROOT / config["paths"]["output_dir"]
    currency = str(config["export"]["currency"])

    source_manifest = _verified_source_manifest(input_dir)
    transactions = pd.read_parquet(input_dir / TRANSACTIONS_FILE)
    customers = pd.read_parquet(input_dir / CUSTOMERS_FILE)

    contacts = _contacts(customers)
    orders = _orders(transactions, currency)

    output_dir.mkdir(parents=True, exist_ok=True)
    outputs = {
        "contacts.csv.gz": _write_csv_gz(contacts, CONTACT_COLUMNS, output_dir / "contacts.csv.gz"),
        "orders.csv.gz": _write_csv_gz(orders, ORDER_COLUMNS, output_dir / "orders.csv.gz"),
    }
    manifest = {
        "format_version": int(config["export"]["format_version"]),
        "attribution": config["attribution"],
        "config_sha256": sha256_bytes(config_bytes),
        "source_manifest_sha256": sha256_file(input_dir / SOURCE_MANIFEST_FILE),
        "source": source_manifest["dataset"],
        "currency": currency,
        "outputs": outputs,
    }
    write_json(output_dir / "manifest.json", manifest)
    return manifest


def _verified_source_manifest(input_dir: Path) -> dict[str, Any]:
    source = json.loads((input_dir / SOURCE_MANIFEST_FILE).read_text(encoding="utf-8"))
    for name, meta in source["outputs"].items():
        if sha256_file(input_dir / name) != meta["sha256"]:
            raise DemoExportError(f"Checksum of {name} differs from its source manifest")
    return source


def _contacts(customers: pd.DataFrame) -> pd.DataFrame:
    frame = customers.loc[:, list(CONTACT_COLUMNS)].copy()
    frame["customer_id"] = frame["customer_id"].astype("int64")
    frame["country_iso2"] = frame["country_iso2"].astype("object").where(frame["country_iso2"].notna(), "")
    return frame.sort_values("customer_id", kind="mergesort").reset_index(drop=True)


def _orders(transactions: pd.DataFrame, currency: str) -> pd.DataFrame:
    customers_per_invoice = transactions.groupby("invoice_no")["customer_id"].nunique()
    if (customers_per_invoice > 1).any():
        raise DemoExportError("An invoice maps to more than one customer id")

    grouped = transactions.groupby("invoice_no", sort=True)
    orders = pd.DataFrame(
        {
            "customer_id": grouped["customer_id"].first().astype("int64"),
            "ordered_at": grouped["invoice_ts"].min(),
            "total_amount": grouped["line_amount"].sum().round(2),
            "line_count": grouped["stock_code"].size().astype("int64"),
            "is_cancellation": grouped["is_cancellation"].first().astype(bool),
        }
    )
    orders["currency"] = currency
    orders = orders.reset_index().sort_values(["ordered_at", "invoice_no"], kind="mergesort")
    return orders.reset_index(drop=True)


def _write_csv_gz(frame: pd.DataFrame, columns: tuple[str, ...], path: Path) -> dict[str, Any]:
    text = _to_csv_text(frame, columns)
    buffer = io.BytesIO()
    with gzip.GzipFile(filename="", fileobj=buffer, mode="wb", mtime=0) as archive:
        archive.write(text.encode("utf-8"))
    payload = buffer.getvalue()
    path.write_bytes(payload)
    return {"rows": len(frame), "sha256": sha256_bytes(payload), "columns": list(columns)}


def _to_csv_text(frame: pd.DataFrame, columns: tuple[str, ...]) -> str:
    out = io.StringIO()
    writer = csv.writer(out, lineterminator="\n")
    writer.writerow(columns)
    for record in frame.loc[:, list(columns)].itertuples(index=False, name=None):
        writer.writerow(_format_cell(value) for value in record)
    return out.getvalue()


def _format_cell(value: object) -> str:
    if isinstance(value, pd.Timestamp):
        return value.strftime("%Y-%m-%dT%H:%M:%SZ")
    if isinstance(value, (bool, np.bool_)):
        return "1" if value else "0"
    if isinstance(value, float):
        return f"{value:.2f}"
    return "" if value is None else str(value)


if __name__ == "__main__":
    raise SystemExit(main())
