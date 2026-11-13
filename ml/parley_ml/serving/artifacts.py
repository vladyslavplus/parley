"""Artifact registry for the scoring service.

Each artifact is a directory ``<root>/<version>/`` holding ``manifest.json`` and the files it lists. Manifest schema 2:
``schema_version``, ``model_name``, ``version``, ``kind`` (``clv`` or ``uplift``), ``feature_schema_version``,
``provenance``, ``trained_at``, ``metrics`` and ``files`` (relative path -> sha256 of the file). Every listed file must
match its checksum before the model is loaded. A model that fails a check is reported as unavailable with a stable
code and is never loaded; the other models keep working.
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Final

import xgboost as xgb

from parley_ml.datasets.checksums import sha256_file
from parley_ml.serving.clv import BetaGeoParameters, GammaGammaParameters
from parley_ml.serving.schemas import UPLIFT_FEATURE_ORDER

MANIFEST_FILE: Final = "manifest.json"
MANIFEST_SCHEMA_VERSION: Final = 2
KIND_CLV: Final = "clv"
KIND_UPLIFT: Final = "uplift"
KINDS: Final[tuple[str, ...]] = (KIND_CLV, KIND_UPLIFT)
# Provenance of the generated placeholder. It is served only when no trained model is available for the kind.
PROVENANCE_STUB: Final = "stub"
# Research-only provenance is never served. Criteo-trained models stay out of the service by rule, not by convention.
SERVED_PROVENANCES: Final[tuple[str, ...]] = ("public_dataset", "semi_synthetic", PROVENANCE_STUB)
REQUIRED_MANIFEST_KEYS: Final[tuple[str, ...]] = (
    "schema_version",
    "model_name",
    "version",
    "kind",
    "feature_schema_version",
    "provenance",
    "trained_at",
    "files",
)
UPLIFT_FILES: Final[tuple[str, str]] = ("treated.ubj", "control.ubj")
CLV_FILES: Final[tuple[str, ...]] = ("parameters.json",)
# Segment thresholds, in the manifest. Mapping (first match): uplift below the do-not-disturb threshold ->
# do_not_disturb; uplift at or above the persuadable threshold -> persuadable; control rate at or above the sure-thing
# threshold -> sure_thing; otherwise lost_cause.
UPLIFT_RULE_KEYS: Final[tuple[str, ...]] = (
    "do_not_disturb_uplift_below",
    "persuadable_uplift_at_least",
    "sure_thing_control_rate_at_least",
)

# Stable model status codes (reported in /models, never in score errors).
CODE_MANIFEST_INVALID: Final = "ml.model.manifest_invalid"
CODE_SCHEMA_UNSUPPORTED: Final = "ml.model.schema_unsupported"
CODE_FILE_MISSING: Final = "ml.model.file_missing"
CODE_CHECKSUM_MISMATCH: Final = "ml.model.checksum_mismatch"
CODE_LOAD_FAILED: Final = "ml.model.load_failed"
CODE_PROVENANCE_NOT_SERVED: Final = "ml.model.provenance_not_served"
CODE_FEATURE_ORDER_MISMATCH: Final = "ml.model.feature_order_mismatch"


class ArtifactError(Exception):
    """A model directory cannot be served. ``code`` is one of the ``ml.model.*`` status codes."""

    def __init__(self, code: str, detail: str) -> None:
        super().__init__(detail)
        self.code = code
        self.detail = detail


@dataclass(frozen=True)
class UpliftPayload:
    treated: xgb.Booster
    control: xgb.Booster
    rules: dict[str, float]


@dataclass(frozen=True)
class ClvPayload:
    transactions: BetaGeoParameters
    spend: GammaGammaParameters


@dataclass(frozen=True)
class ModelInfo:
    model_name: str
    version: str
    kind: str
    feature_schema_version: int
    provenance: str
    trained_at: str
    metrics: dict[str, Any]
    status: str  # "available" or "unavailable"
    error_code: str | None
    payload: UpliftPayload | ClvPayload | None = field(default=None, repr=False)

    @property
    def available(self) -> bool:
        return self.status == "available" and self.payload is not None


@dataclass(frozen=True)
class Registry:
    models: tuple[ModelInfo, ...]

    def available(self, kind: str) -> list[ModelInfo]:
        return [m for m in self.models if m.kind == kind and m.available]

    def find(self, version: str) -> ModelInfo | None:
        return next((m for m in self.models if m.version == version), None)

    def default(self, kind: str) -> ModelInfo | None:
        """Newest trained model of the kind (by trained_at, then version); a stub only when nothing trained exists."""
        candidates = self.available(kind)
        if not candidates:
            return None
        trained = [m for m in candidates if m.provenance != PROVENANCE_STUB]
        pool = trained or candidates
        return max(pool, key=lambda m: (m.trained_at, m.version))

    def loaded_count(self) -> int:
        return sum(1 for m in self.models if m.available)


def _read_manifest(directory: Path) -> dict[str, Any]:
    try:
        manifest = json.loads((directory / MANIFEST_FILE).read_text(encoding="utf-8"))
    except (OSError, ValueError) as error:
        raise ArtifactError(CODE_MANIFEST_INVALID, f"manifest unreadable: {type(error).__name__}") from error
    if not isinstance(manifest, dict):
        raise ArtifactError(CODE_MANIFEST_INVALID, "manifest is not an object")
    missing = [key for key in REQUIRED_MANIFEST_KEYS if key not in manifest]
    if missing:
        raise ArtifactError(CODE_MANIFEST_INVALID, f"manifest misses {missing}")
    if manifest["schema_version"] != MANIFEST_SCHEMA_VERSION:
        raise ArtifactError(CODE_SCHEMA_UNSUPPORTED, f"manifest schema {manifest['schema_version']!r} is not served")
    if manifest["kind"] not in KINDS:
        raise ArtifactError(CODE_MANIFEST_INVALID, f"unknown kind {manifest['kind']!r}")
    if manifest["provenance"] not in SERVED_PROVENANCES:
        raise ArtifactError(CODE_PROVENANCE_NOT_SERVED, f"provenance {manifest['provenance']!r} is not served")
    return manifest


def _verified_path(directory: Path, relative: str, expected_sha256: str) -> Path:
    path = Path(relative)
    if path.is_absolute() or ".." in path.parts or path.name != relative:
        raise ArtifactError(CODE_MANIFEST_INVALID, "manifest lists a file outside its directory")
    target = directory / relative
    if not target.is_file():
        raise ArtifactError(CODE_FILE_MISSING, f"listed file {relative} is missing")
    if sha256_file(target) != expected_sha256:
        raise ArtifactError(CODE_CHECKSUM_MISMATCH, f"checksum of {relative} does not match the manifest")
    return target


def _listed_files(manifest: dict[str, Any], required: tuple[str, ...]) -> dict[str, str]:
    files = manifest["files"]
    if not isinstance(files, dict) or not all(isinstance(v, str) for v in files.values()):
        raise ArtifactError(CODE_MANIFEST_INVALID, "files must map names to sha256 strings")
    missing = [name for name in required if name not in files]
    if missing:
        raise ArtifactError(CODE_MANIFEST_INVALID, f"manifest files misses {missing}")
    return files


def _load_clv(directory: Path, manifest: dict[str, Any]) -> ClvPayload:
    files = _listed_files(manifest, CLV_FILES)
    verified = {name: _verified_path(directory, name, digest) for name, digest in files.items()}
    try:
        parameters = json.loads(verified["parameters.json"].read_text(encoding="utf-8"))
        bg = parameters["bg_nbd"]
        gg = parameters["gamma_gamma"]
        return ClvPayload(
            transactions=BetaGeoParameters(alpha=bg["alpha"], r=bg["r"], a=bg["a"], b=bg["b"]),
            spend=GammaGammaParameters(p=gg["p"], q=gg["q"], v=gg["v"]),
        )
    except (KeyError, TypeError, ValueError) as error:
        raise ArtifactError(CODE_LOAD_FAILED, f"parameters unusable: {type(error).__name__}") from error


def _load_uplift(directory: Path, manifest: dict[str, Any]) -> UpliftPayload:
    files = _listed_files(manifest, UPLIFT_FILES)
    feature_order = manifest.get("feature_order")
    if not isinstance(feature_order, list) or [str(name) for name in feature_order] != list(UPLIFT_FEATURE_ORDER):
        raise ArtifactError(
            CODE_FEATURE_ORDER_MISMATCH,
            f"feature_order must be {list(UPLIFT_FEATURE_ORDER)}",
        )
    rules = manifest.get("segment_rules")
    if not isinstance(rules, dict) or any(key not in rules for key in UPLIFT_RULE_KEYS):
        raise ArtifactError(CODE_MANIFEST_INVALID, f"segment_rules must define {list(UPLIFT_RULE_KEYS)}")
    paths = {name: _verified_path(directory, name, files[name]) for name in UPLIFT_FILES}
    try:
        treated = xgb.Booster()
        treated.load_model(str(paths["treated.ubj"]))
        control = xgb.Booster()
        control.load_model(str(paths["control.ubj"]))
        treated.set_param({"device": "cpu"})
        control.set_param({"device": "cpu"})
    except (xgb.core.XGBoostError, OSError, ValueError) as error:
        raise ArtifactError(CODE_LOAD_FAILED, "booster file could not be loaded") from error
    return UpliftPayload(treated=treated, control=control, rules={k: float(rules[k]) for k in UPLIFT_RULE_KEYS})


def load_model_directory(directory: Path) -> ModelInfo:
    """Verify and load one artifact directory. Raises ``ArtifactError`` when it cannot be served."""
    manifest = _read_manifest(directory)
    kind = manifest["kind"]
    payload = _load_clv(directory, manifest) if kind == KIND_CLV else _load_uplift(directory, manifest)
    metrics = manifest.get("metrics")
    if metrics is None:
        metrics = manifest.get("evaluation", {}).get("metrics", {})
    return ModelInfo(
        model_name=str(manifest["model_name"]),
        version=str(manifest["version"]),
        kind=kind,
        feature_schema_version=int(manifest["feature_schema_version"]),
        provenance=str(manifest["provenance"]),
        trained_at=str(manifest["trained_at"]),
        metrics=dict(metrics),
        status="available",
        error_code=None,
        payload=payload,
    )


def _unavailable(directory: Path, error: ArtifactError) -> ModelInfo:
    try:
        manifest = json.loads((directory / MANIFEST_FILE).read_text(encoding="utf-8"))
        return ModelInfo(
            model_name=str(manifest.get("model_name", "unknown")),
            version=str(manifest.get("version", directory.name)),
            kind=str(manifest.get("kind", "unknown")),
            feature_schema_version=int(manifest.get("feature_schema_version", 0)),
            provenance=str(manifest.get("provenance", "unknown")),
            trained_at=str(manifest.get("trained_at", "")),
            metrics={},
            status="unavailable",
            error_code=error.code,
        )
    except (OSError, ValueError, TypeError, AttributeError):
        return ModelInfo(
            model_name="unknown",
            version=directory.name,
            kind="unknown",
            feature_schema_version=0,
            provenance="unknown",
            trained_at="",
            metrics={},
            status="unavailable",
            error_code=error.code,
        )


def _refuse_criteo_path(path: Path) -> None:
    """Criteo data never reaches the serving path, whatever the manifest says."""
    if any("criteo" in part.lower() for part in path.resolve().parts):
        raise ValueError("the scoring service never reads Criteo paths")


def load_registry(root: Path) -> Registry:
    """Load every ``<root>/<version>/`` directory. A directory that fails is listed as unavailable, never skipped."""
    _refuse_criteo_path(root)
    models: list[ModelInfo] = []
    if root.is_dir():
        for directory in sorted(p for p in root.iterdir() if p.is_dir()):
            if not (directory / MANIFEST_FILE).exists():
                continue
            try:
                models.append(load_model_directory(directory))
            except ArtifactError as error:
                models.append(_unavailable(directory, error))
            except (KeyError, TypeError, ValueError, OSError) as error:
                # A manifest that is readable but malformed. Listed as unavailable; the other models still load.
                unexpected = ArtifactError(CODE_MANIFEST_INVALID, f"manifest unusable: {type(error).__name__}")
                models.append(_unavailable(directory, unexpected))
    return Registry(models=tuple(models))
