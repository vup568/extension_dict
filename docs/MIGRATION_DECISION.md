# JP Reading Platform — V2 Migration Decision

**Status:** Approved for V2 Architecture Design  
**Version:** 1.0.0  
**Target Requirements:** `docs/REQUIREMENT.md`

---

## 1. Purpose

This document defines how the existing Japanese Dictionary Popup prototype
shall be treated during development of JP Reading Platform v2.

The current implementation is considered a **legacy prototype**.

The v2 architecture is NOT required to preserve the current implementation,
folder structure, runtime architecture, dependencies, storage mechanisms,
browser-specific integrations, or internal APIs.

The authoritative target product behavior is defined by:

`docs/REQUIREMENT.md`

This document determines which legacy assets may be reused, migrated,
reimplemented, investigated, or removed.

---

# 2. Migration Strategy

V2 SHALL follow a controlled greenfield strategy.

The project SHALL preserve valuable:

- linguistic knowledge;
- upstream datasets;
- canonical source identifiers;
- regression test cases;
- edge cases;
- data provenance;
- licensing information;
- useful user-facing behavior.

The project SHALL NOT preserve legacy implementation merely because it is
currently functional.

The migration principle is:

```text
Legacy Prototype
       │
       ▼
Extract Knowledge + Tests + Data
       │
       ▼
Design V2 Architecture
       │
       ▼
Reimplement Required Capabilities
       │
       ▼
Validate Against Regression Corpus
       │
       ▼
Remove Obsolete Legacy Runtime
```

Backward compatibility with legacy internal architecture is NOT a requirement.

---

# 3. Decision Categories

Legacy assets SHALL be classified using the following categories.

## PRESERVE

The asset represents knowledge, source data, behavior, tests, or project
infrastructure that remains directly valuable to v2.

Preservation does NOT necessarily mean preserving the current implementation.

## MIGRATE

The underlying knowledge or behavior remains valuable, but it SHALL be moved
into a new representation or architectural boundary.

## REIMPLEMENT

The capability remains required by the target product, but the current
implementation SHALL NOT be considered the basis of the v2 implementation.

## REMOVE

The asset belongs to the obsolete architecture and SHALL NOT exist as part of
the final v2 runtime.

## INVESTIGATE

The asset may be useful, but correctness, quality, licensing, architecture
suitability, or long-term maintainability must be resolved before adoption.

---

# 4. Assets to Preserve

## 4.1 Product Requirements

`docs/REQUIREMENT.md` SHALL remain the authoritative source for target product
behavior and scope.

Legacy README files, source code, tests, or implementation behavior SHALL NOT
override approved requirements.

---

## 4.2 Spec Kit Tooling

The following project tooling SHOULD be preserved:

- `.specify/`
- `.agents/skills/`

These directories are development workflow infrastructure and are not part of
the runtime architecture.

---

## 4.3 Type Safety

Strict compile-time type checking SHOULD remain an engineering standard for
TypeScript packages.

The existence of TypeScript in the legacy project SHALL NOT require every v2
component to use TypeScript if architecture planning identifies a better
technology for a particular component.

---

## 4.4 Linguistic Regression Knowledge

Existing verified linguistic behavior SHALL be preserved as regression
knowledge whenever it represents desired product behavior.

High-value examples include:

- grammar positive examples;
- expression inflection cases;
- expression offsets;
- repeated expressions;
- overlapping expressions;
- negative expression cases;
- morphological base-form examples;
- irregular conjugation cases;
- noun-compound cases;
- known false-positive cases.

The legacy implementation itself does not need to be preserved in order to
preserve these behaviors.

---

# 5. Assets to Migrate as Data or Knowledge

## 5.1 JMdict

JMdict SHALL remain a primary lexical source candidate for v2.

The migration SHALL preserve, where provided by the source:

- upstream entry identifiers;
- written forms;
- readings;
- senses;
- parts of speech;
- form restrictions;
- reading restrictions;
- sense restrictions;
- other metadata required for linguistically correct lookup.

The legacy transformed representation SHALL NOT be treated as canonical.

