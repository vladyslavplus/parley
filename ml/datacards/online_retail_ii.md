# Data card: Online Retail II (clean transactions and customers)

Status: schema v1, frozen. Later steps (demo seed, CLV fit) read this schema; changing it needs a new schema version.

## Attribution and license
- Dataset: Online Retail II. Creator: Daqing Chen. Year: 2012 (UCI release).
- DOI: <https://doi.org/10.24432/C5CG6D>. UCI dataset id 502.
- License: [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Attribution is required in any output that uses this data (the demo export manifest carries it).
- Citation: Chen, Daqing. Online Retail II [Dataset]. UCI Machine Learning Repository, 2012. https://doi.org/10.24432/C5CG6D.

## Source
- URL: `https://archive.ics.uci.edu/static/public/502/online+retail+ii.zip` (UCI-provided archive, reached on 2026-11-09; no mirror was needed).
- Archive: 45,622,418 bytes, SHA-256 `572e36277c2390fbfde10664750731e0a86f55e33470d91919085f0408e67bfb`.
- Workbook inside the archive: `online_retail_II.xlsx`, 45,622,278 bytes, SHA-256 `bcbe73b35f5b7babf197fb0cb983a11f5d9ff929078d4aa53d171b1f2df2e980`.
- Sheets: `Year 2009-2010` (525,461 rows), `Year 2010-2011` (541,910 rows). Columns: `Invoice`, `StockCode`, `Description`, `Quantity`, `InvoiceDate`, `Price`, `Customer ID`, `Country`.
- Identity checked by assertions in the ingest (raw rows, distinct customers, rows without customer id). The host is not part of the identity.

## Time zone
Source `InvoiceDate` values are UK local time without an offset. They are stored as UTC without conversion (`timestamp_convention` in `configs/online_retail.yaml`). Intervals are real; the error is at most one hour around daylight-saving changes. The demo seed shifts dates by a constant, so intervals stay unchanged.

## Raw counts (from the workbook, asserted)
| Item | Count |
|---|---|
| Raw rows (both sheets) | 1,067,371 |
| Distinct raw customer ids | 5,942 |
| Rows without customer id | 243,007 |

## Cleaning rules (applied in this order, each counted)
| # | Rule | Rows removed |
|---|---|---|
| 1 | No customer id | 243,007 |
| 2 | Exact duplicate of an earlier row (all eight source columns equal) | 26,479 |
| 3 | Stock code is not a product code (`^\d{5}[A-Za-z]{0,2}$` after trimming): postage, fees, adjustments, tests, gift vouchers | 3,662 |
| 4 | Price <= 0 | 60 |
| 5 | Cancellation invoice (`C` prefix) with quantity >= 0 | 0 |
| 6 | Sale invoice with quantity <= 0 | 0 |
| | **Total removed** | **273,208** |

Reconciliation: 1,067,371 - 273,208 = **794,163** clean lines.

Notes on rule 2:
- 15,123 duplicates are in the sheet overlap window (2010-12-01 to 2010-12-09) and span both sheets. The overlap is therefore the only source of cross-sheet repeats.
- The other 11,356 exact repeats are inside one sheet, outside the overlap window. They are removed too: the source has no line number, so an exact repeat cannot be told from a real repeated line. This is a stated limitation.

Notes on rule 3: 13 distinct non-product codes reach this rule after rules 1-2 (manifest `details.non_product_stock_codes_distinct`). Source examples across the whole workbook: `POST`, `DOT`, `M`, `BANK CHARGES`, `PADS`, `ADJUST`, `TEST001`, `gift_0001_*`, `DCGS*`. Trailing spaces in codes are trimmed first (one code, `47503J `).

Notes on cancellations: cancellations are kept, flagged `is_cancellation = true`, and carry negative `line_amount`. They are excluded from purchase counts (`invoice_count`) but included in amounts.

## Clean counts
| Item | Count |
|---|---|
| Transaction lines | 794,163 |
| Cancellation lines | 17,586 |
| Distinct invoices, all | 43,876 |
| Distinct purchase invoices (non-cancellation) | 36,594 |
| Distinct cancellation invoices | 7,282 |
| Distinct customers | 5,875 (67 raw ids lost all their lines to rules 2-6) |
| Customers with 0 purchase invoices (only cancellations) | 23 |
| Customers with more than one purchase invoice | 4,234 (72.1 %) |
| Customers without a mapped country | 21 |
| Lines without a mapped country (ISO 3166-1 alpha-2) | 2,266 |
| Customers whose lines map to several countries (main country used) | 12 |
| Lines in the workbook sheet 2009-2010 / 2010-2011 (clean) | 408,924 / 385,239 |
| Date range (UTC, as stored) | 2009-12-01 07:45 to 2011-12-09 12:50 |
| Sum of `line_amount` | 16,358,614.54 GBP (cancellations: -709,953.43) |
| Share of lines from the United Kingdom | 90.25 % |

Country values without an ISO code (kept with a null country): `Channel Islands`, `European Community`, `Unspecified`, `West Indies`. The mapping is in `configs/online_retail.yaml`.

## Schema v1
`artifacts/online_retail/transactions.parquet` (one row per clean line, sorted by `invoice_ts`, `invoice_no`, `stock_code`, `customer_id`, `quantity`, `unit_price`):

| Column | Type | Meaning |
|---|---|---|
| `invoice_no` | string | Invoice number as in source (`C` prefix for cancellations) |
| `invoice_ts` | timestamp[us, UTC] | Source invoice time, stored as UTC (see time zone) |
| `customer_id` | int64 | Source customer id (pseudonymous, no personal data) |
| `stock_code` | string | Trimmed stock code (product codes only) |
| `quantity` | int64 | Quantity (negative for cancellations) |
| `unit_price` | float64 | Price as published (some have three decimals) |
| `line_amount` | float64 | `round(quantity * unit_price, 2)` |
| `is_cancellation` | bool | Invoice starts with `C` |
| `country_iso2` | string, nullable | ISO 3166-1 alpha-2 from the source country name |
| `source_sheet` | string | Workbook sheet the line came from |

`artifacts/online_retail/customers.parquet` (one row per customer id, sorted by `customer_id`):

| Column | Type | Meaning |
|---|---|---|
| `customer_id` | int64 | Source customer id |
| `country_iso2` | string, nullable | Most frequent mapped country of the customer's lines (ties: alphabetical) |
| `first_line_ts` | timestamp[us, UTC] | Earliest clean line |
| `last_line_ts` | timestamp[us, UTC] | Latest clean line (includes cancellations) |
| `line_count` | int64 | Clean lines of the customer |
| `invoice_count` | int64 | Distinct purchase invoices (non-cancellation) |

Money: stored as float64, no currency column. The source currency is pounds sterling (UK retailer); the CRM stores `GBP` per organization.

## Reproducibility
- Command: `uv run --directory ml python -m parley_ml.datasets.online_retail --config configs/online_retail.yaml`.
- Re-run on 2026-11-09 with the final config gave identical checksums (checked twice, then again after the country fix).
- Country codes: the config quotes every ISO value. An unquoted `NO` (Norway) had parsed as a YAML boolean; it was caught by the demo import and fixed before the final run. `test_country_codes_are_alpha2_strings_in_the_production_config` guards it.
- Output checksums: `transactions.parquet` `13d5999255eca051199d25c6fe7a4e0b8c2ab43c4978aa3e28521a7ba1348884`; `customers.parquet` `a8d31029bca981d7dded91144dfc5550ced6ff686ed7db5ce41e291a8f5eb041`.
- Manifest (`artifacts/online_retail/manifest.json`) holds the full counts, the config hash and the library versions.
- Versions: pandas 3.1.1, pyarrow 26.0.1 (from `uv.lock`).

## CLV daily purchase days and split (computed by `parley_ml.clv.fit`)
Config: `configs/clv_fit.yaml`. Command: `uv run --directory ml python -m parley_ml.clv.fit --config configs/clv_fit.yaml`. Run id `clv_fit-14e62fd3ac3c` (config sha256 `14e62fd3ac3c7c94b9c455738af7698d1222ec3fdcf06bfca54f05126e337a1c`).

Rules (from `parley_ml/clv/split.py`):
- A purchase day is a customer's UTC calendar day with at least one non-cancellation line. Its value is the net of all that day's lines (cancellations on the day subtract). This matches the CRM daily net spend used in the metrics definitions.
- Customer-days with cancellation lines only are not purchases. Their net amount is reported and not modeled.
- Calibration: purchase days up to and including 2011-06-09 (UTC). Holdout: 2011-06-10 to 2011-12-09 inclusive (183 days). Days after 2011-12-09: none.

| Item | Count |
|---|---|
| Clean transaction lines (read from the frozen Parquet, equal to the manifest) | 794,163 |
| Lines of purchase invoices / cancellation lines | 776,577 / 17,586 |
| Customer-days (customer, UTC day) | 37,990 |
| Purchase days (one BG/NBD transaction each) | 32,878 |
| Cancellation-only customer-days (not purchases) | 5,112, net -242,389.35 GBP |
| Purchase-day net amount + cancellation-only net amount | 16,601,003.89 + (-242,389.35) = 16,358,614.54 (equals the sum of all line amounts) |
| Calibration purchase days (2009-12-01 to 2011-06-09) | 23,330 |
| Holdout purchase days (2011-06-10 to 2011-12-09) | 9,548 |
| Customers with calibration purchases | 4,944 |
| Customers with holdout purchases | 3,480 (2,572 also in calibration) |
| Customers first seen in holdout (to be excluded from evaluation) | 908 |
| Customers with cancellation lines only (no purchase day at all) | 23 (5,875 - 4,944 - 908) |
| Calibration customers with T = 0 (first purchase on 2011-06-09) | 3 |
| Gamma-Gamma customers (frequency >= 1 and average repeat value > 0) | 3,328 (8 repeat customers excluded for value <= 0) |

Fit (MAP, PyMC-Marketing 1.3.0, library default priors, recorded in `fit.json`):

| Model | Parameters | Optimizer | Restart check |
|---|---|---|---|
| BG/NBD | alpha 60.6495, r 0.6458, a 0.1428, b 2.8270, phi_dropout 0.0481, kappa_dropout 2.9698 | success, 20 iterations, log posterior -94,076.99; relative gradient 6.0e-8 | starts at 0.5x and 2x: objective gap at most 8.1e-11 relative; parameters differ by up to 5.3e-4 on a weakly identified ridge |
| Gamma-Gamma | p 4.7442, q 2.2683, v 117.479 | success, 14 iterations, log posterior -22,887.94; relative gradient 7.8e-7 | starts at 0.5x and 2x: objective gap at most 7.7e-13 relative |

Derived: purchase rate `r / alpha` = 0.01065 per day (about one purchase per 94 days per active customer).

Frequency-monetary correlation (Gamma-Gamma customers, n = 3,328): Pearson 0.179, Spearman 0.246. The weak positive relation is the case where Gamma-Gamma's independence assumption is most tested; the report should state it.

Reproducibility: two consecutive runs of the fit produced identical bytes for every output (`calibration_summary.parquet` sha256 `5b534f1629b66258d2c3707585e328c0cc299a7023e75067fc3993319a5a27c0`; `holdout_outcomes.parquet` sha256 `25e8a1f26e1975aa37638a2e8325a5a4bc421f885609b69ca117ef4ce48d564c`; `fit.json` sha256 `8134ad8e16e876a3b069dafc17461278ab48971bff4c848a890469254d60064c`). Re-running the ingest gave the same Parquet checksums as above. Elapsed fit time: 7.1 s on the development machine (budget 600 s).

Time zone caveat applies here too: day boundaries are UTC midnight of the stored naive source time.

## Not in this card
Evaluation against baselines, bootstrap intervals and calibration are in `results/rq2_clv.md` (run `clv_eval-c2134d2f41e6cd`, `configs/clv_eval.yaml`); the serving artifact is `artifacts/models/clv_bgnbd_gg-20261109-14e62fd/`.

## Demo export (CRM seed input)
`artifacts/demo_export/` is produced by `uv run --directory ml python -m parley_ml.datasets.demo_export --config configs/demo_export.yaml`. It is git-ignored and carries this card's attribution in `manifest.json`.

| File | Rows | SHA-256 |
|---|---|---|
| `contacts.csv.gz` | 5,875 | `0f57ced652f450d1b4249c00db75de9a1252652a58444d62e1d14c85e8bb7d5d` |
| `orders.csv.gz` | 43,876 | `29fdcae7a9ebbd33136ca501272f3f7be42efd8d6c345b901f4cee8070114125` |

Format (version 1; gzip mtime 0; UTF-8; LF; header row):
- `contacts.csv.gz`: `customer_id,country_iso2`. `country_iso2` is empty when unknown.
- `orders.csv.gz`: `invoice_no,customer_id,ordered_at,total_amount,currency,line_count,is_cancellation`. One row per invoice. `ordered_at` is ISO 8601 UTC with `Z`. `total_amount` has two decimals, negative for cancellations, `currency` is `GBP`, `is_cancellation` is `1` or `0`.

The export contains only source ids, the country and invoice facts. No names, emails or other personal data: the seed generates them.
