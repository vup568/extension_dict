<!--
Sync Impact Report

- Version change: Unratified template -> 1.0.0
- Ratification type: Initial constitution ratification.
- Modified principles: Generic constitution placeholders -> project-specific governing
  invariants for product boundaries, architecture, linguistic correctness, knowledge and
  provenance, clients and asynchronous behavior, privacy and security, provider isolation,
  Spec Kit gates, testing, and review.
- Added sections:
  - 1. Scope
  - 2. Product And System Boundaries
  - 3. Architecture Principles
  - 4. Linguistic Correctness And Stable Identity
  - 5. Knowledge, Data, And Provenance Invariants
  - 6. Client, Browser, And Async Invariants
  - 7. Privacy, Security, And Authentication
  - 8. External Providers And Replaceable Infrastructure
  - 9. Spec Kit Artifact Gates
  - 10. Testing And Definition Of Done
  - 11. Git And Review
  - 12. Governance
- Removed sections: None; the unratified generic scaffold contained placeholders only.
- Dependent template changes: None. Constitution consumers read this file at runtime.
- Follow-up TODOs: None.
-->

# JP Reading Platform Constitution

## 1. Scope

This Constitution defines the project-wide, non-negotiable engineering and governance
rules for JP Reading Platform. It applies to the Backend, Browser Extension, Web
Application, shared contracts, linguistic knowledge, data pipelines, external-provider
integrations, learning data, tests, and Spec Kit artifacts.

`docs/REQUIREMENT.md` is authoritative for target product behavior and scope.
`docs/MIGRATION_DECISION.md` is authoritative for how legacy assets are preserved,
migrated, reimplemented, investigated, or removed. The legacy README, source code,
generated assets, tests, and current runtime behavior MUST NOT override those documents.
They MAY be used only as evidence for domain knowledge, valid regression behavior, edge
cases, datasets, provenance, licensing, and migration risk.

This Constitution governs durable project invariants. Feature coverage, user-interface
details, exact thresholds, endpoint shapes, and other feature-level decisions MUST remain
in approved requirements, specifications, and plans unless they become project-wide
invariants through a constitutional amendment.

## 2. Product And System Boundaries

The Backend MUST be the authoritative runtime boundary for shared linguistic knowledge
and persistent learning data, including canonical dictionary and kanji data, morphology,
grammar analysis, translation orchestration, and authenticated learning records.

The Browser Extension and Web Application MUST be clients of shared Backend capabilities.
They MUST consume shared, versioned contracts and MUST NOT establish competing canonical
dictionary, grammar, tokenizer, or learning stores. Local client storage MAY support
bounded client concerns such as preferences, ephemeral caches, or explicitly specified
offline coordination, but it MUST NOT silently become an authoritative knowledge store.

Client and Backend boundaries MUST preserve equivalent domain semantics. Presentation
differences MUST NOT create divergent linguistic meanings, identities, or learning
references between clients.

These responsibility boundaries do not mandate a deployment topology, programming
language, framework, database, cache, API protocol, cloud, or hosting provider.

## 3. Architecture Principles

Linguistic correctness and approved product behavior MUST take precedence over legacy
implementation compatibility. Required capabilities MAY be redesigned or reimplemented;
legacy internal APIs, storage formats, dependencies, directory structure, and browser-only
architecture are not compatibility constraints.

Core linguistic and application logic MUST be independent of browser-vendor APIs.
Browser lifecycle, permissions, page integration, and other platform-specific behavior
MUST be isolated behind explicit client boundaries.

Shared contracts and system boundaries MUST be explicit, versioned where independently
deployed consumers can coexist, runtime-validatable, and capable of returning structured,
machine-readable errors.

Linguistic knowledge expected to grow, be corrected, localized, or editorially maintained
MUST be represented as versioned data or editorial records separate from runtime
application logic. Operational policy SHOULD be centralized as configuration where
appropriate. Presentation values MAY remain client constants or design tokens. The project
MUST NOT require all constants to be stored in a database.

Technology choices that remain open in approved requirements or migration decisions MUST
be resolved in an appropriate specification or plan. This Constitution MUST NOT be used to
preselect a backend language or framework, frontend framework, database, cache, provider,
cloud, hosting platform, repository layout, or equivalent undecided technology.

## 4. Linguistic Correctness And Stable Identity

All linguistic processing MUST favor semantically correct, Unicode-aware behavior over
legacy output parity. Known incorrect legacy behavior MUST become negative or bug-regression
evidence, not a compatibility requirement.

Dictionary entries, grammar rules, and other persistently referenced linguistic entities
MUST have stable canonical identities. When an upstream source provides a stable identity,
the normalized model SHOULD retain a traceable relationship to it. Saved learning records
MUST reference canonical identities rather than localized strings or copied presentation
text whenever such identities exist.