V2 SHALL derive its canonical lexical representation directly from verified
upstream source data.

---

## 5.2 KANJIDIC2

KANJIDIC2 SHALL remain a primary kanji source candidate.

Relevant source information SHOULD be migrated into a canonical backend
representation.

Raw/source information and derived values SHALL be distinguishable where
possible.

Approximate or derived JLPT mappings SHALL NOT be represented as authoritative
upstream values.

---

## 5.3 Grammar Knowledge

The existing N5–N4 grammar corpus SHALL be migrated as knowledge, not copied as
runtime TypeScript rules.

Each grammar record SHOULD support:

- stable canonical ID;
- grammar pattern;
- JLPT level;
- localized meanings;
- formation information;
- examples;
- matcher metadata;
- provenance/source information.

Grammar knowledge SHALL become versioned data separate from the grammar
execution engine.

---

## 5.4 Expression Knowledge

Useful expression behavior and generated expression cases SHALL be migrated.

The v2 expression index SHALL be regenerated from canonical lexical data rather
than copied from legacy browser-packed assets.

Existing expression regression tests SHALL be preserved where behavior remains
valid.

---

## 5.5 Vietnamese Lexical Overrides

Existing Vietnamese lexical override knowledge SHALL NOT be discarded without
review.

Overrides SHOULD be migrated as explicit editorial records with:

- target canonical entry/form;
- replacement or supplemental meaning;
- reason;
- provenance where available;
- version/change history where practical.

Overrides SHALL NOT remain as unexplained constants scattered through runtime
code.

---

## 5.6 Conjugation Knowledge

Existing Vietnamese conjugation and auxiliary descriptions SHALL be treated as
knowledge seeds.

They SHOULD be migrated into localized linguistic data or an equivalent
maintainable representation.

The legacy conjugation implementation SHALL NOT be preserved solely because
these descriptions currently live inside it.

---

## 5.7 Radical Knowledge

The existing 214-radical Vietnamese reference information MAY be migrated if
its correctness and provenance are acceptable.

Radical knowledge SHOULD become versioned data rather than application
constants.

---

## 5.8 Selection and Ruby Behavior

Useful selection behavior SHALL be preserved at the behavioral level,
including:

- preservation of Japanese base text;
- exclusion of ruby annotation text from linguistic input;
- protection from stale asynchronous responses.

Legacy DOM implementation MAY be used as reference material but SHALL NOT
define the v2 architecture.

---

# 6. Capabilities to Reimplement

The following capabilities are still required, but the legacy implementation
SHALL be replaced.

---

## 6.1 Overall Runtime Architecture

The legacy extension-only architecture SHALL be replaced.

V2 SHALL be designed around:

- shared backend knowledge services;
- browser extension client;
- web application client;
- shared API contracts;
- future learning services.

---

## 6.2 Dictionary ETL

The existing dictionary-generation pipeline SHALL be replaced with a
reproducible backend-oriented ETL pipeline.

The new pipeline SHOULD provide:

- pinned source versions;
- source checksums;
- schema validation;
- deterministic transformations where practical;
- source manifests;
- row-count validation;
- rejected-record reporting;
- atomic or controlled publication.

ETL behavior SHALL NOT silently discard linguistic restrictions or source
metadata required by v2.

---

## 6.3 Dictionary Lookup

Dictionary lookup SHALL be reimplemented on the backend.

Lookup results SHALL preserve match provenance, including relevant matched:

- written form;
- reading;
- applicable sense or senses;
- source restrictions.

Legacy browser lookup result shape SHALL NOT constrain the new API.

---

## 6.4 Japanese Text Detection

Japanese detection SHALL be reimplemented using correct Unicode-aware
semantics.

Known legacy false positives SHALL become regression tests.

---

## 6.5 Morphology and Deinflection

Morphological analysis SHALL be reimplemented as a backend capability.

The design SHALL separate:

- tokenizer/provider integration;
- normalized token representation;
- morphology;
- dictionary lookup;
- conjugation;
- presentation.

