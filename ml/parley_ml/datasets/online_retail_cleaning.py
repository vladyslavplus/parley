"""Cleaning rules for Online Retail II lines and the derived customer table.

The rules run in a fixed order. Each rule records how many rows it removed,
so the counts reconcile from raw rows to clean rows.
"""

from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass, field
from typing import Any

import pandas as pd
import pyarrow as pa

SCHEMA_VERSION = 1

TRANSACTIONS_FILE = "transactions.parquet"
CUSTOMERS_FILE = "customers.parquet"

TRANSACTIONS_SCHEMA = pa.schema(
    [
        ("invoice_no", pa.string()),
        ("invoice_ts", pa.timestamp("us", tz="UTC")),
        ("customer_id", pa.int64()),
        ("stock_code", pa.string()),
        ("quantity", pa.int64()),
        ("unit_price", pa.float64()),
        ("line_amount", pa.float64()),
        ("is_cancellation", pa.bool_()),
        ("country_iso2", pa.string()),
        ("source_sheet", pa.string()),
    ]
)

CUSTOMERS_SCHEMA = pa.schema(
    [
        ("customer_id", pa.int64()),
        ("country_iso2", pa.string()),
        ("first_line_ts", pa.timestamp("us", tz="UTC")),
        ("last_line_ts", pa.timestamp("us", tz="UTC")),
        ("line_count", pa.int64()),
        ("invoice_count", pa.int64()),
    ]
)

# The eight source columns (canonical names) that make a row an exact duplicate.
DUPLICATE_KEY: tuple[str, ...] = (
    "invoice_no",
    "stock_code",
    "description",
    "quantity",
    "invoice_ts",
    "unit_price",
    "customer_id",
    "country_raw",
)

CANONICAL_COLUMNS: tuple[str, ...] = DUPLICATE_KEY + ("source_sheet",)


@dataclass(frozen=True)
class CleaningRules:
    cancellation_prefix: str
    product_stock_code_pattern: str
    overlap_start: pd.Timestamp
    overlap_end_exclusive: pd.Timestamp
    country_iso2: Mapping[str, str | None]

    @classmethod
    def from_config(cls, cleaning: Mapping[str, Any], countries: Mapping[str, str | None]) -> CleaningRules:
        return cls(
            cancellation_prefix=str(cleaning["cancellation_invoice_prefix"]),
            product_stock_code_pattern=str(cleaning["product_stock_code_pattern"]),
            overlap_start=pd.Timestamp(str(cleaning["sheet_overlap_start"])),
            overlap_end_exclusive=pd.Timestamp(str(cleaning["sheet_overlap_end_exclusive"])),
            country_iso2=dict(countries),
        )


@dataclass
class CleaningReport:
    rows_in: int
    removed: dict[str, int] = field(default_factory=dict)
    details: dict[str, Any] = field(default_factory=dict)
    rows_out: int = 0

    def drop(self, frame: pd.DataFrame, mask: pd.Series, rule: str) -> pd.DataFrame:
        self.removed[rule] = int(mask.sum())
        return frame.loc[~mask]

    def as_dict(self) -> dict[str, Any]:
        return {
            "rows_in": self.rows_in,
            "removed": dict(self.removed),
            "rows_out": self.rows_out,
            "details": dict(self.details),
        }


def clean_lines(raw: pd.DataFrame, rules: CleaningRules) -> tuple[pd.DataFrame, CleaningReport]:
    """Apply the cleaning rules to canonical raw lines.

    Input: frame with CANONICAL_COLUMNS. Output: frame with TRANSACTIONS_SCHEMA names.
    """
    missing_columns = set(CANONICAL_COLUMNS) - set(raw.columns)
    if missing_columns:
        raise ValueError(f"Raw lines miss canonical columns: {sorted(missing_columns)}")

    report = CleaningReport(rows_in=len(raw))
    frame = raw.assign(stock_code=raw["stock_code"].str.strip())

    frame = report.drop(frame, frame["customer_id"].isna(), "rows_without_customer_id")

    duplicated = frame.duplicated(subset=list(DUPLICATE_KEY), keep="first")
    if duplicated.any():
        sheets_per_key = frame.groupby(list(DUPLICATE_KEY), dropna=False)["source_sheet"].transform("nunique")
        in_window = frame["invoice_ts"].between(
            rules.overlap_start, rules.overlap_end_exclusive, inclusive="left"
        )
        report.details["duplicates_spanning_both_sheets"] = int((duplicated & (sheets_per_key > 1)).sum())
        report.details["duplicates_in_sheet_overlap_window"] = int((duplicated & in_window).sum())
    frame = report.drop(frame, duplicated, "exact_duplicate_rows")

    is_product = frame["stock_code"].str.fullmatch(rules.product_stock_code_pattern).fillna(False).astype(bool)
    report.details["non_product_stock_codes_distinct"] = int(frame.loc[~is_product, "stock_code"].nunique())
    frame = report.drop(frame, ~is_product, "non_product_stock_code_rows")

    frame = report.drop(frame, frame["unit_price"] <= 0, "non_positive_price_rows")

    is_cancellation = _is_cancellation(frame, rules)
    frame = report.drop(
        frame,
        is_cancellation & (frame["quantity"] >= 0),
        "cancellation_with_non_negative_quantity_rows",
    )
    is_cancellation = _is_cancellation(frame, rules)
    frame = report.drop(
        frame,
        ~is_cancellation & (frame["quantity"] <= 0),
        "sale_with_non_positive_quantity_rows",
    )

    lines = _shape_lines(frame, rules, report)
    report.rows_out = len(lines)
    _check_reconciliation(report)
    return lines, report


