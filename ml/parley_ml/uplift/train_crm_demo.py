"""Train the demo ``crm_uplift`` model on the semi-synthetic fixture and write its served artifact and report.

Pipeline validation on semi-synthetic data, not an effect estimate. Steps, in order:
1. Build the fixture (real covariates and baseline outcomes, injected effect from the config).
2. Fit the T-learner (one XGBoost model per arm) on train, early-stopped on validation.
3. Choose the segment thresholds on validation only: the grid value with the best macro F1 against the known segments.
4. Score the test split once, with the thresholds frozen.
5. Write ``<artifact root>/crm_uplift-<date>-<hash>/`` (schema 2, provenance ``semi_synthetic``) and the report.

Run from ml/:
    uv run --directory ml python -m parley_ml.uplift.train_crm_demo --config configs/crm_uplift_demo.yaml
"""

from __future__ import annotations

import argparse
import hashlib
import json
from dataclasses import dataclass
from datetime import date
from itertools import product
from pathlib import Path
from typing import Any, Final

import numpy as np
import pandas as pd
import yaml
from scipy.stats import pearsonr, spearmanr

from parley_ml.datasets.checksums import sha256_file, write_json
from parley_ml.evaluation.uplift import auuc, bootstrap, qini_coefficient, rank_sample, summarize, uplift_at_k
from parley_ml.features.crm import FEATURE_FIELDS
from parley_ml.serving.artifacts import MANIFEST_FILE, MANIFEST_SCHEMA_VERSION, UPLIFT_FILES
from parley_ml.serving.scoring import segment_of as served_segment
from parley_ml.serving.settings import ML_ROOT
from parley_ml.uplift.crm_demo_fixture import (
    LABEL,
    FixtureSettings,
    build_fixture,
    feature_matrix,
    fixture_summary,
    load_customer_refs,
    load_demo_orders,
    load_fixture_settings,
)
from parley_ml.uplift.data import SplitArrays
from parley_ml.uplift.learners import BoosterSettings, TLearner

MODEL_NAME: Final = "crm_uplift"
PROVENANCE: Final = "semi_synthetic"
SEGMENT_LABELS: Final[tuple[str, ...]] = ("persuadable", "sure_thing", "lost_cause", "do_not_disturb")
TRUE_LABELS: Final[tuple[str, ...]] = (*SEGMENT_LABELS, "other")
RULE_KEYS: Final[tuple[str, ...]] = (
    "do_not_disturb_uplift_below",
    "persuadable_uplift_at_least",
    "sure_thing_control_rate_at_least",
)
HASH_CHARS: Final = 7
CHECK_PREFIX: Final = "[semi-synthetic pipeline check]"
SERVED_DEFAULT_NOTE: Final = (
    "- Served default: the newest non-stub `crm_uplift` artifact in the registry (by trained date, then version). "
    "A stub is refused beside a trained artifact, so the two cannot tie."
)


@dataclass(frozen=True)
class TrainSettings:
    fixture: FixtureSettings
    booster_params: dict[str, Any]
    seed: int
    early_stopping_rounds: int
    max_rounds: int
    grids: dict[str, tuple[float, ...]]
    bootstrap_replicates: int
    bootstrap_seed: int
    top_share: float
    trained_at: date
    artifact_root: Path
    results_md: Path
    config_sha256: str


def load_train_settings(config_path: Path, root: Path = ML_ROOT) -> TrainSettings:
    raw_bytes = config_path.read_bytes()
    raw = yaml.safe_load(raw_bytes.decode("utf-8"))
    model = raw["model"]
    evaluation = raw["evaluation"]
    artifact = raw["artifact"]
    xgboost = dict(model["xgboost"])
    xgboost["seed"] = int(model["seed"])
    return TrainSettings(
        fixture=load_fixture_settings(config_path, root),
        booster_params=xgboost,
        seed=int(model["seed"]),
        early_stopping_rounds=int(model["early_stopping_rounds"]),
        max_rounds=int(model["max_rounds"]),
        grids={name: tuple(float(v) for v in values) for name, values in raw["selection"].items()},
        bootstrap_replicates=int(evaluation["bootstrap_replicates"]),
        bootstrap_seed=int(evaluation["bootstrap_seed"]),
        top_share=float(evaluation["top_share"]),
        trained_at=date.fromisoformat(str(artifact["trained_at"])),
        artifact_root=root / str(artifact["root"]),
        results_md=root / str(artifact["results_md"]),
        config_sha256=hashlib.sha256(raw_bytes).hexdigest(),
    )


