"""Write the CLV artifact: MAP parameters and a manifest, read by ``parley_ml.serving.clv``."""

from __future__ import annotations

import importlib.metadata
from datetime import date
from pathlib import Path
from typing import Any, Final

from parley_ml.datasets.checksums import sha256_file, write_json
from parley_ml.serving.clv import MANIFEST_FILE, BetaGeoParameters, GammaGammaParameters

MODEL_NAME: Final = "clv_bgnbd_gg"
FEATURE_SCHEMA_VERSION: Final = 1
PARAMETERS_FILE: Final = "parameters.json"
HASH_CHARS: Final = 7
# Mapping from CRM customer metrics to the BG/NBD and Gamma-Gamma inputs, in days.
FEATURE_MAPPING: Final[dict[str, str]] = {
    "frequency": "purchase_day_count - 1 (repeat purchase days)",
    "recency_days_since_first": "last_order_at - first_order_at, in days",
    "tenure_days": "as_of - first_order_at, in days",
    "monetary_value": (
        "avg_repeat_order_value (mean daily net spend over repeat purchase days). Customers without repeats, or with a "
        "mean repeat value at or below 0, get the population mean spend v * p / (q - 1)"
    ),
}


def write_artifact(
    root: Path,
    fit: dict[str, Any],
    transaction_parameters: BetaGeoParameters,
    spend_parameters: GammaGammaParameters,
    version_date: date,
    horizon_days: int,
    evaluation: dict[str, Any],
    evaluation_run: str,
) -> Path:
    """Write ``<root>/clv_bgnbd_gg-<yyyymmdd>-<config hash>/`` and return the directory."""
    config_hash = str(fit["config_sha256"])[:HASH_CHARS]
    version = f"{MODEL_NAME}-{version_date:%Y%m%d}-{config_hash}"
    directory = root / version
    directory.mkdir(parents=True, exist_ok=True)

    parameters = {
        "bg_nbd": {
            "alpha": transaction_parameters.alpha,
            "r": transaction_parameters.r,
            "a": transaction_parameters.a,
            "b": transaction_parameters.b,
        },
        "gamma_gamma": {"p": spend_parameters.p, "q": spend_parameters.q, "v": spend_parameters.v},
    }
    write_json(directory / PARAMETERS_FILE, parameters)

    manifest = {
        "schema_version": 1,
        "model_name": MODEL_NAME,
        "trained_at": version_date.isoformat(),
        "version": version,
        "kind": "clv",
        "feature_schema_version": FEATURE_SCHEMA_VERSION,
        "provenance": "public_dataset",
        "training_data": {
            "dataset": fit["dataset"]["name"],
            "creator": "Daqing Chen",
            "doi": "10.24432/C5CG6D",
            "license": "CC BY 4.0",
            "attribution": "Chen, Daqing. Online Retail II [Dataset]. UCI Machine Learning Repository, 2012. https://doi.org/10.24432/C5CG6D",
            "transactions_sha256": fit["dataset"]["transactions_sha256"],
            "calibration_end": fit["counts"]["split"]["calibration_end"],
            "holdout_end": fit["counts"]["split"]["holdout_end"],
            "fit_run_id": fit["run_id"],
            "fit_config_sha256": fit["config_sha256"],
        },
        "method": {
            "fit": "MAP (PyMC-Marketing fit method map), library default priors",
            "closed_form": "parley_ml.serving.clv: BG/NBD expected purchases and P(alive); Gamma-Gamma expected spend",
            "horizon_days_default": horizon_days,
            "value_definition": "expected purchase days over the horizon times expected spend, undiscounted",
        },
        "feature_mapping": FEATURE_MAPPING,
        "parameters_file": PARAMETERS_FILE,
        "parameters_sha256": sha256_file(directory / PARAMETERS_FILE),
        "evaluation": {
            "run": evaluation_run,
            "population": evaluation["population"],
            "exclusion": evaluation["exclusion"],
            "metrics": evaluation["metrics"],
            "intervals": evaluation["metrics_intervals"],
            "calibration": evaluation["calibration"],
        },
        "libraries": {
            "pymc_marketing": importlib.metadata.version("pymc-marketing"),
            "numpy": evaluation["libraries"]["numpy"],
            "pandas": evaluation["libraries"]["pandas"],
        },
    }
    write_json(directory / MANIFEST_FILE, manifest)
    return directory
