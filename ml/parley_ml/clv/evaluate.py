"""Evaluate BG/NBD + Gamma-Gamma on the holdout against RFM and last-period spend (RQ2), and write the artifact.

Run from ml/:
    uv run --directory ml python -m parley_ml.clv.evaluate --config configs/clv_eval.yaml

Reads the fit run written by ``parley_ml.clv.fit`` (dates, MAP parameters, checksums) and the clean data.

Population: customers with calibration purchases. Customers first seen in the holdout are excluded and reported.
Targets: holdout purchase days (count) and holdout net revenue (all lines in the holdout window, cancellations
included). Predictions: expected purchase days over the holdout length, times expected spend per purchase.
Uncertainty: customer bootstrap, paired differences against each baseline.

Outputs:
- ``results/clv/`` (committed): ``metrics.csv``, ``calibration_by_frequency.{csv,png}``, ``cumulative_gains.{csv,png}``.
- ``artifacts/runs/clv_eval-<config hash>/evaluation.json`` (git-ignored, no timestamps).
- ``artifacts/models/clv_bgnbd_gg-<yyyymmdd>-<hash>/`` (git-ignored): ``parameters.json``, ``manifest.json``.
"""

from __future__ import annotations

import argparse
import importlib.metadata
import json
import sys
import time
from dataclasses import dataclass
from datetime import date
from pathlib import Path
from typing import Any, Final

import numpy as np
import pandas as pd
import yaml
from matplotlib.backends.backend_agg import FigureCanvasAgg
from matplotlib.figure import Figure

from parley_ml.clv.artifact import write_artifact
from parley_ml.clv.config import RUN_HASH_CHARS
from parley_ml.clv.fit import CALIBRATION_FILE, FIT_FILE, TRANSACTIONS_FILE, _calibration_summary
from parley_ml.clv.split import daily_purchase_days, holdout_outcomes, split_daily, utc_midnight
from parley_ml.datasets.checksums import sha256_bytes, sha256_file, write_json
from parley_ml.evaluation.ranking import (
    cumulative_gains_curve,
    cumulative_gains_gini,
    mean_absolute_error,
    spearman_correlation,
    top_fraction_capture,
    total_purchase_error,
)
from parley_ml.serving.clv import (
    BetaGeoParameters,
    GammaGammaParameters,
    expected_purchases,
    expected_spend,
)

_ML_ROOT = Path(__file__).resolve().parents[2]
EVAL_PREFIX: Final = "clv_eval"
CURVE_POINTS: Final = 101
CI_LOW: Final = 2.5
CI_HIGH: Final = 97.5
RANK_METRICS: Final[tuple[str, ...]] = ("spearman", "top_decile_capture", "gini")
COMPARATORS: Final[tuple[str, ...]] = ("model", "rfm_score", "last_period_spend")
BASELINES: Final[tuple[str, ...]] = ("rfm_score", "last_period_spend")
POPULATIONS: Final[tuple[str, ...]] = ("all_customers", "repeat_buyers")


@dataclass(frozen=True)
class EvalConfig:
    fit_run_dir: Path
    input_dir: Path
    results_dir: Path
    run_root: Path
    artifact_root: Path
    bootstrap_replicates: int
    bootstrap_seed: int
    decile_fraction: float
    frequency_bin_edges: tuple[int, ...]
    last_period_days: int
    rfm_window_days: int
    rfm_tiers: int
    artifact_version_date: date
    artifact_horizon_days: int
    config_sha256: str


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Evaluate BG/NBD + Gamma-Gamma against baselines on the holdout.")
    parser.add_argument("--config", required=True, type=Path, help="Path to clv_eval.yaml")
    args = parser.parse_args(argv)
    started = time.perf_counter()
    try:
        config = load_eval_config(args.config, root=_ML_ROOT)
        result = run(config)
    except (FileNotFoundError, ValueError) as error:
        print(f"clv_eval: {error}", file=sys.stderr)
        return 1
    print(json.dumps(result["summary"], indent=2))
    print(f"elapsed_seconds: {time.perf_counter() - started:.1f}")
    return 0


