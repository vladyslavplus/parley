"""Write the final RQ1 outputs: committed CSV and Markdown under ml/results, figures under git-ignored artifacts.

Everything here is a function of the results dictionary, so a re-run with the same configs writes the same bytes.
"""

from __future__ import annotations

import csv
import io
from typing import Any, Final

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

from parley_ml.datasets.checksums import sha256_file  # noqa: E402
from parley_ml.uplift.final import BASELINE, FAMILIES, FinalConfig  # noqa: E402

FAMILY_LABEL: Final = {"response": "Response propensity", "s": "S-learner", "t": "T-learner"}
UPLIFT_KEYS: Final = ("uplift_at_0.05", "uplift_at_0.10", "uplift_at_0.20", "uplift_at_0.30", "uplift_at_0.50")
COMPARED: Final = [family for family in FAMILIES if family != BASELINE]


def _size_items(config: FinalConfig, blocks: dict[str, Any]) -> list[tuple[str, Any]]:
    """Sample-size blocks in the configured order (not alphabetical), so tables read smallest to full."""
    labels = ["full" if size is None else str(size) for size in config.sample_sizes]
    return [(label, blocks[label]) for label in labels]


def _csv_text(header: list[str], rows: list[list[Any]]) -> str:
    buffer = io.StringIO(newline="")
    writer = csv.writer(buffer, lineterminator="\n")
    writer.writerow(header)
    writer.writerows(rows)
    return buffer.getvalue()


def _fmt(value: Any, digits: int = 4) -> str:
    if value is None:
        return ""
    return f"{value:.{digits}f}"


def _interval(entry: dict[str, Any], digits: int = 4) -> str:
    return f"{_fmt(entry['point'], digits)} [{_fmt(entry['ci_low'], digits)}, {_fmt(entry['ci_high'], digits)}]"


def _md_table(header: list[str], rows: list[list[str]]) -> str:
    lines = ["| " + " | ".join(header) + " |", "|" + "|".join("---" for _ in header) + "|"]
    lines += ["| " + " | ".join(row) + " |" for row in rows]
    return "\n".join(lines)


def render(
    config: FinalConfig,
    results: dict[str, Any],
    freeze: dict[str, Any],
    winners_check: dict[str, Any],
    header: dict[str, Any],
    notes_text: str,
) -> dict[str, Any]:
    results_dir = config.results_dir
    results_dir.mkdir(parents=True, exist_ok=True)
    config.figures.mkdir(parents=True, exist_ok=True)
    written: dict[str, Any] = {}

    def write_text(name: str, text: str) -> None:
        path = results_dir / name
        path.write_text(text, encoding="utf-8", newline="\n")
        written[name] = path

    # Point metrics per seed.
    rows = []
    for outcome, by_family in results["point"].items():
        for family, per_seed in by_family.items():
            for seed, entry in per_seed.items():
                for metric in ("qini_coefficient", "auuc", "separate_relative_auuc", *UPLIFT_KEYS):
                    rows.append([outcome, family, seed, metric, f"{entry[metric]:.8g}"])
    write_text("rq1_test_metrics.csv", _csv_text(["outcome", "family", "seed", "metric", "value"], rows))

    # Intervals (point, bootstrap 95 %, share of replicates above zero for differences).
    rows = []
    for outcome, intervals in results["intervals"].items():
        for name, entry in intervals.items():
            rows.append(
                [outcome, name, f"{entry['point']:.8g}", f"{entry['ci_low']:.8g}", f"{entry['ci_high']:.8g}",
                 entry["replicates"], f"{entry['share_positive']:.4f}"]
            )
    write_text(
        "rq1_intervals.csv",
        _csv_text(["outcome", "statistic", "point", "ci_low", "ci_high", "replicates", "share_positive"], rows),
    )

    # Sample-size curve.
    rows = []
    for outcome, sizes in results["sample_size"].items():
        for label, block in _size_items(config, sizes):
            for name, entry in block["summary"].items():
                rows.append(
                    [outcome, label, block["rows"], name, f"{entry['point']:.8g}", f"{entry['ci_low']:.8g}",
                     f"{entry['ci_high']:.8g}", entry["replicates"]]
                )
    write_text(
        "rq1_sample_size.csv",
        _csv_text(["outcome", "sample_size", "rows", "statistic", "point", "ci_low", "ci_high", "replicates"], rows),
    )

    # Conditional profit at k* (chosen on validation).
    rows = []
    for outcome, by_family in results["profit"].items():
        for family, by_ratio in by_family.items():
            for ratio, entry in by_ratio.items():
                interval = results["intervals"][outcome][f"{family}.profit_{ratio}"]
                rows.append(
                    [outcome, family, ratio, f"{entry['k_star']:.2f}", f"{interval['point']:.8g}",
                     f"{interval['ci_low']:.8g}", f"{interval['ci_high']:.8g}"]
                )
    write_text(
        "rq1_profit.csv",
        _csv_text(["outcome", "family", "cost_to_value_ratio", "k_star", "profit", "ci_low", "ci_high"], rows),
    )

    rows = []
    for outcome, by_family in results["profit_curves"].items():
        for family, by_ratio in by_family.items():
            for ratio, values in by_ratio.items():
                for fraction, value in zip(results["qini_curves"][outcome][family]["fraction"], values):
                    rows.append([outcome, family, ratio, f"{fraction:.2f}", f"{value:.8g}"])
    write_text("rq1_profit_curves.csv", _csv_text(["outcome", "family", "cost_to_value_ratio", "fraction", "profit"], rows))

    rows = []
    for outcome, by_family in results["qini_curves"].items():
        for family, curve in by_family.items():
            for fraction, value, random_value in zip(curve["fraction"], curve["qini"], curve["random"]):
                rows.append([outcome, family, f"{fraction:.2f}", f"{value:.8g}", f"{random_value:.8g}"])
    write_text("rq1_qini_curves.csv", _csv_text(["outcome", "family", "fraction", "qini", "random"], rows))

    write_text("rq1_uplift.md", _markdown(config, results, freeze, winners_check, header, notes_text))
    written["figures"] = _figures(config, results)
    return written


