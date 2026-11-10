"""Write the lift reference vectors from statsmodels and scipy directly (independent of parley_ml.experiments.lift).

Run from ml/:  uv run --directory ml python scripts/make_lift_vectors.py
The output is committed as tests/fixtures/lift_vectors.json. Undefined values are written as null with the field
name listed in ``undefined``.
"""

from __future__ import annotations

import importlib.metadata as metadata
import json
import math
from pathlib import Path

from scipy.stats import chisquare, norm
from statsmodels.stats.proportion import confint_proportions_2indep, proportions_ztest

ML_ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ML_ROOT / "tests" / "fixtures" / "lift_vectors.json"
ALPHA = 0.05
POWER = 0.8
BASIS_POINTS = 10_000

# id, description, treatment (conversions, n, mean revenue), holdout (conversions, n, mean revenue), share_bps
CASES = [
    ("balanced", "Balanced arms, moderate rates", (500, 2000, 12.5), (400, 2000, 10.0), 5000),
    ("tiny_holdout", "Tiny holdout; Wilson and Newcombe intervals are wide", (10, 50, 30.0), (1, 5, 20.0), 9000),
    ("zero_conversions_treatment", "No conversions in the treatment arm", (0, 300, 0.0), (40, 600, 8.0), 6667),
    ("zero_conversions_both", "No conversions in either arm (pooled rate 0: z-test undefined)", (0, 400, 0.0), (0, 400, 0.0), 5000),
    ("all_converted_treatment", "Every treated recipient converted", (300, 300, 25.0), (150, 300, 12.0), 5000),
    ("all_converted_both", "Every recipient converted in both arms (pooled rate 1: z-test undefined)", (200, 200, 9.0), (100, 100, 9.0), 5000),
    ("equal_rates", "Equal rates: lift 0, z 0, p 1", (200, 1000, 10.0), (100, 500, 10.0), 6667),
    ("large_n", "Large arms", (30300, 1000000, 15.0), (2950, 100000, 14.0), 9091),
    ("mde_example", "Plan example: n_h 590, n_t 5310, p 0.20", (1062, 5310, 20.0), (118, 590, 20.0), 9000),
    ("srm_exact", "Arm counts exactly match the share", (900, 9000, 11.0), (100, 1000, 11.0), 9000),
    ("srm_failing", "Arm counts far from the share (SRM alarm)", (950, 9500, 11.0), (50, 500, 11.0), 9000),
    ("negative_lift", "Treatment worse than holdout", (90, 1000, 7.0), (150, 1000, 10.0), 5000),
    ("small_share", "Holdout share 75 %", (50, 500, 9.0), (20, 1500, 9.0), 2500),
    ("zero_holdout_arm", "Holdout arm empty: holdout rate and lift undefined", (10, 100, 6.0), (0, 0, None), 9000),
    ("both_arms_empty", "No recipients at all: everything undefined", (0, 0, None), (0, 0, None), 5000),
]


def _rate(c: int, n: int) -> float | None:
    return None if n == 0 else c / n


