# F-01 Final Traceability

**Reviewed:** 2026-09-20
**Implementation branch:** feat/japanese-text-detection
**Unicode policy:** 17.0.0

## Functional requirements

| Requirement | Implementation and verification | Status |
|---|---|---|
| F01-FR-001 | Generated policy table plus detector positive/negative suites | Pass |
| F01-FR-002 | Code-point iteration; supplementary cases AC-003/004/006/025 | Pass |
| F01-FR-003 | Mixed-script cases AC-007/008/017 | Pass |
| F01-FR-004 | Negative corpus AC-009–018 | Pass |
| F01-FR-005 | No normalization/decoding/repair; input-preservation tests | Pass |
| F01-FR-006 | Boundary invalid-input result and tests | Pass |
| F01-FR-007 | Explicit UTF-16 code-unit fixtures AC-024/025 | Pass |
| F01-FR-008 | Dependency isolation and browser privacy/side-effect suites | Pass |
| F01-FR-009 | Statelessness/environment tests; Chrome/Edge parity | Partial: Brave/Firefox pending |
| F01-FR-010 | Non-content result type, source inspection and marker tests | Pass |
| F01-FR-011 | Core imports only generated policy data | Pass |
| F01-FR-012 | 100,000-character positive-last tests and benchmark | Pass |

## Non-functional requirements

| Requirement | Evidence | Status |
|---|---|---|
| F01-NFR-001 | Pinned UCD checksums, deterministic 25-range table, 103-case corpus | Pass |
| F01-NFR-002 | Privacy harness, isolation test and result contract | Pass |
| F01-NFR-003 | browser-parity-privacy-report.md | Pending Brave and Firefox |
| F01-NFR-004 | performance-report.md cold/warm p95 profiles | Measurements pass; threshold pending OD-005 |
| F01-NFR-005 | Statelessness, host-page and environment tests | Pass on unit, Chrome and Edge |

## Acceptance criteria

| Acceptance criterion | Primary evidence | Status |
|---|---|---|
| F01-AC-001 | Single/multiple Han corpus | Pass |
| F01-AC-002 | Hiragana/Katakana corpus | Pass |
| F01-AC-003 | Standalone U+20B9F, 𠮟る and U+20000 | Pass |
| F01-AC-004 | Half-width, phonetic extension and supplementary Kana | Pass |
| F01-AC-005 | Composed/decomposed Kana without normalization | Pass |
| F01-AC-006 | Compatibility ideographs and U+3007 | Pass |
| F01-AC-007 | Mixed-script at either position | Pass |
| F01-AC-008 | Han policy and accompanying marks | Pass |
| F01-AC-009 | Empty and whitespace variants | Pass |
| F01-AC-010 | Latin, Vietnamese, digits and punctuation | Pass |
| F01-AC-011 | Hangul/Jamo variants | Pass |
| F01-AC-012 | Cyrillic, Arabic, Greek, Bopomofo, Yi and Tangut | Pass |
| F01-AC-013 | Emoji, music, math and square symbols | Pass |
| F01-AC-014 | Radicals, descriptions and compatibility symbols | Pass |
| F01-AC-015 | Standalone marks | Pass |
| F01-AC-016 | Selectors, controls, replacement, private-use and noncharacters | Pass |
| F01-AC-017 | Trigger letters with formats/selectors | Pass |
| F01-AC-018 | Unassigned boundary points | Pass |
| F01-AC-019 | Literal mojibake code-point fixture | Pass |
| F01-AC-020 | Literal entity and escape text | Pass |
| F01-AC-021 | Three 100,000-character cases | Pass |
| F01-AC-022 | Six-result sequence repeated 100 times | Pass |
| F01-AC-023 | Six non-string input categories | Pass |
| F01-AC-024 | Lone/separated surrogate units | Pass |
| F01-AC-025 | Trigger adjacent to malformed surrogate | Pass |
| F01-AC-026 | Injected unexpected failure with non-content result | Pass |
| F01-AC-027 | Dependency-isolation test and pure Node unit execution | Pass |
| F01-AC-028 | Complete corpus on target browsers | Partial: Brave/Firefox pending |
| F01-AC-029 | Synthetic marker on true/false/invalid/unexpected paths | Pass on available browsers |
| F01-AC-030 | Host DOM/style/focus/selection assertions | Pass on Chrome/Edge |
| F01-AC-031 | Connectivity and simulated page/user-state invariance | Pass on Chrome/Edge |
| F01-AC-032 | 12 profiles with cold and warm p95 | Measurements pass; budget pending OD-005 |
| F01-AC-033 | Stable IDs, independent expected values and provenance | Pass |

## Success criteria

| Success criterion | Evidence | Status |
|---|---|---|
| F01-SC-001 | Required AC-001–025 classifications/errors | Pass |
| F01-SC-002 | Four-browser zero-divergence requirement | Pending Brave and Firefox |
| F01-SC-003 | Network/log/storage/history prohibition | Pass for source/unit/Chrome/Edge; missing browsers are not fabricated |
| F01-SC-004 | Errors never classify as success | Pass |
| F01-SC-005 | Benchmark against approved detector budget | Pending OD-005 |

## Verification summary

- npm ci: pass; 54 packages installed from lockfile; audit reports 0 vulnerabilities.
- Unicode reproducibility: pass; 25 ranges; SHA-256 b46a981ebcc331220a81dafbc8c1f35bf17b1859cf016fa52a8c0dd6fec39bf9.
- Type check and Vite build: pass.
- Unit/corpus tests: 116/116 pass.
- Coverage: 93.54% statements, 86.66% branches, 100% functions, 93.33% lines.
- Browser tests: 10/10 pass on Chrome and Edge; Brave/Firefox pending.
- Benchmark measurements: complete; detector-specific threshold pending.
- Existing .NET Release build: pass with 0 warnings and 0 errors.
- Existing .NET unit tests: 51/51 pass.
- Existing .NET integration tests: environment-blocked; Testcontainers could not connect to `npipe://./pipe/docker_engine`, so 50/50 cases failed during fixture startup before their assertions ran.
