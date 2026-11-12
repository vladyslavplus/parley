"""Stub ``crm_uplift`` artifact: a pipeline placeholder that runs the service before the trained fixture exists.

The stub is fitted on synthetic rows drawn from a fixed seed. It makes no claim about any effect, carries provenance
``stub`` in its manifest, and every score response repeats that provenance. It is generated, never committed.
"""

from __future__ import annotations

import hashlib
import json
from datetime import date
from pathlib import Path
from typing import Final

import numpy as np
import xgboost as xgb

from parley_ml.datasets.checksums import sha256_file, write_json
from parley_ml.serving.artifacts import MANIFEST_FILE, MANIFEST_SCHEMA_VERSION, UPLIFT_FILES
from parley_ml.serving.scoring import UPLIFT_FEATURE_ORDER, feature_matrix
from parley_ml.serving.schemas import UpliftFeatures

MODEL_NAME: Final = "crm_uplift"
KIND: Final = "uplift"
PROVENANCE: Final = "stub"
STUB_VERSION_DATE: Final = date(2026, 11, 10)
HASH_CHARS: Final = 7
STUB_SEED: Final = 20261110
STUB_ROWS: Final = 4000
STUB_ROUNDS: Final = 20
STUB_MAX_DEPTH: Final = 3
STUB_ETA: Final = 0.3
STUB_RULES: Final[dict[str, float]] = {
    "do_not_disturb_uplift_below": -0.02,
    "persuadable_uplift_at_least": 0.05,
    "sure_thing_control_rate_at_least": 0.25,
}
STUB_NOTE: Final = "Pipeline stub on synthetic rows. Not an effect estimate and not a trained model."


def stub_version() -> str:
    config = {
        "seed": STUB_SEED,
        "rows": STUB_ROWS,
        "rounds": STUB_ROUNDS,
        "max_depth": STUB_MAX_DEPTH,
        "eta": STUB_ETA,
        "segment_rules": STUB_RULES,
    }
    config_text = json.dumps(config, sort_keys=True, separators=(",", ":"))
    config_hash = hashlib.sha256(config_text.encode("utf-8")).hexdigest()[:HASH_CHARS]
    return f"{MODEL_NAME}-{STUB_VERSION_DATE:%Y%m%d}-{config_hash}"


def _synthetic_rows(rows: int, seed: int) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    rng = np.random.default_rng(seed)
    recency = rng.integers(0, 400, rows)
    tenure = recency + rng.integers(0, 1200, rows)
    orders_90d = rng.poisson(0.6, rows)
    orders_365d = orders_90d + rng.poisson(1.5, rows)
    order_count = orders_365d + rng.poisson(2.0, rows)
    value_365d = np.round(orders_365d * rng.gamma(2.0, 60.0, rows), 2)
    avg_order = np.where(order_count > 0, np.round(value_365d / np.maximum(orders_365d, 1), 2), 0.0)
    open_deal = rng.integers(0, 2, rows)
    features = [
        UpliftFeatures(
            recency_days=int(r),
            tenure_days=int(t),
            orders_90d=int(o90),
            orders_365d=int(o365),
            order_count=int(oc),
            value_365d=float(v),
            avg_order_value=float(a),
            has_open_deal=int(d),
        )
        for r, t, o90, o365, oc, v, a, d in zip(
            recency, tenure, orders_90d, orders_365d, order_count, value_365d, avg_order, open_deal, strict=True
        )
    ]
    matrix = feature_matrix(features)
    treatment = rng.integers(0, 2, rows)
    logit = -1.2 + 0.4 * (np.asarray(recency) < 120) - 0.1 * np.asarray(orders_365d)
    outcome = (rng.random(rows) < 1.0 / (1.0 + np.exp(-logit))).astype(np.int8)
    return matrix, treatment, outcome


def _fit(matrix: np.ndarray, labels: np.ndarray, seed: int) -> xgb.Booster:
    params = {
        "objective": "binary:logistic",
        "eval_metric": "logloss",
        "tree_method": "hist",
        "device": "cpu",
        "max_depth": STUB_MAX_DEPTH,
        "eta": STUB_ETA,
        "seed": seed,
    }
    data = xgb.DMatrix(matrix, label=labels, feature_names=list(UPLIFT_FEATURE_ORDER))
    return xgb.train(params, data, num_boost_round=STUB_ROUNDS)


