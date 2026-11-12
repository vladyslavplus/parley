"""Test-split access log for the demo pipeline.

The same idea as the Criteo final evaluation (``parley_ml.uplift.final``): an attempt is appended before the test rows
are read. A rerun is allowed only with the same config and an explicit ``reproduce`` request; a changed config is
refused, so the test split cannot be reused while the procedure is being tuned.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Final

from parley_ml.datasets.checksums import write_json

ACCESS_SCHEMA_VERSION: Final = 1
MODE_FIT: Final = "fit"
MODE_REPRODUCE: Final = "reproduce"


class AccessLogError(ValueError):
    """The test split may not be opened by this run."""


def read_access(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {"schema_version": ACCESS_SCHEMA_VERSION, "attempts": [], "openings": 0}
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("schema_version") != ACCESS_SCHEMA_VERSION:
        raise AccessLogError(f"{path}: unsupported access log schema {data.get('schema_version')!r}")
    return data


def check_access(path: Path, config_sha256: str, reproduce: bool) -> None:
    """Refuse a second opening under a changed config, or under the same config without ``reproduce``."""
    if not path.exists():
        return
    data = read_access(path)
    attempts = data.get("attempts", [])
    if not attempts:
        return
    configs = {str(attempt.get("config_sha256")) for attempt in attempts}
    if configs != {config_sha256}:
        raise AccessLogError(
            f"the test split of {path} was opened under a different configuration; "
            "a changed procedure needs a fresh artifact directory, not a reuse of the test split"
        )
    if not reproduce:
        raise AccessLogError(
            f"the test split was already opened under this configuration; rerun with --reproduce for an identical "
            f"reproduction, or use a fresh artifact directory (access log: {path})"
        )


def record_attempt(path: Path, config_sha256: str, reproduce: bool) -> int:
    """Append one attempt before the test rows are read. Returns the attempt number."""
    data = read_access(path)
    number = len(data["attempts"]) + 1
    data["attempts"].append(
        {
            "attempt": number,
            "mode": MODE_REPRODUCE if reproduce else MODE_FIT,
            "config_sha256": config_sha256,
            "opened_test_split": True,
        }
    )
    data["openings"] = sum(1 for attempt in data["attempts"] if attempt.get("opened_test_split"))
    write_json(path, data)
    return number


def update_attempt(path: Path, number: int, fields: dict[str, Any]) -> None:
    data = read_access(path)
    for attempt in data["attempts"]:
        if attempt.get("attempt") == number:
            attempt.update(fields)
    write_json(path, data)
