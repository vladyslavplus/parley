"""Semi-synthetic ``crm_uplift`` fixture (pipeline validation, not an effect estimate).

Covariates are real: the eight feature-schema-1 fields, computed as of T0 from demo orders before T0. Baseline outcomes
are real: a purchase in (T0, T0 + baseline window]. The only synthetic part is the treated outcome, drawn from the
effect function tau(x) in ``configs/crm_uplift_demo.yaml``. Arms come from the shared holdout algorithm. Nothing here
is an estimate of the effect of an email.
"""

from __future__ import annotations

import hashlib
import uuid
from dataclasses import dataclass
from pathlib import Path
from typing import Final

import numpy as np
import pandas as pd
import yaml

from parley_ml.experiments.demo_effect import ContactFeatures, EffectConfig, load_effect_config, segment_of, tau, treated_outcome
from parley_ml.experiments.holdout import ARM_HOLDOUT, assign_arm
from parley_ml.features.crm import FEATURE_FIELDS, eligible_contacts, feature_frame
from parley_ml.features.customer_metrics import ORDER_COLUMNS

CONTACT_KIND: Final = "contact"  # the kind string the backend demo seed hashes into contact ids
SPLITS: Final[tuple[str, ...]] = ("train", "validation", "test")
ARM_TREATED: Final = 1
ARM_CONTROL: Final = 0
LABEL: Final = "pipeline validation on semi-synthetic data, not an effect estimate"


@dataclass(frozen=True)
class FixtureSettings:
    effect: EffectConfig
    t0: pd.Timestamp
    baseline_days: int
    holdout_share_bps: int
    assignment_seed: str
    outcome_seed: int
    split_seed: int
    validation_share: float
    test_share: float
    demo_export_dir: Path


def load_fixture_settings(config_path: Path, root: Path) -> FixtureSettings:
    raw = yaml.safe_load(config_path.read_text(encoding="utf-8"))
    fixture = raw["fixture"]
    split = raw["split"]
    validation_share = float(split["validation_share"])
    test_share = float(split["test_share"])
    if not 0.0 < validation_share < 1.0 or not 0.0 < test_share < 1.0 or validation_share + test_share >= 1.0:
        raise ValueError("split shares must be positive and leave a training share")
    return FixtureSettings(
        effect=load_effect_config(config_path),
        t0=pd.Timestamp(str(fixture["t0"]), tz="UTC"),
        baseline_days=int(fixture["baseline_window_days"]),
        holdout_share_bps=int(fixture["holdout_share_bps"]),
        assignment_seed=str(fixture["assignment_seed"]),
        outcome_seed=int(fixture["outcome_seed"]),
        split_seed=int(split["seed"]),
        validation_share=validation_share,
        test_share=test_share,
        demo_export_dir=root / str(raw["artifact"]["demo_export_dir"]),
    )


def demo_contact_id(customer_ref: str) -> str:
    """Contact id the backend demo seed derives: MD5 of ``contact:<customer ref>`` rendered as a UUID.

    Mirrors ``DemoIdentifiers.Derive`` in the Crm module, so the id here is the id the service receives.
    """
    digest = hashlib.md5(f"{CONTACT_KIND}:{customer_ref}".encode("utf-8")).hexdigest()
    return str(uuid.UUID(hex=digest))


def load_demo_orders(export_dir: Path) -> pd.DataFrame:
    """Cleaned orders in the ``ORDER_COLUMNS`` layout; ``contact_id`` holds the customer reference (text)."""
    raw = pd.read_csv(export_dir / "orders.csv.gz", dtype={"invoice_no": str, "customer_id": str})
    orders = pd.DataFrame(
        {
            "order_id": raw["invoice_no"],
            "contact_id": raw["customer_id"],
            "ordered_at": pd.to_datetime(raw["ordered_at"], utc=True),
            "total_amount": raw["total_amount"].astype(float),
            "is_cancellation": raw["is_cancellation"].astype(int) == 1,
        }
    )
    return orders[list(ORDER_COLUMNS)]


