"""Scoring math shared by the service and the stub artifact: T-learner uplift, segment mapping and CLV closed forms."""

from __future__ import annotations

from collections.abc import Sequence
from typing import Final

import numpy as np

from parley_ml.serving.artifacts import UpliftPayload
from parley_ml.serving.clv import (
    BetaGeoParameters,
    GammaGammaParameters,
    expected_purchases,
    expected_spend,
    probability_alive,
)
from parley_ml.serving.schemas import UpliftFeatures

UPLIFT_FEATURE_ORDER: Final[tuple[str, ...]] = tuple(UpliftFeatures.model_fields)
SEGMENT_DO_NOT_DISTURB: Final = "do_not_disturb"
SEGMENT_PERSUADABLE: Final = "persuadable"
SEGMENT_SURE_THING: Final = "sure_thing"
SEGMENT_LOST_CAUSE: Final = "lost_cause"


def feature_matrix(rows: Sequence[UpliftFeatures]) -> np.ndarray:
    """Feature rows in ``UPLIFT_FEATURE_ORDER``, float32 as the boosters expect."""
    values = [[float(getattr(row, name)) for name in UPLIFT_FEATURE_ORDER] for row in rows]
    return np.asarray(values, dtype=np.float32).reshape(len(rows), len(UPLIFT_FEATURE_ORDER))


def predict_uplift(payload: UpliftPayload, matrix: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    """T-learner: response probability under treatment and under control, each from its own booster."""
    if matrix.shape[0] == 0:
        empty = np.zeros(0, dtype=np.float64)
        return empty, empty
    p_treated = np.asarray(payload.treated.inplace_predict(matrix), dtype=np.float64)
    p_control = np.asarray(payload.control.inplace_predict(matrix), dtype=np.float64)
    return p_treated, p_control


def segment_of(uplift: float, p_control: float, rules: dict[str, float]) -> str:
    """First matching rule wins: do not disturb, then persuadable, then sure thing, else lost cause."""
    if uplift < rules["do_not_disturb_uplift_below"]:
        return SEGMENT_DO_NOT_DISTURB
    if uplift >= rules["persuadable_uplift_at_least"]:
        return SEGMENT_PERSUADABLE
    if p_control >= rules["sure_thing_control_rate_at_least"]:
        return SEGMENT_SURE_THING
    return SEGMENT_LOST_CAUSE


def clv_values(
    transactions: BetaGeoParameters,
    spend: GammaGammaParameters,
    frequency: np.ndarray,
    recency: np.ndarray,
    tenure: np.ndarray,
    monetary_value: np.ndarray,
    horizon: float,
) -> dict[str, np.ndarray]:
    """Closed-form CLV outputs for items that already passed domain validation."""
    purchases = expected_purchases(transactions, frequency, recency, tenure, horizon)
    average = expected_spend(spend, frequency, monetary_value)
    return {
        "clv": purchases * average,
        "p_alive": probability_alive(transactions, frequency, recency, tenure),
        "expected_purchases": purchases,
        "expected_avg_value": average,
    }
