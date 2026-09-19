# Implementation Plan: Unified Analysis API (`feat-unified-analysis`)

**Version:** 1.0.0 | **Date:** 2026-09-04  
**Feature:** F-05 Unified Analysis API  
**Spec Reference:** [SPEC.md](./SPEC.md) v0.2.0  
**Target Stack:** .NET 10 (C# 14), PostgreSQL 16 (EF Core 9), xUnit  
**Branch:** `feat/grammar-detection`

---

## Plan Summary

| Phase | Description | Task Count | Dependencies | Priority |
|---|---|---|---|---|
| **Phase 1** | Application Layer — DTOs, Use Case & DI Registration | 4 tasks | None | P1 |
| **Phase 2** | WebApi Layer — Minimal API Endpoint | 2 tasks | Phase 1 | P1 |
| **Phase 3** | Unit Tests & Integration Tests | 4 tasks | Phase 1, Phase 2 | P1 |
| **Phase 4** | Wire-Up, Build Verification & Acceptance | 2 tasks | Phase 1-3 | P1 |

**Total estimated tasks:** 12  
**Estimated effort:** 3-4 hours of focused implementation

---

## Architectural Overview

### Design Principle: Orchestrator Pattern (Thin Coordinator)

Unified Analysis là **orchestrator use case** — nó KHÔNG chứa logic tra cứu riêng mà **delegate** cho các use cases đã có:

```
POST /api/analysis
       │
       ▼
┌──────────────────────────────┐
│   UnifiedAnalysisEndpoint    │   (Interface Layer)
│   - Input validation         │
│   - Structured error mapping │
└──────────┬───────────────────┘
           │
           ▼
┌──────────────────────────────┐
│   AnalyzeTextUseCase         │   (Application Layer — NEW)
│   - Validate text/interaction_id│
│   - Parallel dispatch via    │
│     Task.WhenAll:            │
│     ├─ LookupWordUseCase     │   (F-06 — existing)
│     ├─ LookupKanjiUseCase    │   (F-07 — existing)
│     └─ DetectGrammarUseCase  │   (F-08 — existing)
│   - Catch per-capability     │
│     exceptions → "failed"    │
│   - Map results → response   │
│     envelope with capabilities│
└──────────────────────────────┘
```

### Key Design Decisions

1. **Reuse, don't duplicate.** `AnalyzeTextUseCase` gọi trực tiếp 3 use cases đã có thông qua DI. Không copy logic.
2. **Parallel execution.** 3 capabilities chạy song song qua `Task.WhenAll` vì chúng không phụ thuộc lẫn nhau.
3. **Per-capability try/catch.** Mỗi capability được wrap trong try/catch riêng để 1 lỗi không kéo sập cả analysis.
4. **Grammar dependency.** `DetectGrammarUseCase` phụ thuộc ngầm vào Tokenizer Sidecar (gRPC Sudachi). Khi Sidecar down, use case tự trả `Status: "failed"` — orchestrator chỉ cần map status.
5. **Unavailable capabilities.** Conjugation và Morphology chưa implement → trả `[]` + `"unavailable"`.
6. **interaction_id echo.** Server không generate, chỉ echo client-sent UUID (validate format).
7. **request_id.** Sử dụng `HttpContext.TraceIdentifier` giống pattern hiện có.

---

## Phase 1: Application Layer — DTOs, Use Case & DI Registration

### Mục tiêu
Tạo `AnalyzeTextUseCase` trong Application layer để điều phối song song 3 capabilities (Vocabulary, Kanji, Grammar) và trả response envelope chuẩn hóa.

### Task 1.1: Create Request/Response DTOs
**File tạo mới:** `src/usecase/Analysis/Models/AnalysisDtos.cs`

```csharp
namespace Application.Analysis.Models;

// Request DTO
public sealed record AnalysisRequestDto(
    string Text,
    string? InteractionId = null,
    string? Context = null);

// Capability status enum-like constants
public static class CapabilityStatus
{
    public const string Completed = "completed";
    public const string Unavailable = "unavailable";
    public const string Failed = "failed";
}

// Per-capability status map
public sealed record AnalysisCapabilitiesDto(
    string Vocabulary,
    string Kanji,
    string Grammar,
    string Morphology,
    string Conjugation);

// Full response envelope (data portion)
public sealed record AnalysisResultDto(
    string NormalizedText,
    IReadOnlyList<object> Tokens,                          // Future: morphology
    IReadOnlyList<EntryMatchDto> DictionaryMatches,        // F-06
    IReadOnlyList<KanjiDetailDto> Kanji,                   // F-07
    IReadOnlyList<object> Conjugations,                    // Future: F-09
    IReadOnlyList<GrammarOccurrenceDto> GrammarOccurrences,// F-08
    AnalysisCapabilitiesDto Capabilities);
```

> **Lưu ý:** Reuse `EntryMatchDto` từ F-06, `KanjiDetailDto` từ F-07, `GrammarOccurrenceDto` từ F-08. Không duplicate DTO.

### Task 1.2: Implement `AnalyzeTextUseCase`
**File tạo mới:** `src/usecase/Analysis/AnalyzeTextUseCase.cs`

Logic chính:
1. **Validate input:**
   - `text` null/empty/whitespace → `ArgumentException`
   - `text.Length > 1000` → `ArgumentException` (baseline OD-014)
   - `interactionId` provided nhưng không phải valid UUID → `ArgumentException`
2. **Dispatch 3 capabilities song song** via `Task.WhenAll`:
   - Vocabulary: wrap `LookupWordUseCase.ExecuteAsync(text)` trong try/catch → `completed` hoặc `failed`
   - Kanji: wrap `LookupKanjiUseCase.ExecuteAsync(text)` trong try/catch → `completed` hoặc `failed`
   - Grammar: wrap `DetectGrammarUseCase.ExecuteAsync(new GrammarDetectionRequestDto(text))` trong try/catch → `completed` hoặc `failed`
3. **Map status:** nếu use case throw exception → `"failed"`, kết quả = empty list
4. **Unavailable capabilities:** Morphology = `"unavailable"`, Conjugation = `"unavailable"`, luôn trả `[]`
5. **All-failed check:** nếu cả 3 capabilities đều `"failed"` → throw `AllCapabilitiesFailedException` (custom exception)
6. **Return** `AnalysisResultDto` với full envelope

**Xử lý edge case quan trọng:**
- Vocabulary use case có giới hạn 255 ký tự riêng. Nếu text > 255 chars, vocabulary sẽ throw `ArgumentException` → orchestrator catch và set vocabulary = `"failed"` (hoặc wrap lại gọn hơn: cắt text cho vocabulary riêng).
  - **Quyết định:** Set vocabulary capability = `"failed"` khi text quá dài cho vocabulary, chứ không cắt text — giữ nguyên behavior hiện tại của `LookupWordUseCase`.
- Grammar use case có giới hạn 2000 ký tự. Với text ≤ 1000 (analysis limit), grammar luôn nằm trong giới hạn.

### Task 1.3: Create Custom Exception
**File tạo mới:** `src/usecase/Analysis/Exceptions/AllCapabilitiesFailedException.cs`

```csharp
namespace Application.Analysis.Exceptions;

public sealed class AllCapabilitiesFailedException : Exception
{
    public AllCapabilitiesFailedException()
        : base("Tất cả các dịch vụ phân tích đều không khả dụng.") { }
}
```

### Task 1.4: Register in DI
**File sửa đổi:** `src/usecase/DependencyInjection.cs`

Thêm `services.AddScoped<AnalyzeTextUseCase>();`

---

## Phase 2: WebApi Layer — Minimal API Endpoint

### Mục tiêu
Expose `POST /api/analysis` endpoint theo đúng response contract trong SPEC §3.4.

### Task 2.1: Create Minimal API Endpoint
**File tạo mới:** `src/interface/Endpoints/AnalysisEndpoints.cs`

Pattern tuân theo `DictionaryEndpoints.cs` / `GrammarEndpoints.cs`:

```csharp
POST /api/analysis
```

**Request body:**
```json
{
  "text": "食べました",
  "interaction_id": "uuid-optional",
  "context": "optional-sentence-context"
}
```

**Response mapping:**
- `AnalysisResultDto` thành công → HTTP 200 với envelope `{ data: {...}, meta: { request_id, interaction_id, contract_version } }`
- `ArgumentException` → HTTP 400 `VALIDATION_FAILED`
- `AllCapabilitiesFailedException` → HTTP 503 `SERVICE_UNAVAILABLE` với `retryable: true`
- Unexpected `Exception` → HTTP 500 `INTERNAL_ERROR` (không leak stack trace per SEC-003)

**Privacy (PRIV-002):** KHÔNG log `request.Text` hay `request.Context` vào application logs.

### Task 2.2: Wire Up in Program.cs
**Files sửa đổi:** `src/interface/Program.cs`, `src/interface/ApiComposition.cs`

Đăng ký services và map `app.MapAnalysisEndpoints()` qua composition dùng chung giữa production bootstrap và integration host.

---

## Phase 3: Unit Tests & Integration Tests

### Mục tiêu
Đảm bảo tất cả Acceptance Criteria trong SPEC §7 đều được cover.

### Task 3.1: Unit Tests — `AnalyzeTextUseCase` Core Logic
**File tạo mới:** `tests/unit/AnalyzeTextUseCaseTests.cs`

| Test Case | AC | Expected |
|---|---|---|
| Input text rỗng → `ArgumentException` | AC-008 | Throw |
| Input text > 1000 ký tự → `ArgumentException` | AC-009 | Throw |
| `interaction_id` invalid UUID → `ArgumentException` | AC-010 | Throw |
| `interaction_id` null → không lỗi | AC-004 | Pass |
| Happy path: text = `食べました` → trả vocabulary + kanji + grammar | AC-001 | Full result |
| Vocabulary thành công, Kanji throw → partial result | AC-005 | `kanji = "failed"` |
| Grammar sidecar down → `grammar = "failed"`, khác OK | AC-006b | Partial |
| Conjugation/Morphology luôn `"unavailable"` | AC-006 | Static |
| Tất cả capabilities throw → `AllCapabilitiesFailedException` | AC-007 | Throw |

**Mock strategy:** Dùng real application use cases; mock repository/tokenizer ports bằng NSubstitute để giữ đúng wiring và cô lập side effects.

### Task 3.2: Unit Tests — Interaction ID Validation
**File sửa đổi:** `tests/unit/AnalyzeTextUseCaseTests.cs` (cùng file)

| Test Case | AC | Expected |
|---|---|---|
| Valid UUID interaction_id → echo in response | AC-003 | Matched |
| Null interaction_id → response.InteractionId = null | AC-004 | Null |
| Malformed interaction_id (e.g. "abc") → throw | AC-010 | Throw |

### Task 3.3: Integration Test — End-to-End with Testcontainers
**File tạo mới:** `tests/integration/UnifiedAnalysisIntegrationTests.cs`

Sử dụng Kestrel loopback test host + PostgreSQL Testcontainers. Các endpoint production và composition được liên kết trực tiếp vào test project để giữ nguyên route/service contract mà không cần thêm package `Microsoft.AspNetCore.Mvc.Testing`:
- `POST /api/analysis` với `食べました` → HTTP 200, verify response structure
- Verify `dictionary_matches`, `kanji`, `grammar_occurrences` sections present
- Verify `capabilities` map present
- Verify `meta.request_id` is a server-generated UUID
- Verify `meta.contract_version = "1"`
- Verify error response khi text rỗng → HTTP 400
- Verify malformed JSON, interaction ID validation, partial capability failure, and all-capability 503 response
- Verify `interaction_id` echo, invalid UUID → HTTP 400 và no-auth request
- Tokenizer grammar được thay bằng deterministic fake; database vẫn là PostgreSQL Testcontainer

### Task 3.4: Semantic Equivalence Tests
**File sửa đổi:** `tests/integration/UnifiedAnalysisIntegrationTests.cs` (cùng file)

| Test Case | AC |
|---|---|
| Vocabulary result trong analysis == standalone `GET /api/dictionary/lookup` | AC-015 |
| Kanji result trong analysis == standalone `POST /api/kanji/lookup` | AC-016 |
| Grammar result trong analysis == standalone `POST /api/grammar/detect` | AC-016a |

---

## Phase 4: Wire-Up, Build Verification & Acceptance

### Mục tiêu
Đảm bảo toàn bộ hệ thống build clean, test pass, và traceability matrix đầy đủ.

### Task 4.1: Build & Test Verification
```bash
dotnet build --no-restore
dotnet test --no-restore
```

- Đảm bảo zero build errors
- Đảm bảo tất cả test cases pass (unit + integration khi Docker Desktop khả dụng)
- Hiện tại integration suite đã compile; execution bị chặn bởi Docker Desktop không chạy
- Verify domain layer zero external imports (AC-017)

### Task 4.2: Traceability Matrix & SPEC Alignment
Đối chiếu toàn bộ AC trong SPEC §7 với implementation:

| AC | Mô tả | Covered by |
|---|---|---|
| AC-001 | Full analysis response | Task 3.1, 3.3 |
| AC-002 | `meta.request_id` + `contract_version` | Task 2.1, 3.3 |
| AC-003 | Echo `interaction_id` | Task 3.2 |
| AC-004 | Null `interaction_id` OK | Task 3.2 |
| AC-005 | Partial result (vocab OK, kanji failed) | Task 3.1 |
| AC-006 | Conjugation/Morphology unavailable | Task 3.1 |
| AC-006a | Grammar completed | Task 3.1 |
| AC-006b | Grammar failed (sidecar down) | Task 3.1 |
| AC-007 | All failed → 503 | Task 3.1, 2.1 |
| AC-008 | Empty text → 400 | Task 3.1, 3.3 |
| AC-009 | Text > 1000 → 400 | Task 3.1 |
| AC-010 | Invalid UUID → 400 | Task 3.2 |
| AC-011 | No raw text in logs | Task 2.1 (code review) |
| AC-012 | Error response structured | Task 2.1 |
| AC-013 | No auth required | Task 3.3 |
| AC-014 | No auto translation | Task 1.2 (by design) |
| AC-015 | Vocab semantic equivalence | Task 3.4 |
| AC-016 | Kanji semantic equivalence | Task 3.4 |
| AC-016a | Grammar semantic equivalence | Task 3.4 |
| AC-017 | Domain zero imports | Task 4.1 (build check) |

---

## Risk Assessment

| Risk | Impact | Mitigation |
|---|---|---|
| Vocabulary throws cho text dài (> 255 chars) | Vocabulary section = `"failed"` trong analysis | Chấp nhận: Vocabulary designed cho từ đơn. Khi Tokenizer sẵn sàng sẽ tách tokens trước |
| Grammar Tokenizer Sidecar down | Grammar = `"failed"` | Graceful degradation đã built-in F-08. Orchestrator chỉ map status |
| `Task.WhenAll` — 1 task chậm kéo cả batch | Analysis response chậm | Hiện tại chấp nhận. Future: add per-capability timeout (CancellationTokenSource) |
| Response envelope khác biệt giữa analysis và standalone endpoints | Client confusion | Task 3.4 semantic equivalence tests đảm bảo nhất quán |

---

## Files Summary

### Files tạo mới (Create)
1. `src/usecase/Analysis/Models/AnalysisDtos.cs`
2. `src/usecase/Analysis/AnalyzeTextUseCase.cs`
3. `src/usecase/Analysis/Exceptions/AllCapabilitiesFailedException.cs`
4. `src/interface/Endpoints/AnalysisEndpoints.cs`
5. `tests/unit/AnalyzeTextUseCaseTests.cs`
6. `tests/integration/UnifiedAnalysisIntegrationTests.cs`

### Files sửa đổi (Modify)
1. `src/usecase/DependencyInjection.cs` — thêm `AnalyzeTextUseCase`
2. `src/interface/Program.cs`, `src/interface/ApiComposition.cs` — shared service registration và endpoint mapping
3. `tests/integration/IntegrationTests.csproj` — framework reference và endpoint links cho HTTP integration host
