"""Final RQ1 evaluation on the test split.

Modes:
- default: fit the winners with every seed, write the freeze file, open the test split, write results. This was the
  first attempt. It is refused once the access record exists.
- ``--test-from-saved``: load the frozen winners from the saved boosters and the freeze file, check the freeze checksum
  recorded by the first attempt, open the test split, write results. Nothing is refitted.
- ``--test-from-saved --reproduce``: the same run into ``artifacts/final/reproduction``; every committed output is then
  compared byte for byte with the regenerated copy and the result is written to ``artifacts/final/reproduction.json``.
- ``--render-only``: rewrite the Markdown from the saved results. Reads no test rows.

Every mode that opens the test split first appends an attempt to ``artifacts/final/test_access.json``.
"""

from __future__ import annotations

import argparse
import dataclasses
import json
import subprocess
import sys
import time
from pathlib import Path
from typing import Final

from parley_ml.datasets.checksums import sha256_bytes, write_json
from parley_ml.uplift.config import FIT_SPLITS, ML_ROOT, FamilyConfig, load_family_config
from parley_ml.uplift.data import DataBundle, load_bundle, load_test_split
from parley_ml.uplift.final import (
    FAMILIES,
    FREEZE_FILE,
    RESULTS_FILE,
    FinalConfig,
    access_log_exists,
    fit_winners,
    freeze_bytes,
    load_final_config,
    load_saved_winners,
    provenance,
    read_access,
    record_attempt,
    score_chunked,
    test_phase,
    update_attempt,
    validation_phase,
    validation_scores_for,
    verify_saved_models,
    verify_winners,
)
from parley_ml.uplift.report import render

DEFAULT_CONFIG: Final = ML_ROOT / "configs" / "uplift_final.yaml"
MIN_FREE_GIB: Final = 6.0
REPRODUCTION_DIR: Final = "reproduction"
_CREATE_NO_WINDOW: Final = 0x08000000
OUTPUT_NAMES: Final[tuple[str, ...]] = (
    "rq1_test_metrics.csv",
    "rq1_intervals.csv",
    "rq1_sample_size.csv",
    "rq1_profit.csv",
    "rq1_profit_curves.csv",
    "rq1_qini_curves.csv",
    "rq1_uplift.md",
)


def free_memory_gib() -> float:
    completed = subprocess.run(
        ["powershell", "-NoProfile", "-Command", "(Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory"],
        capture_output=True,
        text=True,
        creationflags=_CREATE_NO_WINDOW,
        check=True,
    )
    return int(completed.stdout.strip()) / (1024 * 1024)


def figure_names(config: FinalConfig) -> tuple[str, ...]:
    names: list[str] = []
    for outcome in config.winners:
        names += [f"qini_{outcome}.png", f"sample_size_{outcome}.png", f"profit_{outcome}.png"]
    return tuple(names)


