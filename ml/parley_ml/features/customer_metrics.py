"""Customer purchase metrics as of an instant.

Same definitions as the CRM reader ``ICrmCustomerMetricsReader`` (backend contract
``Parley.Crm.Contracts.Metrics``). The two implementations are compared on a fixture; a change to one
side is a change to both and needs a feature schema version bump.

Definitions (UTC throughout):
- Only orders with ``ordered_at <= as_of`` count.
- ``order_count``: non-cancellation orders. ``first_order_at`` / ``last_order_at`` over those.
- ``purchase_day_count``: distinct days with at least one non-cancellation order.
- ``orders_short`` / ``orders_long``: non-cancellation orders in ``(as_of - window, as_of]``.
- ``total_value``: net sum of all orders (cancellations subtract). ``value_long``: same inside the long window.
- ``avg_order_value``: ``total_value / order_count``, 0 without orders, rounded half-up to 4 places.
- ``avg_repeat_order_value``: mean of the daily net spend over the purchase days after the first purchase
  day (a daily net spend sums all orders of that day), 0 without repeat days, rounded to 4 places.
- ``has_open_deal``: the contact is in the open deal set.
"""

from __future__ import annotations

from collections.abc import Iterable
from dataclasses import dataclass
from datetime import timedelta
from decimal import ROUND_HALF_UP, Decimal

import pandas as pd

ROUNDING_PLACES = 4
ORDER_COLUMNS = ("order_id", "contact_id", "ordered_at", "total_amount", "is_cancellation")

METRIC_COLUMNS = (
    "contact_id",
    "first_order_at",
    "last_order_at",
    "order_count",
    "purchase_day_count",
    "orders_short",
    "orders_long",
    "total_value",
    "value_long",
    "avg_order_value",
    "avg_repeat_order_value",
    "has_open_deal",
)


@dataclass(frozen=True)
class MetricWindows:
    short_days: int
    long_days: int


def compute_customer_metrics(
    orders: pd.DataFrame,
    contact_ids: Iterable[str],
    open_deal_contact_ids: Iterable[str],
    as_of: pd.Timestamp,
    windows: MetricWindows,
) -> pd.DataFrame:
    """Return one row per contact id (ascending), columns ``METRIC_COLUMNS``.

    ``orders`` needs ``ORDER_COLUMNS``. ``ordered_at`` must be timezone-aware UTC.
    """
    _require_columns(orders, ORDER_COLUMNS)
    if as_of.tzinfo is None:
        raise ValueError("as_of must be timezone-aware UTC")

    contacts = sorted(set(contact_ids))
    considered = orders.loc[orders["ordered_at"] <= as_of].copy()
    considered["ordered_at"] = considered["ordered_at"].dt.tz_convert("UTC")
    considered["purchase"] = ~considered["is_cancellation"].astype(bool)
    considered["day"] = considered["ordered_at"].dt.date
    purchases = considered.loc[considered["purchase"]]

    short_start = as_of - timedelta(days=windows.short_days)
    long_start = as_of - timedelta(days=windows.long_days)

    frame = pd.DataFrame(index=pd.Index(contacts, name="contact_id"), columns=list(METRIC_COLUMNS[1:]), dtype=object)
    frame["first_order_at"] = purchases.groupby("contact_id")["ordered_at"].min()
    frame["last_order_at"] = purchases.groupby("contact_id")["ordered_at"].max()
    frame["order_count"] = purchases.groupby("contact_id").size()
    frame["purchase_day_count"] = purchases.groupby("contact_id")["day"].nunique()
    frame["orders_short"] = purchases.loc[purchases["ordered_at"] > short_start].groupby("contact_id").size()
    frame["orders_long"] = purchases.loc[purchases["ordered_at"] > long_start].groupby("contact_id").size()
    frame["total_value"] = considered.groupby("contact_id")["total_amount"].sum()
    frame["value_long"] = considered.loc[considered["ordered_at"] > long_start].groupby("contact_id")["total_amount"].sum()
    frame["avg_repeat_order_value"] = _average_repeat_day(considered)
    open_deals = set(open_deal_contact_ids)
    frame["has_open_deal"] = [contact in open_deals for contact in contacts]

    frame = frame.fillna({"order_count": 0, "purchase_day_count": 0, "orders_short": 0, "orders_long": 0,
                          "total_value": 0, "value_long": 0, "avg_repeat_order_value": 0})
    for column in ("order_count", "purchase_day_count", "orders_short", "orders_long"):
        frame[column] = frame[column].astype(int)
    frame["avg_order_value"] = [
        _round_half_up(total / count) if count > 0 else 0
        for total, count in zip(frame["total_value"], frame["order_count"], strict=True)
    ]
    frame["avg_repeat_order_value"] = [_round_half_up(value) for value in frame["avg_repeat_order_value"]]
    frame["total_value"] = [_round_half_up(value, places=2) for value in frame["total_value"]]
    frame["value_long"] = [_round_half_up(value, places=2) for value in frame["value_long"]]
    frame["avg_order_value"] = [_round_half_up(value) for value in frame["avg_order_value"]]
    return frame.reset_index()[list(METRIC_COLUMNS)]


def _average_repeat_day(considered: pd.DataFrame) -> pd.Series:
    daily = (
        considered.groupby(["contact_id", "day"])
        .agg(day_total=("total_amount", "sum"), has_purchase=("purchase", "any"))
        .reset_index()
    )
    purchase_days = daily.loc[daily["has_purchase"]].sort_values(["contact_id", "day"]).copy()
    purchase_days["rank"] = purchase_days.groupby("contact_id").cumcount() + 1
    repeat = purchase_days.loc[purchase_days["rank"] > 1]
    return repeat.groupby("contact_id")["day_total"].mean()


def _round_half_up(value: float | Decimal | int, places: int = ROUNDING_PLACES) -> float:
    quantum = Decimal(1).scaleb(-places)
    return float(Decimal(str(value)).quantize(quantum, rounding=ROUND_HALF_UP))


def _require_columns(frame: pd.DataFrame, columns: tuple[str, ...]) -> None:
    missing = [column for column in columns if column not in frame.columns]
    if missing:
        raise ValueError(f"Orders miss columns: {missing}")
