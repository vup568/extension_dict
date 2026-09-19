# TASKS: Unified Analysis API (F-05)

**Feature:** feat-unified-analysis  
**Spec:** [SPEC.md](./SPEC.md) v0.2.0  
**Plan:** [PLAN.md](./PLAN.md) v1.0.0  
**Created:** 2026-09-04  
**Branch:** `feat/grammar-detection`

---

## Phase 1: Application Layer — DTOs, Use Case & DI Registration

> **Dependencies:** None  
> **Priority:** P1  
> **Status:** ✅ Completed

### Task 1.1: Create Request/Response DTOs
- [x] Tạo file `src/usecase/Analysis/Models/AnalysisDtos.cs`
- [x] Define `AnalysisRequestDto(Text, InteractionId?, Context?)`
- [x] Define `CapabilityStatus` static class (`Completed`, `Unavailable`, `Failed`)
- [x] Define `AnalysisCapabilitiesDto(Vocabulary, Kanji, Grammar, Morphology, Conjugation)`
- [x] Define `AnalysisResultDto` envelope chứa:
  - `NormalizedText`
  - `Tokens` (empty list — future morphology)
  - `DictionaryMatches` → reuse `EntryMatchDto` từ F-06
  - `Kanji` → reuse `KanjiDetailDto` từ F-07
  - `Conjugations` (empty list — future F-09)
  - `GrammarOccurrences` → reuse `GrammarOccurrenceDto` từ F-08
  - `Capabilities` map
- [x] Verify: KHÔNG duplicate DTO — chỉ import từ existing namespaces

**Acceptance Criteria:** AC-001 (response structure), AC-006 (capability map)  
**SPEC Reference:** §3.4, §5.2

---

### Task 1.2: Implement `AnalyzeTextUseCase`
- [x] Tạo file `src/usecase/Analysis/AnalyzeTextUseCase.cs`
- [x] Constructor injection: `LookupWordUseCase`, `LookupKanjiUseCase`, `DetectGrammarUseCase`
- [x] **Input Validation:**
  - [x] `text` null/empty/whitespace → throw `ArgumentException` [AC-008]
  - [x] `text.Length > 1000` → throw `ArgumentException` [AC-009]
  - [x] `interactionId` provided nhưng invalid UUID format → throw `ArgumentException` [AC-010]
  - [x] `interactionId` null → chấp nhận, không lỗi [AC-004]
- [x] **Parallel Dispatch via `Task.WhenAll`:**
  - [x] Vocabulary: wrap `LookupWordUseCase.ExecuteAsync(text)` trong try/catch riêng
  - [x] Kanji: wrap `LookupKanjiUseCase.ExecuteAsync(text)` trong try/catch riêng
  - [x] Grammar: wrap `DetectGrammarUseCase.ExecuteAsync(new GrammarDetectionRequestDto(text, context))` trong try/catch riêng [NET-006]
- [x] **Per-capability status mapping:**
  - [x] Use case return thành công → `CapabilityStatus.Completed`
  - [x] Use case throw exception → `CapabilityStatus.Failed`, result = empty list
  - [x] Morphology luôn = `CapabilityStatus.Unavailable`, `tokens = []`
  - [x] Conjugation luôn = `CapabilityStatus.Unavailable`, `conjugations = []`
- [x] **All-failed check:** nếu cả 3 (Vocab + Kanji + Grammar) đều `Failed` → throw `AllCapabilitiesFailedException` [AC-007]
- [x] **Privacy:** KHÔNG log `text` hoặc `context` vào application logs [PRIV-002, AC-011]
- [x] **Translation isolation:** KHÔNG gọi translation endpoint [TRN-004, AC-014]
- [x] Return `AnalysisResultDto` với đầy đủ data + capabilities map

**Acceptance Criteria:** AC-001, AC-004, AC-005, AC-006, AC-006a, AC-006b, AC-007, AC-008, AC-009, AC-010, AC-011, AC-014  
**SPEC Reference:** §3.2, §3.3, §3.5, §3.6

---

