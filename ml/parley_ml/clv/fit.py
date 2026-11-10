"""Fit BG/NBD and Gamma-Gamma (MAP) on the calibration period of the clean Online Retail II data.

Run from ml/:
    uv run --directory ml python -m parley_ml.clv.fit --config configs/clv_fit.yaml

Outputs (git-ignored) in ``<output_dir>/clv_fit-<config sha256[:12]>/``:
    calibration_summary.parquet, holdout_outcomes.parquet, fit.json, config.yaml

``fit.json`` holds no timestamps or durations, so a re-run on the same input gives identical bytes.
The elapsed time is printed to stdout and compared with ``budget_seconds``.
"""

from __future__ import annotations

import argparse
import importlib.metadata
import json
import shutil
import sys
import time
import warnings
from dataclasses import asdict
from datetime import date
from pathlib import Path
from typing import Any, Final

import numpy as np
import pandas as pd
import pymc as pm
import pymc_marketing.clv as clv

from parley_ml.clv.config import RUN_HASH_CHARS, FitConfig, load_fit_config
from parley_ml.clv.split import daily_purchase_days, holdout_outcomes, split_daily
from parley_ml.datasets.checksums import sha256_file, write_json

_ML_ROOT = Path(__file__).resolve().parents[2]
TRANSACTIONS_FILE: Final = "transactions.parquet"
INPUT_MANIFEST_FILE: Final = "manifest.json"
CALIBRATION_FILE: Final = "calibration_summary.parquet"
HOLDOUT_FILE: Final = "holdout_outcomes.parquet"
FIT_FILE: Final = "fit.json"
CONFIG_COPY_FILE: Final = "config.yaml"
RUN_PREFIX: Final = "clv_fit"
BG_PARAMETERS: Final[tuple[str, ...]] = ("alpha", "r", "a", "b", "phi_dropout", "kappa_dropout")
GG_PARAMETERS: Final[tuple[str, ...]] = ("p", "q", "v")
TRANSACTION_COLUMNS: Final[tuple[str, ...]] = ("customer_id", "invoice_ts", "line_amount", "is_cancellation")
# PyMC-Marketing warns about T=0 customers. They are kept in the fit and counted in fit.json.
_T_ZERO_WARNING: Final = "T=0 is uninformative"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Fit BG/NBD and Gamma-Gamma (MAP) on the calibration period.")
    parser.add_argument("--config", required=True, type=Path, help="Path to clv_fit.yaml")
    args = parser.parse_args(argv)
    started = time.perf_counter()
    try:
        config = load_fit_config(args.config, root=_ML_ROOT)
        result = run(config, args.config)
    except (FileNotFoundError, ValueError) as error:
        print(f"clv_fit: {error}", file=sys.stderr)
        return 1
    elapsed = time.perf_counter() - started
    print(json.dumps(result["summary"], indent=2))
    print(f"elapsed_seconds: {elapsed:.1f} (budget {config.budget_seconds:.0f})")
    if elapsed > config.budget_seconds:
        print("clv_fit: run exceeded budget_seconds", file=sys.stderr)
        return 1
    if not result["summary"]["all_checks_passed"]:
        print("clv_fit: a convergence or sanity check failed, see fit.json", file=sys.stderr)
        return 1
    return 0


