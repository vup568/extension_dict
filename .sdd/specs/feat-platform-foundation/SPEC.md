# Feature Specification: Platform Foundation

**Feature Branch**: `dev`

**Created**: 2026-08-14

**Status**: Approved

**Approved by**: VuPM

**Approval date**: 2026-08-21

**Input**: User description: "Define the minimum shared platform foundation for the Backend, Browser Extension, Web Application, linguistic knowledge, external providers, identity, privacy, asynchronous behavior, regression protection, and controlled legacy transition."

**Authoritative Sources**:

- [Project Constitution](../../constitution.md)
- [Product Requirements v2](../../../REQUIREMENT.md)
- [V2 Migration Decision](../../../MIGRATION_DECISION.md)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consume Shared Platform Semantics (Priority: P1)

As a Browser Extension or Web Application feature, I receive the same canonical linguistic identities, meanings, match provenance, and failure semantics for the same requested capability so that users do not encounter competing interpretations across clients.

**Why this priority**: Shared semantics are the central reason for introducing a platform foundation and are a prerequisite for every later client feature.

**Independent Test**: Submit a provider-neutral corpus of equivalent valid and invalid requests through both client boundaries and verify that both consumers receive semantically equivalent domain results, stable identities, and machine-readable failure categories.

**Acceptance Scenarios**:

1. **Given** the Extension and Web Application request the same analysis capability for the same Japanese input, **When** the Backend returns a result, **Then** both clients receive equivalent canonical identities, linguistic facts, provenance, and occurrence semantics even if their presentations differ.
2. **Given** an independently deployed client uses a supported contract version, **When** it communicates with the Backend, **Then** the exchange is validated at the boundary and does not require the client to understand Backend internals or an external provider's protocol.
3. **Given** a request is invalid, unsupported, rate-limited, or cannot be completed, **When** the Backend reports the failure, **Then** each client can identify the failure category and applicable retry guidance without parsing free-form prose.

---

### User Story 2 - Preserve Linguistic Identity and Correctness (Priority: P1)

As a future lookup, grammar, learning, review, or export feature, I can reference stable linguistic entities and correctness-preserving results so that saved knowledge remains valid as presentation and linguistic content evolve.

**Why this priority**: Stable identity and source-correct semantics prevent data corruption and make persistent learning features possible.

**Independent Test**: Use fixtures containing alternate forms and readings, restricted senses, repeated grammar matches, overlapping matches, and upstream identifiers; verify that every applicable identity, restriction, matched form, reading, sense, and occurrence span survives the platform boundary unambiguously.

**Acceptance Scenarios**:

1. **Given** an upstream dictionary entry has stable identity and form, reading, or sense restrictions, **When** it is ingested and returned as a match, **Then** its canonical identity remains traceable and no meaning is associated with an incompatible form or reading.
2. **Given** the same grammar rule occurs more than once or overlaps another meaningful match, **When** grammar results are represented, **Then** each occurrence retains the canonical rule identity and an unambiguous span under the shared offset convention.
3. **Given** a user-owned learning capability persists a dictionary entry or grammar rule, **When** the resource is later displayed in either client, **Then** it is resolved from its canonical identity rather than treating copied presentation text as its identity.

---

### User Story 3 - Integrate Safely with Browser Pages (Priority: P1)

As a reader using the Browser Extension, I can select Japanese text on supported browser pages and receive current results without page markup, hostile page behavior, or delayed responses corrupting the analysis or visible state.

**Why this priority**: Selection correctness and stale-result protection are required for a trustworthy reading experience and must be established before feature-specific popup behavior.

**Independent Test**: Exercise the provider-neutral browser corpus across the approved browsers using ruby, nested content, unrelated Unicode, hostile page styles/scripts, rapid selection changes, timeouts, retries, and out-of-order responses.

**Acceptance Scenarios**:

1. **Given** selected page content includes ruby annotations, **When** text is extracted for analysis, **Then** the Japanese base text is preserved and `rt` and `rp` annotation content does not pollute the linguistic input.
2. **Given** a selection contains Japanese supplementary ideographs or unrelated scripts, symbols, or emoji, **When** Japanese detection runs, **Then** valid Japanese is accepted and unrelated Unicode is not classified as Japanese solely because of a broad code-point range.
3. **Given** selection A is followed by selection B and A completes last, **When** both responses arrive, **Then** the result for A cannot replace any analysis, translation, error, or loading state belonging to B even if cancellation did not succeed.
4. **Given** the Backend is slow or unavailable, **When** a selection-dependent operation is attempted, **Then** the client can present loading, explicit failure, timeout, and retry states without breaking the host page or presenting stale results as current.

---

### User Story 4 - Publish Trustworthy Linguistic Knowledge (Priority: P1)

As a platform operator or linguistic editor, I can reproduce, validate, attribute, and safely publish a versioned knowledge release so that runtime engines consume maintainable knowledge rather than unexplained constants or untraceable data.

**Why this priority**: Shared Backend authority is only reliable if canonical knowledge is legally traceable, reproducible, and correctness-preserving.

**Independent Test**: Rebuild a knowledge release from a fixed source manifest and verify source identities, checksums where applicable, licensing and attribution records, transformation identity, record counts, validation results, rejected records, controlled publication, and separation of source facts from overrides and derived values.

**Acceptance Scenarios**:

1. **Given** an approved external dataset and fixed transformation inputs, **When** the knowledge release is rebuilt, **Then** the release can be traced to its source and transformation and produces repeatable validated output where determinism is required.
2. **Given** records fail validation, **When** a release is prepared, **Then** rejected records and reasons are reported rather than silently discarded.
3. **Given** an editorial correction is needed, **When** it is recorded, **Then** its canonical target, purpose, provenance when available, and change history appropriate to its risk remain distinguishable from source and normalized facts.
4. **Given** a candidate source has unresolved identity, quality, or licensing concerns, **When** production publication is considered, **Then** it remains non-canonical until explicitly reviewed and approved.

---

### User Story 5 - Use Anonymous Reading and Private Learning Boundaries (Priority: P1)

As a reader, I can use reading, lookup, and analysis without creating an account solely for tracking, while any learning records I choose to persist remain private to my authenticated identity.

**Why this priority**: Optional learning and private ownership are approved product boundaries, not details to be rediscovered by later features.

**Independent Test**: Complete anonymous reading and analysis flows, then exercise authenticated creation and cross-user access attempts against representative private learning records while inspecting ordinary operational logs for selected content.

**Acceptance Scenarios**:

1. **Given** a reader is not authenticated, **When** they use reading, lookup, or analysis, **Then** the platform does not require an account solely to track them.
2. **Given** a capability persists user-owned learning data, **When** a user invokes it, **Then** the platform may require authentication and must authorize access against ownership.
3. **Given** two authenticated users have private learning records, **When** either user attempts to read or modify the other's records, **Then** access is denied without disclosing private content.
4. **Given** selected page text is processed, **When** ordinary application and operational logs are inspected, **Then** raw selected text is absent by default and telemetry is primarily non-content metadata.

---

### User Story 6 - Replace Providers and Retire Legacy Safely (Priority: P2)

As a future feature team or maintainer, I can replace an external provider and retire an obsolete legacy subsystem without changing stable product semantics or losing valuable knowledge, identities, provenance, or regression evidence.

**Why this priority**: Provider independence and controlled greenfield migration reduce long-term lock-in and prevent destructive cleanup from erasing correctness evidence.

**Independent Test**: Substitute a conforming provider behind a platform capability and run the same client contract corpus; separately attempt a legacy-removal review and verify that removal is blocked until its checkpoint, extraction decisions, and replacement evidence are recorded.

**Acceptance Scenarios**:

1. **Given** a translation, morphology, authentication, or future learning provider is replaced, **When** the replacement satisfies the normalized platform contract, **Then** clients do not need provider-protocol changes and canonical linguistic identities remain unchanged.
2. **Given** a legacy subsystem contains potentially valuable behavior or data, **When** removal is proposed, **Then** the review identifies and preserves or intentionally rejects its regression cases, stable identities, data, editorial knowledge, provenance, and licensing information.
3. **Given** applicable replacement behavior has not been verified or no recoverable Git checkpoint exists, **When** destructive legacy cleanup is proposed, **Then** the cleanup is not approved.

