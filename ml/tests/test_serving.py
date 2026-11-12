"""Scoring service contract: auth on every score and model endpoint, every error code, partial rejection, artifact
checksums, log privacy and the committed contract files. Runs on CPU with a small synthetic stub; no data is needed."""

from __future__ import annotations

import json
import logging
import shutil
import uuid
from collections.abc import Iterator
from pathlib import Path
from typing import Any

import pytest
from fastapi.testclient import TestClient

from parley_ml.serving.artifacts import (
    CODE_CHECKSUM_MISMATCH,
    CODE_FILE_MISSING,
    CODE_MANIFEST_INVALID,
    CODE_PROVENANCE_NOT_SERVED,
    CODE_SCHEMA_UNSUPPORTED,
    MANIFEST_FILE,
    load_registry,
)
from parley_ml.serving.contracts import CONTRACTS_DIR, MODEL_CONTRACTS, SAMPLES_DIR, generated_files, render_schema
from parley_ml.serving.schemas import (
    ClvScoreResponse,
    ErrorBody,
    HealthResponse,
    ModelsResponse,
    UpliftScoreResponse,
)
from parley_ml.serving.service import create_app
from parley_ml.serving.settings import ServiceSettings, ServingConfig, load_settings
from parley_ml.serving.stub import stub_version, write_clv_fixture, write_stub_into, write_stub_uplift

KEY = "test-key-not-a-secret"
KEY_HEADER = {"X-Parley-Ml-Key": KEY}
SUBJECT = "0192f5a1-0000-7000-8000-000000000001"
FEATURES = {
    "recency_days": 42,
    "tenure_days": 610,
    "orders_90d": 1,
    "orders_365d": 4,
    "order_count": 9,
    "value_365d": 812.5,
    "avg_order_value": 203.1,
    "has_open_deal": 0,
}
CLV_FEATURES = {"frequency": 8, "recency_days_since_first": 590, "tenure_days": 610, "monetary_value": 203.1}
SEGMENTS = {"persuadable", "sure_thing", "lost_cause", "do_not_disturb"}


@pytest.fixture(scope="module")
def stub_root(tmp_path_factory: pytest.TempPathFactory) -> Path:
    root = tmp_path_factory.mktemp("models")
    write_stub_uplift(root)
    write_clv_fixture(root)
    return root


def _client(root: Path, max_batch: int = 1000) -> TestClient:
    settings = ServiceSettings(key=KEY, artifacts_dir=root, config=ServingConfig(max_batch_size=max_batch))
    return TestClient(create_app(settings), raise_server_exceptions=False)


@pytest.fixture
def client(stub_root: Path) -> Iterator[TestClient]:
    with _client(stub_root) as test_client:
        yield test_client


def _item(subject: str = SUBJECT, **feature_changes: Any) -> dict[str, Any]:
    return {"subject_id": subject, "features": {**FEATURES, **feature_changes}}


def _uplift_body(items: list[Any], version: str | None = None, schema: int = 1) -> dict[str, Any]:
    return {"model_version": version, "feature_schema_version": schema, "items": items}


def _clv_body(items: list[Any], horizon: int = 365, version: str | None = None) -> dict[str, Any]:
    return {"model_version": version, "horizon_days": horizon, "feature_schema_version": 1, "items": items}


def _clv_item(subject: str = SUBJECT, **feature_changes: Any) -> dict[str, Any]:
    return {"subject_id": subject, "features": {**CLV_FEATURES, **feature_changes}}


def _uplift_version(stub_root: Path) -> str:
    return next(m.version for m in load_registry(stub_root).models if m.kind == "uplift")


def _clv_version(stub_root: Path) -> str:
    return next(m.version for m in load_registry(stub_root).models if m.kind == "clv")


# Auth ------------------------------------------------------------------------------------------------------------


def test_health_is_open_and_reveals_only_counts(client: TestClient) -> None:
    response = client.get("/health")

    assert response.status_code == 200
    assert set(response.json()) == {"status", "models_loaded"}
    assert response.json() == {"status": "ok", "models_loaded": 2}


