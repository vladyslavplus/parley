"""crm_uplift demo: effect from the config, fixture invariants, a fast training run on a tiny export, served predictions
that match the artifact, the test-access guard, report honesty and byte-identical reruns. The tiny export is generated
in the test's temp folder."""

from __future__ import annotations

import hashlib
import json
from datetime import UTC, datetime, timedelta
from pathlib import Path
from typing import Any

import numpy as np
import pandas as pd
import pytest
import xgboost as xgb
import yaml
from fastapi.testclient import TestClient

from parley_ml.experiments.demo_effect import ContactFeatures, load_effect_config, tau
from parley_ml.experiments.holdout import assign_arm
from parley_ml.features.crm import FEATURE_FIELDS
from parley_ml.serving.artifacts import MANIFEST_FILE, load_registry
from parley_ml.serving.service import create_app
from parley_ml.serving.settings import ML_ROOT, ServiceSettings, ServingConfig
from parley_ml.serving.stub import write_stub_uplift
from parley_ml.uplift.crm_demo_fixture import (
    SPLITS,
    SPLIT_TEST,
    build_fixture,
    load_customer_refs,
    load_demo_orders,
    load_fixture_settings,
    stack_training_rows,
)
from parley_ml.uplift.test_access import AccessLogError, check_access
from parley_ml.uplift.train_crm_demo import (
    DND_DISABLED_THRESHOLD,
    SEGMENT_LABELS,
    SERVED_DEFAULT_NOTE,
    choose_segment_rules,
    macro_f1,
    run,
)

REAL_CONFIG = ML_ROOT / "configs" / "crm_uplift_demo.yaml"
KEY = "test-key-not-a-secret"
PERSUADABLE = ContactFeatures(
    recency_days=60.0,
    tenure_days=400.0,
    orders_90d=0.0,
    orders_365d=2.0,
    order_count=4.0,
    value_365d=300.0,
    avg_order_value=75.0,
    has_open_deal=0.0,
)


def _tiny_export(root: Path, customers: int = 300, seed: int = 7) -> Path:
    export = root / "demo_export"
    export.mkdir(parents=True)
    rng = np.random.default_rng(seed)
    start = datetime(2010, 1, 1, tzinfo=UTC)
    rows = []
    for index in range(customers):
        customer = str(10_000 + index)
        for order in range(int(rng.integers(0, 14))):
            moment = start + timedelta(seconds=int(rng.integers(0, 2 * 365 * 86_400)))
            cancelled = bool(rng.random() < 0.1)
            amount = round(float(rng.uniform(5.0, 300.0)), 2)
            rows.append(
                {
                    "invoice_no": f"{index:04d}-{order:03d}",
                    "customer_id": customer,
                    "ordered_at": moment.strftime("%Y-%m-%dT%H:%M:%SZ"),
                    "total_amount": -amount if cancelled else amount,
                    "currency": "GBP",
                    "line_count": 1,
                    "is_cancellation": int(cancelled),
                }
            )
    pd.DataFrame(rows).to_csv(export / "orders.csv.gz", index=False, compression="gzip")
    pd.DataFrame({"customer_id": [str(10_000 + i) for i in range(customers)], "country_iso2": "GB"}).to_csv(
        export / "contacts.csv.gz", index=False, compression="gzip"
    )
    (export / "manifest.json").write_text(json.dumps({"format_version": 1}), encoding="utf-8")
    return export


def _tiny_config(root: Path, export: Path) -> Path:
    raw: dict[str, Any] = yaml.safe_load(REAL_CONFIG.read_text(encoding="utf-8"))
    raw["artifact"]["root"] = str(root / "models")
    raw["artifact"]["demo_export_dir"] = str(export)
    raw["artifact"]["results_md"] = str(root / "report.md")
    raw["artifact"]["test_access"] = str(root / "test_access.json")
    raw["model"]["max_rounds"] = 40
    raw["model"]["early_stopping_rounds"] = 5
    raw["evaluation"]["bootstrap_replicates"] = 20
    raw["evaluation"]["random_reference_draws"] = 20
    raw["evaluation"]["seed_count"] = 2
    path = root / "config.yaml"
    path.write_text(yaml.safe_dump(raw, sort_keys=False), encoding="utf-8")
    return path


@pytest.fixture(scope="module")
def trained(tmp_path_factory: pytest.TempPathFactory) -> dict[str, Any]:
    root = tmp_path_factory.mktemp("crm_demo")
    export = _tiny_export(root)
    config = _tiny_config(root, export)
    result = run(config, root=root)
    return {"root": root, "config": config, "export": export, "result": result}


def test_the_injected_effect_comes_from_the_config(tmp_path: Path) -> None:
    assert tau(PERSUADABLE, load_effect_config(REAL_CONFIG)) == pytest.approx(0.30)

    raw = yaml.safe_load(REAL_CONFIG.read_text(encoding="utf-8"))
    for segment in raw["effect"]["segments"]:
        if segment["name"] == "persuadable":
            segment["tau"] = 0.5
    altered = tmp_path / "effect.yaml"
    altered.write_text(yaml.safe_dump(raw, sort_keys=False), encoding="utf-8")

    assert tau(PERSUADABLE, load_effect_config(altered)) == pytest.approx(0.5)


