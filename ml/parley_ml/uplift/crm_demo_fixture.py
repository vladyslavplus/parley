"""Semi-synthetic ``crm_uplift`` fixture (pipeline validation, not an effect estimate).

Covariates are real: the eight feature-schema-1 fields, computed as of T0 from demo orders before T0. Baseline outcomes
are real: a purchase in (T0, T0 + baseline window]. The only synthetic part is the treated outcome, drawn from the
effect function tau(x) in ``configs/crm_uplift_demo.yaml``. Arms come from the shared holdout algorithm. Nothing here
is an estimate of the effect of an email.

The fixture is a panel of snapshots. The last snapshot is primary: it carries the split and the only test rows. Earlier
snapshots are stacked as extra training rows for customers whose primary split is train, which keeps the split at the
customer level. Each snapshot has its own arm assignment and outcome seeds, all in the config.

The true effect of a row is ``tau * (1 - P(y0))`` for positive tau and ``tau * P(y0)`` for negative tau
(:func:`parley_ml.experiments.demo_effect.expected_effect`), with ``P(y0)`` the snapshot's observed baseline purchase
rate. ``tau`` alone would overstate the effect, because a positive effect can only convert a non-buyer.
"""

from __future__ import annotations

import hashlib
import uuid
from dataclasses import dataclass, replace
from pathlib import Path
from typing import Final

import numpy as np
import pandas as pd
import yaml

from parley_ml.experiments.demo_effect import (
    ContactFeatures,
    EffectConfig,
    expected_effect,
    load_effect_config,
    segment_of,
    tau,
    treated_outcome,
)
from parley_ml.experiments.holdout import ARM_HOLDOUT, assign_arm
from parley_ml.features.crm import FEATURE_FIELDS, eligible_contacts, feature_frame
from parley_ml.features.customer_metrics import ORDER_COLUMNS

CONTACT_KIND: Final = "contact"  # the kind string the backend demo seed hashes into contact ids
SPLIT_TRAIN: Final = "train"
SPLIT_VALIDATION_MODEL: Final = "validation_model"
SPLIT_VALIDATION_SELECTION: Final = "validation_selection"
SPLIT_TEST: Final = "test"
SPLITS: Final[tuple[str, ...]] = (SPLIT_TRAIN, SPLIT_VALIDATION_MODEL, SPLIT_VALIDATION_SELECTION, SPLIT_TEST)
ARM_TREATED: Final = 1
ARM_CONTROL: Final = 0
LABEL: Final = "pipeline validation on semi-synthetic data, not an effect estimate"


@dataclass(frozen=True)
class Snapshot:
    """One point in time of the panel: the cutoff, the arm assignment seed and the outcome draw seed."""

    t0: pd.Timestamp
    assignment_seed: str
    outcome_seed: int


@dataclass(frozen=True)
class FixtureSettings:
    effect: EffectConfig
    snapshots: tuple[Snapshot, ...]
    baseline_days: int
    holdout_share_bps: int
    split_seed: int
    validation_share: float
    validation_selection_share: float
    test_share: float
    demo_export_dir: Path

    @property
    def primary(self) -> Snapshot:
        """The last snapshot: it carries the split, the test rows and the served model's target."""
        return self.snapshots[-1]


def load_fixture_settings(config_path: Path, root: Path) -> FixtureSettings:
    raw = yaml.safe_load(config_path.read_text(encoding="utf-8"))
    fixture = raw["fixture"]
    split = raw["split"]
    validation_share = float(split["validation_share"])
    validation_selection_share = float(split["validation_selection_share"])
    test_share = float(split["test_share"])
    if not 0.0 < validation_share < 1.0 or not 0.0 < test_share < 1.0 or validation_share + test_share >= 1.0:
        raise ValueError("split shares must be positive and leave a training share")
    if not 0.0 < validation_selection_share < 1.0:
        raise ValueError("validation_selection_share must be in (0, 1)")
    snapshots = _load_snapshots(fixture["snapshots"])
    return FixtureSettings(
        effect=load_effect_config(config_path),
        snapshots=snapshots,
        baseline_days=int(fixture["baseline_window_days"]),
        holdout_share_bps=int(fixture["holdout_share_bps"]),
        split_seed=int(split["seed"]),
        validation_share=validation_share,
        validation_selection_share=validation_selection_share,
        test_share=test_share,
        demo_export_dir=root / str(raw["artifact"]["demo_export_dir"]),
    )


