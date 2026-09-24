# Feature Specification: Ruby-Safe DOM Extraction (F-02)

**Feature Branch**: `feat/ruby-safe-dom-extraction`  
**Feature Directory**: `.sdd/specs/001-ruby-safe-dom-extraction`  
**Created**: 2026-09-24  
**Status**: Implemented — local verification complete; Firefox execution evidence pending  
**Input**: Create a complete specification for F-02 Ruby-Safe DOM Extraction using the approved question-based structure. Product-owner decisions: preserve structural line breaks, process a selection locally in the frame where it occurs, and support exactly one non-collapsed range in MVP.

## Source Authority

- [REQUIREMENT.md](../../../REQUIREMENT.md): `DOM-001`, `JPN-001`, `PRIV-002`, `PRIV-006`, `PERF-001`, and `BROWSER-001`–`BROWSER-002`.
- [FEATURE_MAP.md](../../../docs/FEATURE_MAP.md): F-01 → F-02 → F-03 dependency and UC-01.
- [MIGRATION_DECISION.md](../../../MIGRATION_DECISION.md): §5.8 and §9.2 selection/ruby regression coverage.
- [Constitution](../../constitution.md): browser isolation, ruby-safe text, privacy, cross-browser behavior, testing, and Spec Kit gates.

This specification defines observable product behavior. It does not select an extraction algorithm, browser API wrapper, framework, package, or source-file layout.

## 1. Context & Goal — Tại sao feature này tồn tại?

F-01 can classify a supplied string, but a reader interacts with a browser DOM rather than a prebuilt plain string. Japanese pages commonly render pronunciation with ruby markup. A naive extraction can mix the base text with `rt` or `rp` annotation, turning a selection such as 日本 with にほん furigana into polluted linguistic input such as 日本にほん.

F-02 exists to produce one trustworthy, ephemeral base-text value from the reader's current selection. The result becomes the only selection text eligible for downstream Japanese detection and analysis. F-02 protects linguistic correctness without reading unrelated page content, modifying the host page, creating history, opening a popup, or contacting the Backend.

The feature succeeds when:

- selected base text is preserved in document order;
- all selected `rt` and `rp` annotation content is excluded;
- structural line boundaries are deterministic;
- selection behavior is equivalent across the approved browser targets;
- unsupported, stale, inaccessible, or empty selections fail explicitly without partial output;
- selected text remains ephemeral and content-free diagnostics reveal no raw text.

## 2. Actors & Roles — Ai tương tác, với quyền gì?

| Actor / system | Role in F-02 | Permissions and limits |
|---|---|---|
| Anonymous Reader | Creates or changes a text selection while reading a page | No account or login is required; the reader does not grant F-02 permission to inspect content outside the active selection |
| Browser Extension | Requests extraction and consumes the resulting base text or error category | May inspect only the selected DOM range in the document/frame where the selection occurred; may not persist, transmit, log, or build history from the raw text |
| Browser Host Page | Supplies untrusted DOM, selection state, and page lifecycle | May contain nested elements, ruby markup, editable content, hostile styling, DOM mutation, or frames; it is never an authority over product behavior |
| Downstream detector/analysis orchestration | Consumes successful base text | Must treat F-02 output, rather than annotation-polluted page text, as the authoritative selection input |

Authentication and authorization roles are intentionally absent. F-02 neither reads nor writes user-owned persistent data and must behave the same for anonymous and authenticated readers.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Extract clean Japanese base text (Priority: P1)

As a reader, I want the extension to use the Japanese characters I selected without duplicated furigana so that later dictionary and grammar analysis receives the text I actually read.

**Why this priority**: Removing ruby annotation is the defining value of F-02 and a correctness prerequisite for every later selection-based feature.

**Independent Test**: Select fixtures containing plain text, `ruby`, `rt`, and `rp`; verify the returned value contains the selected base text in document order and none of the annotation text.

**Acceptance Scenarios**:

