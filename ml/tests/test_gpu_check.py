"""Tests for the GPU smoke check fallback logic (no GPU required)."""

import pytest

from parley_ml import gpu_check


def test_cuda_fit_is_preferred(monkeypatch):
    calls = []

    def fake_train(device, X, y):
        calls.append(device)
        return device

    monkeypatch.setattr(gpu_check, "_train", fake_train)
    assert gpu_check.run_smoke_check() == "cuda"
    assert calls == ["cuda"]


def test_falls_back_to_cpu_when_cuda_unavailable(monkeypatch):
    def fake_train(device, X, y):
        if device == "cuda":
            raise RuntimeError("CUDA error: no device")
        return "cpu"

    monkeypatch.setattr(gpu_check, "_train", fake_train)
    assert gpu_check.run_smoke_check() == "cpu"


def test_silent_cpu_fallback_is_not_cuda(monkeypatch):
    # XGBoost does not raise when no GPU is visible; the trained model
    # simply reports the CPU as its device.
    monkeypatch.setattr(gpu_check, "_train", lambda device, X, y: "cpu")
    assert gpu_check.run_smoke_check() == "cpu"


def test_real_errors_propagate(monkeypatch):
    def fake_train(device, X, y):
        raise RuntimeError("training failed")

    monkeypatch.setattr(gpu_check, "_train", fake_train)
    with pytest.raises(RuntimeError, match="training failed"):
        gpu_check.run_smoke_check()


def test_smoke_check_runs_on_either_device():
    assert gpu_check.run_smoke_check() in {"cuda", "cpu"}


def test_require_cuda_skips_cpu_fallback(monkeypatch):
    calls = []

    def fake_train(device, X, y):
        calls.append(device)
        if device == "cuda":
            raise RuntimeError("CUDA error: no device")
        return "cpu"

    monkeypatch.setattr(gpu_check, "_train", fake_train)
    with pytest.raises(RuntimeError, match="CUDA device required"):
        gpu_check.run_smoke_check(require_cuda=True)
    assert calls == ["cuda"]


def test_require_cuda_fails_on_silent_cpu_fallback(monkeypatch):
    monkeypatch.setattr(gpu_check, "_train", lambda device, X, y: "cpu")
    with pytest.raises(RuntimeError, match="CUDA device required"):
        gpu_check.run_smoke_check(require_cuda=True)


def test_main_require_cuda_flag_exits_nonzero(monkeypatch):
    monkeypatch.setenv("PARLEY_REQUIRE_CUDA", "")

    def fake_train(device, X, y):
        raise RuntimeError("CUDA error: no device")

    monkeypatch.setattr(gpu_check, "_train", fake_train)
    assert gpu_check.main(["--require-cuda"]) == 1


def test_main_require_cuda_env_exits_nonzero(monkeypatch):
    monkeypatch.setenv("PARLEY_REQUIRE_CUDA", "1")

    def fake_train(device, X, y):
        raise RuntimeError("CUDA error: no device")

    monkeypatch.setattr(gpu_check, "_train", fake_train)
    assert gpu_check.main([]) == 1