def _split_arrays(frame: pd.DataFrame, name: str) -> SplitArrays:
    rows = frame.loc[frame["split"] == name]
    return SplitArrays(
        name=name,
        features=feature_matrix(rows),
        treatment=rows["arm"].to_numpy(dtype=np.int8),
        labels={"conversion": rows["outcome"].to_numpy(dtype=np.int8)},
    )


def macro_f1(true_labels: np.ndarray, predicted: np.ndarray) -> float:
    """Mean F1 over the four segment labels. Rows whose true label is ``other`` count as misses."""
    scores = []
    for label in SEGMENT_LABELS:
        hit = np.sum((predicted == label) & (true_labels == label))
        false_alarm = np.sum((predicted == label) & (true_labels != label))
        miss = np.sum((predicted != label) & (true_labels == label))
        denominator = 2 * hit + false_alarm + miss
        scores.append(0.0 if denominator == 0 else 2 * hit / denominator)
    return float(np.mean(scores))


def predicted_segments(uplift: np.ndarray, p_control: np.ndarray, rules: dict[str, float]) -> np.ndarray:
    return np.array([served_segment(float(u), float(p), rules) for u, p in zip(uplift, p_control, strict=True)])


def choose_segment_rules(
    uplift: np.ndarray, p_control: np.ndarray, true_labels: np.ndarray, grids: dict[str, tuple[float, ...]]
) -> tuple[dict[str, float], float]:
    """Best grid point on validation. The first maximum in grid order wins, so the choice is deterministic."""
    best_rules: dict[str, float] | None = None
    best_score = -1.0
    for dnd, persuadable, sure in product(
        grids["do_not_disturb_uplift_below"],
        grids["persuadable_uplift_at_least"],
        grids["sure_thing_control_rate_at_least"],
    ):
        rules = {
            "do_not_disturb_uplift_below": dnd,
            "persuadable_uplift_at_least": persuadable,
            "sure_thing_control_rate_at_least": sure,
        }
        score = macro_f1(true_labels, predicted_segments(uplift, p_control, rules))
        if score > best_score:
            best_rules, best_score = rules, score
    if best_rules is None:
        raise ValueError("selection grids are empty")
    return best_rules, best_score


def _plain(value: Any) -> Any:
    if isinstance(value, dict):
        return {key: _plain(item) for key, item in value.items()}
    if isinstance(value, (list, tuple)):
        return [_plain(item) for item in value]
    if isinstance(value, (np.floating, float)):
        return None if not np.isfinite(value) else round(float(value), 10)
    if isinstance(value, (np.integer, int)) and not isinstance(value, bool):
        return int(value)
    return value


