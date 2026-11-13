"""Train the demo ``crm_uplift`` model on the semi-synthetic fixture and write its served artifact and report.

Pipeline validation on semi-synthetic data, not an effect estimate. Steps, in order:
1. Build the snapshot panel (real covariates and baseline outcomes, injected effect from the config). The primary
   snapshot carries the split and the only test rows; earlier snapshots are stacked as training rows for train
   customers.
2. Fit the T-learner (one XGBoost model per arm) on the stacked training rows, early-stopped on ``validation_model``.
3. Choose the segment thresholds on ``validation_selection`` only, with a precision floor for do-not-disturb. When no
   threshold reaches the floor the label is switched off and the model never emits it.
4. Open the primary test split once (recorded in the access log), score it with the frozen thresholds, and compute
   tau-based metrics, reference rows (perfect score and 300 random scores) and the seed table.
5. Write ``<artifact root>/crm_uplift-<date>-<hash>/`` (schema 2, provenance ``semi_synthetic``) and the report.

The report leads with true-effect metrics and never claims accuracy: the effect of one message on one person is never
observed, so an uplift model has no accuracy, only a ranking.

Run from ml/:
    uv run --directory ml --group train python -m parley_ml.uplift.train_crm_demo --config configs/crm_uplift_demo.yaml
A rerun of the same config opens the test split again only with ``--reproduce``; a changed config is refused.
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
from sklearn.metrics import roc_auc_score

from parley_ml.datasets.checksums import sha256_file, write_json
from parley_ml.evaluation.uplift import auuc, bootstrap, qini_coefficient, rank_sample, summarize, uplift_at_k
from parley_ml.features.crm import FEATURE_FIELDS
from parley_ml.serving.artifacts import MANIFEST_FILE, MANIFEST_SCHEMA_VERSION, UPLIFT_FILES
from parley_ml.serving.scoring import segment_of as served_segment
from parley_ml.serving.settings import ML_ROOT
from parley_ml.serving.stub import refuse_stub_in_artifact_root
from parley_ml.uplift.crm_demo_fixture import (
    LABEL,
    SPLIT_TRAIN,
    SPLIT_VALIDATION_MODEL,
    SPLIT_VALIDATION_SELECTION,
    FixtureSettings,
    build_fixture,
    feature_matrix,
    fixture_summary,
    load_customer_refs,
    load_demo_orders,
    load_fixture_settings,
    seeded_fixture,
    segment_effects,
    stack_training_rows,
)
from parley_ml.uplift.data import SplitArrays
from parley_ml.uplift.learners import BoosterSettings, TLearner
from parley_ml.uplift.test_access import check_access, record_attempt, update_attempt

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
# An uplift is a probability difference, so it can never be below -1. A threshold of -1 can never match and is the
# manifest representation of "the do-not-disturb label is switched off".
DND_DISABLED_THRESHOLD: Final = -1.0
# The rows of the test table. The true-effect rows come first; the outcome-based rows are noisy on this test size.
METRIC_ROWS: Final[tuple[str, ...]] = (
    "tau_gain_top_share",
    "share_of_perfect_gain",
    "auc_persuadable",
    "spearman_effect",
    "pearson_effect",
    "qini_coefficient",
    "auuc",
    "uplift_top_share",
)
METRIC_LABELS: Final[dict[str, str]] = {
    "tau_gain_top_share": "Mean true effect, top {share} minus all",
    "share_of_perfect_gain": "Share of the perfect gain in the top {share}",
    "auc_persuadable": "AUC persuadable vs rest",
    "spearman_effect": "Spearman correlation with the true effect",
    "pearson_effect": "Pearson correlation with the true effect",
    "qini_coefficient": "Qini coefficient",
    "auuc": "AUUC",
    "uplift_top_share": "Uplift in the top {share} (observed outcomes)",
}
SERVED_DEFAULT_NOTE: Final = (
    "- Served default: the newest trained `crm_uplift` artifact in the registry (by trained date, then version). "
    "The service default is a stub only when nothing trained exists; the API never selects a stub automatically, "
    "an admin must activate it."
)


@dataclass(frozen=True)
class TrainSettings:
    fixture: FixtureSettings
    booster_params: dict[str, Any]
    seed: int
    early_stopping_rounds: int
    max_rounds: int
    grids: dict[str, tuple[float, ...]]
    dnd_precision_floor: float
    bootstrap_replicates: int
    bootstrap_seed: int
    top_share: float
    random_reference_draws: int
    random_reference_seed: int
    seed_count: int
    seed_stride: int
    success_share_floor: float
    success_auc_floor: float
    trained_at: date
    artifact_root: Path
    results_md: Path
    test_access: Path
    config_sha256: str


def load_train_settings(config_path: Path, root: Path = ML_ROOT) -> TrainSettings:
    raw_bytes = config_path.read_bytes()
    raw = yaml.safe_load(raw_bytes.decode("utf-8"))
    model = raw["model"]
    selection = raw["selection"]
    evaluation = raw["evaluation"]
    artifact = raw["artifact"]
    xgboost = dict(model["xgboost"])
    xgboost["seed"] = int(model["seed"])
    success = evaluation["success"]
    return TrainSettings(
        fixture=load_fixture_settings(config_path, root),
        booster_params=xgboost,
        seed=int(model["seed"]),
        early_stopping_rounds=int(model["early_stopping_rounds"]),
        max_rounds=int(model["max_rounds"]),
        grids={key: tuple(float(value) for value in selection[key]) for key in RULE_KEYS},
        dnd_precision_floor=float(selection["do_not_disturb_precision_floor"]),
        bootstrap_replicates=int(evaluation["bootstrap_replicates"]),
        bootstrap_seed=int(evaluation["bootstrap_seed"]),
        top_share=float(evaluation["top_share"]),
        random_reference_draws=int(evaluation["random_reference_draws"]),
        random_reference_seed=int(evaluation["random_reference_seed"]),
        seed_count=int(evaluation["seed_count"]),
        seed_stride=int(evaluation["seed_stride"]),
        success_share_floor=float(success["share_of_perfect_gain_at_least"]),
        success_auc_floor=float(success["auc_persuadable_at_least"]),
        trained_at=date.fromisoformat(str(artifact["trained_at"])),
        artifact_root=root / str(artifact["root"]),
        results_md=root / str(artifact["results_md"]),
        test_access=root / str(artifact["test_access"]),
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


def macro_f1(true_labels: np.ndarray, predicted: np.ndarray, labels: tuple[str, ...] = TRUE_LABELS) -> float:
    """Mean F1 over the named classes. ``other`` is a class: the served rule never emits it, so its F1 is zero."""
    scores = []
    for label in labels:
        hit = np.sum((predicted == label) & (true_labels == label))
        false_alarm = np.sum((predicted == label) & (true_labels != label))
        miss = np.sum((predicted != label) & (true_labels == label))
        denominator = 2 * hit + false_alarm + miss
        scores.append(0.0 if denominator == 0 else 2 * hit / denominator)
    return float(np.mean(scores))


def predicted_segments(uplift: np.ndarray, p_control: np.ndarray, rules: dict[str, float]) -> np.ndarray:
    return np.array([served_segment(float(u), float(p), rules) for u, p in zip(uplift, p_control, strict=True)])


def choose_segment_rules(
    uplift: np.ndarray,
    p_control: np.ndarray,
    true_labels: np.ndarray,
    grids: dict[str, tuple[float, ...]],
    precision_floor: float,
) -> tuple[dict[str, float], dict[str, Any]]:
    """Best validation-selection grid point under a do-not-disturb precision floor.

    The switched-off label is tried first, so a do-not-disturb threshold is chosen only when it improves the macro F1
    and labels the true do-not-disturb customers at or above the floor. The first maximum in grid order wins, so the
    choice is deterministic.
    """
    candidates = [(DND_DISABLED_THRESHOLD, True), *((value, False) for value in grids["do_not_disturb_uplift_below"])]
    best_rules: dict[str, float] | None = None
    best_score = -1.0
    best_diagnostics: dict[str, Any] = {}
    for dnd, disabled in candidates:
        for persuadable, sure in product(
            grids["persuadable_uplift_at_least"],
            grids["sure_thing_control_rate_at_least"],
        ):
            rules = {
                "do_not_disturb_uplift_below": dnd,
                "persuadable_uplift_at_least": persuadable,
                "sure_thing_control_rate_at_least": sure,
            }
            predicted = predicted_segments(uplift, p_control, rules)
            dnd_mask = predicted == "do_not_disturb"
            predicted_dnd = int(dnd_mask.sum())
            precision = float(np.mean(true_labels[dnd_mask] == "do_not_disturb")) if predicted_dnd else None
            if not disabled and predicted_dnd and (precision or 0.0) < precision_floor:
                continue
            score = macro_f1(true_labels, predicted)
            if score > best_score:
                best_rules, best_score = rules, score
                best_diagnostics = {
                    "macro_f1": score,
                    "do_not_disturb_enabled": not disabled,
                    "do_not_disturb_precision": precision,
                    "do_not_disturb_rows": predicted_dnd,
                }
    if best_rules is None:
        raise ValueError("selection grids are empty")
    return best_rules, best_diagnostics


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


def _correlation(left: np.ndarray, right: np.ndarray, kind: str) -> float:
    if np.std(left) == 0.0 or np.std(right) == 0.0:
        return float("nan")
    value = spearmanr(left, right).statistic if kind == "spearman" else pearsonr(left, right).statistic
    return float(value)


def _auc(score: np.ndarray, positive: np.ndarray) -> float:
    if positive.all() or not positive.any():
        return float("nan")
    return float(roc_auc_score(positive.astype(int), score))


def point_metrics(
    score: np.ndarray,
    effect: np.ndarray,
    treatment: np.ndarray,
    outcome: np.ndarray,
    persuadable: np.ndarray,
    top_share: float,
) -> dict[str, float]:
    """Ranking metrics on one test split. The perfect score ranks by the true effect (tau-based, not observed)."""
    sample = rank_sample(score, treatment, outcome)
    top_count = max(1, round(top_share * score.shape[0]))
    top = np.argsort(-score, kind="stable")[:top_count]
    perfect_top = np.argsort(-effect, kind="stable")[:top_count]
    overall = float(np.mean(effect))
    gain = float(np.mean(effect[top]) - overall)
    perfect_gain = float(np.mean(effect[perfect_top]) - overall)
    return {
        "tau_gain_top_share": gain,
        "share_of_perfect_gain": gain / perfect_gain if perfect_gain > 0.0 else float("nan"),
        "auc_persuadable": _auc(score, persuadable),
        "spearman_effect": _correlation(score, effect, "spearman"),
        "pearson_effect": _correlation(score, effect, "pearson"),
        "qini_coefficient": qini_coefficient(sample),
        "auuc": auuc(sample),
        "uplift_top_share": uplift_at_k(sample, top_share),
    }


def evaluate_test_split(
    score: np.ndarray,
    effect: np.ndarray,
    treatment: np.ndarray,
    outcome: np.ndarray,
    persuadable: np.ndarray,
    settings: TrainSettings,
) -> dict[str, dict[str, Any]]:
    """Point metrics with intervals. The intervals cover test-row sampling only, never training or outcome draws."""
    point = point_metrics(score, effect, treatment, outcome, persuadable, settings.top_share)
    sample = rank_sample(score, treatment, outcome)
    top_count = max(1, round(settings.top_share * score.shape[0]))
    top_mask = np.zeros(score.shape[0], dtype=bool)
    top_mask[np.argsort(-score, kind="stable")[:top_count]] = True

    def weighted(weights: np.ndarray) -> dict[str, float]:
        top_total = float(np.sum(weights * top_mask))
        top_effect = float(np.sum(weights * effect * top_mask) / top_total) if top_total > 0.0 else float("nan")
        overall = float(np.sum(weights * effect) / np.sum(weights))
        return {
            "tau_gain_top_share": top_effect - overall,
            "qini_coefficient": qini_coefficient(sample, weights=weights),
            "auuc": auuc(sample, weights=weights),
            "uplift_top_share": uplift_at_k(sample, settings.top_share, weights=weights),
        }

    draws = bootstrap(weighted, score.shape[0], settings.bootstrap_replicates, settings.bootstrap_seed)
    summary = summarize(draws, point)

    rng = np.random.default_rng(settings.bootstrap_seed)
    resampled: dict[str, list[float]] = {"auc_persuadable": [], "spearman_effect": [], "pearson_effect": []}
    for _ in range(settings.bootstrap_replicates):
        index = rng.integers(0, score.shape[0], score.shape[0])
        resampled["auc_persuadable"].append(_auc(score[index], persuadable[index]))
        resampled["spearman_effect"].append(_correlation(score[index], effect[index], "spearman"))
        resampled["pearson_effect"].append(_correlation(score[index], effect[index], "pearson"))
    summary.update(summarize({name: np.asarray(values) for name, values in resampled.items()}, point))
    summary["share_of_perfect_gain"] = {
        "point": point["share_of_perfect_gain"],
        "ci_low": float("nan"),
        "ci_high": float("nan"),
        "replicates": 0,
    }
    return summary


def reference_metrics(
    effect: np.ndarray,
    treatment: np.ndarray,
    outcome: np.ndarray,
    persuadable: np.ndarray,
    settings: TrainSettings,
) -> dict[str, dict[str, float]]:
    """Perfect score (rank by the true effect) and random scores on the same test rows.

    Random reference: mean and 97.5th percentile over the configured number of draws, which shows how large a metric
    can look on this test size with no signal at all.
    """
    perfect = point_metrics(effect, effect, treatment, outcome, persuadable, settings.top_share)
    rng = np.random.default_rng(settings.random_reference_seed)
    draws = [
        point_metrics(rng.random(effect.shape[0]), effect, treatment, outcome, persuadable, settings.top_share)
        for _ in range(settings.random_reference_draws)
    ]
    references: dict[str, dict[str, float]] = {}
    for name in METRIC_ROWS:
        values = np.asarray([draw[name] for draw in draws], dtype=float)
        finite = values[np.isfinite(values)]
        references[name] = {
            "perfect": perfect[name],
            "random_mean": float(np.mean(finite)) if finite.size else float("nan"),
            "random_high": float(np.percentile(finite, 97.5)) if finite.size else float("nan"),
        }
    return references


def confusion(true_labels: np.ndarray, predicted: np.ndarray) -> dict[str, dict[str, int]]:
    table: dict[str, dict[str, int]] = {}
    for truth in TRUE_LABELS:
        mask = true_labels == truth
        table[truth] = {label: int(np.sum(mask & (predicted == label))) for label in SEGMENT_LABELS}
    return table


def segment_precision(true_labels: np.ndarray, predicted: np.ndarray, label: str) -> dict[str, Any]:
    mask = predicted == label
    rows = int(mask.sum())
    return {
        "rows": rows,
        "precision": float(np.mean(true_labels[mask] == label)) if rows else None,
    }


def seed_table(
    settings: TrainSettings,
    orders: pd.DataFrame,
    customer_refs: list[str],
    primary_metrics: dict[str, float],
) -> list[dict[str, Any]]:
    """The same pipeline under shifted outcome and split seeds. Seed 0 is the primary run; no choice uses test rows."""
    rows: list[dict[str, Any]] = [{"seed": 0, **primary_metrics}]
    for index in range(1, settings.seed_count):
        fixture = seeded_fixture(settings.fixture, index, settings.seed_stride)
        primary = build_fixture(orders, customer_refs, fixture)
        panel = stack_training_rows(orders, customer_refs, fixture, primary)
        learner = TLearner()
        learner.fit(
            _split_arrays(panel, SPLIT_TRAIN),
            _split_arrays(primary, SPLIT_VALIDATION_MODEL),
            "conversion",
            BoosterSettings(
                params=settings.booster_params,
                early_stopping_rounds=settings.early_stopping_rounds,
                max_rounds=settings.max_rounds,
            ),
        )
        test_rows = primary.loc[primary["split"] == "test"]
        score = learner.score(feature_matrix(test_rows)).astype(float)
        metrics = point_metrics(
            score,
            test_rows["effect"].to_numpy(dtype=float),
            test_rows["arm"].to_numpy(dtype=int),
            test_rows["outcome"].to_numpy(dtype=int),
            (test_rows["segment"] == "persuadable").to_numpy(),
            settings.top_share,
        )
        rows.append({"seed": index, **metrics})
        print(f"{CHECK_PREFIX} seed {index} share_of_perfect={metrics['share_of_perfect_gain']:.4f}", flush=True)
    return rows


def success_summary(rows: list[dict[str, Any]], settings: TrainSettings) -> dict[str, Any]:
    shares = np.asarray([row["share_of_perfect_gain"] for row in rows], dtype=float)
    aucs = np.asarray([row["auc_persuadable"] for row in rows], dtype=float)

    def stat(values: np.ndarray, name: str) -> dict[str, float]:
        finite = values[np.isfinite(values)]
        return {
            f"{name}_mean": float(np.mean(finite)) if finite.size else float("nan"),
            f"{name}_min": float(np.min(finite)) if finite.size else float("nan"),
            f"{name}_max": float(np.max(finite)) if finite.size else float("nan"),
        }

    share_ok = bool(np.all(shares >= settings.success_share_floor))
    auc_ok = bool(np.all(aucs >= settings.success_auc_floor))
    return {
        "seeds": len(rows),
        "share_of_perfect_gain_at_least": settings.success_share_floor,
        "auc_persuadable_at_least": settings.success_auc_floor,
        **stat(shares, "share_of_perfect_gain"),
        **stat(aucs, "auc_persuadable"),
        "share_met_in_all_seeds": share_ok,
        "auc_met_in_all_seeds": auc_ok,
        "met_in_all_seeds": share_ok and auc_ok,
    }


def run(config_path: Path, root: Path = ML_ROOT, reproduce: bool = False) -> dict[str, Any]:
    settings = load_train_settings(config_path, root)
    refuse_stub_in_artifact_root(settings.artifact_root)
    fixture = settings.fixture
    orders = load_demo_orders(fixture.demo_export_dir)
    customer_refs = load_customer_refs(fixture.demo_export_dir)

    primary = build_fixture(orders, customer_refs, fixture)
    summary = fixture_summary(primary)
    effects = segment_effects(primary)
    panel = stack_training_rows(orders, customer_refs, fixture, primary)
    print(
        f"{CHECK_PREFIX} customers={summary['customers']} splits={summary['splits']} stacked_train_rows={len(panel)}",
        flush=True,
    )

    learner = TLearner()
    rounds = learner.fit(
        _split_arrays(panel, SPLIT_TRAIN),
        _split_arrays(primary, SPLIT_VALIDATION_MODEL),
        "conversion",
        BoosterSettings(
            params=settings.booster_params,
            early_stopping_rounds=settings.early_stopping_rounds,
            max_rounds=settings.max_rounds,
        ),
    )
    treated_model, control_model = learner.models["treated"], learner.models["control"]

    selection_rows = primary.loc[primary["split"] == SPLIT_VALIDATION_SELECTION]
    selection_x = feature_matrix(selection_rows)
    selection_p_control = control_model.predict(selection_x).astype(float)
    selection_uplift = treated_model.predict(selection_x).astype(float) - selection_p_control
    rules, selection = choose_segment_rules(
        selection_uplift,
        selection_p_control,
        selection_rows["segment"].to_numpy(),
        settings.grids,
        settings.dnd_precision_floor,
    )
    print(f"{CHECK_PREFIX} validation thresholds={rules} diagnostics={selection}", flush=True)

    # The test split is opened only here, after every choice is frozen, and the attempt is recorded first.
    check_access(settings.test_access, settings.config_sha256, reproduce)
    attempt = record_attempt(settings.test_access, settings.config_sha256, reproduce)

    test_rows = primary.loc[primary["split"] == "test"]
    test_x = feature_matrix(test_rows)
    test_p_treated = treated_model.predict(test_x).astype(float)
    test_p_control = control_model.predict(test_x).astype(float)
    test_uplift = test_p_treated - test_p_control
    true_test = test_rows["segment"].to_numpy()
    served_test = predicted_segments(test_uplift, test_p_control, rules)
    test_effect = test_rows["effect"].to_numpy(dtype=float)
    test_treatment = test_rows["arm"].to_numpy(dtype=int)
    test_outcome = test_rows["outcome"].to_numpy(dtype=int)
    test_persuadable = (test_rows["segment"] == "persuadable").to_numpy()

    metrics = evaluate_test_split(test_uplift, test_effect, test_treatment, test_outcome, test_persuadable, settings)
    references = reference_metrics(test_effect, test_treatment, test_outcome, test_persuadable, settings)
    point = {name: metrics[name]["point"] for name in metrics}
    table = confusion(true_test, served_test)
    dnd = segment_precision(true_test, served_test, "do_not_disturb")
    per_segment_effect = {
        label: float(test_rows.loc[served_test == label, "effect"].mean()) if np.any(served_test == label) else None
        for label in SEGMENT_LABELS
    }
    macro_f1_served = macro_f1(true_test, served_test, SEGMENT_LABELS)
    macro_f1_known = macro_f1(true_test, served_test, TRUE_LABELS)

    seed_rows = seed_table(settings, orders, customer_refs, point)
    success = success_summary(seed_rows, settings)

    version, artifact_dir = _write_artifact(
        settings,
        rules,
        rounds,
        treated_model,
        control_model,
        metrics,
        references,
        seed_rows,
        success,
        summary,
        effects,
        len(panel),
    )
    report = _render_report(
        settings,
        summary,
        effects,
        len(panel),
        rules,
        selection,
        metrics,
        references,
        table,
        dnd,
        per_segment_effect,
        macro_f1_served,
        macro_f1_known,
        seed_rows,
        success,
        version,
        rounds,
    )
    settings.results_md.parent.mkdir(parents=True, exist_ok=True)
    settings.results_md.write_text(report, encoding="utf-8", newline="\n")
    update_attempt(settings.test_access, attempt, {"version": version, "outcome": "completed", "success": _plain(success)})
    print(f"{CHECK_PREFIX} artifact {artifact_dir.name}", flush=True)
    return {
        "version": version,
        "artifact": artifact_dir,
        "rules": rules,
        "metrics": metrics,
        "references": references,
        "seeds": seed_rows,
        "success": success,
    }


def _write_artifact(
    settings: TrainSettings,
    rules: dict[str, float],
    rounds: dict[str, int],
    treated_model: Any,
    control_model: Any,
    metrics: dict[str, Any],
    references: dict[str, Any],
    seed_rows: list[dict[str, Any]],
    success: dict[str, Any],
    summary: dict[str, object],
    effects: dict[str, dict[str, float]],
    training_rows: int,
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
            "references": _plain(references),
            "seed_table": _plain(seed_rows),
            "success_criterion": _plain(success),
        },
        "files": {
            UPLIFT_FILES[0]: sha256_file(treated_path),
            UPLIFT_FILES[1]: sha256_file(control_path),
        },
        "training_data": {
            "source": "Online Retail II demo export (cleaned orders)",
            "demo_export_manifest_sha256": demo_manifest_sha,
            "snapshots": [
                {
                    "t0": snapshot.t0.isoformat(),
                    "assignment_seed": snapshot.assignment_seed,
                    "outcome_seed": snapshot.outcome_seed,
                }
                for snapshot in settings.fixture.snapshots
            ],
            "test_snapshot": settings.fixture.primary.t0.isoformat(),
            "baseline_window_days": settings.fixture.baseline_days,
            "holdout_share_bps": settings.fixture.holdout_share_bps,
            "effect": "injected tau(x) from configs/crm_uplift_demo.yaml; treated outcomes are simulated",
            "true_effect": "tau * (1 - P(y0)) for positive tau, tau * P(y0) for negative tau",
            "baseline_rate": summary["y0_rate"],
            "segment_effects": effects,
            "splits": summary["splits"],
            "training_rows": training_rows,
            "config_sha256": settings.config_sha256,
        },
        "rounds": rounds,
        "seed": settings.seed,
    }
    write_json(directory / MANIFEST_FILE, manifest)
    return version, directory


def _row(name: str, metrics: dict[str, dict[str, Any]], references: dict[str, dict[str, float]], share: str) -> str:
    item = metrics[name]
    perfect = references[name]["perfect"]
    model = item["point"]
    share_of_perfect = model / perfect if perfect not in (0.0, None) and np.isfinite(perfect) else float("nan")
    share_text = "n/a" if not np.isfinite(share_of_perfect) else f"{share_of_perfect:.2f}"
    if name == "share_of_perfect_gain":
        model_text = f"{model:.4f} (point ratio)" if np.isfinite(model) else "n/a"
        return (
            f"| {METRIC_LABELS[name].format(share=share)} | {model_text} | "
            f"{perfect:.2f} | {references[name]['random_mean']:.2f} | {references[name]['random_high']:.2f} | {share_text} |"
        )
    return (
        f"| {METRIC_LABELS[name].format(share=share)} | {model:.4f} [{item['ci_low']:.4f}, {item['ci_high']:.4f}] | "
        f"{perfect:.4f} | {references[name]['random_mean']:.4f} | {references[name]['random_high']:.4f} | {share_text} |"
    )


def _render_report(
    settings: TrainSettings,
    summary: dict[str, object],
    effects: dict[str, dict[str, float]],
    training_rows: int,
    rules: dict[str, float],
    selection: dict[str, Any],
    metrics: dict[str, dict[str, Any]],
    references: dict[str, dict[str, float]],
    table: dict[str, dict[str, int]],
    dnd: dict[str, Any],
    per_segment_effect: dict[str, float | None],
    macro_f1_served: float,
    macro_f1_known: float,
    seed_rows: list[dict[str, Any]],
    success: dict[str, Any],
    version: str,
    rounds: dict[str, int],
) -> str:
    share = f"{settings.top_share:.0%}"
    splits = summary["splits"]
    snapshot_days = " → ".join(str(snapshot.t0.date()) for snapshot in settings.fixture.snapshots)
    try:
        access_path = settings.test_access.relative_to(ML_ROOT).as_posix()
    except ValueError:
        access_path = settings.test_access.name
    dnd_text = (
        f"enabled at {rules['do_not_disturb_uplift_below']:g}"
        if selection["do_not_disturb_enabled"]
        else "switched off: no threshold both met the precision floor and improved the macro F1, so the model never "
        "emits it"
    )
    validation_precision = selection.get("do_not_disturb_precision")
    lines = [
        "# CRM uplift demo: pipeline validation on semi-synthetic data, not an effect estimate",
        "",
        f"> {LABEL}. Covariates and baseline outcomes are real Online Retail II data; the treated outcome is simulated "
        "from an effect function written in `configs/crm_uplift_demo.yaml`. The numbers show that the pipeline recovers "
        "a known effect. They say nothing about real email effects. No accuracy is reported: the effect of one message "
        "on one person is never observed, so an uplift model is judged by its ranking, not by accuracy.",
        "",
        "## Setup",
        "",
        f"- Artifact: `{version}` (provenance `{PROVENANCE}`), served as `crm_uplift`. The artifact root is generated "
        "and git-ignored.",
        SERVED_DEFAULT_NOTE,
        f"- Snapshots: {snapshot_days}. Only the last snapshot (the primary one) provides test rows; earlier snapshots "
        "add training rows for customers whose primary split is train.",
        f"- Customers at the primary snapshot: {summary['customers']} eligible (purchase before T0, non-negative value "
        f"and average). Training rows after stacking: {training_rows}.",
        f"- Splits (customer level, stratified by arm and outcome): {splits}. `validation_model` stops the boosters; "
        "`validation_selection` chooses the segment thresholds. The two parts never share rows.",
        f"- Arms: {summary['arms']} (1 treated, 0 control), shared holdout algorithm, "
        f"{settings.fixture.holdout_share_bps} bps holdout. Observed rates: {summary['rates']}.",
        f"- Model: T-learner, XGBoost per arm, rounds kept {rounds}, early stopping on `validation_model` logloss.",
        "- Thresholds chosen on `validation_selection` only: "
        + ", ".join(f"{key} = {value:g}" for key, value in rules.items())
        + f" (validation macro F1 {selection['macro_f1']:.4f}; do-not-disturb {dnd_text}"
        + (
            f", validation precision {validation_precision:.2f} on {selection['do_not_disturb_rows']} rows"
            if validation_precision is not None
            else ""
        )
        + ").",
        f"- True effect of a row: `tau * (1 - P(y0))` for positive tau and `tau * P(y0)` for negative tau, with P(y0) "
        f"the snapshot's observed baseline purchase rate ({summary['y0_rate']:.4f} at the primary snapshot). `tau` "
        "alone would overstate the effect, because a positive effect can only convert a non-buyer.",
        "",
        "| True segment | Rows | Mean tau | Mean true effect |",
        "|---|---|---|---|",
    ]
    for name in TRUE_LABELS:
        item = effects.get(name)
        if item is None:
            continue
        lines.append(f"| {name} | {item['rows']} | {item['mean_tau']:.4f} | {item['mean_effect']:.4f} |")
    lines += [
        "",
        f"## Test split (opened once per run, after the thresholds were frozen; top share {share})",
        "",
        "| Metric | Model point [95 % interval] | Perfect score | Random mean | Random 97.5th pct | Share of perfect |",
        "|---|---|---|---|---|---|",
    ]
    for name in METRIC_ROWS:
        lines.append(_row(name, metrics, references, share))
    lines += [
        "",
        f"- The perfect score ranks by the true effect. Random scores are drawn on the same test rows: mean and 97.5th "
        f"percentile over {settings.random_reference_draws} draws. A model value near the random 97.5th percentile is "
        "noise on this test size.",
        f"- Intervals are percentile intervals from {settings.bootstrap_replicates} replicates (Poisson weights for the "
        "test-row sampling bootstrap of the ranking metrics, row resampling for AUC and the correlations). They cover "
        "test-row sampling only. They do not cover training variance or the outcome draw; the seed table below does.",
        f"- The share of the perfect gain is the model's top-{share} gain divided by the perfect-score gain. The perfect "
        "value of the share row is 1; it is a point ratio and the interval of the raw gain is the row above.",
        "",
        f"## Seed table ({success['seeds']} outcome/split seeds, the artifact is seed 0)",
        "",
        "Each seed redraws the simulated outcomes and the customer split with fixed assignment and booster seeds. No "
        "choice uses the test rows; the table only measures how stable the result is.",
        "",
        "| Seed | Share of perfect gain | AUC persuadable | Spearman | Pearson | Qini | Qini > 0 |",
        "|---|---|---|---|---|---|---|",
    ]
    for row in seed_rows:
        lines.append(
            f"| {row['seed']} | {row['share_of_perfect_gain']:.4f} | {row['auc_persuadable']:.4f} | "
            f"{row['spearman_effect']:.4f} | {row['pearson_effect']:.4f} | {row['qini_coefficient']:.4f} | "
            f"{'yes' if row['qini_coefficient'] > 0 else 'no'} |"
        )
    share_values = np.asarray([row["share_of_perfect_gain"] for row in seed_rows], dtype=float)
    auc_values = np.asarray([row["auc_persuadable"] for row in seed_rows], dtype=float)
    lines += [
        "",
        f"- Share of perfect gain: mean {np.nanmean(share_values):.4f}, range "
        f"[{np.nanmin(share_values):.4f}, {np.nanmax(share_values):.4f}].",
        f"- AUC persuadable vs rest: mean {np.nanmean(auc_values):.4f}, range "
        f"[{np.nanmin(auc_values):.4f}, {np.nanmax(auc_values):.4f}].",
        f"- Success criterion, fixed before the run: share of perfect gain at least "
        f"{settings.success_share_floor:.2f} and AUC at least {settings.success_auc_floor:.2f}. "
        + (
            "Met in every seed."
            if success["met_in_all_seeds"]
            else "Not met in every seed: share "
            + ("met" if success["share_met_in_all_seeds"] else "missed")
            + ", AUC "
            + ("met" if success["auc_met_in_all_seeds"] else "missed")
            + ". The numbers above are the result; nothing was tuned towards the criterion."
        ),
        "",
        "## Segment labels on test (frozen thresholds)",
        "",
        "| Known segment | " + " | ".join(SEGMENT_LABELS) + " |",
        "|---|" + "---|" * len(SEGMENT_LABELS),
    ]
    for truth in TRUE_LABELS:
        counts = table[truth]
        lines.append(f"| {truth} | " + " | ".join(str(counts[label]) for label in SEGMENT_LABELS) + " |")
    lines += [
        "",
        f"- Macro F1 over the four served labels: {macro_f1_served:.4f}. Macro F1 over all five known classes, with "
        f"`other` counted as a class the served rule never emits: {macro_f1_known:.4f}.",
        f"- Do-not-disturb precision on test: "
        + (
            f"{dnd['precision']:.2f} on {dnd['rows']} served rows."
            if dnd["precision"] is not None
            else "no served rows, the label is switched off."
        )
        + " A contact is never labeled do_not_disturb on validation unless the threshold reaches the precision floor.",
        "- Mean true effect of the customers served in each segment (test): "
        + ", ".join(
            f"{label} {value:.4f}" if value is not None else f"{label} none"
            for label, value in per_segment_effect.items()
        )
        + ".",
        "- `other` is the customers with no injected segment (tau 0). The served model has no such label; those rows are "
        "assigned to one of the four segments by the threshold rule.",
        "",
        "## Limits",
        "",
        "- The effect is injected by construction. A high Qini or correlation shows the pipeline recovers that effect, "
        "not that an email changes behaviour.",
        "- No accuracy is claimed anywhere: an individual treatment effect is never observed, so accuracy does not apply.",
        "- Baseline outcomes are one purchase in 30 days from Online Retail II (UK, 2009-2011). Not a marketing exposure "
        "log.",
        "- The segment thresholds used the known segments on validation, so the segment scores are optimistic about the "
        "served rule. The test split was not used for any choice.",
        "- The demo model is not allowed to drive hard exclusion in the product: its do_not_disturb label is advisory, "
        "and excluding a contact is a backend decision reserved for a model with real holdout data.",
        "- Only the demo population: customers with a purchase history. New customers are not covered.",
        "",
        "## Reproduce",
        "",
        "```bash",
        "uv run --directory ml --group train python -m parley_ml.uplift.train_crm_demo --config configs/crm_uplift_demo.yaml",
        "```",
        "",
        "A rerun of this exact config needs `--reproduce` (same inputs, same artifact bytes) and is recorded in the test "
        f"access log at `{access_path}`. A changed config is refused, so the test split cannot be reused while the "
        "procedure is being tuned.",
        "",
    ]
    return "\n".join(lines)


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Train the semi-synthetic crm_uplift demo model.")
    parser.add_argument("--config", required=True, type=Path, help="Path to crm_uplift_demo.yaml")
    parser.add_argument(
        "--reproduce",
        action="store_true",
        help="Allowed only after the same config already opened the test split; used for the determinism check.",
    )
    args = parser.parse_args(argv)
    config = args.config if args.config.is_absolute() else ML_ROOT / args.config
    result = run(config, reproduce=args.reproduce)
    print(f"{CHECK_PREFIX} done version={result['version']}", flush=True)


if __name__ == "__main__":
    main()
