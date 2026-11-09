"""Run the validation grid for every uplift family and outcome, sequentially, and write the summary.

    uv run --directory ml python -m parley_ml.uplift.train_grid --config configs/uplift_grid.yaml

Each finished run is skipped on the next invocation (resume by run id). A run that was killed before its
``metrics.json`` was written is refitted from its start. Winners are chosen per family and outcome on the validation
split only, by ``selection.metric`` from the grid config. The test split is never loaded.
"""

from __future__ import annotations

import argparse
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Final

import numpy as np

from parley_ml.datasets.checksums import write_json
from parley_ml.uplift.config import ML_ROOT, GridConfig, load_family_config, load_grid_config
from parley_ml.uplift.data import load_bundle
from parley_ml.uplift.runs import validation_metrics
from parley_ml.uplift.train import run_cell

DEFAULT_CONFIG: Final = ML_ROOT / "configs" / "uplift_grid.yaml"
RANDOM_FAMILY: Final = "random"


@dataclass(frozen=True)
class GridRow:
    family: str
    outcome: str
    max_depth: int | None
    learning_rate: float | None
    run_id: str
    reused: bool
    metrics: dict[str, Any]
    wall_seconds: float | None
    gpu_memory_peak_mib: int | None
    rounds: str


def _row_from_run(metrics: dict[str, Any], reused: bool) -> GridRow:
    training = metrics.get("training", {})
    rounds = training.get("rounds_kept")
    rounds_text = "/".join(f"{name}={value}" for name, value in rounds.items()) if isinstance(rounds, dict) else str(rounds)
    return GridRow(
        family=metrics["family"],
        outcome=metrics["outcome"],
        max_depth=int(metrics["max_depth"]),
        learning_rate=float(metrics["learning_rate"]),
        run_id=metrics["run_id"],
        reused=reused,
        metrics=metrics["validation"],
        wall_seconds=training.get("wall_seconds"),
        gpu_memory_peak_mib=training.get("gpu_memory_peak_mib"),
        rounds=rounds_text,
    )


def run_grid(grid: GridConfig) -> tuple[list[GridRow], list[GridRow]]:
    """Returns (grid rows for every family, outcome and cell; random baseline rows, one per outcome)."""
    rows: list[GridRow] = []
    baselines: list[GridRow] = []
    for family_path in grid.families:
        config = load_family_config(family_path)
        bundle = load_bundle(config)
        validation = bundle["validation"]
        for outcome in config.outcomes:
            if not any(baseline.outcome == outcome for baseline in baselines):
                scores = np.random.default_rng(config.seed).random(validation.rows)
                metrics = validation_metrics(
                    scores, validation.treatment, validation.outcome(outcome), config.uplift_k
                )
                baselines.append(
                    GridRow(
                        family=RANDOM_FAMILY,
                        outcome=outcome,
                        max_depth=None,
                        learning_rate=None,
                        run_id="baseline-random",
                        reused=False,
                        metrics=metrics,
                        wall_seconds=None,
                        gpu_memory_peak_mib=None,
                        rounds="",
                    )
                )
            for cell in grid.cells:
                started = time.perf_counter()
                metrics, reused = run_cell(config, bundle, outcome, cell, grid.runs_root)
                row = _row_from_run(metrics, reused)
                rows.append(row)
                print(
                    f"{row.family:9s} {outcome:10s} depth={cell.max_depth} lr={cell.learning_rate:<5} "
                    f"reused={reused} qini={row.metrics['qini_coefficient']:.2f} "
                    f"seconds={time.perf_counter() - started:.1f}",
                    flush=True,
                )
        del bundle
    return rows, baselines


def select_winners(rows: list[GridRow], metric: str) -> dict[tuple[str, str], GridRow]:
    """Best cell per (family, outcome) on the given validation metric. Ties keep the earlier row."""
    winners: dict[tuple[str, str], GridRow] = {}
    for row in rows:
        key = (row.family, row.outcome)
        if key not in winners or row.metrics[metric] > winners[key].metrics[metric]:
            winners[key] = row
    return winners


def _csv(rows: list[GridRow], uplift_keys: list[str]) -> str:
    header = [
        "family",
        "outcome",
        "max_depth",
        "learning_rate",
        "run_id",
        "reused",
        "rounds_kept",
        "qini_coefficient",
        "auuc",
        "separate_relative_auuc",
        *[f"uplift_at_{key}" for key in uplift_keys],
        "wall_seconds",
        "gpu_memory_peak_mib",
    ]
    lines = [",".join(header)]
    for row in rows:
        values = [
            row.family,
            row.outcome,
            "" if row.max_depth is None else str(row.max_depth),
            "" if row.learning_rate is None else f"{row.learning_rate:g}",
            row.run_id,
            str(row.reused).lower(),
            row.rounds,
            f"{row.metrics['qini_coefficient']:.6f}",
            f"{row.metrics['auuc']:.6f}",
            f"{row.metrics['separate_relative_auuc']:.6f}",
            *[f"{row.metrics['uplift_at_k'][key]:.6f}" for key in uplift_keys],
            "" if row.wall_seconds is None else f"{row.wall_seconds:.2f}",
            "" if row.gpu_memory_peak_mib is None else str(row.gpu_memory_peak_mib),
        ]
        lines.append(",".join(values))
    return "\n".join(lines) + "\n"