def evaluate_test_split(
    uplift: np.ndarray,
    treated: np.ndarray,
    outcome: np.ndarray,
    tau: np.ndarray,
    settings: TrainSettings,
) -> dict[str, dict[str, Any]]:
    """Qini, AUUC, uplift in the top share, and correlation with the true tau. Intervals from row bootstrap."""
    sample = rank_sample(uplift, treated, outcome)
    top_count = round(settings.top_share * uplift.shape[0])
    top_mask = np.zeros(uplift.shape[0], dtype=bool)
    top_mask[np.argsort(-uplift, kind="stable")[:top_count]] = True

    def weighted(weights: np.ndarray | None) -> dict[str, float]:
        w = np.ones(uplift.shape[0]) if weights is None else weights
        top_tau = np.sum(w * tau * top_mask) / np.sum(w * top_mask) if np.sum(w * top_mask) > 0 else np.nan
        overall_tau = np.sum(w * tau) / np.sum(w)
        return {
            "qini_coefficient": qini_coefficient(sample, weights=weights),
            "auuc": auuc(sample, weights=weights),
            "uplift_top_share": uplift_at_k(sample, settings.top_share, weights=weights),
            "mean_tau_top_minus_all": float(top_tau - overall_tau),
        }

    point = weighted(None)
    draws = bootstrap(weighted, uplift.shape[0], settings.bootstrap_replicates, settings.bootstrap_seed)
    summary = summarize(draws, point)

    rng = np.random.default_rng(settings.bootstrap_seed)
    correlation_draws: dict[str, list[float]] = {"spearman_tau": [], "pearson_tau": []}
    for _ in range(settings.bootstrap_replicates):
        index = rng.integers(0, uplift.shape[0], uplift.shape[0])
        correlation_draws["spearman_tau"].append(float(spearmanr(uplift[index], tau[index]).statistic))
        correlation_draws["pearson_tau"].append(float(pearsonr(uplift[index], tau[index]).statistic))
    correlation_point = {"spearman_tau": float(spearmanr(uplift, tau).statistic), "pearson_tau": float(pearsonr(uplift, tau).statistic)}
    summary.update(summarize({k: np.asarray(v) for k, v in correlation_draws.items()}, correlation_point))
    return summary


def confusion(true_labels: np.ndarray, predicted: np.ndarray) -> dict[str, dict[str, int]]:
    table: dict[str, dict[str, int]] = {}
    for truth in TRUE_LABELS:
        mask = true_labels == truth
        table[truth] = {label: int(np.sum(mask & (predicted == label))) for label in SEGMENT_LABELS}
    return table


def run(config_path: Path, root: Path = ML_ROOT) -> dict[str, Any]:
    settings = load_train_settings(config_path, root)
    fixture = settings.fixture
    orders = load_demo_orders(fixture.demo_export_dir)
    refs = load_customer_refs(fixture.demo_export_dir)
    frame = build_fixture(orders, refs, fixture)
    summary = fixture_summary(frame)
    print(f"{CHECK_PREFIX} fixture customers={summary['customers']} splits={summary['splits']}", flush=True)

    train = _split_arrays(frame, "train")
    validation = _split_arrays(frame, "validation")
    test_rows = frame.loc[frame["split"] == "test"]

    learner = TLearner()
    rounds = learner.fit(
        train,
        validation,
        "conversion",
        BoosterSettings(
            params=settings.booster_params,
            early_stopping_rounds=settings.early_stopping_rounds,
            max_rounds=settings.max_rounds,
        ),
    )
    treated_model, control_model = learner.models["treated"], learner.models["control"]

    validation_rows = frame.loc[frame["split"] == "validation"]
    validation_x = feature_matrix(validation_rows)
    validation_p_control = control_model.predict(validation_x).astype(float)
    validation_uplift = treated_model.predict(validation_x).astype(float) - validation_p_control
    rules, validation_macro_f1 = choose_segment_rules(
        validation_uplift,
        validation_p_control,
        validation_rows["segment"].to_numpy(),
        settings.grids,
    )
    print(f"{CHECK_PREFIX} validation thresholds={rules} macro_f1={validation_macro_f1:.4f}", flush=True)

    test_x = feature_matrix(test_rows)
    test_p_treated = treated_model.predict(test_x).astype(float)
    test_p_control = control_model.predict(test_x).astype(float)
    test_uplift = test_p_treated - test_p_control
    true_test = test_rows["segment"].to_numpy()
    served_test = predicted_segments(test_uplift, test_p_control, rules)
    metrics = evaluate_test_split(
        test_uplift,
        test_rows["arm"].to_numpy(dtype=int),
        test_rows["outcome"].to_numpy(dtype=int),
        test_rows["tau"].to_numpy(dtype=float),
        settings,
    )
    table = confusion(true_test, served_test)
    per_segment_tau = {
        label: float(test_rows.loc[served_test == label, "tau"].mean()) if np.any(served_test == label) else None
        for label in SEGMENT_LABELS
    }
    test_macro_f1 = macro_f1(true_test, served_test)

    version, artifact_dir = _write_artifact(settings, rules, rounds, treated_model, control_model, metrics, summary)
    report = _render_report(settings, summary, rules, validation_macro_f1, metrics, table, per_segment_tau,
                            test_macro_f1, version, rounds)
    settings.results_md.parent.mkdir(parents=True, exist_ok=True)
    settings.results_md.write_text(report, encoding="utf-8", newline="\n")
    print(f"{CHECK_PREFIX} artifact {artifact_dir.name}", flush=True)
    return {"version": version, "artifact": artifact_dir, "rules": rules, "metrics": metrics}


