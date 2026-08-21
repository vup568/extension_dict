# JP Reading Platform V2 — Project Design Principles

**Status:** Project-level direction for future feature design. The final visual language is intentionally unresolved.

## 1. Purpose and source authority

This document answers **how the product should be designed across features**. It does not define exactly what the product must look like, pre-design an individual feature, or approve new product behavior.

Authority is applied in this order:

1. `REQUIREMENT.md` owns approved product behavior, scope, actors, and acceptance criteria.
2. `.sdd/constitution.md`, `SDD.md`, and `MIGRATION_DECISION.md` constrain architecture, platform responsibilities, privacy, migration, and implementation boundaries.
3. An active feature `SPEC.md` may refine its own approved scope but may not expand the product requirements.
4. This document governs project-level design principles without overriding any source above.

External design libraries, websites, and visual examples are references only. They do not create requirements, and no named visual style is selected by this constitution.

The requirement references in this document are trace links, not duplicated specifications. When a source changes, the source remains authoritative.

## 2. Product design characteristics

| Characteristic | Project-level meaning | Classification and trace |
|---|---|---|
| Reading-flow preserving | Help must appear with minimal interruption, especially when the Extension is used over another page. The interface should return attention to the source material quickly. | `SOURCE-DERIVED` — `EXT-001`, `EXT-003`, `NET-001`, `NET-004`, `PERF-001`; `SDD.md` “Reading First” |
| Content-first | Linguistic content, task state, and available user actions take precedence over decorative presentation. | `SOURCE-DERIVED` — `VOC-003`–`VOC-008`, `KAN-001`–`KAN-002`, `WEB-003`–`WEB-005`; Platform Foundation `FR-016`–`FR-020` |
| Linguistically trustworthy | Presentation must preserve applicable forms, readings, senses, restrictions, occurrence identity, provenance, and distinctions between authoritative and derived facts. Visual compression must not change meaning. | `SOURCE-DERIVED` — `VOC-003`–`VOC-008`, `KAN-002`, `GRM-007`–`GRM-008`; Platform Foundation `FR-016`–`FR-020` |
| Truthful about state | The interface must distinguish current loading, success, partial, unavailable, timeout, and retryable outcomes when applicable. Older interactions must never appear current. | `SOURCE-DERIVED` — `ASYNC-001`–`ASYNC-002`, `API-004`, `NET-001`–`NET-006`, `PERF-004`; Platform Foundation `FR-039`–`FR-041`, `FR-051`–`FR-054` |
| Vietnamese-first and multilingual-ready | Vietnamese is the primary MVP interface language. Japanese learning material, language tags, and reviewed English content must remain understandable without turning localized strings into canonical identity. | `SOURCE-DERIVED` — `I18N-001`–`I18N-004`, `VOC-004`–`VOC-005`, `CONJ-002`, `WEB-005` |
| Privacy-visible | The design must make ownership, external disclosure, and persistence boundaries understandable at the moment they matter. Selected text is ephemeral by default. | `SOURCE-DERIVED` — `PRIV-001`–`PRIV-006`, `AUTH-004`; Platform Foundation `FR-044`–`FR-050` |
| Anonymous-reading friendly | Reading, lookup, kanji, grammar analysis, and translation remain available without login; authentication is introduced only for approved persistence or synchronization behavior. | `SOURCE-DERIVED` — `AUTH-001`–`AUTH-002`, `LEARN-001`, `LEARN-003`; Platform Foundation `FR-042`–`FR-043` |
| Progressive | Complexity, optional capability, and supporting detail should follow user intent instead of competing for attention by default. | `SOURCE-DERIVED` — on-demand translation in `TRN-004`; authentication boundary in `AUTH-001`–`AUTH-002`; partial capability semantics in `NET-006` |
| Responsive and resilient | Interaction design must respect low-latency goals, remain useful through network failure, and avoid destabilizing the host page or client. | `SOURCE-DERIVED` — `NET-001`–`NET-005`, `PERF-001`–`PERF-004`, `BROWSER-003`; `MIGRATION_DECISION.md` §§6.10, 8.4 |

## 3. Global UX principles

### 3.1 Preserve the reading task

