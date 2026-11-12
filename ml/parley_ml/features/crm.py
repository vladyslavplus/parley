"""Feature schema 1: the eight CRM fields the scoring service accepts for ``crm_uplift``.

Each field is a function of the customer metrics (``customer_metrics.py``), which follow the definitions of the CRM
reader ``ICrmCustomerMetricsReader``. Values are computed as of an instant: orders after it are ignored, so the
features cannot see the future. Day counts are whole days, rounded down.

Field definitions (as of ``as_of``):
- ``recency_days``: whole days from the last purchase to ``as_of``.
- ``tenure_days``: whole days from the first purchase to ``as_of``.
- ``orders_90d`` / ``orders_365d``: purchases in the last 90 / 365 days.
- ``order_count``: all purchases up to ``as_of``.
- ``value_365d``: net value of the last 365 days (cancellations subtract).
- ``avg_order_value``: net value up to ``as_of`` divided by ``order_count``.
- ``has_open_deal``: 0 or 1, from the open-deal set passed in.

Contacts without purchases have no recency or tenure. ``eligible_contacts`` keeps only contacts with a purchase
history and non-negative values, which is what the service validates.
"""

from __future__ import annotations

from collections.abc import Iterable
from typing import Final

import numpy as np
import pandas as pd

from parley_ml.features.customer_metrics import MetricWindows, compute_customer_metrics

FEATURE_SCHEMA_VERSION: Final = 1
FEATURE_FIELDS: Final[tuple[str, ...]] = (
    "recency_days",
    "tenure_days",
    "orders_90d",
    "orders_365d",
    "order_count",
    "value_365d",
    "avg_order_value",
    "has_open_deal",
)
FEATURE_WINDOWS: Final = MetricWindows(short_days=90, long_days=365)
SECONDS_PER_DAY: Final = 86_400


def feature_frame(
    orders: pd.DataFrame,
    contact_ids: Iterable[str],
    open_deal_contact_ids: Iterable[str],
    as_of: pd.Timestamp,
) -> pd.DataFrame:
    """One row per contact (ascending id), columns ``FEATURE_FIELDS``, computed as of ``as_of``.

    ``orders`` needs the order columns of ``customer_metrics.ORDER_COLUMNS``; ``ordered_at`` is UTC.
    """
    metrics = compute_customer_metrics(orders, contact_ids, open_deal_contact_ids, as_of, FEATURE_WINDOWS)
    has_history = metrics["order_count"] > 0
    first = pd.to_datetime(metrics["first_order_at"], utc=True)
    last = pd.to_datetime(metrics["last_order_at"], utc=True)
    frame = pd.DataFrame(index=pd.Index(metrics["contact_id"], name="contact_id"))
    frame["recency_days"] = _whole_days(as_of - last, has_history)
    frame["tenure_days"] = _whole_days(as_of - first, has_history)
    frame["orders_90d"] = metrics["orders_short"].to_numpy(dtype=float)
    frame["orders_365d"] = metrics["orders_long"].to_numpy(dtype=float)
    frame["order_count"] = metrics["order_count"].to_numpy(dtype=float)
    frame["value_365d"] = metrics["value_long"].to_numpy(dtype=float)
    frame["avg_order_value"] = metrics["avg_order_value"].to_numpy(dtype=float)
    frame["has_open_deal"] = metrics["has_open_deal"].astype(int).to_numpy()
    return frame[list(FEATURE_FIELDS)]


def eligible_contacts(frame: pd.DataFrame) -> pd.DataFrame:
    """Contacts the schema can describe: at least one purchase, non-negative value and average value."""
    mask = (
        frame["recency_days"].notna()
        & (frame["order_count"] >= 1)
        & (frame["value_365d"] >= 0)
        & (frame["avg_order_value"] >= 0)
    )
    return frame.loc[mask]


def _whole_days(delta: pd.Series, present: pd.Series) -> np.ndarray:
    seconds = delta.dt.total_seconds().to_numpy(dtype=float)
    days = np.floor(seconds / SECONDS_PER_DAY)
    return np.where(present.to_numpy(dtype=bool), days, np.nan)
