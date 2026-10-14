# F-09 corpus conventions

Status: editorial proposal until G1 and G2 are explicitly approved. All example strings are synthetic public test inputs, never captured user selections.

Scope note (2026-10-13): files directly under `corpus/` are the **unapproved nine-rule draft**.
They are retained for traceability and must not be used as the G1 target after the user's
choice B. The replacement four-group/advanced-form inventory is under
[`expanded-20261013.1/`](./expanded-20261013.1/coverage-catalog.md); its packet and
cases must be completed and approved independently. The remaining conventions below
describe the old packet's structure, not acceptance of its limited scope.

For the expanded packet, each slot has a stable unique ID, a word group, a PO decision,
source references and reviewed cases. Every required non-exact slot needs at least one
supported case; every implemented rule needs at least two supported and one negative
case. Ambiguous forms need discriminating cases or a documented unresolved outcome.
The same UTF-16, privacy, source-byte hash, revision and human-gate rules apply.

- `manifest.json` gives each of the nine baseline rule IDs one unique scope decision. This pass proposes `keep` and `required` for all nine. Changing that decision requires a new corpus revision and PO approval.
- `cases.json` gives every case a stable unique ID. Each required rule needs at least two `supported` cases and one `unavailable`/false-positive case. Cross-cutting exact, missing-entry, Unicode and failure cases supplement that minimum.
- For `supported`, the expected base form and Vietnamese explanation are editorial answers. `segments` are ordered observed surfaces with half-open UTF-16 spans; concatenated surfaces must equal the whole observed input. The provider observation helps identify its segmentation, but never certifies the meaning or rewrites the expected answer.
- Negative cases must not produce a conjugation detail. `completed_empty` means a form is recognized but the canonical base entry is absent; `unavailable` means no supported whole-word explanation can be produced. Exact dictionary hits keep exact-first lookup without conjugation.
- `sources.json` distinguishes original project editorial text from Sudachi technical observations. Project-authored content must still receive the named user's linguistic review. Existing provider documentation is not a linguistic authority.
- SHA-256 values are lowercase 64-character digests of exact UTF-8 source file bytes, not reconstructed JSONB bytes. A file never contains its own hash. Freeze sequence: cases/source packet → G1 scope approval → G2 review packet/answers → optional candidate dataset → fresh validation report → G3 acceptance manifest.
- Approved/frozen artifacts must not be overwritten to revise an answer. Preserve the old revision, create a new corpus/review version and invalidate dependent approvals.
