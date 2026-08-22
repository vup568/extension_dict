# Task Checklist: Dictionary Lookup API (`feat-dict-lookup`)

**Version:** 1.0.0 | **Date:** 2026-08-22  
**Target Feature:** `feat-dict-lookup` (F-06 Vocabulary Lookup API)  
**Spec Reference:** [SPEC.md](./SPEC.md) | **Plan Reference:** [PLAN.md](./PLAN.md)

---

## 📋 Task Summary

| Phase | Description | Task Count | Status |
|---|---|---|---|
| **Phase 1** | Application Layer Core Abstractions & Use Case | 5 tasks | ✅ Completed |
| **Phase 2** | Infrastructure Implementation (EF Core & Sidecar) | 3 tasks | ✅ Completed |
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

- [x] **Task 2.2: Implement Fallback Tokenizer Adapter (`NullTokenizerAdapter`)**
  - File: `src/infra/Adapters/NullTokenizerAdapter.cs`
  - Details:
    - Return `DeinflectionResult.Empty` to allow standalone execution without a running sidecar container.

- [x] **Task 2.3: Create Infrastructure Extension Registration**
  - File: `src/infra/DependencyInjection.cs`
  - Details:
    - Register `IDictionaryRepository` -> `EfDictionaryRepository` and `ITokenizerAdapter` -> `NullTokenizerAdapter`.

---

## 🌐 Phase 3: WebApi Layer & Endpoint Registration

- [x] **Task 3.1: Create Minimal API Endpoint**
  - File: `src/interface/Endpoints/DictionaryEndpoints.cs`
  - Details:
    - Route: `GET /api/v1/dictionary/lookup` & `GET /api/dictionary/lookup`
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
    - Test AC-001 (Exact Match with `日本語`).
    - Test AC-002 (Language Priority `vi` > `en`).
    - Test AC-003 (Empty result for unknown word).
    - Test AC-004 (Validation error for invalid query).
    - Test AC-005 (Restriction provenance integrity).

- [x] **Task 4.2: Final Verification & Solution Build**
  - Command: `dotnet build` -> 0 errors, 0 warnings.
  - Command: `dotnet test tests/unit/UnitTests.csproj` -> 14/14 unit tests pass.