def _markdown(
    config: FinalConfig,
    results: dict[str, Any],
    freeze: dict[str, Any],
    winners_check: dict[str, Any],
    header: dict[str, Any],
    notes_text: str,
) -> str:
    out: list[str] = []
    out.append("# RQ1: does ranking by predicted uplift beat ranking by response propensity?")
    out.append("")
    out.append(
        "The test split was opened by two test-phase attempts (the first stopped for low memory and wrote no results; see Protocol in the notes below) and by one reproduction run. The reproduction run compares every committed output byte for byte; its record is git-ignored (artifacts/final/reproduction.json). Winners and k* were fixed on "
        "the validation split before the test split was read. Intervals are 95 % Poisson-bootstrap percentile intervals "
        f"with B = {config.bootstrap_replicates}; differences are paired (same bootstrap weights for both models)."
    )
    out.append("")
    out.append("## Provenance")
    out.append("")
    out.append(_md_table(
        ["Item", "Value"],
        [
            ["Final config", f"`{config.path.name}`, sha256 `{header['config_sha256']}`"],
            ["Validation grid", f"`{config.grid_summary.relative_to(config.grid_summary.parents[1]).as_posix()}`, sha256 `{freeze['grid_summary_sha256']}`"],
            ["Freeze file", f"sha256 `{header['freeze_sha256']}`"],
            ["Git commit", f"`{header['git']['commit']}`" + (" (ml/ has uncommitted changes)" if header["git"]["ml_has_uncommitted_changes"] else "")],
            ["Environment", f"hash `{header['environment']['hash']}`, Python {header['environment']['python']}, "
                            + ", ".join(f"{name} {version}" for name, version in header["environment"]["libraries"].items())],
            ["Seeds", ", ".join(str(seed) for seed in config.seeds)],
            ["Test rows", f"{results['test_rows']:,} ({results['test_treated_rows']:,} treated, {results['test_control_rows']:,} control)"],
            *[
                [f"Family config ({family})", f"`{config.family_configs[family].name}`, sha256 `{sha256_file(config.family_configs[family])}`"]
                for family in FAMILIES
            ],
            ["Saved model checksums", f"`model_checksums.json` (18 boosters), sha256 `{sha256_file(config.final_root / 'model_checksums.json')}`"],
        ],
    ))
    out.append("")
    out.append("## Pipeline")
    out.append("")
    out.append("![Uplift research pipeline](uplift_pipeline.svg)")
    out.append("")
    out.append("## Winners fixed on validation")
    out.append("")
    rows = []
    for key, entry in winners_check.items():
        outcome, family = key.split("/")
        metric = [name for name in entry if name.startswith("validation_")][0]
        rows.append([outcome, FAMILY_LABEL[family], str(entry["max_depth"]), f"{entry['learning_rate']:g}",
                     f"{entry[metric]:.2f}"])
    out.append(_md_table(["Outcome", "Family", "max_depth", "learning_rate", f"Validation {config.selection_metric}"], rows))
    out.append("")

    for outcome in config.winners:
        out.append(f"## Test results: {outcome}" + (" (primary)" if outcome == config.primary_outcome else " (secondary)"))
        out.append("")
        out.append("Point values on the full test split, seed 0 with the seed range over the three seeds in brackets.")
        out.append("")
        header_row = ["Family", "Qini coefficient", "AUUC", "Separate relative AUUC"] + [f"Uplift@{key.split('_')[-1]}" for key in UPLIFT_KEYS]
        rows = []
        for family in FAMILIES:
            seed_summary = results["seed_summary"][outcome][family]
            seed0 = results["point"][outcome][family][str(config.seeds[0])]
            cells = [FAMILY_LABEL[family]]
            for metric in ("qini_coefficient", "auuc", "separate_relative_auuc", *UPLIFT_KEYS):
                summary = seed_summary[metric]
                digits = 2 if metric in ("qini_coefficient", "auuc") else 4
                cells.append(f"{seed0[metric]:.{digits}f} [{summary['min']:.{digits}f}, {summary['max']:.{digits}f}]")
            rows.append(cells)
        out.append(_md_table(header_row, rows))
        out.append("")
        out.append("Paired differences from the response propensity model, 95 % interval, share of bootstrap replicates above zero:")
        out.append("")
        rows = []
        for family in COMPARED:
            for metric, digits in (("qini_coefficient", 2), ("auuc", 2), ("separate_relative_auuc", 4), ("uplift_at_0.10", 4)):
                entry = results["intervals"][outcome][f"{family}_minus_{BASELINE}.{metric}"]
                rows.append([
                    FAMILY_LABEL[family], metric,
                    f"{entry['point']:.{digits}f} [{entry['ci_low']:.{digits}f}, {entry['ci_high']:.{digits}f}]",
                    f"{entry['share_positive']:.3f}",
                ])
        out.append(_md_table(["Model minus response", "Statistic", "Difference [95 % CI]", "Share > 0"], rows))
        out.append("")
        out.append("Point values for each model on the test split (bootstrap intervals in brackets):")
        out.append("")
        rows = []
        for family in FAMILIES:
            cells = [FAMILY_LABEL[family]]
            for metric, digits in (("qini_coefficient", 2), ("auuc", 2), ("separate_relative_auuc", 4)):
                cells.append(_interval(results["intervals"][outcome][f"{family}.{metric}"], digits))
            rows.append(cells)
        out.append(_md_table(["Family", "Qini coefficient", "AUUC", "Separate relative AUUC"], rows))
        out.append("")

        out.append(f"### Sample-size curve: {outcome}")
        out.append("")
        out.append("Qini coefficient and AUUC on test subsamples (same rows for every model), with intervals.")
        out.append("")
        rows = []
        for label, block in _size_items(config, results["sample_size"][outcome]):
            for family in FAMILIES:
                summary = block["summary"]
                rows.append([
                    label, f"{block['rows']:,}", FAMILY_LABEL[family],
                    _interval(summary[f"{family}.qini_coefficient"], 2),
                    _interval(summary[f"{family}.auuc"], 2),
                ])
        out.append(_md_table(["Sample", "Rows", "Family", "Qini coefficient", "AUUC"], rows))
        out.append("")
        rows = []
        for label, block in _size_items(config, results["sample_size"][outcome]):
            for family in COMPARED:
                summary = block["summary"]
                rows.append([
                    label, FAMILY_LABEL[family],
                    _interval(summary[f"{family}_minus_{BASELINE}.qini_coefficient"], 2),
                    _interval(summary[f"{family}_minus_{BASELINE}.auuc"], 2),
                ])
        out.append(_md_table(["Sample", "Model minus response", "Qini difference [CI]", "AUUC difference [CI]"], rows))
        out.append("")

        out.append(f"### Conditional profit at k* ({outcome})")
        out.append("")
        out.append(
            "Profit(k) = N(k) * (uplift(k) - c/v) per unit of value v; k* is the fraction with the highest mean "
            "validation profit for each c/v. Values are on the test split with bootstrap intervals."
        )
        out.append("")
        rows = []
        for family in FAMILIES:
            for ratio in config.cost_to_value_ratios:
                key = f"{ratio:g}"
                entry = results["profit"][outcome][family][key]
                interval = results["intervals"][outcome][f"{family}.profit_{key}"]
                rows.append([FAMILY_LABEL[family], key, f"{entry['k_star']:.2f}", _interval(interval, 1)])
        out.append(_md_table(["Family", "c/v", "k*", "Profit [CI]"], rows))
        out.append("")

    out.append("## Notes for the report")
    out.append("")
    out.append(notes_text.strip())
    out.append("")
    return "\n".join(out)


