# Task Checklist: Dictionary Lookup API (`feat-dict-lookup`)

**Version:** 1.1.0 | **Updated:** 2026-10-12
**Target Feature:** `feat-dict-lookup` (F-06 Vocabulary Lookup API)  
**Spec Reference:** [SPEC.md](./SPEC.md) | **Plan Reference:** [PLAN.md](./PLAN.md)

---

## 📋 Task Summary

| Phase | Description | Task Count | Status |
|---|---|---|---|
| **Phase 1** | Application Layer Core Abstractions & Use Case | 5 tasks | ✅ Completed |
| **Phase 2** | Infrastructure Implementation (EF Core & Sidecar) | 4 tasks | ⚠️ Fallback complete; production Sidecar pending OD-010 |
| **Phase 3** | WebApi Layer & Endpoint Registration | 3 tasks | ✅ Completed |
| **Phase 4** | Integration Testing & Verification | 2 tasks | ✅ Completed |

---

## 🚀 Phase 1: Application Layer Core Abstractions & Use Case

- [x] **Task 1.1: Create Ports (Repository & Tokenizer Interfaces)**
  - File: `src/usecase/Dictionary/Ports/IDictionaryRepository.cs`
  - File: `src/usecase/Dictionary/Ports/ITokenizerAdapter.cs`
  - Details:
    - `IDictionaryRepository`: `SearchByFormOrReadingAsync(string query, CancellationToken ct)`, `GetByCanonicalIdsAsync(IEnumerable<string> canonicalIds, CancellationToken ct)`.
    - `ITokenizerAdapter`: `DeinflectAsync(string surfaceText, CancellationToken ct)`.

- [x] **Task 1.2: Create DTOs (Data Transfer Objects)**
  - File: `src/usecase/Dictionary/Models/LookupResultDto.cs`
  - File: `src/usecase/Dictionary/Models/EntryMatchDto.cs`
  - File: `src/usecase/Dictionary/Models/SenseDto.cs`
  - File: `src/usecase/Dictionary/Models/DeinflectionResult.cs`
  - Details:
    - Include `CanonicalId`, `MatchedWrittenForm`, `MatchedReading`, `Senses`, `WrittenForms`, `Readings`.
    - Filter/Sort glosses to put Vietnamese (`vi`) glosses first, fallback to English (`en`).

- [x] **Task 1.3: Create Use Case (`LookupWordUseCase`)**
  - File: `src/usecase/Dictionary/LookupWordUseCase.cs`
  - Details:
    - Validate query `q` (null/empty/whitespace/length > 255 -> throw `ArgumentException`).
    - Attempt exact match search via `IDictionaryRepository`.
    - If exact match is empty, call `ITokenizerAdapter.DeinflectAsync()`.
    - Perform secondary search if base form is resolved.
    - Assemble DTOs preserving `sense_applicabilities` (restrictions) and Vietnamese-first priority.

- [x] **Task 1.4: Create Application Layer Extension Registration**
  - File: `src/usecase/DependencyInjection.cs`
  - Details:
    - Add `AddApplicationServices(this IServiceCollection services)` method registering `LookupWordUseCase`.

- [x] **Task 1.5: Unit Tests for Use Case & DTO Mapping**
  - File: `tests/unit/LookupWordUseCaseTests.cs`
  - Details:
    - Test validation rules (empty query, >255 chars).
    - Test Vietnamese priority and English fallback.
    - Test graceful degradation when sidecar fails.

---

## 📦 Phase 2: Infrastructure Implementation (EF Core & Sidecar)

- [x] **Task 2.1: Implement EF Core Repository (`EfDictionaryRepository`)**
  - File: `src/infra/Persistence/Repositories/EfDictionaryRepository.cs`
  - Details:
    - Query `AppDbContext.DictionaryEntries` with eager loading (`Include`/`ThenInclude`) for `WrittenForms`, `Readings`, `Senses`, `Glosses` (on Sense), and `Applicabilities` (on Sense).
    - Ensure queries use indexes on `written_forms.form` (`idx_written_forms_form`) and `readings.reading` (`idx_readings_reading`).

- [x] **Task 2.2a: Implement unavailable-provider adapter (`NullTokenizerAdapter`)**
  - File: `src/infra/Adapters/NullTokenizerAdapter.cs`
  - Details:
    - Return an explicit unavailable result so exact lookup remains usable without falsely claiming morphology completed.

- [ ] **Task 2.2b: Implement production Tokenizer Sidecar adapter**
  - Blocked by `OD-010` (Go/Python and MeCab/Sudachi selection).
  - Required for production AC-010 deinflection; not part of the current verified exact-lookup slice.

- [x] **Task 2.3: Create Infrastructure Extension Registration**
  - File: `src/infra/DependencyInjection.cs`
  - Details:
    - Register `IDictionaryRepository` -> `EfDictionaryRepository` and `ITokenizerAdapter` -> `NullTokenizerAdapter`.

---

## 🌐 Phase 3: WebApi Layer & Endpoint Registration

- [x] **Task 3.1: Create Minimal API Endpoint**
  - File: `src/interface/Endpoints/DictionaryEndpoints.cs`
  - Details:
    - Route: `GET /api/dictionary/lookup`
    - Map parameters `[FromQuery] string q`.

- [x] **Task 3.2: Create Response Envelope & Structured Error Handler**
  - File: `src/interface/Endpoints/DictionaryEndpoints.cs`
  - Details:
    - Map `ArgumentException` -> 400 Bad Request `{ "error_code": "VALIDATION_FAILED", "message": "...", "request_id": "..." }`.
    - Map `NpgsqlException` -> 503 Service Unavailable `{ "error_code": "SERVICE_UNAVAILABLE", ... }`.
    - Map unexpected errors -> 500 Internal Server Error (without leaking stack trace).

- [x] **Task 3.3: Wire Up `Program.cs`**
  - File: `src/interface/Program.cs`
  - Details:
    - Call `builder.Services.AddApplicationServices()` and `builder.Services.AddInfrastructureServices()`.
    - Map `DictionaryEndpoints`.

---

## 🧪 Phase 4: Integration Testing & Verification

- [x] **Task 4.1: Create Integration Test Suite**
  - File: `tests/integration/DictionaryLookupIntegrationTests.cs`
  - Details:
    - Test SPEC AC-002 exact match with `日本語` and VOC-001 exact lookup by kana reading.
    - Test SPEC AC-003 language priority `vi` > `en`.
    - Test SPEC AC-007 empty result plus AC-011 unavailable-provider capability.
    - Test SPEC AC-006 use-case validation for invalid query.
    - Test SPEC AC-004 restriction provenance integrity.

- [x] **Task 4.2: Final Verification & Solution Build**
  - 2026-10-12 Release build: 0 errors, 0 warnings.
  - Full unit suite: 53/53; F-06-focused unit slice: 13/13.
  - Full PostgreSQL Testcontainers suite: 51/51; F-06-focused integration slice: 6/6.
  - Evidence: [evidence/validation-report.md](./evidence/validation-report.md).
