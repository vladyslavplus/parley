"""Daily purchase aggregation and the calibration/holdout split for CLV (pure pandas).

Rules:
- A purchase day is a customer's UTC calendar day with at least one line of a non-cancellation invoice
  (BG/NBD convention: one transaction per customer per day).
- The value of a purchase day is the net amount of all its lines, so cancellations on that day subtract
  (same as the CRM metric ``avg_repeat_order_value``, which uses daily net spend).
- Customer-days with cancellation lines only are not purchases. Their net amount is reported and not modeled.
- Calibration: purchase days up to and including ``calibration_end``.
- Holdout: purchase days after ``calibration_end`` up to and including ``holdout_end``.
- Purchase days after ``holdout_end`` are counted and not used.
- Customers first seen in the holdout have no calibration history. They are flagged, not dropped here.
"""

from __future__ import annotations

from dataclasses import dataclass
from datetime import date
from typing import Final

import pandas as pd

DAY_COLUMN: Final = "day"
NET_COLUMN: Final = "net_amount"
CUSTOMER_COLUMN: Final = "customer_id"
REQUIRED_LINE_COLUMNS: Final[tuple[str, ...]] = (CUSTOMER_COLUMN, "invoice_ts", "line_amount", "is_cancellation")
_RECONCILIATION_TOLERANCE: Final = 0.005


@dataclass(frozen=True)
class DailyReport:
    lines: int
    purchase_lines: int
    customer_days: int
    purchase_days: int
    cancellation_only_days: int
    cancellation_only_net_amount: float
    purchase_day_net_amount: float
    total_line_amount: float


@dataclass(frozen=True)
class SplitReport:
    calibration_end: date
    holdout_end: date
    calibration_days: int
    holdout_days: int
    days_after_holdout_end: int
    calibration_customers: int
    holdout_customers: int
    holdout_only_customers: int


@dataclass(frozen=True)
class SplitResult:
    calibration: pd.DataFrame
    holdout: pd.DataFrame
    report: SplitReport


def daily_purchase_days(transactions: pd.DataFrame) -> tuple[pd.DataFrame, DailyReport]:
    """Return one row per purchase day: ``customer_id``, ``day`` (UTC midnight), ``net_amount``."""
    missing = [column for column in REQUIRED_LINE_COLUMNS if column not in transactions.columns]
    if missing:
        raise ValueError(f"Transactions miss columns: {missing}")

    lines = pd.DataFrame(
        {
            CUSTOMER_COLUMN: transactions[CUSTOMER_COLUMN].astype("int64"),
            DAY_COLUMN: utc_midnight(transactions["invoice_ts"]),
            NET_COLUMN: transactions["line_amount"].astype("float64"),
            "is_purchase_line": ~transactions["is_cancellation"].astype(bool),
        }
    )
    per_day = lines.groupby([CUSTOMER_COLUMN, DAY_COLUMN], sort=True).agg(
        **{NET_COLUMN: (NET_COLUMN, "sum"), "purchase_lines": ("is_purchase_line", "sum")}
    )
    has_purchase = per_day["purchase_lines"] > 0
    purchase_days = per_day.loc[has_purchase, [NET_COLUMN]].reset_index()
    cancellation_only = per_day.loc[~has_purchase, NET_COLUMN]

    report = DailyReport(
        lines=len(lines),
        purchase_lines=int(lines["is_purchase_line"].sum()),
        customer_days=len(per_day),
        purchase_days=len(purchase_days),
        cancellation_only_days=int(len(cancellation_only)),
        cancellation_only_net_amount=round(float(cancellation_only.sum()), 2),
        purchase_day_net_amount=round(float(purchase_days[NET_COLUMN].sum()), 2),
        total_line_amount=round(float(lines[NET_COLUMN].sum()), 2),
    )
    reconciled = report.purchase_day_net_amount + report.cancellation_only_net_amount
    if abs(reconciled - report.total_line_amount) > _RECONCILIATION_TOLERANCE:
        raise ValueError(
            f"Daily amounts do not reconcile: {reconciled} from days, {report.total_line_amount} from lines"
        )
    return purchase_days, report


def split_daily(daily: pd.DataFrame, calibration_end: date, holdout_end: date) -> SplitResult:
    """Split purchase days at ``calibration_end`` and cut off after ``holdout_end``."""
    if not calibration_end < holdout_end:
        raise ValueError("calibration_end must be before holdout_end")
    cal_end = pd.Timestamp(calibration_end)
    hold_end = pd.Timestamp(holdout_end)
    days = daily[DAY_COLUMN]

    calibration = daily.loc[days <= cal_end].reset_index(drop=True)
    holdout = daily.loc[(days > cal_end) & (days <= hold_end)].reset_index(drop=True)
    days_after = int((days > hold_end).sum())

    if not calibration.empty and calibration[DAY_COLUMN].max() > cal_end:
        raise ValueError("Leakage: calibration contains days after calibration_end")
    if not holdout.empty and holdout[DAY_COLUMN].min() <= cal_end:
        raise ValueError("Leakage: holdout contains days on or before calibration_end")

    calibration_customers = set(calibration[CUSTOMER_COLUMN].unique())
    holdout_customers = set(holdout[CUSTOMER_COLUMN].unique())
    report = SplitReport(
        calibration_end=calibration_end,
        holdout_end=holdout_end,
        calibration_days=len(calibration),
        holdout_days=len(holdout),
        days_after_holdout_end=days_after,
        calibration_customers=len(calibration_customers),
        holdout_customers=len(holdout_customers),
        holdout_only_customers=len(holdout_customers - calibration_customers),
    )
    return SplitResult(calibration=calibration, holdout=holdout, report=report)


def holdout_outcomes(split: SplitResult) -> pd.DataFrame:
    """One row per customer seen in either period: holdout purchase days and net holdout amount.

    ``first_seen_in_holdout`` marks customers with no calibration history. Evaluation excludes them later.
    """
    calibration_customers = set(split.calibration[CUSTOMER_COLUMN].unique())
    holdout = split.holdout
    per_customer = holdout.groupby(CUSTOMER_COLUMN, sort=True).agg(
        holdout_frequency=(DAY_COLUMN, "size"), holdout_net_amount=(NET_COLUMN, "sum")
    )
    customers = sorted(calibration_customers | set(per_customer.index))
    outcomes = per_customer.reindex(customers).fillna({"holdout_frequency": 0, "holdout_net_amount": 0.0})
    outcomes = outcomes.reset_index().rename(columns={"index": CUSTOMER_COLUMN})
    outcomes[CUSTOMER_COLUMN] = outcomes[CUSTOMER_COLUMN].astype("int64")
    outcomes["holdout_frequency"] = outcomes["holdout_frequency"].astype("int64")
    outcomes["holdout_net_amount"] = outcomes["holdout_net_amount"].astype("float64").round(2)
    outcomes["in_calibration"] = outcomes[CUSTOMER_COLUMN].isin(calibration_customers)
    outcomes["first_seen_in_holdout"] = ~outcomes["in_calibration"]
    return outcomes[
        [CUSTOMER_COLUMN, "in_calibration", "first_seen_in_holdout", "holdout_frequency", "holdout_net_amount"]
    ]


def utc_midnight(timestamps: pd.Series) -> pd.Series:
    """UTC calendar day as a naive timestamp at midnight. Naive inputs are taken as UTC already."""
    if timestamps.dt.tz is None:
        return timestamps.dt.normalize()
    return timestamps.dt.tz_convert("UTC").dt.tz_localize(None).dt.normalize()