@pytest.mark.parametrize(
    ("method", "path", "body"),
    [
        ("GET", "/models", None),
        ("POST", "/score/uplift", {"not": "a valid body"}),
        ("POST", "/score/clv", {"not": "a valid body"}),
    ],
)
def test_every_score_and_model_endpoint_needs_the_key_before_the_body_is_read(
    client: TestClient, method: str, path: str, body: Any
) -> None:
    missing = client.request(method, path, json=body)
    wrong = client.request(method, path, json=body, headers={"X-Parley-Ml-Key": "wrong"})

    for response in (missing, wrong):
        assert response.status_code == 401
        assert response.json()["code"] == "ml.auth.invalid_key"
        assert "trace_id" in response.json()


def test_the_key_is_required_to_start_the_service(tmp_path: Path) -> None:
    with pytest.raises(RuntimeError, match="PARLEY_ML_KEY"):
        load_settings({})
    with pytest.raises(RuntimeError, match="PARLEY_ML_KEY"):
        load_settings({"PARLEY_ML_KEY": "   "})
    settings = load_settings({"PARLEY_ML_KEY": KEY, "PARLEY_ML_ARTIFACTS_DIR": str(tmp_path)})
    assert settings.key == KEY
    assert settings.artifacts_dir == tmp_path


def test_models_lists_the_stub_with_provenance(client: TestClient, stub_root: Path) -> None:
    response = client.get("/models", headers=KEY_HEADER)

    assert response.status_code == 200
    models = {entry["kind"]: entry for entry in response.json()["models"]}
    assert models["uplift"]["version"] == _uplift_version(stub_root)
    assert models["uplift"]["provenance"] == "stub"
    assert models["uplift"]["status"] == "available"
    assert models["uplift"]["error_code"] is None
    assert models["uplift"]["feature_schema_version"] == 1
    ModelsResponse.model_validate(response.json())


# Uplift scoring --------------------------------------------------------------------------------------------------


def test_uplift_scores_a_valid_batch_with_provenance(client: TestClient, stub_root: Path) -> None:
    body = _uplift_body([_item(), _item("0192f5a1-0000-7000-8000-000000000002", recency_days=300)])

    response = client.post("/score/uplift", json=body, headers=KEY_HEADER)

    assert response.status_code == 200
    payload = response.json()
    UpliftScoreResponse.model_validate(payload)
    assert payload["model_name"] == "crm_uplift"
    assert payload["model_version"] == _uplift_version(stub_root)
    assert payload["provenance"] == "stub"
    assert payload["rejected"] == []
    assert [score["subject_id"] for score in payload["scores"]] == [SUBJECT, "0192f5a1-0000-7000-8000-000000000002"]
    for score in payload["scores"]:
        assert score["uplift"] == pytest.approx(score["p_treated"] - score["p_control"], abs=1e-12)
        assert score["segment"] in SEGMENTS


def test_omitted_model_version_uses_the_default(client: TestClient, stub_root: Path) -> None:
    response = client.post("/score/uplift", json=_uplift_body([_item()]), headers=KEY_HEADER)

    assert response.status_code == 200
    assert response.json()["model_version"] == _uplift_version(stub_root) == stub_version()


def test_invalid_items_are_rejected_one_by_one_and_the_rest_are_scored(client: TestClient) -> None:
    body = _uplift_body(
        [
            _item(),
            {"subject_id": "not-a-uuid", "features": FEATURES},
            _item("0192f5a1-0000-7000-8000-000000000003", recency_days=-1),
            _item("0192f5a1-0000-7000-8000-000000000004", value_365d=123456.789),
            "not an object",
            _item(),
        ]
    )
    # 1e400 parses to infinity; the service must reject it as a non-finite feature, not score it.
    raw = json.dumps(body).replace("123456.789", "1e400")

    response = client.post(
        "/score/uplift", content=raw, headers={**KEY_HEADER, "Content-Type": "application/json"}
    )

    assert response.status_code == 200
    payload = response.json()
    assert [score["subject_id"] for score in payload["scores"]] == [SUBJECT]
    codes = [entry["code"] for entry in payload["rejected"]]
    assert codes == [
        "ml.request.invalid_subject_id",
        "ml.request.invalid_features",
        "ml.request.invalid_features",
        "ml.request.invalid_features",
        "ml.request.duplicate_subject",
    ]
    assert payload["rejected"][0]["subject_id"] == "not-a-uuid"
    assert payload["rejected"][4]["subject_id"] == SUBJECT