def load_eval_config(path: Path, root: Path) -> EvalConfig:
    payload = path.read_bytes()
    raw = yaml.safe_load(payload)
    paths = raw["paths"]
    evaluation = raw["evaluation"]
    artifact = raw["artifact"]
    edges = tuple(int(edge) for edge in evaluation["frequency_bin_edges"])
    if list(edges) != sorted(set(edges)) or edges[0] != 0:
        raise ValueError("evaluation.frequency_bin_edges must be strictly increasing and start at 0")
    if int(evaluation["bootstrap_replicates"]) < 1:
        raise ValueError("evaluation.bootstrap_replicates must be at least 1")
    fraction = float(evaluation["decile_fraction"])
    if not 0 < fraction < 1:
        raise ValueError("evaluation.decile_fraction must be between 0 and 1")
    return EvalConfig(
        fit_run_dir=root / paths["fit_run_dir"],
        input_dir=root / paths["input_dir"],
        results_dir=root / paths["results_dir"],
        run_root=root / paths["run_root"],
        artifact_root=root / paths["artifact_root"],
        bootstrap_replicates=int(evaluation["bootstrap_replicates"]),
        bootstrap_seed=int(evaluation["bootstrap_seed"]),
        decile_fraction=fraction,
        frequency_bin_edges=edges,
        last_period_days=int(evaluation["last_period_days"]),
        rfm_window_days=int(evaluation["rfm_window_days"]),
        rfm_tiers=int(evaluation["rfm_tiers"]),
        artifact_version_date=date.fromisoformat(str(artifact["version_date"])),
        artifact_horizon_days=int(artifact["horizon_days"]),
        config_sha256=sha256_bytes(payload),
    )