def run_test_from_saved(config: FinalConfig, family_configs: dict[str, FamilyConfig], reproduce: bool) -> int:
    final_root = config.final_root
    access = read_access(final_root)
    first = next((a for a in access["attempts"] if a["attempt"] == 1), None)
    if first is None or "freeze_sha256" not in first:
        raise SystemExit("The access record has no first attempt with a freeze checksum; refusing to continue.")

    freeze_raw = (final_root / FREEZE_FILE).read_bytes()
    freeze_sha = sha256_bytes(freeze_raw)
    if freeze_sha != first["freeze_sha256"]:
        raise SystemExit(f"freeze.json sha256 {freeze_sha} differs from the first attempt's {first['freeze_sha256']}.")
    freeze = json.loads(freeze_raw)
    if freeze["config_sha256"] != config.config_sha256:
        raise SystemExit("The freeze file was made from a different final config; refusing to continue.")

    if reproduce:
        out_root = final_root / REPRODUCTION_DIR
        results_dir = out_root / "results"
        figures_dir = out_root / "figures"
        committed = {name: (config.results_dir / name).read_bytes() for name in OUTPUT_NAMES}
        committed.update({name: (config.figures / name).read_bytes() for name in figure_names(config)})
        committed[RESULTS_FILE] = (final_root / RESULTS_FILE).read_bytes()
    else:
        if (final_root / RESULTS_FILE).exists():
            raise SystemExit("results.json already exists; a new test-split opening is refused. Use --reproduce.")
        out_root = final_root
        results_dir = config.results_dir
        figures_dir = config.figures
        committed = {}
    out_root.mkdir(parents=True, exist_ok=True)
    out_config = dataclasses.replace(config, results_dir=results_dir, figures=figures_dir)

    validation = load_bundle(family_configs[FAMILIES[0]], names=("validation",))["validation"]
    integrity = verify_saved_models(config, freeze, freeze_sha, family_configs, validation)
    if not integrity["passed"]:
        raise SystemExit(f"Saved models do not reproduce the frozen validation metrics: {integrity['mismatches']}. "
                         "Nothing was opened.")
    print(f"integrity check passed: {integrity['models']} models, largest relative difference "
          f"{integrity['largest_relative_difference']:.3g}", flush=True)

    free = free_memory_gib()
    if free < MIN_FREE_GIB:
        raise SystemExit(f"Only {free:.1f} GiB free; the test phase needs {MIN_FREE_GIB:.0f} GiB. Nothing was opened.")

    mode = "reproduction" if reproduce else "test_from_saved"
    number = record_attempt(
        final_root,
        {"mode": mode, "opened_test_split": True, "outcome": "started", "free_memory_gib": round(free, 2)},
    )
    print(f"attempt {number} ({mode}) recorded before reading the test split; free memory {free:.1f} GiB", flush=True)
    started = time.perf_counter()

    fitted = load_saved_winners(config, freeze)
    test_split = load_test_split(family_configs[FAMILIES[0]])
    bundle = DataBundle(splits={"test": test_split}, checksums={}, subsample=None)
    scores = {key: score_chunked(winner.learner, test_split.features) for key, winner in fitted.items()}
    results = test_phase(out_config, family_configs, fitted, bundle, freeze, scores)
    header = {**provenance(config), "config_sha256": config.config_sha256, "freeze_sha256": freeze_sha}
    full = {"schema_version": 1, "header": header, **results}
    (out_root / RESULTS_FILE).write_text(
        json.dumps(full, indent=2, sort_keys=True, default=float) + "\n", encoding="utf-8", newline="\n"
    )
    saved = json.loads((out_root / RESULTS_FILE).read_text(encoding="utf-8"))
    render(out_config, saved, freeze, verify_winners(config), saved["header"], config.notes.read_text(encoding="utf-8"))
    wall = round(time.perf_counter() - started, 1)
    update_attempt(final_root, number, {"outcome": "completed", "results_written": True, "wall_seconds": wall})
    print(f"test phase completed in {wall} s", flush=True)

    if not reproduce:
        return 0
    return compare_reproduction(config, number, committed=committed)


def compare_reproduction(config: FinalConfig, attempt: int, committed: dict[str, bytes]) -> int:
    """Compare the regenerated outputs of the reproduction run with the committed outputs, byte for byte."""
    final_root = config.final_root
    regenerated = {name: (config.final_root / REPRODUCTION_DIR / "results" / name).read_bytes() for name in OUTPUT_NAMES}
    regenerated.update({name: (config.final_root / REPRODUCTION_DIR / "figures" / name).read_bytes() for name in figure_names(config)})
    regenerated[RESULTS_FILE] = (config.final_root / REPRODUCTION_DIR / RESULTS_FILE).read_bytes()
    report = {name: regenerated[name] == before for name, before in committed.items()}
    identical = all(report.values())
    record_path = final_root / "reproduction.json"
    history = json.loads(record_path.read_text(encoding="utf-8")) if record_path.exists() else {}
    write_json(record_path, {"identical": identical, "attempt": attempt, "files": report, "history": history})
    print(f"reproduction identical: {identical}", flush=True)
    return 0 if identical else 1


def run_compare_reproduction(config: FinalConfig, family_configs: dict[str, FamilyConfig]) -> int:
    """Re-render the reproduction folder from its saved results.json (no test rows) and compare every output.

    Used after a rendering fix: the computed results are unchanged, so only the rendering is repeated.
    """
    final_root = config.final_root
    reproduction = final_root / REPRODUCTION_DIR
    saved = json.loads((reproduction / RESULTS_FILE).read_text(encoding="utf-8"))
    freeze = json.loads((final_root / FREEZE_FILE).read_text(encoding="utf-8"))
    out_config = dataclasses.replace(config, results_dir=reproduction / "results", figures=reproduction / "figures")
    render(out_config, saved, freeze, verify_winners(config), saved["header"], config.notes.read_text(encoding="utf-8"))
    committed = {name: (config.results_dir / name).read_bytes() for name in OUTPUT_NAMES}
    committed.update({name: (config.figures / name).read_bytes() for name in figure_names(config)})
    committed[RESULTS_FILE] = (final_root / RESULTS_FILE).read_bytes()
    attempt = max(a["attempt"] for a in read_access(final_root)["attempts"] if a["mode"] == "reproduction")
    return compare_reproduction(config, attempt, committed=committed)