1. **Given** `<ruby>日本<rt>にほん</rt></ruby>` is fully selected, **When** extraction runs, **Then** the successful base text is exactly `日本`.
2. **Given** `<ruby>日本<rp>(</rp><rt>にほん</rt><rp>)</rp></ruby>` is fully selected, **When** extraction runs, **Then** the result is exactly `日本`.
3. **Given** only annotation content is selected, **When** extraction runs, **Then** the result is `no-base-text` and no partial or annotation text is returned.
4. **Given** selected supplementary ideographs, combining marks, or variation selectors belong to base text, **When** extraction runs, **Then** their original code points and order are preserved.

---

### User Story 2 — Preserve meaningful structure across real DOM (Priority: P2)

As a reader, I want selections spanning nested elements, line breaks, blocks, and editable content to remain readable so that words and sentences are not silently joined or normalized into different text.

**Why this priority**: Correct ruby removal is insufficient if DOM boundaries merge words, lose line breaks, or alter Unicode content.

**Independent Test**: Select nested inline, `br`, multi-block, partial-node, and contenteditable fixtures; compare the result with the exact expected string.

**Acceptance Scenarios**:

1. **Given** adjacent selected inline elements, **When** extraction runs, **Then** their base text is concatenated without an invented separator.
2. **Given** a selected `br`, **When** extraction runs, **Then** it contributes one logical `LF` line break.
3. **Given** selected content crosses distinct block boundaries, **When** extraction runs, **Then** adjacent selected blocks are separated by one logical `LF`, without an artificial leading or trailing line break.
4. **Given** whitespace already present in selected text nodes, **When** extraction runs, **Then** it is preserved without trimming, collapsing, Unicode normalization, decoding, or repair.
5. **Given** one valid range inside contenteditable content, **When** extraction runs, **Then** it follows the same base-text, ruby, inline, and line-boundary rules.

---

### User Story 3 — Fail safely in hostile or changing page contexts (Priority: P3)

As a reader, I want the extension to ignore invalid or inaccessible selections safely so that it never sends partial, stale, cross-frame, or unexpectedly broadened page content for analysis.

**Why this priority**: Browser pages are untrusted and can mutate during selection. Explicit failure protects privacy and prevents misleading analysis.

**Independent Test**: Exercise collapsed, multi-range, detached, mutated, inaccessible-frame, and synthetic unexpected-failure fixtures; verify a non-content error and zero page side effects.

**Acceptance Scenarios**:

1. **Given** no range or a collapsed range, **When** extraction runs, **Then** it reports `no-selection`.
2. **Given** more than one range, **When** extraction runs, **Then** it reports `unsupported-multi-range` and returns no text from any range.
3. **Given** the selected range becomes detached or materially changes before extraction can complete, **When** extraction cannot produce a coherent snapshot, **Then** it reports `stale-selection` and returns no partial text.
4. **Given** a selection occurs inside an allowed frame, **When** extraction runs there, **Then** it reads only that frame's selected range and does not aggregate parent or sibling-frame content.
5. **Given** the selection context is inaccessible or outside granted page access, **When** extraction is requested, **Then** it reports `inaccessible-context` without attempting to broaden access.

### Edge Cases

- The selection begins or ends inside a base text node rather than at element boundaries.
- The selection begins in base text and ends inside `rt`/`rp`, or begins inside annotation and ends in base text.
- Ruby markup is nested inside inline formatting, links, or editable content.
- The same annotation text appears elsewhere as legitimate base text; only ancestry inside selected `rt`/`rp` excludes it.
- Consecutive `br` elements preserve consecutive logical line breaks.
- A block boundary already has an adjacent selected `LF`; extraction must not invent a duplicate boundary line break.
- Selection direction is backward; output still follows document order.
- The selected base text contains U+0000, supplementary code points, combining marks, variation selectors, replacement characters, or lone UTF-16 surrogates; F-02 preserves received text and leaves classification to F-01.
- The page changes unrelated DOM outside the selected range; that alone does not invalidate a coherent selection.
- A permitted iframe contains the selection while the parent page has another selection-like state; only the event frame is authoritative.
- A closed or otherwise inaccessible tree contains text; F-02 does not bypass the platform access boundary.
- The extracted base text is very long; F-02 does not truncate it because the product-wide maximum-selection policy remains deferred under OD-014.

