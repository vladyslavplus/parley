# RQ2: CLV ranking and calibration on a time holdout

Research question RQ2: on a time-based holdout, does BG/NBD + Gamma-Gamma rank future customer value better than the RFM rule and last-period spend, and how well is it calibrated?

Answer: the model ranks better than last-period spend on most measures. It ranks better than the RFM score on top-decile capture and Gini, and on Spearman for repeat buyers. On Spearman over all customers it ties with the RFM score. Its advantage over last-period spend on repeat-buyer top-decile capture is not significant.

## Setup

| Item | Value |
|---|---|
| Data | Online Retail II, clean schema v1 (UCI id 502, CC BY 4.0, Chen, Daqing; doi 10.24432/C5CG6D) |
| Calibration | Purchase days up to and including 2011-06-09 (UTC) |
| Holdout | 2011-06-10 to 2011-12-09, 183 days (the prediction horizon) |
| Fit | MAP, PyMC-Marketing 1.3.0, library default priors. Run `clv_fit-14e62fd3ac3c` (`configs/clv_fit.yaml`) |
| Evaluation | Run `clv_eval-de95448f616b` (`configs/clv_eval.yaml`), customer bootstrap with B = 500, seed 20261109 |
| Population | 4,944 customers with calibration purchases (all), 3,336 with at least one repeat purchase day (repeat buyers) |
| Excluded | 908 customers first seen in the holdout. They have no calibration history. Their holdout revenue is 731,467.57 GBP, which is 14.7 % of all holdout revenue (4,965,512.90 GBP). |
| Target, ranking | Holdout net revenue of all lines (cancellations included) |
| Target, calibration | Holdout purchase days (days with a non-cancellation invoice) |
| Model score | Expected purchase days over the holdout times expected spend per purchase. Undiscounted, no margin. |
| RFM score | Quintile tiers R, F, M in 1..5, summed (3..15). Recency is days to the last purchase at 2011-06-09. Frequency is distinct purchase invoices and monetary value is net amount, both over the 365 days before 2011-06-09. Tiers come from average percentile ranks, so equal values share a tier and the score does not depend on customer order. |
| Last-period spend | Net amount of all lines in the 183 days before 2011-06-09 |

The product's `rfm` mode ranks contacts by segment weight, not by this summed R+F+M score. The summed score is used here only as a value-ranking baseline.

Metrics:
- Spearman correlation between score and holdout revenue.
- Top-decile capture: share of holdout revenue held by the top 10 % of customers by score.
- Gini of the cumulative-gains curve (2 × AUC − 1). It depends on the value distribution, so compare within a population only.
- Calibration: relative error of the cohort total purchase days, and MAE per customer.

Tied scores are averaged: customers with the same score share their mean realized revenue. That is the expected curve under random order inside a tie, and the result does not depend on the order of rows.

Intervals are 95 % percentile intervals from 500 customer bootstrap replicates. Differences are paired on the same replicate (model minus baseline). The share of replicates above zero is also reported.

## Results

### All customers (n = 4,944)

| Metric | Model | RFM score | Last-period spend |
|---|---|---|---|
| Spearman | 0.608 [0.586, 0.629] | 0.610 [0.589, 0.629] | 0.555 [0.530, 0.578] |
| Top-decile capture | 0.636 [0.581, 0.685] | 0.579 [0.516, 0.636] | 0.616 [0.554, 0.666] |
| Gini (cumulative gains) | 0.720 [0.676, 0.756] | 0.667 [0.630, 0.698] | 0.677 [0.624, 0.719] |

Paired differences, model minus baseline:

| Metric | vs RFM score | vs last-period spend |
|---|---|---|
| Spearman | −0.002 [−0.014, +0.009] (positive in 38 % of replicates) | +0.053 [+0.038, +0.071] |
| Top-decile capture | +0.057 [+0.041, +0.076] | +0.020 [+0.009, +0.032] |
| Gini | +0.053 [+0.041, +0.062] | +0.044 [+0.030, +0.061] |

### Repeat buyers (n = 3,336)

| Metric | Model | RFM score | Last-period spend |
|---|---|---|---|
| Spearman | 0.646 [0.621, 0.669] | 0.610 [0.585, 0.633] | 0.581 [0.551, 0.606] |
| Top-decile capture | 0.600 [0.533, 0.654] | 0.441 [0.385, 0.492] | 0.596 [0.526, 0.651] |
| Gini (cumulative gains) | 0.694 [0.644, 0.734] | 0.606 [0.560, 0.641] | 0.657 [0.598, 0.701] |

Paired differences, model minus baseline:

| Metric | vs RFM score | vs last-period spend |
|---|---|---|
| Spearman | +0.037 [+0.022, +0.052] | +0.066 [+0.048, +0.083] |
| Top-decile capture | +0.160 [+0.126, +0.192] | +0.004 [−0.005, +0.015] (positive in 81 % of replicates, not significant) |
| Gini | +0.088 [+0.075, +0.099] | +0.038 [+0.024, +0.056] |

### Calibration (model only)

| Population | Cohort total error | MAE per customer (purchase days) |
|---|---|---|
| All customers | −1.3 % [−4.2 %, +1.8 %] | 1.04 [1.00, 1.09] |
| Repeat buyers | −2.4 % [−5.6 %, +1.0 %] | 1.25 [1.19, 1.31] |

By calibration frequency bin (all customers, `results/clv/calibration_by_frequency.csv`):

