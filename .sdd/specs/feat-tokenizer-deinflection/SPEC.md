# FEATURE SPEC: Tokenizer Deinflection & Vietnamese Conjugation

**Version:** 0.1.0 | **Date:** 2026-10-13 | **Status:** Reviewed implementation scope, user-authorized sequence
**Features:** F-06 deinflection + initial F-09 | **Stack:** .NET10/C#14, React19/TypeScript, PostgreSQL16, Python/Sudachi/gRPC
**Inherits:** [Global](../../constraints/global.md), [F-06](../feat-dict-lookup/SPEC.md), [F-08](../feat-grammar-detection/SPEC.md)

## 1. Goal, order and boundaries

OD-010 is resolved. Reuse existing Sidecar/Grammar adapter, then connect Dictionary so
`食べました` finds `食べる` with a Vietnamese explanation visible in popup.
Order: commit OD-010 corrections → existing Docker smoke → F-06 integration → initial F-09/API/popup.
Anonymous reading, ephemeral input, existing dependency versions and Clean Architecture remain.
Whole-word chains only; no sentence vocabulary segmentation, auth, translation, learning,
full F-09 corpus, general morphology or production deployment.
Unified Analysis keeps `tokens=[]`, morphology=`unavailable`.

## 2. Requirements (EARS)

- **EARS[Event]:** WHEN called with text/modeC, Sidecar SHALL return real token surface,
  dictionary form, complete POS and half-open UTF-16 spans using existing protobuf field numbers.
- **EARS[Unwanted]:** WHEN initialization fails, Sidecar SHALL fail unavailable, not return
  successful empty tokens; concurrent tokenization SHALL be synchronized or independently isolated.
- **EARS[Ubiquitous]:** Infrastructure SHALL normalize provider labels using versioned mapping
  data. New F-06/F-09 Domain/Application/API models SHALL not expose raw Sudachi labels or gRPC types.
  The existing Grammar POS representation is retained and covered by regressions.
- **EARS[Unwanted]:** WHEN provider is unavailable or exceeds500ms, adapter SHALL degrade;
  caller cancellation SHALL propagate. Logs SHALL contain no selected text/token surfaces.
- **EARS[Event]:** WHEN exact Dictionary lookup misses, F-06 SHALL match the entire normalized
  token chain against supported rules; base form comes from the sole lexical head's dictionary form.
- **EARS[Ubiquitous]:** Full contiguous chain coverage and auxiliary categories/forms SHALL
  be verified. Suffix stripping, first-token sentence lookup and guessed explanations are prohibited.
- **EARS[Event]:** WHEN base-form lookup finds entries, F-06 SHALL preserve canonical provenance,
  restrictions and Vietnamese-first glosses, and attach the rule's VI explanation.
- **EARS[State]:** WHILE exact match succeeds, its entries win and tokenizer is not called.
  Provider failure must not affect exact lookup. Secondary repository errors SHALL propagate
  rather than be swallowed as tokenizer failure.
- **EARS[Event]:** WHEN F-06 resolves a supported chain with actual Dictionary matches,
  F-09 SHALL expose the same explanation and ordered observed segments through contract2.
- **EARS[Unwanted]:** WHEN chain is unsupported/ambiguous or base entries do not exist,
  no conjugation SHALL be invented. Unsupported chains/missing VI content/dataset report unavailable.

### Initial corpus (9 declarative rules)

| Rule | Regression example | Base form |
|---|---|---|
| Verb polite non-past | `食べます` | `食べる` |
| Verb polite past | `食べました` | `食べる` |
| Verb negative | `食べない` | `食べる` |
| Verb negative past | `食べなかった` | `食べる` |
| Verb plain past | `食べた` | `食べる` |
| Verb polite negative | `食べません` | `食べる` |
| I-adjective past | `高かった` | `高い` |
| I-adjective negative | `高くない` | `高い` |
| I-adjective negative past | `高くなかった` | `高い` |

Validate additional heads `書く/読む/する/来る`; map past auxiliary by conjugation-type
metadata (e.g. surface/base `だ` can be past). Polite-negative `ん` has base `ぬ`.
Rules are draft development knowledge, pending production linguistic signoff.
Causative/passive, te-chains, na-adjectives and polite-negative past remain follow-up corpus.

## 3. Data and contracts

