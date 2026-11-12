"""Generate the committed contract: JSON Schemas from the Pydantic models, and sample requests with live responses.

``ml/contracts/*.schema.json`` is the source for the .NET contract tests. ``ml/contracts/samples/`` holds the requests
and the responses the live service gave for them, indexed by ``samples/index.json``. The contract test regenerates
both into a scratch directory and fails when the committed files differ.
"""

from __future__ import annotations

import json
import tempfile
from pathlib import Path
from typing import Any, Final

from pydantic import BaseModel

from parley_ml.serving.artifacts import load_registry
from parley_ml.serving.schemas import (
    ClvItem,
    ClvScoreRequest,
    ClvScoreResponse,
    ErrorBody,
    HealthResponse,
    ModelsResponse,
    UpliftItem,
    UpliftScoreRequest,
    UpliftScoreResponse,
)
from parley_ml.serving.settings import ML_ROOT, ServiceSettings, ServingConfig
from parley_ml.serving.stub import write_clv_fixture, write_stub_uplift

CONTRACTS_DIR: Final = ML_ROOT / "contracts"
SAMPLES_DIR: Final = CONTRACTS_DIR / "samples"
INDEX_FILE: Final = "index.json"
SAMPLE_KEY: Final = "contract-sample-key"  # only the generator and the tests use it; it is never deployed
FLOAT_DIGITS: Final = 10
MODEL_CONTRACTS: Final[dict[str, type[BaseModel]]] = {
    "health_response": HealthResponse,
    "models_response": ModelsResponse,
    "uplift_request": UpliftScoreRequest,
    "uplift_response": UpliftScoreResponse,
    "clv_request": ClvScoreRequest,
    "clv_response": ClvScoreResponse,
    "error_response": ErrorBody,
}
ITEM_MODELS: Final[dict[str, type[BaseModel]]] = {"uplift_request": UpliftItem, "clv_request": ClvItem}
SCHEMA_SUFFIX: Final = ".schema.json"
REQUEST_ID_HEADER: Final = "X-Request-Id"
KEY_HEADER: Final = "X-Parley-Ml-Key"
BATCH_LIMIT: Final = 1000
COMPACT_ITEM_THRESHOLD: Final = 50


def render_schema(name: str) -> str:
    """JSON Schema text for one contract. Items of request bodies are inlined under ``$defs``."""
    model = MODEL_CONTRACTS[name]
    schema: dict[str, Any] = model.model_json_schema()
    if name in ITEM_MODELS:
        item = ITEM_MODELS[name]
        item_schema = item.model_json_schema()
        defs = dict(schema.get("$defs", {}))
        defs.update(item_schema.pop("$defs", {}))
        defs[item.__name__] = item_schema
        schema.pop("$defs", None)
        schema["$defs"] = defs
        schema["properties"]["items"] = {
            "type": "array",
            "description": f"Each element is validated as {item.__name__}; invalid elements are rejected, not the batch.",
            "items": {"$ref": f"#/$defs/{item.__name__}"},
        }
    return json.dumps(schema, indent=2, sort_keys=True, ensure_ascii=True) + "\n"


def _round_floats(value: Any) -> Any:
    if isinstance(value, float):
        return round(value, FLOAT_DIGITS)
    if isinstance(value, dict):
        return {key: _round_floats(item) for key, item in value.items()}
    if isinstance(value, list):
        return [_round_floats(item) for item in value]
    return value


def _features(index: int) -> dict[str, Any]:
    return {
        "recency_days": 42 + index,
        "tenure_days": 610,
        "orders_90d": 1,
        "orders_365d": 4,
        "order_count": 9,
        "value_365d": 812.5,
        "avg_order_value": 203.1,
        "has_open_deal": 0,
    }


def _uplift_item(index: int) -> dict[str, Any]:
    return {"subject_id": f"0192f5a1-0000-7000-8000-{index:012d}", "features": _features(index)}


def _clv_item(index: int) -> dict[str, Any]:
    return {
        "subject_id": f"0192f5a1-0000-7000-8000-{index:012d}",
        "features": {
            "frequency": 8,
            "recency_days_since_first": 590,
            "tenure_days": 610,
            "monetary_value": 203.1,
        },
    }