def write_stub_uplift(root: Path) -> Path:
    """Write ``<root>/<version>/`` with two boosters and a schema 2 manifest. Returns the directory."""
    seed = STUB_SEED
    matrix, treatment, outcome = _synthetic_rows(STUB_ROWS, seed)
    treated_model = _fit(matrix[treatment == 1], outcome[treatment == 1], seed)
    control_model = _fit(matrix[treatment == 0], outcome[treatment == 0], seed + 1)

    version = stub_version()
    directory = root / version
    directory.mkdir(parents=True, exist_ok=True)
    treated_path, control_path = directory / UPLIFT_FILES[0], directory / UPLIFT_FILES[1]
    treated_model.save_model(treated_path)
    control_model.save_model(control_path)

    manifest = {
        "schema_version": MANIFEST_SCHEMA_VERSION,
        "model_name": MODEL_NAME,
        "version": version,
        "kind": KIND,
        "feature_schema_version": 1,
        "provenance": PROVENANCE,
        "trained_at": STUB_VERSION_DATE.isoformat(),
        "note": STUB_NOTE,
        "feature_order": list(UPLIFT_FEATURE_ORDER),
        "segment_rules": STUB_RULES,
        "metrics": {"status": "stub, not evaluated"},
        "files": {
            UPLIFT_FILES[0]: sha256_file(treated_path),
            UPLIFT_FILES[1]: sha256_file(control_path),
        },
    }
    write_json(directory / MANIFEST_FILE, manifest)
    return directory


def _uplift_versions(root: Path, stub: bool) -> list[str]:
    """Versions of ``crm_uplift`` artifacts under ``root``, either the stubs or the trained ones."""
    if not root.is_dir():
        return []
    found: list[str] = []
    for manifest_path in sorted(root.glob(f"*/{MANIFEST_FILE}")):
        try:
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            continue
        if not isinstance(manifest, dict) or manifest.get("model_name") != MODEL_NAME:
            continue
        if (manifest.get("provenance") == PROVENANCE) == stub:
            found.append(str(manifest.get("version", manifest_path.parent.name)))
    return found


def trained_uplift_versions(root: Path) -> list[str]:
    """Versions of non-stub ``crm_uplift`` artifacts under ``root``. Unreadable manifests are not counted."""
    return _uplift_versions(root, stub=False)


def stub_uplift_versions(root: Path) -> list[str]:
    """Versions of stub ``crm_uplift`` artifacts under ``root``. Unreadable manifests are not counted."""
    return _uplift_versions(root, stub=True)


def refuse_stub_in_artifact_root(root: Path) -> None:
    """Training refuses to add a trained model beside a stub, so the stub is removed by a person, not silently mixed in."""
    stubs = stub_uplift_versions(root)
    if stubs:
        raise ValueError(
            f"refusing to train {MODEL_NAME}: stub artifact(s) {stubs} already exist in {root}; "
            "remove the stub before training so a placeholder can never sit beside a trained model"
        )


def write_stub_into(root: Path) -> Path:
    """Write the stub only when no trained ``crm_uplift`` artifact sits beside it, so the two can never tie."""
    trained = trained_uplift_versions(root)
    if trained:
        raise ValueError(
            f"refusing to write the crm_uplift stub: trained artifact(s) {trained} already exist in {root}; "
            "the stub is only for an empty artifact folder"
        )
    return write_stub_uplift(root)


CLV_FIXTURE_MODEL: Final = "clv_bgnbd_gg"
CLV_FIXTURE_PARAMETERS: Final[dict[str, dict[str, float]]] = {
    "bg_nbd": {"alpha": 60.649492305926536, "r": 0.6458305076241568, "a": 0.14275234497573008, "b": 2.8270225477991073},
    "gamma_gamma": {"p": 4.744233578985129, "q": 2.2682654271770772, "v": 117.47889039378104},
}


def write_clv_fixture(root: Path) -> Path:
    """Write a labeled CLV fixture for contract samples and tests. Not a served model: it is never written to artifacts.

    The parameters are copied from a calibration fit so that sample values look realistic. Provenance is ``stub``.
    """
    config_text = json.dumps(CLV_FIXTURE_PARAMETERS, sort_keys=True, separators=(",", ":"))
    config_hash = hashlib.sha256(config_text.encode("utf-8")).hexdigest()[:HASH_CHARS]
    version = f"{CLV_FIXTURE_MODEL}-{STUB_VERSION_DATE:%Y%m%d}-{config_hash}"
    directory = root / version
    directory.mkdir(parents=True, exist_ok=True)
    parameters_path = directory / "parameters.json"
    parameters_path.write_text(json.dumps(CLV_FIXTURE_PARAMETERS, sort_keys=True, indent=2) + "\n", encoding="utf-8", newline="\n")
    manifest = {
        "schema_version": MANIFEST_SCHEMA_VERSION,
        "model_name": CLV_FIXTURE_MODEL,
        "version": version,
        "kind": "clv",
        "feature_schema_version": 1,
        "provenance": PROVENANCE,
        "trained_at": STUB_VERSION_DATE.isoformat(),
        "note": "Fixture for contract samples and tests. Not an evaluated model.",
        "metrics": {"status": "stub, not evaluated"},
        "files": {"parameters.json": sha256_file(parameters_path)},
    }
    write_json(directory / MANIFEST_FILE, manifest)
    return directory