### Task 1.3: Create Custom Exception
- [x] Tạo file `src/usecase/Analysis/Exceptions/AllCapabilitiesFailedException.cs`
- [x] Extend `Exception` với message tiếng Việt: _"Tất cả các dịch vụ phân tích đều không khả dụng."_
- [x] Sealed class (immutable)

**Acceptance Criteria:** AC-007  
**SPEC Reference:** §6

---

### Task 1.4: Register in DI Container
- [x] Sửa file `src/usecase/DependencyInjection.cs`
- [x] Thêm `services.AddScoped<AnalyzeTextUseCase>();`
- [x] Thêm `using Application.Analysis;`
- [x] Verify: build thành công sau khi thêm

**Acceptance Criteria:** —  
**SPEC Reference:** —

---

## Phase 2: WebApi Layer — Minimal API Endpoint

> **Dependencies:** Phase 1  
> **Priority:** P1  
> **Status:** ✅ Completed

### Task 2.1: Create `POST /api/analysis` Endpoint
- [x] Tạo file `src/interface/Endpoints/AnalysisEndpoints.cs`
- [x] Tạo static class `AnalysisEndpoints` với extension method `MapAnalysisEndpoints`
- [x] **Request binding:** JSON body → `AnalysisRequestDto`
  - [x] Field mapping: `text` → Text, `interaction_id` → InteractionId, `context` → Context
- [x] **Response envelope:** `{ data: AnalysisResultDto, meta: { request_id, interaction_id, contract_version } }`
  - [x] `request_id` = server-generated UUID [AC-002]
  - [x] `interaction_id` = echo from request (hoặc null) [AC-003, AC-004]
  - [x] `contract_version` = `"1"` [AC-002]
- [x] **Error mapping:**
  - [x] `ArgumentException` → HTTP 400, `error_code: "VALIDATION_FAILED"` [AC-008, AC-009, AC-010, AC-012]
  - [x] `AllCapabilitiesFailedException` → HTTP 503, `error_code: "SERVICE_UNAVAILABLE"` [AC-007, AC-012]
  - [x] Generic `Exception` → HTTP 500, `error_code: "INTERNAL_ERROR"` (không leak stack trace) [AC-012]
- [x] **Error response format:** `{ error_code, message, request_id }` (structured) [AC-012]
- [x] **Authentication:** Không yêu cầu auth [AC-013]
- [x] Follow Minimal API pattern giống `DictionaryEndpoints.cs`

**Acceptance Criteria:** AC-001, AC-002, AC-003, AC-004, AC-007, AC-008, AC-009, AC-010, AC-012, AC-013  
**SPEC Reference:** §3.1, §3.4, §6

---

### Task 2.2: Wire Up in Program.cs
- [x] Sửa file `src/interface/Program.cs`
- [x] Chia sẻ service registration và endpoint mapping qua `src/interface/ApiComposition.cs`
- [x] Production `Program.cs` và integration host dùng cùng composition, gồm `app.MapAnalysisEndpoints()`
- [x] Verify: build thành công, endpoint accessible

**Acceptance Criteria:** —  
**SPEC Reference:** —

---

## Phase 3: Unit Tests & Integration Tests

> **Dependencies:** Phase 1, Phase 2  
> **Priority:** P1  
> **Status:** ✅ Completed

### Task 3.1: Unit Tests — `AnalyzeTextUseCase` Core Logic
- [x] Tạo file `tests/unit/AnalyzeTextUseCaseTests.cs`
- [x] Mock dependencies: `LookupWordUseCase`, `LookupKanjiUseCase`, `DetectGrammarUseCase`
- [x] **Validation tests:**
  - [x] Input text rỗng → `ArgumentException` [AC-008]
  - [x] Input text chỉ whitespace → `ArgumentException` [AC-008]
  - [x] Input text > 1000 ký tự → `ArgumentException` [AC-009]
  - [x] `interaction_id` invalid UUID (e.g. `"abc"`) → `ArgumentException` [AC-010]
  - [x] `interaction_id` null → không lỗi, trả null [AC-004]
  - [x] `interaction_id` valid UUID → echo lại [AC-003]
