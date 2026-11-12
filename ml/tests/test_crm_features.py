"""Feature schema 1 calculator: window and day rules, eligibility, and no leakage past as-of."""

from __future__ import annotations

import pandas as pd

from parley_ml.features.crm import eligible_contacts, feature_frame

AS_OF = pd.Timestamp("2011-06-09T23:59:59Z")


def _orders(rows: list[tuple[str, str, str, float, bool]]) -> pd.DataFrame:
    frame = pd.DataFrame(rows, columns=["order_id", "contact_id", "ordered_at", "total_amount", "is_cancellation"])
    frame["ordered_at"] = pd.to_datetime(frame["ordered_at"], utc=True)
    return frame


def test_whole_day_recency_and_tenure_round_down() -> None:
    orders = _orders([
        ("o1", "c1", "2011-05-01T00:00:00Z", 50.0, False),
        ("o2", "c1", "2011-06-08T12:00:00Z", 30.0, False),
    ])

    row = feature_frame(orders, ["c1"], [], AS_OF).loc["c1"]

    assert row["recency_days"] == 1  # last order 1.49 days before as-of, rounded down
    assert row["tenure_days"] == 39  # 2011-05-01 to 2011-06-09 23:59:59 is 39 days and a fraction
    assert row["order_count"] == 2
    assert row["orders_90d"] == 2
    assert row["orders_365d"] == 2


def test_windows_follow_the_short_and_long_definitions() -> None:
    orders = _orders([
        ("o1", "c1", "2010-06-01T10:00:00Z", 100.0, False),  # outside 365 days
        ("o2", "c1", "2011-01-01T10:00:00Z", 40.0, False),  # inside 365, outside 90
        ("o3", "c1", "2011-05-01T10:00:00Z", 20.0, False),  # inside 90
    ])

    row = feature_frame(orders, ["c1"], [], AS_OF).loc["c1"]

    assert row["orders_90d"] == 1
    assert row["orders_365d"] == 2
    assert row["value_365d"] == 60.0
    assert row["order_count"] == 3


def test_cancellations_are_not_purchases_but_reduce_value() -> None:
    orders = _orders([
        ("o1", "c1", "2011-03-01T10:00:00Z", 100.0, False),
        ("o2", "c1", "2011-03-02T10:00:00Z", -30.0, True),
    ])

    row = feature_frame(orders, ["c1"], [], AS_OF).loc["c1"]

    assert row["order_count"] == 1
    assert row["value_365d"] == 70.0


def test_features_do_not_change_when_later_orders_are_added() -> None:
    history = _orders([
        ("o1", "c1", "2011-02-01T10:00:00Z", 80.0, False),
        ("o2", "c2", "2011-04-01T10:00:00Z", 25.0, False),
    ])
    later = _orders([
        ("o3", "c1", "2011-06-10T00:00:00Z", 999.0, False),  # at T0 itself: not history
        ("o4", "c2", "2011-07-15T09:00:00Z", -500.0, True),
        ("o5", "c1", "2012-01-01T09:00:00Z", 5000.0, False),
    ])
    contacts = ["c1", "c2", "c3"]

    before = feature_frame(history, contacts, ["c2"], AS_OF)
    after = feature_frame(pd.concat([history, later], ignore_index=True), contacts, ["c2"], AS_OF)

    pd.testing.assert_frame_equal(before, after)


def test_eligibility_drops_contacts_without_history_or_with_negative_value() -> None:
    orders = _orders([
        ("o1", "c1", "2011-03-01T10:00:00Z", 100.0, False),
        ("o2", "c2", "2011-03-02T10:00:00Z", 10.0, False),
        ("o3", "c2", "2011-03-03T10:00:00Z", -50.0, True),
        ("o4", "c3", "2011-03-04T10:00:00Z", 5.0, True),  # cancellation only
    ])

    frame = feature_frame(orders, ["c1", "c2", "c3", "c4"], [], AS_OF)
    kept = list(eligible_contacts(frame).index)

    assert kept == ["c1"]


def test_open_deal_flag_is_zero_or_one() -> None:
    orders = _orders([("o1", "c1", "2011-03-01T10:00:00Z", 10.0, False)])

    frame = feature_frame(orders, ["c1", "c2"], ["c2"], AS_OF)

    assert frame.loc["c1", "has_open_deal"] == 0
    assert frame.loc["c2", "has_open_deal"] == 1
