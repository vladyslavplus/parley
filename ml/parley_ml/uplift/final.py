"""Final evaluation of the validation winners on the test split (RQ1).

Order of operations, enforced in code:
1. Winners are read from the committed config and checked against the committed validation grid (same selection rule).
2. Validation phase: each winner is refitted with each seed (training split, early stopping on validation), validation
   scores are computed, and k* is chosen per cost/value ratio on validation. The freeze file is written here.
3. Test phase: only after the freeze file exists, the test split is opened. The access log is written before the
   split is read. A second opening needs ``reproduce=True``, which compares the regenerated freeze and results with the
   files from the first run and changes nothing.
"""

from __future__ import annotations

import csv
import dataclasses
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Final

import numpy as np
import xgboost as xgb

from parley_ml.datasets.checksums import sha256_bytes, sha256_file, write_json
from parley_ml.evaluation.uplift import (
    auuc,
    best_fraction,
    bootstrap,
    conditional_profit,
    qini_coefficient,
    qini_curve,
    rank_sample,
    separate_relative_auuc,
    summarize,
    uniform_grid,
    uplift_at_k,
)
from parley_ml.uplift.config import ML_ROOT, Cell, FamilyConfig
from parley_ml.uplift.data import DataBundle, SplitArrays
from parley_ml.uplift.learners import LEARNERS, BoosterSettings, FittedBooster
from parley_ml.uplift.runs import environment, git_state
from parley_ml.uplift.train import booster_params

SCHEMA_VERSION: Final = 1
SCORE_CHUNK_ROWS: Final = 500_000
FAMILIES: Final[tuple[str, ...]] = ("response", "s", "t")
BASELINE: Final = "response"
FREEZE_FILE: Final = "freeze.json"
ACCESS_FILE: Final = "test_access.json"
RESULTS_FILE: Final = "results.json"
MISSING_SIZE: Final = "full"


@dataclass(frozen=True)
class FinalConfig:
    path: Path
    config_sha256: str
    primary_outcome: str
    seeds: tuple[int, ...]
    grid_summary: Path
    selection_metric: str
    winners: dict[str, dict[str, Cell]]
    bootstrap_replicates: int
    bootstrap_seed: int
    sample_sizes: tuple[int | None, ...]
    sample_seed: int
    cost_to_value_ratios: tuple[float, ...]
    profit_grid_points: int
    final_root: Path
    results_dir: Path
    report: Path
    notes: Path
    figures: Path
    family_configs: dict[str, Path]


def load_final_config(path: Path) -> FinalConfig:
    payload = path.read_bytes()
    raw = yaml_load(payload, path)
    families = {name: ML_ROOT / str(raw["family_configs"][name]) for name in FAMILIES}
    winners: dict[str, dict[str, Cell]] = {}
    for outcome, by_family in raw["winners"].items():
        winners[outcome] = {
            family: Cell(max_depth=int(by_family[family]["max_depth"]), learning_rate=float(by_family[family]["learning_rate"]))
            for family in FAMILIES
        }
    sizes = tuple(None if value == MISSING_SIZE else int(value) for value in raw["sample_sizes"])
    if sizes[-1] is not None:
        raise ValueError("the last sample size must be 'full'")
    outputs = raw["outputs"]
    config = FinalConfig(
        path=path,
        config_sha256=sha256_bytes(payload),
        primary_outcome=str(raw["primary_outcome"]),
        seeds=tuple(int(seed) for seed in raw["seeds"]),
        grid_summary=ML_ROOT / str(raw["grid_summary"]),
        selection_metric=str(raw["selection_metric"]),
        winners=winners,
        bootstrap_replicates=int(raw["bootstrap"]["replicates"]),
        bootstrap_seed=int(raw["bootstrap"]["seed"]),
        sample_sizes=sizes,
        sample_seed=int(raw["sample_seed"]),
        cost_to_value_ratios=tuple(float(value) for value in raw["cost_to_value_ratios"]),
        profit_grid_points=int(raw["profit_grid_points"]),
        final_root=ML_ROOT / str(outputs["final_root"]),
        results_dir=ML_ROOT / str(outputs["results_dir"]),
        report=ML_ROOT / str(outputs["report"]),
        notes=ML_ROOT / str(outputs["notes"]),
        figures=ML_ROOT / str(outputs["figures"]),
        family_configs=families,
    )
    if config.primary_outcome not in winners:
        raise ValueError("primary_outcome must be one of the winner outcomes")
    return config