def _write_artifact(
    settings: TrainSettings,
    rules: dict[str, float],
    rounds: dict[str, int],
    treated_model: Any,
    control_model: Any,
    metrics: dict[str, Any],
    summary: dict[str, object],
) -> tuple[str, Path]:
    demo_manifest_sha = sha256_file(settings.fixture.demo_export_dir / "manifest.json")
    identity = {
        "config_sha256": settings.config_sha256,
        "demo_export_manifest_sha256": demo_manifest_sha,
        "rounds": rounds,
        "segment_rules": rules,
    }
    config_hash = hashlib.sha256(json.dumps(identity, sort_keys=True).encode("utf-8")).hexdigest()[:HASH_CHARS]
    version = f"{MODEL_NAME}-{settings.trained_at:%Y%m%d}-{config_hash}"
    directory = settings.artifact_root / version
    directory.mkdir(parents=True, exist_ok=True)
    treated_path, control_path = directory / UPLIFT_FILES[0], directory / UPLIFT_FILES[1]
    # Early stopping keeps the best iteration; slicing drops the rounds after it so the service scores the same model.
    treated_model.booster[: treated_model.rounds].save_model(treated_path)
    control_model.booster[: control_model.rounds].save_model(control_path)

    manifest = {
        "schema_version": MANIFEST_SCHEMA_VERSION,
        "model_name": MODEL_NAME,
        "version": version,
        "kind": "uplift",
        "feature_schema_version": 1,
        "provenance": PROVENANCE,
        "trained_at": settings.trained_at.isoformat(),
        "label": LABEL,
        "feature_order": list(FEATURE_FIELDS),
        "segment_rules": rules,
        "metrics": {
            "label": LABEL,
            "evaluation_split": "test",
            "test": _plain(metrics),
        },
        "files": {
            UPLIFT_FILES[0]: sha256_file(treated_path),
            UPLIFT_FILES[1]: sha256_file(control_path),
        },
        "training_data": {
            "source": "Online Retail II demo export (cleaned orders)",
            "demo_export_manifest_sha256": demo_manifest_sha,
            "t0": settings.fixture.t0.isoformat(),
            "baseline_window_days": settings.fixture.baseline_days,
            "holdout_share_bps": settings.fixture.holdout_share_bps,
            "effect": "injected tau(x) from configs/crm_uplift_demo.yaml; treated outcomes are simulated",
            "splits": summary["splits"],
            "config_sha256": settings.config_sha256,
        },
        "rounds": rounds,
        "seed": settings.seed,
    }
    write_json(directory / MANIFEST_FILE, manifest)
    return version, directory


