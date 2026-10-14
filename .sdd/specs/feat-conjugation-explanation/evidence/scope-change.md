# F-09 scope change — 2026-10-13

The user rejected the prior nine-rule-only scope and selected **B**: include advanced verb forms plus い-adjectives, な-adjectives and noun/copula forms. The previous proposed packet `f09-nine-rules-20261013.1` (scope SHA-256 `9c541d95e04648f898abb167286865fa8bbac5118da87121f2815c1a39edcf0e`) was never G1 approved. It is retained byte-for-byte as an obsolete draft for traceability, not as an acceptance target.

The new catalog begins at [expanded coverage catalog](../corpus/expanded-20261013.1/coverage-catalog.md). Its examples and completion matrix are drafts. The 9 baseline rules remain prior implemented data, but they are no longer the full F-09 acceptance boundary. G1/G2/G3 are pending for the expanded scope. No runtime source, migration or user database was changed by this scope correction.

## Technical implications observed, not yet implemented

- Current `ConjugationMatcher.Match` returns no match when the first token's base form equals its surface. That excludes copula chains like `静かだ` and `学生です`; the enlarged scope requires a reviewed way to accept a changed auxiliary chain while preserving exact-first dictionary behavior.
- Sudachi maps `書ける` as its own base form and `書け` to `書ける`; potential/imperative cannot be deinflected to `書く` merely by taking the first provider base form.
- `食べられる` can express potential or passive without enough word-only context to choose one meaning. The user chose to display both evidenced explanations for the reader to choose. The current v2 contract allows one explanation, so a backward-compatible contract3 is required; v2 must not arbitrarily choose one.
- `学生ではありませんでした` tokenizes differently from the visually similar `静かではありません` prefix. Provider mappings must be derived from observed metadata and regression tested for both lexical groups.

These findings are constraints for the revised SPEC/PLAN and reviewer packet, not permission to guess answers. The user explicitly chose multiple explanations; contract3 is now planned, with v1/v2 preserved and implementation still gated by approved corpus/review.
