# F-09 — Gap matrix

G1/G2 accepted by user for corpus .3. This is baseline code/observed-provider triage; runtime corpus comparison follows T028.

| Approved cases | Baseline gap | Resolution |
|---|---|---|
| A*/N* copula, including 学生です/静かだ | Matcher rejects unchanged lexical head | Data-driven word patterns + canonical dictionary class validation |
| V10,書ける/書け | Provider base differs or unchanged | Full-word paradigm decoding, then exact canonical base/POS verification; never suffix-only guessing |
| V11/V13/V21 | Unique-result matcher and v2 cardinality | All independently evidenced alternatives; v3, v2 one-or-none |
| V04/V08–V20,I04–I11 | Missing finite variants/compositions | New immutable candidate with generalized patterns,115 reviewed rule identities |
| Nine baseline rules | Editorial VI/components revised in approved corpus | Stable IDs,version2; old development dataset untouched |
| Faults/exact/Unicode/async | Regressions required | Fresh unit/integration/client tests; no use of actual as expected |

23 duplicated supported-input buckets have identical rule/base/component shapes, not conflicting expected outputs. Vietnamese editorial phrasings may vary for the same identity; tests must require one of the approved equivalent wordings, not derive meaning from actual output.