def run(config: EvalConfig) -> dict[str, Any]:
    fit = json.loads((config.fit_run_dir / FIT_FILE).read_text(encoding="utf-8"))
    cal_end = date.fromisoformat(fit["counts"]["split"]["calibration_end"])
    hold_end = date.fromisoformat(fit["counts"]["split"]["holdout_end"])
    horizon = (hold_end - cal_end).days
    bg = BetaGeoParameters(**_select(fit["bg_nbd"]["parameters"], ("alpha", "r", "a", "b")))
    gg = GammaGammaParameters(**_select(fit["gamma_gamma"]["parameters"], ("p", "q", "v")))

    transactions = _read_transactions(config.input_dir, fit)
    daily, _ = daily_purchase_days(transactions)
    split = split_daily(daily, cal_end, hold_end)
    calibration_summary = _calibration_summary(split.calibration, cal_end)
    _verify_calibration_summary(calibration_summary, config.fit_run_dir / CALIBRATION_FILE)
    purchase_outcomes = holdout_outcomes(split).set_index("customer_id")

    holdout_revenue, total_holdout_revenue = _holdout_revenue(transactions, cal_end, hold_end)
    rfm = _rfm_scores(transactions, split.calibration, cal_end, config)
    last_period = _last_period_spend(transactions, cal_end, config.last_period_days)

    population = calibration_summary.set_index("customer_id").sort_index()
    frame = pd.DataFrame(index=population.index)
    frame["frequency"] = population["frequency"]
    frame["recency"] = population["recency"]
    frame["tenure"] = population["T"]
    frame["mean_repeat_value"] = population["monetary_value"]
    frame["repeat"] = frame["frequency"] > 0
    frame["holdout_purchases"] = purchase_outcomes["holdout_frequency"].reindex(frame.index).fillna(0).astype(float)
    frame["holdout_revenue"] = holdout_revenue.reindex(frame.index).fillna(0.0)
    frame["predicted_purchases"] = expected_purchases(bg, frame["frequency"], frame["recency"], frame["tenure"], horizon)
    if frame["predicted_purchases"].isna().any():
        raise ValueError("Calibration items rejected by the closed-form domain; the population is not scoreable")
    frame["predicted_spend"] = expected_spend(gg, frame["frequency"], frame["mean_repeat_value"])
    frame["predicted_value"] = frame["predicted_purchases"] * frame["predicted_spend"]
    frame["rfm_score"] = rfm.reindex(frame.index).astype(float)
    frame["last_period_spend"] = last_period.reindex(frame.index).fillna(0.0)

    excluded = purchase_outcomes.loc[purchase_outcomes["first_seen_in_holdout"]]
    exclusion = {
        "customers_first_seen_in_holdout": int(len(excluded)),
        "holdout_revenue_of_excluded_customers": round(float(holdout_revenue.reindex(excluded.index).fillna(0).sum()), 2),
        "holdout_revenue_all_customers": round(float(total_holdout_revenue), 2),
        "holdout_revenue_share_excluded": float(
            holdout_revenue.reindex(excluded.index).fillna(0).sum() / total_holdout_revenue
        ),
    }

    metrics = _evaluate(frame, config)
    calibration_bins = _calibration_by_frequency(frame, config)
    curves = _cumulative_gains(frame)

    results_dir = config.results_dir / "clv"
    results_dir.mkdir(parents=True, exist_ok=True)
    result_files = {
        "metrics.csv": _write_csv(metrics["table"], results_dir / "metrics.csv"),
        "calibration_by_frequency.csv": _write_csv(calibration_bins, results_dir / "calibration_by_frequency.csv"),
        "cumulative_gains.csv": _write_csv(curves, results_dir / "cumulative_gains.csv"),
    }
    _plot_calibration(calibration_bins, results_dir / "calibration_by_frequency.png")
    _plot_cumulative_gains(curves, results_dir / "cumulative_gains.png")
    result_files["calibration_by_frequency.png"] = {"sha256": sha256_file(results_dir / "calibration_by_frequency.png")}
    result_files["cumulative_gains.png"] = {"sha256": sha256_file(results_dir / "cumulative_gains.png")}

    summary = {
        "fit_run_id": fit["run_id"],
        "horizon_days": horizon,
        "population": {
            "all_customers": int(len(frame)),
            "repeat_buyers": int(frame["repeat"].sum()),
        },
        "exclusion": exclusion,
        "metrics": metrics["point"],
        "calibration": metrics["calibration"],
        "negative_predicted_spend_customers": int((frame["predicted_spend"] < 0).sum()),
        "repeat_customers_with_population_spend": int(((frame["frequency"] > 0) & (frame["mean_repeat_value"] <= 0)).sum()),
    }
    evaluation = {
        "config_sha256": config.config_sha256,
        "fit_run_id": fit["run_id"],
        "fit_config_sha256": fit["config_sha256"],
        "transactions_sha256": fit["dataset"]["transactions_sha256"],
        "split": fit["counts"]["split"],
        "horizon_days": horizon,
        "bootstrap": {"replicates": config.bootstrap_replicates, "seed": config.bootstrap_seed},
        "population": summary["population"],
        "exclusion": exclusion,
        "metrics": metrics["point"],
        "metrics_intervals": metrics["intervals"],
        "calibration": metrics["calibration"],
        "negative_predicted_spend_customers": summary["negative_predicted_spend_customers"],
        "repeat_customers_with_population_spend": summary["repeat_customers_with_population_spend"],
        "results": result_files,
        "libraries": {"numpy": np.__version__, "pandas": pd.__version__, "matplotlib": importlib.metadata.version("matplotlib")},
    }
    run_dir = config.run_root / f"{EVAL_PREFIX}-{config.config_sha256[:RUN_HASH_CHARS]}"
    run_dir.mkdir(parents=True, exist_ok=True)
    write_json(run_dir / "evaluation.json", evaluation)

    artifact_dir = write_artifact(
        root=config.artifact_root,
        fit=fit,
        transaction_parameters=bg,
        spend_parameters=gg,
        version_date=config.artifact_version_date,
        horizon_days=config.artifact_horizon_days,
        evaluation=evaluation,
        evaluation_run=run_dir.name,
    )
    summary["artifact"] = artifact_dir.name
    summary["run_dir"] = str(run_dir)
    summary["results_dir"] = str(results_dir)
    return {"summary": summary, "evaluation": evaluation, "frame": frame}


def _select(source: dict[str, float], names: tuple[str, ...]) -> dict[str, float]:
    return {name: float(source[name]) for name in names}