def _markdown(
    grid: GridConfig,
    rows: list[GridRow],
    baselines: list[GridRow],
    winners: dict[tuple[str, str], GridRow],
) -> str:
    metric = grid.selection_metric
    lines = [
        "# Uplift validation grid (RQ1)",
        "",
        "Validation split only. Test split not opened. Metric values are point estimates; the final evaluation adds bootstrap intervals.",
        f"Selection: best cell per family and outcome by `{metric}` on validation.",
        "",
        "## Winners per family and outcome",
        "",
        "| Outcome | Family | max_depth | learning_rate | Qini coefficient | AUUC | Separate relative AUUC | Run |",
        "|---|---|---|---|---|---|---|---|",
    ]
    for (family, outcome), row in sorted(winners.items(), key=lambda item: (item[0][1], item[0][0])):
        lines.append(
            f"| {outcome} | {family} | {row.max_depth} | {row.learning_rate:g} | "
            f"{row.metrics['qini_coefficient']:.2f} | {row.metrics['auuc']:.2f} | "
            f"{row.metrics['separate_relative_auuc']:.2f} | `{row.run_id}` |"
        )
    lines += ["", "## Random baseline (validation)", "", "| Outcome | Qini coefficient | AUUC | Separate relative AUUC |", "|---|---|---|---|"]
    for baseline in sorted(baselines, key=lambda row: row.outcome):
        lines.append(
            f"| {baseline.outcome} | {baseline.metrics['qini_coefficient']:.2f} | "
            f"{baseline.metrics['auuc']:.2f} | {baseline.metrics['separate_relative_auuc']:.2f} |"
        )
    timed = [row for row in rows if row.wall_seconds is not None]
    peaks = [row.gpu_memory_peak_mib for row in timed if row.gpu_memory_peak_mib is not None]
    lines += [
        "",
        "## Measured compute",
        "",
        f"- Runs with timing: {len(timed)}, of which reused from an earlier invocation: {sum(1 for r in rows if r.reused)}.",
        f"- Sum of fit and scoring seconds over timed runs: {sum(row.wall_seconds for row in timed):.0f}.",
        f"- Peak device GPU memory used (nvidia-smi, device-wide): {max(peaks) if peaks else 'not available'} MiB.",
        "",
        "## All grid cells",
        "",
        "| Outcome | Family | max_depth | learning_rate | Qini | AUUC | Sep. rel. AUUC | Rounds | Seconds | GPU MiB |",
        "|---|---|---|---|---|---|---|---|---|---|",
    ]
    for row in sorted(rows, key=lambda r: (r.outcome, r.family, r.max_depth or 0, r.learning_rate or 0.0)):
        lines.append(
            f"| {row.outcome} | {row.family} | {row.max_depth} | {row.learning_rate:g} | "
            f"{row.metrics['qini_coefficient']:.2f} | {row.metrics['auuc']:.2f} | "
            f"{row.metrics['separate_relative_auuc']:.2f} | {row.rounds} | "
            f"{'' if row.wall_seconds is None else f'{row.wall_seconds:.0f}'} | "
            f"{'' if row.gpu_memory_peak_mib is None else row.gpu_memory_peak_mib} |"
        )
    return "\n".join(lines) + "\n"


def write_summary(grid: GridConfig, rows: list[GridRow], baselines: list[GridRow]) -> dict[str, Any]:
    uplift_keys = list(baselines[0].metrics["uplift_at_k"])
    winners = select_winners(rows, grid.selection_metric)
    csv_text = _csv(rows + baselines, uplift_keys)
    grid.summary_csv.parent.mkdir(parents=True, exist_ok=True)
    grid.summary_csv.write_text(csv_text, encoding="utf-8", newline="\n")
    grid.summary_markdown.write_text(
        _markdown(grid, rows, baselines, winners),
        encoding="utf-8",
        newline="\n",
    )
    return {
        "winners": {f"{outcome}/{family}": row.run_id for (family, outcome), row in winners.items()},
        "rows": len(rows),
        "summary_csv": str(grid.summary_csv),
        "summary_markdown": str(grid.summary_markdown),
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Run the uplift validation grid (resumable).")
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    args = parser.parse_args(argv)
    config_path = args.config if args.config.is_absolute() else ML_ROOT / args.config
    grid = load_grid_config(config_path)
    started = time.perf_counter()
    rows, baselines = run_grid(grid)
    summary = write_summary(grid, rows, baselines)
    summary["grid_wall_seconds"] = round(time.perf_counter() - started, 1)
    summary["reused_runs"] = sum(1 for row in rows if row.reused)
    write_json(grid.runs_root / "grid_summary.json", summary)
    print(summary)
    return 0


if __name__ == "__main__":
    sys.exit(main())
