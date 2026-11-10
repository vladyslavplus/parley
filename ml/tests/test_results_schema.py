"""Schema and consistency checks for the RQ1 results. No data is needed, so this runs in the default suite.

Each check skips with a stated reason when its generated input is absent, so a clean checkout without the results
still passes. When the results are present, every check runs.
"""

from __future__ import annotations

import csv
import json
import re
from pathlib import Path

import pytest

from parley_ml.uplift.config import ML_ROOT
from parley_ml.uplift.final import load_final_config, verify_winners

RESULTS = ML_ROOT / "results"
FINAL_ROOT = ML_ROOT / "artifacts" / "final"
REPORT = RESULTS / "rq1_uplift.md"
GRID_CSV = RESULTS / "uplift_validation_grid.csv"
FINAL_CONFIG = ML_ROOT / "configs" / "uplift_final.yaml"
ALLOWED_RESULT_SUFFIXES = (".csv", ".md", ".yaml", ".svg")

REQUIRED_HEADERS = {
    "rq1_test_metrics.csv": ["outcome", "family", "seed", "metric", "value"],
    "rq1_intervals.csv": ["outcome", "statistic", "point", "ci_low", "ci_high", "replicates", "share_positive"],
    "rq1_sample_size.csv": ["outcome", "sample_size", "rows", "statistic", "point", "ci_low", "ci_high", "replicates"],
    "rq1_profit.csv": ["outcome", "family", "cost_to_value_ratio", "k_star", "profit", "ci_low", "ci_high"],
    "rq1_profit_curves.csv": ["outcome", "family", "cost_to_value_ratio", "fraction", "profit"],
    "rq1_qini_curves.csv": ["outcome", "family", "fraction", "qini", "random"],
}
REQUIRED_SECTIONS = (
    "## Provenance",
    "## Winners fixed on validation",
    "## Notes for the report",
)
# Backlog ids, planning-doc names and the academic-context label must not appear in tracked files (hard rule).
FORBIDDEN = re.compile(r"\bS\d{2}\b|\bB\d\b|\bDG-\d+|\bDC-\d+|\bCMP-\d+|\bINS-\d+|cour[s]ework|docs[/]ignored|09[-]ml[-]plan", re.I)

needs_report = pytest.mark.skipif(not REPORT.exists(), reason="generated RQ1 report is absent; run the test phase")
needs_grid = pytest.mark.skipif(not GRID_CSV.exists(), reason="validation grid CSV is absent; run the grid")
needs_reproduction = pytest.mark.skipif(
    not (FINAL_ROOT / "reproduction.json").exists(), reason="no reproduction run recorded in artifacts/final"
)


def _rows(name: str) -> list[dict[str, str]]:
    with (RESULTS / name).open(encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))


@pytest.mark.parametrize("name", sorted(REQUIRED_HEADERS))
def test_committed_csv_has_the_documented_header(name: str) -> None:
    path = RESULTS / name
    if not path.exists():
        pytest.skip(f"{name} is absent; run the test phase to generate it")
    with path.open(encoding="utf-8", newline="") as handle:
        header = next(csv.reader(handle))
    assert header == REQUIRED_HEADERS[name]


@needs_report
def test_intervals_are_ordered_and_use_the_configured_replicates() -> None:
    for row in _rows("rq1_intervals.csv"):
        assert float(row["ci_low"]) <= float(row["ci_high"]), row
        assert int(row["replicates"]) == 200, row
        assert 0.0 <= float(row["share_positive"]) <= 1.0, row


@needs_report
def test_report_has_the_required_sections_and_no_forbidden_references() -> None:
    text = REPORT.read_text(encoding="utf-8")
    for section in REQUIRED_SECTIONS:
        assert section in text, section
    match = FORBIDDEN.search(text)
    assert match is None, match


@needs_grid
def test_committed_winners_match_the_validation_grid() -> None:
    config = load_final_config(FINAL_CONFIG)
    assert len(verify_winners(config)) == 6


def test_result_files_are_only_text_or_svg() -> None:
    for path in RESULTS.iterdir():
        if path.is_dir() or path.name == ".gitkeep":
            continue
        assert path.suffix in ALLOWED_RESULT_SUFFIXES, path


@needs_reproduction
def test_reproduction_report_says_identical() -> None:
    record = json.loads((FINAL_ROOT / "reproduction.json").read_text(encoding="utf-8"))
    assert record["identical"] is True, record
