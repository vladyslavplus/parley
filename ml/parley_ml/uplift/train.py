"""Train one uplift family on one outcome and one grid cell, on the train split, and score the validation split.

Full run (one cell):
    uv run --directory ml python -m parley_ml.uplift.train --config configs/uplift_t.yaml --outcome visit \
        --max-depth 4 --learning-rate 0.05

Smoke run (small subsample, 30 rounds, cross-checked against causalml with fixed rounds, writes under artifacts/smoke):
    uv run --directory ml python -m parley_ml.uplift.train --config configs/uplift_t.yaml --smoke

The grid over all families and cells is ``parley_ml.uplift.train_grid``.
"""

from __future__ import annotations

import argparse
import json
import shutil
import sys
import time
from dataclasses import replace
from pathlib import Path
from typing import Any, Final

import numpy as np

from parley_ml.datasets.checksums import write_json
from parley_ml.uplift.config import ML_ROOT, Cell, FamilyConfig, load_family_config
from parley_ml.uplift.data import DataBundle, load_bundle, require_fit_splits, describe_bundle
from parley_ml.uplift.learners import FAMILY_RESPONSE, LEARNERS, BoosterSettings, causalml_reference
from parley_ml.uplift.runs import (
    SCHEMA_VERSION,
    GpuMemorySampler,
    checksums_payload,
    environment,
    finished_metrics,
    git_state,
    gpu_memory_mib,
    run_config_sha256,
    run_id,
    validation_metrics,
    write_metrics,
    write_run_config,
)

SMOKE_ROWS: Final = 200_000
SMOKE_ROUNDS: Final = 30
SMOKE_EARLY_STOPPING: Final = 10
SMOKE_CELL: Final = Cell(max_depth=4, learning_rate=0.1)
SMOKE_OUTPUT: Final = ML_ROOT / "artifacts" / "smoke"
RUNS_ROOT_DEFAULT: Final = ML_ROOT / "artifacts" / "runs"


def booster_params(config: FamilyConfig, cell: Cell) -> dict[str, Any]:
    """Native XGBoost parameters for one cell. Everything comes from the family config except depth and eta."""
    xgboost = dict(config.xgboost)
    return {
        "objective": xgboost["objective"],
        "eval_metric": xgboost["eval_metric"],
        "tree_method": xgboost["tree_method"],
        "device": xgboost["device"],
        "max_bin": int(xgboost["max_bin"]),
        "max_depth": cell.max_depth,
        "eta": cell.learning_rate,
        "seed": config.seed,
    }


def run_cell(
    config: FamilyConfig,
    bundle: DataBundle,
    outcome: str,
    cell: Cell,
    runs_root: Path,
) -> tuple[dict[str, Any], bool]:
    """Fit one run, or return the finished metrics of the same run. Returns (metrics, reused)."""
    if outcome not in config.outcomes:
        raise ValueError(f"{config.path.name} does not list outcome '{outcome}'")
    require_fit_splits(tuple(bundle.splits))

    config_sha = run_config_sha256(config.config_sha256, outcome, cell.max_depth, cell.learning_rate)
    rid = run_id(config.family, outcome, config_sha)
    run_dir = runs_root / rid
    finished = finished_metrics(run_dir, config_sha)
    if finished is not None:
        return finished, True
    if run_dir.exists():
        shutil.rmtree(run_dir)
    run_dir.mkdir(parents=True)

    params = booster_params(config, cell)
    settings = BoosterSettings(
        params=params,
        early_stopping_rounds=config.early_stopping_rounds,
        max_rounds=config.max_rounds,
    )
    write_run_config(
        run_dir,
        {
            "schema_version": SCHEMA_VERSION,
            "run_id": rid,
            "family": config.family,
            "outcome": outcome,
            "max_depth": cell.max_depth,
            "learning_rate": cell.learning_rate,
            "config_sha256": config_sha,
            "family_config": str(config.path.relative_to(ML_ROOT)).replace("\\", "/"),
            "family_config_sha256": config.config_sha256,
            "xgboost": params,
            "early_stopping_rounds": config.early_stopping_rounds,
            "max_rounds": config.max_rounds,
            "seed": config.seed,
            "features": list(config.features),
            "treatment": config.treatment,
            "splits": describe_bundle(bundle),
            "split_checksums": checksums_payload(bundle.checksums),
            "subsample": bundle.subsample,
        },
    )

    train, validation = bundle["train"], bundle["validation"]
    learner = LEARNERS[config.family]()
    gpu_before = gpu_memory_mib()
    with GpuMemorySampler() as gpu:
        started = time.perf_counter()
        rounds = learner.fit(train, validation, outcome, settings)
        fit_seconds = time.perf_counter() - started
        started = time.perf_counter()
        scores = learner.score(validation.features)
        score_seconds = time.perf_counter() - started
        gpu_peak = gpu.peak_mib

    metrics = validation_metrics(scores, validation.treatment, validation.outcome(outcome), config.uplift_k)
    for name, model in learner.models.items():
        model.booster.save_model(run_dir / f"{name}.ubj")

    payload: dict[str, Any] = {
        "schema_version": SCHEMA_VERSION,
        "run_id": rid,
        "family": config.family,
        "outcome": outcome,
        "max_depth": cell.max_depth,
        "learning_rate": cell.learning_rate,
        "config_sha256": config_sha,
        "split": "validation",
        "validation": metrics,
        "training": {
            "rounds_kept": rounds,
            "max_rounds": config.max_rounds,
            "early_stopping_rounds": config.early_stopping_rounds,
            "fit_seconds": round(fit_seconds, 2),
            "score_seconds": round(score_seconds, 2),
            "wall_seconds": round(fit_seconds + score_seconds, 2),
            "gpu_memory_before_mib": gpu_before,
            "gpu_memory_peak_mib": gpu_peak,
            "gpu_memory_note": "device-wide memory.used from nvidia-smi, includes other processes",
            "device": params["device"],
        },
        "environment": environment(),
        "git": git_state(),
    }
    write_metrics(run_dir, payload)
    return payload, False