def _case(case_id: str, description: str, treated, holdout, share_bps: int) -> dict:
    ct, nt, rt = treated
    ch, nh, rh = holdout
    undefined: list[str] = []
    rate_t, rate_h = _rate(ct, nt), _rate(ch, nh)
    if rate_t is None:
        undefined.append("rate_treatment")
    if rate_h is None:
        undefined.append("rate_holdout")
    lift = None if rate_t is None or rate_h is None else rate_t - rate_h
    if lift is None:
        undefined += ["lift", "ci_low", "ci_high"]
        ci_low = ci_high = None
    else:
        ci_low, ci_high = confint_proportions_2indep(ct, nt, ch, nh, method="newcomb", compare="diff", alpha=ALPHA)
        ci_low, ci_high = float(ci_low), float(ci_high)

    pooled = (ct + ch) / (nt + nh) if nt + nh else None
    if pooled is None or pooled in (0.0, 1.0) or nt == 0 or nh == 0:
        p_value = None
        undefined.append("z_test_p_value")
    else:
        _, p_value = proportions_ztest([ct, ch], [nt, nh], value=0, alternative="two-sided", prop_var=pooled)
        p_value = float(p_value)

    total = nt + nh
    if total == 0:
        srm = None
        undefined.append("srm_p_value")
    else:
        expected = [total * share_bps / BASIS_POINTS, total * (BASIS_POINTS - share_bps) / BASIS_POINTS]
        srm = float(chisquare([nt, nh], expected).pvalue)

    baseline = rate_h
    if baseline is None or nt == 0 or nh == 0:
        mde = None
        undefined.append("mde_absolute")
    else:
        z_alpha = norm.ppf(1.0 - ALPHA / 2.0)
        z_beta = norm.ppf(POWER)
        mde = float((z_alpha + z_beta) * math.sqrt(baseline * (1.0 - baseline) * (1.0 / nt + 1.0 / nh)))

    if nt == 0 or rt is None or rh is None:
        incremental = None
        undefined.append("incremental_revenue")
    else:
        incremental = float((rt - rh) * nt)

    return {
        "id": case_id,
        "description": description,
        "treatment": {"conversions": ct, "n": nt, "mean_revenue": rt},
        "holdout": {"conversions": ch, "n": nh, "mean_revenue": rh},
        "share_treatment_bps": share_bps,
        "expected": {
            "rate_treatment": rate_t,
            "rate_holdout": rate_h,
            "lift": lift,
            "ci_low": ci_low,
            "ci_high": ci_high,
            "z_test_p_value": p_value,
            "srm_p_value": srm,
            "mde_absolute": mde,
            "incremental_revenue": incremental,
        },
        "undefined": sorted(set(undefined)),
    }


def main() -> None:
    header = {
        "format_version": 1,
        "generated_by": "ml/scripts/make_lift_vectors.py (statsmodels and scipy directly, not the reference module)",
        "libraries": {name: metadata.version(name) for name in ("statsmodels", "scipy")},
        "alpha": ALPHA,
        "power": POWER,
        "z_interval_and_test": "z = norm.ppf(1 - alpha/2) = 1.959963984540054 (two-sided)",
        "z_power": "z_beta = norm.ppf(power) = 0.8416212335729143",
        "rates": "rate = conversions / n; undefined when n = 0",
        "lift": "rate_treatment - rate_holdout",
        "wilson": "score interval per arm at z; centre (p + z^2/(2n))/(1+z^2/n), half z*sqrt(p(1-p)/n + z^2/(4n^2))/(1+z^2/n)",
        "newcombe_wilson": "Newcombe 1998 method 10 (hybrid score): lower = lift - sqrt((p_t - l_t)^2 + (u_h - p_h)^2); upper = lift + sqrt((u_t - p_t)^2 + (p_h - l_h)^2)",
        "z_test": "pooled two-proportion z, two-sided: p0 = (c_t+c_h)/(n_t+n_h); z = lift/sqrt(p0(1-p0)(1/n_t+1/n_h)); p = 2(1-Phi(|z|)); undefined when p0 in {0,1} or an arm is empty",
        "srm": "chi-squared, 1 df, expected = n_total * share, share_treatment = share_treatment_bps/10000; p = P(chi2_1 > stat); undefined when n_total = 0",
        "mde": "absolute: (z_alpha + z_beta) * sqrt(p(1-p)(1/n_t + 1/n_h)), p = holdout rate; undefined when an arm is empty",
        "incremental_revenue": "(mean_revenue_treatment - mean_revenue_holdout) * n_treatment; undefined when n_treatment = 0 or a mean is missing",
        "undefined": "a field listed in a case's undefined array is null in expected and has no defined value",
        "precision": "every float is written with Python repr, the shortest string that round-trips to the same double",
    }
    document = {"header": header, "cases": [_case(*case) for case in CASES]}
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"wrote {len(document['cases'])} cases to {OUTPUT}")


if __name__ == "__main__":
    main()