Design around the user’s reading or learning goal, not around the number of available backend capabilities. Contextual assistance should be easy to enter, understand, and leave. A feature may expose depth progressively, but it must not hide information required for linguistic correctness.

Trace: `EXT-001`–`EXT-003`, `WEB-001`–`WEB-003`, `TRN-004`, `VOC-007`–`VOC-008`.

### 3.2 Present meaning before visual compression

Information hierarchy may vary by feature, input, and surface. In every case, hierarchy must preserve canonical meaning, applicability, provenance, language, and whether information is unavailable, partial, or derived. A shorter presentation is not acceptable when it creates a false or ambiguous linguistic claim.

Trace: `VOC-003`–`VOC-008`, `KAN-002`, `GRM-007`–`GRM-008`, Platform Foundation `FR-016`–`FR-020`.

### 3.3 Keep interaction state current and recoverable

Interfaces must expose the state that belongs to the current interaction. When applicable, feature design must account for loading, partial completion, timeout, unavailable service, retry guidance, and recovery without fabricating a result. Cancellation is an efficiency mechanism; interaction identity is the correctness mechanism.

Trace: `ASYNC-001`–`ASYNC-002`, `API-004`, `NET-001`–`NET-006`, `PERF-004`; Platform Foundation `FR-039`–`FR-041`, `FR-051`–`FR-054`.

### 3.4 Reveal optional capability by intent

Optional or sensitive operations should appear when relevant to the user’s goal and should disclose material consequences before they occur. In particular, translation remains user-initiated in MVP, and anonymous reading must not become a login funnel merely because private learning features exist.

Trace: `TRN-004`, `PRIV-004`, `AUTH-001`–`AUTH-002`.

### 3.5 Prefer understandable behavior over decorative novelty

Visual treatment may establish identity and atmosphere, but it must earn its cost in the feature where it is proposed. Decoration must not reduce readability, state clarity, accessibility, low-latency interaction, Extension performance, or host-page resilience.

Governance status: **project-level principle explicitly approved for this document**. It is not a `FEATURE-DD` and has not passed through the `PROMOTED-DD` lifecycle. It does not select or prohibit a named visual style and must be revisited if feature evidence shows that it is too restrictive.

## 4. Cross-surface principles

The Browser Extension and Web Application serve different contexts and **do not need identical layouts**.

- They may differ in density, navigation, information presentation, responsive behavior, and interaction patterns.
- They must preserve the same canonical linguistic identities and meanings.
- They must preserve equivalent status, currentness, partial-result, and error semantics when the underlying capability is shared.
- They must preserve privacy, authentication, authorization, persistence, and ownership semantics.
- They must consume authoritative Backend capabilities and must not create conflicting client-side sources of truth.
- Neither surface may contradict approved product behavior to achieve visual consistency with the other.

Classification and trace: `SOURCE-DERIVED` — `WEB-002`, `API-001`, `ARCH-002`, `BROWSER-001`–`BROWSER-003`; Platform Foundation `FR-001`–`FR-005`, `FR-034`–`FR-041`; `MIGRATION_DECISION.md` §§6.1, 6.14, 15.

## 5. Accessibility, multilingual, and privacy principles

### 5.1 Accessibility

Accessibility is a project-level design principle, while each feature determines which specific interaction and verification obligations apply.

- Interaction must remain understandable without relying on color alone.
- Keyboard order, focus visibility, focus movement, and focus restoration must be designed wherever keyboard or programmatic focus is relevant.
- Motion must respect reduced-motion needs when motion is introduced.
- Text, controls, and state cues must remain perceivable and operable under the feature’s supported viewport and host constraints.
- Accessibility must be validated in implementation; this document does not claim a conformance level before evidence exists.

Source-backed minimum: `BROWSER-003` and `MIGRATION_DECISION.md` §8.4 require Extension focus and keyboard accessibility under host-page isolation. Applying accessibility across other project features is a **project-level principle explicitly approved for this document**. It is outside the feature-decision promotion lifecycle and does not claim that the sources already specify every accessibility behavior.

### 5.2 Multilingual presentation

