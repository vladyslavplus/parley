### Question and answer

Question: on a randomized experiment, does ranking customers by predicted uplift yield more incremental visits (and conversions) than ranking by response propensity, at the same targeting budget?

Answer, primary outcome (visit):
- The S-learner beats response propensity on the full test split. Qini coefficient +668 [409, 933], AUUC +745 [441, 1054], both 95 % paired intervals excluding zero. The gain is in the top of the ranking: uplift@5 % +0.040 [0.034, 0.047], uplift@10 % +0.014 [0.011, 0.017], uplift@20 % +0.0015 [0.0002, 0.0028]. At 30 % and 50 % no detectable difference remains.
- The T-learner does not beat response propensity on the overall Qini coefficient: −103 [−639, 528]. It does beat it in the top of the ranking: uplift@5 % +0.031 [0.024, 0.038] and uplift@10 % +0.0082 [0.0052, 0.0121]. At 20 % and 30 % there is no detectable difference, and at 50 % it is significantly lower (−0.0008 [−0.0014, −0.0002]).
- Under a small budget, the T-learner is significantly worse in profit at cost-to-value ratio 0.001: −365 [−579, −159]. At 0.005 and 0.01 the intervals include zero, and at 0.02 the T-learner is +455 [−413, 1382], not significant.
- The S-learner advantage is not detected at every sample size. On a 50,000-row subsample it is +59 [16, 110]; on 100,000 rows the interval includes zero (+23 [−46, 92]); on 1,000,000 rows it is +274 [114, 457]. The full split gives the tightest interval.

Answer, secondary outcome (conversion):
- No separation for the S-learner: Qini +37 [−33, 109] over response propensity.
- The T-learner is significantly worse than response propensity: Qini −404 [−554, −271].
- This is expected from the weak conversion signal (about 0.29 % positives) and is reported as a negative result.

Conditional profit (per unit of value, cost-to-value ratio c/v, k* chosen on validation):
- Visit, S minus response: +728 [221, 1255] at c/v 0.01 and +1637 [651, 2515] at c/v 0.02. At c/v 0.001 (+133 [−271, 458]) and 0.005 (−137 [−567, 175]) the intervals include zero.
- Conversion: the T-learner loses at every ratio. The S-learner gains at c/v 0.001 (+119 [20, 207]) and loses at 0.005 (−148 [−309, −6]).
- The profit values are per unit of value under an assumed c/v. Criteo has no money, so these are curves over c/v, not estimated profit.

Hypotheses fixed before the test split was read, and what the data shows:
1. All uplift models beat random on visit Qini, with intervals excluding zero. Supported. The Qini coefficient is the area above the random line, so zero is random. Visit intervals on the full split: response 8560 [7980, 9188], S 9228 [8585, 9866], T 8457 [7801, 9024]. On the conversion outcome at 50,000 rows the intervals include zero for all three models (response 3.1 [−25.7, 25.3], S 1.7 [−28.7, 23.5], T 2.9 [−27.8, 32.1]). On the full conversion split the intervals exclude zero.
2. T and S beat response propensity on visit Qini for part of the curve. Supported for S (top 5 % to 20 %) and for T (top 5 % to 10 %). Neither beats response on the whole Qini curve for T.
3. S and T may be statistically indistinguishable. Not tested. No paired S-minus-T statistic was computed. Point estimates on visit Qini are 9228 (S) and 8457 (T), and no interval for their difference is reported.
4. Conversion is noisier and may show no separation. Supported for S. The T-learner is worse than response, which goes further than no separation.

### Paper comparison (separate relative AUUC only)

Devriendt, Guns and Verbeke (2020, Table 6) report separate relative AUUC on the Criteo visit outcome: 0.01479 for the pointwise approach and 0.0152 to 0.0158 for listwise approaches. That table uses a randomly drawn 25,310-row subsample, not the full table, and their learners are a flipped-label pointwise model and LambdaMART, not the S- and T-learners used here.

Their printed Eq. 16 sums V(p) over 100 points with no 1/100 factor, but the magnitudes they report are about one hundredth of that sum, which suggests the averaged form was used. This report gives both forms. Here, the separate relative AUUC on the full test split is 0.943 for response, 0.957 for the S-learner and 0.932 for the T-learner (sum form); divided by 100 these are 0.00943, 0.00957 and 0.00932. The paper's values and ours are of the same order of magnitude, but they are not directly comparable because the sample size, the learners and the split differ. The comparison is reported as a scale check only.