def build_customers(lines: pd.DataFrame, report: CleaningReport) -> pd.DataFrame:
    """One row per customer id present in the clean lines.

    Country is the most frequent mapped ISO code of the customer's lines (ties:
    alphabetical). Customers whose lines map to several countries are counted.
    """
    summary = lines.groupby("customer_id").agg(
        first_line_ts=("invoice_ts", "min"),
        last_line_ts=("invoice_ts", "max"),
        line_count=("invoice_no", "size"),
    )
    purchases = lines.loc[~lines["is_cancellation"]].groupby("customer_id")["invoice_no"].nunique()
    summary["invoice_count"] = purchases.reindex(summary.index).fillna(0).astype("int64")

    known = lines.loc[lines["country_iso2"].notna(), ["customer_id", "country_iso2"]]
    countries_per_customer = known.groupby("customer_id")["country_iso2"].nunique()
    report.details["customers_with_several_countries"] = int((countries_per_customer > 1).sum())
    main_country = (
        known.groupby(["customer_id", "country_iso2"])
        .size()
        .rename("lines")
        .reset_index()
        .sort_values(["customer_id", "lines", "country_iso2"], ascending=[True, False, True])
        .drop_duplicates("customer_id")
        .set_index("customer_id")["country_iso2"]
    )
    summary["country_iso2"] = main_country.reindex(summary.index).astype("string")

    customers = summary.reset_index()
    return customers[
        ["customer_id", "country_iso2", "first_line_ts", "last_line_ts", "line_count", "invoice_count"]
    ].sort_values("customer_id", kind="mergesort").reset_index(drop=True)


def _is_cancellation(frame: pd.DataFrame, rules: CleaningRules) -> pd.Series:
    return frame["invoice_no"].str.startswith(rules.cancellation_prefix).fillna(False).astype(bool)


def _shape_lines(frame: pd.DataFrame, rules: CleaningRules, report: CleaningReport) -> pd.DataFrame:
    country_iso2 = frame["country_raw"].map(rules.country_iso2)
    report.details["rows_without_country_iso2"] = int(country_iso2.isna().sum())
    report.details["country_values_without_iso2"] = sorted(
        frame.loc[country_iso2.isna(), "country_raw"].unique().tolist()
    )

    lines = pd.DataFrame(
        {
            "invoice_no": frame["invoice_no"].astype("string"),
            "invoice_ts": frame["invoice_ts"].dt.tz_localize("UTC").astype("datetime64[us, UTC]"),
            "customer_id": frame["customer_id"].astype("int64"),
            "stock_code": frame["stock_code"].astype("string"),
            "quantity": frame["quantity"].astype("int64"),
            "unit_price": frame["unit_price"].astype("float64"),
            "line_amount": (frame["quantity"] * frame["unit_price"]).round(2).astype("float64"),
            "is_cancellation": _is_cancellation(frame, rules),
            "country_iso2": country_iso2.astype("string"),
            "source_sheet": frame["source_sheet"].astype("string"),
        },
        index=frame.index,
    )
    return lines.sort_values(
        ["invoice_ts", "invoice_no", "stock_code", "customer_id", "quantity", "unit_price"],
        kind="mergesort",
    ).reset_index(drop=True)


def _check_reconciliation(report: CleaningReport) -> None:
    removed_total = sum(report.removed.values())
    if report.rows_in - removed_total != report.rows_out:
        raise ValueError(
            "Cleaning counts do not reconcile: "
            f"{report.rows_in} in, {removed_total} removed, {report.rows_out} out"
        )