- [x] **Happy path tests:**
  - [x] Text `食べました` → vocabulary + kanji + grammar results [AC-001]
  - [x] Capabilities map chứa `vocabulary: completed`, `kanji: completed`, `grammar: completed` [AC-001]
- [x] **Partial failure tests:**
  - [x] Vocabulary OK, Kanji throws → `capabilities.kanji = "failed"`, `kanji = []` [AC-005]
  - [x] Grammar throws (sidecar down) → `capabilities.grammar = "failed"`, khác vẫn OK [AC-006b]
  - [x] Vocabulary throws (text > 255) → `capabilities.vocabulary = "failed"`, khác vẫn OK
- [x] **Static capability tests:**
  - [x] Morphology luôn `"unavailable"`, tokens luôn `[]` [AC-006]
  - [x] Conjugation luôn `"unavailable"`, conjugations luôn `[]` [AC-006]
- [x] **All-failed test:**
  - [x] Cả 3 capabilities throw → `AllCapabilitiesFailedException` [AC-007]

**Acceptance Criteria:** AC-001, AC-003, AC-004, AC-005, AC-006, AC-006a, AC-006b, AC-007, AC-008, AC-009, AC-010  
**SPEC Reference:** §7

---

### Task 3.2: Unit Tests — Privacy & Translation Isolation
- [x] Verify: `AnalyzeTextUseCase` không log raw text (code review) [AC-011]
- [x] Verify: không gọi translation service [AC-014]
- [x] Thêm test case nếu cần

**Acceptance Criteria:** AC-011, AC-014  
**SPEC Reference:** §3.6, §3.7

---

### Task 3.3: Integration Tests — End-to-End với Testcontainers
- [x] Tạo file `tests/integration/UnifiedAnalysisIntegrationTests.cs`
- [x] Setup: Kestrel loopback test host + PostgreSQL Testcontainer, liên kết trực tiếp các endpoint production (không thêm package test host) 
- [x] **Happy path E2E:**
  - [x] `POST /api/analysis` body `{ "text": "食べました" }` → HTTP 200
  - [x] Response chứa `data.dictionary_matches` (array)
  - [x] Response chứa `data.kanji` (array)
  - [x] Response chứa `data.grammar_occurrences` (array)
  - [x] Response chứa `data.capabilities` (object)
  - [x] `meta.request_id` non-null [AC-002]
  - [x] `meta.contract_version` = `"1"` [AC-002]
- [x] **Interaction ID echo:**
  - [x] Gửi `interaction_id` → response echo lại [AC-003]
  - [x] Không gửi `interaction_id` → `meta.interaction_id` = null [AC-004]
- [x] **Validation E2E:**
  - [x] Body `{ "text": "" }` → HTTP 400, `error_code: "VALIDATION_FAILED"` [AC-008]
  - [x] Body `{ "text": "<1001 chars>" }` → HTTP 400 [AC-009]
- [x] **No auth required:**
  - [x] Gọi API không có token/session → HTTP 200 [AC-013]

**Acceptance Criteria:** AC-001, AC-002, AC-003, AC-004, AC-008, AC-009, AC-013  
**SPEC Reference:** §7

---

### Task 3.4: Semantic Equivalence Tests
- [x] Cùng file `tests/integration/UnifiedAnalysisIntegrationTests.cs`
- [x] **Vocabulary equivalence:**
  - [x] Gọi `POST /api/analysis` và `GET /api/dictionary/lookup?q=食べました` với cùng input
  - [x] So sánh `dictionary_matches` section → semantic equivalent [AC-015]
- [x] **Kanji equivalence:**
  - [x] Gọi `POST /api/analysis` và `POST /api/kanji/lookup` với cùng input
  - [x] So sánh `kanji` section → semantic equivalent [AC-016]
- [x] **Grammar equivalence:**
  - [x] Gọi `POST /api/analysis` và `POST /api/grammar/detect` với cùng input
  - [x] So sánh `grammar_occurrences` section → semantic equivalent [AC-016a]

