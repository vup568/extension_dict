# Japanese–English local import execution

User changed scope to JMdict Japanese–English and KANJIDIC2 and requested local import and extension testing. Database target: `jp_dict_real_jaen_20261030_v3`; run `jaen_20261030_v3`. Neither `.env` nor the connection to `app_db` was changed.

- Source lock SHA-256: `d28e0fe72c47c90ee997cd24d7a44d8982e46571c251ac45615e5476b8c3640c`.
- Stored canonical facts SHA-256: `168480cd1fd390f37fcbf4d2d30dccd24496175ae4916eed1084ac69501728e7`.
- Source counts and database counts: 218,914 JMdict entries; 13,108 KANJIDIC2 characters; 232,022 source records; 443,736 English glosses.
- The backend replayed and verified the imported source records and canonical facts before listening, verified the configured conjugation dataset, and initialized and checked the five existing grammar seed rules.
- The packed MV3 extension passed its live-browser smoke test for 食べる, 食べました → 食べる, 橋, 箸 and kanji 食 against this database, using an installed Brave browser. This is not manual Chrome acceptance and does not prove all forms or words work.
- Regression evidence: 56 focused .NET integration tests passed after the batch fix; 107 .NET unit tests and 254 extension unit tests passed; extension typecheck and build passed.

The JMdict snapshot's `1339160` sense 2 has an internally incompatible written-form/reading restriction. The source record and both restrictions are retained; lookup does not present an impossible pairing. This exception is limited to the exact artifact hash, entry, sense and source restrictions. No automatic editorial correction was made.

The earlier `jaen_20261030_v2` import was interrupted at 65,000 JMdict records. It remains unserved and was not deleted. A new target and lock were used for the bounded-memory writer so its provenance is not silently changed. An earlier empty prepared target `jp_dict_real_jaen_20261030` also remains. The original `app_db` remained at zero vocabulary records, zero kanji and five grammar rules.

The import report's `ready_for_local_use` remains false because its legacy full-release gate includes independent review and release/cutover criteria; local startup explicitly validates the imported facts and is not a production release. Runtime validation is a point-in-time check: the database is not sealed read-only, and the candidate should not be used for personal learning data. Attribution and update requirements remain to be implemented before publishing the application. See `docs/local-japanese-english-dictionary.md`.
