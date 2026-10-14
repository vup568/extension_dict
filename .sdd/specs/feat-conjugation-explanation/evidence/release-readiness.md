# F-09 — Release readiness, 2026-10-14

## Current state

Implemented/tested `candidate-20261013.1`;corpus .3 has actual G1/G2/G3 user approval. [Accepted scoped manifest](./acceptance-manifest.json) records G3 on2026-10-14. Parser keeps provenance.status=development;acceptance does not publish or activate production.

## Activation boundary

Local opt-in after separately authorized database migration:existing `GrpcTokenizerAddress` plus `ConjugationDatasetVersion=candidate-20261013.1`. Missing pin retains Null adapter;old development pin remains available/replayable. No migration/start of the user's backend or production service executed in this task.

Production activation requires approved publication binding dataset version/hash/provenance/review to eligible KnowledgeRelease data (F-20 dependency). F09 tests,pin or local INSERT do not authorize publishing. No schema/ResourceRevision expansion,deployment,commit,push,PR or merge inferred.

## Remaining gates

Firefox parity,publication/F-20,and separately scoped performance/OD-005. PO runtime G3 completed;no further F09 implementation task. Sentence segmentation/general morphology remain outside F09. Reference-only sources do not grant redistribution of external text/images;candidate VI/examples independently authored.