def _read_transactions(input_dir: Path, fit: dict[str, Any]) -> pd.DataFrame:
    path = input_dir / TRANSACTIONS_FILE
    if sha256_file(path) != fit["dataset"]["transactions_sha256"]:
        raise ValueError(f"{path} differs from the input of the fit run")
    return pd.read_parquet(path, columns=["customer_id", "invoice_no", "invoice_ts", "line_amount", "is_cancellation"])


def _verify_calibration_summary(recomputed: pd.DataFrame, stored_path: Path) -> None:
    stored = pd.read_parquet(stored_path)
    pd.testing.assert_frame_equal(
        recomputed.reset_index(drop=True), stored.reset_index(drop=True), check_exact=False, rtol=1e-12
    )


def _holdout_revenue(transactions: pd.DataFrame, cal_end: date, hold_end: date) -> tuple[pd.Series, float]:
    """Net amount of all lines in (cal_end, hold_end], per customer, and the all-customer total."""
    days = utc_midnight(transactions["invoice_ts"])
    window = (days > pd.Timestamp(cal_end)) & (days <= pd.Timestamp(hold_end))
    lines = transactions.loc[window]
    per_customer = lines.groupby("customer_id")["line_amount"].sum().astype(float)
    return per_customer, float(lines["line_amount"].sum())


def _rfm_scores(transactions: pd.DataFrame, calibration: pd.DataFrame, cal_end: date, config: EvalConfig) -> pd.Series:
    """RFM score: R, F and M quintile tiers 1..5, summed (3..15). Higher is better.

    Recency is days from the last purchase to the end of calibration. Frequency and monetary value cover the last
    ``rfm_window_days``: distinct purchase invoices and net amount of all lines. Equal values share a tier.
    """
    end = pd.Timestamp(cal_end)
    days = utc_midnight(transactions["invoice_ts"])
    start = end - pd.Timedelta(days=config.rfm_window_days)
    window = (days > start) & (days <= end)

    last_purchase = calibration.groupby("customer_id")["day"].max()
    recency = (end - last_purchase).dt.days.astype(float)

    purchase_lines = transactions.loc[window & ~transactions["is_cancellation"]]
    orders = purchase_lines.groupby("customer_id")["invoice_no"].nunique().astype(float)
    value = transactions.loc[window].groupby("customer_id")["line_amount"].sum().astype(float)
    orders = orders.reindex(last_purchase.index).fillna(0.0)
    value = value.reindex(last_purchase.index).fillna(0.0)

    tiers = config.rfm_tiers
    recency_tier = _ntile(-recency, tiers)
    frequency_tier = _ntile(orders, tiers)
    monetary_tier = _ntile(value, tiers)
    return (recency_tier + frequency_tier + monetary_tier).rename("rfm_score")


def _ntile(values: pd.Series, tiers: int) -> pd.Series:
    """Quantile tier 1..tiers from the average percentile rank. Equal values share one tier, so the tier does not
    depend on customer order."""
    percentile = values.rank(method="average", pct=True)
    return np.ceil(percentile * tiers).clip(lower=1.0).astype(float)


def _last_period_spend(transactions: pd.DataFrame, cal_end: date, days: int) -> pd.Series:
    end = pd.Timestamp(cal_end)
    dates = utc_midnight(transactions["invoice_ts"])
    window = (dates > end - pd.Timedelta(days=days)) & (dates <= end)
    return transactions.loc[window].groupby("customer_id")["line_amount"].sum().astype(float).rename("last_period_spend")


def _bootstrap_indices(size: int, config: EvalConfig, population_index: int) -> np.ndarray:
    rng = np.random.default_rng(config.bootstrap_seed + population_index)
    return rng.integers(0, size, size=(config.bootstrap_replicates, size))


def _metrics_for(subset: pd.DataFrame, fraction: float) -> dict[str, float]:
    actual = subset["holdout_revenue"].to_numpy()
    predictions = {
        "model": subset["predicted_value"].to_numpy(),
        "rfm_score": subset["rfm_score"].to_numpy(),
        "last_period_spend": subset["last_period_spend"].to_numpy(),
    }
    row: dict[str, float] = {}
    for name, scores in predictions.items():
        row[f"spearman_{name}"] = spearman_correlation(scores, actual)
        row[f"top_decile_capture_{name}"] = top_fraction_capture(scores, actual, fraction)
        row[f"gini_{name}"] = cumulative_gains_gini(scores, actual)
    purchases = subset["holdout_purchases"].to_numpy()
    row["total_purchase_error"] = total_purchase_error(subset["predicted_purchases"].to_numpy(), purchases)
    row["mae_purchases"] = mean_absolute_error(subset["predicted_purchases"].to_numpy(), purchases)
    return row


