# F-09 — Independent technical review, 2026-10-14

## Summary

Reviewer: independent agent `/root/f09_final_quality` under ck:code-review,read-only;not author of implementation and not human linguistic/PO signoff. Spec-compliance pass preceded quality review. Final cycle2: no outstanding P1/P2. G3 remains for the user.

## Findings and resolution

| Cycle | Finding | Evidence/resolution |
|---|---|---|
|1|P1:decoder deduped rule/version/base before canonical POS,discarding valid godan-r realization of 帰られる|Two new unit regressions red;decoder preserves distinct POS realizations/components,lookup dedupes only after canonical verification. Never union incompatible component shapes.|
|2|No remaining P1/P2 found|Reviewer audited16 affected godan-r passive/honorific realizations;ichidan three interpretations retained;source/data/reproducibility/old hashes/parser/migration reviewed.|

Scouts additionally identified v5r-i regular-row overreach,weak segment VI/span checks,canonical match trust and ineffective stale-v3 test. All addressed before quality review;see [spec-compliance.md](./spec-compliance.md).

## Parent verification

After fix/re-review: Release0 warnings/errors;unit79/79;fullintegration118/118 including live帰られる,404 linguistic corpus cases,actual HTTP→TS parser,old replay/immutability and provenance guards. FE251/251,browser136/136,Python12/12. No test skipped or expected corpus rewritten.

## Boundaries

Technical source/data review does not approve Japanese semantics beyond user G1/G2,PO runtime G3,Firefox parity,performance or production publication. Missing auxiliary skill reviewer template was handled using SKILL/SPEC criteria;no review gate skipped.