Provider-specific labels SHALL NOT leak throughout unrelated domain modules.

---

## 6.6 Grammar Engine

The legacy grammar matcher SHALL be replaced.

The new grammar engine SHALL operate on versioned grammar data and SHALL
preserve:

- canonical grammar IDs;
- exact occurrence identity;
- occurrence spans;
- repeated matches;
- meaningful overlap information where required.

---

## 6.7 Expression Matcher

The expression matcher SHALL be reimplemented using shared backend morphology
and canonical lexical data.

The current regression suite SHOULD be used to validate intended behavior.

---

## 6.8 Conjugation Analysis

Conjugation analysis SHALL be reimplemented using normalized linguistic
semantics rather than provider-specific presentation logic.

Localized descriptions SHALL be separated from core conjugation analysis.

---

## 6.9 Sentence Extraction

Browser sentence/context extraction SHALL be redesigned and tested.

The new implementation SHALL explicitly handle context and privacy boundaries.

Legacy handwritten punctuation and truncation behavior SHALL NOT be assumed to
be correct.

---

## 6.10 Popup UI

The extension popup SHALL be rebuilt for the v2 interaction model.

The popup SHALL support:

- immediate shell rendering;
- loading state;
- structured error state;
- retry;
- stale-response protection;
- authentication/save flow where applicable.

Legacy visual behavior MAY be referenced selectively but is not binding.

---

## 6.11 Browser Service Worker

The legacy service worker SHALL be replaced.

The v2 service worker/background runtime SHOULD remain thin and focus on
browser responsibilities such as:

- lifecycle;
- browser APIs;
- permissions;
- authentication/session coordination where needed;
- backend communication.

It SHALL NOT act as the canonical dictionary, grammar, or tokenizer runtime.

---

## 6.12 API Contracts

Legacy extension-internal lookup, kanji, grammar, and translation message DTOs
SHALL be replaced.

Extension and Web clients SHALL use shared, versioned backend API contracts.

System boundaries SHOULD provide runtime validation and structured errors.

---

## 6.13 Translation

Translation SHALL be reimplemented behind a provider-independent backend
interface.

The client SHALL NOT be directly coupled to a specific external translation
provider.

Translation SHALL follow the on-demand behavior defined by
`docs/REQUIREMENT.md`.

---

## 6.14 Cross-Browser Integration

Browser integration SHALL be redesigned for:

- Chrome;
- Edge;
- Brave;
- Firefox.

Browser-specific APIs SHALL be isolated behind appropriate platform boundaries.

Core linguistic and application logic SHALL NOT depend directly on Chrome-only
APIs.

---

## 6.15 UI Localization

Hard-coded Vietnamese/English presentation fields and duplicated localized UI
logic SHALL be replaced with an intentional localization model.

Vietnamese remains the MVP primary interface language.

The architecture SHALL remain capable of supporting full English localization
later.

---

# 7. Assets to Remove From V2

The following legacy runtime assets SHALL NOT form part of the final v2
architecture.

---

## 7.1 Browser-Side Canonical Dictionary

The browser extension SHALL NOT contain the authoritative full dictionary.

---

## 7.2 Full IndexedDB Dictionary

The existing IndexedDB dictionary/import architecture SHALL be removed.

IndexedDB or equivalent browser storage MAY later be used for small local
concerns such as:

- preferences;
- cached results;
- offline queue;
- session-support data.

It SHALL NOT become the canonical v2 knowledge store.

---

## 7.3 Browser-Side Kuromoji/IPADIC Packs

Large tokenizer/dictionary packs SHALL NOT be distributed as required extension
runtime assets in the MVP.

---

## 7.4 Generated Browser Dictionary Packs

Legacy generated assets such as browser dictionary chunks and packed expression
indexes SHALL be removed once their useful test/data knowledge has been
extracted.

---

## 7.5 Direct Google Translation Integration

Direct calls from extension runtime to unofficial Google translation endpoints
SHALL be removed.

---

## 7.6 Chrome Translator Dependency

Chrome on-device Translator API SHALL NOT be a required v2 translation
dependency.