### Edge Cases

- A dictionary spelling has several readings, and only some senses apply to the matched spelling-reading pair.
- An upstream identifier is absent, duplicated, changed, or invalid during ingestion.
- The same grammar rule occurs multiple times, while another valid rule overlaps one occurrence.
- Text offsets include supplementary Unicode characters; all participants must interpret the shared span convention identically.
- Page selection crosses nested elements, line breaks, ruby markup, editable content, or DOM that changes before extraction completes.
- A host page attempts to interfere with extension styling, events, focus, positioning, or injected elements.
- Selection, translation target, authentication state, or another interaction changes while an older operation is still in flight.
- Cancellation is unsupported, races with completion, or reports success after a response is already deliverable.
- One part of a composite analysis succeeds while another provider-backed part fails.
- A retry follows a timeout even though the earlier operation may have completed.
- A structured error is newer than an old successful response, or vice versa.
- An anonymous client exceeds an abuse limit without permitting invasive browser fingerprinting.
- A third-party capability requests more text context than the approved user action requires.
- An editorial override conflicts with a later upstream release.
- A data release contains invalid restrictions, missing attribution, rejected records, or a non-reproducible manual change.
- A legacy test encodes known incorrect behavior or obsolete implementation details.
- A client remains on an older supported contract while a newer client is deployed.

## Requirements *(mandatory)*

### Functional Requirements

#### System Surfaces and Responsibilities

- **FR-001**: The Backend MUST be the authoritative runtime boundary for shared dictionary and kanji knowledge, morphology, grammar analysis, translation orchestration, and persistent learning data.
- **FR-002**: The Browser Extension MUST own browser-page integration, selection interaction, and browser lifecycle concerns while consuming shared Backend capabilities for authoritative linguistic behavior.
- **FR-003**: The Web Application MUST consume the same shared Backend knowledge and persistent learning capabilities as the Browser Extension while owning its web-specific presentation and interaction concerns.
- **FR-004**: Extension and Web presentation differences MUST NOT create divergent canonical identities, linguistic meanings, occurrence semantics, ownership rules, or provider semantics.
- **FR-005**: Client-local storage MAY support bounded concerns such as preferences, ephemeral cache, or explicitly specified offline coordination, but MUST NOT become a competing canonical linguistic or learning store.
- **FR-006**: These responsibility boundaries MUST NOT prescribe deployment topology or any currently undecided technology.

#### Shared Contracts

- **FR-007**: Independently deployed clients and the Backend MUST communicate through shared contracts that define normalized domain semantics.
- **FR-008**: Shared contracts MUST be versioned whenever supported consumers may coexist independently, and compatibility behavior MUST be explicit and verifiable.
- **FR-009**: Inputs and outputs MUST be runtime-validatable at every untrusted system boundary.
- **FR-010**: Client-visible failures MUST use structured, machine-readable categories and details sufficient to distinguish validation, authentication, authorization, rate limiting, timeout, provider, availability, compatibility, and partial-result conditions when applicable.
- **FR-011**: Contract semantics MUST remain provider-neutral; provider credentials, protocols, labels, and raw provider errors MUST NOT become requirements for every client.
- **FR-012**: Selection- and interaction-dependent operations MUST carry correlation identity sufficient to associate results, failures, retries, and partial results with the initiating interaction.

#### Linguistic Domain Identity and Result Semantics

