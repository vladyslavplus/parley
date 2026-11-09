# Uplift validation grid (RQ1)

Validation split only. Test split not opened. Metric values are point estimates; the final evaluation adds bootstrap intervals.
Selection: best cell per family and outcome by `qini_coefficient` on validation.

## Winners per family and outcome

| Outcome | Family | max_depth | learning_rate | Qini coefficient | AUUC | Separate relative AUUC | Run |
|---|---|---|---|---|---|---|---|
| conversion | response | 6 | 0.1 | 551.31 | 1448.60 | 0.11 | `uplift-response-conversion-d68698fd4eb6` |
| conversion | s | 4 | 0.1 | 504.67 | 1393.71 | 0.11 | `uplift-s-conversion-0940db80125c` |
| conversion | t | 6 | 0.1 | 395.63 | 1265.94 | 0.10 | `uplift-t-conversion-3d983581d71b` |
| visit | response | 6 | 0.1 | 4152.37 | 12078.61 | 0.94 | `uplift-response-visit-a8d5f1622cc7` |
| visit | s | 4 | 0.05 | 4308.04 | 12244.02 | 0.94 | `uplift-s-visit-3bd237e8550d` |
| visit | t | 6 | 0.05 | 4260.65 | 12190.77 | 0.94 | `uplift-t-visit-d47d8c6f703a` |

## Random baseline (validation)

| Outcome | Qini coefficient | AUUC | Separate relative AUUC |
|---|---|---|---|
| conversion | -23.14 | 778.83 | 0.06 |
| visit | 115.21 | 7365.68 | 0.53 |

## Measured compute

- Runs with timing: 24, of which reused from an earlier invocation: 0.
- Sum of fit and scoring seconds over timed runs: 2496.
- Peak device GPU memory used (nvidia-smi, device-wide): 6566 MiB.

## All grid cells

| Outcome | Family | max_depth | learning_rate | Qini | AUUC | Sep. rel. AUUC | Rounds | Seconds | GPU MiB |
|---|---|---|---|---|---|---|---|---|---|
| conversion | response | 4 | 0.05 | 531.96 | 1426.05 | 0.11 | response=613 | 19 | 6361 |
| conversion | response | 4 | 0.1 | 539.59 | 1434.85 | 0.11 | response=830 | 24 | 6360 |
| conversion | response | 6 | 0.05 | 532.91 | 1427.23 | 0.11 | response=307 | 14 | 6328 |
| conversion | response | 6 | 0.1 | 551.31 | 1448.60 | 0.11 | response=241 | 12 | 6360 |
| conversion | s | 4 | 0.05 | 497.78 | 1385.72 | 0.11 | s=1054 | 303 | 6376 |
| conversion | s | 4 | 0.1 | 504.67 | 1393.71 | 0.11 | s=593 | 197 | 6442 |
| conversion | s | 6 | 0.05 | 484.60 | 1370.51 | 0.10 | s=430 | 177 | 6474 |
| conversion | s | 6 | 0.1 | 502.58 | 1391.24 | 0.11 | s=232 | 73 | 6474 |
| conversion | t | 4 | 0.05 | 376.87 | 1243.82 | 0.10 | treated=613/control=205 | 247 | 6534 |
| conversion | t | 4 | 0.1 | 304.35 | 1158.93 | 0.09 | treated=830/control=210 | 128 | 6536 |
| conversion | t | 6 | 0.05 | 365.00 | 1230.25 | 0.09 | treated=307/control=134 | 100 | 6566 |
| conversion | t | 6 | 0.1 | 395.63 | 1265.94 | 0.10 | treated=241/control=96 | 14 | 6537 |
| visit | response | 4 | 0.05 | 4138.66 | 12062.06 | 0.94 | response=1366 | 38 | 6361 |
| visit | response | 4 | 0.1 | 4136.27 | 12059.30 | 0.94 | response=1054 | 30 | 6329 |
| visit | response | 6 | 0.05 | 4151.10 | 12077.11 | 0.94 | response=711 | 28 | 6361 |
| visit | response | 6 | 0.1 | 4152.37 | 12078.61 | 0.94 | response=344 | 16 | 6329 |
| visit | s | 4 | 0.05 | 4308.04 | 12244.02 | 0.94 | s=1369 | 46 | 6478 |
| visit | s | 4 | 0.1 | 4306.23 | 12242.22 | 0.94 | s=716 | 111 | 6519 |
| visit | s | 6 | 0.05 | 4279.79 | 12211.53 | 0.94 | s=926 | 162 | 6321 |
| visit | s | 6 | 0.1 | 4300.57 | 12235.00 | 0.94 | s=354 | 74 | 6285 |
| visit | t | 4 | 0.05 | 4031.89 | 11919.52 | 0.92 | treated=1366/control=650 | 213 | 6445 |
| visit | t | 4 | 0.1 | 3935.04 | 11807.06 | 0.91 | treated=1054/control=364 | 221 | 6479 |
| visit | t | 6 | 0.05 | 4260.65 | 12190.77 | 0.94 | treated=711/control=188 | 33 | 6502 |
| visit | t | 6 | 0.1 | 4137.60 | 12049.43 | 0.92 | treated=344/control=83 | 216 | 6502 |