---

## 7.7 Google Translation Host Permission

Legacy host permissions required solely for direct Google translation SHALL be
removed.

---

## 7.8 Chrome-Specific Core Coupling

Direct Chrome assumptions SHALL be removed from core product/domain logic.

---

## 7.9 Legacy Local Analysis Contracts

Separate browser-local dictionary, kanji, and grammar orchestration contracts
SHALL be removed in favor of the shared backend analysis architecture.

---

## 7.10 Legacy Generated Build Output

Old generated `dist` output and obsolete runtime artifacts SHALL NOT be carried
forward as v2 source assets.

---

# 8. Assets Requiring Investigation

## 8.1 FVDP / OVDP

FVDP/OVDP SHALL NOT automatically be accepted as production data.

Before production ingestion, the project SHALL investigate:

- canonical/original source;
- license;
- relationship between original data and mirrors;
- redistribution obligations;
- transformation obligations;
- data quality;
- identity/linking quality against canonical dictionary entries.

Development experiments MAY continue using existing data where legally
appropriate, but production adoption requires explicit approval.

---

## 8.2 Japanese Tokenizer / Morphology Provider

Kuromoji/IPADIC MAY remain a candidate backend tokenizer provider.

V2 SHALL NOT commit to it until architecture evaluation considers:

- linguistic quality;
- backend suitability;
- maintenance status;
- performance;
- licensing;
- dictionary size;
- provider-specific coupling;
- alternatives.

The architecture SHALL expose a normalized tokenizer boundary so the domain
logic is not permanently locked to one provider.

---

## 8.3 UI Framework

Preact SHALL NOT automatically be preserved.

The frontend architecture plan SHALL determine whether to:

- retain Preact;
- use another framework;
- share components between Web and Extension;
- use different UI entry points with shared packages.

Framework selection SHALL be based on v2 requirements rather than legacy
investment alone.

---

## 8.4 Shadow DOM Strategy

Host-page style isolation remains required for the browser popup.

Whether the implementation uses:

- open Shadow DOM;
- closed Shadow DOM;
- another appropriate isolation approach;

SHALL be determined during architecture/design planning.

The implementation SHALL address:

- style isolation;
- focus;
- keyboard accessibility;
- positioning;
- hostile host CSS;
- SPA lifecycle behavior.

---

# 9. Regression Corpus to Freeze Before Legacy Removal

Before removing significant legacy linguistic implementation, the project SHALL
create or preserve a provider-neutral regression corpus.

At minimum it SHOULD cover the following categories.

---

## 9.1 Japanese Detection

Include:

- valid Japanese;
- mixed Japanese/Latin;
- Hangul;
- emoji;
- musical symbols;
- supplementary Unicode;
- `𠮟`;
- unrelated scripts.

Known incorrect legacy results SHALL be represented as bug regressions, not
expected behavior.

---

## 9.2 Browser Selection

Include:

- ruby;
- `<rt>`;
- `<rp>`;
- nested DOM nodes;
- `<br>`;
- multiple blocks;
- contenteditable;
- relevant iframe cases;
- detached/changed ranges where applicable.

---

## 9.3 Async Client Behavior

Include:

- immediate popup shell;
- debounce;
- selection A followed by selection B;
- stale response rejection;
- cancellation;
- timeout;
- retry;
- translation target switching;
- proof that translation is not automatically requested when the MVP behavior
  requires explicit user action.

---

## 9.4 Vocabulary and Morphology

Include representative cases for:

- exact forms;
- kana-only lookup;
- alternate written forms;
- alternate readings;
- restricted senses;
- homographs;
- inflected verbs;
- inflected adjectives;
- irregular verbs;
- passive;
- causative;
- causative-passive;
- noun compounds.

Existing useful examples such as:

- `食べました → 食べる`;
- `高くなかった → 高い`;
- `熱中症`;

SHOULD be retained.

---

## 9.5 Grammar

Preserve all currently verified positive grammar examples where the intended
behavior is still valid.

Add coverage for:

