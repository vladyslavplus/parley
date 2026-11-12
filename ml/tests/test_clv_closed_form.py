"""Closed-form BG/NBD and Gamma-Gamma functions against PyMC-Marketing on 1,000 customers, and hyp2f1 extremes."""

from __future__ import annotations

import json
from datetime import date
from pathlib import Path

import numpy as np
import pandas as pd
import pytest
import xarray as xr

from parley_ml.clv.artifact import write_artifact
from parley_ml.serving.artifacts import CODE_CHECKSUM_MISMATCH, ArtifactError, load_model_directory
from parley_ml.serving.clv import (
    REASON_FREQUENCY_NOT_INTEGER,
    REASON_FREQUENCY_RANGE,
    REASON_RECENCY_RANGE,
    REASON_TENURE_RANGE,
    BetaGeoParameters,
    GammaGammaParameters,
    expected_purchases,
    expected_spend,
    probability_alive,
    validate_items,
)
# MAP estimates from the clean Online Retail II calibration fit (clv_fit-14e62fd3ac3c).
BG = BetaGeoParameters(alpha=60.649492305926536, r=0.6458305076241568, a=0.14275234497573008, b=2.8270225477991073)
GG = GammaGammaParameters(p=4.744233578985129, q=2.2682654271770772, v=117.47889039378104)
HORIZON = 183
CUSTOMERS = 1_000
RNG_SEED = 20261109


def _customers() -> pd.DataFrame:
    rng = np.random.default_rng(RNG_SEED)
    tenure = rng.uniform(1.0, 500.0, CUSTOMERS).round()
    frequency = rng.poisson(2.0, CUSTOMERS).astype(float)
    frequency = np.minimum(frequency, tenure)
    recency = np.where(frequency > 0, np.floor(rng.uniform(0.0, 1.0, CUSTOMERS) * tenure), 0.0)
    monetary = np.where(frequency > 0, rng.lognormal(4.0, 1.0, CUSTOMERS), 0.0)
    return pd.DataFrame(
        {
            "customer_id": np.arange(CUSTOMERS, dtype="int64"),
            "frequency": frequency,
            "recency": recency,
            "T": tenure,
            "monetary_value": monetary,
        }
    )


def _posterior(values: dict[str, float]) -> xr.DataTree:
    dataset = xr.Dataset(
        {name: xr.DataArray([[value]], dims=["chain", "draw"]) for name, value in values.items()}
    ).assign_coords(chain=[0], draw=[0])
    return xr.DataTree.from_dict({"posterior": dataset})


def test_matches_pymc_marketing_on_1000_customers() -> None:
    import pymc_marketing.clv as clv

    data = _customers()
    repeat = data.loc[(data["frequency"] > 0) & (data["monetary_value"] > 0)]

    bg_model = clv.BetaGeoModel()
    bg_model.build_model(data)
    bg_model.fit_result = _posterior({"alpha": BG.alpha, "r": BG.r, "a": BG.a, "b": BG.b})
    gg_model = clv.GammaGammaModel()
    gg_model.build_model(repeat)
    gg_model.fit_result = _posterior({"p": GG.p, "q": GG.q, "v": GG.v})

    pymc_purchases = bg_model.expected_purchases(data, future_t=HORIZON).values.reshape(-1)
    pymc_alive = bg_model.expected_probability_alive(data).values.reshape(-1)
    pymc_spend = gg_model.expected_customer_spend(data).values.reshape(-1)

    purchases = expected_purchases(BG, data["frequency"], data["recency"], data["T"], HORIZON)
    alive = probability_alive(BG, data["frequency"], data["recency"], data["T"])
    spend = expected_spend(GG, data["frequency"], data["monetary_value"])

    np.testing.assert_allclose(purchases, pymc_purchases, rtol=1e-9, atol=1e-12)
    np.testing.assert_allclose(alive, pymc_alive, rtol=1e-9, atol=1e-12)
    np.testing.assert_allclose(spend, pymc_spend, rtol=1e-9, atol=1e-9)


def _domain_grid() -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    """Every (frequency, recency, tenure) combination at the edges of the numerical domain, with recency <= tenure."""
    rows = []
    for frequency in (0.0, 1.0, 5.0, 50.0, 165.0, 365.0):
        for tenure in (1.0, 30.0, 730.0, 3650.0, 36500.0):
            if frequency > tenure:
                continue
            for recency in (0.0, tenure / 2.0, tenure):
                rows.append((frequency, recency, tenure))
    grid = np.array(rows)
    return grid[:, 0], grid[:, 1], grid[:, 2]