def run_verify_saved_models(config: FinalConfig, family_configs: dict[str, FamilyConfig]) -> int:
    """Validation only: recompute the frozen validation metrics from the saved boosters and write the model checksums."""
    freeze_raw = (config.final_root / FREEZE_FILE).read_bytes()
    freeze = json.loads(freeze_raw)
    validation = load_bundle(family_configs[FAMILIES[0]], names=("validation",))["validation"]
    integrity = verify_saved_models(config, freeze, sha256_bytes(freeze_raw), family_configs, validation)
    print(json.dumps({k: v for k, v in integrity.items() if k != "model_files"}, indent=2, default=str), flush=True)
    return 0 if integrity["passed"] else 1


def run_render_only(config: FinalConfig) -> int:
    results = json.loads((config.final_root / RESULTS_FILE).read_text(encoding="utf-8"))
    freeze = json.loads((config.final_root / FREEZE_FILE).read_text(encoding="utf-8"))
    render(config, results, freeze, verify_winners(config), results["header"], config.notes.read_text(encoding="utf-8"))
    print("rendered from saved results", flush=True)
    return 0


def run_first_attempt(config: FinalConfig, family_configs: dict[str, FamilyConfig]) -> int:
    """Attempt 1: fit every winner with every seed, write the freeze file, then open the test split."""
    if access_log_exists(config.final_root):
        raise SystemExit("The test split was already opened (test_access.json exists); use --test-from-saved.")
    fit_bundle = load_bundle(family_configs[FAMILIES[0]], names=FIT_SPLITS)
    fitted = fit_winners(config, family_configs, {family: fit_bundle for family in FAMILIES})
    for (outcome, family, seed), winner in fitted.items():
        for name, model in winner.learner.models.items():
            model_dir = config.final_root / "models" / outcome / family / f"seed-{seed}"
            model_dir.mkdir(parents=True, exist_ok=True)
            model.booster.save_model(model_dir / f"{name}.ubj")
    freeze = validation_phase(
        config, family_configs, fitted, validation_scores_for(fitted, fit_bundle["validation"].features), fit_bundle
    )
    new_freeze = freeze_bytes(freeze)
    config.final_root.mkdir(parents=True, exist_ok=True)
    (config.final_root / FREEZE_FILE).write_bytes(new_freeze)
    record_attempt(
        config.final_root,
        {"mode": "fit_freeze_test", "opened_test_split": True, "outcome": "started",
         "freeze_sha256": sha256_bytes(new_freeze)},
    )
    test_bundle = load_bundle(family_configs[FAMILIES[0]], names=("test",), allow_test=True)
    scores = {key: score_chunked(winner.learner, test_bundle["test"].features) for key, winner in fitted.items()}
    results = test_phase(config, family_configs, fitted, test_bundle, freeze, scores)
    header = {**provenance(config), "config_sha256": config.config_sha256, "freeze_sha256": sha256_bytes(new_freeze)}
    full = {"schema_version": 1, "header": header, **results}
    (config.final_root / RESULTS_FILE).write_text(
        json.dumps(full, indent=2, sort_keys=True, default=float) + "\n", encoding="utf-8", newline="\n"
    )
    render(config, full, freeze, verify_winners(config), header, config.notes.read_text(encoding="utf-8"))
    return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Final RQ1 evaluation on the test split.")
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--test-from-saved", action="store_true", help="test phase only, from saved models")
    parser.add_argument("--reproduce", action="store_true", help="with --test-from-saved: re-run and compare bytes")
    parser.add_argument("--render-only", action="store_true", help="rewrite the Markdown from saved results")
    parser.add_argument("--verify-saved-models", action="store_true",
                        help="validation only: recompute frozen validation metrics from the saved boosters")
    parser.add_argument("--compare-reproduction", action="store_true",
                        help="re-render the reproduction from its saved results and compare bytes (reads no test rows)")
    args = parser.parse_args(argv)
    config_path = args.config if args.config.is_absolute() else ML_ROOT / args.config
    config = load_final_config(config_path)
    family_configs = {family: load_family_config(config.family_configs[family]) for family in FAMILIES}

    if args.render_only:
        return run_render_only(config)
    if args.compare_reproduction:
        return run_compare_reproduction(config, family_configs)
    if args.verify_saved_models:
        return run_verify_saved_models(config, family_configs)
    if args.test_from_saved:
        return run_test_from_saved(config, family_configs, reproduce=args.reproduce)
    if args.reproduce:
        raise SystemExit("--reproduce needs --test-from-saved")
    return run_first_attempt(config, family_configs)


if __name__ == "__main__":
    sys.exit(main())