def test_schema_mismatch_is_a_409_and_never_substituted(client: TestClient) -> None:
    response = client.post("/score/uplift", json=_uplift_body([_item()], schema=2), headers=KEY_HEADER)

    assert response.status_code == 409
    assert response.json()["code"] == "ml.score.feature_schema_mismatch"


def test_unknown_version_is_a_404_and_never_substituted(client: TestClient) -> None:
    response = client.post(
        "/score/uplift", json=_uplift_body([_item()], version="crm_uplift-20991231-0000000"), headers=KEY_HEADER
    )

    assert response.status_code == 404
    assert response.json()["code"] == "ml.score.unknown_model_version"


def test_a_clv_version_sent_to_the_uplift_endpoint_is_unknown(client: TestClient, stub_root: Path) -> None:
    response = client.post(
        "/score/uplift", json=_uplift_body([_item()], version=_clv_version(stub_root)), headers=KEY_HEADER
    )

    assert response.status_code == 404
    assert response.json()["code"] == "ml.score.unknown_model_version"


def test_batch_limit_is_enforced_and_the_limit_itself_is_allowed(stub_root: Path) -> None:
    with _client(stub_root, max_batch=2) as small:
        at_limit = small.post("/score/uplift", json=_uplift_body([_item(), _item(str(uuid.uuid4()))]), headers=KEY_HEADER)
        over = small.post(
            "/score/uplift",
            json=_uplift_body([_item(str(uuid.UUID(int=n + 1))) for n in range(3)]),
            headers=KEY_HEADER,
        )

    assert at_limit.status_code == 200
    assert over.status_code == 413
    assert over.json()["code"] == "ml.request.batch_too_large"


def test_a_malformed_body_is_a_422_without_echoing_values(client: TestClient) -> None:
    response = client.post(
        "/score/uplift", json={"feature_schema_version": "secret-value-9931", "items": []}, headers=KEY_HEADER
    )

    assert response.status_code == 422
    assert response.json()["code"] == "ml.request.invalid"
    assert "secret-value-9931" not in response.text


def test_an_empty_batch_is_scored_as_an_empty_list(client: TestClient) -> None:
    response = client.post("/score/uplift", json=_uplift_body([]), headers=KEY_HEADER)

    assert response.status_code == 200
    assert response.json()["scores"] == []


# CLV scoring -----------------------------------------------------------------------------------------------------


def test_clv_scores_with_closed_forms_and_provenance(client: TestClient, stub_root: Path) -> None:
    body = _clv_body([_clv_item(), _clv_item("0192f5a1-0000-7000-8000-000000000002", frequency=0, monetary_value=0.0)])

    response = client.post("/score/clv", json=body, headers=KEY_HEADER)

    assert response.status_code == 200
    payload = response.json()
    ClvScoreResponse.model_validate(payload)
    assert payload["model_version"] == _clv_version(stub_root)
    assert payload["provenance"] == "stub"
    first, second = payload["scores"]
    assert first["clv"] == pytest.approx(first["expected_purchases"] * first["expected_avg_value"], rel=1e-9)
    assert 0.0 <= first["p_alive"] <= 1.0
    assert first["horizon_days"] == 365
    assert second["expected_avg_value"] > 0.0