## Requirements *(mandatory)*

### 3. Functional Requirements — Hệ thống làm gì?

- **F02-FR-001**: WHEN extraction receives exactly one active, non-collapsed range, the system SHALL inspect only the portion of the DOM covered by that range.
- **F02-FR-002**: The system SHALL return selected eligible base text in document order, independent of forward or backward selection direction.
- **F02-FR-003**: The system SHALL exclude all selected textual descendants of `rt` and `rp` elements from the extracted result.
- **F02-FR-004**: The system SHALL preserve selected base text inside `ruby` and all other selected eligible descendants that are not excluded by F02-FR-003.
- **F02-FR-005**: Adjacent selected inline base-text segments SHALL concatenate without an invented separator.
- **F02-FR-006**: Every selected `br` boundary SHALL contribute one `LF` line break.
- **F02-FR-007**: Crossing between distinct selected block contents SHALL contribute one logical `LF` separator when no selected line break already represents that boundary; extraction SHALL NOT add a line break solely before the first or after the last selected base segment.
- **F02-FR-008**: The system SHALL preserve whitespace and Unicode content already present in selected text nodes; it SHALL NOT trim, collapse, normalize, decode entities/escapes, repair encoding, or rewrite malformed UTF-16.
- **F02-FR-009**: Partial selections SHALL include only the selected code-unit boundaries of the start and end text nodes and SHALL NOT expand to whole words, elements, ruby groups, sentences, or blocks.
- **F02-FR-010**: Contenteditable selections SHALL follow the same extraction rules as non-editable page content.
- **F02-FR-011**: Extraction SHALL execute within the document/frame context where the selection occurred and SHALL NOT combine content from a parent, child, or sibling frame.
- **F02-FR-012**: A selection inside a frame SHALL require only the access already granted for that frame; an inaccessible context SHALL fail explicitly rather than trigger broader collection.
- **F02-FR-013**: MVP SHALL accept exactly one non-collapsed range. Zero/collapsed ranges and multiple ranges SHALL produce explicit non-success outcomes.
- **F02-FR-014**: If the range is detached or changes so that a coherent snapshot cannot be produced, extraction SHALL return no partial text.
- **F02-FR-015**: A successful extraction containing no eligible base text after annotation removal SHALL be represented as `no-base-text`, not as annotation text or a normal analyzable selection.
- **F02-FR-016**: F-02 SHALL provide its successful base text as the authoritative selection text for downstream Japanese detection and analysis gating.
- **F02-FR-017**: F-02 SHALL NOT itself open a popup, debounce selection events, call the Backend, classify Japanese text, or perform linguistic analysis.
- **F02-FR-018**: F-02 SHALL NOT persist, cache, transmit, log, or create history from selected or extracted text.
- **F02-FR-019**: F-02 SHALL NOT mutate host DOM, style, focus, active selection, editable content, navigation, clipboard, or page event behavior.
- **F02-FR-020**: F-02 SHALL process the complete selected range without silent prefix-only scanning or truncation; future input limits must be enforced by a separately approved boundary under OD-014.

### 4. Non-functional Requirements — Tốt đến mức nào?

