# Source parser audit — 2026-10-15

This is read-only source investigation after the owner's G0a audit-download permission. No database was written, no canonical mapping approved, and no dataset activated. G0b, OD-009, corpus/mapping review and target-bound activation remain pending.

## Full-source reconciliation

The new bounded C# parsers scanned the complete locally pinned artifacts. An independent artifact inventory in `data/dictionary-import/audit-20261015/audit-counts.json` supplied the expected counts.

| Component | Input records | Normalization eligible | Quarantined / blocked | Largest raw record, UTF-16 characters |
| --- | ---: | ---: | ---: | ---: |
| JMdict Japanese/English | 218,914 | 218,913 | 1 restriction contradiction | 4,922 |
| KANJIDIC2 | 13,108 | 13,108 | 0 | 4,247 |
| Vietnamese tab inventory | 172,321 | 0 approved mappings | 172,321 quarantined; 0 malformed | 15,596 |
| V1 overrides | 50 | 0 approved mappings | 50 quarantined | Lossless declarations retained |

Each category reconciles to its input total. “Normalization eligible” means structurally representable under SPEC/PLAN; it is not source/license approval or readiness to serve. JMdict canonical import fails closed until the one contradiction is resolved through review. The full audit retains that record and continues inventorying the remaining records; it does not silently omit it from an import.

Default limits were used: 2 Gi expanded source characters, 256 Ki prolog characters, 2 Mi characters per record/lexical node, depth32 and at most512 entity declarations. The pinned JMdict DTD contains267 declarations for266 distinct entities because `ik` is repeated with the identical value. Every declaration must exactly match the pinned entity table; repeated identical definitions remain bounded. Unknown, changed, external, parameter or nested entity definitions are rejected before XML parsing; no network/file resolver or recursive general-entity expansion is used.

The successful JMdict graphs contain233,641 written forms,265,825 readings,253,799 senses,443,729 English glosses,6,646 no-kanji readings and2,349 restricted joint applicability pairs. These graph totals exclude the blocked entry and are therefore not full-source canonical totals. KANJIDIC2 includes303 supplementary Unicode scalars and11,068 records with `r_type=vietnam`. These readings populate Hán Việt only; they do not populate Vietnamese meanings. Reading/meaning groups, variants, stroke variants, legacy JLPT and header/version metadata remain in raw metadata. Legacy JLPT is not relabeled as a modern official N level.

## Unresolved source contradiction

Exactly one record fails semantic applicability normalization: JMdict ordinal31,388, `ent_seq=1339160`, sense2.

- Written forms include `出し`, `出汁`.
- Reading `ダシ` is explicitly `re_nokanji`.
- Sense2 simultaneously declares `stagk=出し` and `stagr=ダシ`.
- The SPEC requires joint written AND reading restrictions; a no-kanji reading cannot pair with the required written form. The resulting pair set is empty.

The bounded raw record and exact failure are retained in ignored `data/dictionary-import/audit-20261015/jmdict-restriction-issues.json`. A synthetic regression preserves this shape without copying the source record into tests. `JmdictParser.Audit` returns its raw record and error, whereas canonical `Read` throws an identity-bearing error.

Decision options for the owner are: obtain an upstream-corrected snapshot and repin/revalidate it; or review an explicit versioned editorial correction for this exact entry/sense with evidence, then implement and hash that mapping before import. Leaving it unresolved keeps canonical import blocked. No interpretation, relaxation or correction has been applied. Dropping the whole entry or sense would not meet the full-source acceptance criteria.

## Vietnamese and V1 inventories

The Vietnamese inventory keeps ordinal identity bound to the snapshot SHA256, duplicate headwords, full logical lines and full content including tabs, HTML and escaped source markers. It performs no trimming, HTML interpretation, sense extraction, reading inference, deduplication or automatic semantic merge. All172,321 records remain quarantined. The largest logical record is15,596 UTF-16 characters, well beyond the former160-character packaging limit; it is retained in full.

Source lineage investigation points to OVDP/OVDP mirror metadata, including the172,321-word TOML declaration. A mirror-level license or project-level GPLv2 indication does not by itself establish the license of these exact bytes and their source components. OD-009 remains unresolved; no source clearance has been inferred.

All50 V1 override declarations were extracted from commit `788e93156f56dd735e16c3c99a209c5eeca089dd`, `scripts/prepare-javi.ts`. The original source blob SHA256 is `06d8e6ae231f5efa753e79845cd1be57dc1a94080168fe397a0bfd1b0cbd1220`; the exact override section SHA256 is `ab40af3e0d3608a834ddad3248d61f8ea5c514b68aa201f4b93a328187a1983d`. The [override inventory](v1-overrides.inventory.json) keeps ordinal identity, headword, Vietnamese text and original declaration. Every item is explicitly quarantined with null canonical target/sense/reviewer/date and no fabricated approval evidence.

## Artifacts and reproduction

Large/raw artifacts remain ignored under `data/dictionary-import/audit-20261015/`:

| Artifact | Size / records | SHA256 |
| --- | --- | --- |
| `JMdict_e.gz` | 10,585,292 bytes /218,914 | `1e89ac398ec0230b7bb1daa246ce0095421bf3667bb6509aab93e26e7fdfd1b5` |
| `kanjidic2.xml.gz` | 1,488,582 bytes /13,108 | `83f7b8775e6a66a4c10620b49101644d51d6de75cea329e1bff6994a982cc70d` |
| `star_nhatviet.tab` | 40,931,366 bytes /172,321 | `0ab16ce4121f5228a529203506bb771392b33dcd64eef1b109f91facad433f0e` |
| `vietnamese-inventory.jsonl` | 120,243,494 bytes /172,321 | `a80aceb682d2b84ca5497b7ad624ef40195a57457986b20152c98cd08fb1bb9d` |
| `v1-overrides.inventory.json` | 26,910 bytes /50 | `a198bc6a24711fe565ea84c4bc0681c5841553b5a75f2adb3ba27274845186a2` |
| `jmdict-restriction-issues.json` | 1 record | `b0aa2a1ea280e912f1ce0d251dcbc6dbe99d819b7277374ad39679cff424f323` |

Inventory schemas are [Vietnamese JSONL record schema](vietnamese-inventory.schema.json) (SHA256 `b547631c8be53b54e297bbef6f11d1d6c7ab82cdf1ddb71f32a3f44f62f5fa0a`) and [V1 override schema](v1-overrides.inventory.schema.json) (SHA256 `4bd497faf1ddafbbf921ab4db548a70d537c27fc5686167ea41d4eb82b914937`). Copies also accompany the ignored audit artifacts. JSONL `RawLine` excludes only the CR/LF record separator; the original source bytes remain separately pinned.

The temporary ignored `ParserAudit/ParserAudit.csproj` references existing Domain/parser sources and adds no package dependencies. Build with `--no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false`; run its DLL against the audit directory. The machine-readable `parser-audit.json` records full-scan counts and observed times. These local read/serialization timings are investigation evidence, not an import benchmark or SLA certification. Rebuilding artifacts may change timing fields while the pinned source and inventory facts remain unchanged.

Parser regressions cover stable identity, POS inheritance/codes, no-kanji and joint restrictions, raw retention, Hán Việt separation, supplementary scalars, HTML quarantine, long content, duplicate headwords, truncation/cancellation and bounded malicious XML. Final suite evidence is recorded by the parent implementation report.