| Frequency (repeat purchase days) | Customers | Predicted purchase days | Actual purchase days |
|---|---|---|---|
| 0 | 1,608 | 0.49 [0.48, 0.51] | 0.45 [0.41, 0.51] |
| 1 | 883 | 0.76 [0.73, 0.79] | 0.75 [0.66, 0.83] |
| 2 | 563 | 1.09 [1.03, 1.14] | 1.09 [0.98, 1.19] |
| 3-4 | 723 | 1.45 [1.40, 1.51] | 1.46 [1.32, 1.60] |
| 5-9 | 700 | 2.40 [2.32, 2.48] | 2.29 [2.13, 2.43] |
| 10+ | 467 | 6.10 [5.65, 6.60] | 6.64 [6.00, 7.38] |

Curves: `results/clv/calibration_by_frequency.png` and `.csv`, `results/clv/cumulative_gains.png` and `.csv`. Full metric table: `results/clv/metrics.csv`.

## Negative and partial results (reported as they are)

- All customers, Spearman: the model is not different from the RFM score (−0.002, interval −0.014 to +0.009). The model's advantage over all customers is in top-decile capture and Gini, and over last-period spend in every measure.
- Repeat buyers, top-decile capture: the model is not different from last-period spend (+0.004, interval −0.005 to +0.015). For repeat buyers, last-period spend is as good as the model at the top of the ranking.
- Calibration: the model slightly under-predicts holdout purchases (−1.3 % overall, −2.4 % for repeat buyers). Both intervals include zero, so there is no evidence of bias.
- Correction to the earlier draft: the earlier report broke RFM ties by customer id, which put an arbitrary ordering into the baseline. That gave a repeat-buyer RFM top-decile capture of 0.418 and a model gain of 0.182. The first fix averaged tied values in the metrics but still broke tier ties by id. With tiers from average ranks as well, the RFM figure is 0.441 and the gain is 0.160. The advantage over RFM on top-decile capture remains, but it is smaller than the earlier draft said.

## Limitations

1. One UK non-store online retailer, 2009 to 2011. The results do not transfer to other businesses without refitting.
2. Customers first seen in the holdout are excluded, as the evaluation protocol requires. They are 14.7 % of holdout revenue. The model's value for new customers is not tested here, and the figures above describe existing customers only.
3. Fixed parameters. The intervals are customer bootstrap intervals with the MAP parameters held fixed, so they understate parameter uncertainty. Posterior uncertainty (NUTS) is out of scope here.
4. The dropout parameter is a = 0.143, below 1. The closed forms are finite over the serving domain (`parley_ml/serving/clv.py`), and they match PyMC-Marketing. Whether the Fader et al. parameterization is well identified at this value is not checked here. The calibration table above is the evidence for the fit.
5. Repeat customers with a mean repeat-day net value at or below zero (8 customers, cancellations) get the population mean spend, the same as customers without repeats. Before this rule, the Gamma-Gamma formula gave them negative spend and negative predicted value. The Gamma-Gamma model itself was fitted only on customers with a positive mean repeat value (3,328 of 3,336 repeat customers).
6. Definitions differ by design. The target and the baselines use net revenue of all lines, cancellations included. The model's monetary input is daily net spend on purchase days only, so cancellation-only days are left out of the monetary input. The Gamma-Gamma spend and the holdout revenue target are therefore not the same quantity.
7. The RFM score takes only 13 distinct values (3 to 15), so many customers share a score. Tied scores are handled by averaging the realized value within each tie group, and tied inputs share tiers, so the baseline does not depend on customer order. A top-decile cut inside a tie group takes the group mean pro rata. A unit test checks that a permutation of rows gives identical capture and Gini. Coarse tiers are still a weakness of the baseline: 44 % of calibration customers (2,178 of 4,944) have at most one purchase invoice in the 365-day window, so their frequency tier reflects little behaviour.
8. Value is undiscounted revenue over 183 days. It is not profit and includes no cost of contact. Priority value `uplift * clv` is a heuristic, not a causal quantity, which RQ4 examines.
9. The frequency-monetary Pearson correlation is 0.18 (Spearman 0.25) over the Gamma-Gamma customers, so the independence assumption is weakly violated. The correlation is reported in `fit.json`.
10. No parameter was tuned on the holdout. The fit used calibration data only, and the horizon and bootstrap settings were fixed in the configs before this run.

## Artifact

`ml/artifacts/models/clv_bgnbd_gg-20261109-14e62fd/` (git-ignored, served from the artifact volume): `parameters.json` (MAP values) and `manifest.json` (schema 1, `trained_at` from the fixed config date, provenance `public_dataset`, training data reference with the transactions checksum, feature mapping from CRM metrics, parameter checksum, evaluation summary). Loader and closed forms: `parley_ml/serving/clv.py`. Items outside the domain are rejected one by one with reason codes (`validate_items`). The closed forms match PyMC-Marketing 1.3.0 to 1e-9 on 1,000 customers (`tests/test_clv_closed_form.py`).

## Reproduce

```bash
uv run --directory ml python -m parley_ml.datasets.online_retail --config configs/online_retail.yaml
uv run --directory ml python -m parley_ml.clv.fit --config configs/clv_fit.yaml
uv run --directory ml python -m parley_ml.clv.evaluate --config configs/clv_eval.yaml
```

Two consecutive evaluation runs produced identical bytes for the CSV, PNG, `evaluation.json`, `manifest.json` and `parameters.json`.
