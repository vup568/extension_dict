# F-09 .3 — Validation of corpus preparation

Date: 2026-10-13. Stage: preparation, not runtime acceptance.

- PASS: 60 slots/4 groups,412 cases/115 proposed rule IDs/17 synthetic fixtures.
- PASS: at least2 positive+1 negative/rule;13 original live positives retained;38 multi-alternative cases.
- PASS: expected base identity, Vietnamese/components, full UTF-16 coverage, v2/v3 cardinality.
- PASS:369 unique synthetic inputs observed over real gRPC inside isolated existing Docker image; token spans reconstruct each input. 8 intentionally invalid corpus mutations rejected.
- PASS:12 frozen file bindings and manifest cases/source/material hashes; .1/.2 bytes and old baseline unchanged.
- PENDING:G1/G2 exact answer review, new runtime dataset/matcher/API3/popup, backend semantic comparisons and G3 acceptance. No production claim.

## Commands

`node .sdd/specs/feat-conjugation-explanation/scripts/validate-corpus-20261013.3.mjs .sdd/specs/feat-conjugation-explanation/corpus/expanded-20261013.3 --self-test --verify-freeze`

`docker run --rm --read-only --network none --cap-drop ALL --security-opt no-new-privileges --mount 'type=bind,source=E:\\jp-dict-extension\\.sdd\\specs\\feat-conjugation-explanation,target=/corpus,readonly' jp-reading-tokenizer:verified-20261013 python /corpus/scripts/observe-corpus.py /corpus/corpus/expanded-20261013.3/cases.json`

Image already present; no package installation/download, no host ports, no user DB/volume mutations. Docker initially unavailable; after user enabled Engine29.6.1, run succeeded. Provider:SudachiPy0.7.0/core20260723.1/grpcio1.84.0. RPC observation timeout5s is not a claim that backend500ms performance has been verified.

## Interpretation of results

Observed tokens are technical metadata, not an oracle for meaning/VI.109 supported cases have unchanged provider head;24 differ from expected canonical base;47 have component layout different from provider tokens. Those are retained as implementation-gap evidence, not rewritten into expected answers.

A validation helper initially referenced .2 observations; its version check rejected the mismatch. Corrected to .3. Initial freeze helper incorrectly rejected a prior snapshot with the same filename as the current material; corrected to reject only self-reference, re-bound validator hash before any human approval. Final checks pass; no expected linguistic answers changed to fit actual output.

## Frozen identities

- Corpus:`f09-expanded-20261013.3`
- Cases SHA-256:`ca3e3ff71b4d97a96e33c70695d5cfb067244bb6cf63a7d3340cd901dd7883c4`
- Sources SHA-256:`13e9637e25e00cbd77c3d7c49a103e42f165c18ba7bbc91dacb57a08144ab8d2`
- Material SHA-256:`01997f4191863a4beb04e6872ea5f21145560da0c368eec043c209a37ea7889f`
- Observations SHA-256:`bcd160c8ca110d65f93fe38d02f8e90185d1cb2723bf338721472037e02899ce`

Raw outputs:[JSON report](./expanded-corpus-validation-20261013.3.json),[observations](./expanded-sudachi-observations-20261013.3.json). Human review:[case-review](../corpus/expanded-20261013.3/case-review.md).

