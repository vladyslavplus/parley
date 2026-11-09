"""Online Retail download: the copy is capped at the expected size and network failures give one clear error."""

from __future__ import annotations

import io
import urllib.error
from pathlib import Path

import pytest

from parley_ml.datasets import online_retail as retail

URL = "https://example.test/online_retail_ii.zip"


def test_download_reads_at_most_one_byte_over_the_expected_size(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setattr(retail.urllib.request, "urlopen", lambda request, timeout: io.BytesIO(b"x" * 5000))
    partial = tmp_path / "archive.part"

    retail._download(URL, partial, expected_bytes=100, user_agent="test", timeout_seconds=5)

    assert partial.stat().st_size == 101


def test_oversized_archive_is_rejected_and_its_partial_file_removed(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setattr(retail.urllib.request, "urlopen", lambda request, timeout: io.BytesIO(b"x" * 5000))
    destination = tmp_path / "online_retail_ii.zip"

    with pytest.raises(retail.DatasetIdentityError, match="size differs"):
        retail.ensure_archive(
            url=URL,
            destination=destination,
            expected_sha256="0" * 64,
            expected_bytes=100,
            user_agent="test",
            timeout_seconds=5,
        )

    assert not destination.exists()
    assert not destination.with_suffix(".part").exists()


def test_network_failure_is_one_clear_error_and_leaves_no_partial_file(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    def refuse(request: object, timeout: int) -> io.BytesIO:
        raise urllib.error.URLError("connection refused")

    monkeypatch.setattr(retail.urllib.request, "urlopen", refuse)
    partial = tmp_path / "archive.part"

    with pytest.raises(retail.DatasetDownloadError, match="connection refused"):
        retail._download(URL, partial, expected_bytes=100, user_agent="test", timeout_seconds=5)

    assert not partial.exists()
