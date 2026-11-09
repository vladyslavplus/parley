"""Synthetic fixture for the customer metrics parity test (Python calculator and CRM reader).

Writes, into one directory:
- ``fixture.json``: the as-of instant and the windows.
- ``contacts.csv``, ``orders.csv``, ``open_deals.csv``: the input rows (identifiers and amounts only).
- ``expected_metrics.csv``: the Python calculator output, formatted exactly as the CRM test reads it.

Run from ml/: ``uv run --directory ml python -m parley_ml.features.customer_metrics_fixture --out <dir>``.
The output is deterministic: the same code gives the same bytes.
"""

from __future__ import annotations

import argparse
import csv
import json
import random
from collections.abc import Mapping
from datetime import UTC, datetime, timedelta
from pathlib import Path
from typing import Any

import pandas as pd

from parley_ml.features.customer_metrics import METRIC_COLUMNS, MetricWindows, compute_customer_metrics

FIXTURE_SEED = 20261109
CONTACT_COUNT = 100
# One hand-made contact per window edge, after the random contacts: an order exactly at as-of (in both windows),
# exactly at as-of minus the short window (only in the long window) and exactly at as-of minus the long window (in neither).
EDGE_ORDER_AMOUNTS = (10.00, 20.00, 30.00)
EDGE_CONTACT_COUNT = len(EDGE_ORDER_AMOUNTS)
AS_OF = datetime(2011, 12, 31, 12, 0, 0, tzinfo=UTC)
WINDOWS = MetricWindows(short_days=90, long_days=365)
HISTORY_START = datetime(2010, 1, 1, tzinfo=UTC)
HISTORY_DAYS = 800  # past the as-of instant, so orders after it exist and must be ignored
OPEN_DEAL_EVERY = 9
CANCELLATION_SHARE = 0.1
CONTACT_ID_PREFIX = "0192f5a0-7b1c-7000-8000-"
DECIMAL_FORMATS = {"total_value": "{:.2f}", "value_long": "{:.2f}", "avg_order_value": "{:.4f}", "avg_repeat_order_value": "{:.4f}"}


def contact_id(index: int) -> str:
    return f"{CONTACT_ID_PREFIX}{index:012x}"


def generate_inputs() -> tuple[list[str], pd.DataFrame, list[str]]:
    """Deterministic contacts, orders and open-deal contacts."""
    rng = random.Random(FIXTURE_SEED)
    contacts = [contact_id(index) for index in range(1, CONTACT_COUNT + 1)]
    rows: list[dict[str, Any]] = []
    sequence = 0
    for index, contact in enumerate(contacts, start=1):
        for _ in range(rng.choice([0, 0, 1, 2, 3, 4, 6, 9, 12])):
            sequence += 1
            day = HISTORY_START + timedelta(days=rng.randrange(HISTORY_DAYS))
            ordered_at = day.replace(hour=rng.randrange(24), minute=rng.randrange(60), second=0)
            cancelled = rng.random() < CANCELLATION_SHARE
            amount = round(rng.uniform(2.0, 300.0), 2)
            rows.append({
                "order_id": f"{index:03d}-{sequence:05d}",
                "contact_id": contact,
                "ordered_at": ordered_at,
                "total_amount": -amount if cancelled else amount,
                "is_cancellation": cancelled,
            })
    edge_instants = (AS_OF, AS_OF - timedelta(days=WINDOWS.short_days), AS_OF - timedelta(days=WINDOWS.long_days))
    for offset, (ordered_at, amount) in enumerate(zip(edge_instants, EDGE_ORDER_AMOUNTS, strict=True), start=1):
        index = CONTACT_COUNT + offset
        sequence += 1
        contacts.append(contact_id(index))
        rows.append({
            "order_id": f"{index:03d}-{sequence:05d}",
            "contact_id": contact_id(index),
            "ordered_at": ordered_at,
            "total_amount": amount,
            "is_cancellation": False,
        })
    open_deals = [contact for index, contact in enumerate(contacts, start=1) if index % OPEN_DEAL_EVERY == 0]
    orders = pd.DataFrame(rows, columns=["order_id", "contact_id", "ordered_at", "total_amount", "is_cancellation"])
    orders["ordered_at"] = pd.to_datetime(orders["ordered_at"], utc=True)
    return contacts, orders, open_deals


def compute_expected(contacts: list[str], orders: pd.DataFrame, open_deals: list[str]) -> pd.DataFrame:
    return compute_customer_metrics(orders, contacts, open_deals, pd.Timestamp(AS_OF), WINDOWS)


def format_row(row: Mapping[str, Any]) -> dict[str, str]:
    """Text form of one expected row. The CRM test parses this same form."""
    text: dict[str, str] = {"contact_id": str(row["contact_id"])}
    for column in ("first_order_at", "last_order_at"):
        value = row[column]
        text[column] = "" if value is None or pd.isna(value) else _iso(value)
    for column in ("order_count", "purchase_day_count", "orders_short", "orders_long"):
        text[column] = str(int(row[column]))
    for column, template in DECIMAL_FORMATS.items():
        text[column] = template.format(float(row[column]))
    text["has_open_deal"] = "1" if bool(row["has_open_deal"]) else "0"
    return text


def write_fixture(out: Path) -> None:
    out.mkdir(parents=True, exist_ok=True)
    contacts, orders, open_deals = generate_inputs()
    expected = compute_expected(contacts, orders, open_deals)

    (out / "fixture.json").write_text(
        json.dumps(
            {
                "as_of": _iso(pd.Timestamp(AS_OF)),
                "short_window_days": WINDOWS.short_days,
                "long_window_days": WINDOWS.long_days,
                "seed": FIXTURE_SEED,
            },
            indent=2,
        ) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    _write_csv(out / "contacts.csv", ["contact_id"], [[contact] for contact in contacts])
    _write_csv(out / "open_deals.csv", ["contact_id"], [[contact] for contact in open_deals])
    _write_csv(
        out / "orders.csv",
        ["order_id", "contact_id", "ordered_at", "total_amount", "is_cancellation"],
        [
            [row.order_id, row.contact_id, _iso(row.ordered_at), f"{row.total_amount:.2f}", "1" if row.is_cancellation else "0"]
            for row in orders.itertuples(index=False)
        ],
    )
    formatted = [format_row(row) for row in expected.to_dict("records")]
    _write_csv(
        out / "expected_metrics.csv",
        list(METRIC_COLUMNS),
        [[row[column] for column in METRIC_COLUMNS] for row in formatted],
    )


def _write_csv(path: Path, header: list[str], rows: list[list[str]]) -> None:
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle, lineterminator="\n")
        writer.writerow(header)
        writer.writerows(rows)


def _iso(value: Any) -> str:
    instant = pd.Timestamp(value).tz_convert("UTC")
    return instant.strftime("%Y-%m-%dT%H:%M:%SZ")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Write the customer metrics fixture.")
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args(argv)
    write_fixture(args.out)
    print(f"Wrote fixture to {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