def test_clv_rejects_items_outside_the_model_domain_with_a_reason(client: TestClient) -> None:
    body = _clv_body([_clv_item(), _clv_item("0192f5a1-0000-7000-8000-000000000002", recency_days_since_first=700)])

    response = client.post("/score/clv", json=body, headers=KEY_HEADER)

    assert response.status_code == 200
    payload = response.json()
    assert len(payload["scores"]) == 1
    assert payload["rejected"] == [
        {
            "subject_id": "0192f5a1-0000-7000-8000-000000000002",
            "code": "ml.request.outside_model_domain",
            "detail": "recency_out_of_range",
        }
    ]


def test_clv_horizon_outside_the_range_is_a_422(client: TestClient) -> None:
    response = client.post("/score/clv", json=_clv_body([_clv_item()], horizon=3651), headers=KEY_HEADER)

    assert response.status_code == 422
    assert response.json()["code"] == "ml.request.invalid"


def test_clv_without_an_available_model_is_a_404(stub_root: Path, tmp_path: Path) -> None:
    write_stub_uplift(tmp_path)
    with _client(tmp_path) as only_uplift:
        response = only_uplift.post("/score/clv", json=_clv_body([_clv_item()]), headers=KEY_HEADER)

    assert response.status_code == 404
    assert response.json()["code"] == "ml.score.unknown_model_version"


# Artifact checks -------------------------------------------------------------------------------------------------


def _copy_stub(stub_root: Path, destination: Path) -> Path:
    shutil.copytree(stub_root, destination)
    return destination


def test_a_bad_checksum_marks_one_model_unavailable_and_the_others_still_score(
    stub_root: Path, tmp_path: Path
) -> None:
    root = _copy_stub(stub_root, tmp_path / "models")
    uplift_dir = next(p for p in root.iterdir() if p.name.startswith("crm_uplift-"))
    (uplift_dir / "treated.ubj").write_bytes(b"tampered")

    with _client(root) as test_client:
        models = test_client.get("/models", headers=KEY_HEADER).json()["models"]
        health = test_client.get("/health").json()
        scored = test_client.post("/score/clv", json=_clv_body([_clv_item()]), headers=KEY_HEADER)
        refused = test_client.post("/score/uplift", json=_uplift_body([_item()]), headers=KEY_HEADER)

    uplift = next(entry for entry in models if entry["kind"] == "uplift")
    assert uplift["status"] == "unavailable"
    assert uplift["error_code"] == CODE_CHECKSUM_MISMATCH
    assert uplift["provenance"] == "stub"
    assert health["models_loaded"] == 1
    assert scored.status_code == 200
    assert refused.status_code == 404
    assert refused.json()["code"] == "ml.score.unknown_model_version"


def test_a_missing_listed_file_is_reported_not_loaded(stub_root: Path, tmp_path: Path) -> None:
    root = _copy_stub(stub_root, tmp_path / "models")
    clv_dir = next(p for p in root.iterdir() if p.name.startswith("clv_bgnbd_gg-"))
    (clv_dir / "parameters.json").unlink()

    entry = next(m for m in load_registry(root).models if m.kind == "clv")

    assert entry.available is False
    assert entry.error_code == CODE_FILE_MISSING


def test_an_old_manifest_schema_is_refused(stub_root: Path, tmp_path: Path) -> None:
    root = _copy_stub(stub_root, tmp_path / "models")
    clv_dir = next(p for p in root.iterdir() if p.name.startswith("clv_bgnbd_gg-"))
    manifest = json.loads((clv_dir / MANIFEST_FILE).read_text(encoding="utf-8"))
    manifest["schema_version"] = 1
    (clv_dir / MANIFEST_FILE).write_text(json.dumps(manifest), encoding="utf-8")

    entry = next(m for m in load_registry(root).models if m.kind == "clv")

    assert entry.error_code == CODE_SCHEMA_UNSUPPORTED


def test_a_research_provenance_is_never_served(stub_root: Path, tmp_path: Path) -> None:
    root = _copy_stub(stub_root, tmp_path / "models")
    uplift_dir = next(p for p in root.iterdir() if p.name.startswith("crm_uplift-"))
    manifest = json.loads((uplift_dir / MANIFEST_FILE).read_text(encoding="utf-8"))
    manifest["provenance"] = "criteo_research"
    (uplift_dir / MANIFEST_FILE).write_text(json.dumps(manifest), encoding="utf-8")

    entry = next(m for m in load_registry(root).models if m.kind == "uplift")

    assert entry.error_code == CODE_PROVENANCE_NOT_SERVED