| ID | Quality requirement | Verifiable outcome |
|---|---|---|
| F02-NFR-001 | Linguistic correctness | 100% of the approved ruby, nested-DOM, line-boundary, partial-range, Unicode, and editable-content corpus returns the exact expected base text or error category |
| F02-NFR-002 | Privacy minimization | Extraction reads only the selected range in the event document/frame; no surrounding page, parent frame, sibling frame, or full-document content appears in the result or diagnostics |
| F02-NFR-003 | Ephemeral handling | Raw selected/extracted text creates zero network requests, storage entries, history records, telemetry content, or ordinary log content |
| F02-NFR-004 | Cross-browser semantics | The same applicable corpus produces equivalent text and error categories on Chrome, Edge, Brave, and Firefox; Brave and Chrome are the owner's primary manual-validation browsers, without weakening the four-browser contract |
| F02-NFR-005 | Host-page safety | Extraction causes zero observable DOM, style, focus, selection, editable-value, clipboard, navigation, or event-default mutations |
| F02-NFR-006 | Determinism | The same coherent selection snapshot and rules produce the same output regardless of locale, page language, login state, network state, font, CSS, or previous extraction |
| F02-NFR-007 | Performance contribution | Extraction performs no network wait or intentional debounce. The plan SHALL benchmark representative small, nested-ruby, multi-block, editable, and long selections and report its contribution to the approved selection-to-popup p95 ≤ 150 ms objective without inventing a standalone pass threshold |
| F02-NFR-008 | Browser isolation | Browser/platform access is confined to the extraction boundary; downstream text transformation and result semantics remain independent of vendor-specific globals |
| F02-NFR-009 | Untrusted-page resilience | Malformed, deeply nested, rapidly changing, or inaccessible page structure produces a bounded success/error outcome and SHALL NOT execute selected markup or serialize executable HTML |

### 5. Data Model — Dữ liệu có cấu trúc gì?

F-02 has an ephemeral interaction model and no persistent database model.

#### Selection Source

- Exactly one non-collapsed range.
- Belongs to one document/frame context.
- Carries ordered start and end boundaries plus the selected DOM portion.
- Exists only for the immediate extraction operation.

#### Extracted Base Text

- A plain string containing selected base text in document order.
- Excludes all selected `rt`/`rp` descendants.
- Preserves original selected Unicode content and deterministic `LF` structural boundaries.
- Contains neither HTML markup nor page nodes/references.
- Is not a canonical linguistic resource, user history, cache record, or saved learning item.

#### Extraction Outcome

- Success contains only the extracted base-text value.
- Non-success contains only a stable error category and optional non-content metadata needed to correlate the current interaction.
- A non-success outcome never contains partial selection text, raw DOM, markup, selector paths, page URL fragments, or user content.

No entity in F-02 has a durable identifier, ownership lifecycle, database schema, migration, retention period, or soft-delete behavior because nothing is persisted.

### 6. Error Handling — Khi sai thì làm gì?

| Error category | Condition | Required behavior |
|---|---|---|
| `no-selection` | No range exists or the only range is collapsed | Return no text; downstream popup/analysis does not start |
| `unsupported-multi-range` | More than one range exists | Return no range text, including the first range; caller may wait for a supported selection |
| `no-base-text` | Selection contains only excluded annotation or otherwise yields no eligible base text | Return no analyzable text; do not classify annotation as Japanese |
| `stale-selection` | Selected boundaries detach or materially change before a coherent snapshot is obtained | Return no partial text; a later user selection may be processed independently |
| `inaccessible-context` | Event document/frame/tree cannot be read under existing access | Return no content and do not broaden access or inspect related contexts |
| `unexpected-failure` | An unanticipated extraction failure occurs | Return a content-free failure category; do not leak raw text, DOM, markup, page content, or stack details to ordinary logs/UI |

Errors are distinct from a successful empty or non-Japanese classification. F-02 does not retry by rescanning the page, substitute full-page text, silently fall back to `textContent`, or return a partial selection.

## 7. Acceptance Criteria — Định nghĩa “xong” là gì?