def test_fixture_is_customer_level_and_the_arms_follow_the_holdout_rule(trained: dict[str, Any]) -> None:
    settings = load_fixture_settings(trained["config"], trained["root"])
    frame = build_fixture(load_demo_orders(trained["export"]), load_customer_refs(trained["export"]), settings)

    assert frame["contact_id"].is_unique
    assert set(frame["split"]) == set(SPLITS)
    assert set(frame["arm"]) == {0, 1}
    assert all(
        assign_arm(settings.primary.assignment_seed, contact, settings.holdout_share_bps)
        == ("treatment" if arm else "holdout")
        for contact, arm in zip(frame["contact_id"], frame["arm"], strict=True)
    )
    treated = frame.loc[frame["arm"] == 1]
    control = frame.loc[frame["arm"] == 0]
    assert (treated["outcome"] == treated["y1"]).all()
    assert (control["outcome"] == control["y0"]).all()
    assert (treated.loc[treated["tau"] > 0, "y1"] >= treated.loc[treated["tau"] > 0, "y0"]).all()
    assert (treated.loc[treated["tau"] < 0, "y1"] <= treated.loc[treated["tau"] < 0, "y0"]).all()


def test_the_true_effect_is_the_signed_tau_times_the_baseline_rate(trained: dict[str, Any]) -> None:
    settings = load_fixture_settings(trained["config"], trained["root"])
    frame = build_fixture(load_demo_orders(trained["export"]), load_customer_refs(trained["export"]), settings)
    baseline = float(frame["y0"].mean())

    expected = np.where(frame["tau"] > 0, frame["tau"] * (1 - baseline), frame["tau"] * baseline)
    assert np.allclose(frame["effect"].to_numpy(dtype=float), expected)


def test_snapshots_stack_training_rows_only_for_train_customers(trained: dict[str, Any]) -> None:
    settings = load_fixture_settings(trained["config"], trained["root"])
    assert len(settings.snapshots) == 4
    assert settings.snapshots == tuple(sorted(settings.snapshots, key=lambda snapshot: snapshot.t0))
    orders = load_demo_orders(trained["export"])
    refs = load_customer_refs(trained["export"])
    primary = build_fixture(orders, refs, settings)
    panel = stack_training_rows(orders, refs, settings, primary)

    train_customers = set(primary.loc[primary["split"] == "train", "customer_ref"])
    assert set(panel["split"]) == {"train"}
    assert set(panel["customer_ref"]) == train_customers
    assert len(panel) > len(primary.loc[primary["split"] == "train"])
    assert set(primary.loc[primary["split"] == SPLIT_TEST, "customer_ref"]).isdisjoint(panel["customer_ref"])


def test_training_writes_a_schema_two_artifact_labeled_semi_synthetic(trained: dict[str, Any]) -> None:
    result = trained["result"]
    directory = result["artifact"]
    manifest = json.loads((directory / MANIFEST_FILE).read_text(encoding="utf-8"))

    assert directory.name == result["version"]
    assert result["version"].startswith("crm_uplift-20261112-")
    assert manifest["schema_version"] == 2
    assert manifest["provenance"] == "semi_synthetic"
    assert "not an effect estimate" in manifest["label"]
    assert manifest["feature_order"] == list(FEATURE_FIELDS)
    assert set(manifest["segment_rules"]) == {
        "do_not_disturb_uplift_below",
        "persuadable_uplift_at_least",
        "sure_thing_control_rate_at_least",
    }
    assert {"test", "references", "seed_table", "success_criterion"} <= set(manifest["metrics"])
    assert len(manifest["training_data"]["snapshots"]) == 4
    assert manifest["training_data"]["test_snapshot"] == "2011-06-10T00:00:00+00:00"
    assert len(manifest["metrics"]["seed_table"]) == 2
    for name in ("treated.ubj", "control.ubj"):
        booster = xgb.Booster(model_file=str(directory / name))
        assert booster.num_boosted_rounds() == manifest["rounds"][name.split(".")[0]]


def test_the_report_leads_with_true_effect_metrics_and_names_the_references(trained: dict[str, Any]) -> None:
    report = (trained["root"] / "report.md").read_text(encoding="utf-8")

    assert "Mean true effect, top 20% minus all" in report
    assert report.index("Mean true effect") < report.index("Qini coefficient")
    assert "Perfect score" in report and "Random mean" in report and "Random 97.5th pct" in report
    assert "Share of perfect" in report
    assert "cover test-row sampling only" in report
    assert "No accuracy is claimed anywhere" in report
    assert "Seed table" in report
    assert "Success criterion" in report