The dataset paper (Diemert et al., arXiv 2111.10106) does not define a separate or relative AUUC, so no values from it are reported here.

### Protocol

- Winners and k* were fixed on the validation split before the test split was read. The winners are the best cell per family and outcome in the committed validation grid, by Qini coefficient on validation. The evaluation checks the configured winners against that grid before fitting. Each winner was refitted with three seeds, and k* was taken from the mean validation profit over those seeds.
- The freeze file (winners, tree counts, validation metrics, k*) was written before any test access. Its sha256 is in the provenance table. Its checksum was verified before and after each test-phase run and did not change.
- Test-split openings recorded in the access record in the git-ignored artifacts folder: three. Attempt 1 (fit, freeze, test) was stopped by the operating system for low memory during the test phase and wrote no results. Its outcome was recorded by hand in `test_access.json`, because the first attempt's code did not write those fields. The logs cannot show whether any test rows were read before the stop. The winners and k* had been frozen before that attempt, so the crash did not change any selection. Attempt 2 (test phase only, from the saved models) produced the reported results. Attempt 3 is the reproduction run described below.
- Attempt 2 ran in a new process, from the saved models and the frozen configuration, with no refitting. Before it opened the test split, it checked free memory, and it re-scored the validation split with the saved boosters, compared every frozen validation metric, and refused on mismatch. The check passed on all 18 models (largest relative difference 1.45e-5 on CPU, tolerance 1e-4, see Limitations). The check's model checksums are in the git-ignored `model_checksums.json`, and the digest of that file is in the provenance table. The test rows are then loaded one column at a time and scored in chunks of 500,000 rows.
- The reproduction run re-ran attempt 2 into a separate folder and compared every committed output byte for byte. The computed results (`results.json`) were identical on the first try. The first byte-for-byte comparison of the rendered files failed on nine files (the CSVs, the report and the sample-size PNGs). The cause was the renderer: rows were written in dictionary order, which differed between a fresh run and a re-render from the saved file. The renderer now reads only the saved results and writes sample sizes in configured order. The reproduction folder was then re-rendered from its own saved results with no test rows, and all 14 outputs matched the committed copies. The failed first record is kept in `reproduction.json` under `history`.
- Intervals are 95 % percentile intervals from 200 Poisson bootstrap replicates over test rows. Differences are paired: each replicate uses the same weights for both models.
- The sample-size subsets are drawn independently for each size, not nested: each size is a fresh random draw from the same seeded generator. Intervals are conditional on that subsample.
- Earlier random-baseline check, disclosed. The slow test `test_random_baseline_qini_interval_covers_zero` ran in the earlier slow test runs. It read the test split's treatment and visit labels and scored a random ranking. No model was involved, and no selection used its output. Its output (point 10.7, interval [−414, 393]) appeared in the earlier report, which labelled it as the test split. It is not in `test_access.json`, so the access record undercounts test-label reads by that amount. The test now runs on the validation split (`test_random_baseline_qini_interval_covers_zero_on_the_validation_split`), and the default and slow runs no longer open the test split.

### Limitations

- The data is one randomized advertising experiment with anonymized features `f0` to `f11`. Results validate the method. They say nothing about CRM fields, email, or any production population. The data is research only, under CC BY-NC-SA 4.0, and no model trained on it is served.
- The seeds gave identical models. The three-seed range is therefore zero for every metric, because training is deterministic without row or column subsampling. The reported intervals reflect sampling variation in the test rows only. They do not include variation from the training procedure or from the choice of hyperparameters.
- The grid has four cells per family and outcome, selected on validation. The winners are at different cells: response and S-learner on visit use depth 6 and 0.1 versus depth 4 and 0.05, and the T-learner uses depth 6 and 0.05. So the family comparison mixes model family and hyperparameter choice. Winners could differ under another grid or another validation split.
- The T-learner's treated model is the response model when both use the same cell. At depth 6 and learning rate 0.1 on visit, the two are identical on validation (maximum absolute difference 0.0, 394 trees each). In that case T minus response equals minus the control model's score. The winners use different cells, so this identity does not hold for them, and the visit Qini loss of the T-learner comes from the control model together with the cell choice.
- Qini and AUUC are in outcome counts for the test split (2,795,920 rows) and are not normalised, so they grow with sample size. Compare them only within one table.
- Multiplicity: the reported tables contain 120 intervals in `rq1_intervals.csv` and 80 in `rq1_sample_size.csv`, with no correction for multiple comparisons. Marginal intervals, such as visit S uplift@20 % [0.0002, 0.0028], conversion S profit at c/v 0.005 [−309, −6], and the 50,000-row subsample results, are exploratory.
- The random-targeting check on the test labels, run in earlier slow test runs, is described under Protocol. It is not part of the reported results, and the test-split access is not counted in the access record.
- Scores from the validation phase came from GPU-trained boosters. The test scores come from the saved boosters evaluated on CPU. The integrity check measured this difference: at most 1.45e-5 relative on any frozen validation metric. The check tolerance was set to 1e-4 after the first check, which used 1e-6, failed on that difference alone; the tolerance was set from validation numbers only. Real tampering moves the metrics far more (see the unit test that replaces a booster).
- The T-learner at c/v 0.001 selects k* = 1.00 (target everyone). Its profit at that ratio is a boundary choice and should not be read as a targeting policy.
- Priority value `uplift * clv` is not tested here. Value weighting is a separate question.
- The test split was opened in three test-phase runs (see Protocol). The reported results come from attempt 2.

