# Data card: Criteo uplift research dataset (v2.1)

Status: research only. Used to validate the uplift method on a real randomized experiment. Criteo features are anonymized, so results say nothing about CRM fields or email. The model trained on this data never serves real contacts.

## Attribution and license
- Dataset: Criteo uplift research dataset, release v2.1 (file name as published on the source page).
- Licence: [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/). Non-commercial use only, share-alike. Results and derived models must not be used commercially.
- Citation: Diemert, Eustache; Betlei, Artem; Renaudin, Christophe; Amini, Massih-Reza. "A Large Scale Benchmark for Uplift Modeling." AdKDD 2018. arXiv:2111.10106.
- Source page: <https://huggingface.co/datasets/criteo/criteo-uplift>.

## Source
- URL (pinned to commit `2424920019e49d52d72c13ac1143ec5d53af276b`): `https://huggingface.co/datasets/criteo/criteo-uplift/resolve/2424920019e49d52d72c13ac1143ec5d53af276b/criteo-research-uplift-v2.1.csv.gz`. The URL is in `configs/criteo_ingest.yaml`.
- File: `criteo-research-uplift-v2.1.csv.gz`, 311,422,618 bytes, SHA-256 `2716e1bf0fd157a93b5bf86924d9088419dfbac2022c6cd90030220634f616dc`. Checked by the ingest on every run.
- Stored in `ml/data/criteo_uplift/` (git-ignored). Never committed.

## Raw counts (measured by the ingest)
| Item | Value |
|---|---|
| Rows | 13,979,592 (asserted) |
| Columns | `f0`...`f11` (features), `treatment`, `visit`, `conversion`, `exposure` (asserted) |
| Treatment ratio | 0.8500001 (expected 0.85, tolerance 0.005) |
| Visit rate | 0.046992 (expected 0.047, tolerance 0.001) |
| Conversion rate | 0.0029167 (expected 0.0029, tolerance 0.0003) |
| Exposure rate | 0.030631 (recorded only) |

## Measured by arm (all rows)
| Arm | Rows | Visit rate | Conversion rate |
|---|---|---|---|
| Treated | 11,882,655 | 0.048543 | 0.0030895 |
| Control | 2,096,937 | 0.038201 | 0.0019376 |
| Difference (absolute uplift) | | 0.010342 | 0.0011519 |

The uplift is the difference in rates between arms. Both are randomized rates, so the difference is an average treatment effect estimate over the whole dataset. Exposure is recorded after treatment and is never a model input.

## Storage format
- `criteo_float32.parquet` (zstd): 13,979,592 rows. Features are float32. `treatment`, `visit`, `conversion` and `exposure` are Int8. Written by the ingest.
- Parquet SHA-256 `879b34bfa52d10cb6a811e6621c9126c4e381272bb8119e53466ec045b1cd58d`.
- The ingest manifest (`ml/data/criteo_uplift/ingest_manifest.json`) holds the same counts and checksums.

## Stratified split
Config: `configs/criteo_split.yaml`. Seed 20261110. Strata: the 8 combinations of treatment, visit and conversion. Within each stratum, rows are shuffled with one seeded generator and cut by fractions 70 / 10 / 20. Row-index files are `int64` `.npy`, one per split, stored in `ml/data/criteo_uplift/splits/`.

| Split | Rows | Share | Treatment share | File SHA-256 |
|---|---|---|---|---|
| train | 9,785,714 | 70.0 % | 0.8500002 | `fc19562d3ec93ed9a645d42a47ee69e1a79e0ec261393b1627422143c0bf76a3` |
| validation | 1,397,958 | 10.0 % | 0.8500005 | `2337cadf4ff6936ef8df5a2fd24bc6fcaa3fd0a57ffe5356015671057d99d784` |
| test | 2,795,920 | 20.0 % | 0.8499996 | `6d743d9f027212f88ebc542af84fcbc0a4a06469a2f11300d4cfb289e2d6cdf8` |

Checks: each row appears in exactly one split; the treatment share of each split is within 0.001 of the overall share (measured deviations below 1e-6); two consecutive ingest runs gave identical SHA-256 values for the Parquet and all three split files. The test indices are not opened by the metric or training code until the final evaluation step.

## Reproduce
```bash
uv run --directory ml python -m parley_ml.datasets.criteo --config configs/criteo_ingest.yaml
```
The download runs once; later runs verify the cached file. Ingest time on the development machine: 34 s including the download.