Pure Domain `ConjugationDataset`: Version primary key, Content string mapped to JSONB.
Content holds provider mappings, normalized matcher patterns, VI explanation/segments,
rule identity/version and provenance. PostgreSQL rejects UPDATE/DELETE; new dataset versions
install additively; `ConjugationDatasetVersion` explicitly pins active version. Unset means
adapter unavailable. No canonical ResourceRevision/currentKnowledgeRelease integration;
development seed is not a production-approved release.
Application engine interprets normalized rule data; Infra parses/maps raw provider data.

Keep existing endpoints/requests/error envelope/interaction echo.
Intentional API evolution: `X-Contract-Version: 2` opts into typed conjugations and
meta `contract_version="2"`. Absent header/default1 preserves historical empty conjugations/
unavailable semantics; unknown version returns structured400. Extension sends header2 and
validates both historical1 and new2. Dictionary retains existing Conjugation fields/meaning.

```json
{
  "surface": "食べました", "base_form": "食べる",
  "explanation": "Dạng lịch sự, quá khứ của 食べる.", "language_tag": "vi",
  "span": {"start": 0, "end": 5, "unit": "utf16"},
  "rule_id": "conjugation:verb-polite-past", "rule_version": 1,
  "dataset_version": "development-20261013.1",
  "segments": [
    {"surface": "食べ", "explanation": "Phần động từ.", "span": {"start": 0, "end": 2, "unit": "utf16"}},
    {"surface": "まし", "explanation": "Trợ động từ lịch sự.", "span": {"start": 2, "end": 4, "unit": "utf16"}},
    {"surface": "た", "explanation": "Trợ động từ quá khứ.", "span": {"start": 4, "end": 5, "unit": "utf16"}}
  ]
}
```

Spans refer to trimmed `normalized_text`; segments exactly partition the observed surface.
Usable rule + found entry => completed occurrence; no supported rule/provider/dataset/VI content
=> unavailable empty; supported rule without base entry => completed empty; internal failure => failed empty.
F-09 derives from F-06's result without another tokenizer call; existing per-capability isolation
remains. Caller cancellation must never become successful partial output.
Validator checks nonempty strings, positive rule version, VI language, UTF-16 integer bounds,
surface equality, segment ordering/coverage and status/result consistency. Popup renders safe
React text nodes with base form, VI explanation and segments; Retry/stale/close remain intact.

## 4. Acceptance criteria and evidence

| ID | Required behavior | Evidence |
|---|---|---|
| AC-T01 | Existing image returns real tokens/POS/base forms before feature edits | Live Docker gRPC smoke |
| AC-T02 | Missing provider unavailable; concurrent deterministic; no raw text logs | Runtime/unit/privacy |
| AC-T03 | `𠮷野で食べました` spans UTF-16 and surfaces match slices | Live gRPC + adapter |
| AC-D01 | `食べました` finds canonical `食べる` entry + VI explanation | Real PostgreSQL/Sidecar/API |
| AC-D02 | Exact wins; sentence/unsupported chain has no false lemma | Unit/API negatives |
| AC-D03 | Provider degrades; caller cancel/secondary DB failure propagate | Adapter/usecase failures |
| AC-C01 | 9 cases + verified alternative heads have correct semantics | Engine/live corpus |
| AC-C02 | Unsupported/ambiguous chains and missing VI content never fabricate results | Negative/content tests |
| AC-C03 | Dataset persists immutable; new pinned version works without engine edits | PostgreSQL migration/data |
| AC-C04 | v2 typed result linked found entry; v1 empty; unknown version400 | API integration |
| AC-C05 | v1/v2 valid accepted; bad spans/segments/status rejected | FE validator |
| AC-C06 | Safe VI/base/segments popup; historical Retry/stale behavior intact | UI/browser |
| AC-X01 | Domain/provider isolation and touched workflows regression-free | Review/full suites |

Traceability: VOC-001/004/005/007/008, CONJ-001/002, ARCH-003/008/009,
NET-006, PRIV-002/006, I18N-003, LESSON-001/002.
Build/full .NET/PostgreSQL/Extension suites and relevant browser checks must pass.
Capture timings without claiming OD-005/PERF certification. Synchronize F-06/F-05 docs,
Feature Map/memory and evidence; initial data approval/general morphology remain separate.