def test_a_listed_path_outside_the_model_directory_is_refused(stub_root: Path, tmp_path: Path) -> None:
    root = _copy_stub(stub_root, tmp_path / "models")
    clv_dir = next(p for p in root.iterdir() if p.name.startswith("clv_bgnbd_gg-"))
    manifest = json.loads((clv_dir / MANIFEST_FILE).read_text(encoding="utf-8"))
    manifest["files"] = {"../parameters.json": "0" * 64}
    (clv_dir / MANIFEST_FILE).write_text(json.dumps(manifest), encoding="utf-8")

    entry = next(m for m in load_registry(root).models if m.kind == "clv")

    assert entry.error_code == CODE_MANIFEST_INVALID


def test_the_service_never_reads_criteo_paths(tmp_path: Path) -> None:
    with pytest.raises(ValueError, match="Criteo"):
        load_registry(tmp_path / "criteo_research" / "models")


# Privacy and failure behavior ------------------------------------------------------------------------------------


def test_logs_carry_counts_and_ids_only(client: TestClient, caplog: pytest.LogCaptureFixture) -> None:
    caplog.set_level(logging.INFO, logger="parley_ml.serving")
    body = _uplift_body([_item(), {"subject_id": "not-a-uuid", "features": {**FEATURES, "value_365d": 7777.25}}])

    client.post("/score/uplift", json=body, headers=KEY_HEADER)
    client.get("/models", headers={"X-Parley-Ml-Key": "wrong-key-value"})

    text = "\n".join(record.getMessage() for record in caplog.records)
    assert "score_uplift" in text
    for forbidden in (SUBJECT, "not-a-uuid", "812.5", "7777.25", "203.1", KEY, "wrong-key-value"):
        assert forbidden not in text