Dictionary ingestion and lookup MUST preserve source information required to associate a
meaning with the correct written form, reading, and sense. Form, reading, sense, and similar
restrictions supplied by a source MUST NOT be silently discarded or flattened in a way that
creates linguistically invalid matches.

Lookup results MUST retain sufficient match provenance to explain the matched written form,
reading, applicable sense or senses, and relevant restrictions. Derived or approximate
values MUST be distinguishable from authoritative source values.

Every detected grammar occurrence MUST retain its canonical grammar identity and an
unambiguous span under a shared offset convention. Repeated occurrences MUST NOT be
silently collapsed, and meaningful overlaps MUST remain representable.

## 5. Knowledge, Data, And Provenance Invariants

Production linguistic knowledge MUST be traceable to a reviewed source or an explicit
editorial decision. External production datasets MUST record, as applicable, the canonical
publisher or source, exact release or commit, source checksum, license or SPDX expression,
notice and attribution obligations, transformation version, normalized artifact checksum,
record counts, validation results, and rejected-record information.

Knowledge whose source, license, or redistribution conditions cannot be reasonably
established MUST NOT silently become canonical production data. Candidate data with
unresolved quality, identity, or licensing questions MUST remain marked for investigation
until explicitly approved.

Canonical data preparation MUST be reproducible. Ingestion and transformation pipelines
MUST validate schemas and identity integrity, preserve correctness-critical source metadata,
report rejected records, and use controlled publication. Manual production edits that
cannot be reproduced MUST NOT be the ordinary path for changing canonical linguistic data.

Raw source facts, normalized facts, editorial overrides, and derived values MUST remain
distinguishable where confusing them could affect correctness or provenance. Editorial
overrides MUST identify their canonical target, purpose, provenance when available, and
change history appropriate to their risk.

Legacy linguistic knowledge, regression cases, stable identifiers, and provenance MUST be
extracted, reviewed, and either preserved or intentionally rejected before the obsolete
representation that contains them is removed.

## 6. Client, Browser, And Async Invariants

Browser integrations MUST isolate vendor-specific APIs from core linguistic and product
logic. Behavior that is part of an approved cross-browser contract MUST remain semantically
consistent across the approved browser targets; target lists and release coverage remain in
requirements and feature specifications.

Text extracted from a page MUST preserve the intended Japanese base text. Ruby annotation
content from `rt` and `rp` elements MUST NOT pollute linguistic input, and DOM extraction
MUST account explicitly for context, hostile page structure, and privacy boundaries.

Every selection-dependent asynchronous operation MUST carry enough request identity to
determine whether its result is still current. A response for an older selection, request,
translation target, or interaction MUST NOT overwrite state belonging to a newer one.
Cancellation MAY reduce wasted work, but correctness MUST NOT depend on cancellation alone.

Clients MUST represent loading, partial failure, retry, timeout, and unavailable-backend
states without presenting stale or fabricated linguistic results as current. Boundary
validation and structured errors MUST be used to keep client behavior consistent.

## 7. Privacy, Security, And Authentication

User-selected page text MUST be treated as potentially sensitive. Collection and transfer
MUST be limited to the context required for the requested capability. Raw selected text MUST
NOT be persisted in ordinary application or operational logs by default. Operational
telemetry SHOULD prefer non-content metadata such as timing, status, and request size.

Any retention, secondary use, or disclosure of selected text beyond the immediate requested
processing MUST be explicitly specified, justified, and protected. When text is sent to a
third-party provider, the product MUST provide appropriate user disclosure and the system
MUST send no more context than the approved capability requires.

Reading, lookup, and analysis MUST NOT require an account solely to track a user.
Authentication MAY be required for capabilities that persist or synchronize user-owned
data. Learning records MUST be private by default, authorization MUST enforce ownership,
and one user MUST NOT be able to read or modify another user's records.

Production APIs MUST use encrypted transport. Secrets and private Backend credentials MUST
NOT be embedded in client bundles. Untrusted input MUST be validated at the Backend boundary,
and authentication MUST NOT substitute for authorization, input validation, rate limiting,
or abuse protection.

## 8. External Providers And Replaceable Infrastructure

Replaceable external services and linguistic infrastructure MUST be isolated behind
appropriate boundaries. This includes translation, tokenizer or morphology, authentication,
and future external learning integrations when used.

Provider-specific credentials, protocols, labels, errors, and presentation formats MUST NOT
leak through unrelated domain modules or force clients to depend directly on a provider.
Core contracts MUST express normalized product semantics so that a provider can be replaced
without redefining canonical linguistic identities or rewriting every client.

Provider adoption MUST be preceded by proportionate evaluation of correctness, privacy,
licensing, maintenance, cost, operational behavior, and lock-in risk. An external provider
MUST NOT become an unstated source of canonical identity.

