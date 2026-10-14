# F-09 — Backend validation, 2026-10-14

## Summary

Candidate `candidate-20261013.1`, approved corpus `f09-expanded-20261013.3`. Usecases remain provider-neutral; new data installed only in isolated PostgreSQL16 Testcontainers. No user DB migration.

## Commands and results

From repository root; dependencies/tools already installed, no restore/install:

```powershell
dotnet build JPReadingPlatform.sln --no-restore --configuration Release --verbosity minimal -m:1 -nr:false -p:UseSharedCompilation=false
dotnet test tests/unit/UnitTests.csproj --no-build --configuration Release --collect 'Code Coverage' --logger 'trx;LogFileName=f09-unit.trx' --results-directory tests/TestResults/f09-final-unit -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
dotnet test tests/integration/IntegrationTests.csproj --no-build --configuration Release --logger 'trx;LogFileName=f09-final-integration.trx' --results-directory tests/TestResults/f09-20261014
docker run --rm --read-only --cap-drop ALL --security-opt no-new-privileges jp-reading-tokenizer:verified-20261013 python -m unittest -v test_tokenizer
```

- Release build:0 warnings/0 errors.
- Unit:79/79,0 failed/skipped.
- Integration:118/118,0 failed/skipped;full result in `tests/TestResults/f09-20261014/f09-final-integration.trx`.
- Sidecar Python:12/12,0 failures.
- Runtime image pinned `sha256:14ece806bfac24843e05a79a1c1c899a929621ad0d26ff4c02bf4c894585a1f9` (no new image/packages); real gRPC,not mocked for linguistic corpus.

## Corpus and boundaries

`ConjugationCorpusIntegrationTests` exercises404 non-scenario cases (303 supported,10 exact,91 unavailable), using real SQL and live gRPC with explicit synthetic dictionary availability. Expected never generated from actual. All alternative IDs/versions/base/VI/ordered segment surfaces/component VI/exact UTF16 bounds match an independently approved realization. Duplicate accepted phrasings are matched as a whole,not mixed segment-by-segment.

Seven explicit scenarios MIS-01/ERR-01/ERR-02/DATA-01/VI-01/DB-01/CANCEL-01 execute the candidate path in isolated infrastructure tests:missing canonical base,provider down,500ms cancellation,dataset absent,VI missing,DB failure,caller cancellation. ASYNC-01 executes through real browser pending-v3 tests; total412 mapped corpus cases. Analysis unit verifies candidate DB failure yields truthful failed capability and preserves other sections.

Additional live checks cover10 unlisted roots (all9 regular godan rows+ichidan;30 surfaces),irregular v5r-i negative,anonymous real HTTP1/2/3 for singular/2/3 meanings,interaction echo and actual HTTP JSON parsed by production TS client. Old suites cover unknown version400,default1,exact-first,sidecar cancellation/privacy and old dataset behavior.

New migration `20261014022755_AddConjugationCandidate` only INSERTs into existing JSONB table;Down throws. Historical seed/resource/migration hashes unchanged. Both old/new source content are JSONB-semantic-equal;UPDATE/DELETE/TRUNCATE remain rejected. Upgrade regression now uses one fresh test DB per scenario,not rollback through an immutable dataset install.

## Coverage

Existing Microsoft.CodeCoverage,unique source lines aggregated across async state machines/generated classes:

| Modified source | Covered/total | Line coverage |
|---|---|---|
| LookupWordUseCase.cs |143/146|97.95%|
| AnalyzeTextUseCase.cs |56/64|87.50%|
| ConjugationParadigmMatcher.cs |40/40|100%|

Both modified usecases exceed85%. Source XML: `tests/TestResults/f09-final-unit/5dc818b0-8820-4c57-a6f2-e0164003b4e2/ADMIN_VULAP_2026-10-14.09_49_17.cobertura.xml` (ignored machine artifact).

## Failures and resolutions

No failure hidden: initial full suite failed because shared legacy upgrade fixture rolled back through forward-only migration;fixed isolation. Applicability regressions red→green fixed joint form/reading checks. Corpus harness span path and Node transform flag corrected without changing expected. Independent review P1 early class dedupe was reproduced red,then fixed by retaining class-specific realizations until canonical verification. Live 帰られる asserts passive/honorific with reviewed godan components (`帰ら/れ/る`,derived from V11-P2);its initial new assertion incorrectly assumed2 segments and was corrected from approved V11-P2,not actual-as-oracle. Fresh full118 and79units pass;frozen expected unchanged,no skips.

## Remaining gates

Independent technical review and PO G3 runtime acceptance;development pin is not production publication/F-20. Performance/OD-005 and general morphology remain outside this slice.