def yaml_load(payload: bytes, path: Path) -> dict[str, Any]:
    import yaml

    raw = yaml.safe_load(payload)
    if not isinstance(raw, dict):
        raise ValueError(f"{path}: the config must be a mapping")
    return raw


def verify_winners(config: FinalConfig) -> dict[str, Any]:
    """The configured winners must equal the best cell per family and outcome on the committed validation grid."""
    with config.grid_summary.open(encoding="utf-8", newline="") as handle:
        rows = list(csv.DictReader(handle))
    checked: dict[str, Any] = {}
    for outcome, by_family in config.winners.items():
        for family in FAMILIES:
            candidates = [
                row
                for row in rows
                if row["family"] == family and row["outcome"] == outcome and row["max_depth"] != ""
            ]
            if not candidates:
                raise ValueError(f"no grid rows for {family}/{outcome}")
            best = max(candidates, key=lambda row: float(row[config.selection_metric]))
            cell = by_family[family]
            if int(best["max_depth"]) != cell.max_depth or abs(float(best["learning_rate"]) - cell.learning_rate) > 1e-12:
                raise ValueError(
                    f"configured winner for {family}/{outcome} is depth {cell.max_depth} lr {cell.learning_rate}, "
                    f"but the validation grid selects depth {best['max_depth']} lr {best['learning_rate']}"
                )
            checked[f"{outcome}/{family}"] = {
                "max_depth": cell.max_depth,
                "learning_rate": cell.learning_rate,
                "validation_" + config.selection_metric: float(best[config.selection_metric]),
            }
    return checked


@dataclass
class FittedWinner:
    outcome: str
    family: str
    seed: int
    cell: Cell
    learner: Any
    rounds: dict[str, int]


def fit_winners(
    config: FinalConfig,
    family_configs: dict[str, FamilyConfig],
    train_bundles: dict[str, DataBundle],
) -> dict[tuple[str, str, int], FittedWinner]:
    fitted: dict[tuple[str, str, int], FittedWinner] = {}
    for outcome in config.winners:
        for family in FAMILIES:
            cell = config.winners[outcome][family]
            for seed in config.seeds:
                fam = dataclasses.replace(family_configs[family], seed=seed)
                settings = BoosterSettings(
                    params=booster_params(fam, cell),
                    early_stopping_rounds=fam.early_stopping_rounds,
                    max_rounds=fam.max_rounds,
                )
                bundle = train_bundles[family]
                learner = LEARNERS[family]()
                rounds = learner.fit(bundle["train"], bundle["validation"], outcome, settings)
                fitted[(outcome, family, seed)] = FittedWinner(outcome, family, seed, cell, learner, rounds)
    return fitted


def validation_phase(
    config: FinalConfig,
    family_configs: dict[str, FamilyConfig],
    fitted: dict[tuple[str, str, int], FittedWinner],
    validation_scores: dict[tuple[str, str, int], np.ndarray],
    validation_bundle: DataBundle,
) -> dict[str, Any]:
    """Validation metrics per model and the k* chosen per cost/value ratio, from the mean profit curve over seeds."""
    fractions = uniform_grid(config.profit_grid_points)
    ratios = np.asarray(config.cost_to_value_ratios)
    validation = validation_bundle["validation"]
    freeze: dict[str, Any] = {
        "schema_version": SCHEMA_VERSION,
        "config_sha256": config.config_sha256,
        "grid_summary_sha256": sha256_file(config.grid_summary),
        "winners": {},
        "k_star": {},
        "validation": {},
    }
    for outcome in config.winners:
        y = validation.outcome(outcome)
        freeze["validation"][outcome] = {}
        freeze["winners"][outcome] = {}
        freeze["k_star"][outcome] = {}
        for family in FAMILIES:
            freeze["winners"][outcome][family] = {
                "max_depth": config.winners[outcome][family].max_depth,
                "learning_rate": config.winners[outcome][family].learning_rate,
            }
            profits = []
            per_seed: dict[str, Any] = {}
            for seed in config.seeds:
                scores = validation_scores[(outcome, family, seed)]
                sample = rank_sample(scores, validation.treatment, y)
                profit = conditional_profit(sample, fractions, ratios)
                profits.append(profit)
                per_seed[str(seed)] = {
                    "rounds": fitted[(outcome, family, seed)].rounds,
                    **point_metrics(sample, scores, validation.treatment, y, family_configs[family].uplift_k),
                }
            freeze["validation"][outcome][family] = per_seed
            mean_profit = np.mean(np.stack(profits), axis=0)
            freeze["k_star"][outcome][family] = {
                f"{ratio:g}": best_fraction(mean_profit[index], fractions) for index, ratio in enumerate(ratios)
            }
    return freeze


