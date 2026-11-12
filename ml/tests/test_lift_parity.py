"""Campaign lift statistics: the reference module against the committed vectors and against statsmodels and scipy."""

from __future__ import annotations

import json
import math
from pathlib import Path

import pytest
from scipy.stats import chisquare
from statsmodels.stats.proportion import confint_proportions_2indep, proportions_ztest

from parley_ml.experiments import lift as L

FIXTURE = Path(__file__).parent / "fixtures" / "lift_vectors.json"
REL = 1e-10
ABS = 1e-14
CASES = json.loads(FIXTURE.read_text(encoding="utf-8"))["cases"]


def _case(case_id: str) -> dict:
    return next(case for case in CASES if case["id"] == case_id)


def _mean(arm: dict) -> float:
    return math.nan if arm["mean_revenue"] is None else float(arm["mean_revenue"])


def _values(case: dict) -> dict[str, float]:
    t, h = case["treatment"], case["holdout"]
    treatment = L.ArmCounts(t["conversions"], t["n"])
    holdout = L.ArmCounts(h["conversions"], h["n"])
    lift, low, high = L.newcombe_wilson_difference(treatment, holdout)
    baseline = L.rate(h["conversions"], h["n"])
    return {
        "rate_treatment": L.rate(t["conversions"], t["n"]),
        "rate_holdout": L.rate(h["conversions"], h["n"]),
        "lift": lift,
        "ci_low": low,
        "ci_high": high,
        "z_test_p_value": L.two_proportion_z_p_value(treatment, holdout),
        "srm_p_value": L.srm_p_value(t["n"], h["n"], case["share_treatment_bps"]) if t["n"] + h["n"] else math.nan,
        "mde_absolute": L.minimum_detectable_effect(baseline, t["n"], h["n"]) if not math.isnan(baseline) else math.nan,
        "incremental_revenue": L.incremental_revenue(_mean(t), _mean(h), t["n"]),
    }


@pytest.mark.parametrize("case_id", [case["id"] for case in CASES])
def test_reference_module_matches_the_committed_vector(case_id: str) -> None:
    case = _case(case_id)
    values = _values(case)
    for field, expected in case["expected"].items():
        if field in case["undefined"]:
            assert math.isnan(values[field]), (case_id, field)
        else:
            assert values[field] == pytest.approx(expected, rel=REL, abs=ABS), (case_id, field)


def test_the_committed_vectors_are_at_least_twelve_and_cover_the_required_edges() -> None:
    assert len(CASES) >= 12
    ids = {case["id"] for case in CASES}
    for required in ("balanced", "tiny_holdout", "zero_conversions_treatment", "all_converted_treatment",
                     "equal_rates", "large_n", "mde_example", "srm_exact", "srm_failing",
                     "zero_holdout_arm", "both_arms_empty"):
        assert required in ids, required


@pytest.mark.parametrize("case_id", ["balanced", "tiny_holdout", "zero_conversions_treatment", "equal_rates",
                                     "large_n", "mde_example", "small_share", "negative_lift"])
def test_newcombe_wilson_and_z_test_match_statsmodels_live(case_id: str) -> None:
    case = _case(case_id)
    t, h = case["treatment"], case["holdout"]
    lift, low, high = L.newcombe_wilson_difference(L.ArmCounts(t["conversions"], t["n"]), L.ArmCounts(h["conversions"], h["n"]))
    sm_low, sm_high = confint_proportions_2indep(t["conversions"], t["n"], h["conversions"], h["n"],
                                                 method="newcomb", compare="diff", alpha=0.05)
    assert low == pytest.approx(sm_low, rel=1e-9, abs=1e-12)
    assert high == pytest.approx(sm_high, rel=1e-9, abs=1e-12)

    pooled = (t["conversions"] + h["conversions"]) / (t["n"] + h["n"])
    _, sm_p = proportions_ztest([t["conversions"], h["conversions"]], [t["n"], h["n"]], value=0,
                                alternative="two-sided", prop_var=pooled)
    assert L.two_proportion_z_p_value(L.ArmCounts(t["conversions"], t["n"]), L.ArmCounts(h["conversions"], h["n"])) \
        == pytest.approx(sm_p, rel=1e-9, abs=1e-14)


@pytest.mark.parametrize("case_id", ["srm_exact", "srm_failing", "small_share", "balanced"])
def test_srm_matches_scipy_chisquare_live(case_id: str) -> None:
    case = _case(case_id)
    t, h = case["treatment"], case["holdout"]
    total = t["n"] + h["n"]
    share = case["share_treatment_bps"] / 10_000
    expected = [total * share, total * (1 - share)]
    sci = chisquare([t["n"], h["n"]], expected).pvalue
    assert L.srm_p_value(t["n"], h["n"], case["share_treatment_bps"]) == pytest.approx(sci, rel=1e-9, abs=1e-14)


def test_mde_example_from_the_domain_model_is_4_9_percentage_points() -> None:
    mde = L.minimum_detectable_effect(0.20, 5310, 590)
    assert round(mde * 100, 1) == 4.9
    assert mde == pytest.approx(0.04863137714214683, rel=1e-12)


def test_zero_denominators_are_undefined_not_zero() -> None:
    empty = L.ArmCounts(0, 0)
    assert math.isnan(L.rate(0, 0))
    assert all(math.isnan(value) for value in L.newcombe_wilson_difference(L.ArmCounts(5, 10), empty))
    assert math.isnan(L.two_proportion_z_p_value(L.ArmCounts(0, 400), L.ArmCounts(0, 400)))
    assert math.isnan(L.srm_p_value(0, 0, 5000))
    assert math.isnan(L.incremental_revenue(10.0, 9.0, 0))


def test_invalid_share_is_rejected() -> None:
    with pytest.raises(ValueError):
        L.srm_p_value(10, 10, 0)
    with pytest.raises(ValueError):
        L.srm_p_value(10, 10, 10_000)