- Vietnamese is the primary MVP UI language.
- Japanese content must remain legible and must preserve the relationship between observed form, reading, meaning, and applicable restrictions.
- `vi` and `en` content must retain explicit language meaning; missing reviewed English must not be presented as missing Vietnamese content.
- Layout and content structure must tolerate future English UI/content without changing canonical identity.
- Exact type families, sizes, weights, and fallback stacks remain feature/system design work until validated.

Classification and trace: `SOURCE-DERIVED` — `I18N-001`–`I18N-004`, `VOC-003`–`VOC-005`, `CONJ-002`, `WEB-005`.

### 5.3 Privacy and ownership

- Reading capabilities must not require login solely for tracking.
- A design must distinguish ephemeral analysis from user-owned persisted learning data.
- Authentication must not be presented as equivalent to authorization or ownership.
- Third-party transfer must be disclosed before the transfer and limited to the context required by the user-requested action.
- The UI must not imply that selected text or interaction history is stored when the approved behavior is ephemeral by default.

Classification and trace: `SOURCE-DERIVED` — `AUTH-001`–`AUTH-004`, `PRIV-001`–`PRIV-006`; Platform Foundation `FR-042`–`FR-050`.

## 6. Visual System Status

The final visual system is intentionally unresolved. The project must learn these decisions through representative feature design, implementation prototypes, accessibility checks, performance checks, and visual evaluation. No value should be selected merely to make this document appear complete.

| Visual-system area | Status | Decision boundary |
|---|---|---|
| Color palette and brand/action colors | **TBD** | Validate semantic use, contrast, content hierarchy, and cross-surface suitability. |
| Typography family and detailed type scale | **TBD** | Validate Vietnamese diacritics, Japanese scripts, fallback behavior, loading, and dense linguistic content. |
| Spacing scale | **TBD** | Derive from representative compact and full-page features before standardization. |
| Radius system | **TBD** | No global geometry has been approved. |
| Elevation and shadow model | **TBD** | Evaluate separation, host-page legibility, contrast, and rendering cost. |
| Icon language | **TBD** | Validate meaning, labeling, localization, and accessibility before selecting a family or style. |
| Motion language | **TBD** | Validate purpose, reduced motion, performance, state clarity, and distraction. |
| Light/dark appearance | **TBD** | No appearance modes or palette mapping are approved. |
| Component visual styling | **TBD** | Components may emerge locally; visual reuse requires evidence and promotion. |

`MIGRATION_DECISION.md` §14 permits presentation values to become UI constants or design tokens, but it does not choose those values. Exact values remain local `FEATURE-DD` decisions until they qualify for promotion.

## 7. Feature Design Contract

Future features must be designed individually. Their design artifact should define, where applicable:

- user goal and approved requirement scope;
- entry points and user flow;
- information hierarchy and available actions;
- interaction model;
- empty, loading, success, error, partial, and unavailable states;
- stale/currentness behavior and retry/recovery behavior;
- keyboard and focus behavior;
- accessibility considerations;
- multilingual behavior;
- privacy, disclosure, authentication, authorization, and ownership implications;
- surface, responsive, browser, and host-environment constraints;
- existing project patterns being reused;
- new feature-specific design decisions and their evidence.

Not every feature needs every state or consideration. The feature artifact must mark non-applicable areas rather than invent behavior to fill a template.

Feature design must remain consistent with `REQUIREMENT.md`, applicable architecture constraints, active feature specifications, and the global principles in this document. Exact layouts, navigation, component hierarchy, copy, actions, and interaction sequences belong to feature design unless already mandated by an authoritative source.

Each feature must identify its applicable verification areas. Possible areas include accessibility, keyboard/focus, multilingual rendering, async/currentness, error/recovery, privacy, performance, responsive behavior, host-environment isolation, contrast, and reduced motion. This list is conditional, not a universal test suite.

## 8. Feature design decision classification

This taxonomy records the authority and lifecycle of constraints or decisions used during feature design. It must not be stretched to classify every normative project principle:

- A principle directly backed by an approved source uses `SOURCE-DERIVED`.
- A choice created for one feature uses `FEATURE-DD`.
- A validated feature choice intentionally made global uses `PROMOTED-DD`.
- A project-level governance principle explicitly approved directly in this document is identified in prose as such. It is not a fourth taxonomy category, a `FEATURE-DD`, or a `PROMOTED-DD`. If it later produces a reusable feature pattern, that pattern must still follow the normal promotion lifecycle.