def _render_report(
    settings: TrainSettings,
    summary: dict[str, object],
    rules: dict[str, float],
    validation_macro_f1: float,
    metrics: dict[str, dict[str, Any]],
    table: dict[str, dict[str, int]],
    per_segment_tau: dict[str, float | None],
    test_macro_f1: float,
    version: str,
    rounds: dict[str, int],
) -> str:
    def row(name: str) -> str:
        item = metrics[name]
        return f"{item['point']:.4f} [{item['ci_low']:.4f}, {item['ci_high']:.4f}]"

    lines = [
        "# CRM uplift demo: pipeline validation on semi-synthetic data, not an effect estimate",
        "",
        f"> {LABEL}. Covariates and baseline outcomes are real Online Retail II data; the treated outcome is simulated "
        "from an effect function written in `configs/crm_uplift_demo.yaml`. The numbers show that the pipeline recovers "
        "a known effect. They say nothing about real email effects.",
        "",
        "## Setup",
        "",
        f"- Artifact: `{version}` (provenance `{PROVENANCE}`), served as `crm_uplift`.",
        SERVED_DEFAULT_NOTE,
        f"- Customers: {summary['customers']} eligible at T0 (purchase before T0, non-negative value and average).",
        f"- Splits (customer level, stratified by arm and outcome): {summary['splits']}.",
        f"- Arms: {summary['arms']} (1 treated, 0 control), shared holdout algorithm, {settings.fixture.holdout_share_bps} bps holdout.",
        f"- Model: T-learner, XGBoost per arm, rounds kept {rounds}, early stopping on validation logloss.",
        "- Thresholds chosen on validation only: " + ", ".join(f"{key} = {value}" for key, value in rules.items())
        + f" (validation macro F1 {validation_macro_f1:.4f}).",
        "",
        "## Test split (opened once, after the thresholds were frozen)",
        "",
        "| Metric | Point [95 % interval] |",
        "|---|---|",
        f"| Qini coefficient | {row('qini_coefficient')} |",
        f"| AUUC | {row('auuc')} |",
        f"| Uplift in the top {settings.top_share:.0%} | {row('uplift_top_share')} |",
        f"| Mean true tau, top {settings.top_share:.0%} minus all | {row('mean_tau_top_minus_all')} |",
        f"| Spearman correlation with true tau | {row('spearman_tau')} |",
        f"| Pearson correlation with true tau | {row('pearson_tau')} |",
        "",
        f"Intervals are percentile intervals from {settings.bootstrap_replicates} replicates (Poisson weights for the ranking "
        "metrics, row resampling for the correlations).",
        "",
        f"Macro F1 over the four segments on test: {test_macro_f1:.4f}.",
        "",
        "## Confusion by segment (test rows; rows = known segment, columns = served segment)",
        "",
        "| Known segment | " + " | ".join(SEGMENT_LABELS) + " |",
        "|---|" + "---|" * len(SEGMENT_LABELS),
    ]
    for truth in TRUE_LABELS:
        counts = table[truth]
        lines.append(f"| {truth} | " + " | ".join(str(counts[label]) for label in SEGMENT_LABELS) + " |")
    lines += [
        "",
        "`other` is the customers with no injected segment (tau 0). The served model has no such label; those rows are "
        "assigned to one of the four segments by the threshold rule.",
        "",
        "Mean true tau of the customers served in each segment (test): "
        + ", ".join(f"{label} {value:.4f}" if value is not None else f"{label} none" for label, value in per_segment_tau.items())
        + ".",
        "",
        "## Limits",
        "",
        "- The effect is injected by construction. A high Qini or correlation shows the pipeline recovers that effect, "
        "not that an email changes behaviour.",
        "- Baseline outcomes are one purchase in 30 days from Online Retail II (UK, 2009-2011). Not a marketing exposure log.",
        "- The segment thresholds used the known segments on validation, so the segment scores are optimistic about "
        "the served rule. The test split was not used for any choice.",
        "- Only the demo population: customers with a purchase history. New customers are not covered.",
        "",
        "## Reproduce",
        "",
        "```bash",
        "uv run --directory ml python -m parley_ml.uplift.train_crm_demo --config configs/crm_uplift_demo.yaml",
        "```",
        "",
    ]
    return "\n".join(lines)


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Train the semi-synthetic crm_uplift demo model.")
    parser.add_argument("--config", required=True, type=Path, help="Path to crm_uplift_demo.yaml")
    args = parser.parse_args(argv)
    config = args.config if args.config.is_absolute() else ML_ROOT / args.config
    result = run(config)
    print(f"{CHECK_PREFIX} done version={result['version']}", flush=True)


if __name__ == "__main__":
    main()
