# CRM uplift demo: pipeline validation on semi-synthetic data, not an effect estimate

> pipeline validation on semi-synthetic data, not an effect estimate. Covariates and baseline outcomes are real Online Retail II data; the treated outcome is simulated from an effect function written in `configs/crm_uplift_demo.yaml`. The numbers show that the pipeline recovers a known effect. They say nothing about real email effects. No accuracy is reported: the effect of one message on one person is never observed, so an uplift model is judged by its ranking, not by accuracy.

## Setup

- Artifact: `crm_uplift-20261112-44be95a` (provenance `semi_synthetic`), served as `crm_uplift`. The artifact root is generated and git-ignored.
- Served default: the newest trained `crm_uplift` artifact in the registry (by trained date, then version). The service default is a stub only when nothing trained exists; the API never selects a stub automatically, an admin must activate it.
- Snapshots: 2010-09-10 → 2010-12-10 → 2011-03-10 → 2011-06-10. Only the last snapshot (the primary one) provides test rows; earlier snapshots add training rows for customers whose primary split is train.
- Customers at the primary snapshot: 4929 eligible (purchase before T0, non-negative value and average). Training rows after stacking: 10231.
- Splits (customer level, stratified by arm and outcome): {'train': 2955, 'validation_model': 495, 'validation_selection': 492, 'test': 987}. `validation_model` stops the boosters; `validation_selection` chooses the segment thresholds. The two parts never share rows.
- Arms: {'treated': 2486, 'control': 2443} (1 treated, 0 control), shared holdout algorithm, 5000 bps holdout. Observed rates: {'treated_observed_rate': 0.18624296057924378, 'control_observed_rate': 0.17151043798608268}.
- Model: T-learner, XGBoost per arm, rounds kept {'treated': 26, 'control': 37}, early stopping on `validation_model` logloss.
- Thresholds chosen on `validation_selection` only: do_not_disturb_uplift_below = -1, persuadable_uplift_at_least = 0.1, sure_thing_control_rate_at_least = 0.35 (validation macro F1 0.3624; do-not-disturb switched off: no threshold both met the precision floor and improved the macro F1, so the model never emits it).
- True effect of a row: `tau * (1 - P(y0))` for positive tau and `tau * P(y0)` for negative tau, with P(y0) the snapshot's observed baseline purchase rate (0.1647 at the primary snapshot). `tau` alone would overstate the effect, because a positive effect can only convert a non-buyer.

| True segment | Rows | Mean tau | Mean true effect |
|---|---|---|---|
| persuadable | 546 | 0.3000 | 0.2506 |
| sure_thing | 486 | 0.0000 | 0.0000 |
| lost_cause | 602 | 0.0100 | 0.0084 |
| do_not_disturb | 271 | -0.3000 | -0.0494 |
| other | 3024 | 0.0000 | 0.0000 |

## Test split (opened once per run, after the thresholds were frozen; top share 20%)

| Metric | Model point [95 % interval] | Perfect score | Random mean | Random 97.5th pct | Share of perfect |
|---|---|---|---|---|---|
| Mean true effect, top 20% minus all | 0.1080 [0.0942, 0.1230] | 0.1180 | 0.0002 | 0.0100 | 0.92 |
| Share of the perfect gain in the top 20% | 0.9152 (point ratio) | 1.00 | 0.00 | 0.08 | 0.92 |
| AUC persuadable vs rest | 0.9789 [0.9716, 0.9860] | 1.0000 | 0.5001 | 0.5545 | 0.98 |
| Spearman correlation with the true effect | 0.4482 [0.3993, 0.4960] | 1.0000 | 0.0003 | 0.0603 | 0.45 |
| Pearson correlation with the true effect | 0.6245 [0.5784, 0.6731] | 1.0000 | 0.0000 | 0.0583 | 0.62 |
| Qini coefficient | 7.3786 [0.3059, 16.1473] | 6.1611 | -0.0331 | 7.8168 | 1.20 |
| AUUC | 21.5488 [-3.4887, 50.2419] | 19.6349 | 7.3199 | 23.0188 | 1.10 |
| Uplift in the top 20% (observed outcomes) | 0.1255 [0.0323, 0.2594] | 0.1279 | 0.0177 | 0.1111 | 0.98 |

