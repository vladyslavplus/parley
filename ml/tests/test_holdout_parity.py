"""Holdout assignment parity: the shared test vectors of the assignment rule.

The rule: bucket = big-endian uint64 of SHA-256("{holdout_seed}:{contact_id}") mod 10000, holdout when bucket < share_bps.
The vectors below are fixed values of that rule; the C# test asserts the same values. Do not change them to make
a test pass: fix the implementation instead.
"""

from __future__ import annotations

import random
import uuid

import pytest

from parley_ml.experiments.holdout import (
    ARM_HOLDOUT,
    ARM_TREATMENT,
    BUCKET_COUNT,
    DEFAULT_SHARE_BPS,
    MAX_SHARE_BPS,
    MIN_SHARE_BPS,
    assign_arm,
    assignment_bucket,
)

SEED = "0192f5a0-7b1c-7c3e-8a10-3f6d2b9e4c11"
SHARED_VECTORS: tuple[tuple[str, int, str], ...] = (
    ("0192f5a1-0000-7000-8000-000000000001", 3443, ARM_TREATMENT),
    ("0192f5a1-0000-7000-8000-000000000002", 8750, ARM_TREATMENT),
    ("0192f5a1-0000-7000-8000-000000000003", 8290, ARM_TREATMENT),
    ("0192f5a1-0000-7000-8000-00000000000a", 6242, ARM_TREATMENT),
    ("0192f5a1-0000-7000-8000-0000000000ff", 2771, ARM_TREATMENT),
    ("0192f5a1-0000-7000-8000-000000000006", 883, ARM_HOLDOUT),
    ("0192f5a1-0000-7000-8000-00000000001b", 134, ARM_HOLDOUT),
)
STATISTICAL_SAMPLE_SIZE = 100_000
STATISTICAL_SAMPLE_SEED = 20261107
STATISTICAL_TOLERANCE = 0.005


@pytest.mark.parametrize(("contact_id", "expected_bucket", "expected_arm"), SHARED_VECTORS)
def test_shared_vectors_match_domain_model(contact_id: str, expected_bucket: int, expected_arm: str) -> None:
    assert assignment_bucket(SEED, contact_id) == expected_bucket
    assert assign_arm(SEED, contact_id, DEFAULT_SHARE_BPS) == expected_arm


def test_assignment_is_independent_of_processing_order() -> None:
    contact_ids = [vector[0] for vector in SHARED_VECTORS]
    forward = {contact_id: assign_arm(SEED, contact_id, DEFAULT_SHARE_BPS) for contact_id in contact_ids}
    backward = {contact_id: assign_arm(SEED, contact_id, DEFAULT_SHARE_BPS) for contact_id in reversed(contact_ids)}

    assert forward == backward


def test_uppercase_text_and_uuid_object_normalize_to_the_same_bucket() -> None:
    contact_id = SHARED_VECTORS[0][0]
    expected = assignment_bucket(SEED, contact_id)

    assert assignment_bucket(SEED, contact_id.upper()) == expected
    assert assignment_bucket(SEED, uuid.UUID(contact_id)) == expected


@pytest.mark.parametrize("share_bps", [MIN_SHARE_BPS - 1, MAX_SHARE_BPS + 1])
def test_share_outside_bounds_is_rejected(share_bps: int) -> None:
    with pytest.raises(ValueError):
        assign_arm(SEED, SHARED_VECTORS[0][0], share_bps)


def test_holdout_share_matches_configured_share_on_random_ids() -> None:
    rng = random.Random(STATISTICAL_SAMPLE_SEED)
    contact_ids = [str(uuid.UUID(int=rng.getrandbits(128))) for _ in range(STATISTICAL_SAMPLE_SIZE)]

    holdout_count = sum(assign_arm(SEED, contact_id, DEFAULT_SHARE_BPS) == ARM_HOLDOUT for contact_id in contact_ids)

    observed_share = holdout_count / STATISTICAL_SAMPLE_SIZE
    assert abs(observed_share - DEFAULT_SHARE_BPS / BUCKET_COUNT) < STATISTICAL_TOLERANCE
