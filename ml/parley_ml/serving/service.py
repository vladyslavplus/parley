"""FastAPI scoring service: ``/health``, ``/models``, ``/score/uplift`` and ``/score/clv``. Contract: ``ml/contracts``.

Auth runs in middleware, before the body is read, so an unauthorized caller gets 401 whatever the body holds. Logs
carry request ids, endpoint names, counts and model versions only: never feature values, subject ids or the key.
"""

from __future__ import annotations

import hmac
import logging
import os
import uuid
from collections.abc import AsyncIterator, Awaitable, Callable
from contextlib import asynccontextmanager
from typing import Any, Final

import numpy as np
from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse, Response
from pydantic import ValidationError

from parley_ml.serving.artifacts import ClvPayload, ModelInfo, Registry, UpliftPayload, load_registry
from parley_ml.serving.clv import validate_items
from parley_ml.serving.scoring import clv_values, feature_matrix, predict_uplift, segment_of
from parley_ml.serving.schemas import (
    CODE_AUTH_INVALID_KEY,
    CODE_BATCH_TOO_LARGE,
    CODE_DUPLICATE_SUBJECT,
    CODE_INTERNAL,
    CODE_INVALID_FEATURES,
    CODE_INVALID_SUBJECT,
    CODE_OUTSIDE_DOMAIN,
    CODE_REQUEST_INVALID,
    CODE_SCHEMA_MISMATCH,
    CODE_UNKNOWN_VERSION,
    ClvItem,
    ClvScore,
    ClvScoreRequest,
    ClvScoreResponse,
    ErrorBody,
    HealthResponse,
    ModelEntry,
    ModelsResponse,
    Rejection,
    UpliftFeatures,
    UpliftItem,
    UpliftScore,
    UpliftScoreRequest,
    UpliftScoreResponse,
)
from parley_ml.serving.settings import ServiceSettings, load_settings

LOGGER: Final = logging.getLogger("parley_ml.serving")
KEY_HEADER: Final = "X-Parley-Ml-Key"
REQUEST_ID_HEADER: Final = "X-Request-Id"
MAX_REQUEST_ID_CHARS: Final = 64
PROTECTED_PATHS: Final[tuple[str, ...]] = ("/models", "/score/uplift", "/score/clv")


class ServiceError(Exception):
    """A request-level failure with its HTTP status and stable code."""

    def __init__(self, status: int, code: str, detail: str) -> None:
        super().__init__(detail)
        self.status = status
        self.code = code
        self.detail = detail


def _request_id(request: Request) -> str:
    """Keep a sane incoming request id, otherwise make one."""
    state_id: str | None = getattr(request.state, "request_id", None)
    if state_id:
        return state_id
    incoming = request.headers.get(REQUEST_ID_HEADER, "")
    if 0 < len(incoming) <= MAX_REQUEST_ID_CHARS and incoming.isascii() and incoming.replace("-", "").isalnum():
        return incoming
    return str(uuid.uuid4())


def _error(status: int, code: str, detail: str, request_id: str) -> JSONResponse:
    body = ErrorBody(code=code, detail=detail, trace_id=request_id)
    return JSONResponse(status_code=status, content=body.model_dump(), headers={REQUEST_ID_HEADER: request_id})


def _key_matches(provided: str | None, expected: str) -> bool:
    if provided is None:
        return False
    return hmac.compare_digest(provided.encode("utf-8"), expected.encode("utf-8"))


def _is_protected(path: str) -> bool:
    return path in PROTECTED_PATHS


def _registry(request: Request) -> Registry:
    registry: Registry = request.app.state.registry
    return registry


def _settings(request: Request) -> ServiceSettings:
    settings: ServiceSettings = request.app.state.settings
    return settings


def _resolve_model(
    request: Request,
    kind: str,
    model_version: str | None,
    feature_schema_version: int,
    item_count: int,
) -> ModelInfo:
    """Batch limit, then exact or default model, then feature schema. Never substitutes another version."""
    limit = _settings(request).config.max_batch_size
    if item_count > limit:
        raise ServiceError(413, CODE_BATCH_TOO_LARGE, f"at most {limit} items per request")
    registry = _registry(request)
    if model_version is None:
        model = registry.default(kind)
        if model is None:
            raise ServiceError(404, CODE_UNKNOWN_VERSION, f"no available {kind} model")
    else:
        model = registry.find(model_version)
        if model is None or model.kind != kind or not model.available:
            raise ServiceError(404, CODE_UNKNOWN_VERSION, f"model version {model_version!r} is not available")
    if model.feature_schema_version != feature_schema_version:
        raise ServiceError(409, CODE_SCHEMA_MISMATCH, f"model expects feature schema {model.feature_schema_version}")
    return model


def _model_entry(model: ModelInfo) -> ModelEntry:
    return ModelEntry(
        model_name=model.model_name,
        version=model.version,
        kind=model.kind,
        feature_schema_version=model.feature_schema_version,
        provenance=model.provenance,
        trained_at=model.trained_at,
        metrics=model.metrics,
        status="available" if model.available else "unavailable",
        error_code=model.error_code,
    )


