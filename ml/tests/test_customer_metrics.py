"""Customer metrics: hand-computed case, fixture reproducibility and the committed parity fixture."""

from __future__ import annotations

import csv
from pathlib import Path

import pandas as pd
import pytest

from parley_ml.features import customer_metrics_fixture as fixture
from parley_ml.features.customer_metrics import ORDER_COLUMNS, METRIC_COLUMNS, MetricWindows, compute_customer_metrics

REPO_ROOT = Path(__file__).resolve().parents[2]
COMMITTED_FIXTURE = REPO_ROOT / "backend" / "tests" / "Parley.IntegrationTests" / "Crm" / "Metrics" / "Fixtures"
WINDOWS = MetricWindows(short_days=90, long_days=365)
AS_OF = pd.Timestamp("2011-12-31T12:00:00Z")


def _orders(rows: list[tuple[str, str, str, float, bool]]) -> pd.DataFrame:
    frame = pd.DataFrame(rows, columns=list(ORDER_COLUMNS))
    frame["ordered_at"] = pd.to_datetime(frame["ordered_at"], utc=True)
    return frame


def test_hand_computed_contact_matches_the_definitions() -> None:
    orders = _orders([
        ("o1", "c1", "2011-01-10T09:00:00Z", 10.0, False),
        ("o2", "c1", "2011-01-10T18:00:00Z", 5.0, False),
        ("o3", "c1", "2011-01-11T08:00:00Z", 20.0, False),
        ("o4", "c1", "2011-01-12T08:00:00Z", -4.0, True),
        ("o5", "c1", "2012-02-01T08:00:00Z", 99.0, False),
    ])

    result = compute_customer_metrics(orders, ["c1", "c2"], ["c2"], AS_OF, WINDOWS).set_index("contact_id")

    first = result.loc["c1"]
    assert first["order_count"] == 3
    assert first["purchase_day_count"] == 2
    assert first["orders_short"] == 0
    assert first["orders_long"] == 3
    assert first["total_value"] == pytest.approx(31.0)
    assert first["value_long"] == pytest.approx(31.0)
    assert first["avg_order_value"] == pytest.approx(10.3333)
    assert first["avg_repeat_order_value"] == pytest.approx(20.0)
    assert str(first["first_order_at"]) == "2011-01-10 09:00:00+00:00"
    assert str(first["last_order_at"]) == "2011-01-11 08:00:00+00:00"
    assert bool(first["has_open_deal"]) is False

    empty = result.loc["c2"]
    assert empty["order_count"] == 0
    assert empty["avg_order_value"] == 0
    assert empty["avg_repeat_order_value"] == 0
    assert bool(empty["has_open_deal"]) is True
    assert pd.isna(empty["first_order_at"])


def test_fixture_generation_is_reproducible(tmp_path: Path) -> None:
    fixture.write_fixture(tmp_path)

    for name in ("fixture.json", "contacts.csv", "orders.csv", "open_deals.csv", "expected_metrics.csv"):
        assert (tmp_path / name).read_bytes() == (COMMITTED_FIXTURE / name).read_bytes(), name


def test_committed_expectations_match_the_calculator() -> None:
    contacts = _read_column(COMMITTED_FIXTURE / "contacts.csv", "contact_id")
    open_deals = _read_column(COMMITTED_FIXTURE / "open_deals.csv", "contact_id")
    orders = pd.read_csv(COMMITTED_FIXTURE / "orders.csv", dtype={"is_cancellation": str})
    orders["is_cancellation"] = orders["is_cancellation"] == "1"
    orders["ordered_at"] = pd.to_datetime(orders["ordered_at"], utc=True)

    computed = compute_customer_metrics(orders, contacts, open_deals, AS_OF, WINDOWS)
    actual = [fixture.format_row(row) for row in computed.to_dict("records")]

    with (COMMITTED_FIXTURE / "expected_metrics.csv").open(encoding="utf-8", newline="") as handle:
        expected = list(csv.DictReader(handle))

    assert list(computed.columns) == list(METRIC_COLUMNS)
    assert len(actual) == len(expected) == fixture.CONTACT_COUNT + fixture.EDGE_CONTACT_COUNT
    assert actual == expected


def test_window_edges_follow_the_definitions() -> None:
    _, orders, _ = fixture.generate_inputs()
    edge = [fixture.contact_id(fixture.CONTACT_COUNT + offset) for offset in (1, 2, 3)]
    edge_orders = orders.loc[orders["contact_id"].isin(edge)]

    result = compute_customer_metrics(edge_orders, edge, [], AS_OF, WINDOWS).set_index("contact_id")
    at_as_of, at_short_edge, at_long_edge = (result.loc[contact] for contact in edge)

    # An order exactly at as-of counts in both windows; the windows start after their edge, so the others drop out.
    assert (at_as_of["orders_short"], at_as_of["orders_long"]) == (1, 1)
    assert (at_short_edge["orders_short"], at_short_edge["orders_long"]) == (0, 1)
    assert (at_long_edge["orders_short"], at_long_edge["orders_long"]) == (0, 0)
    assert at_long_edge["total_value"] == pytest.approx(30.0)
    assert at_long_edge["value_long"] == pytest.approx(0.0)


def _read_column(path: Path, column: str) -> list[str]:
    with path.open(encoding="utf-8", newline="") as handle:
        return [row[column] for row in csv.DictReader(handle)]
