# F-09 — Spec compliance, 2026-10-14

## Summary

Implementation requirements pass for the approved scoped candidate; independent quality review follows. Human G3 acceptance, Firefox runtime and production publication are not claimed.

| Requirement | Evidence | Status |
|---|---|---|
| FR-001/002; SC-001 | .3 frozen catalog:60 slots/four groups; user .3 G1 receipt bound to actual hashes | PASS |
| FR-003; SC-003 |412 cases;115 rules each≥2 positives+1 negative;404 linguistic cases through real Sudachi/PostgreSQL;7 fault cases and browser async case | PASS implementation |
| FR-004/005; SC-002/005 |User appointed self reviewer; explicit .3 batch approval,G2 dossier/source-reference-only boundary; no expected rewrites | PASS |
| FR-006 |Separate candidate,909 parameterized realizations,115 stable reviewed rule IDs; new resource/migration only INSERT | PASS |
| FR-007 |Corpus compares approved overall/component VI,base/rule/dataset and UTF16 spans;v3 all meanings;2/3 separate popup cards, no ranking | PASS |
| FR-008; SC-004 |Exact-first,canonical POS/joint sense restrictions,missing base/content/down/deadline/cancel;v1 empty/v2 one-or-none/v3 alternatives;real HTTP→client parser | PASS |
| FR-009 |Frozen hash guard,reproducible compilation,semantic JSONB comparison,old-version replay/immutability;G3 must bind final report | PASS candidate; human G3 pending |
| FR-010 |Anonymous HTTP,provider logs no synthetic selection;client no storage/log leakage,stale/close/Retry;development status remains | PASS |

## Scout and remediation

Backend/client scouts were read-only. Addressed joint form/reading sense restrictions with red→green tests; removed unreviewed v5r-i from regular godan rows; tightened v3 canonical evidence; strengthened component VI/span checks and genuine pending-v3 stale/close browser tests. Preserved old migration/seed bytes and frozen corpus.

## Next steps

Independent code/data quality review, fresh full suite after any fixes, then PO runtime acceptance. No new dependency, user DB migration, Git delivery or production authority inferred.