def _first_error_loc(error: ValidationError) -> str:
    errors = error.errors()
    if not errors:
        return "item"
    return ".".join(str(part) for part in errors[0]["loc"])


def _parse_uplift_item(raw: Any, seen: set[str]) -> UpliftItem | Rejection:
    if not isinstance(raw, dict):
        return Rejection(subject_id=None, code=CODE_INVALID_FEATURES, detail="item must be an object")
    subject = raw.get("subject_id")
    subject_text = subject if isinstance(subject, str) else None
    try:
        item = UpliftItem.model_validate(raw)
    except ValidationError as error:
        loc = _first_error_loc(error)
        if loc.startswith("subject_id"):
            return Rejection(subject_id=subject_text, code=CODE_INVALID_SUBJECT, detail="subject_id must be a UUID")
        return Rejection(subject_id=subject_text, code=CODE_INVALID_FEATURES, detail=f"invalid field {loc}")
    key = str(item.subject_id)
    if key in seen:
        return Rejection(subject_id=key, code=CODE_DUPLICATE_SUBJECT, detail="subject_id repeats an earlier item")
    seen.add(key)
    return item


def _parse_clv_item(raw: Any, seen: set[str]) -> ClvItem | Rejection:
    if not isinstance(raw, dict):
        return Rejection(subject_id=None, code=CODE_INVALID_FEATURES, detail="item must be an object")
    subject = raw.get("subject_id")
    subject_text = subject if isinstance(subject, str) else None
    try:
        item = ClvItem.model_validate(raw)
    except ValidationError as error:
        loc = _first_error_loc(error)
        if loc.startswith("subject_id"):
            return Rejection(subject_id=subject_text, code=CODE_INVALID_SUBJECT, detail="subject_id must be a UUID")
        return Rejection(subject_id=subject_text, code=CODE_INVALID_FEATURES, detail=f"invalid field {loc}")
    key = str(item.subject_id)
    if key in seen:
        return Rejection(subject_id=key, code=CODE_DUPLICATE_SUBJECT, detail="subject_id repeats an earlier item")
    seen.add(key)
    return item


def _score_uplift(model: ModelInfo, raw_items: list[Any]) -> tuple[list[UpliftScore], list[Rejection]]:
    payload = model.payload
    if not isinstance(payload, UpliftPayload):
        raise ServiceError(500, CODE_INTERNAL, "uplift model has no uplift payload")
    seen: set[str] = set()
    outcomes: list[UpliftItem | Rejection] = [_parse_uplift_item(raw, seen) for raw in raw_items]
    valid = [(index, outcome) for index, outcome in enumerate(outcomes) if isinstance(outcome, UpliftItem)]
    scores_by_index: dict[int, UpliftScore] = {}
    if valid:
        features: list[UpliftFeatures] = [item.features for _, item in valid]
        p_treated, p_control = predict_uplift(payload, feature_matrix(features))
        for position, (index, item) in enumerate(valid):
            uplift = float(p_treated[position] - p_control[position])
            scores_by_index[index] = UpliftScore(
                subject_id=str(item.subject_id),
                uplift=uplift,
                p_treated=float(p_treated[position]),
                p_control=float(p_control[position]),
                segment=segment_of(uplift, float(p_control[position]), payload.rules),  # type: ignore[arg-type]
            )
    scores: list[UpliftScore] = []
    rejected: list[Rejection] = []
    for index, outcome in enumerate(outcomes):
        if isinstance(outcome, Rejection):
            rejected.append(outcome)
        else:
            scores.append(scores_by_index[index])
    return scores, rejected


def _score_clv(model: ModelInfo, raw_items: list[Any], horizon_days: int) -> tuple[list[ClvScore], list[Rejection]]:
    payload = model.payload
    if not isinstance(payload, ClvPayload):
        raise ServiceError(500, CODE_INTERNAL, "clv model has no clv payload")
    seen: set[str] = set()
    outcomes: list[ClvItem | Rejection] = [_parse_clv_item(raw, seen) for raw in raw_items]
    valid = [(index, outcome) for index, outcome in enumerate(outcomes) if isinstance(outcome, ClvItem)]
    results: dict[int, ClvScore | Rejection] = {}
    if valid:
        frequency = np.array([item.features.frequency for _, item in valid], dtype=float)
        recency = np.array([item.features.recency_days_since_first for _, item in valid], dtype=float)
        tenure = np.array([item.features.tenure_days for _, item in valid], dtype=float)
        monetary = np.array([item.features.monetary_value for _, item in valid], dtype=float)
        validity = validate_items(frequency, recency, tenure)
        values = clv_values(payload.transactions, payload.spend, frequency, recency, tenure, monetary, horizon_days)
        for position, (index, item) in enumerate(valid):
            reason = validity.reasons[position]
            subject = str(item.subject_id)
            if reason is not None:
                results[index] = Rejection(subject_id=subject, code=CODE_OUTSIDE_DOMAIN, detail=reason)
                continue
            numbers = [float(values[name][position]) for name in ("clv", "p_alive", "expected_purchases", "expected_avg_value")]
            if not all(np.isfinite(numbers)):
                results[index] = Rejection(subject_id=subject, code=CODE_OUTSIDE_DOMAIN, detail="closed form not finite")
                continue
            results[index] = ClvScore(
                subject_id=subject,
                clv=numbers[0],
                p_alive=numbers[1],
                expected_purchases=numbers[2],
                expected_avg_value=numbers[3],
                horizon_days=horizon_days,
            )
    scores: list[ClvScore] = []
    rejected: list[Rejection] = []
    for index, outcome in enumerate(outcomes):
        if isinstance(outcome, Rejection):
            rejected.append(outcome)
            continue
        result = results[index]
        if isinstance(result, Rejection):
            rejected.append(result)
        else:
            scores.append(result)
    return scores, rejected