def point_metrics(
    sample: Any,
    scores: np.ndarray,
    treatment: np.ndarray,
    outcome: np.ndarray,
    uplift_k: tuple[float, ...],
    weights: np.ndarray | None = None,
) -> dict[str, float]:
    values = {"qini_coefficient": qini_coefficient(sample, weights=weights), "auuc": auuc(sample, weights=weights)}
    for k in uplift_k:
        values[f"uplift_at_{k:.2f}"] = uplift_at_k(sample, k, weights)
    values["separate_relative_auuc"] = separate_relative_auuc(scores, treatment, outcome, weights)
    return values


def test_phase(
    config: FinalConfig,
    family_configs: dict[str, FamilyConfig],
    fitted: dict[tuple[str, str, int], FittedWinner],
    test_bundle: DataBundle,
    freeze: dict[str, Any],
    test_scores: dict[tuple[str, str, int], np.ndarray],
) -> dict[str, Any]:
    """Metrics on the test split for scores already computed from the frozen models."""
    test = test_bundle["test"]
    ratios = np.asarray(config.cost_to_value_ratios)
    fractions = uniform_grid(config.profit_grid_points)
    results: dict[str, Any] = {
        "test_rows": test.rows,
        "test_treated_rows": int(np.sum(test.treatment == 1)),
        "test_control_rows": int(np.sum(test.treatment == 0)),
        "point": {},
        "seed_summary": {},
        "intervals": {},
        "sample_size": {},
        "profit": {},
        "qini_curves": {},
        "profit_curves": {},
    }
    seed0 = config.seeds[0]
    for outcome in config.winners:
        y = test.outcome(outcome)
        results["point"][outcome] = {}
        results["seed_summary"][outcome] = {}
        for family in FAMILIES:
            k_stars = freeze["k_star"][outcome][family]
            per_seed: dict[str, Any] = {}
            for seed in config.seeds:
                scores = test_scores[(outcome, family, seed)]
                sample = rank_sample(scores, test.treatment, y)
                entry = point_metrics(sample, scores, test.treatment, y, family_configs[family].uplift_k)
                entry["profit_at_k_star"] = {
                    f"{ratio:g}": float(
                        conditional_profit(sample, np.array([k_stars[f"{ratio:g}"]]), ratios)[index, 0]
                    )
                    for index, ratio in enumerate(ratios)
                }
                per_seed[str(seed)] = entry
            results["point"][outcome][family] = per_seed
            results["seed_summary"][outcome][family] = _seed_summary(per_seed)

        seed0_scores = {family: test_scores[(outcome, family, seed0)] for family in FAMILIES}
        results["intervals"][outcome] = _intervals(
            seed0_scores,
            test.treatment,
            y,
            family_configs,
            freeze["k_star"][outcome],
            ratios,
            config.bootstrap_replicates,
            config.bootstrap_seed,
        )
        results["sample_size"][outcome] = _sample_size_curves(config, seed0_scores, test.treatment, y, family_configs, ratios)
        results["profit"][outcome] = {}
        results["qini_curves"][outcome] = {}
        results["profit_curves"][outcome] = {}
        for family in FAMILIES:
            scores = seed0_scores[family]
            sample = rank_sample(scores, test.treatment, y)
            curve = qini_curve(sample, fractions)
            total = qini_curve(sample, np.array([1.0]))[0]
            results["qini_curves"][outcome][family] = {
                "fraction": [float(value) for value in fractions],
                "qini": [float(value) for value in np.nan_to_num(curve)],
                "random": [float(value) for value in fractions * total],
            }
            profit = conditional_profit(sample, fractions, ratios)
            results["profit_curves"][outcome][family] = {
                f"{ratio:g}": [float(value) for value in profit[index]] for index, ratio in enumerate(ratios)
            }
            results["profit"][outcome][family] = {
                f"{ratio:g}": {
                    "k_star": k_stars_value(freeze, outcome, family, ratio),
                    "point": results["point"][outcome][family][str(seed0)]["profit_at_k_star"][f"{ratio:g}"],
                }
                for ratio in ratios
            }
    return results