- **FR-013**: Dictionary entries, grammar rules, and any other persistently referenced linguistic resources MUST have stable canonical identities.
- **FR-014**: Where an upstream source provides a stable identifier, the canonical resource SHOULD retain a traceable relationship to that identifier.
- **FR-015**: Persistent learning references MUST use canonical linguistic identity rather than localized meaning, copied display text, or provider identity whenever a canonical identity exists.
- **FR-016**: Dictionary results MUST preserve the matched written form, matched reading, applicable sense or senses, and relevant source restrictions.
- **FR-017**: Source restrictions MUST NOT be discarded or flattened in a way that associates a meaning with an incompatible written form or reading.
- **FR-018**: Every grammar match MUST include the canonical grammar rule identity and an unambiguous occurrence span under one shared offset convention.
- **FR-019**: Repeated occurrences of the same grammar rule MUST remain distinct, and meaningful overlaps MUST remain representable.
- **FR-020**: Source facts, normalized facts, editorial overrides, and derived or approximate values MUST remain distinguishable wherever confusion could affect correctness, provenance, or user interpretation.
- **FR-021**: This foundation MUST support correctness-preserving representations without selecting the final lookup ranking, morphology, conjugation, or grammar matching algorithm.

#### Linguistic Knowledge and Data Boundary

- **FR-022**: Linguistic knowledge that is expected to grow, be corrected, localized, or editorially maintained MUST be versioned separately from the runtime engines that consume it.
- **FR-023**: Every production linguistic data release MUST identify its reviewed source, source version or equivalent identity, license and attribution obligations, transformation identity, validation results, and record counts; checksums and normalized artifact identity MUST be recorded where applicable.
- **FR-024**: Candidate knowledge with unresolved source, licensing, redistribution, quality, or identity concerns MUST NOT silently become canonical production data.
- **FR-025**: Canonical ingestion and transformation MUST be reproducible from controlled inputs, validate schemas and identity integrity, and preserve correctness-critical source metadata.
- **FR-026**: Ingestion MUST report rejected records and reasons and MUST support controlled publication that does not expose a partially accepted release as canonical.
- **FR-027**: Editorial overrides MUST identify their canonical target, purpose, provenance when available, and change history appropriate to their risk.
- **FR-028**: Unreproducible manual edits MUST NOT be the ordinary path for changing canonical production knowledge.

#### External Provider Boundary

- **FR-029**: Replaceable translation, tokenizer or morphology, authentication, and future external learning capabilities MUST integrate through explicit platform boundaries when adopted.
- **FR-030**: Provider-specific credentials, protocols, errors, labels, and presentation formats MUST remain isolated from unrelated domain behavior and client contracts.
- **FR-031**: Replacing a provider MUST NOT redefine canonical linguistic identities or require every client to adopt provider-specific behavior.
- **FR-032**: Provider adoption MUST receive proportionate review of correctness, privacy, licensing, maintenance, cost, operational behavior, and lock-in risk before production use.
- **FR-033**: No external provider MAY become an unstated source of canonical product identity.

#### Browser Platform Boundary

- **FR-034**: Browser-vendor APIs and lifecycle behavior MUST be isolated from core linguistic and application logic.
- **FR-035**: The browser boundary MUST support semantically consistent behavior across Chrome, Edge, Brave, and Firefox while leaving version and release details to browser feature specifications.
- **FR-036**: Page extraction MUST preserve intended Japanese base text and exclude `rt` and `rp` annotation text from linguistic input.
- **FR-037**: Japanese detection MUST be Unicode-correct, include valid supplementary Japanese ideographs, and reject unrelated scripts, symbols, and emoji that only match overly broad code-point ranges.
- **FR-038**: Host-page integration MUST tolerate hostile styles, scripts, mutable document structure, and supported complex selection structures without allowing page behavior to alter core linguistic semantics.
- **FR-039**: Every selection and dependent interaction MUST have identity sufficient to determine which asynchronous state is current.
- **FR-040**: An older analysis, translation, error, or partial result MUST NOT replace state belonging to a newer selection, target, or interaction.
- **FR-041**: Cancellation MAY reduce unnecessary work, but stale-result correctness MUST remain enforceable when cancellation is unavailable, late, or unsuccessful.

#### Anonymous, Authenticated, Privacy, and Security Boundaries