def smoke(config_path: Path) -> dict[str, Any]:
    """Pipeline check on a small subsample: fit, score, metrics, and a causalml comparison at fixed rounds."""
    config = load_family_config(config_path)
    smoke_config = replace(config, early_stopping_rounds=SMOKE_EARLY_STOPPING, max_rounds=SMOKE_ROUNDS)
    bundle = load_bundle(smoke_config, subsample=SMOKE_ROWS)
    outcome = config.outcomes[0]
    result, _ = run_cell(smoke_config, bundle, outcome, SMOKE_CELL, SMOKE_OUTPUT / "runs")

    summary: dict[str, Any] = {
        "family": config.family,
        "outcome": outcome,
        "run_id": result["run_id"],
        "rounds_kept": result["training"]["rounds_kept"],
        "validation_qini_coefficient": result["validation"]["qini_coefficient"],
        "wall_seconds": result["training"]["wall_seconds"],
        "device": result["training"]["device"],
    }
    if config.family != FAMILY_RESPONSE:
        # causalml with device=cuda exits with status 127 at interpreter teardown on this machine, so the
        # cross-check runs both sides on CPU. The GPU fit above is the one that is timed.
        params = {**booster_params(smoke_config, SMOKE_CELL), "device": "cpu"}
        ours = LEARNERS[config.family]()
        fixed = BoosterSettings(params=params, early_stopping_rounds=None, max_rounds=SMOKE_ROUNDS)
        ours.fit(bundle["train"], bundle["validation"], outcome, fixed)
        our_scores = ours.score(bundle["validation"].features)
        reference = causalml_reference(
            config.family,
            bundle["train"],
            outcome,
            bundle["validation"].features,
            params,
            rounds=SMOKE_ROUNDS,
        )
        difference = float(np.max(np.abs(our_scores - reference)))
        summary["causalml_rounds"] = SMOKE_ROUNDS
        summary["causalml_device"] = "cpu"
        summary["causalml_max_abs_difference"] = difference
        summary["causalml_agrees"] = bool(np.allclose(our_scores, reference, rtol=1e-5, atol=1e-6))
    write_json(SMOKE_OUTPUT / f"smoke_{config.family}.json", {"schema_version": SCHEMA_VERSION, **summary})
    return summary


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Train one uplift family on one grid cell.")
    parser.add_argument("--config", required=True, type=Path, help="configs/uplift_<family>.yaml")
    parser.add_argument("--smoke", action="store_true", help="small pipeline check with a causalml comparison")
    parser.add_argument("--outcome", choices=("visit", "conversion"), default=None)
    parser.add_argument("--max-depth", type=int, default=None)
    parser.add_argument("--learning-rate", type=float, default=None)
    parser.add_argument("--runs-root", type=Path, default=RUNS_ROOT_DEFAULT)
    args = parser.parse_args(argv)
    config_path = args.config if args.config.is_absolute() else ML_ROOT / args.config

    if args.smoke:
        print(json.dumps(smoke(config_path), indent=2))
        return 0
    if args.max_depth is None or args.learning_rate is None:
        parser.error("a full run needs --max-depth and --learning-rate; use train_grid for the whole grid")
    config = load_family_config(config_path)
    outcome = args.outcome or config.outcomes[0]
    bundle = load_bundle(config)
    metrics, reused = run_cell(config, bundle, outcome, Cell(args.max_depth, args.learning_rate), args.runs_root)
    print(json.dumps({"reused": reused, "run_id": metrics["run_id"], "validation": metrics["validation"]}, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
