# Data Model: Japanese Text Detection (F-01)

**Feature:** [SPEC.md](SPEC.md)
**Persistence:** None

F-01 has no database entity, API resource, migration, user history, preference or persistent linguistic record. The models below describe only in-memory input, output and development fixtures.

## Runtime input

| Item | Shape | Lifetime | Rules |
|---|---|---|---|
| Detector input | A JavaScript string | One classifier call | It is used exactly as received. No normalization, entity decoding, encoding repair, truncation or context lookup is permitted. |
| Boundary input | Unknown caller value | One boundary call | Only a string is a classifiable value. null, undefined, primitives other than string, arrays and objects are invalid input; none may be coerced. |

The boundary may receive an empty string. Empty and whitespace strings are valid inputs that classify false, not invalid input.

## Runtime outcome

| Outcome | Meaning | Consumer rule |
|---|---|---|
| Classification true | At least one code point is a member of the frozen trigger policy. | Eligible to proceed to a later feature; F-01 does not start analysis itself. |
| Classification false | The input is a valid string with no trigger member. | Normal negative result. |
| Invalid input | The boundary received a non-string. | Must remain distinguishable from false; diagnostic content cannot include raw input. |
| Unexpected failure | An execution fault occurred at the defined seam. | Must remain distinguishable from false and true; it must not trigger downstream analysis. |

There is no confidence, language label, matched span, canonical identifier, user identifier or retained input reference.

## Unicode policy data

| Item | Fields required for review | Relationship |
|---|---|---|
| Policy identity | Unicode version 17.0.0; policy name/version | One policy identifies the semantics for one generated table and corpus run. |
| Source manifest entry | UCD filename, canonical URL, source checksum, license/notice reference | Four entries supply the generation inputs. |
| Generated range table | Sorted inclusive start/end code-point pairs; generated-artifact checksum | The core reads this immutable build artifact. |
| Generator record | Generator revision/command and validation result | Links inputs deterministically to the output artifact. |

The range table contains only the membership selected in SPEC.md section 3.2. It is not a copy of a CJK block and must not represent an open-ended Unicode upgrade.

## Test corpus case

| Field | Meaning and validation |
|---|---|
| id | Stable F01-AC identifier or a stable subcase identifier derived from it. IDs must not be renumbered when cases are added. |
| input representation | Literal text only when unambiguous; otherwise an ordered list of Unicode code points or UTF-16 code units. |
| expected outcome | true, false, invalid-input, or unexpected-failure at the deliberate test seam. It is authored independently of the detector. |
| policy version | Unicode 17.0.0. |
| source note | Acceptance criterion or verified migration/provenance note. |

Malformed-surrogate fixtures use UTF-16 code units, not Unicode scalar values. Supplementary, selector and private-use fixtures state code points explicitly where display can be ambiguous.

## State and retention

F-01 is stateless. Each call is independent; it does not create a cache entry, history row, telemetry record, storage key, DOM mutation, request, or retained reference to input. There are therefore no state transitions, relationships to Backend data, or deletion/migration rules.