Boundaries MUST be proportional to realistic replacement or isolation needs. This principle
does not require an abstraction around every internal module and does not select any specific
translation, tokenizer, authentication, storage, cache, cloud, or hosting provider.

## 9. Spec Kit Artifact Gates

Spec Kit artifacts MUST explicitly surface constitutional impact. Every feature specification
MUST identify applicable principles, privacy and data implications, stable-identity or
linguistic-correctness concerns, external-provider dependencies, and any proposed exception.

Every implementation plan MUST include a Constitution Check that demonstrates how its
architecture and data flow satisfy the applicable invariants without turning open technology
decisions into undocumented commitments. Plans that introduce data sources MUST include
provenance and licensing treatment; plans that process page text MUST include minimization,
logging, retention, and third-party disclosure treatment.

Tasks MUST translate constitutional obligations into actionable work and verification,
including relevant regression tests, contract validation, migration safeguards, and
documentation. Analysis MUST flag omissions, contradictions, and unjustified complexity
before implementation proceeds.

Human approval gates defined by the project workflow MUST be respected. Implementation MUST
NOT silently expand beyond an approved specification. Any necessary deviation MUST return to
the appropriate artifact, document its constitutional impact, and receive approval before
the divergent work is treated as accepted.

## 10. Testing And Definition Of Done

Every change that can affect approved behavior, linguistic semantics, stable identity, data
integrity, privacy, authorization, shared contracts, or asynchronous interaction MUST include
meaningful automated regression protection at the lowest sufficient level and integration or
acceptance coverage where boundaries make unit tests insufficient.

The provider-neutral regression corpus MUST be preserved and expanded before affected legacy
implementations are removed. Relevant coverage MUST include correctness-critical Unicode and
linguistic edge cases, source restrictions and identities, data-ingestion integrity, exact
grammar occurrences, client contract parity, stale-response rejection, redacted logging,
request-context minimization, and user-data isolation.

Tests MUST assert intended semantics rather than obsolete implementation details. A legacy
test MAY be retained as evidence only after its expected behavior has been checked against
the authoritative requirements and linguistic correctness rules.

Work is done only when the approved acceptance criteria are met; applicable tests pass;
runtime boundaries and errors are validated; privacy, security, provenance, licensing, and
migration obligations are satisfied; and no unresolved constitutional violation is hidden in
code, data, documentation, or deferred work. Any accepted exception MUST be explicit,
approved, time-bounded where practical, and linked to follow-up work.

## 11. Git And Review

Material changes MUST be reviewable as coherent, scoped changes with their specification,
constitutional impact, test evidence, migration impact, and data or licensing implications
visible to reviewers. Review MUST verify compliance with this Constitution and the two
authoritative decision documents, not merely consistency with the legacy codebase.

Before large-scale deletion or restructuring of legacy source, the repository MUST have a
recoverable Git checkpoint or equivalent immutable reference. Reviewers MUST confirm that
valuable regression cases, data, stable identities, editorial knowledge, and provenance have
been extracted or intentionally rejected before approving removal.

Commits MUST NOT introduce secrets or private credentials. Generated or obsolete legacy
artifacts MUST NOT be retained as active v2 source merely to preserve history; Git history and
approved checkpoints provide historical recovery.

Exceptions, known risks, and deferred corrective work MUST be stated plainly in the relevant
Spec Kit artifact and review record. Approval MUST NOT be inferred from silence or from the
fact that legacy behavior already exists.

## 12. Governance

This Constitution is the highest authority for project-wide engineering and governance
invariants. `docs/REQUIREMENT.md` remains authoritative for approved product behavior and
scope, and `docs/MIGRATION_DECISION.md` remains authoritative for legacy disposition. When an
artifact conflicts with this Constitution, the artifact MUST be amended or the Constitution
MUST be amended before the conflicting work is accepted.

Amendments require a written proposal that states the rationale, affected principles and
artifacts, compatibility or migration impact, and required follow-up work. An amendment MUST
receive explicit approval from the project owner or delegated maintainers and MUST update the
Sync Impact Report, version, and Last Amended date in the same change.

Constitution versions follow semantic versioning:

- MAJOR for removal or incompatible redefinition of an existing principle or governance rule.
- MINOR for a new principle or materially expanded mandatory guidance.
- PATCH for clarification, wording correction, or other non-semantic refinement.

Specifications, plans, tasks, reviews, and release readiness checks MUST verify constitutional
compliance. Complexity and exceptions MUST be justified in the relevant artifact. Compliance
reviews MUST use evidence appropriate to the risk, including tests, data manifests, privacy
analysis, and migration records where applicable.

**Version**: 1.0.0 | **Ratified**: 2026-08-13 | **Last Amended**: 2026-08-13 |
**Status**: Active