def test_expected_purchases_are_finite_and_nondecreasing_across_the_domain() -> None:
    frequency, recency, tenure = _domain_grid()
    small_alpha = BetaGeoParameters(alpha=1e-3, r=1e-3, a=0.5, b=0.6)
    large = BetaGeoParameters(alpha=1e6, r=50.0, a=1.5, b=40.0)

    for params in (BG, small_alpha, large):
        horizons = [0.0, 1.0, 183.0, 365.0, 3650.0]
        values = np.array([expected_purchases(params, frequency, recency, tenure, t) for t in horizons])
        # Documented numeric exception: horizon 3,650 with frequency at least 365 and tenure equal to frequency.
        exception = (frequency >= 365.0) & (tenure == frequency)
        finite = np.isfinite(values)
        assert np.all(finite[:, ~exception]), params
        assert np.all(finite[:4, exception]), params
        ok = finite.all(axis=0)
        assert np.all(np.diff(values[:, ok], axis=0) >= -1e-9), params


def test_invalid_items_are_rejected_one_by_one_and_the_rest_are_scored() -> None:
    frequency = np.array([3.0, 2.5, 400.0, 1.0, 5.0])
    recency = np.array([10.0, 10.0, 10.0, 10.0, 500.0])
    tenure = np.array([100.0, 100.0, 300.0, 4e4, 100.0])

    validity = validate_items(frequency, recency, tenure)
    values = expected_purchases(BG, frequency, recency, tenure, HORIZON)

    assert validity.reasons == (
        None,
        REASON_FREQUENCY_NOT_INTEGER,
        REASON_FREQUENCY_RANGE,
        REASON_TENURE_RANGE,
        REASON_RECENCY_RANGE,
    )
    assert np.isfinite(values[0])
    assert np.all(np.isnan(values[1:]))


def test_horizon_outside_the_domain_is_a_request_error() -> None:
    with pytest.raises(ValueError, match="horizon"):
        expected_purchases(BG, np.array([1.0]), np.array([10.0]), np.array([400.0]), 3651.0)


def test_repeat_customers_with_non_positive_mean_value_get_the_population_mean_spend() -> None:
    frequency = np.array([0.0, 4.0, 4.0])
    monetary = np.array([0.0, -20.0, 50.0])

    spend = expected_spend(GG, frequency, monetary)

    population = GG.v * GG.p / (GG.q - 1.0)
    assert spend[0] == pytest.approx(population)
    assert spend[1] == pytest.approx(population)
    assert spend[2] != pytest.approx(population)


def test_parameter_domain_is_enforced() -> None:
    with pytest.raises(ValueError, match="a \\+ b > 1"):
        BetaGeoParameters(alpha=1.0, r=1.0, a=0.2, b=0.5)
    with pytest.raises(ValueError, match="equal 1"):
        BetaGeoParameters(alpha=1.0, r=1.0, a=1.0, b=2.0)
    with pytest.raises(ValueError, match="q > 1"):
        GammaGammaParameters(p=1.0, q=1.0, v=1.0)


def test_expected_purchases_are_zero_at_zero_horizon() -> None:
    value = expected_purchases(BG, np.array([0.0, 4.0]), np.array([0.0, 30.0]), np.array([100.0, 200.0]), 0.0)

    np.testing.assert_allclose(value, 0.0, atol=1e-12)


def test_probability_alive_is_one_without_repeat_purchases_and_within_unit_interval() -> None:
    alive = probability_alive(
        BG, np.array([0.0, 2.0, 50.0]), np.array([0.0, 100.0, 400.0]), np.array([10.0, 500.0, 500.0])
    )

    assert alive[0] == 1.0
    assert np.all((alive >= 0.0) & (alive <= 1.0))


def test_load_artifact_rejects_a_changed_parameter_file(tmp_path: Path) -> None:
    fit = {
        "config_sha256": "14e62fd3ac3c7c94b9c455738af7698d1222ec3fdcf06bfca54f05126e337a1c",
        "run_id": "clv_fit-14e62fd3ac3c",
        "dataset": {"name": "online_retail_ii", "transactions_sha256": "0" * 64},
        "counts": {"split": {"calibration_end": "2011-06-09", "holdout_end": "2011-12-09"}},
    }
    evaluation = {
        "population": {"all_customers": 1, "repeat_buyers": 1},
        "exclusion": {},
        "metrics": {},
        "metrics_intervals": {},
        "calibration": {},
        "libraries": {"numpy": "2.4.6", "pandas": "3.1.1"},
    }
    directory = write_artifact(
        root=tmp_path,
        fit=fit,
        transaction_parameters=BG,
        spend_parameters=GG,
        version_date=date(2026, 11, 9),
        horizon_days=365,
        evaluation=evaluation,
        evaluation_run="clv_eval-test",
    )

    loaded = load_model_directory(directory)
    assert loaded.version == "clv_bgnbd_gg-20261109-14e62fd"
    assert loaded.available
    assert loaded.payload.transactions.alpha == pytest.approx(BG.alpha)

    parameters_path = directory / "parameters.json"
    tampered = json.loads(parameters_path.read_text(encoding="utf-8"))
    tampered["bg_nbd"]["alpha"] = 1.0
    parameters_path.write_text(json.dumps(tampered), encoding="utf-8", newline="\n")
    with pytest.raises(ArtifactError) as refused:
        load_model_directory(directory)
    assert refused.value.code == CODE_CHECKSUM_MISMATCH