**Acceptance Criteria:** AC-015, AC-016, AC-016a  
**SPEC Reference:** §7 (Capability Reuse & Architecture)

---

## Phase 4: Wire-Up, Build Verification & Acceptance

> **Dependencies:** Phase 1, Phase 2, Phase 3  
> **Priority:** P1  
> **Status:** ✅ Completed

### Task 4.1: Build & Test Verification
- [x] `dotnet build --no-restore` → zero errors
- [x] `dotnet test --no-restore` → all unit tests pass (51/51)
- [x] `dotnet test` → integration tests pass (50/50 with Docker Testcontainers, 2026-09-19)
- [x] Verify domain layer: `src/domain/` không có `using Microsoft.*` hoặc `using Npgsql.*` [AC-017]
- [x] Verify no new lint/build warnings

**Acceptance Criteria:** AC-017  
**SPEC Reference:** §7

---

### Task 4.2: Traceability Matrix Verification
- [x] Đối chiếu tất cả 20 AC (AC-001 → AC-017) với tests/code:

> `✅` = đã xác minh bằng code, unit test hoặc integration/E2E test với Docker.

| AC | Mô tả | Task Cover | Status |
|---|---|---|---|
| AC-001 | Full analysis response | T3.1, T3.3 | ✅ |
| AC-002 | `meta.request_id` + `contract_version` | T2.1, T3.3 | ✅ |
| AC-003 | Echo `interaction_id` | T3.1, T3.3 | ✅ |
| AC-004 | Null `interaction_id` OK | T3.1, T3.3 | ✅ |
| AC-005 | Partial result (vocab OK, kanji failed) | T3.1 | ✅ |
| AC-006 | Conjugation/Morphology unavailable | T3.1 | ✅ |
| AC-006a | Grammar completed | T3.1, T3.3 | ✅ |
| AC-006b | Grammar failed (sidecar down) | T3.1 | ✅ |
| AC-007 | All failed → 503 | T3.1, T2.1 | ✅ |
| AC-008 | Empty text → 400 | T3.1, T3.3 | ✅ |
| AC-009 | Text > 1000 → 400 | T3.1, T3.3 | ✅ |
| AC-010 | Invalid UUID → 400 | T3.1, T3.3 | ✅ |
| AC-011 | No raw text in logs | T3.2, T2.1 | ✅ |
| AC-012 | Structured error responses | T2.1, T3.3 | ✅ |
| AC-013 | No auth required | T3.3 | ✅ |
| AC-014 | No auto translation | T3.2, T1.2 | ✅ |
| AC-015 | Vocab semantic equivalence | T3.4 | ✅ |
| AC-016 | Kanji semantic equivalence | T3.4 | ✅ |
| AC-016a | Grammar semantic equivalence | T3.4 | ✅ |
| AC-017 | Domain zero imports | T4.1 | ✅ |

---

## Issues Encountered

- Resolved 2026-09-19: Docker Desktop/Testcontainers integration suite passed (50/50).
- Resolved 2026-09-19: Parallel capabilities initially shared one scoped EF Core `DbContext`, which caused the Kanji capability to degrade incorrectly. `AppDbContext` is transient so each capability use case receives an independent context.
- Resolved 2026-09-19: Grammar context results are filtered to the selected text and offsets remapped before returning them, preserving the response span contract.
- `Microsoft.AspNetCore.Mvc.Testing` không có sẵn trong dependency lock và AGENTS.md cấm tự cài package mới; integration tests dùng Kestrel loopback host và liên kết endpoint production trực tiếp thay cho `WebApplicationFactory`.

---

## Completion Checklist

- [x] Tất cả 12 tasks hoàn thành
- [x] `dotnet build` → zero errors
- [x] `dotnet test` → all pass (51 unit + 50 integration)
- [x] Traceability matrix 20/20 AC covered and verified
- [x] Domain layer zero external imports verified
- [x] Không log raw text / không gọi translation
- [x] Code review passed (context/span finding fixed and re-verified)
- [x] Ready for PR merge
