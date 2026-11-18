# Scope and source evidence: JMdict Japanese–English and KANJIDIC2

On 2026-10-30 the project owner explicitly chose JMdict Japanese–English instead of Japanese–Vietnamese and requested the implementation and import using JMdict and KANJIDIC2. This record documents that request; it does not claim an independent editorial review or permission to redistribute unrelated material.

The selected local snapshots are the EDRDG files `JMdict_e.gz` and `kanjidic2.xml.gz` in `data/dictionary-import/audit-20261015`. Their exact sizes, compressed/expanded hashes and measured record counts are in `audit-counts.json`. The source files contain embedded DTDs, separately captured and hashed along with the JMdict entity table. Replays use the locally pinned bytes, not a moving download URL.

According to [EDRDG's dictionary licence](https://www.edrdg.org/edrdg/licence.html), JMdict's Japanese/English components and KANJIDIC2 are CC BY-SA 4.0; the licence also specifies acknowledgements, access to licence documentation and a regular update procedure. KANJIDIC2 incorporates third-party components including the [SKIP system and codes by Jack Halpern](https://www.kanji.org/dictionaries/skip_permission.htm), covered by their own CC BY-SA 4.0 and credit requirements. The project must implement applicable attribution, documentation and update procedures before publication.

No Vietnamese meanings, V1 overrides or semantic mapping are part of this import scope. Selecting these datasets does not resolve contradictory source facts or by itself prove an import valid. The target is a fresh isolated local database until validation; source integrity and attribution remain explicit requirements.