- **FR-042**: Reading, lookup, and analysis MUST remain available without requiring an account solely for tracking.
- **FR-043**: Authentication MAY be required for capabilities that persist or synchronize user-owned learning data and MUST support a coherent user identity across Extension and Web clients.
- **FR-044**: Authorization MUST enforce ownership of private learning records, which MUST be private by default; authentication alone MUST NOT be treated as proof of authorization.
- **FR-045**: Selected page text MUST be treated as potentially sensitive, and collection and transfer MUST be limited to the minimum context required for the user-requested capability.
- **FR-046**: Raw selected text MUST NOT be persisted in ordinary application or operational logs by default.
- **FR-047**: Operational telemetry SHOULD primarily use non-content metadata such as timing, outcome, request size, and correlation information that does not reveal the selected content.
- **FR-048**: Any retention, secondary use, or third-party disclosure of selected text beyond immediate requested processing MUST be explicitly specified, justified, protected, and disclosed appropriately to the user.
- **FR-049**: External providers MUST receive no more selected-text context than the approved capability requires.
- **FR-050**: Production communication MUST protect data in transit; private credentials MUST remain outside client-delivered artifacts; untrusted Backend inputs MUST be validated; and abuse protection MUST apply without invasive browser fingerprinting.

#### Failure and Asynchronous Semantics

- **FR-051**: Clients MUST be able to represent loading, timeout, Backend-unavailable, retry, and applicable partial-failure states without fabricating results or breaking the surrounding client or host page.
- **FR-052**: Any valid partial result MUST identify what completed and what failed so clients do not present incomplete information as a complete analysis.
- **FR-053**: Retry guidance MUST be machine-readable where a failure is retryable, and later feature specifications MUST define operation-specific duplicate or idempotency behavior for state-changing retries.
- **FR-054**: No response, including a success, partial result, or error, MAY update selection-dependent state unless its interaction identity is still current.

#### Regression, Verification, and Legacy Transition

- **FR-055**: The platform MUST maintain a provider-neutral regression corpus covering Unicode/Japanese detection, ruby and DOM extraction, dictionary identities and restrictions, morphology and base forms, grammar occurrence identity and spans, meaningful overlaps, stale asynchronous behavior, shared-contract parity, privacy and logging, user-data isolation, and ingestion provenance.
- **FR-056**: Regression expectations MUST represent approved semantics; known legacy defects and obsolete implementation details MUST NOT be preserved as required behavior.
- **FR-057**: Before removing an affected legacy subsystem, maintainers MUST extract and preserve or explicitly reject its valuable regression cases, stable identities, data, editorial knowledge, provenance, and licensing information.
- **FR-058**: Large-scale destructive legacy cleanup MUST require a recoverable Git checkpoint or equivalent immutable reference and verification of applicable replacement behavior.
- **FR-059**: Legacy removal MUST NOT require line-by-line migration or internal implementation parity once approved behavior, semantic correctness, and the required evidence have been preserved.
- **FR-060**: Every later feature specification and plan that relies on this foundation MUST identify applicable constitutional, privacy, identity, provider, data, regression, and migration impacts and surface any proposed exception for human approval.
- **FR-061**: Human approval gates between specification, clarification, planning, tasking, and implementation MUST be respected; this foundation MUST NOT be treated as approval to implement later user-facing features.

### Key Entities *(include if feature involves data)*

- **Canonical Linguistic Resource**: A persistently referenceable dictionary entry, grammar rule, kanji record, or other linguistic entity with a stable platform identity and, where available, traceable upstream identity.
- **Dictionary Match**: A relationship between analyzed input and a canonical dictionary entry, including matched written form, matched reading, applicable senses, and governing source restrictions.
- **Grammar Occurrence**: One occurrence of a canonical grammar rule in analyzed text, identified by an unambiguous span and able to coexist with repeated or overlapping occurrences.
- **Knowledge Release**: A versioned, validated collection of canonical linguistic knowledge prepared for runtime use.
- **Source Manifest**: The provenance and licensing record for a knowledge release, including source identity, version, applicable checksums, obligations, transformation identity, counts, validation, and rejected-record information.
- **Editorial Override**: A reviewed correction or supplement tied to a canonical resource, with purpose, provenance when available, and appropriate change history.
- **Analysis Interaction**: A selection- or input-driven operation with identity and currentness sufficient to correlate loading, results, failures, retries, and dependent actions.
- **Structured Error**: A machine-readable failure carrying a stable category and applicable details such as retry guidance, affected capability, and interaction identity without leaking sensitive content or raw provider semantics.
- **User Principal**: An anonymous or authenticated platform identity used only for approved concerns such as abuse protection, session continuity, or ownership enforcement; it is not a substitute for authorization.
- **Learning Reference**: A private, user-owned persistent reference to a canonical linguistic resource, with feature-specific learning attributes defined by later specifications.
- **Provider-Neutral Capability Result**: A normalized product result whose identity and meaning do not depend on the protocol or labels of the provider that helped produce it.

