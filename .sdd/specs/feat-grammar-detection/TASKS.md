# Task Checklist: Grammar Detection API (`feat-grammar-detection`)

**Version:** 1.0.0 | **Date:** 2026-08-25  
**Target Feature:** `feat-grammar-detection` (F-08 Grammar Detection API)  
**Spec Reference:** [SPEC.md](./SPEC.md) | **Plan Reference:** [PLAN.md](./PLAN.md)  
**Target Stack:** .NET 10 (C# 14), SudachiPy (Python 3.11 + gRPC), PostgreSQL 16 (EF Core 9), In-Memory Cache, xUnit

---

## 📋 Task Summary

| Phase | Description | Task Count | Status |
|---|---|---|---|
| **Phase 1** | gRPC Protocol & Python Sudachi Sidecar Setup | 3 tasks | ✅ Completed |
| **Phase 2** | Application Layer Core Abstractions & Matcher Engine | 4 tasks | ✅ Completed |
| **Phase 3** | Application Use Case (`DetectGrammarUseCase`) | 3 tasks | ✅ Completed |
| **Phase 4** | Infrastructure Implementation (gRPC Client, Caching & Seed Data) | 6 tasks | ✅ Completed |
| **Phase 5** | WebApi Layer & Integration Testing | 3 tasks | ✅ Completed |
| **Phase 6** | Matcher Engine Hardening & Seed Data Fix | 4 tasks | ✅ Completed |

---

## 🐍 Phase 1: gRPC Protocol & Python Sudachi Sidecar Setup

- [x] **Task 1.1: Create Protobuf Service Definition**
  - File: `sidecars/tokenizer/proto/tokenizer.proto`
  - Details:
    - Define `TokenizerService` with `Tokenize(TokenizeRequest)` RPC returning `TokenizeResponse`.
    - Define `TokenizeRequest` (`text`, `mode` e.g. "A", "B", "C").
    - Define `TokenSpec` (`surface`, `base_form`, `pos`, `span_start`, `span_end`).

- [x] **Task 1.2: Implement Python gRPC Sudachi Sidecar Server**
  - File: `sidecars/tokenizer/app.py`
  - File: `sidecars/tokenizer/requirements.txt`
  - Details:
    - Install `sudachipy`, `sudachidict_core`, `grpcio`, `grpcio-tools`.
    - Implement `TokenizerServiceServicer` to parse Japanese text into normalized tokens with POS tags and character offsets.
    - Validate privacy requirement (PRIV-002): **Do not log raw input text** in stdout/stderr or log files.

- [x] **Task 1.3: Setup Sidecar Dockerfile & Execution Script**
  - File: `sidecars/tokenizer/Dockerfile`
  - Details:
    - Configure Python 3.11 lightweight image with gRPC server listening on port 50051.

---

## 🧠 Phase 2: Application Layer Core Abstractions & Matcher Engine

- [x] **Task 2.1: Create Application DTOs**
  - File: `src/usecase/Grammar/Models/GrammarDtos.cs`
  - Details:
    - `NormalizedTokenDto(string Surface, string BaseForm, string Pos, int SpanStart, int SpanEnd)`
    - `GrammarDetectionRequestDto(string Text)`
    - `GrammarOccurrenceDto(string GrammarId, string Pattern, string JlptLevel, string MeaningVi, string MatchedText, TextSpanDto Span)`
    - `GrammarDetectionResultDto(IReadOnlyList<GrammarOccurrenceDto> Occurrences, string Status)`

- [x] **Task 2.2: Create Domain/Application Ports**
  - File: `src/usecase/Grammar/Ports/IGrammarTokenizerPort.cs`
  - File: `src/usecase/Grammar/Ports/IGrammarRepository.cs`
  - Details:
    - `IGrammarTokenizerPort`: `Task<IReadOnlyList<NormalizedTokenDto>> TokenizeAsync(string text, CancellationToken ct)`.
    - `IGrammarRepository`: `Task<IReadOnlyList<GrammarRule>> GetAllActiveRulesAsync(CancellationToken ct)`.

- [x] **Task 2.3: Implement Token-Sequence Matching Engine**
  - File: `src/usecase/Grammar/Helpers/GrammarSequenceMatcher.cs`
  - Details:
    - Parse `matcher_metadata` JSONB into structured token specs (`Surface`, `Base`, `Pos`, `Optional`).
    - Support Hierarchical / Wildcard POS tag matching (prefix match on Sudachi 6-level POS array).
    - Implement sliding window sequence matching on `NormalizedTokenDto` stream without token skipping.
    - Preserve all overlapping pattern occurrences across token positions, sorting by longest-match priority per span.
    - Calculate exact character spans (`SpanStart` to `SpanEnd`).

- [x] **Task 2.4: Unit Tests for Token-Sequence Matcher**
  - File: `tests/unit/GrammarSequenceMatcherTests.cs`
  - Details:
    - Test standard pattern matching (`〜ている`, `〜てはいけない`).
    - Test repeated patterns in a single text.
    - Test overlapping spans and optional token branches.

---

## ⚙️ Phase 3: Application Use Case (`DetectGrammarUseCase`)

- [x] **Task 3.1: Implement `DetectGrammarUseCase`**
  - File: `src/usecase/Grammar/DetectGrammarUseCase.cs`
  - Details:
    - Validate input `Text` (null/empty/whitespace or length > 2000 -> throw `ArgumentException`).
    - Call `IGrammarTokenizerPort.TokenizeAsync()`.
    - Handle sidecar failure / gRPC exception gracefully: return `GrammarDetectionResultDto([], Status: "failed")` without crashing (NET-006).
    - Load rules from `IGrammarRepository` and execute `GrammarSequenceMatcher`.
    - Map matches to `GrammarOccurrenceDto` and return `Status: "completed"`.

- [x] **Task 3.2: Register Application Services**
  - File: `src/usecase/DependencyInjection.cs`
  - Details:
    - Register `DetectGrammarUseCase` and `GrammarSequenceMatcher` into `IServiceCollection`.

- [x] **Task 3.3: Unit Tests for Use Case & Graceful Degradation**
  - File: `tests/unit/DetectGrammarUseCaseTests.cs`
  - Details:
    - Test input validation (empty text, >2000 chars).
    - Test sidecar timeout / gRPC error fallback behavior (`Status: "failed"`).
    - Test successful match mapping.

---

## 📦 Phase 4: Infrastructure Implementation (gRPC Client, Caching & Seed Data)

- [x] **Task 4.1: Configure Infrastructure gRPC Dependencies & Protobuf Compilation**
  - File: `src/infra/Infrastructure.csproj`
  - Details:
    - Add `Grpc.Net.Client`, `Google.Protobuf`, `Grpc.Tools` NuGet packages.
    - Configure `<Protobuf Include="..\..\sidecars\tokenizer\proto\tokenizer.proto" GrpcServices="Client" />`.

- [x] **Task 4.2: Implement gRPC Tokenizer Adapter**
  - File: `src/infra/Adapters/GrpcGrammarTokenizerAdapter.cs`
  - Details:
    - Register GrpcChannel as Singleton / Managed HttpClient in DI to reuse connection pooling.
    - Implement `IGrammarTokenizerPort` connecting to Python sidecar via gRPC channel.
    - Set 500ms strict timeout (NFR-PERF-01).

- [x] **Task 4.3: Implement EF Core Repository**
  - File: `src/infra/Persistence/Repositories/EfGrammarRepository.cs`
  - Details:
    - Query active rules from `AppDbContext.GrammarRules` with `AsNoTracking()`.

- [x] **Task 4.4: Implement In-Memory RAM Caching Decorator**
  - File: `src/infra/Persistence/Repositories/CachedGrammarRepository.cs`
  - Details:
    - Wrap `IGrammarRepository` using `IMemoryCache` to cache active rules in RAM for fast sequence matching.

- [x] **Task 4.5: Create Seed Data for N5-N4 Grammar Rules**
  - File: `src/infra/Persistence/SeedData/grammar_rules_seed.json`
  - Details:
    - Draft 100+ standard N5-N4 grammar rules with `matcher_metadata` compatible with Sudachi POS tags.
    - Add database seeder/migration script to populate PostgreSQL `grammar_rules`.

- [x] **Task 4.6: Register Infrastructure Services**
  - File: `src/infra/DependencyInjection.cs`
  - Details:
    - Register `IGrammarTokenizerPort` -> `GrpcGrammarTokenizerAdapter`.
    - Register `IGrammarRepository` -> `CachedGrammarRepository` (decorating `EfGrammarRepository`).

---

## 🌐 Phase 5: WebApi Layer & Integration Testing

- [x] **Task 5.1: Create Minimal API Endpoint**
  - File: `src/interface/Endpoints/GrammarEndpoints.cs`
  - Details:
    - Route: `POST /api/grammar/detect`.
    - Accept `GrammarDetectionRequestDto`.
    - Standard response envelope: `{ data: result.Occurrences, meta: { status: result.Status, contract_version: "1" } }`.
    - Error mapping: `ArgumentException` -> 400 Bad Request (`VALIDATION_FAILED`).

- [x] **Task 5.2: Wire Up WebApi `Program.cs`**
  - File: `src/interface/Program.cs`
  - Details:
    - Register `app.MapGrammarEndpoints()`.

- [x] **Task 5.3: Integration Testing & Verification**
  - File: `tests/integration/GrammarDetectionIntegrationTests.cs`
  - Details:
    - E2E test with PostgreSQL Testcontainers & gRPC sidecar mock/real.
    - Test AC-001 (Pattern detection `〜てはいけない`).
    - Test AC-003 (Repeated pattern occurrence).
    - Test AC-006 (Graceful degradation on sidecar failure).
    - Verify `dotnet build` and `dotnet test`.

---

## 🔧 Phase 6: Matcher Engine Hardening & Seed Data Fix

> **Nguyên nhân:** Postman test phát hiện 2/5 mẫu ngữ pháp không được phát hiện do:
> 1. `〜てはいけない`: Matcher chỉ khớp `surface:"て"` nhưng bỏ sót biến thể `で` (nhóm động từ ぶ/む/ぬ).
> 2. `〜ないで`: Matcher giả định `ないで` là 1 token, nhưng Sudachi tách thành 2 token `ない` + `で`.

- [x] **Task 6.1: Hỗ trợ toán tử OR (`|`) trong Matcher Engine**
  - File: `src/usecase/Grammar/Helpers/GrammarSequenceMatcher.cs`
  - Details:
    - Cập nhật method `IsMatch()` để hỗ trợ cú pháp `|` (pipe) trong trường `Surface` và `Base` của `TokenMatcherSpec`.
    - Khi `spec.Surface` chứa ký tự `|` (ví dụ `"て|で"`), split thành mảng `["て", "で"]` và kiểm tra `token.Surface` khớp **bất kỳ** phần tử nào.
    - Tương tự cho `spec.Base` nếu chứa `|`.
    - Logic so khớp hiện tại (exact match, case-insensitive) vẫn giữ nguyên cho các trường hợp không có `|`.

- [x] **Task 6.2: Cập nhật Seed Data JSON**
  - File: `src/infra/Persistence/SeedData/grammar_rules_seed.json`
  - Details:
    - `〜てはいけない`: Đổi matcher_metadata thành `[{"surface":"て|で"},{"surface":"は"},{"base":"いけない"}]`.
    - `〜ないで`: Đổi matcher_metadata thành `[{"base":"ない"},{"surface":"で"}]`.
    - Đồng thời cập nhật `〜てから`: Đổi thành `[{"surface":"て|で"},{"surface":"から"}]` (cùng lý do biến thể て/で).

- [x] **Task 6.3: Cập nhật Fallback Seed Data trong Repository**
  - File: `src/infra/Persistence/Repositories/EfGrammarRepository.cs`
  - Details:
    - Đồng bộ matcher_metadata trong phần fallback hard-coded seed khớp với file JSON ở Task 6.2.
    - Đảm bảo nhất quán giữa 2 nguồn seed data.

- [x] **Task 6.4: Cập nhật dữ liệu đã tồn tại trong PostgreSQL**
  - Lệnh: `docker exec` chạy `UPDATE grammar_rules SET matcher_metadata = ...` cho 3 dòng bị ảnh hưởng.
  - Details:
    - UPDATE `grammar:n4:te-wa-ikenai` matcher_metadata → `[{"surface":"て|で"},{"surface":"は"},{"base":"いけない"}]`.
    - UPDATE `grammar:n5:nai-de` matcher_metadata → `[{"base":"ない"},{"surface":"で"}]`.
    - UPDATE `grammar:n5:te-kara` matcher_metadata → `[{"surface":"て|で"},{"surface":"から"}]`.
    - Verify bằng `SELECT canonical_id, matcher_metadata FROM grammar_rules;`.