def k_stars_value(freeze: dict[str, Any], outcome: str, family: str, ratio: float) -> float:
    return float(freeze["k_star"][outcome][family][f"{ratio:g}"])


def _seed_summary(per_seed: dict[str, Any]) -> dict[str, Any]:
    """Mean, min and max over seeds for every scalar metric (profit entries are nested and skipped)."""
    first = next(iter(per_seed.values()))
    names = [name for name, value in first.items() if not isinstance(value, dict)]
    summary: dict[str, Any] = {}
    for name in names:
        values = np.array([entry[name] for entry in per_seed.values()], dtype=float)
        summary[name] = {"mean": float(values.mean()), "min": float(values.min()), "max": float(values.max())}
    return summary


def _intervals(
    scores: dict[str, np.ndarray],
    treatment: np.ndarray,
    outcome: np.ndarray,
    family_configs: dict[str, FamilyConfig],
    k_stars: dict[str, dict[str, float]],
    ratios: np.ndarray,
    replicates: int,
    seed: int,
) -> dict[str, Any]:
    """Paired Poisson bootstrap: every family and every family-minus-baseline difference uses the same weights."""
    uplift_k = family_configs[BASELINE].uplift_k
    samples = {family: rank_sample(scores[family], treatment, outcome) for family in FAMILIES}
    keys = [f"{ratio:g}" for ratio in ratios]

    def per_family(weights: np.ndarray | None) -> dict[str, dict[str, float]]:
        out: dict[str, dict[str, float]] = {}
        for family in FAMILIES:
            values = point_metrics(samples[family], scores[family], treatment, outcome, uplift_k, weights)
            for index, key in enumerate(keys):
                profit = conditional_profit(samples[family], np.array([k_stars[family][key]]), ratios, weights)
                values[f"profit_{key}"] = float(profit[index, 0])
            out[family] = values
        return out

    def statistic(weights: np.ndarray | None) -> dict[str, float]:
        return flatten_with_differences(per_family(weights))

    draws = bootstrap(lambda weights: statistic(weights), treatment.shape[0], replicates, seed)
    summary = summarize(draws, statistic(None))
    for name in summary:
        summary[name]["share_positive"] = float(np.mean(draws[name] > 0))
    return summary


def flatten_with_differences(per_family: dict[str, dict[str, float]]) -> dict[str, float]:
    """Flat statistic names: ``family.metric`` and ``family_minus_baseline.metric`` for the non-baseline families."""
    flat: dict[str, float] = {}
    for family, values in per_family.items():
        for name, value in values.items():
            flat[f"{family}.{name}"] = value
    for family in FAMILIES:
        if family == BASELINE:
            continue
        for name in per_family[family]:
            flat[f"{family}_minus_{BASELINE}.{name}"] = per_family[family][name] - per_family[BASELINE][name]
    return flat


