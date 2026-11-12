# CRM uplift demo: pipeline validation on semi-synthetic data, not an effect estimate

> pipeline validation on semi-synthetic data, not an effect estimate. Covariates and baseline outcomes are real Online Retail II data; the treated outcome is simulated from an effect function written in `configs/crm_uplift_demo.yaml`. The numbers show that the pipeline recovers a known effect. They say nothing about real email effects.

## Setup

- Artifact: `crm_uplift-20261110-b2fb7b5` (provenance `semi_synthetic`), served as `crm_uplift`.
- Served default: the newest non-stub `crm_uplift` artifact in the registry (by trained date, then version). A stub is refused beside a trained artifact, so the two cannot tie.
- Customers: 4929 eligible at T0 (purchase before T0, non-negative value and average).
- Splits (customer level, stratified by arm and outcome): {'train': 3450, 'validation': 493, 'test': 986}.
- Arms: {'treated': 2486, 'control': 2443} (1 treated, 0 control), shared holdout algorithm, 5000 bps holdout.
- Model: T-learner, XGBoost per arm, rounds kept {'treated': 44, 'control': 35}, early stopping on validation logloss.
- Thresholds chosen on validation only: do_not_disturb_uplift_below = -0.05, persuadable_uplift_at_least = 0.05, sure_thing_control_rate_at_least = 0.25 (validation macro F1 0.2900).

## Test split (opened once, after the thresholds were frozen)

| Metric | Point [95 % interval] |
|---|---|
| Qini coefficient | 8.1785 [0.1385, 17.9734] |
| AUUC | 16.0113 [-7.5373, 45.1152] |
| Uplift in the top 20% | 0.1288 [0.0234, 0.2510] |
| Mean true tau, top 20% minus all | 0.0139 [0.0070, 0.0203] |
| Spearman correlation with true tau | 0.0858 [0.0244, 0.1481] |
| Pearson correlation with true tau | 0.1442 [0.0817, 0.2065] |

Intervals are percentile intervals from 200 replicates (Poisson weights for the ranking metrics, row resampling for the correlations).

Macro F1 over the four segments on test: 0.2473.

## Confusion by segment (test rows; rows = known segment, columns = served segment)

| Known segment | persuadable | sure_thing | lost_cause | do_not_disturb |
|---|---|---|---|---|
| persuadable | 41 | 2 | 65 | 5 |
| sure_thing | 41 | 29 | 5 | 21 |
| lost_cause | 5 | 0 | 103 | 5 |
| do_not_disturb | 3 | 0 | 8 | 4 |
| other | 100 | 27 | 412 | 110 |

`other` is the customers with no injected segment (tau 0). The served model has no such label; those rows are assigned to one of the four segments by the threshold rule.

Mean true tau of the customers served in each segment (test): persuadable 0.0322, sure_thing 0.0052, lost_cause 0.0178, do_not_disturb 0.0047.

## Limits

- The effect is injected by construction. A high Qini or correlation shows the pipeline recovers that effect, not that an email changes behaviour.
- Baseline outcomes are one purchase in 30 days from Online Retail II (UK, 2009-2011). Not a marketing exposure log.
- The segment thresholds used the known segments on validation, so the segment scores are optimistic about the served rule. The test split was not used for any choice.
- Only the demo population: customers with a purchase history. New customers are not covered.

## Reproduce

```bash
uv run --directory ml python -m parley_ml.uplift.train_crm_demo --config configs/crm_uplift_demo.yaml
```
