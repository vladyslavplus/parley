"""Test-split access log: one opening per configuration, a repeat only on request, a changed config refused."""

from __future__ import annotations

from pathlib import Path

import pytest

from parley_ml.uplift.test_access import AccessLogError, check_access, read_access, record_attempt

CONFIG = "a" * 64
OTHER = "b" * 64


def test_a_first_opening_is_allowed_and_recorded(tmp_path: Path) -> None:
    path = tmp_path / "test_access.json"

    check_access(path, CONFIG, reproduce=False)
    attempt = record_attempt(path, CONFIG, reproduce=False)

    data = read_access(path)
    assert attempt == 1
    assert data["openings"] == 1
    assert data["attempts"][0]["config_sha256"] == CONFIG
    assert data["attempts"][0]["mode"] == "fit"


def test_a_second_opening_of_the_same_config_needs_reproduce(tmp_path: Path) -> None:
    path = tmp_path / "test_access.json"
    record_attempt(path, CONFIG, reproduce=False)

    with pytest.raises(AccessLogError, match="--reproduce"):
        check_access(path, CONFIG, reproduce=False)

    check_access(path, CONFIG, reproduce=True)
    record_attempt(path, CONFIG, reproduce=True)
    assert read_access(path)["openings"] == 2


def test_a_changed_config_cannot_reuse_the_test_split(tmp_path: Path) -> None:
    path = tmp_path / "test_access.json"
    record_attempt(path, CONFIG, reproduce=False)

    with pytest.raises(AccessLogError, match="different configuration"):
        check_access(path, OTHER, reproduce=True)
