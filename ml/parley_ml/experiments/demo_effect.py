"""Documented effect function tau(x) for the demo campaigns and the semi-synthetic fixture.

Reads ``configs/crm_uplift_demo.yaml``. Pure Python; no training. A segment rule compares the eight feature-schema
fields (feature_schema_version 1) with fixed thresholds, and the first matching segment gives tau.
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any, Final, Mapping

import yaml

from parley_ml.features.crm import FEATURE_FIELDS

OPERATORS: Final[tuple[str, ...]] = ("eq", "lt", "lte", "gt", "gte")


@dataclass(frozen=True)
class ContactFeatures:
    recency_days: float
    tenure_days: float
    orders_90d: float
    orders_365d: float
    order_count: float
    value_365d: float
    avg_order_value: float
    has_open_deal: float


@dataclass(frozen=True)
class Segment:
    name: str
    tau: float
    conditions: tuple[tuple[str, str, float], ...]  # (field, operator, value), all must hold

    def matches(self, features: ContactFeatures) -> bool:
        for field, operator, value in self.conditions:
            actual = getattr(features, field)
            if operator == "eq" and not actual == value:
                return False
            if operator == "lt" and not actual < value:
                return False
            if operator == "lte" and not actual <= value:
                return False
            if operator == "gt" and not actual > value:
                return False
            if operator == "gte" and not actual >= value:
                return False
        return True


@dataclass(frozen=True)
class EffectConfig:
    seed: int
    dataset_end: str
    segments: tuple[Segment, ...]
    default_tau: float


def _conditions(name: str, when: Mapping[str, Any]) -> tuple[tuple[str, str, float], ...]:
    if not when:
        raise ValueError(f"segment {name} has no conditions")
    out = []
    for field, clause in when.items():
        if field not in FEATURE_FIELDS:
            raise ValueError(f"segment {name}: unknown feature field {field!r}")
        if not isinstance(clause, Mapping) or not clause:
            raise ValueError(f"segment {name}: field {field} needs an operator")
        for operator, value in clause.items():
            if operator not in OPERATORS:
                raise ValueError(f"segment {name}: unknown operator {operator!r}")
            out.append((field, operator, float(value)))
    return tuple(out)


def load_effect_config(path: Path) -> EffectConfig:
    raw = yaml.safe_load(path.read_text(encoding="utf-8"))
    effect = raw["effect"]
    segments = []
    names: set[str] = set()
    for item in effect["segments"]:
        name = str(item["name"])
        if name in names:
            raise ValueError(f"duplicate segment {name}")
        names.add(name)
        segments.append(Segment(name=name, tau=float(item["tau"]), conditions=_conditions(name, item["when"])))
    return EffectConfig(
        seed=int(raw["seed"]),
        dataset_end=str(raw["dataset_end"]),
        segments=tuple(segments),
        default_tau=float(effect["default_tau"]),
    )


def segment_of(features: ContactFeatures, config: EffectConfig) -> str:
    """Name of the first matching segment, or ``other`` when none matches."""
    for segment in config.segments:
        if segment.matches(features):
            return segment.name
    return "other"


def tau(features: ContactFeatures, config: EffectConfig) -> float:
    """Effect on conversion probability for one contact."""
    for segment in config.segments:
        if segment.matches(features):
            return segment.tau
    return config.default_tau


def expected_effect(y0_probability: float, effect: float) -> float:
    """Expected change in the outcome probability under :func:`treated_outcome`.

    A positive effect converts non-buyers only, so the expected change is ``effect * (1 - P(y0))``. A negative effect
    removes buyers only, so it is ``effect * P(y0)``. ``P(y0)`` is the baseline purchase probability; on the fixture
    it is the snapshot's observed baseline purchase rate. The raw ``tau`` is therefore not the true effect of a row.
    """
    if not 0.0 <= y0_probability <= 1.0:
        raise ValueError("y0_probability must be in [0, 1]")
    if effect > 0.0:
        return effect * (1.0 - y0_probability)
    if effect < 0.0:
        return effect * y0_probability
    return 0.0


def treated_outcome(y0: int, effect: float, draw: float) -> int:
    """Outcome under treatment from the baseline outcome, the effect and a uniform draw in [0, 1).

    Positive effect: a non-buyer becomes a buyer when draw < effect. Negative effect: a buyer stops buying when
    draw < |effect|. Zero effect leaves the outcome unchanged.
    """
    if effect > 0.0 and y0 == 0 and draw < effect:
        return 1
    if effect < 0.0 and y0 == 1 and draw < -effect:
        return 0
    return y0