| ID | Given / When | Then |
|---|---|---|
| F02-AC-001 | Full selection of `<ruby>日本<rt>にほん</rt></ruby>` | Success is exactly `日本` |
| F02-AC-002 | Full selection of ruby containing `rp` parentheses and `rt` reading | Success contains only the base text, with neither reading nor parentheses |
| F02-AC-003 | Selection contains plain Japanese and multiple ruby groups | Base segments remain in document order and every annotation descendant is absent |
| F02-AC-004 | Selection contains the same string once as base text and once inside `rt` | Only the ancestry-qualified annotation occurrence is excluded |
| F02-AC-005 | Start/end boundaries fall inside different text nodes | Only selected portions plus selected intervening base content are returned |
| F02-AC-006 | Selection starts in base text and ends in annotation, and the reverse fixture | Only eligible selected base portions are returned |
| F02-AC-007 | Selection contains only `rt`/`rp` content | Outcome is `no-base-text` with no annotation payload |
| F02-AC-008 | Nested inline elements form `今日は晴れ` | Output is exactly `今日は晴れ` without invented spacing |
| F02-AC-009 | Selected content is `一行<br>二行` | Output is exactly `一行\n二行` |
| F02-AC-010 | Two selected block contents are adjacent | Output contains exactly one logical `LF` boundary and no artificial outer line break |
| F02-AC-011 | Two consecutive selected `br` boundaries occur | Both logical line breaks are preserved |
| F02-AC-012 | Selected text nodes contain spaces, tabs, `LF`, decomposed kana, supplementary CJK, or variation selectors | Original selected code points/code units are preserved except defined structural separators |
| F02-AC-013 | Forward and backward selections cover the same DOM interval | Outputs are identical and ordered by the document |
| F02-AC-014 | One valid range occurs inside contenteditable content | Exact result follows all ruby, inline, block, and whitespace rules without changing the editable value |
| F02-AC-015 | Selection is absent or collapsed | Outcome is `no-selection` |
| F02-AC-016 | Selection contains two ranges | Outcome is `unsupported-multi-range`; neither range is returned |
| F02-AC-017 | Range becomes detached or incoherent during extraction | Outcome is `stale-selection`; no partial text is returned |
| F02-AC-018 | Selection occurs in an allowed iframe | Only that frame's selected range is extracted |
| F02-AC-019 | Parent or sibling frame contains separate content/state | No content from those contexts appears in output or diagnostics |
| F02-AC-020 | Frame/context is inaccessible | Outcome is `inaccessible-context`; no permission broadening or fallback page scan occurs |
| F02-AC-021 | Synthetic unexpected failure is injected at the extraction boundary | Outcome is `unexpected-failure` and contains no selected text/DOM/markup |
| F02-AC-022 | Host page has hostile CSS, active focus, existing selection, event handlers, and observed DOM | All values and observations remain unchanged after extraction |
| F02-AC-023 | Browser is offline, locale/page language/font/login state varies | The same coherent selection yields the same result and no network/auth dependency is observed |
| F02-AC-024 | Complete corpus runs on Chrome, Edge, Brave, and Firefox | Exact text/error categories have zero divergence; unavailable browser evidence remains pending rather than passed |
| F02-AC-025 | Synthetic unique raw-text markers exercise success and every error path | Network, storage, history, telemetry content, console/ordinary logs, and retained-result inspection reveal zero marker copies beyond the immediate success value |
| F02-AC-026 | A long valid selection ends with base text after deeply nested markup | Complete output is returned without prefix truncation; timing and input length are recorded |
| F02-AC-027 | Selection contains CSS-generated visual content or unselected neighboring DOM | Neither appears unless represented by selected eligible DOM text under this contract |
| F02-AC-028 | Annotation-only Japanese surrounds non-Japanese base text | Annotation is excluded; only base text is eligible for downstream F-01 classification |
| F02-AC-029 | Review compares implementation scope to this spec | No popup, debounce, request, Backend, persistence, authentication, page-context expansion, or linguistic analysis was added |
| F02-AC-030 | Review maps requirements and scenarios to tests/evidence | Every F02-FR, F02-NFR, F02-AC, and F02-SC has a pass, fail, or explicit pending status |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **F02-SC-001**: 100% of mandatory ruby/base-text fixtures produce the exact expected plain text with zero `rt`/`rp` annotation leakage.
- **F02-SC-002**: 100% of nested, partial, line-boundary, contenteditable, Unicode, and long-selection fixtures match their exact expected string without normalization or truncation.
- **F02-SC-003**: The complete applicable corpus has zero text/error-category divergence across Chrome, Edge, Brave, and Firefox; missing browser execution is recorded as pending.
- **F02-SC-004**: 100% of defined invalid, unsupported, stale, inaccessible, empty-base, and unexpected-failure fixtures return the required non-content category with zero partial raw-text payload.
- **F02-SC-005**: Privacy and host-page inspection finds zero network requests, persistent/storage/history records, raw-content log entries, retained DOM references, or host DOM/style/focus/selection mutations caused by F-02.
- **F02-SC-006**: Benchmark evidence covers small, nested-ruby, multi-block, contenteditable, and long selections on the approved profile and reports F-02 latency without claiming an unapproved standalone threshold.
- **F02-SC-007**: A reviewer can trace 100% of F02-FR, F02-NFR, F02-AC, and F02-SC identifiers to planned implementation/test/evidence before coding begins.

