"""Demo effect tau(x): a few contacts per segment, the first-match order, the outcome rule and config validation."""

from __future__ import annotations

from pathlib import Path

import pytest

from parley_ml.experiments.demo_effect import (
    ContactFeatures,
    load_effect_config,
    segment_of,
    tau,
    treated_outcome,
)
from parley_ml.uplift.config import ML_ROOT

CONFIG = load_effect_config(ML_ROOT / "configs" / "crm_uplift_demo.yaml")


def _contact(recency: float, orders_365d: float, order_count: float, value_365d: float = 0.0) -> ContactFeatures:
    return ContactFeatures(
        recency_days=recency,
        tenure_days=400.0,
        orders_90d=0.0,
        orders_365d=orders_365d,
        order_count=order_count,
        value_365d=value_365d,
        avg_order_value=value_365d / orders_365d if orders_365d else 0.0,
        has_open_deal=0.0,
    )


@pytest.mark.parametrize(
    ("contact", "segment", "effect"),
    [
        (_contact(recency=10, orders_365d=1, order_count=1, value_365d=40), "do_not_disturb", -0.03),
        (_contact(recency=400, orders_365d=0, order_count=3), "lost_cause", 0.01),
        (_contact(recency=5, orders_365d=8, order_count=9, value_365d=2000), "sure_thing", 0.0),
        (_contact(recency=90, orders_365d=2, order_count=2, value_365d=120), "persuadable", 0.15),
        (_contact(recency=90, orders_365d=1, order_count=2, value_365d=60), "other", 0.0),
        (_contact(recency=200, orders_365d=2, order_count=2, value_365d=120), "other", 0.0),
    ],
)
def test_each_segment_has_its_documented_effect(contact: ContactFeatures, segment: str, effect: float) -> None:
    assert segment_of(contact, CONFIG) == segment
    assert tau(contact, CONFIG) == pytest.approx(effect)


def test_the_first_matching_segment_wins() -> None:
    # Matches do_not_disturb (one order, recent) and sure_thing (six or more orders, recent, high value).
    # The config lists do_not_disturb first, so it wins.
    contact = _contact(recency=5, orders_365d=6, order_count=1, value_365d=2000)
    assert segment_of(contact, CONFIG) == "do_not_disturb"
    assert tau(contact, CONFIG) == pytest.approx(-0.03)


def test_positive_effect_adds_a_conversion_only_to_non_buyers() -> None:
    assert treated_outcome(0, 0.15, draw=0.10) == 1
    assert treated_outcome(0, 0.15, draw=0.50) == 0
    assert treated_outcome(1, 0.15, draw=0.99) == 1


def test_negative_effect_removes_a_conversion_only_from_buyers() -> None:
    assert treated_outcome(1, -0.03, draw=0.01) == 0
    assert treated_outcome(1, -0.03, draw=0.50) == 1
    assert treated_outcome(0, -0.03, draw=0.01) == 0


def test_zero_effect_leaves_the_outcome_unchanged() -> None:
    assert treated_outcome(1, 0.0, draw=0.0) == 1
    assert treated_outcome(0, 0.0, draw=0.0) == 0


def test_config_rejects_an_unknown_feature_field(tmp_path: Path) -> None:
    text = (ML_ROOT / "configs" / "crm_uplift_demo.yaml").read_text(encoding="utf-8")
    broken = text.replace("recency_days: {gt: 365}", "recency_weeks: {gt: 52}", 1)
    assert broken != text
    path = tmp_path / "broken.yaml"
    path.write_text(broken, encoding="utf-8", newline="\n")
    with pytest.raises(ValueError, match="unknown feature field"):
        load_effect_config(path)
