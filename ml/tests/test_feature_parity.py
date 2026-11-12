"""Feature schema 1 parity with the CRM side.

The C# metrics reader is tested against ``expected_metrics.csv`` in the backend fixture folder; this test derives the
eight feature values from the same committed input rows and the same expectations, so Python and C# agree on the
inputs of the scoring service. The demo contact id vectors pin the derivation the backend demo seed uses.
"""

from __future__ import annotations

import json
import math
from pathlib import Path

import pandas as pd
import pytest

from parley_ml.features.crm import FEATURE_FIELDS, eligible_contacts, feature_frame
from parley_ml.uplift.crm_demo_fixture import demo_contact_id

REPO_ROOT = Path(__file__).resolve().parents[2]
FIXTURE_DIR = REPO_ROOT / "backend" / "tests" / "Parley.IntegrationTests" / "Crm" / "Metrics" / "Fixtures"
DAY_SECONDS = 86_400


def _read_inputs() -> tuple[pd.Timestamp, pd.DataFrame, list[str], list[str]]:
    settings = json.loads((FIXTURE_DIR / "fixture.json").read_text(encoding="utf-8"))
    as_of = pd.Timestamp(settings["as_of"])
    orders = pd.read_csv(FIXTURE_DIR / "orders.csv", dtype={"is_cancellation": str})
    orders["is_cancellation"] = orders["is_cancellation"] == "1"
    orders["ordered_at"] = pd.to_datetime(orders["ordered_at"], utc=True)
    contacts = pd.read_csv(FIXTURE_DIR / "contacts.csv")["contact_id"].tolist()
    open_deals = pd.read_csv(FIXTURE_DIR / "open_deals.csv")["contact_id"].tolist()
    return as_of, orders, contacts, open_deals


def test_features_match_the_committed_metrics_expectations() -> None:
    as_of, orders, contacts, open_deals = _read_inputs()
    frame = feature_frame(orders, contacts, open_deals, as_of)
    expected = pd.read_csv(FIXTURE_DIR / "expected_metrics.csv", dtype=str).set_index("contact_id")

    assert list(frame.columns) == list(FEATURE_FIELDS)
    for contact_id, row in frame.iterrows():
        reference = expected.loc[contact_id]
        if isinstance(reference["last_order_at"], str):
            last = pd.Timestamp(reference["last_order_at"])
            first = pd.Timestamp(reference["first_order_at"])
            recency = math.floor((as_of - last).total_seconds() / DAY_SECONDS)
            tenure = math.floor((as_of - first).total_seconds() / DAY_SECONDS)
        else:
            recency = tenure = math.nan
        assert row["recency_days"] == pytest.approx(recency, nan_ok=True), contact_id
        assert row["tenure_days"] == pytest.approx(tenure, nan_ok=True), contact_id
        assert row["orders_90d"] == float(reference["orders_short"]), contact_id
        assert row["orders_365d"] == float(reference["orders_long"]), contact_id
        assert row["order_count"] == float(reference["order_count"]), contact_id
        assert row["value_365d"] == pytest.approx(float(reference["value_long"])), contact_id
        assert row["avg_order_value"] == pytest.approx(float(reference["avg_order_value"])), contact_id
        assert row["has_open_deal"] == float(reference["has_open_deal"] == "1"), contact_id


def test_eligibility_is_the_service_rule() -> None:
    as_of, orders, contacts, open_deals = _read_inputs()
    frame = feature_frame(orders, contacts, open_deals, as_of)
    kept = eligible_contacts(frame)

    assert (kept["order_count"] >= 1).all()
    assert (kept["value_365d"] >= 0).all()
    assert (kept["avg_order_value"] >= 0).all()
    assert kept["recency_days"].notna().all()


def test_demo_contact_ids_follow_the_backend_derivation() -> None:
    # MD5 of "contact:<customer ref>" as a UUID, the same as DemoIdentifiers.Derive("contact", ref) in the Crm module.
    # The backend demo test should assert these same two values; see the report for the request.
    assert demo_contact_id("12346") == "d1172cba-2e21-9f00-a831-10772844328e"
    assert demo_contact_id("13085") == "f007c625-6c97-2d3d-542c-0fcbf02142ae"
