"""Write the stub ``crm_uplift`` artifact into ``artifacts/models`` so the scoring service can start before the trained model.

The stub has provenance ``stub`` and is labeled in every response. It is git-ignored, like all artifacts. The script
refuses to write while a trained ``crm_uplift`` artifact exists in the same folder, so the two can never tie on date.

    uv run --directory ml python scripts/write_stub_artifacts.py
"""

from __future__ import annotations

import sys

from parley_ml.serving.settings import DEFAULT_ARTIFACTS_DIR
from parley_ml.serving.stub import write_stub_into


def main() -> int:
    try:
        directory = write_stub_into(DEFAULT_ARTIFACTS_DIR)
    except ValueError as error:
        print(f"write_stub_artifacts: {error}", file=sys.stderr)
        return 1
    print(directory.as_posix())
    return 0


if __name__ == "__main__":
    sys.exit(main())