def _sample_size_curves(
    config: FinalConfig,
    scores: dict[str, np.ndarray],
    treatment: np.ndarray,
    outcome: np.ndarray,
    family_configs: dict[str, FamilyConfig],
    ratios: np.ndarray,
) -> dict[str, Any]:
    """Qini and AUUC with paired intervals on test subsamples of each configured size (same rows for every model)."""
    rng = np.random.default_rng(config.sample_seed)
    curves: dict[str, Any] = {}
    for size in config.sample_sizes:
        label = MISSING_SIZE if size is None else str(size)
        if size is None or size >= treatment.shape[0]:
            rows = np.arange(treatment.shape[0])
        else:
            rows = np.sort(rng.choice(treatment.shape[0], size=size, replace=False))
        sub_scores = {family: scores[family][rows] for family in FAMILIES}
        sub_t = treatment[rows]
        sub_y = outcome[rows]
        samples = {family: rank_sample(sub_scores[family], sub_t, sub_y) for family in FAMILIES}

        def statistic(weights: np.ndarray, samples=samples) -> dict[str, float]:
            values = {}
            for family in FAMILIES:
                values[f"{family}.qini_coefficient"] = qini_coefficient(samples[family], weights=weights)
                values[f"{family}.auuc"] = auuc(samples[family], weights=weights)
            for family in FAMILIES:
                if family == BASELINE:
                    continue
                for metric in ("qini_coefficient", "auuc"):
                    values[f"{family}_minus_{BASELINE}.{metric}"] = (
                        values[f"{family}.{metric}"] - values[f"{BASELINE}.{metric}"]
                    )
            return values

        point = statistic(np.ones(sub_t.shape[0]))
        draws = bootstrap(statistic, sub_t.shape[0], config.bootstrap_replicates, config.bootstrap_seed)
        curves[label] = {
            "rows": int(sub_t.shape[0]),
            "summary": summarize(draws, point),
        }
    return curves


def run_test_phase_from_freeze(
    config: FinalConfig,
    family_configs: dict[str, FamilyConfig],
    fitted: dict[tuple[str, str, int], FittedWinner],
    freeze: dict[str, Any],
    test_bundle: DataBundle,
) -> dict[str, Any]:
    test = test_bundle["test"]
    scores: dict[tuple[str, str, int], np.ndarray] = {}
    for key, winner in fitted.items():
        scores[key] = winner.learner.score(test.features)
    return test_phase(config, family_configs, fitted, test_bundle, freeze, scores)


def validation_scores_for(
    fitted: dict[tuple[str, str, int], FittedWinner], validation_features: np.ndarray
) -> dict[tuple[str, str, int], np.ndarray]:
    return {key: winner.learner.score(validation_features) for key, winner in fitted.items()}


def read_access(final_root: Path) -> dict[str, Any]:
    path = final_root / ACCESS_FILE
    if not path.exists():
        return {"schema_version": SCHEMA_VERSION, "attempts": []}
    return json.loads(path.read_text(encoding="utf-8"))


def _write_access(final_root: Path, data: dict[str, Any]) -> None:
    data["openings"] = sum(1 for attempt in data["attempts"] if attempt.get("opened_test_split"))
    write_json(final_root / ACCESS_FILE, data)


def record_attempt(final_root: Path, entry: dict[str, Any]) -> int:
    """Append one attempt to the access record. Call before the test split is read. Returns the attempt number."""
    data = read_access(final_root)
    number = len(data["attempts"]) + 1
    data["attempts"].append({"attempt": number, **entry})
    _write_access(final_root, data)
    return number


def update_attempt(final_root: Path, number: int, fields: dict[str, Any]) -> None:
    data = read_access(final_root)
    for attempt in data["attempts"]:
        if attempt["attempt"] == number:
            attempt.update(fields)
    _write_access(final_root, data)


def load_saved_winners(
    config: FinalConfig, freeze: dict[str, Any]
) -> dict[tuple[str, str, int], FittedWinner]:
    """Rebuild every fitted winner from the saved boosters, with the tree counts recorded in the freeze file.

    Loads models only; no test rows are read here. Test scoring runs on CPU for determinism.
    """
    fitted: dict[tuple[str, str, int], FittedWinner] = {}
    for outcome in config.winners:
        for family in FAMILIES:
            cell = config.winners[outcome][family]
            for seed in config.seeds:
                rounds = freeze["validation"][outcome][family][str(seed)]["rounds"]
                learner = LEARNERS[family]()
                directory = config.final_root / "models" / outcome / family / f"seed-{seed}"
                for name, count in rounds.items():
                    booster = xgb.Booster()
                    booster.load_model(str(directory / f"{name}.ubj"))
                    booster.set_param({"device": "cpu"})
                    learner.models[name] = FittedBooster(booster=booster, rounds=int(count))
                fitted[(outcome, family, seed)] = FittedWinner(outcome, family, seed, cell, learner, rounds)
    return fitted


