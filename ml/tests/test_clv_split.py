"""Daily aggregation and the calibration/holdout split: counts, boundary days and no leakage (pure pandas, fast)."""

from __future__ import annotations

from datetime import date

import pandas as pd
import pytest

from parley_ml.clv.split import daily_purchase_days, holdout_outcomes, split_daily

CALIBRATION_END = date(2011, 6, 9)
HOLDOUT_END = date(2011, 12, 9)


def _lines(rows: list[tuple[int, str, float, bool]]) -> pd.DataFrame:
    frame = pd.DataFrame(rows, columns=["customer_id", "invoice_ts", "line_amount", "is_cancellation"])
    frame["customer_id"] = frame["customer_id"].astype("int64")
    frame["invoice_ts"] = pd.to_datetime(frame["invoice_ts"], utc=True)
    return frame


def test_purchase_days_net_cancellations_and_drop_cancellation_only_days() -> None:
    lines = _lines(
        [
            (1, "2011-01-10T09:00:00Z", 10.0, False),
            (1, "2011-01-10T18:00:00Z", 5.0, False),
            (1, "2011-01-10T20:00:00Z", -4.0, True),
            (1, "2011-01-12T08:00:00Z", -7.0, True),
            (2, "2011-01-10T08:00:00Z", 3.0, False),
        ]
    )

    daily, report = daily_purchase_days(lines)

    assert daily.to_dict("records") == [
        {"customer_id": 1, "day": pd.Timestamp("2011-01-10"), "net_amount": 11.0},
        {"customer_id": 2, "day": pd.Timestamp("2011-01-10"), "net_amount": 3.0},
    ]
    assert report.customer_days == 3
    assert report.purchase_days == 2
    assert report.cancellation_only_days == 1
    assert report.cancellation_only_net_amount == -7.0
    assert report.purchase_day_net_amount + report.cancellation_only_net_amount == report.total_line_amount


def test_boundary_day_is_calibration_and_next_day_is_holdout() -> None:
    daily = pd.DataFrame(
        {
            "customer_id": [1, 1, 1],
            "day": pd.to_datetime(["2011-06-08", "2011-06-09", "2011-06-10"]),
            "net_amount": [1.0, 2.0, 4.0],
        }
    )

    split = split_daily(daily, CALIBRATION_END, HOLDOUT_END)

    assert split.calibration["day"].tolist() == [pd.Timestamp("2011-06-08"), pd.Timestamp("2011-06-09")]
    assert split.holdout["day"].tolist() == [pd.Timestamp("2011-06-10")]
    assert split.report.calibration_days == 2
    assert split.report.holdout_days == 1


def test_no_holdout_day_reaches_calibration_and_days_after_holdout_end_are_dropped() -> None:
    daily = pd.DataFrame(
        {
            "customer_id": [1, 2, 3, 3],
            "day": pd.to_datetime(["2011-03-01", "2011-08-01", "2011-06-09", "2011-12-10"]),
            "net_amount": [1.0, 1.0, 1.0, 1.0],
        }
    )

    split = split_daily(daily, CALIBRATION_END, HOLDOUT_END)
    outcomes = holdout_outcomes(split).set_index("customer_id")

    assert split.calibration["day"].max() <= pd.Timestamp(CALIBRATION_END)
    assert split.holdout["day"].min() > pd.Timestamp(CALIBRATION_END)
    assert split.report.days_after_holdout_end == 1
    assert split.report.holdout_only_customers == 1
    assert outcomes.loc[2, "first_seen_in_holdout"]
    assert not outcomes.loc[2, "in_calibration"]
    assert outcomes.loc[1, "holdout_frequency"] == 0
    assert outcomes.loc[3, "holdout_frequency"] == 0
    assert outcomes.loc[3, "in_calibration"]


def test_holdout_outcomes_count_holdout_days_and_sum_net_amount() -> None:
    daily = pd.DataFrame(
        {
            "customer_id": [1, 1, 1],
            "day": pd.to_datetime(["2011-01-01", "2011-06-20", "2011-07-01"]),
            "net_amount": [5.0, 2.5, -1.0],
        }
    )

    outcomes = holdout_outcomes(split_daily(daily, CALIBRATION_END, HOLDOUT_END)).set_index("customer_id")

    assert outcomes.loc[1, "holdout_frequency"] == 2
    assert outcomes.loc[1, "holdout_net_amount"] == pytest.approx(1.5)
    assert not outcomes.loc[1, "first_seen_in_holdout"]


def test_split_rejects_non_increasing_dates() -> None:
    daily = pd.DataFrame({"customer_id": [1], "day": pd.to_datetime(["2011-01-01"]), "net_amount": [1.0]})

    with pytest.raises(ValueError):
        split_daily(daily, HOLDOUT_END, CALIBRATION_END)
