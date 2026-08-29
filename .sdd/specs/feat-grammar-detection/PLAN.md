# IMPLEMENTATION PLAN: Grammar Detection API (F-08)

**Version:** 1.0.0 | **Date:** 2026-08-25 | **Spec:** [.sdd/specs/feat-grammar-detection/SPEC.md](./SPEC.md)  
**Target Stack:** .NET 10 (C# 14), SudachiPy (Python 3.11 + gRPC), PostgreSQL 16 (EF Core 9), In-Memory Cache, xUnit

---

## 1. Overview & Architectural Boundaries

Triển khai tính năng **Grammar Detection API** (F-08) cho phép phân tích một chuỗi văn bản tiếng Nhật, thực hiện tokenization thông qua **Sudachi Python Sidecar** qua **gRPC**, sau đó so khớp token stream với các mẫu ngữ pháp (N5-N4) được lưu trữ trong cơ sở dữ liệu và nạp vào bộ nhớ RAM.

### Compliance Checklist (AGENTS.md & Constitution)
- [x] **Zero external dependencies in Domain Layer**: Domain không chứa dependency Sudachi/gRPC.
- [x] **Server-Authoritative**: Backend chịu trách nhiệm điều phối tokenization và grammar matching.
- [x] **Provider Isolation (ARCH-008, ARCH-009)**: Sudachi Sidecar giao tiếp qua gRPC với Adapter độc lập ở Infrastructure layer.
- [x] **Knowledge as Versioned Data (GRM-005)**: Grammar patterns lưu dưới dạng dữ liệu versioned trong database, tách biệt hoàn toàn khỏi matching engine code.
- [x] **Graceful Degradation (NET-006)**: Nếu Sidecar lỗi/timeout, trả capability status `"failed"` mà không crash ứng dụng.
- [x] **Privacy (PRIV-002, PRIV-006)**: Không log raw input text trong Sidecar logs.

---

## 2. Affected Files

### A. New Files to Create

| Layer | File Path | Purpose |
|---|---|---|
| **Contracts** | `sidecars/tokenizer/proto/tokenizer.proto` | Protobuf contract định nghĩa gRPC service `TokenizerService` (`Tokenize` rpc) |
| **Sidecar** | `sidecars/tokenizer/app.py` | Python gRPC Server chạy SudachiPy (`sudachidict_core`) biến đổi text thành Normalized Tokens |
| **Sidecar** | `sidecars/tokenizer/requirements.txt` | Python dependencies (`sudachipy`, `sudachidict_core`, `grpcio`, `grpcio-tools`) |
| **Application** | `src/usecase/Grammar/Ports/IGrammarTokenizerPort.cs` | Interface định nghĩa Hợp đồng Tokenizer cho Grammar Analysis |
| **Application** | `src/usecase/Grammar/Ports/IGrammarRepository.cs` | Interface truy vấn bộ quy tắc ngữ pháp từ DB/Cache |
| **Application** | `src/usecase/Grammar/Models/GrammarDtos.cs` | DTOs (`NormalizedTokenDto`, `GrammarDetectionRequestDto`, `GrammarOccurrenceDto`, `GrammarDetectionResultDto`) |
| **Application** | `src/usecase/Grammar/Helpers/GrammarSequenceMatcher.cs` | Core Token-sequence Matching Engine (sliding window, longest-match priority, overlaps support) |
| **Application** | `src/usecase/Grammar/DetectGrammarUseCase.cs` | Use Case điều phối: Validate text -> Call Sidecar -> Call Matcher -> Format Result |
| **Infrastructure**| `src/infra/Adapters/GrpcGrammarTokenizerAdapter.cs` | gRPC Client implementation kết nối tới Python Sudachi Sidecar |
| **Infrastructure**| `src/infra/Persistence/Repositories/EfGrammarRepository.cs` | EF Core implementation truy vấn `AppDbContext.GrammarRules` |
| **Infrastructure**| `src/infra/Persistence/Repositories/CachedGrammarRepository.cs` | Decorator bọc `IGrammarRepository` để nạp & cache `grammar_rules` trên RAM (Singleton / MemoryCache) |
| **Infrastructure**| `src/infra/Persistence/SeedData/grammar_rules_seed.json` | Seed data 100+ quy tắc ngữ pháp N5-N4 (Hybrid source: jkindrix CC BY-SA 4.0 + V1 Vietnamese meanings) |
| **WebApi** | `src/interface/Endpoints/GrammarEndpoints.cs` | Endpoint `POST /api/grammar/detect` (phục vụ testing/internal) & tích hợp với `UnifiedAnalysis` (F-05) |
| **Unit Tests** | `tests/unit/GrammarSequenceMatcherTests.cs` | Unit tests kiểm thử thuật toán matching: longest match, optional tokens, overlaps, repeated patterns |
| **Unit Tests** | `tests/unit/DetectGrammarUseCaseTests.cs` | Unit tests kiểm thử Use Case: validation, sidecar failure graceful degradation |
| **Integration**| `tests/integration/GrammarDetectionIntegrationTests.cs` | Test E2E với DB PostgreSQL & gRPC Sidecar mock/real |

### B. Existing Files to Modify

| Layer | File Path | Changes Required |
|---|---|---|
| **Infrastructure**| `src/infra/Infrastructure.csproj` | Thêm PackageReference `Grpc.Net.Client`, `Google.Protobuf`, `Grpc.Tools` để compile `.proto` |
| **Application** | `src/usecase/DependencyInjection.cs` | Đăng ký `DetectGrammarUseCase` và `GrammarSequenceMatcher` vào IServiceCollection |
| **Infrastructure**| `src/infra/DependencyInjection.cs` | Đăng ký `IGrammarTokenizerPort` (gRPC), `IGrammarRepository` (Cached Decorator) vào IServiceCollection |
| **WebApi** | `src/interface/Program.cs` | Gọi `app.MapGrammarEndpoints()` để đăng ký routes |

---

## 3. Detailed Step-by-Step Execution Plan

### Phase 1: gRPC Protocol & Python Sudachi Sidecar Setup
1. **Tạo `tokenizer.proto`**:
   - Định nghĩa `TokenizeRequest { string text = 1; string mode = 2; }` (mode A/B/C).
   - Định nghĩa `TokenSpec { string surface = 1; string base_form = 2; string pos = 3; int32 span_start = 4; int32 span_end = 5; }`.
   - Định nghĩa `TokenizeResponse { repeated TokenSpec tokens = 1; }`.
2. **Triển khai Python Sidecar (`sidecars/tokenizer/app.py`)**:
   - Cài đặt `sudachipy` và `sudachidict_core`.
   - Đăng ký `TokenizerServiceServicer` triển khai phương thức `Tokenize`.
   - Trả về danh sách `TokenSpec` chứa `surface`, `dictionary_form()` (`base_form`), normalized `part_of_speech()`, và `begin()`/`end()` char offsets.
   - Bọc logging: Đảm bảo **KHÔNG log raw input text** (PRIV-002).

### Phase 2: Application Layer Definition (Ports, DTOs & Matcher Engine)
1. **Định nghĩa DTOs**:
   - `NormalizedTokenDto(string Surface, string BaseForm, string Pos, int SpanStart, int SpanEnd)`
   - `GrammarOccurrenceDto(string GrammarId, string Pattern, string JlptLevel, string MeaningVi, string MatchedText, TextSpanDto Span)`
   - `GrammarDetectionResultDto(IReadOnlyList<GrammarOccurrenceDto> Occurrences, string Status)`
2. **Định nghĩa Ports**:
   - `IGrammarTokenizerPort`: `Task<IReadOnlyList<NormalizedTokenDto>> TokenizeAsync(string text, CancellationToken ct)`
   - `IGrammarRepository`: `Task<IReadOnlyList<GrammarRule>> GetAllActiveRulesAsync(CancellationToken ct)`
3. **Triển khai `GrammarSequenceMatcher`**:
   - Parse `MatcherMetadata` JSONB thành danh sách matcher rules: `TokenMatcherSpec(string? Surface, string? Base, string? Pos, bool Optional)`.
   - Sử dụng thuật toán Sliding Window duyệt qua chuỗi `NormalizedTokenDto`:
     - Kiểm tra từng vị trí token với danh sách `GrammarRule` trong bộ nhớ.
     - Xử lý các token tùy chọn (`Optional = true`).
     - Ghi nhận tất cả các khớp (occurrences), bảo toàn vị trí `Span` (lấy `SpanStart` của token đầu và `SpanEnd` của token cuối).
     - Đánh thứ tự ưu tiên Longest Match khi xuất hiện pattern chồng lấp nhưng giữ nguyên toàn bộ meaningful overlaps (AC-004).

### Phase 3: Application Use Case (`DetectGrammarUseCase`)
1. Validate `Text` input:
   - Null hoặc WhiteSpace -> Throw `ArgumentException`.
   - Độ dài > 2000 ký tự -> Throw `ArgumentException`.
2. Gọi `IGrammarTokenizerPort.TokenizeAsync(text)`:
   - Catch gRPC Exception / Timeout -> Trả về `GrammarDetectionResultDto([], Status: "failed")` (Graceful degradation - NET-006).
3. Nếu Tokenize thành công:
   - Gọi `IGrammarRepository.GetAllActiveRulesAsync()` (lấy từ RAM Cache).
   - Exec `GrammarSequenceMatcher.Match(tokens, activeRules)`.
   - Map danh sách matched rules sang `GrammarOccurrenceDto`.
   - Trả về `GrammarDetectionResultDto(occurrences, Status: "completed")`.

### Phase 4: Infrastructure Implementation (gRPC Client & Caching Repository)
1. **Triển khai `GrpcGrammarTokenizerAdapter`**:
   - Sử dụng `GrpcChannel` gọi Python Sidecar qua gRPC.
   - Thêm timeout (ví dụ 500ms) để đảm bảo chỉ tiêu NFR-PERF-01.
2. **Triển khai `EfGrammarRepository` & `CachedGrammarRepository`**:
   - `EfGrammarRepository`: Query PostgreSQL `grammar_rules`.
   - `CachedGrammarRepository`: Sử dụng `IMemoryCache` nạp danh sách `GrammarRule` vào RAM với sliding expiration / startup preload.
3. **Soạn thảo `grammar_rules_seed.json`**:
   - Nạp 100+ mẫu ngữ pháp N5-N4 tiêu chuẩn với `matcher_metadata` tương thích Sudachi POS tags.
   - Nạp seed script/migration để đưa vào PostgreSQL `grammar_rules`.

### Phase 5: WebApi & Integration Testing
1. **WebApi Endpoints (`GrammarEndpoints.cs`)**:
   - `POST /api/grammar/detect` nhận `{ text: "..." }`.
   - Trả về response JSON chuẩn envelope: `{ data: result.Occurrences, meta: { status: result.Status } }`.
2. **Unit Tests**:
   - `GrammarSequenceMatcherTests`: Kiểm thử `〜ている`, `〜てはいけない`, repeated patterns, overlapping spans, optional tokens.
   - `DetectGrammarUseCaseTests`: Kiểm thử validation, sidecar unavailable fallback.
3. **Integration Tests**:
   - Chạy test E2E với database PostgreSQL và Sidecar.

---

## 4. Risk Assessment & Mitigations

| Risk | Impact | Likelihood | Mitigation Strategy |
|---|---|---|---|
| **gRPC Sidecar Connection Failure / Latency**: Python process bị sập hoặc phản hồi chậm gây treo request. | High | Medium | Cài đặt strict timeout (500ms) trong gRPC Client. Bọc try-catch `RpcException` để fallback về status `"failed"` mà không crash ứng dụng. |
| **Sudachi POS Tag Mismatch**: Mẫu `matcher_metadata` JSONB viết không đúng với POS tag chuẩn của Sudachi dẫn tới không match được pattern. | High | Medium | Xây dựng bộ Unit Test phong phú trong `GrammarSequenceMatcherTests` với đầy đủ ví dụ thực tế cho 100+ patterns N5-N4 để verify trước khi release. |
| **RAM Cache Stale Data**: Khi admin cập nhật DB `grammar_rules`, Cache trên RAM chưa được xóa. | Low | Low | Trong MVP, cache nạp lúc startup. Khi có tính năng Admin sửa rule (F-12), thêm lệnh invalidate cache. |
| **Privacy Leakage**: Raw input text bị ghi vào file log của Sidecar. | High | Low | Review kỹ mã nguồn `app.py` Python Sidecar, chỉ log log-level, token count và execution time, không log field `text`. |

---

## 5. Requirement Traceability Matrix

| Requirement / AC | Covered by Phase / File | Verification Method |
|---|---|---|
| **GRM-002** (Pattern detection) | Phase 2 & 3 (`GrammarSequenceMatcher`, `DetectGrammarUseCase`) | Unit Test (`AC-001`, `AC-002`) |
| **GRM-005** (Versioned Data in DB) | Phase 4 (`grammar_rules_seed.json`, `CachedGrammarRepository`) | Integration Test (`AC-010`) |
| **GRM-007** (Canonical ID + Span) | Phase 2 (`GrammarSequenceMatcher`) | Unit Test (`AC-001`) |
| **GRM-008** (Repeated & Overlaps) | Phase 2 (`GrammarSequenceMatcher`) | Unit Test (`AC-003`, `AC-004`) |
| **ARCH-008** (Provider Isolation) | Phase 4 (`GrpcGrammarTokenizerAdapter`) | Unit Test (`AC-007`) |
| **NET-006** (Graceful Degradation) | Phase 3 (`DetectGrammarUseCase`) | Unit Test (`AC-006`) |
| **PRIV-002** (No raw text logging) | Phase 1 (`sidecars/tokenizer/app.py`) | Code Review (`AC-013`) |

---

## 6. Approval Gate

> **STOP**: Vui lòng xem xét Kế hoạch Triển khai (Implementation Plan) trên. Hãy cho tôi biết nếu bạn đã **ĐỒNG Ý (APPROVE)** để bắt đầu viết code theo từng Phase!
