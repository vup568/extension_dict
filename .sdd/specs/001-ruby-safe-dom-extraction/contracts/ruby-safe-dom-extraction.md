# Local Contract: Ruby-Safe DOM Extraction (F-02)

**Status**: Planned  
**Boundary type**: Local Extension browser boundary  
**Network API**: None

## Purpose

Convert the current single-range browser selection into ruby-safe plain base text or a content-free error. The contract is called locally and performs no network, storage, logging, popup, debounce, or analysis operation.

## Input Contract

The caller supplies a selection reader bound to the current document/frame.

Conceptual signature:

```text
extractRubySafeSelection(readSelection) → ExtractionResult
```

The reader:

- returns the Selection for only its current document/frame;
- returns no selection when none exists;
- may fail when its context is inaccessible;
- does not traverse or aggregate any other frame.

Preconditions are validated by the boundary; callers do not need to pre-read selected text.

## Output Contract

### Success

```json
{
  "status": "ok",
  "text": "日本語"
}
```

Rules:

- `text` is plain text only;
- it is complete and non-empty;
- selected HTML `rt`/`rp` descendants are absent;
- original eligible Unicode and whitespace are preserved;
- structural boundaries use `LF`;
- no DOM reference escapes the call.

### Error

```json
{
  "status": "error",
  "code": "no-selection"
}
```

Allowed `code` values:

- `no-selection`
- `unsupported-multi-range`
- `no-base-text`
- `stale-selection`
- `inaccessible-context`
- `unexpected-failure`

No error object may contain `text`, partial text, markup, DOM data, selector paths, page URLs, or stack traces.

## Behavioral Contract

1. Acquire Selection exactly once through the supplied reader.
2. Validate range count before reading range content.
3. Accept exactly one non-collapsed range.
4. Validate that range boundaries belong to the expected connected document.
5. Traverse only nodes intersecting the selected range in document order.
6. Exclude HTML `rt` and `rp` subtrees by original DOM ancestry.
7. Preserve exact selected portions of boundary text nodes.
8. Convert selected `br` elements to explicit `LF`.
9. Convert the reviewed semantic block policy to coalesced logical `LF` boundaries.
10. Return `no-base-text` when no eligible text remains.
11. Release transient Selection/Range/Node references after returning.

## Error Mapping

| Boundary observation | Contract result |
|---|---|
| Reader returns null; Selection has no range; Selection is collapsed | `no-selection` |
| Selection has more than one range | `unsupported-multi-range` |
| Eligible assembled text is empty | `no-base-text` |
| Boundary nodes are disconnected, cross-document, invalid, or lose coherence | `stale-selection` |
| Selection reader cannot access its current context | `inaccessible-context` |
| Other unexpected extraction exception | `unexpected-failure` |

Failures never fall back to full-page text, `Selection.toString()`, a first range, a parent frame, or partial accumulated output.

## Consumer Contract

- F-01 receives only successful `text`.
- Annotation-polluted raw selection text is not an authoritative fallback.
- F-03 may decide whether and when to call this contract, but it cannot reinterpret error as successful selection.
- F-04 may associate the call with interaction identity later; F-02 result does not own that identity.
- Backend analysis is outside this contract.

## Privacy and Side-Effect Contract

For one invocation, F-02 creates:

- zero network requests;
- zero persistent or session storage entries;
- zero selection-history records;
- zero raw-content logs/telemetry;
- zero DOM/style/focus/selection/editable/clipboard/navigation mutations;
- zero new browser permission requests.

## Compatibility Contract

The same fixture must produce the same success text or error category on Chrome, Edge, Brave, and Firefox. Brave and Chrome are primary manual-validation targets; this does not alter required parity.

An unavailable target is reported as pending. A Chromium result is not substituted as evidence for a named browser that was not executed.