def test_the_service_serves_the_new_artifact_with_its_provenance(trained: dict[str, Any]) -> None:
    settings = load_fixture_settings(trained["config"], trained["root"])
    frame = build_fixture(load_demo_orders(trained["export"]), load_customer_refs(trained["export"]), settings)
    rows = frame.loc[frame["split"] == "test"].head(3)
    items = [
        {"subject_id": row["contact_id"], "features": {name: float(row[name]) for name in FEATURE_FIELDS}}
        for _, row in rows.iterrows()
    ]
    service_settings = ServiceSettings(
        key=KEY, artifacts_dir=trained["root"] / "models", config=ServingConfig(max_batch_size=50)
    )

    with TestClient(create_app(service_settings), raise_server_exceptions=False) as client:
        response = client.post(
            "/score/uplift",
            json={"feature_schema_version": 1, "items": items},
            headers={"X-Parley-Ml-Key": KEY},
        )

    assert response.status_code == 200, response.text
    body = response.json()
    assert body["provenance"] == "semi_synthetic"
    assert body["model_version"] == trained["result"]["version"]
    assert len(body["scores"]) == 3
    assert {score["segment"] for score in body["scores"]} <= {"persuadable", "sure_thing", "lost_cause", "do_not_disturb"}

    model = load_registry(trained["root"] / "models").find(trained["result"]["version"])
    assert model is not None and model.available
    matrix = np.asarray([[float(item["features"][name]) for name in FEATURE_FIELDS] for item in items], dtype=np.float32)
    expected_treated = model.payload.treated.inplace_predict(matrix)
    expected_control = model.payload.control.inplace_predict(matrix)
    for score, p_treated, p_control in zip(body["scores"], expected_treated, expected_control, strict=True):
        assert score["p_treated"] == pytest.approx(float(p_treated), abs=1e-6)
        assert score["p_control"] == pytest.approx(float(p_control), abs=1e-6)


def test_a_rerun_of_the_same_config_gives_the_same_artifact_bytes(trained: dict[str, Any]) -> None:
    directory = trained["result"]["artifact"]
    names = ("treated.ubj", "control.ubj", MANIFEST_FILE)
    before = {name: hashlib.sha256((directory / name).read_bytes()).hexdigest() for name in names}

    again = run(trained["config"], root=trained["root"], reproduce=True)

    assert again["version"] == trained["result"]["version"]
    after = {name: hashlib.sha256((again["artifact"] / name).read_bytes()).hexdigest() for name in names}
    assert after == before
    assert again["seeds"] == trained["result"]["seeds"]


def test_a_second_opening_of_the_test_split_needs_reproduce(trained: dict[str, Any]) -> None:
    with pytest.raises(AccessLogError, match="--reproduce"):
        check_access(trained["root"] / "test_access.json", _config_sha(trained["config"]), reproduce=False)


def _config_sha(config: Path) -> str:
    return hashlib.sha256(config.read_bytes()).hexdigest()


def test_training_refuses_a_stub_in_the_artifact_root(tmp_path: Path) -> None:
    export = _tiny_export(tmp_path)
    config = _tiny_config(tmp_path, export)
    write_stub_uplift(tmp_path / "models")

    with pytest.raises(ValueError, match="remove the stub before training"):
        run(config, root=tmp_path)


def test_the_committed_report_states_the_served_default() -> None:
    report = (ML_ROOT / "results" / "crm_uplift_demo.md").read_text(encoding="utf-8")

    assert SERVED_DEFAULT_NOTE in report
    assert report.startswith("# CRM uplift demo: pipeline validation on semi-synthetic data, not an effect estimate")


def _selection_grids() -> dict[str, tuple[float, ...]]:
    return {
        "do_not_disturb_uplift_below": (-0.20, -0.15, -0.10, -0.05, -0.02),
        "persuadable_uplift_at_least": (0.05, 0.10),
        "sure_thing_control_rate_at_least": (0.15, 0.25),
    }


def test_do_not_disturb_is_switched_off_when_no_threshold_reaches_the_floor() -> None:
    uplift = np.zeros(100)
    uplift[:20] = -0.5
    labels = np.array(["other"] * 100, dtype=object)

    rules, diagnostics = choose_segment_rules(uplift, np.full(100, 0.2), labels, _selection_grids(), 0.50)

    assert rules["do_not_disturb_uplift_below"] == DND_DISABLED_THRESHOLD
    assert diagnostics["do_not_disturb_enabled"] is False


def test_do_not_disturb_is_enabled_when_the_threshold_meets_the_floor() -> None:
    uplift = np.zeros(100)
    uplift[:20] = -0.5
    labels = np.array(["do_not_disturb"] * 20 + ["other"] * 80, dtype=object)

    rules, diagnostics = choose_segment_rules(uplift, np.full(100, 0.2), labels, _selection_grids(), 0.50)

    assert rules["do_not_disturb_uplift_below"] == pytest.approx(-0.20)
    assert diagnostics["do_not_disturb_precision"] == pytest.approx(1.0)
    assert diagnostics["do_not_disturb_rows"] == 20


def test_macro_f1_counts_other_as_a_class() -> None:
    true = np.array(["persuadable", "other"] + ["lost_cause"] * 98, dtype=object)
    predicted = np.array(["persuadable", "persuadable"] + ["lost_cause"] * 98, dtype=object)

    four = macro_f1(true, predicted, SEGMENT_LABELS)
    five = macro_f1(true, predicted)

    assert five == pytest.approx(four * 4 / 5)