## 8. Out of Scope — Hệ thống KHÔNG làm gì?

F-02 does not:

- determine whether text is Japanese; F-01 owns classification;
- listen globally for selection changes or decide when a selection is stable;
- debounce reader input;
- open, position, style, or manage a popup;
- create Shadow DOM UI;
- send an analysis or translation request;
- collect sentence/page context outside the selected range;
- aggregate selections across frames;
- support multiple simultaneous ranges in MVP;
- bypass browser permissions, same-origin/platform access boundaries, or closed/inaccessible trees;
- infer visual text from CSS pseudo-elements, canvas, images, OCR, accessibility labels, or pixels;
- define a maximum selection length, truncate, summarize, or reject content by length; OD-014 remains a later contract decision;
- normalize Unicode, repair mojibake/surrogates, decode literal entities/escapes, tokenize, deinflect, translate, or perform grammar analysis;
- persist selection history, cache raw text, write learning data, or require authentication;
- redesign F-01, F-03, F-04, F-05, Backend contracts, database schema, or browser permission policy.

## Assumptions

- Product-owner decisions on 2026-09-24 select structural `LF` boundaries, local frame processing, and exactly one non-collapsed range for MVP.
- Brave and Chrome are the owner's primary manual-use browsers. Approved product parity still covers Chrome, Edge, Brave, and Firefox under BROWSER-002.
- F-01 is available as a downstream pure string classifier; F-02 does not duplicate its Unicode policy.
- F-03 will own selection-event lifecycle, stability/debounce, popup behavior, and orchestration of F-01/F-02.
- Existing browser access determines whether a frame/context is readable; F-02 does not introduce or expand host permissions.
- OD-014 remains deferred. F-02 preserves complete selected base text; later request boundaries may apply an approved length policy without changing ruby extraction semantics.
- Exact block-boundary classification and performance profiles are planning concerns, provided they implement the observable line-boundary contract in this specification.

## Constitutional Impact

| Principle | F-02 treatment |
|---|---|
| Product/system boundary | F-02 is a browser-client extraction capability; it creates no competing linguistic knowledge or persistent store |
| Browser isolation | Page/selection access remains at the browser boundary; extracted plain-text semantics are vendor-neutral |
| Linguistic correctness | Base text is preserved and ruby annotation excluded before downstream detection/analysis |
| Privacy and minimization | Only the active selected range is read; raw text is ephemeral and absent from logs, storage, history, and diagnostics |
| Stable identity/data/provenance | No canonical entity, imported dataset, provider, database migration, or provenance-bearing knowledge is introduced |
| External providers | None |
| Testing | Exact corpus, four-browser parity, privacy, host-side-effect, stale/inaccessible-range, and long-selection evidence are mandatory |
| Exceptions | No constitutional exception is proposed |

