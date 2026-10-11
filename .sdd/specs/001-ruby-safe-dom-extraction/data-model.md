# Data Model: Ruby-Safe DOM Extraction (F-02)

**Date**: 2026-09-24  
**Persistence**: None — every value is ephemeral for one extraction call

## 1. Selection Reader

Represents the browser boundary that returns the selection associated with the current document/frame.

| Field / behavior | Rule |
|---|---|
| Current context | Bound to exactly one document/frame |
| Selection acquisition | Returns the current Selection, no Selection, or raises an access failure |
| Frame scope | Never resolves parent, child, or sibling frame content |
| Lifetime | One synchronous extraction operation |
| Retention | Reader and returned DOM references are released after the operation |

Validation:

- An acquisition access failure maps to `inaccessible-context`.
- A null/empty/collapsed Selection maps to `no-selection`.
- More than one range maps to `unsupported-multi-range` before any range content is read.

## 2. Selection Snapshot

Represents the one selected DOM interval validated for extraction.

| Attribute | Meaning | Validation |
|---|---|---|
| Context document | The document/frame that owns the selection | Must match both range boundary nodes |
| Range count | Number of selected ranges | Exactly 1 |
| Collapsed state | Whether start equals end | Must be false |
| Start boundary | Start node and UTF-16 offset | Must remain valid and connected to the context |
| End boundary | End node and UTF-16 offset | Must remain valid and connected to the context |
| Direction | Forward or backward user gesture | Does not change document-order output |

The snapshot is not serialized, logged, cached, or passed to the Backend.

## 3. Extraction Token

Internal, browser-independent input to deterministic text assembly.

### Token variants

| Variant | Payload | Meaning |
|---|---|---|
| `text` | Exact selected string segment | Eligible base text copied without normalization |
| `explicit-break` | None | A selected `br`; always contributes one `LF` |
| `block-boundary` | None | A boundary after selected structural block content; contributes at most one needed `LF` |

Rules:

- Empty `text` tokens have no effect.
- `explicit-break` tokens are never coalesced with another explicit break.
- Consecutive/nested `block-boundary` tokens are coalesced.
- A block boundary does not create a leading or trailing `LF`.
- If existing text or an explicit break already ends with `LF`, a pending block boundary adds no duplicate.

## 4. Extracted Base Text

Represents successful output.

| Attribute | Rule |
|---|---|
| Text | Complete selected eligible base text in document order |
| Annotation | Contains no selected descendants of HTML `rt` or `rp` |
| Whitespace | Original selected text whitespace is preserved |
| Structural separator | Deterministic `LF` according to explicit and block-boundary rules |
| Unicode | No normalization, decoding, repair, or surrogate rewriting |
| Markup | None; output is plain text |
| Persistence | None |

Validation:

- Text must be non-empty after annotation exclusion and structural assembly.
- Empty output maps to `no-base-text`.
- The complete text is returned; F-02 has no truncation rule.

## 5. Extraction Error

Content-free non-success outcome.

| Code | Trigger | Retry semantics |
|---|---|---|
| `no-selection` | Null, empty, or collapsed selection | Wait for a new non-collapsed selection |
| `unsupported-multi-range` | Range count is greater than one | Reader must create a supported single-range selection |
| `no-base-text` | Annotation exclusion leaves no eligible base text | Do not retry without a different selection |
| `stale-selection` | Boundaries are detached/incoherent or lose validity during traversal | A newer selection may be processed as a new interaction |
| `inaccessible-context` | Current document/frame selection cannot be accessed | Do not broaden permissions automatically |
| `unexpected-failure` | Any other unexpected boundary failure | Caller may expose a generic non-content failure in a later feature |

Error invariants:

- No raw or partial text.
- No DOM/Range/Node references.
- No HTML markup or selector paths.
- No URL fragment or page content.
- No stack trace in the public result.

## 6. Extraction Result

Discriminated union:

| Status | Content |
|---|---|
| `ok` | One Extracted Base Text value |
| `error` | One Extraction Error code |

The caller must exhaustively handle both statuses. An error is never equivalent to successful non-Japanese text; F-01 classification runs only for `ok`.

## 7. State Transitions

```text
requested
  ├─ selection unavailable/collapsed ─────────────→ error:no-selection
  ├─ selection access denied ─────────────────────→ error:inaccessible-context
  ├─ multiple ranges ─────────────────────────────→ error:unsupported-multi-range
  └─ one valid range
       ├─ snapshot loses coherence ───────────────→ error:stale-selection
       ├─ unexpected traversal failure ───────────→ error:unexpected-failure
       └─ tokens assembled
            ├─ no eligible base text ─────────────→ error:no-base-text
            └─ complete base text ────────────────→ ok
```

Every terminal state releases transient DOM references. There is no persisted transition history.

## 8. Relationships

```text
Selection Reader
  → Selection Snapshot
      → ordered Extraction Tokens
          → Extraction Result
              ├─ Extracted Base Text → F-01 Japanese detection
              └─ Extraction Error    → no downstream analysis
```

F-03 will own the interaction identity, event timing, debounce, and popup behavior. F-02 neither stores nor advances F-03 state.

