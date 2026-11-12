"""Regenerate ``ml/contracts``: the JSON Schemas and the sample requests with the responses the live service gave.

    uv run --directory ml python scripts/generate_contracts.py
"""

from __future__ import annotations

from parley_ml.serving.contracts import CONTRACTS_DIR, write_contracts


def main() -> None:
    written = write_contracts()
    for path in written:
        print(path.relative_to(CONTRACTS_DIR.parent).as_posix())


if __name__ == "__main__":
    main()
