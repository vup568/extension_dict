# Validation — Tokenizer Deinflection & Initial F-09

Date: 2026-10-13. Branch: `feat/tokenizer-deinflection`.
Scope: [SPEC](../SPEC.md), [PLAN](../PLAN.md), [TASKS](../TASKS.md).
Implementation and local verification complete; not production corpus approval,
remote CI, deployment or feature merge.

## Verified results

| Gate | Actual result |
|---|---|
| Pinned Docker build | PASS: `jp-reading-tokenizer:verified-20261013` |
| Python Sidecar in read-only/non-root container | 12/12 PASS |
| Compose nlp Sidecar | Running, healthy, localhost50051; user PostgreSQL/volume preserved |
| .NET Release solution | 0 warnings/errors |
| .NET unit suite | 58/58 PASS, no skips |
| .NET full integration suite | 83/83 PASS, no skips, PostgreSQL16 + pinned Sudachi container |
| Live linguistic/PostgreSQL/API slice | 22/22 PASS (included in83) |
| Isolated adapter boundary slice | 10/10 PASS (included in83, not live-provider evidence) |
| Extension typecheck, Unicode, library/browser packages | PASS |
| Extension unit suite | 236/236 PASS |
| Browser regression | 121/121 PASS: Chrome40, Edge40, Brave40, packaged MV3 Brave1 |
| Independent final review | No outstanding P1/P2; misleading footnote corrected |
| Diff whitespace | `git diff --check` clean |

Dependencies pinned to already-smoked versions: Python3.11 image digest,
SudachiPy0.7.0, core20260723.1, grpcio/grpcio-tools1.84.0, protobuf7.36.2,
typing_extensions4.16.0. No .NET/npm dependencies added or upgraded.
Baseline image was smoked before edits; its temporary container was removed
after verification (no volume). Compose Sidecar remains running; user DB unchanged.

## Reproducible commands

From repository root with Docker Engine available:

```powershell
docker build -t jp-reading-tokenizer:verified-20261013 sidecars/tokenizer
docker run --rm --read-only --cap-drop ALL --security-opt no-new-privileges jp-reading-tokenizer:verified-20261013 python -m unittest -v test_tokenizer
docker compose --profile nlp config --quiet
docker compose --profile nlp up -d --build tokenizer
dotnet build JPReadingPlatform.sln --no-restore --configuration Release
dotnet test tests/unit/UnitTests.csproj --no-build --configuration Release
dotnet test tests/integration/IntegrationTests.csproj --no-build --configuration Release
dotnet test tests/unit/UnitTests.csproj --no-build --configuration Release --collect 'Code Coverage' --results-directory tests/TestResults/tokenizer-unit-coverage -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

From `extension/`, existing installed browsers only:

```powershell
npm.cmd run typecheck
npm.cmd run test:coverage
npm.cmd run unicode:verify
npm.cmd run build
$env:BRAVE_EXECUTABLE_PATH = 'C:\Users\ADMIN\AppData\Local\BraveSoftware\Brave-Browser\Application\brave.exe'
npm.cmd run test:browser -- --project=chrome --project=edge --project=brave --project=packaged
```

## Coverage

Existing Microsoft.CodeCoverage18.9.0 is transitive in Test SDK; no collector install.
Cobertura lines grouped by source filename/number, counting covered if any generated
class records hits. Raw artifact ignored under `tests/TestResults/tokenizer-unit-coverage/`.

| Modified Application file | Covered executable lines | Line coverage | >=85% |
|---|---|---|---|
| LookupWordUseCase.cs | 98/100 | 98.00% | PASS |
| AnalyzeTextUseCase.cs | 55/63 | 87.30% | PASS |
| ConjugationMatcher.cs | 28/30 | 93.33% | PASS |

Grammar/Kanji/Publish usecase files: 43/44 (97.73%), 36/36 (100%), 5/5 (100%).
These are line, not branch, thresholds. Extension V8 configured TypeScript scope:
statements96.74%, branches93.98%, functions98.33%, lines98%; excludes browser `.ts`
and React `.tsx`. Popup lifecycle/rendering has browser evidence, not that coverage claim.

## Acceptance traceability

| SPEC AC | Code | Executed evidence |
|---|---|---|
| T01 | Existing Sidecar before edits | Baseline live Docker gRPC9 chains + alternative heads |
| T02 | app.py availability/lock/safe logs | Python12: initialization, concurrency, cancellation, privacy |
| T03 | app.py UTF-16/proto comments | Python supplementary slices; live SupplementalSpansUseUtf16AndNoSelectionTextIsLogged |
| D01 | GrpcDictionaryTokenizerAdapter + LookupWordUseCase | Live13 positive corpus; anonymous Dictionary/Analysis HTTP |
| D02 | Whole-chain matcher/exact-first | Live4 unsupported + exact case, existing exact-skip unit |
| D03 | Linked500ms token/narrowed catch | Adapter10; unit caller cancel + secondary DB failure |
| C01 | Immutable development9 rule documents | Live9 cases + 書いた/読んだ/しました/来ました |
| C02 | Unique pattern/content checks | Unit ambiguity/gaps/unknown/extra/missingVI, adapter missingVI, live unsupported/no-base-entry |
| C03 | JSONB migration/triggers/repository | Live UPDATE/DELETE/TRUNCATE rejected; new pin changes VI without engine edits |
| C04 | Typed DTO/header negotiation | Live default1 empty, opt-in2 typed linked entry, unknown99 structured400 |
| C05 | FE parser/background | Conjugation13 validator cases, unit header2, actual packaged header2 |
| C06 | Safe PopupShell React text | v2 render/segments/footnote/Retry/stale on Chrome/Edge/Brave; prior popup regressions |
| X01 | Provider-isolated data-driven architecture | Final review + full83/58/236/121 regressions/builds |

## Failures encountered and resolved

- First full integration: baseline51 passed, new22 failed. First new case hit EF
  SQL-format parsing of literal `{}`; later cases duplicated seeds because per-test
  initialization wrote into a shared DB. Moved setup into one shared provider/DB
  fixture with deterministic disposal and escaped literal braces. No production
  logic changed for harness issues; fresh live22/full83 passed.
- Adapter test initially failed compile (`Token` vs generated `TokenSpec`);
  corrected test type and reran10. No ignored/disabled test.
- Popup footnote incorrectly said conjugation unavailable despite v2 result;
  split truthful morphology/conjugation wording; dedicated assertion passes.
- Existing null-returning mocks now explicitly return unavailable after narrowing
  catches; not a runtime null fallback or suppression of repository errors.
- Dependency download took about11min; pinned image build completed successfully.
- Final Git audit found broad `data/` ignore hiding the new development dataset on
  Windows; added a narrow exception for `src/infra/Conjugation/Data/*.json` so a fresh
  checkout includes embedded knowledge. Generated dictionary directories stay ignored.

## Remaining boundaries

Opt-in `ConjugationDatasetVersion=development-20261013.1`: immutable development
dataset, not published KnowledgeRelease or production linguistic signoff. Only9
whole-word rules; full F-09, sentence segmentation, generic morphology, passive/
causative/te/na-adjective corpus separate. `tokens=[]`, morphology unavailable.
Firefox runtime not executed; package build is not parity certification. OD-005
performance, F-06 rate-limit/504/control-character/error-path gates and production
data approval remain outside this slice. OD-010 docs committed `c0cb500`; feature
implementation not pushed/merged. Local tests do not certify remote CI.
