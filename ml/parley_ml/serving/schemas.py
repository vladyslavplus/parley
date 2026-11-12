"""Request and response models of the scoring contract. These models generate ``ml/contracts``."""

from __future__ import annotations

from typing import Any, Final, Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field

FEATURE_SCHEMA_VERSION: Final = 1
MAX_HORIZON_DAYS: Final = 3_650
SEGMENTS: Final = ("persuadable", "sure_thing", "lost_cause", "do_not_disturb")

# Stable item-level rejection codes (in ``rejected``) and request-level codes (in error bodies).
CODE_INVALID_SUBJECT: Final = "ml.request.invalid_subject_id"
CODE_INVALID_FEATURES: Final = "ml.request.invalid_features"
CODE_DUPLICATE_SUBJECT: Final = "ml.request.duplicate_subject"
CODE_OUTSIDE_DOMAIN: Final = "ml.request.outside_model_domain"
CODE_REQUEST_INVALID: Final = "ml.request.invalid"
CODE_BATCH_TOO_LARGE: Final = "ml.request.batch_too_large"
CODE_AUTH_INVALID_KEY: Final = "ml.auth.invalid_key"
CODE_UNKNOWN_VERSION: Final = "ml.score.unknown_model_version"
CODE_SCHEMA_MISMATCH: Final = "ml.score.feature_schema_mismatch"
CODE_INTERNAL: Final = "ml.internal.unexpected"


class _Strict(BaseModel):
    model_config = ConfigDict(extra="forbid")


class ErrorBody(_Strict):
    code: str = Field(description="Stable error code, for example ml.score.unknown_model_version.")
    detail: str = Field(description="Short English detail for logs. The .NET side shows its own localized text.")
    trace_id: str = Field(description="Request id; the same value is returned in the X-Request-Id header.")


class HealthResponse(_Strict):
    status: Literal["ok"]
    models_loaded: int = Field(ge=0, description="Number of models with status available.")


class ModelEntry(_Strict):
    model_name: str
    version: str
    kind: str = Field(description="uplift or clv; unknown when the manifest is unreadable.")
    feature_schema_version: int = Field(ge=0)
    provenance: str = Field(description="public_dataset, semi_synthetic, stub, or unknown when the manifest is unreadable.")
    trained_at: str
    metrics: dict[str, Any] = Field(description="Evaluation metrics from the manifest; empty when unavailable.")
    status: Literal["available", "unavailable"]
    error_code: str | None = Field(description="Model status code (ml.model.*) when unavailable, else null.")


class ModelsResponse(_Strict):
    models: list[ModelEntry]


class UpliftFeatures(_Strict):
    recency_days: int = Field(ge=0)
    tenure_days: int = Field(ge=0)
    orders_90d: int = Field(ge=0)
    orders_365d: int = Field(ge=0)
    order_count: int = Field(ge=0)
    value_365d: float = Field(ge=0, allow_inf_nan=False)
    avg_order_value: float = Field(ge=0, allow_inf_nan=False)
    has_open_deal: int = Field(ge=0, le=1)


class UpliftItem(_Strict):
    subject_id: UUID
    features: UpliftFeatures


class UpliftScoreRequest(_Strict):
    model_version: str | None = Field(
        default=None, description="Exact version to score with. Omitted: the service default for the kind."
    )
    feature_schema_version: int = Field(ge=0)
    items: list[Any] = Field(description="Validated one by one against UpliftItem; invalid items are rejected.")


class UpliftScore(_Strict):
    subject_id: str
    uplift: float = Field(description="p_treated minus p_control.")
    p_treated: float = Field(ge=0, le=1)
    p_control: float = Field(ge=0, le=1)
    segment: Literal["persuadable", "sure_thing", "lost_cause", "do_not_disturb"]


class Rejection(_Strict):
    subject_id: str | None = Field(description="The subject id as sent, or null when it was not a string.")
    code: str
    detail: str


class UpliftScoreResponse(_Strict):
    model_name: str
    model_version: str
    feature_schema_version: int
    provenance: str
    scores: list[UpliftScore]
    rejected: list[Rejection]


class ClvFeatures(_Strict):
    frequency: int = Field(ge=0, description="Repeat purchase days: purchase_day_count - 1.")
    recency_days_since_first: int = Field(ge=0)
    tenure_days: int = Field(ge=0)
    monetary_value: float = Field(ge=0, allow_inf_nan=False)


class ClvItem(_Strict):
    subject_id: UUID
    features: ClvFeatures


class ClvScoreRequest(_Strict):
    model_version: str | None = Field(default=None)
    horizon_days: int = Field(ge=0, le=MAX_HORIZON_DAYS)
    feature_schema_version: int = Field(ge=0)
    items: list[Any] = Field(description="Validated one by one against ClvItem; invalid items are rejected.")


class ClvScore(_Strict):
    subject_id: str
    clv: float = Field(description="Expected purchases over the horizon times expected spend, undiscounted.")
    p_alive: float = Field(ge=0, le=1)
    expected_purchases: float = Field(ge=0)
    expected_avg_value: float = Field(ge=0)
    horizon_days: int = Field(ge=0, le=MAX_HORIZON_DAYS)


class ClvScoreResponse(_Strict):
    model_name: str
    model_version: str
    feature_schema_version: int
    provenance: str
    scores: list[ClvScore]
    rejected: list[Rejection]
