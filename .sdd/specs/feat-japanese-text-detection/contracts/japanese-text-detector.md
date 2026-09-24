# Local Contract: Japanese Text Detector (F-01)

**Feature:** [SPEC.md](../SPEC.md)
**Contract kind:** In-process TypeScript boundary; no HTTP endpoint or browser-vendor API

## Purpose

This contract lets the future F-02 selection boundary ask F-01 whether its final, ruby-safe base text contains an approved Japanese trigger. F-01 does not acquire selection text and does not initiate UI, network or Backend work.

## Input contract

| Caller supplies | Boundary behavior |
|---|---|
| A string, including empty or whitespace-only text | Classify the exact string according to the Unicode 17.0.0 policy. |
| null, undefined, number, boolean, array or object | Return/report invalid-input, distinct from a false classification. Do not stringify or inspect object content. |

Input is ephemeral. Callers must not expect normalization, HTML/entity decoding, encoding repair, language scoring, matched spans or a copy of their text.

## Outcome contract

| Category | Meaning | Required consumer behavior |
|---|---|---|
| true | One or more trigger characters occur in a valid input string. | A later feature may decide what to do next. F-01 itself performs no action. |
| false | Valid input has no trigger character. | Treat as a normal negative result. |
| invalid-input | Caller did not supply a string. | Do not treat as true or false; surface/handle as a boundary failure without raw text. |
| unexpected-failure | A deliberate boundary seam or unexpected runtime fault failed. | Do not treat as true; record only non-content diagnostic metadata if any. |

Implementation may choose a discriminated result value or a typed exception/result channel, but it must preserve this observable distinction and be testable for F01-AC-023 and F01-AC-026.

## Policy contract

The classifier returns true if and only if at least one inspected code point belongs to one of these exact sets:

1. Characters whose Unicode 17.0.0 General_Category is Lo and Script is Hiragana or Katakana.
2. Characters with Unicode 17.0.0 Unified_Ideograph=Yes.
3. Characters whose Unicode 17.0.0 General_Category is Lo and that occur in the CJK Compatibility Ideographs blocks.
4. U+3007.

Everything else, including standalone marks, punctuation, radicals, symbols, emojis, selectors, entity/escape literals and unassigned code points, is false unless another character in the same valid string is a member.

## Side-effect contract

The implementation must not access or mutate DOM, selection, focus, style, local/session storage, IndexedDB, cookies, authentication state, network, browser-vendor APIs, logs containing raw input, or retained module state. The same input always yields the same outcome independent of page state, connectivity, login state, locale and call history.

## Compatibility

This is a local contract for F-01 only. Any later F-02/F-03 integration owns selection extraction, ruby filtering, UI and cancellation. Changing policy version, accepted input semantics or outcome categories requires an update to SPEC.md, this contract, corpus expectations and browser evidence.