def _evaluate(frame: pd.DataFrame, config: EvalConfig) -> dict[str, Any]:
    fraction = config.decile_fraction
    point: dict[str, dict[str, float]] = {}
    intervals: dict[str, dict[str, Any]] = {}
    rows: list[dict[str, Any]] = []
    for index, population in enumerate(POPULATIONS):
        subset = frame if population == "all_customers" else frame.loc[frame["repeat"]]
        full = _metrics_for(subset, fraction)
        indices = _bootstrap_indices(len(subset), config, index)
        replicates = pd.DataFrame(
            [_metrics_for(subset.iloc[row], fraction) for row in indices]
        )
        point[population] = full
        intervals[population] = {}
        for metric in RANK_METRICS:
            for name in COMPARATORS:
                key = f"{metric}_{name}"
                intervals[population][key] = _interval(replicates[key].to_numpy())
                rows.append(_row(population, metric, name, full[key], replicates[key].to_numpy()))
            for baseline in BASELINES:
                diff_key = f"{metric}_model"
                differences = replicates[diff_key].to_numpy() - replicates[f"{metric}_{baseline}"].to_numpy()
                point_difference = full[diff_key] - full[f"{metric}_{baseline}"]
                intervals[population][f"{metric}_model_minus_{baseline}"] = _interval(differences)
                rows.append(_row(population, metric, f"model_minus_{baseline}", point_difference, differences))
        for key in ("total_purchase_error", "mae_purchases"):
            intervals[population][key] = _interval(replicates[key].to_numpy())
            rows.append(_row(population, "calibration", key, full[key], replicates[key].to_numpy()))
    table = pd.DataFrame(rows)
    calibration = {
        population: {
            "total_purchase_error": point[population]["total_purchase_error"],
            "mae_purchases": point[population]["mae_purchases"],
        }
        for population in POPULATIONS
    }
    return {"point": point, "intervals": intervals, "table": table, "calibration": calibration}


def _row(population: str, metric: str, comparison: str, point: float, replicates: np.ndarray) -> dict[str, Any]:
    low, high = _interval(replicates)
    row: dict[str, Any] = {
        "population": population,
        "metric": metric,
        "comparison": comparison,
        "point": point,
        "ci_low": low,
        "ci_high": high,
        "replicates": len(replicates),
        "share_replicates_positive": float("nan"),
    }
    if comparison.startswith("model_minus_"):
        row["share_replicates_positive"] = float(np.mean(replicates > 0))
    return row


def _interval(values: np.ndarray) -> tuple[float, float]:
    finite = values[np.isfinite(values)]
    if finite.size == 0:
        return float("nan"), float("nan")
    return float(np.percentile(finite, CI_LOW)), float(np.percentile(finite, CI_HIGH))


def _calibration_by_frequency(frame: pd.DataFrame, config: EvalConfig) -> pd.DataFrame:
    edges = list(config.frequency_bin_edges)
    labels = _bin_labels(edges)
    bins = np.digitize(frame["frequency"].to_numpy(), edges, right=False) - 1
    predicted = frame["predicted_purchases"].to_numpy()
    actual = frame["holdout_purchases"].to_numpy()
    counts = np.bincount(bins, minlength=len(labels)).astype(float)

    indices = _bootstrap_indices(len(frame), config, 0)
    replicate_predicted = np.empty((len(indices), len(labels)))
    replicate_actual = np.empty((len(indices), len(labels)))
    for row, sample in enumerate(indices):
        b = bins[sample]
        n = np.bincount(b, minlength=len(labels)).astype(float)
        sums_p = np.bincount(b, weights=predicted[sample], minlength=len(labels))
        sums_a = np.bincount(b, weights=actual[sample], minlength=len(labels))
        with np.errstate(invalid="ignore", divide="ignore"):
            replicate_predicted[row] = sums_p / n
            replicate_actual[row] = sums_a / n

    sums_predicted = np.bincount(bins, weights=predicted, minlength=len(labels))
    sums_actual = np.bincount(bins, weights=actual, minlength=len(labels))
    records = []
    for index, label in enumerate(labels):
        p_low, p_high = _interval(replicate_predicted[:, index])
        a_low, a_high = _interval(replicate_actual[:, index])
        records.append(
            {
                "frequency_bin": label,
                "customers": int(counts[index]),
                "predicted_mean": float(sums_predicted[index] / counts[index]),
                "predicted_ci_low": p_low,
                "predicted_ci_high": p_high,
                "actual_mean": float(sums_actual[index] / counts[index]),
                "actual_ci_low": a_low,
                "actual_ci_high": a_high,
            }
        )
    return pd.DataFrame(records)


