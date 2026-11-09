"""Deterministic holdout assignment, reference for the Campaigns module.

The C# implementation must produce the same buckets for the same inputs; the shared vectors in ``tests/test_holdout_parity.py``
keep the two in step.

Definition (UTF-8 throughout):
    digest = SHA-256(f"{holdout_seed}:{contact_id}")
    bucket = big-endian uint64(digest[0:8]) mod BUCKET_COUNT
    arm    = "holdout" if bucket < share_bps else "treatment"

``contact_id`` is normalized to the lowercase canonical UUID string before hashing, so uppercase
text and ``uuid.UUID`` objects give the same bucket.
"""

from __future__ import annotations

import hashlib
import uuid
from typing import Final, Literal

BUCKET_COUNT: Final[int] = 10_000
DIGEST_PREFIX_BYTES: Final[int] = 8
MIN_SHARE_BPS: Final[int] = 500
MAX_SHARE_BPS: Final[int] = 5_000
DEFAULT_SHARE_BPS: Final[int] = 1_000

ARM_HOLDOUT: Final = "holdout"
ARM_TREATMENT: Final = "treatment"

Arm = Literal["holdout", "treatment"]


def assignment_bucket(holdout_seed: str, contact_id: uuid.UUID | str) -> int:
    """Return the bucket (0..9999) of a contact for a holdout seed."""
    canonical_id = str(uuid.UUID(str(contact_id))).lower()
    payload = f"{holdout_seed}:{canonical_id}".encode("utf-8")
    digest = hashlib.sha256(payload).digest()
    return int.from_bytes(digest[:DIGEST_PREFIX_BYTES], byteorder="big") % BUCKET_COUNT


def assign_arm(holdout_seed: str, contact_id: uuid.UUID | str, share_bps: int) -> Arm:
    """Return the arm of a contact; ``share_bps`` is the holdout share in basis points (500..5000)."""
    if not MIN_SHARE_BPS <= share_bps <= MAX_SHARE_BPS:
        raise ValueError(f"share_bps must be between {MIN_SHARE_BPS} and {MAX_SHARE_BPS}, got {share_bps}")
    bucket = assignment_bucket(holdout_seed, contact_id)
    return ARM_HOLDOUT if bucket < share_bps else ARM_TREATMENT