def _load_snapshots(items: list[dict[str, object]]) -> tuple[Snapshot, ...]:
    if not items:
        raise ValueError("fixture.snapshots must list at least one snapshot")
    snapshots = []
    for item in items:
        snapshots.append(
            Snapshot(
                t0=pd.Timestamp(str(item["t0"]), tz="UTC"),
                assignment_seed=str(item["assignment_seed"]),
                outcome_seed=int(item["outcome_seed"]),
            )
        )
    instants = [snapshot.t0 for snapshot in snapshots]
    if any(later <= earlier for earlier, later in zip(instants[:-1], instants[1:], strict=True)) or len(set(instants)) != len(instants):
        raise ValueError("fixture snapshots must be in strictly increasing time order")
    if len({snapshot.assignment_seed for snapshot in snapshots}) != len(snapshots):
        raise ValueError("each fixture snapshot needs its own assignment seed")
    if len({snapshot.outcome_seed for snapshot in snapshots}) != len(snapshots):
        raise ValueError("each fixture snapshot needs its own outcome seed")
    return tuple(snapshots)


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
    """One row per eligible customer of the primary snapshot, sorted by contact id.

    Columns: contact_id, customer_ref, the eight features, the true segment, tau, effect (the true effect), arm
    (1 treated, 0 control), y0, y1 (simulated treated outcome), outcome (observed), split.
    """
    frame = snapshot_frame(orders, customer_refs, settings, settings.primary)
    frame["split"] = _stratified_split(frame, settings)
    return frame


def snapshot_frame(
    orders: pd.DataFrame, customer_refs: list[str], settings: FixtureSettings, snapshot: Snapshot
) -> pd.DataFrame:
    """One row per eligible customer at ``snapshot.t0``, without a split. ``effect`` is the true effect of the row."""
    features = eligible_contacts(feature_frame(orders, customer_refs, [], cutoff_instant(snapshot)))
    purchases = orders.loc[~orders["is_cancellation"]]
    window = purchases.loc[
        (purchases["ordered_at"] > snapshot.t0)
        & (purchases["ordered_at"] <= snapshot.t0 + pd.Timedelta(days=settings.baseline_days))
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
                if assign_arm(snapshot.assignment_seed, contact, settings.holdout_share_bps) != ARM_HOLDOUT
                else ARM_CONTROL,
                "y0": int(str(customer_ref) in bought_after),
            }
        )
    frame = pd.DataFrame(rows).sort_values("contact_id", kind="mergesort").reset_index(drop=True)
    if frame.empty:
        raise ValueError(f"snapshot {snapshot.t0.date()} has no eligible customers; check its t0")

    baseline_rate = float(frame["y0"].mean())
    frame["effect"] = [expected_effect(baseline_rate, float(effect)) for effect in frame["tau"]]
    draws = np.random.default_rng(snapshot.outcome_seed).random(len(frame))
    frame["y1"] = [
        treated_outcome(int(y0), float(tau_value), float(draw))
        for y0, tau_value, draw in zip(frame["y0"], frame["tau"], draws, strict=True)
    ]
    frame["outcome"] = np.where(frame["arm"] == ARM_TREATED, frame["y1"], frame["y0"]).astype(int)
    return frame