### Verification Traceability

| Requirement Area | Primary Acceptance Evidence |
|------------------|-----------------------------|
| FR-001–FR-012: surfaces and shared contracts | User Story 1; SC-001 and SC-010 |
| FR-013–FR-021: linguistic identity and semantics | User Story 2; SC-002 and SC-003 |
| FR-022–FR-028: knowledge and data | User Story 4; SC-008 |
| FR-029–FR-033: provider boundary | User Story 6; SC-009 |
| FR-034–FR-041: browser boundary | User Story 3; SC-004 and SC-005 |
| FR-042–FR-050: identity, privacy, and security | User Story 5; SC-006 and SC-007 |
| FR-051–FR-054: failures and asynchronous state | User Stories 1 and 3; SC-005 and SC-010 |
| FR-055–FR-061: regression and legacy transition | User Story 6; SC-011 and SC-012 |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The Extension and Web Application pass 100% of shared semantic conformance fixtures for canonical identity, linguistic facts, occurrence semantics, and structured failures.
- **SC-002**: In acceptance fixtures for persistently referenced linguistic resources, 100% of records use canonical identities when available and none use localized or copied presentation text as the primary identity.
- **SC-003**: The correctness corpus passes 100% of approved dictionary restriction, matched-form, matched-reading, applicable-sense, repeated-grammar, overlap, and span-convention cases without silent information loss.
- **SC-004**: The cross-browser extraction and Japanese-detection corpus produces equivalent intended input semantics on Chrome, Edge, Brave, and Firefox for all approved cases.
- **SC-005**: In every out-of-order analysis and translation acceptance scenario, an older operation is rejected from updating newer interaction state, including scenarios where cancellation does not occur.
- **SC-006**: Inspection of ordinary application and operational logs across the privacy corpus finds zero persisted raw selected-text values by default, while approved non-content diagnostics remain available.
- **SC-007**: Anonymous users complete every approved reading, lookup, and analysis foundation flow without account creation, and all cross-user private-learning access attempts in the authorization corpus are denied.
- **SC-008**: Every production-candidate knowledge release reviewed against this foundation has complete required provenance, licensing, validation, record-count, and rejected-record evidence, and can be reproduced from its controlled inputs where determinism is required.
- **SC-009**: A conforming substitute provider passes the same platform contract corpus without client protocol changes, provider identities entering canonical references, or changes to stable linguistic identities.
- **SC-010**: 100% of required failure categories can be distinguished by clients from structured data without parsing a human-readable message.
- **SC-011**: Every approved destructive legacy-removal review records a recoverable checkpoint, explicit preservation or rejection decisions for valuable assets, and applicable replacement-verification evidence before deletion.
- **SC-012**: Foundation acceptance review finds no undocumented conflict with the Constitution, Product Requirements, or Migration Decision and no unapproved technology selection or user-facing feature implementation in scope.

## Assumptions

- The Constitution, Product Requirements v2, and V2 Migration Decision are approved and internally consistent; this review found no conflict requiring human resolution.
- This foundation establishes durable platform boundaries and observable guarantees, not one independently deployable end-user feature or the complete MVP.
- Core MVP analysis is network-dependent; offline dictionaries, offline translation, and offline save coordination require later feature specifications.
- The exact span/offset convention, compatibility window, transport, endpoint shape, retry policy per state-changing operation, and partial-result envelope will be selected during clarification or planning while preserving the requirements above.
- Shared semantics permit client-specific presentation and interaction design as long as identities, facts, ownership, and failure meanings remain equivalent.
- Partial results are applicable only where a later capability can return independently valid information without implying that failed portions succeeded.
- Anonymous abuse protection may use a random installation identifier or equivalent non-invasive mechanism; the exact mechanism and limits remain planning decisions.
- Vietnamese remains the primary MVP presentation language and English remains an extensibility requirement, while localized content is not itself canonical identity.
- Provider boundaries are introduced only for realistic replaceable capabilities and do not require abstraction around every internal module.
- The current working branch remains `codex/expression-detection` because no Spec Kit branch-creation hook is configured; the feature directory is independently identified by `.specify/feature.json` as `.sdd/specs/feat-platform-foundation`.

