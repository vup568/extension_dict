# F-09 expanded corpus preparation validation — 2026-10-13

Corpus: `f09-expanded-20261013.2`. Result: **structural/technical preparation PASS**; final human G1/G2 confirmation pending. The user-approved .1 bytes are preserved and their exact hash/approval scope is recorded in [receipt](../reviews/user-review-receipt-20261013.json).

| Check | Result |
|---|---|
| Finite required inventory | 59 slots: verbs 21, い 12, な 14, noun/copula 12 |
| Complete machine cases | 408 unique case IDs, 17 synthetic canonical dictionary fixtures |
| Outcomes | Supported 303; exact-first 10; unavailable 91; completed-empty/failed/discarded/cancelled 1 each |
| Rule proposals | 107 unique/versioned IDs; each has ≥2 positives + ≥1 forbidden-rule negative |
| Existing baseline | 9/9 IDs reviewed in inventory; 13/13 original positive surfaces retained; source/seed evidence unchanged |
| Components | Every supported alternative has Vietnamese content, canonical fixture/base identity, contiguous half-open UTF-16 spans and exact surface reconstruction |
| Multiple readings | 29 cases with >1 interpretation; v3 count matches alternatives, v2 uses unavailable instead of an arbitrary interpretation |
| Actual Sidecar/gRPC | 365/365 unique synthetic inputs produced responses using the existing pinned Docker image |
| Provider wire format | Every token has six POS fields, valid UTF-16 boundaries and exact reconstruction; supplementary character case included |
| Validator guards | 8/8 corrupted inputs rejected: duplicate ID, blank VI, gap, wrong base/fixture, missing source, v2 ambiguity, fake exact explanation, split surrogate |
| Source snapshots | Both official PDF byte hashes/Last-Modified recorded; external content not imported, reference-only use explicitly bounded |
| Frozen binding | 11 source/material files verified; case/source/material hashes agree; no self-hash or checksum cycle |

Scope SHA-256: `0dcb33a8dd48ba0a4627953d79bdcbbacf1b0e306f7dfe615ccc40c52cd1f1d3`.
Case SHA-256: `c01782fe102c072b774701f53028f901e02f6ba631d5d3abd60c2397f147c980`.

## Commands and evidence

- `node scripts/prepare-corpus.mjs summary` (from this feature folder): finite editorial declarations authored independently of provider actuals.
- `docker run --rm --read-only --network none --cap-drop ALL --security-opt no-new-privileges --mount <feature-folder read-only> jp-reading-tokenizer:verified-20261013 python /corpus/scripts/observe-corpus.py /corpus/corpus/expanded-20261013.2/cases.json`: actual image servicer over loopback gRPC; no host port or database access.
- `node scripts/validate-corpus.mjs corpus/expanded-20261013.2 --self-test --verify-freeze`: all checks above; full result in [JSON report](./expanded-corpus-validation.json).
- Node syntax checks for prepare/validator scripts passed; observer Python executed successfully inside the pinned image.

Image ID: `sha256:14ece806bfac24843e05a79a1c1c899a929621ad0d26ff4c02bf4c894585a1f9`.
Provider: SudachiPy 0.7.0, sudachidict_core 20260723.1, grpcio 1.84.0.
[Raw observations](./expanded-sudachi-observations-20261013.2.json) are separate from expected semantic answers. The final 408 cases use the same 365 unique texts as the observation batch; exact input-set equality is verified.

## Technical implications for implementation

109 supported cases have an unchanged first provider base/surface; current matcher rejects this before matching rules. 24 supported cases have a first provider base different from the expected dictionary base (e.g. 書ける versus 書く). 47 have editorial component boundaries different from raw provider token boundaries. These are counted observations/code constraints, not results of running the completed corpus through the backend matcher. API v3/typed alternatives and fixture-backed lexical/group evidence remain application work after final approval.

Human language correctness remains subject to the appointed user's final review, particularly AMB-02 correction, いい/よい fixture normalization, ambiguous copula で and inferred causative-potential readings. No application matcher, dataset seed, migration, user database or client runtime was changed in this preparation stage.
