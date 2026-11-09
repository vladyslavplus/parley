"""Cleaning rules for Online Retail II on a small synthetic frame, using the production config."""

from __future__ import annotations

from pathlib import Path

import pandas as pd
import pytest
import yaml

from parley_ml.datasets.online_retail_cleaning import (
    CANONICAL_COLUMNS,
    TRANSACTIONS_SCHEMA,
    CleaningRules,
    build_customers,
    clean_lines,
)

_CONFIG = Path(__file__).resolve().parents[1] / "configs" / "online_retail.yaml"


@pytest.fixture(scope="module")
def rules() -> CleaningRules:
    config = yaml.safe_load(_CONFIG.read_text(encoding="utf-8"))
    return CleaningRules.from_config(config["cleaning"], config["country_iso2"])


def _line(**overrides: object) -> dict[str, object]:
    row: dict[str, object] = {
        "invoice_no": "536365",
        "stock_code": "85123A",
        "description": "WHITE HANGING HEART T-LIGHT HOLDER",
        "quantity": 6,
        "invoice_ts": pd.Timestamp("2010-12-01 08:26"),
        "unit_price": 2.55,
        "customer_id": 17850,
        "country_raw": "United Kingdom",
        "source_sheet": "Year 2010-2011",
    }
    row.update(overrides)
    return row


def _raw(rows: list[dict[str, object]]) -> pd.DataFrame:
    frame = pd.DataFrame(rows, columns=list(CANONICAL_COLUMNS))
    return frame.assign(
        invoice_no=frame["invoice_no"].astype("string"),
        stock_code=frame["stock_code"].astype("string"),
        description=frame["description"].astype("string"),
        quantity=frame["quantity"].astype("int64"),
        unit_price=frame["unit_price"].astype("float64"),
        customer_id=frame["customer_id"].astype("Int64"),
        country_raw=frame["country_raw"].astype("string"),
        source_sheet=frame["source_sheet"].astype("string"),
    )


def test_each_rule_removes_only_its_rows_and_counts_reconcile(rules: CleaningRules) -> None:
    rows = [
        _line(),
        _line(stock_code="22086"),
        _line(customer_id=None),
        _line(stock_code="POST"),
        _line(unit_price=0.0, stock_code="22087"),
        _line(invoice_no="C536379", quantity=5, stock_code="22088"),
        _line(quantity=0, stock_code="22089"),
        _line(stock_code="47503J ", country_raw="Unspecified"),
    ]
    lines, report = clean_lines(_raw(rows), rules)

    assert report.rows_in == 8
    assert report.removed == {
        "rows_without_customer_id": 1,
        "exact_duplicate_rows": 0,
        "non_product_stock_code_rows": 1,
        "non_positive_price_rows": 1,
        "cancellation_with_non_negative_quantity_rows": 1,
        "sale_with_non_positive_quantity_rows": 1,
    }
    assert report.rows_out == 3 == len(lines)
    assert sorted(lines["stock_code"].tolist()) == ["22086", "47503J", "85123A"]
    assert report.details["country_values_without_iso2"] == ["Unspecified"]
    assert report.details["rows_without_country_iso2"] == 1
    assert list(lines.columns) == [field.name for field in TRANSACTIONS_SCHEMA]


def test_exact_duplicates_across_sheets_are_removed_once_and_reported(rules: CleaningRules) -> None:
    rows = [
        _line(invoice_ts=pd.Timestamp("2010-12-02 09:00"), source_sheet="Year 2009-2010"),
        _line(invoice_ts=pd.Timestamp("2010-12-02 09:00"), source_sheet="Year 2010-2011"),
        _line(invoice_ts=pd.Timestamp("2011-03-01 09:00"), stock_code="22086", source_sheet="Year 2010-2011"),
    ]
    lines, report = clean_lines(_raw(rows), rules)

    assert report.removed["exact_duplicate_rows"] == 1
    assert report.details["duplicates_spanning_both_sheets"] == 1
    assert report.details["duplicates_in_sheet_overlap_window"] == 1
    assert lines["source_sheet"].tolist() == ["Year 2009-2010", "Year 2010-2011"]


def test_cancellations_are_kept_flagged_and_netted_in_amounts(rules: CleaningRules) -> None:
    rows = [
        _line(quantity=4, unit_price=2.5, stock_code="22086"),
        _line(invoice_no="C536400", quantity=-4, unit_price=2.5, stock_code="22086"),
    ]
    lines, report = clean_lines(_raw(rows), rules)

    assert report.rows_out == 2
    cancellation = lines.loc[lines["is_cancellation"]].iloc[0]
    assert cancellation["line_amount"] == -10.0
    assert lines.loc[~lines["is_cancellation"], "line_amount"].iloc[0] == 10.0


def test_customers_count_purchase_invoices_and_main_country(rules: CleaningRules) -> None:
    rows = [
        _line(customer_id=1, invoice_no="100", country_raw="Germany", stock_code="22086"),
        _line(customer_id=1, invoice_no="101", country_raw="Germany", stock_code="22086"),
        _line(customer_id=1, invoice_no="102", country_raw="France", stock_code="22086"),
        _line(customer_id=1, invoice_no="C103", quantity=-1, country_raw="Germany", stock_code="22086"),
        _line(customer_id=2, invoice_no="C104", quantity=-1, country_raw="Spain", stock_code="22086"),
    ]
    lines, report = clean_lines(_raw(rows), rules)
    customers = build_customers(lines, report).set_index("customer_id")

    assert customers.loc[1, "invoice_count"] == 3
    assert customers.loc[1, "country_iso2"] == "DE"
    assert customers.loc[2, "invoice_count"] == 0
    assert report.details["customers_with_several_countries"] == 1


def test_country_codes_are_alpha2_strings_in_the_production_config() -> None:
    config = yaml.safe_load(_CONFIG.read_text(encoding="utf-8"))

    for country, code in config["country_iso2"].items():
        assert code is None or (isinstance(code, str) and len(code) == 2 and code.isupper()), country
