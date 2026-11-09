"""Response propensity, S-learner and T-learner on XGBoost, with early stopping on the validation split.

causalml's meta-learners fit ``learner.fit(X, y)`` with no validation set, so early stopping cannot be passed through
them. The learners here use the native XGBoost API. Their layouts match causalml so the two can be compared:
the S-learner prepends the treatment flag as the first feature, and the T-learner fits one outcome model per arm.
``causalml_reference`` builds the same learners in causalml with fixed rounds; the tests and the smoke run compare them.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Final

import numpy as np
import xgboost as xgb

from parley_ml.uplift.data import SplitArrays

FAMILY_RESPONSE: Final = "response"
FAMILY_S: Final = "s"
FAMILY_T: Final = "t"
_NATIVE_ONLY_KEYS: Final = ("max_bin",)


@dataclass(frozen=True)
class BoosterSettings:
    """Native XGBoost parameters plus the boosting budget. ``early_stopping_rounds=None`` means exactly ``max_rounds``."""

    params: dict[str, Any]
    early_stopping_rounds: int | None
    max_rounds: int

    @property
    def max_bin(self) -> int:
        return int(self.params["max_bin"])


@dataclass(frozen=True)
class FittedBooster:
    booster: xgb.Booster
    rounds: int  # boosting rounds kept: best iteration + 1, or every round when early stopping is off

    def predict(self, features: np.ndarray) -> np.ndarray:
        return self.booster.predict(xgb.DMatrix(features), iteration_range=(0, self.rounds))


def fit_booster(
    features: np.ndarray,
    labels: np.ndarray,
    val_features: np.ndarray | None,
    val_labels: np.ndarray | None,
    settings: BoosterSettings,
) -> FittedBooster:
    train_params = {key: value for key, value in settings.params.items() if key not in _NATIVE_ONLY_KEYS}
    dtrain = xgb.QuantileDMatrix(features, label=labels, max_bin=settings.max_bin)
    evals: list[tuple[xgb.DMatrix, str]] = []
    early = settings.early_stopping_rounds
    if early is not None:
        if val_features is None or val_labels is None:
            raise ValueError("Early stopping needs validation rows")
        dval = xgb.QuantileDMatrix(val_features, label=val_labels, ref=dtrain)
        evals = [(dval, "validation")]
    booster = xgb.train(
        train_params,
        dtrain,
        num_boost_round=settings.max_rounds,
        evals=evals,
        early_stopping_rounds=early,
        verbose_eval=False,
    )
    best = booster.attr("best_iteration")
    rounds = int(best) + 1 if best is not None else booster.num_boosted_rounds()
    return FittedBooster(booster=booster, rounds=rounds)


def _s_layout(treatment: np.ndarray, features: np.ndarray) -> np.ndarray:
    return np.column_stack([treatment.astype(np.float32), features]).astype(np.float32, copy=False)


class ResponseLearner:
    """P(outcome | x), trained on treated rows only. Ranks by response, not by uplift."""

    family: Final = FAMILY_RESPONSE

    def __init__(self) -> None:
        self.models: dict[str, FittedBooster] = {}

    def fit(self, train: SplitArrays, val: SplitArrays, outcome: str, settings: BoosterSettings) -> dict[str, int]:
        treated = train.treatment == 1
        treated_val = val.treatment == 1
        self.models["response"] = fit_booster(
            train.features[treated],
            train.outcome(outcome)[treated],
            val.features[treated_val],
            val.outcome(outcome)[treated_val],
            settings,
        )
        return {"response": self.models["response"].rounds}

    def score(self, features: np.ndarray) -> np.ndarray:
        return self.models["response"].predict(features)


class SLearner:
    """One model over all rows with the treatment flag as feature 0. Uplift = P(1, x) - P(0, x)."""

    family: Final = FAMILY_S

    def __init__(self) -> None:
        self.models: dict[str, FittedBooster] = {}

    def fit(self, train: SplitArrays, val: SplitArrays, outcome: str, settings: BoosterSettings) -> dict[str, int]:
        self.models["s"] = fit_booster(
            _s_layout(train.treatment, train.features),
            train.outcome(outcome),
            _s_layout(val.treatment, val.features),
            val.outcome(outcome),
            settings,
        )
        return {"s": self.models["s"].rounds}

    def score(self, features: np.ndarray) -> np.ndarray:
        model = self.models["s"]
        treated = model.predict(_s_layout(np.ones(features.shape[0], dtype=np.int8), features))
        control = model.predict(_s_layout(np.zeros(features.shape[0], dtype=np.int8), features))
        return treated - control


class TLearner:
    """Two models: treated rows and control rows. Uplift = m1(x) - m0(x)."""

    family: Final = FAMILY_T

    def __init__(self) -> None:
        self.models: dict[str, FittedBooster] = {}

    def fit(self, train: SplitArrays, val: SplitArrays, outcome: str, settings: BoosterSettings) -> dict[str, int]:
        rounds: dict[str, int] = {}
        for arm, name in ((1, "treated"), (0, "control")):
            rows = train.treatment == arm
            rows_val = val.treatment == arm
            self.models[name] = fit_booster(
                train.features[rows],
                train.outcome(outcome)[rows],
                val.features[rows_val],
                val.outcome(outcome)[rows_val],
                settings,
            )
            rounds[name] = self.models[name].rounds
        return rounds

    def score(self, features: np.ndarray) -> np.ndarray:
        return self.models["treated"].predict(features) - self.models["control"].predict(features)


LEARNERS: Final = {FAMILY_RESPONSE: ResponseLearner, FAMILY_S: SLearner, FAMILY_T: TLearner}


def causalml_reference(
    family: str,
    train: SplitArrays,
    outcome: str,
    eval_features: np.ndarray,
    params: dict[str, Any],
    rounds: int,
) -> np.ndarray:
    """The same learner built in causalml with ``rounds`` trees and no early stopping. Returns the uplift scores.

    Only used for cross-checks. ``params`` are the native XGBoost parameters; they are mapped to the sklearn API here.
    """
    from causalml.inference.meta import BaseSClassifier, XGBTClassifier
    from xgboost import XGBClassifier

    sklearn_params = {
        "n_estimators": rounds,
        "learning_rate": params["eta"],
        "max_depth": params["max_depth"],
        "tree_method": params["tree_method"],
        "device": params["device"],
        "max_bin": params["max_bin"],
        "random_state": params["seed"],
        "eval_metric": params["eval_metric"],
    }
    outcome_values = train.outcome(outcome)
    if family == FAMILY_T:
        model = XGBTClassifier(xgb_kwargs=sklearn_params)
        model.fit(train.features, treatment=train.treatment, y=outcome_values)
        return np.ravel(model.predict(eval_features, verbose=False))
    if family == FAMILY_S:
        model = BaseSClassifier(learner=XGBClassifier(**sklearn_params))
        model.fit(train.features, treatment=train.treatment, y=outcome_values)
        return np.ravel(model.predict(eval_features, verbose=False))
    raise ValueError("causalml reference exists for the S and T families only")