### Deviations from the plan

- Learners: native XGBoost, not causalml's `BaseSClassifier` and `XGBTClassifier`. Reason: causalml's `fit` takes no validation set, so early stopping on validation is not possible through it. The fixed-rounds causalml cross-check is in `tests/test_uplift_learners.py` (`test_native_learners_match_causalml_at_fixed_rounds`), and the two agree exactly at fixed rounds.
- Paper comparison: uses Devriendt, Guns and Verbeke (2020, Table 6). The dataset paper (Diemert et al., arXiv 2111.10106) does not define separate or relative AUUC, so there are no values from it to compare against.
- Figures: the PNG curves are generated into the git-ignored `artifacts/final/figures` folder and are not committed, so no binaries are in `results/`. The CSVs are committed. The pipeline diagram is `ml/results/uplift_pipeline.svg`. The plan named a `docs/diagrams/` folder, but every file under `docs/` is git-ignored in this repository, so the diagram lives in `ml/results/`.
- Qini and AUUC use a 1,000-point grid. They equal causalml's `qini_score` and `auuc_score` only when the grid has one point per row. That exact-row equality is tested on a 300-row fixture.
- Model checksums are in `artifacts/final/model_checksums.json`, not in the `results.json` header. `results.json` is a computed output and was not rewritten after the test phase.

### Compute (measured)

| Step | Wall time | Device and memory |
|---|---|---|
| Validation grid (24 runs, three families, two outcomes, four cells), final refit after the tie-aware metric fix | 2537 s (sum of fit and scoring: 2496 s) | RTX 3080 10 GB; peak device GPU memory 6566 MiB (device-wide, from nvidia-smi) |
| Winner refits with three seeds (18 fits, early stopping on validation) | recorded per run in the artifacts folder | GPU |
| Test phase, attempt 2 (scoring 18 models on 2,795,920 rows, bootstrap B = 200 per outcome, sample-size curve, profit curves) | 3399 s | CPU scoring; free memory 9.3 GiB at start |
| Reproduction, attempt 3 (same test phase) | 3242 s | CPU scoring; free memory 14.6 GiB at start |
| Attempt 1 (crashed) | not measured | ran out of memory during the test phase |

The bootstrap dominates the test phase. Attempt 1's crash came from holding the full table and the fitted models in one process. Attempts 2 and 3 hold only the test rows.

### Reproduction

Run from the repository root.

```bash
uv run --directory ml python -m parley_ml.uplift.train_grid --config configs/uplift_grid.yaml
uv run --directory ml python -m parley_ml.uplift.evaluate --config configs/uplift_final.yaml --verify-saved-models
uv run --directory ml python -m parley_ml.uplift.evaluate --config configs/uplift_final.yaml --render-only
uv run --directory ml python -m parley_ml.uplift.evaluate --config configs/uplift_final.yaml --compare-reproduction
```

The test phase itself is `--test-from-saved`. It was run once for the reported results, and a second run without `--reproduce` is refused because `results.json` exists. Its reproduction is `--test-from-saved --reproduce`, which opens the test split again and is recorded as a separate attempt. The first attempt, which fitted the winners and wrote the freeze file, was `python -m parley_ml.uplift.evaluate --config configs/uplift_final.yaml`. That run is refused now that the access record exists, so the fitted models are reused from the artifacts folder.