def run(config: FitConfig, config_path: Path) -> dict[str, Any]:
    input_manifest = _verify_input(config.input_dir)
    transactions = pd.read_parquet(config.input_dir / TRANSACTIONS_FILE, columns=list(TRANSACTION_COLUMNS))
    if len(transactions) != input_manifest["clean"]["transaction_lines"]:
        raise ValueError("Transaction row count differs from the input manifest")

    daily, daily_report = daily_purchase_days(transactions)
    split = split_daily(daily, config.calibration_end, config.holdout_end)
    outcomes = holdout_outcomes(split)

    calibration_summary = _calibration_summary(split.calibration, config.calibration_end)
    t_zero_customers = int((calibration_summary["T"] == 0).sum())

    bg_model = clv.BetaGeoModel()
    bg_result = _fit_map(bg_model, calibration_summary, BG_PARAMETERS, config)

    gg_input = _gamma_gamma_input(calibration_summary, config)
    gg_model = clv.GammaGammaModel()
    gg_result = _fit_map(gg_model, gg_input, GG_PARAMETERS, config)

    correlation = _frequency_monetary_correlation(gg_input)
    checks_passed = bg_result["checks"]["passed"] and gg_result["checks"]["passed"]

    run_id = f"{RUN_PREFIX}-{config.config_sha256[:RUN_HASH_CHARS]}"
    run_dir = config.output_dir / run_id
    run_dir.mkdir(parents=True, exist_ok=True)
    outputs = {
        CALIBRATION_FILE: _write_frame(calibration_summary, run_dir / CALIBRATION_FILE),
        HOLDOUT_FILE: _write_frame(outcomes, run_dir / HOLDOUT_FILE),
    }
    shutil.copyfile(config_path, run_dir / CONFIG_COPY_FILE)

    fit = {
        "schema_version": 1,
        "run_id": run_id,
        "config_sha256": config.config_sha256,
        "dataset": {"name": config.dataset_name, "transactions_sha256": input_manifest["outputs"][TRANSACTIONS_FILE]["sha256"]},
        "counts": {
            "raw_rows": input_manifest["identity"]["raw_rows"],
            "removed_by_cleaning": input_manifest["cleaning"]["removed"],
            "clean_lines": input_manifest["clean"]["transaction_lines"],
            "clean_lines_read": len(transactions),
            "daily": asdict(daily_report),
            "split": _split_report(split.report),
            "calibration_customers_with_t_zero": t_zero_customers,
            "gamma_gamma_customers": len(gg_input),
            "gamma_gamma_customers_excluded": int(len(calibration_summary[calibration_summary["frequency"] >= 1]) - len(gg_input)),
        },
        "priors": {"bg_nbd": _prior_strings(bg_model), "gamma_gamma": _prior_strings(gg_model)},
        "bg_nbd": bg_result,
        "gamma_gamma": gg_result,
        "frequency_monetary_correlation": correlation,
        "libraries": _library_versions(),
        "outputs": outputs,
    }
    write_json(run_dir / FIT_FILE, fit)

    summary = {
        "run_id": run_id,
        "calibration_end": config.calibration_end.isoformat(),
        "holdout_end": config.holdout_end.isoformat(),
        "bg_nbd_parameters": bg_result["parameters"],
        "gamma_gamma_parameters": gg_result["parameters"],
        "frequency_monetary_correlation": correlation,
        "all_checks_passed": checks_passed,
        "output_dir": str(run_dir),
    }
    return {"summary": summary, "fit": fit}


def _verify_input(input_dir: Path) -> dict[str, Any]:
    manifest_path = input_dir / INPUT_MANIFEST_FILE
    transactions_path = input_dir / TRANSACTIONS_FILE
    if not manifest_path.exists() or not transactions_path.exists():
        raise FileNotFoundError(
            f"Clean data missing in {input_dir}. Run parley_ml.datasets.online_retail first (see the data card)."
        )
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    expected = manifest["outputs"][TRANSACTIONS_FILE]["sha256"]
    if sha256_file(transactions_path) != expected:
        raise ValueError(f"{transactions_path} differs from its checksum in {INPUT_MANIFEST_FILE}")
    return manifest


def _calibration_summary(calibration: pd.DataFrame, calibration_end: date) -> pd.DataFrame:
    rows = calibration.rename(columns={"day": "datetime", "net_amount": "monetary_value"})
    return clv.rfm_summary(
        rows,
        customer_id_col="customer_id",
        datetime_col="datetime",
        monetary_value_col="monetary_value",
        observation_period_end=pd.Timestamp(calibration_end),
        time_unit="D",
    )


def _gamma_gamma_input(summary: pd.DataFrame, config: FitConfig) -> pd.DataFrame:
    gg = config.gamma_gamma
    mask = (summary["frequency"] >= gg.min_frequency) & (summary["monetary_value"] > gg.min_monetary_value)
    return summary.loc[mask, ["customer_id", "frequency", "monetary_value"]].reset_index(drop=True)