## Dependencies and Downstream Feature Specifications

- Exact vocabulary and kanji lookup behavior, ranking, result presentation, and performance targets.
- Complete morphology, conjugation, expression, and grammar engine semantics and coverage.
- Translation behavior and provider selection, including user disclosure details.
- Authentication provider, session behavior, and concrete account recovery or linking flows.
- Saved Vocabulary, Saved Grammar, Grammar Library, Quick Review, and export experiences and operation-specific persistence rules.
- Final Extension popup behavior, browser manifests, permissions, version support, accessibility, and page-integration release criteria.
- Final Web Application navigation, UI, and localization behavior.
- Data-source adoption decisions, including unresolved FVDP/OVDP licensing and tokenizer or morphology candidates.
- Architecture and planning decisions for technology stack, repository structure, deployment, storage, caching, rate limits, final operational targets, and observability implementation.

## Out of Scope

- Exact vocabulary or kanji lookup implementation and ranking.
- Complete grammar, morphology, expression, or conjugation algorithms.
- Selection of translation, tokenizer or morphology, authentication, learning, storage, search, cache, cloud, hosting, or observability providers.
- Saved Vocabulary UI, Saved Grammar UI, Grammar Library UI, Quick Review, Anki or Quizlet export, and automatic synchronization.
- Final Web Application UI, final Extension popup design, detailed browser manifests, and production deployment.
- Final operational service levels, exact rate limits, deployment topology, repository layout, and package organization.
- Full offline dictionary or translation behavior, advanced learning, mobile clients, Safari, and other post-MVP product scope.
- Open versus closed Shadow DOM or selection of any browser abstraction library.
- Migration of legacy code line by line or preservation of obsolete internal implementation parity.

## Constitution Alignment

- **Backend authority**: FR-001 through FR-005 establish Backend authority and prevent competing client knowledge or learning stores.
- **Shared client semantics**: FR-004 and FR-007 through FR-012 require equivalent, versioned, validated, provider-neutral contracts and structured errors.
- **Browser-vendor isolation**: FR-034 through FR-041 isolate browser concerns while preserving cross-browser semantics.
- **Stable linguistic identity and correctness**: FR-013 through FR-021 preserve canonical identity, restrictions, provenance, repeated occurrences, spans, overlaps, and fact categories.
- **Provenance and licensing**: FR-022 through FR-028 require traceable, reproducible, validated knowledge and reviewed editorial overrides.
- **Privacy and data minimization**: FR-042 through FR-050 preserve anonymous reading, private ownership, minimal transfer, redacted ordinary logs, and third-party disclosure.
- **Provider isolation**: FR-029 through FR-033 keep replaceable providers from defining core identity or client protocols.
- **Stale asynchronous protection**: FR-039 through FR-041 and FR-051 through FR-054 make current interaction identity—not cancellation—the correctness mechanism.
- **Regression and migration safeguards**: FR-055 through FR-059 require provider-neutral semantic evidence and a recoverable checkpoint before destructive cleanup.
- **Human approval gates**: FR-060 and FR-061 require explicit artifact impact and approval before later phases or exceptions.
- **Conflicts and exceptions**: The stale database-baseline artifacts recorded in `REQUIREMENT.md` §15.1 remain a documentation-alignment follow-up; they are not a constitutional exception and must be resolved before dependent implementation.

## Unresolved Clarifications

No unresolved clarification blocks this foundation approval. Decisions deliberately deferred by Product Owner VuPM are recorded in `REQUIREMENT.md` §15 with owners and blocking gates; they must be resolved before their dependent feature is implemented.