@asynccontextmanager
async def _lifespan(app: FastAPI) -> AsyncIterator[None]:
    settings: ServiceSettings = app.state.settings
    app.state.registry = load_registry(settings.artifacts_dir)
    registry: Registry = app.state.registry
    LOGGER.info("models loaded=%d listed=%d", registry.loaded_count(), len(registry.models))
    yield


def create_app(settings: ServiceSettings) -> FastAPI:
    """Build the service. Models load at startup from ``settings.artifacts_dir``."""
    app = FastAPI(
        title="Parley ML scoring service",
        version="1",
        lifespan=_lifespan,
        docs_url=None,
        redoc_url=None,
        openapi_url=None,
    )
    app.state.settings = settings

    @app.middleware("http")
    async def request_context(
        request: Request, call_next: Callable[[Request], Awaitable[Response]]
    ) -> Response:
        request_id = _request_id(request)
        request.state.request_id = request_id
        if _is_protected(request.url.path) and not _key_matches(
            request.headers.get(KEY_HEADER), settings.key
        ):
            LOGGER.info("auth rejected request_id=%s path=%s", request_id, request.url.path)
            response: Response = _error(401, CODE_AUTH_INVALID_KEY, "shared key missing or invalid", request_id)
        else:
            response = await call_next(request)
        response.headers[REQUEST_ID_HEADER] = request_id
        LOGGER.info(
            "request_id=%s method=%s path=%s status=%d",
            request_id,
            request.method,
            request.url.path,
            response.status_code,
        )
        return response

    @app.exception_handler(ServiceError)
    async def _service_error(request: Request, error: ServiceError) -> JSONResponse:
        return _error(error.status, error.code, error.detail, _request_id(request))

    @app.exception_handler(RequestValidationError)
    async def _validation_error(request: Request, error: RequestValidationError) -> JSONResponse:
        locations = sorted({".".join(str(part) for part in item["loc"]) for item in error.errors()})
        detail = "request body is invalid: " + ", ".join(locations)
        return _error(422, CODE_REQUEST_INVALID, detail, _request_id(request))

    @app.exception_handler(Exception)
    async def _unexpected(request: Request, error: Exception) -> JSONResponse:
        LOGGER.error("unexpected error type=%s request_id=%s", type(error).__name__, _request_id(request))
        return _error(500, CODE_INTERNAL, "unexpected error", _request_id(request))

    @app.get("/health", response_model=HealthResponse)
    def health(request: Request) -> HealthResponse:
        return HealthResponse(status="ok", models_loaded=_registry(request).loaded_count())

    @app.get("/models", response_model=ModelsResponse)
    def models(request: Request) -> ModelsResponse:
        return ModelsResponse(models=[_model_entry(m) for m in _registry(request).models])

    @app.post("/score/uplift", response_model=UpliftScoreResponse)
    def score_uplift(body: UpliftScoreRequest, request: Request) -> UpliftScoreResponse:
        model = _resolve_model(request, "uplift", body.model_version, body.feature_schema_version, len(body.items))
        scores, rejected = _score_uplift(model, body.items)
        LOGGER.info(
            "score_uplift request_id=%s model=%s items=%d scored=%d rejected=%d",
            _request_id(request),
            model.version,
            len(body.items),
            len(scores),
            len(rejected),
        )
        return UpliftScoreResponse(
            model_name=model.model_name,
            model_version=model.version,
            feature_schema_version=model.feature_schema_version,
            provenance=model.provenance,
            scores=scores,
            rejected=rejected,
        )

    @app.post("/score/clv", response_model=ClvScoreResponse)
    def score_clv(body: ClvScoreRequest, request: Request) -> ClvScoreResponse:
        model = _resolve_model(request, "clv", body.model_version, body.feature_schema_version, len(body.items))
        scores, rejected = _score_clv(model, body.items, body.horizon_days)
        LOGGER.info(
            "score_clv request_id=%s model=%s items=%d scored=%d rejected=%d",
            _request_id(request),
            model.version,
            len(body.items),
            len(scores),
            len(rejected),
        )
        return ClvScoreResponse(
            model_name=model.model_name,
            model_version=model.version,
            feature_schema_version=model.feature_schema_version,
            provenance=model.provenance,
            scores=scores,
            rejected=rejected,
        )

    return app


def create_app_from_env() -> FastAPI:
    """Factory for uvicorn: reads the key and the artifact directory from the environment."""
    return create_app(load_settings(os.environ))