def _fit_map(model: Any, data: pd.DataFrame, names: tuple[str, ...], config: FitConfig) -> dict[str, Any]:
    """Fit with the library's MAP method, then check the optimum with solves from other starts.

    The library call gives the parameters. ``pm.find_MAP(return_raw=True)`` on the same model gives the scipy
    result (success, gradient at the optimum). Restarts from scaled library estimates must reach the same optimum.
    """
    with warnings.catch_warnings():
        warnings.filterwarnings("ignore", message=_T_ZERO_WARNING)
        model.fit(data=data, method="map")
    parameters = {name: float(model.fit_result[name].values.item()) for name in names}

    point, optimizer = pm.find_MAP(model=model.model, return_raw=True, progressbar=False)
    value_names = [name for name in names if name in point]
    log_posterior = -float(optimizer.fun)
    relative_gradient = float(np.linalg.norm(optimizer.jac)) / max(abs(log_posterior), 1.0)

    restarts = []
    for scale in config.checks.restart_scales:
        start = {name: parameters[name] * scale for name in value_names}
        restart_point, restart_optimizer = pm.find_MAP(
            model=model.model, return_raw=True, progressbar=False, start=start
        )
        restart_log_posterior = -float(restart_optimizer.fun)
        restarts.append(
            {
                "start_scale": scale,
                "success": bool(restart_optimizer.success),
                "log_posterior": restart_log_posterior,
                "relative_objective_gap": abs(restart_log_posterior - log_posterior) / max(abs(log_posterior), 1.0),
                "max_relative_parameter_difference": _max_relative_difference(restart_point, parameters, value_names),
            }
        )

    objective_gap = max([0.0, *(entry["relative_objective_gap"] for entry in restarts)])
    sanity = _parameter_sanity(parameters)
    checks = {
        "optimizer_success": bool(optimizer.success) or not config.checks.require_optimizer_success,
        "restarts_success": all(entry["success"] for entry in restarts) or not config.checks.require_optimizer_success,
        "relative_gradient_within_tolerance": relative_gradient <= config.checks.max_relative_gradient,
        "restarts_reach_same_objective": objective_gap <= config.checks.max_relative_objective_gap,
        "parameters_sane": sanity["all_positive_finite"] and sanity["phi_in_unit_interval"],
    }
    checks["passed"] = all(checks.values())
    return {
        "parameters": parameters,
        "derived": sanity["derived"],
        "optimizer": {
            "success": bool(optimizer.success),
            "message": str(optimizer.message),
            "iterations": int(optimizer.nit),
            "log_posterior": log_posterior,
            "gradient_norm": float(np.linalg.norm(optimizer.jac)),
            "relative_gradient": relative_gradient,
        },
        "restarts": restarts,
        "max_relative_objective_gap": float(objective_gap),
        "checks": checks,
    }


def _max_relative_difference(solved: Any, parameters: dict[str, float], names: list[str]) -> float:
    return max(
        abs(float(np.asarray(solved[name]).item()) - parameters[name]) / max(abs(parameters[name]), 1e-12)
        for name in names
    )


def _parameter_sanity(parameters: dict[str, float]) -> dict[str, Any]:
    values = np.array(list(parameters.values()), dtype=float)
    all_positive_finite = bool(np.all(np.isfinite(values)) and np.all(values > 0))
    phi = parameters.get("phi_dropout", 0.5)
    derived: dict[str, float] = {}
    if "r" in parameters and "alpha" in parameters:
        derived["purchase_rate_per_day"] = parameters["r"] / parameters["alpha"]
    return {
        "all_positive_finite": all_positive_finite,
        "phi_in_unit_interval": bool(0 < phi < 1),
        "derived": derived,
    }


def _frequency_monetary_correlation(gg_input: pd.DataFrame) -> dict[str, Any]:
    pearson = gg_input["frequency"].corr(gg_input["monetary_value"], method="pearson")
    spearman = gg_input["frequency"].corr(gg_input["monetary_value"], method="spearman")
    return {
        "customers": int(len(gg_input)),
        "pearson": float(pearson),
        "spearman": float(spearman),
    }


def _prior_strings(model: Any) -> dict[str, str]:
    return {name: str(prior) for name, prior in model.model_config.items()}


def _split_report(report: Any) -> dict[str, Any]:
    payload = asdict(report)
    payload["calibration_end"] = report.calibration_end.isoformat()
    payload["holdout_end"] = report.holdout_end.isoformat()
    return payload


def _library_versions() -> dict[str, str]:
    return {
        "pymc_marketing": importlib.metadata.version("pymc-marketing"),
        "pymc": importlib.metadata.version("pymc"),
        "pandas": pd.__version__,
        "numpy": np.__version__,
    }


def _write_frame(frame: pd.DataFrame, path: Path) -> dict[str, Any]:
    frame.to_parquet(path, index=False, compression="zstd")
    return {"rows": len(frame), "sha256": sha256_file(path)}


if __name__ == "__main__":
    raise SystemExit(main())