def score_chunked(learner: Any, features: np.ndarray, chunk_rows: int = SCORE_CHUNK_ROWS) -> np.ndarray:
    """Score in row chunks so the peak memory of one prediction stays bounded."""
    parts = [learner.score(features[start : start + chunk_rows]) for start in range(0, features.shape[0], chunk_rows)]
    return np.concatenate(parts)


def access_log_exists(final_root: Path) -> bool:
    return (final_root / ACCESS_FILE).exists()


def freeze_bytes(freeze: dict[str, Any]) -> bytes:
    return (json.dumps(freeze, indent=2, sort_keys=True) + "\n").encode("utf-8")


def provenance(config: FinalConfig) -> dict[str, Any]:
    return {"config_sha256": config.config_sha256, "git": git_state(), "environment": environment()}



MODEL_CHECKSUMS_FILE: Final = "model_checksums.json"
# Relative tolerance for the integrity check. The freeze values were computed from GPU boosters and the check runs the
# same boosters on CPU. On the validation split the largest difference over all frozen metrics was 1.45e-5 (first
# check, tolerance 1e-6, which failed on that noise alone). 1e-4 is about seven times that noise and far below the
# change a different booster causes (see the unit test that replaces a booster).
INTEGRITY_REL_TOL: Final = 1e-4


def metric_mismatches(
    recomputed: dict[str, float], frozen: dict[str, float], rel_tol: float = INTEGRITY_REL_TOL
) -> list[str]:
    """Names of the frozen metrics that the recomputed values do not reproduce within ``rel_tol``."""
    problems = []
    for name, expected in frozen.items():
        if name == "rounds":
            continue
        if name not in recomputed:
            problems.append(f"{name}: missing")
            continue
        value = recomputed[name]
        scale = max(abs(expected), 1.0)
        if not abs(value - expected) <= rel_tol * scale:
            problems.append(f"{name}: recomputed {value!r} vs frozen {expected!r}")
    return problems


def verify_saved_models(
    config: FinalConfig,
    freeze: dict[str, Any],
    freeze_sha256: str,
    family_configs: dict[str, FamilyConfig],
    validation: SplitArrays,
) -> dict[str, Any]:
    """Recompute every validation metric of every saved winner and compare it with the freeze file.

    Uses validation rows only. Writes the sha256 of every booster file to ``model_checksums.json`` and returns the
    result. Any mismatch is reported; the caller refuses to open the test split.
    """
    fitted = load_saved_winners(config, freeze)
    checksums: dict[str, str] = {}
    mismatches: dict[str, list[str]] = {}
    largest = 0.0
    for (outcome, family, seed), winner in sorted(fitted.items()):
        scores = score_chunked(winner.learner, validation.features)
        y = validation.outcome(outcome)
        sample = rank_sample(scores, validation.treatment, y)
        recomputed = point_metrics(sample, scores, validation.treatment, y, family_configs[family].uplift_k)
        frozen = freeze["validation"][outcome][family][str(seed)]
        key = f"{outcome}/{family}/seed-{seed}"
        problems = metric_mismatches(recomputed, frozen)
        if problems:
            mismatches[key] = problems
        for name, expected in frozen.items():
            if name != "rounds":
                largest = max(largest, abs(recomputed[name] - expected) / max(abs(expected), 1.0))
        directory = config.final_root / "models" / outcome / family / f"seed-{seed}"
        for path in sorted(directory.glob("*.ubj")):
            checksums[path.relative_to(config.final_root).as_posix()] = sha256_file(path)
    result = {
        "schema_version": SCHEMA_VERSION,
        "freeze_sha256": freeze_sha256,
        "rel_tol": INTEGRITY_REL_TOL,
        "largest_relative_difference": largest,
        "models": len(fitted),
        "passed": not mismatches,
        "mismatches": mismatches,
        "model_files": checksums,
    }
    write_json(config.final_root / MODEL_CHECKSUMS_FILE, result)
    return result
