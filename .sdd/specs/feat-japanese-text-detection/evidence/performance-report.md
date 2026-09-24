# F-01 Detector Performance Report

**Executed:** 2026-09-20
**Runtime:** Node.js v22.15.0, win32 x64
**Policy:** Unicode 17.0.0
**Status:** Measurements complete; threshold result pending OD-005

| Profile | UTF-16 length | Samples | Cold ms | Warm p95 ms |
|---|---:|---:|---:|---:|
| small positive first | 16 | 2000 | 0.0552 | 0.0022 |
| small positive last | 16 | 2000 | 0.0249 | 0.0007 |
| small negative | 16 | 2000 | 0.0055 | 0.0007 |
| small supplementary last | 16 | 2000 | 0.0032 | 0.0004 |
| medium positive first | 1000 | 1000 | 0.0045 | 0.0001 |
| medium positive last | 1000 | 1000 | 0.0236 | 0.0186 |
| medium negative | 1000 | 1000 | 0.0194 | 0.0183 |
| medium supplementary last | 1000 | 1000 | 0.0217 | 0.0324 |
| large positive first | 100000 | 200 | 0.0943 | 0.0001 |
| large positive last | 100000 | 200 | 1.8691 | 2.3967 |
| large negative | 100000 | 200 | 1.7455 | 2.3988 |
| large supplementary last | 100000 | 200 | 1.7740 | 2.3014 |

The benchmark executes the complete input and validates each classification while timing it. No detector-specific numeric budget has been approved; therefore these measurements do not claim F01-SC-005 passed and are not compared with the end-to-end 150 ms popup-flow objective.