- negative examples;
- overlaps;
- repeated occurrences;
- nested patterns;
- exact spans;
- stable grammar IDs.

---

## 9.6 Expressions

Preserve useful tests for:

- exact expressions;
- inflected expressions;
- offsets;
- repetitions;
- overlaps;
- negative cases;
- gloss fallback.

---

## 9.7 Kanji

Include representative:

- common kanji;
- rare kanji;
- supplementary ideographs;
- Hán Việt normalization;
- radical variations;
- missing fields;
- missing/derived JLPT values.

---

## 9.8 ETL

New ETL tests SHOULD cover:

- pinned source identifiers;
- checksums;
- schema validation;
- duplicate IDs;
- duplicate forms;
- referential integrity;
- source restrictions;
- source record counts;
- deterministic/repeatable output where required;
- license manifest presence.

---

## 9.9 API and Privacy

Include:

- Extension/Web semantic parity;
- runtime schema validation;
- structured errors;
- partial failures;
- rate-limit responses;
- retry metadata;
- redacted logs;
- request-context minimization;
- cache correctness.

---

## 9.10 Learning

Include:

- anonymous lookup;
- Save requiring authentication;
- resume-after-login where supported;
- idempotent Save;
- duplicate prevention;
- user/tenant isolation;
- Unicode-safe CSV/TSV export.

---

## 9.11 Cross-Browser UI

Acceptance coverage SHALL eventually include:

- Chrome;
- Edge;
- Brave;
- Firefox;
- hostile page CSS;
- SPA navigation;
- scrolling;
- resize/reposition;
- keyboard/focus accessibility;
- loading;
- error;
- retry.

---

# 10. Legacy Deletion Policy

Legacy code SHALL NOT be preserved indefinitely merely for historical purposes.

Git history and an explicit legacy checkpoint SHALL provide historical
recovery.

Before deleting a legacy subsystem, the project SHOULD ensure that valuable:

- regression tests;
- raw/reference data;
- stable identifiers;
- linguistic examples;
- editorial overrides;
- provenance/licensing information;

have been extracted or intentionally rejected.

Once this has occurred, obsolete implementation MAY be deleted even if its code
has not been migrated line-by-line.

V2 does NOT require implementation parity with legacy internals.

V2 requires approved **product behavior and semantic correctness**.

---

# 11. Legacy Git Checkpoint

Before large-scale removal or restructuring of legacy source, the repository
SHOULD have a recoverable Git checkpoint.

Recommended example:

```text
v0.1-legacy-prototype
```

The checkpoint exists for historical recovery and comparison.

It SHALL NOT imply that legacy source must remain in the active v2 tree.

---

# 12. README Decision

The current README SHALL NOT be treated as the v2 architecture specification.

Useful examples and historical behavior MAY be extracted into:

- requirements;
- tests;
- migration documentation;
- data/provenance records.

Once the v2 project foundation accurately represents the active system, the
legacy README SHOULD be replaced with a new README describing the verified
current v2 project.

Git history is sufficient for retaining obsolete README versions.

---

# 13. Data Provenance Decision

Production linguistic data SHALL be traceable.

For external production datasets, the project SHOULD maintain sufficient
metadata to identify:

- canonical publisher/source;
- exact release, tag, version, or commit;
- source checksum;
- normalized artifact checksum where appropriate;
- license/SPDX expression where available;
- required notice and attribution;
- transformation version;
- record counts;
- validation results;
- rejected-record information where applicable.

Knowledge whose source or licensing cannot be reasonably established SHALL NOT
silently become production canonical data.

---

# 14. Hard-Code Migration Policy

V2 SHALL distinguish between domain knowledge, configuration, and ordinary code
constants.

## Domain knowledge SHOULD become versioned data or editorial records

Examples:

- grammar rules;
- radical information;
- lexical overrides;
- localized conjugation descriptions;
- linguistic mapping tables.

## Operational policy SHOULD be centralized configuration where appropriate

Examples:

- timeout;
- debounce;
- cache limits;
- request limits;
- result page sizes.

## Presentation values MAY remain UI constants/design tokens