def test_an_unexpected_error_returns_a_generic_500(stub_root: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    def explode(*_args: Any, **_kwargs: Any) -> None:
        raise RuntimeError("feature leak 812.5 for contact")

    monkeypatch.setattr("parley_ml.serving.service.predict_uplift", explode)
    with _client(stub_root) as test_client:
        response = test_client.post("/score/uplift", json=_uplift_body([_item()]), headers=KEY_HEADER)

    assert response.status_code == 500
    assert response.json()["code"] == "ml.internal.unexpected"
    assert response.json()["detail"] == "unexpected error"
    assert "812.5" not in response.text


def test_request_ids_are_echoed_and_used_as_trace_ids(client: TestClient) -> None:
    response = client.get("/models", headers={**KEY_HEADER, "X-Request-Id": "req-abc-123"})
    failed = client.get("/models", headers={"X-Request-Id": "req-abc-456"})

    assert response.headers["X-Request-Id"] == "req-abc-123"
    assert failed.json()["trace_id"] == "req-abc-456"
    ErrorBody.model_validate(failed.json())


def test_a_hostile_request_id_is_replaced(client: TestClient) -> None:
    response = client.get("/health", headers={"X-Request-Id": "bad id with spaces and ' quotes"})

    assert response.headers["X-Request-Id"] != "bad id with spaces and ' quotes"
    assert len(response.headers["X-Request-Id"]) == 36


# Contract files --------------------------------------------------------------------------------------------------


@pytest.mark.parametrize("name", sorted(MODEL_CONTRACTS))
def test_committed_schema_matches_the_pydantic_model(name: str) -> None:
    committed = (CONTRACTS_DIR / f"{name}.schema.json").read_text(encoding="utf-8")

    assert committed == render_schema(name), f"regenerate with scripts/generate_contracts.py ({name})"


def _assert_close(actual: Any, expected: Any, path: str = "$") -> None:
    if isinstance(expected, float):
        assert isinstance(actual, float), path
        assert actual == pytest.approx(expected, rel=1e-9, abs=1e-9), path
    elif isinstance(expected, dict):
        assert isinstance(actual, dict) and set(actual) == set(expected), path
        for key in expected:
            _assert_close(actual[key], expected[key], f"{path}.{key}")
    elif isinstance(expected, list):
        assert isinstance(actual, list) and len(actual) == len(expected), path
        for index, (left, right) in enumerate(zip(actual, expected, strict=True)):
            _assert_close(left, right, f"{path}[{index}]")
    else:
        assert actual == expected, path


def test_committed_samples_match_a_fresh_generation(tmp_path: Path) -> None:
    fresh = generated_files(tmp_path / "scratch")

    for relative, text in fresh.items():
        committed = CONTRACTS_DIR / relative
        assert committed.exists(), f"missing {relative}; run scripts/generate_contracts.py"
        if relative.endswith(".schema.json"):
            assert committed.read_text(encoding="utf-8") == text, relative
        else:
            _assert_close(json.loads(committed.read_text(encoding="utf-8")), json.loads(text), relative)


def test_committed_samples_validate_against_the_contract_models() -> None:
    index = json.loads((SAMPLES_DIR / "index.json").read_text(encoding="utf-8"))
    models = {
        "uplift_response_valid.json": UpliftScoreResponse,
        "uplift_response_partial_rejection.json": UpliftScoreResponse,
        "clv_response_valid.json": ClvScoreResponse,
        "health_response.json": HealthResponse,
        "models_response.json": ModelsResponse,
        "error_unauthorized.json": ErrorBody,
        "error_batch_too_large.json": ErrorBody,
        "error_schema_mismatch.json": ErrorBody,
        "error_unknown_version.json": ErrorBody,
    }
    for name, model in models.items():
        model.model_validate(json.loads((SAMPLES_DIR / name).read_text(encoding="utf-8")))
    statuses = {sample["name"]: sample["expected_status"] for sample in index["samples"]}
    assert statuses == {
        "uplift_request_valid": 200,
        "uplift_request_partial_rejection": 200,
        "uplift_request_schema_mismatch": 409,
        "uplift_request_unknown_version": 404,
        "uplift_request_batch_too_large": 413,
        "uplift_request_unauthorized": 401,
        "clv_request_valid": 200,
    }


def test_the_contract_samples_still_produce_their_recorded_answers(stub_root: Path) -> None:
    index = json.loads((SAMPLES_DIR / "index.json").read_text(encoding="utf-8"))
    with _client(stub_root) as test_client:
        for sample in index["samples"]:
            body = json.loads((SAMPLES_DIR / sample["request"]).read_text(encoding="utf-8"))
            headers = {"X-Request-Id": f"sample-{sample['name'].replace('_', '-')}"}
            if sample["send_key"]:
                headers["X-Parley-Ml-Key"] = KEY
            response = test_client.post(sample["path"], json=body, headers=headers)
            assert response.status_code == sample["expected_status"], sample["name"]


def test_the_stub_refuses_to_sit_beside_a_trained_uplift_artifact(tmp_path: Path) -> None:
    trained = tmp_path / "crm_uplift-20261110-abcdef0"
    trained.mkdir()
    (trained / MANIFEST_FILE).write_text(
        json.dumps({"model_name": "crm_uplift", "version": "crm_uplift-20261110-abcdef0", "provenance": "semi_synthetic"}),
        encoding="utf-8",
    )

    with pytest.raises(ValueError, match="refusing to write the crm_uplift stub"):
        write_stub_into(tmp_path)

    assert sorted(path.name for path in tmp_path.iterdir()) == ["crm_uplift-20261110-abcdef0"]


def test_the_stub_is_written_into_an_empty_folder_and_rewritten_over_itself(tmp_path: Path) -> None:
    first = write_stub_into(tmp_path)
    second = write_stub_into(tmp_path)

    assert first == second
    assert json.loads((first / MANIFEST_FILE).read_text(encoding="utf-8"))["provenance"] == "stub"