def _figures(config: FinalConfig, results: dict[str, Any]) -> list[str]:
    created: list[str] = []
    for outcome in config.winners:
        figure, axis = plt.subplots(figsize=(6.0, 4.2))
        curve_family = results["qini_curves"][outcome]
        fractions = curve_family[FAMILIES[0]]["fraction"]
        axis.plot(fractions, curve_family[FAMILIES[0]]["random"], color="0.4", linestyle=":", label="random")
        for family in FAMILIES:
            axis.plot(fractions, curve_family[family]["qini"], label=FAMILY_LABEL[family])
        axis.set_xlabel("fraction of test population targeted")
        axis.set_ylabel("Qini (outcome units)")
        axis.set_title(f"Qini curves, test split, outcome {outcome}")
        axis.legend()
        figure.tight_layout()
        name = f"qini_{outcome}.png"
        figure.savefig(config.figures / name, dpi=110)
        plt.close(figure)
        created.append(name)

        figure, axis = plt.subplots(figsize=(6.0, 4.2))
        blocks = results["sample_size"][outcome]
        labels = [label for label, _ in _size_items(config, blocks)]
        xs = list(range(len(labels)))
        for family in COMPARED:
            points = [blocks[label]["summary"][f"{family}_minus_{BASELINE}.qini_coefficient"] for label in labels]
            center = [entry["point"] for entry in points]
            low = [max(0.0, entry["point"] - entry["ci_low"]) for entry in points]
            high = [max(0.0, entry["ci_high"] - entry["point"]) for entry in points]
            axis.errorbar([x + (0.08 if family == "t" else -0.08) for x in xs], center, yerr=[low, high],
                          fmt="o", capsize=3, label=f"{FAMILY_LABEL[family]} minus response")
        axis.axhline(0.0, color="0.4", linewidth=0.8)
        axis.set_xticks(xs)
        axis.set_xticklabels([f"{blocks[label]['rows']:,}" for label in labels])
        axis.set_xlabel("test rows in subsample")
        axis.set_ylabel("Qini difference (outcome units)")
        axis.set_title(f"Sample-size curve, outcome {outcome}")
        axis.legend()
        figure.tight_layout()
        name = f"sample_size_{outcome}.png"
        figure.savefig(config.figures / name, dpi=110)
        plt.close(figure)
        created.append(name)

        ratios = list(config.cost_to_value_ratios)
        figure, axes = plt.subplots(1, len(ratios), figsize=(3.3 * len(ratios), 3.6), sharey=False)
        for axis, ratio in zip(axes, ratios):
            key = f"{ratio:g}"
            for family in FAMILIES:
                values = results["profit_curves"][outcome][family][key]
                axis.plot(fractions, values, label=FAMILY_LABEL[family])
            axis.axhline(0.0, color="0.4", linewidth=0.8)
            axis.set_title(f"c/v = {key}")
            axis.set_xlabel("fraction targeted")
        axes[0].set_ylabel("profit per unit value")
        axes[0].legend(fontsize=7)
        figure.suptitle(f"Conditional profit, test split, outcome {outcome}")
        figure.tight_layout()
        name = f"profit_{outcome}.png"
        figure.savefig(config.figures / name, dpi=110)
        plt.close(figure)
        created.append(name)
    return created
