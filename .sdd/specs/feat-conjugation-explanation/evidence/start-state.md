# F-09 start state — 2026-10-13

- Branch: `feat/tokenizer-deinflection`; HEAD `c0cb500`.
- The working tree already contains uncommitted F-06/F-08/initial F-09 code and documentation. This work preserves those changes and does not commit, reset, merge or migrate the user's database.
- Scope decision from the user: review the nine existing conjugation rules in this pass. The user will review the linguistic answers. This selects the scope and reviewer; it does not approve the yet-to-be-written corpus bytes, individual answers or production publication.
- Applicable artifacts: [SPEC](../SPEC.md), [PLAN](../PLAN.md), [TASKS](../TASKS.md). Implementation follows US1 → G1 → US2 → G2 → US3; no human gate is inferred from test output.
- Initial code/dataset changes in the dirty tree belong to the earlier tokenizer/deinflection slice. F-09 review starts from its immutable `development-20261013.1` dataset.

## Boundary

This pass prepares a nine-rule editorial corpus. No extra conjugation type, sentence analysis, new API or production status is implied by this scope decision.

## Scope correction — 2026-10-13

The later user decision supersedes the nine-rule-only boundary above: choose B,
including advanced verb forms and い/な-adjective plus noun/copula coverage.
The old packet was never G1 approved; see [scope-change](./scope-change.md) and the
[expanded catalog](../corpus/expanded-20261013.1/coverage-catalog.md). T005–T010
are reopened for the replacement scope; the original start-state record is retained
as history rather than silently rewritten.
