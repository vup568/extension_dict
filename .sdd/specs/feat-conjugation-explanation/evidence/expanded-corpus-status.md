# F-09 expanded corpus status — 2026-10-13

Latest packet: **f09-expanded-20261013.2**, [review entry](../corpus/expanded-20261013.2/case-review.md).
User accepted the presented .1 draft; [receipt](../reviews/user-review-receipt-20261013.json) preserves that acceptance and exact bytes. The completed .2 contains added details and is pending final G1/G2 confirmation.

| Item | Current result |
|---|---|
| Inventory | 59 required slots across four word groups |
| Complete cases | 408 cases; 107 proposed rule IDs; 17 synthetic dictionary fixtures |
| Expected answers | All supported alternatives have VI content, base-entry identity and full UTF-16 components/spans |
| Source records | HTTP snapshots/checksums verified; reference-only use documented; original project editorial content pending final reviewer acceptance |
| Sidecar/gRPC | 365 unique inputs observed in isolated Docker; all wire spans/reconstruction checked |
| Coverage checks | Every rule ≥2 positives+1 negative; original 9 rules and 13 live positives carried |
| Frozen scope | SHA-256 `0dcb33a8dd48ba0a4627953d79bdcbbacf1b0e306f7dfe615ccc40c52cd1f1d3`; 11 frozen files bound by [scope material](../corpus/expanded-20261013.2/scope-material.json) |
| Preparation tasks | T001–T010 complete (10/42); T011/T012 and later gates remain pending |
| Runtime implementation | Pending final approval; no application source or dataset migration changed |

Full technical checks: [validation report](./expanded-corpus-validation.md). Previous nine-rule-only and .1 review artifacts remain intact as separate history. Choosing B and accepting .1 do not falsely sign the new .2 content or a production release.