def _request_samples(uplift_version: str, clv_version: str) -> dict[str, tuple[str, dict[str, Any], str]]:
    """Name -> (endpoint, body, headers mode). Headers mode is ``key`` or ``no_key``."""
    valid_uplift = {
        "model_version": uplift_version,
        "feature_schema_version": 1,
        "items": [_uplift_item(1), _uplift_item(2)],
    }
    partial = {
        "model_version": uplift_version,
        "feature_schema_version": 1,
        "items": [
            _uplift_item(1),
            {"subject_id": "not-a-uuid", "features": _features(2)},
            {"subject_id": "0192f5a1-0000-7000-8000-000000000003", "features": {**_features(3), "recency_days": -1}},
            _uplift_item(1),
        ],
    }
    return {
        "uplift_request_valid": ("/score/uplift", valid_uplift, "key"),
        "uplift_request_partial_rejection": ("/score/uplift", partial, "key"),
        "uplift_request_schema_mismatch": (
            "/score/uplift",
            {**valid_uplift, "feature_schema_version": 2},
            "key",
        ),
        "uplift_request_unknown_version": (
            "/score/uplift",
            {**valid_uplift, "model_version": "crm_uplift-20991231-0000000"},
            "key",
        ),
        "uplift_request_batch_too_large": (
            "/score/uplift",
            {
                "model_version": uplift_version,
                "feature_schema_version": 1,
                "items": [_uplift_item(index + 1) for index in range(BATCH_LIMIT + 1)],
            },
            "key",
        ),
        "uplift_request_unauthorized": ("/score/uplift", valid_uplift, "no_key"),
        "clv_request_valid": (
            "/score/clv",
            {
                "model_version": clv_version,
                "horizon_days": 365,
                "feature_schema_version": 1,
                "items": [_clv_item(1), _clv_item(2)],
            },
            "key",
        ),
    }


def build_samples(workdir: Path) -> dict[str, dict[str, Any]]:
    """Write fixture models under ``workdir`` and run every sample request through the live app."""
    from fastapi.testclient import TestClient

    from parley_ml.serving.service import create_app

    models_dir = workdir / "models"
    write_stub_uplift(models_dir)
    write_clv_fixture(models_dir)
    registry = load_registry(models_dir)
    uplift_version = next(m.version for m in registry.models if m.kind == "uplift")
    clv_version = next(m.version for m in registry.models if m.kind == "clv")
    settings = ServiceSettings(key=SAMPLE_KEY, artifacts_dir=models_dir, config=ServingConfig(max_batch_size=BATCH_LIMIT))
    app = create_app(settings)

    outputs: dict[str, dict[str, Any]] = {}
    index: list[dict[str, Any]] = []
    with TestClient(app, raise_server_exceptions=False) as client:
        for name, (path, body, mode) in _request_samples(uplift_version, clv_version).items():
            request_id = f"sample-{name.replace('_', '-')}"  # fixed ids keep trace_id stable across regenerations
            headers = {REQUEST_ID_HEADER: request_id}
            if mode == "key":
                headers[KEY_HEADER] = SAMPLE_KEY
            response = client.post(path, json=body, headers=headers)
            outputs[f"{name}.json"] = body
            response_name = name.replace("_request_", "_response_", 1)
            if response.status_code == 200:
                outputs[f"{response_name}.json"] = _round_floats(response.json())
            else:
                response_name = f"error_{name.split('_request_', 1)[1]}"
                outputs[f"{response_name}.json"] = response.json()
            index.append(
                {
                    "name": name,
                    "method": "POST",
                    "path": path,
                    "request": f"{name}.json",
                    "send_key": mode == "key",
                    "expected_status": response.status_code,
                    "response": f"{response_name}.json",
                    "error_code": None if response.status_code == 200 else response.json()["code"],
                }
            )
        health = client.get("/health")
        models = client.get("/models", headers={KEY_HEADER: SAMPLE_KEY})
        outputs["health_response.json"] = health.json()
        outputs["models_response.json"] = _round_floats(models.json())
    outputs[INDEX_FILE] = {
        "description": (
            "Sample requests and responses of the scoring contract. Each request file is sent with the "
            "header named in send_key, or without it. The response file holds what the service returned for it, "
            "with floats rounded to 10 decimals. Generated by ml/scripts/generate_contracts.py."
        ),
        "samples": index,
    }
    return outputs


def generated_files(workdir: Path) -> dict[str, str]:
    """Every committed contract file as text, keyed by its path relative to ``ml/contracts``."""
    files = {f"{name}{SCHEMA_SUFFIX}": render_schema(name) for name in MODEL_CONTRACTS}
    for name, payload in build_samples(workdir).items():
        large = len(payload.get("items", [])) > COMPACT_ITEM_THRESHOLD
        text = json.dumps(payload, indent=None if large else 2, sort_keys=True, ensure_ascii=True)
        files[f"samples/{name}"] = text + "\n"
    return files


def write_contracts(contracts_dir: Path = CONTRACTS_DIR) -> list[Path]:
    """Write every schema and sample under ``contracts_dir``. Returns the paths written."""
    with tempfile.TemporaryDirectory(prefix="parley-ml-contracts-") as scratch:
        files = generated_files(Path(scratch))
    written: list[Path] = []
    for relative, text in sorted(files.items()):
        target = contracts_dir / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text, encoding="utf-8", newline="\n")
        written.append(target)
    return written