Examples:

- popup dimensions;
- spacing;
- animation timings.

The project SHALL NOT adopt a blanket rule that all constants must be stored in
a database.

The goal is to prevent important linguistic/product knowledge from being
scattered invisibly through runtime implementation.

---

# 15. V2 Architecture Boundary Decision

The expected conceptual direction is:

```text
Browser Extension ─┐
                   │
                   ▼
              Backend API
                   ▲
                   │
Web Application ───┘
```

The Backend SHALL be the authoritative runtime boundary for:

- canonical dictionary data;
- kanji data;
- morphology;
- grammar analysis;
- translation orchestration;
- persistent learning data.

Clients SHALL remain comparatively thin.

This diagram defines responsibility boundaries, not final technology choices.

---

# 16. Learning Identity Decision

Persistent learning resources SHOULD reference canonical domain identities.

Examples:

```text
SavedVocabulary
→ userId
→ canonicalDictionaryEntryId
```

and:

```text
SavedGrammar
→ userId
→ canonicalGrammarId
```

Localized descriptions or presentation strings SHOULD NOT be the primary
identity of saved learning resources.

This allows linguistic content to be corrected or expanded without breaking a
user's saved library.

---

# 17. Provider Independence Decision

External providers and replaceable linguistic infrastructure SHOULD be isolated
behind explicit boundaries.

Candidates include:

- translation providers;
- tokenizer/morphology providers;
- authentication providers;
- future external learning integrations.

The Constitution MAY later elevate provider independence into a project-wide
principle where appropriate.

This decision SHALL NOT be interpreted as requiring unnecessary abstraction for
every internal module.

---

# 18. Migration Order

The intended high-level migration order is:

1. Stabilize and approve `docs/REQUIREMENT.md`.
2. Approve this migration decision.
3. Create a recoverable legacy Git checkpoint.
4. Freeze high-value behavioral regression cases.
5. Create the v2 Constitution.
6. Specify the v2 architecture/foundation.
7. Clarify unresolved architecture questions.
8. Create and approve the architecture plan.
9. Scaffold the clean v2 project structure.
10. Establish shared API/domain contracts.
11. Establish reproducible data ingestion.
12. Implement canonical dictionary and kanji data.
13. Select and normalize morphology infrastructure.
14. Migrate grammar/expression/conjugation knowledge.
15. Implement shared analysis services.
16. Implement translation abstraction.
17. Implement Web and Extension clients.
18. Implement authentication and learning persistence.
19. Implement review/export capabilities.
20. Validate v2 against the approved regression corpus.
21. Remove obsolete legacy runtime and dependencies.
22. Replace legacy README documentation.

Detailed task decomposition SHALL be performed through Spec Kit rather than
being frozen by this document.

---

# 19. Explicit Non-Decisions

This document intentionally does NOT decide:

- backend programming language;
- backend framework;
- web framework;
- database technology;
- ORM;
- hosting provider;
- cloud provider;
- cache technology;
- exact API protocol/design;
- translation provider;
- authentication provider;
- tokenizer/morphology provider;
- exact repository layout;
- monorepo tooling;
- Shadow DOM open/closed mode;
- final performance thresholds;
- final rate-limit thresholds.

These decisions belong to architecture specification, clarification, and
planning.

---

# 20. Approval Statement

By approving this document, the project accepts the following migration
position:

1. The existing codebase is a legacy prototype, not the architecture to
   preserve.
2. V2 may be redesigned substantially or rebuilt from a clean foundation.
3. Linguistic knowledge, data sources, stable identifiers, useful edge cases,
   tests, and provenance are more valuable than preserving legacy runtime code.
4. Required capabilities may be reimplemented rather than refactored.
5. Obsolete client-side dictionary/tokenizer/translation architecture will be
   removed from the final v2 system.
6. Uncertain assets such as FVDP/OVDP licensing and tokenizer choice require
   investigation before they become architectural commitments.
7. Legacy code may be deleted after valuable knowledge has been extracted or
   intentionally rejected and a recoverable Git history exists.