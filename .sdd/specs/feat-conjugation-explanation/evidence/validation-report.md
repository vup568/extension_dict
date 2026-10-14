# F-09 — Candidate validation, 2026-10-14

## Summary

Expanded F09 implementation and local verification complete for the approved corpus .3. Candidate remains development,opt-in. User G1/G2 recorded;human G3 runtime acceptance is pending,not inferred from request to implement. No deployment/user DB migration/Git delivery.

## Frozen identities

| Artifact | Version/SHA256 |
|---|---|
|Corpus|f09-expanded-20261013.3;412 cases/60 slots/four groups/115 rules/17 fixtures|
|Cases|ca3e3ff71b4d97a96e33c70695d5cfb067244bb6cf63a7d3340cd901dd7883c4|
|Scope material|01997f4191863a4beb04e6872ea5f21145560da0c368eec043c209a37ea7889f|
|Sources|13e9637e25e00cbd77c3d7c49a103e42f165c18ba7bbc91dacb57a08144ab8d2|
|Review|f09-user-approval-20261013.3;actual user .3 batch approval receipt|
|Candidate|candidate-20261013.1;c46a28613e9cbd81c60cda51f8114f023c86e001f8daab5218fe49a8bc481e35|
|Regular row data|godan-rows-20261013.1;afd726932079e1063e8ca8ce5ac58c37cc6ed338a65d5e9bf86b58cf0e751233|

Candidate is909 parameterized grammar realizations/115 reviewed IDs,not412 surface-answer lookup records. Offline compiler reproduces semantic JSON from reviewed answers+versioned rows;runtime never reads corpus. Canonical class/sense verification happens before public identity dedupe.

## Test results

| Gate | Result |
|---|---|
|Release build|0 warnings/0 errors|
|Backend unit|79/79;0 failed/skipped|
|PostgreSQL/gRPC integration|118/118;0 failed/skipped|
|Corpus|412/412 mapped:404 linguistic real-Sudachi/SQL cases+7 fault scenarios+ASYNC-01 real-browser stale response|
|Python Sidecar|12/12|
|Client unit|251/251;0 failed/skipped|
|Browser|136/136:Chrome45/Edge45/Brave45/packaged1;0 failures/skips/flaky|
|Client typecheck/Unicode/build|PASS;no new package/browser installed|
|Modified usecase line coverage|Dictionary97.95%,Analysis87.50%;matcher100%|

Commands/raw artifacts and prior failed attempts with root-cause resolutions: [backend-validation.md](./backend-validation.md),[client-validation.md](./client-validation.md). Full corpus compares approved meanings/component VI/base/identities and all UTF16 spans;exact cases assert canonical matches. Current required-slot gaps=0 in approved finite catalog;not a claim of all Japanese grammar.

## Traceability and review

| SPEC | Evidence |
|---|---|
|FR-001–003;SC-001/003|Frozen catalog/per-rule≥2 positives+1 negative,G1 receipt,full corpus guard/integration|
|FR-004/005;SC-002/005|User-appointed reviewer,batch G2,source reference-only/project-authored boundary,0 requested linguistic corrections|
|FR-006;SC-003|Additive candidate/resource/migration;source hashes and reproducible compiler;no old seed rewrites|
|FR-007/008;SC-004|Two/three readings,whole word,canonical POS/joint restrictions,exact/unsupported/missing/fault,UTF16,v1/v2/v3/unknown400,HTTP→client parser,browser async/privacy|
|FR-009|Frozen hash guards,JSONB semantic equality,old replay/immutability;acceptance candidate binds final report;G3 pending|
|FR-010|Anonymous/no raw selection logs,no production/DB/Git authority inferred|

[Spec compliance](./spec-compliance.md),[independent review](./final-review.md): P1 early class dedupe fixed and regression verified;cycle2 no outstanding P1/P2. Old dataset/seed/migration retain exact baseline hashes;all .1/.2/.3 frozen materials preserved. Sources are consulted/linked only;external prose/images not redistributed.

## Remaining gates

- Human PO G3 approval of this runtime evidence/candidate;draft acceptance binding is not a human signature.
- Firefox executable unavailable;build manifest is not runtime parity evidence. No browser downloaded.
- Production publication/dataset→KnowledgeRelease eligibility remains F-20/separate authorization. PERF/OD-005 and general morphology/sentence segmentation are outside this work.

Next: user reviews this report and confirms scoped runtime acceptance;only then freeze accepted G3 manifest. No commit/push/merge was executed.