def load_customer_refs(export_dir: Path) -> list[str]:
    return pd.read_csv(export_dir / "contacts.csv.gz", dtype={"customer_id": str})["customer_id"].tolist()


def build_fixture(orders: pd.DataFrame, customer_refs: list[str], settings: FixtureSettings) -> pd.DataFrame:
    """One row per eligible customer, sorted by contact id. Columns: contact_id, the eight features, the true segment,
    tau, arm (1 treated, 0 control), y0, y1 (simulated treated outcome), outcome (observed), split."""
    features = eligible_contacts(feature_frame(orders, customer_refs, [], cutoff_instant(settings)))
    purchases = orders.loc[~orders["is_cancellation"]]
    window = purchases.loc[
        (purchases["ordered_at"] > settings.t0)
        & (purchases["ordered_at"] <= settings.t0 + pd.Timedelta(days=settings.baseline_days))
    ]
    bought_after = set(window["contact_id"])

    rows = []
    for customer_ref, row in features.iterrows():
        contact = demo_contact_id(str(customer_ref))
        context = ContactFeatures(**{name: float(row[name]) for name in FEATURE_FIELDS})
        rows.append(
            {
                "contact_id": contact,
                "customer_ref": str(customer_ref),
                **{name: float(row[name]) for name in FEATURE_FIELDS},
                "segment": segment_of(context, settings.effect),
                "tau": tau(context, settings.effect),
                "arm": ARM_TREATED
                if assign_arm(settings.assignment_seed, contact, settings.holdout_share_bps) != ARM_HOLDOUT
                else ARM_CONTROL,
                "y0": int(str(customer_ref) in bought_after),
            }
        )
    frame = pd.DataFrame(rows).sort_values("contact_id", kind="mergesort").reset_index(drop=True)

    draws = np.random.default_rng(settings.outcome_seed).random(len(frame))
    frame["y1"] = [
        treated_outcome(int(y0), float(effect), float(draw))
        for y0, effect, draw in zip(frame["y0"], frame["tau"], draws, strict=True)
    ]
    frame["outcome"] = np.where(frame["arm"] == ARM_TREATED, frame["y1"], frame["y0"]).astype(int)
    frame["split"] = _stratified_split(frame, settings)
    return frame


def _stratified_split(frame: pd.DataFrame, settings: FixtureSettings) -> list[str]:
    """Customer-level split, stratified by arm and observed outcome, with a fixed seed."""
    rng = np.random.default_rng(settings.split_seed)
    assigned = np.array(["train"] * len(frame), dtype=object)
    for _, group in frame.groupby(["arm", "outcome"], sort=True):
        indices = np.array(group.index.to_numpy(), dtype=np.int64)
        rng.shuffle(indices)
        validation_count = round(len(indices) * settings.validation_share)
        test_count = round(len(indices) * settings.test_share)
        assigned[indices[:validation_count]] = "validation"
        assigned[indices[validation_count : validation_count + test_count]] = "test"
    return assigned.tolist()


def fixture_summary(frame: pd.DataFrame) -> dict[str, object]:
    arms = {
        "treated": int((frame["arm"] == ARM_TREATED).sum()),
        "control": int((frame["arm"] == ARM_CONTROL).sum()),
    }
    rates = {
        "treated_observed_rate": float(frame.loc[frame["arm"] == ARM_TREATED, "outcome"].mean()),
        "control_observed_rate": float(frame.loc[frame["arm"] == ARM_CONTROL, "outcome"].mean()),
    }
    splits = {name: int((frame["split"] == name).sum()) for name in SPLITS}
    segments = frame["segment"].value_counts().sort_index().to_dict()
    return {"customers": len(frame), "arms": arms, "rates": rates, "splits": splits, "segments": segments}


def feature_matrix(frame: pd.DataFrame) -> np.ndarray:
    return frame[list(FEATURE_FIELDS)].to_numpy(dtype=np.float32)


def cutoff_instant(settings: FixtureSettings) -> pd.Timestamp:
    """Last instant whose orders count as history: one second before T0."""
    return settings.t0 - pd.Timedelta(seconds=1)
