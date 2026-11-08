"""Smoke check that fits an XGBoost model and reports the device used.

Run with ``python -m parley_ml.gpu_check``. A GPU fit is attempted first;
when CUDA is unavailable the check falls back to CPU and says so. Pass
``--require-cuda`` (or set ``PARLEY_REQUIRE_CUDA=1``) to require the GPU:
then a CPU fallback is a failure and the process exits non-zero. The
process also exits non-zero for real failures such as a broken library
or a training error.
"""

from __future__ import annotations

import argparse
import json
import os
import sys

import numpy as np
import xgboost as xgb


def _effective_device(booster: xgb.Booster) -> str:
    """Device the booster actually trained on.

    Asking for CUDA does not fail when no GPU is visible: XGBoost warns
    and trains on CPU instead, so the reported device comes from the
    trained model, not from the request.
    """
    config = json.loads(booster.save_config())
    device = config.get("learner", {}).get("generic_param", {}).get("device", "")
    return "cuda" if str(device).startswith("cuda") else "cpu"


def _train(device: str, X: np.ndarray, y: np.ndarray) -> str:
    dtrain = xgb.DMatrix(X, label=y)
    params = {
        "objective": "binary:logistic",
        "tree_method": "hist",
        "device": device,
        "max_depth": 2,
        "eval_metric": "logloss",
    }
    booster = xgb.train(params, dtrain, num_boost_round=4)
    booster.predict(dtrain)
    return _effective_device(booster)


def run_smoke_check(require_cuda: bool = False) -> str:
    """Fit a tiny model, trying the GPU first. Returns the device used."""
    rng = np.random.default_rng(0)
    X = rng.normal(size=(64, 4))
    y = (X[:, 0] + X[:, 1] > 0).astype(int)
    used: str | None = None
    try:
        used = _train("cuda", X, y)
    except Exception as exc:
        print(f"CUDA device unavailable ({exc})")
    if used == "cuda":
        return "cuda"
    if require_cuda:
        raise RuntimeError("CUDA device required but training ran on CPU")
    print("CUDA device unavailable; falling back to CPU")
    if used is None:
        _train("cpu", X, y)
    return "cpu"


def _require_cuda_flag() -> bool:
    return os.environ.get("PARLEY_REQUIRE_CUDA", "").lower() in {"1", "true", "yes"}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Fit a tiny model and report the training device."
    )
    parser.add_argument(
        "--require-cuda",
        action="store_true",
        help="fail (exit non-zero) when CUDA is unavailable instead of using CPU",
    )
    args = parser.parse_args(argv)
    require_cuda = args.require_cuda or _require_cuda_flag()
    print(f"xgboost {xgb.__version__}")
    try:
        device = run_smoke_check(require_cuda=require_cuda)
    except RuntimeError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1
    print(f"device: {device}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