- The perfect score ranks by the true effect. Random scores are drawn on the same test rows: mean and 97.5th percentile over 300 draws. A model value near the random 97.5th percentile is noise on this test size.
- Intervals are percentile intervals from 200 replicates (Poisson weights for the test-row sampling bootstrap of the ranking metrics, row resampling for AUC and the correlations). They cover test-row sampling only. They do not cover training variance or the outcome draw; the seed table below does.
- The share of the perfect gain is the model's top-20% gain divided by the perfect-score gain. The perfect value of the share row is 1; it is a point ratio and the interval of the raw gain is the row above.

## Seed table (10 outcome/split seeds, the artifact is seed 0)

Each seed redraws the simulated outcomes and the customer split with fixed assignment and booster seeds. No choice uses the test rows; the table only measures how stable the result is.

| Seed | Share of perfect gain | AUC persuadable | Spearman | Pearson | Qini | Qini > 0 |
|---|---|---|---|---|---|---|
| 0 | 0.9152 | 0.9789 | 0.4482 | 0.6245 | 7.3786 | yes |
| 1 | 0.9196 | 0.9686 | 0.3771 | 0.6823 | 7.5203 | yes |
| 2 | 0.8228 | 0.9469 | 0.4796 | 0.5268 | 5.7526 | yes |
| 3 | 0.9342 | 0.9837 | 0.3685 | 0.6594 | 7.7353 | yes |
| 4 | 0.9126 | 0.9812 | 0.4577 | 0.6821 | 9.3243 | yes |
| 5 | 0.9162 | 0.9823 | 0.3887 | 0.6520 | -0.2947 | no |
| 6 | 0.9592 | 0.9908 | 0.3878 | 0.7576 | -1.4354 | no |
| 7 | 0.9295 | 0.9752 | 0.3610 | 0.6825 | 5.4307 | yes |
| 8 | 0.9372 | 0.9893 | 0.4217 | 0.7103 | 3.9989 | yes |
| 9 | 0.8626 | 0.9550 | 0.4299 | 0.5508 | 4.0534 | yes |

- Share of perfect gain: mean 0.9109, range [0.8228, 0.9592].
- AUC persuadable vs rest: mean 0.9752, range [0.9469, 0.9908].
- Success criterion, fixed before the run: share of perfect gain at least 0.50 and AUC at least 0.80. Met in every seed.

## Segment labels on test (frozen thresholds)

| Known segment | persuadable | sure_thing | lost_cause | do_not_disturb |
|---|---|---|---|---|
| persuadable | 99 | 0 | 12 | 0 |
| sure_thing | 4 | 78 | 16 | 0 |
| lost_cause | 0 | 0 | 118 | 0 |
| do_not_disturb | 0 | 0 | 46 | 0 |
| other | 37 | 24 | 553 | 0 |

- Macro F1 over the four served labels: 0.4606. Macro F1 over all five known classes, with `other` counted as a class the served rule never emits: 0.3685.
- Do-not-disturb precision on test: no served rows, the label is switched off. A contact is never labeled do_not_disturb on validation unless the threshold reaches the precision floor.
- Mean true effect of the customers served in each segment (test): persuadable 0.1772, sure_thing 0.0000, lost_cause 0.0023, do_not_disturb none.
- `other` is the customers with no injected segment (tau 0). The served model has no such label; those rows are assigned to one of the four segments by the threshold rule.

## Limits

- The effect is injected by construction. A high Qini or correlation shows the pipeline recovers that effect, not that an email changes behaviour.
- No accuracy is claimed anywhere: an individual treatment effect is never observed, so accuracy does not apply.
- Baseline outcomes are one purchase in 30 days from Online Retail II (UK, 2009-2011). Not a marketing exposure log.
- The segment thresholds used the known segments on validation, so the segment scores are optimistic about the served rule. The test split was not used for any choice.
- The demo model is not allowed to drive hard exclusion in the product: its do_not_disturb label is advisory, and excluding a contact is a backend decision reserved for a model with real holdout data.
- Only the demo population: customers with a purchase history. New customers are not covered.

## Reproduce

```bash
uv run --directory ml --group train python -m parley_ml.uplift.train_crm_demo --config configs/crm_uplift_demo.yaml
```

A rerun of this exact config needs `--reproduce` (same inputs, same artifact bytes) and is recorded in the test access log at `artifacts/crm_uplift_demo_test_access.json`. A changed config is refused, so the test split cannot be reused while the procedure is being tuned.