def stack_training_rows(
    orders: pd.DataFrame, customer_refs: list[str], settings: FixtureSettings, primary: pd.DataFrame
) -> pd.DataFrame:
    """Primary train rows plus earlier snapshots, restricted to customers whose primary split is train.

    The snapshot rows are training rows only: they keep their own features, true segment, tau and effect. The test
    split of the primary snapshot is never stacked and never used for fitting.
    """
    train_customers = set(primary.loc[primary["split"] == SPLIT_TRAIN, "customer_ref"])
    parts = [primary.loc[primary["split"] == SPLIT_TRAIN]]
    for snapshot in settings.snapshots[:-1]:
        rows = snapshot_frame(orders, customer_refs, settings, snapshot)
        rows = rows.loc[rows["customer_ref"].isin(train_customers)].copy()
        rows["split"] = SPLIT_TRAIN
        parts.append(rows)
    return pd.concat(parts, ignore_index=True)


def build_training_panel(orders: pd.DataFrame, customer_refs: list[str], settings: FixtureSettings) -> pd.DataFrame:
    """Primary fixture and its stacked training rows in one frame."""
    primary = build_fixture(orders, customer_refs, settings)
    return stack_training_rows(orders, customer_refs, settings, primary)


def seeded_fixture(settings: FixtureSettings, index: int, stride: int) -> FixtureSettings:
    """The same fixture with outcome and split seeds shifted by ``index * stride``.

    Assignment seeds stay fixed, so the arms of a customer do not change between seeds; only the simulated outcome
    draw and the customer split move.
    """
    snapshots = tuple(
        Snapshot(t0=snapshot.t0, assignment_seed=snapshot.assignment_seed, outcome_seed=snapshot.outcome_seed + index * stride)
        for snapshot in settings.snapshots
    )
    return replace(settings, snapshots=snapshots, split_seed=settings.split_seed + index * stride)


def _stratified_split(frame: pd.DataFrame, settings: FixtureSettings) -> list[str]:
    """Customer-level split, stratified by arm and observed outcome, with a fixed seed.

    The validation rows are split again: one part early-stops the boosters, the other chooses the segment thresholds,
    so the two uses never share rows.
    """
    rng = np.random.default_rng(settings.split_seed)
    assigned = np.array([SPLIT_TRAIN] * len(frame), dtype=object)
    for _, group in frame.groupby(["arm", "outcome"], sort=True):
        indices = np.array(group.index.to_numpy(), dtype=np.int64)
        rng.shuffle(indices)
        validation_count = round(len(indices) * settings.validation_share)
        selection_count = round(validation_count * settings.validation_selection_share)
        test_count = round(len(indices) * settings.test_share)
        model_count = validation_count - selection_count
        assigned[indices[:selection_count]] = SPLIT_VALIDATION_SELECTION
        assigned[indices[selection_count : selection_count + model_count]] = SPLIT_VALIDATION_MODEL
        assigned[indices[validation_count : validation_count + test_count]] = SPLIT_TEST
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
    return {
        "customers": len(frame),
        "arms": arms,
        "rates": rates,
        "splits": splits,
        "segments": segments,
        "y0_rate": float(frame["y0"].mean()),
        "mean_tau": float(frame["tau"].mean()),
        "mean_effect": float(frame["effect"].mean()),
    }


def segment_effects(frame: pd.DataFrame) -> dict[str, dict[str, float]]:
    """Rows, mean true effect and mean tau per true segment, in segment-name order."""
    grouped = frame.groupby("segment", sort=True).agg(rows=("effect", "size"), mean_effect=("effect", "mean"), mean_tau=("tau", "mean"))
    return {
        str(name): {
            "rows": int(row["rows"]),
            "mean_effect": float(row["mean_effect"]),
            "mean_tau": float(row["mean_tau"]),
        }
        for name, row in grouped.iterrows()
    }


def feature_matrix(frame: pd.DataFrame) -> np.ndarray:
    return frame[list(FEATURE_FIELDS)].to_numpy(dtype=np.float32)


def cutoff_instant(snapshot: Snapshot) -> pd.Timestamp:
    """Last instant whose orders count as history: one second before T0."""
    return snapshot.t0 - pd.Timedelta(seconds=1)