def _bin_labels(edges: list[int]) -> list[str]:
    labels = []
    for index, low in enumerate(edges):
        if index + 1 < len(edges):
            high = edges[index + 1] - 1
            labels.append(f"{low}" if high == low else f"{low}-{high}")
        else:
            labels.append(f"{low}+")
    return labels


def _cumulative_gains(frame: pd.DataFrame) -> pd.DataFrame:
    grid = np.linspace(0.0, 1.0, CURVE_POINTS)
    data = {"customer_fraction": grid}
    for name, column in (("model", "predicted_value"), ("rfm_score", "rfm_score"), ("last_period_spend", "last_period_spend")):
        curve = cumulative_gains_curve(frame[column].to_numpy(), frame["holdout_revenue"].to_numpy())
        data[name] = np.interp(grid, np.linspace(0.0, 1.0, len(curve)), curve)
    return pd.DataFrame(data)


def _plot_calibration(table: pd.DataFrame, path: Path) -> None:
    figure = Figure(figsize=(6.4, 4.8), dpi=120)
    FigureCanvasAgg(figure)
    axis = figure.subplots()
    x = np.arange(len(table))
    axis.errorbar(
        x - 0.1,
        table["predicted_mean"],
        yerr=[table["predicted_mean"] - table["predicted_ci_low"], table["predicted_ci_high"] - table["predicted_mean"]],
        fmt="o",
        label="predicted (BG/NBD)",
        capsize=3,
    )
    axis.errorbar(
        x + 0.1,
        table["actual_mean"],
        yerr=[table["actual_mean"] - table["actual_ci_low"], table["actual_ci_high"] - table["actual_mean"]],
        fmt="s",
        label="actual (holdout)",
        capsize=3,
    )
    axis.set_xticks(x, table["frequency_bin"])
    axis.set_xlabel("Calibration frequency (repeat purchase days)")
    axis.set_ylabel("Purchase days per customer, holdout")
    axis.set_title("Calibration by frequency bin, 95 % bootstrap intervals")
    axis.legend()
    figure.tight_layout()
    figure.savefig(path, format="png")


def _plot_cumulative_gains(curves: pd.DataFrame, path: Path) -> None:
    figure = Figure(figsize=(6.4, 4.8), dpi=120)
    FigureCanvasAgg(figure)
    axis = figure.subplots()
    axis.plot(curves["customer_fraction"], curves["customer_fraction"], color="grey", linestyle="--", label="random")
    axis.plot(curves["customer_fraction"], curves["model"], label="BG/NBD + Gamma-Gamma")
    axis.plot(curves["customer_fraction"], curves["rfm_score"], label="RFM score")
    axis.plot(curves["customer_fraction"], curves["last_period_spend"], label="last-period spend")
    axis.set_xlabel("Share of customers, ordered by score")
    axis.set_ylabel("Share of holdout revenue")
    axis.set_title("Cumulative gains on the holdout")
    axis.legend()
    figure.tight_layout()
    figure.savefig(path, format="png")


def _write_csv(frame: pd.DataFrame, path: Path) -> dict[str, Any]:
    frame.to_csv(path, index=False, lineterminator="\n", float_format="%.10g")
    return {"rows": len(frame), "sha256": sha256_file(path)}


if __name__ == "__main__":
    raise SystemExit(main())