No additional project-level decision category is defined, because doing so would create a fourth category outside the approved taxonomy.

### `SOURCE-DERIVED`

A design constraint directly derived from an approved project source. It may be global only when the underlying source is global. Its record must cite the relevant requirement, architecture decision, or approved specification.

### `FEATURE-DD`

A new design decision introduced while designing one specific feature. It remains local to that feature by default, even if it appears useful or visually consistent. Feature-level choices such as layout, copy, tokens, components, or decorative treatment must not silently become project rules.

### `PROMOTED-DD`

A previously feature-specific decision that has been validated across multiple independent features or surfaces and is intentionally promoted into this document. Its record must preserve the evidence, affected features, rationale, compatibility constraints, and promotion date.

Speculative choices must not be recorded as `SOURCE-DERIVED` or `PROMOTED-DD` merely because they seem reasonable.

## 9. Promotion Rule

A `FEATURE-DD` may become a `PROMOTED-DD` only when:

1. it appears across multiple independent features or surfaces;
2. it has proven useful, not merely visually consistent;
3. standardizing it reduces future ambiguity;
4. it remains compatible with product and architecture requirements; and
5. sufficient evidence shows that it represents a project-wide pattern.

The expected evolution is:

```text
Feature design
→ FEATURE-DD
→ implementation and validation
→ reuse across independent features
→ PROMOTED-DD
→ global DESIGN.md
```

One feature’s need is not enough to create a global token, component, interaction pattern, or visual rule.

## 10. Global guardrails

- Visual treatment must not compromise readability, linguistic correctness, state clarity, accessibility, privacy clarity, low-latency interaction, Extension performance, or host-page resilience.
- Decorative or computationally expensive effects require a concrete feature purpose and feature-level validation; they are not introduced by default.
- A client must not visually flatten restrictions, provenance, language, partial completion, or authoritative-versus-derived status into a misleading result.
- State communication must remain understandable without color as its only cue.
- The Extension must remain isolated from hostile host-page CSS and account for applicable focus, keyboard, positioning, and page-lifecycle behavior. Exact Shadow DOM mode and detailed behavior remain downstream decisions (`BROWSER-003`; `MIGRATION_DECISION.md` §8.4; `OD-011`).
- MVP translation must remain explicitly user-initiated and must disclose third-party transfer as required (`TRN-004`, `PRIV-004`).
- Anonymous reading must not be blocked by login; persistence and synchronization may require authentication (`AUTH-001`–`AUTH-002`).
- Raw selected text and analysis/translation history must not be presented or persisted as default history without an approved feature (`PRIV-006`).
- Marketing, pricing, testimonial, social-proof, newsletter, and equivalent landing-page patterns do not belong in product feature design unless future approved requirements introduce them.
- External design references may inspire exploration but may not override requirements, create product behavior, or become project rules without the decision process in this document.

## 11. Open project-wide design decisions

The following are intentionally open rather than unsupported global commitments:

- the visual-system areas marked **TBD** in §6;
- final brand expression and decorative identity;
- the project-wide accessibility conformance target beyond source-specific acceptance criteria;
- which feature-level components or patterns, if any, earn promotion after implementation evidence;
- detailed Web navigation and Extension interaction models, which belong to their applicable feature specifications;
- exact Shadow DOM mode, focus model, and positioning behavior tracked by `OD-011`.

Open decisions must be resolved in the artifact with the appropriate authority. Product behavior belongs in `REQUIREMENT.md` or an approved feature specification; feature presentation belongs in feature design; project-wide patterns enter this document only through the Promotion Rule.

## References

- `REQUIREMENT.md` — authoritative product behavior and scope.
- `SDD.md` — project memory and architectural direction; particularly Reading First, server authority, shared clients, and stale-response protection.
- `MIGRATION_DECISION.md` — legacy disposition and V2 constraints; particularly §§6.10, 6.14–6.15, 8.4, 14–15.
- `.sdd/specs/feat-platform-foundation/SPEC.md` — shared semantic, privacy, browser, and asynchronous boundaries.
- `.sdd/specs/feat-dict-lookup/SPEC.md` — active draft feature context; its feature-specific API, performance, and presentation choices are not promoted here.
