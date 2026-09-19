# IMPLEMENTATION PLAN: Kanji Lookup API (F-07)

**Version:** 1.0.0 | **Date:** 2026-08-23 | **Spec:** [.sdd/specs/feat-kanji-lookup/SPEC.md](./SPEC.md)
**Target Stack:** .NET 10 (C# 14), PostgreSQL 16 (ARCH-005), EF Core 9 (ARCH-007), xUnit + Testcontainers

---

## 1. Overview & Architectural Boundaries

Triển khai tính năng **Kanji Lookup API** (`POST /api/kanji/lookup`) cho phép trích xuất các ký tự Kanji từ một chuỗi văn bản tiếng Nhật và tra cứu thông tin chi tiết (bộ thủ, số nét, âm Hán Việt, Onyomi, Kunyomi, JLPT level) từ cơ sở dữ liệu PostgreSQL (`kanji_records`).

### Compliance Checklist (AGENTS.md & Constitution)
- [x] **Zero external dependencies in Domain Layer**: Chỉ sử dụng POCO `KanjiRecord.cs` hiện có.
- [x] **Server-Authoritative**: Backend chịu trách nhiệm trích xuất Kanji & query DB.
- [x] **Match Provenance & Value Classification**: Phân biệt rõ ràng Authoritative values (Grade, StrokeCount) với Derived/Approximate values (JlptLevel, JlptProvenance).
- [x] **Clean Architecture (4 Layer)**: Domain -> Application -> Infrastructure -> WebApi.

---

## 2. Affected Files

### A. New Files to Create

| Layer | File Path | Purpose |
|---|---|---|
| **Application** | `src/usecase/Kanji/Ports/IKanjiRepository.cs` | Interface định nghĩa Hợp đồng truy vấn dữ liệu Kanji cho Repository |
| **Application** | `src/usecase/Kanji/Models/KanjiDtos.cs` | Các Data Transfer Objects (`KanjiLookupRequestDto`, `KanjiDetailDto`, `KanjiLookupResultDto`) |
| **Application** | `src/usecase/Kanji/Helpers/KanjiExtractor.cs` | Helper trích xuất các ký tự CJK Unified Ideographs unique từ input text, hỗ trợ Unicode Surrogate Pairs (supplementary CJK) & bảo toàn first-occurrence order |
| **Application** | `src/usecase/Kanji/LookupKanjiUseCase.cs` | Use Case chính điều phối validate, extract kanji, query DB, map sang DTOs |
| **Infrastructure**| `src/infra/Persistence/Repositories/EfKanjiRepository.cs` | EF Core implementation truy vấn `AppDbContext.KanjiRecords` không theo dõi trạng thái (`AsNoTracking`) |
| **WebApi** | `src/interface/Endpoints/KanjiEndpoints.cs` | Minimal API endpoint `POST /api/kanji/lookup` với error handling chuẩn |
| **Unit Tests** | `tests/unit/LookupKanjiUseCaseTests.cs` | Test suite kiểm tra Use Case: Unicode extraction, validation, ordering, empty result, duplicate kanji |
| **Integration**| `tests/integration/KanjiLookupIntegrationTests.cs` | Test suite kiểm tra End-to-End API với PostgreSQL Docker Testcontainers |

### B. Existing Files to Modify

| Layer | File Path | Changes Required |
|---|---|---|
| **Application** | `src/usecase/DependencyInjection.cs` | Đăng ký `LookupKanjiUseCase` vào IServiceCollection |
| **Infrastructure**| `src/infra/DependencyInjection.cs` | Đăng ký `IKanjiRepository` -> `EfKanjiRepository` vào IServiceCollection |
| **WebApi** | `src/interface/Program.cs` | Gọi `app.MapKanjiEndpoints()` để đăng ký các routes mới |

---

## 3. Detailed Step-by-Step Execution Plan

### Phase 1: Application Layer Definition (Ports, DTOs & Extractor)
1. **Define Repository Port**:
   - `IKanjiRepository`: Định nghĩa phương thức `Task<IReadOnlyList<KanjiRecord>> GetByCharactersAsync(IEnumerable<string> characters, CancellationToken cancellationToken = default)`.
2. **Define DTOs**:
   - `KanjiLookupRequestDto(string Text)`
   - `KanjiDetailDto`: Chứa đầy đủ thông tin kanji canonical (CanonicalId, Character, StrokeCount, Grade, JlptLevel, JlptProvenance, Frequency, UnicodeCodepoint, RadicalNumber, HanViet, OnReadings, KunReadings, MeaningsVi, MeaningsEn, NanoriReadings).
   - `KanjiLookupResultDto`: Bọc mảng `IReadOnlyList<KanjiDetailDto> Data`.
3. **Implement `KanjiExtractor`**:
   - Duyệt qua chuỗi UTF-16, nhận diện CJK Ideographs (bao gồm cả CJK Unified Ideographs `U+4E00`..`U+9FFF`, CJK Extension A..F và Surrogate Pairs `U+20000`..`U+2A6DF` như `𠮟`).
   - Sử dụng `HashSet<string>` hoặc `List<string>` để đảm bảo:
     - **Unique**: Không lặp lại ký tự kanji trùng nhau (ví dụ: `食食食` -> `食`).
     - **Ordering**: Giữ nguyên thứ tự xuất hiện đầu tiên (first occurrence) trong input text.

### Phase 2: Application Use Case (`LookupKanjiUseCase`)
1. Validate `Text` input:
   - Null hoặc WhiteSpace -> Throw `ArgumentException("Nội dung tra cứu không được trống.")`.
   - Độ dài > 1000 ký tự -> Throw `ArgumentException("Nội dung tra cứu không được vượt quá 1000 ký tự.")`.
2. Gọi `KanjiExtractor.ExtractUniqueKanji(text)` -> Lấy danh sách kanji unique.
3. Nếu danh sách rỗng (input chỉ chứa Hiragana/Katakana/English) -> Trả về `KanjiLookupResultDto` với `Data = []`.
4. Gọi `IKanjiRepository.GetByCharactersAsync(uniqueKanjiList, cancellationToken)`.
5. Map danh sách `KanjiRecord` thành `KanjiDetailDto`:
   - Phân loại rõ ràng Authoritative vs Derived (`jlpt_provenance` không null khi `jlpt_level` có giá trị).
   - Sắp xếp mảng trả về theo đúng thứ tự `uniqueKanjiList` thu được ở bước 2.
6. Trả về `KanjiLookupResultDto`.

### Phase 3: Infrastructure Layer Implementation (`EfKanjiRepository`)
1. Triển khai `EfKanjiRepository` kế thừa `IKanjiRepository`:
   - Sử dụng `AppDbContext.KanjiRecords.AsNoTracking()`
   - Lọc bằng `.Where(k => charList.Contains(k.Character))`
   - Trả về danh sách `KanjiRecord` bất đồng bộ.

### Phase 4: WebApi Layer Endpoints (`KanjiEndpoints`)
1. Tạo `KanjiEndpoints.cs` với extension method `MapKanjiEndpoints(this IEndpointRouteBuilder app)`:
   - Map route `POST /api/kanji/lookup`.
   - Bọc response thành công trong format standard: `{ data: result.Data, meta: { request_id, contract_version: "1" } }`.
   - Bọc exception handlers:
     - `ArgumentException` -> HTTP 400 (`VALIDATION_FAILED`)
     - `NpgsqlException` -> HTTP 503 (`SERVICE_UNAVAILABLE`)
     - `Exception` -> HTTP 500 (`INTERNAL_ERROR`)
2. Đăng ký trong `DependencyInjection.cs` và `Program.cs`.

### Phase 5: Verification & Quality Assurance (Testing)
1. **Unit Tests** (`tests/unit/LookupKanjiUseCaseTests.cs`):
   - Test extraction CJK tiêu chuẩn (`日本語` -> `日`, `本`, `語`).
   - Test supplementary CJK surrogate pair (`𠮟る` -> `𠮟`).
   - Test duplicate kanji (`食食食` -> 1 result `食`).
   - Test preserve first occurrence order (`勉強` -> `勉` trước `強`).
   - Test validation errors (empty string, whitespace, >1000 chars).
   - Test empty state (hiragana/katakana input -> empty data array).
2. **Integration Tests** (`tests/integration/KanjiLookupIntegrationTests.cs`):
   - Chạy thử nghiệm với database PostgreSQL thật qua `PostgreSqlFixture`.
   - Seed dữ liệu mẫu cho kanji `食`, `日`, `本`.
   - Verify HTTP status code (200, 400, 503), response envelope và JSON structure.

---

## 4. Risk Assessment & Mitigations

| Risk | Impact | Likelihood | Mitigation Strategy |
|---|---|---|---|
| **Unicode Surrogate Pair Extraction Bug**: Trích xuất sai các ký tự Kanji mở rộng CJK Extension (ví dụ `𠮟` U+20B9F gồm 2 UTF-16 code units) làm vỡ ký tự. | High | Medium | Sử dụng `Char.IsHighSurrogate` và `Char.ConvertToUtf32` trong `KanjiExtractor` để kiểm tra chuẩn dải CJK ideographs đầy đủ (BMP + Supplementary planes). Viết Unit Test riêng cho case này. |
| **Ordering Loss during DB Query**: `WHERE character = ANY(...)` trong PostgreSQL không bảo đảm thứ tự kết quả trả về khớp với mảng input. | Medium | High | Sau khi query DB lấy `Dictionary<string, KanjiRecord>`, map lại kết quả theo thứ tự mảng `uniqueKanjiList` đã extract ban đầu. |
| **N+1 or Heavy Queries**: Batch query gây chậm khi text quá dài. | Low | Low | Input được giới hạn ≤ 1000 ký tự. Bảng `kanji_records` có Unique Index trên cột `character` giúp O(1) B-Tree lookup per kanji. Query duy nhất 1 lần bằng `Contains()`. |
| **Stacktrace/DB Error Leakage**: Exception leak thông tin nhạy cảm. | High | Low | Sử dụng try-catch bọc quanh Endpoint Handler, chỉ trả về JSON `{ error_code, message, request_id }` theo tiêu chuẩn API-004. |

---

## 5. Requirement Traceability Matrix

| Requirement / AC | Covered by Phase / File | Verification Method |
|---|---|---|
| **KAN-001** (Kanji canonical info) | Phase 1 & 2 (`KanjiDetailDto`, `LookupKanjiUseCase`) | Unit Test & Integration Test |
| **KAN-002** (Authoritative vs Derived JLPT) | Phase 1 & 2 (`KanjiDetailDto.JlptProvenance`) | Unit Test (`AC-005`, `AC-006`) |
| **JPN-001** (Supplementary CJK Ideographs) | Phase 1 (`KanjiExtractor`) | Unit Test (`AC-004`) |
| **AUTH-001** (Anonymous access) | Phase 4 (`KanjiEndpoints`) | Integration Test (`AC-014`) |
| **API-004** (Structured error response) | Phase 4 (`KanjiEndpoints`) | Integration Test (`AC-012`) |
| **First Occurrence Order** | Phase 1 & 2 (`KanjiExtractor`, `LookupKanjiUseCase`) | Unit Test & Integration Test |

---

## 6. Approval Gate

> **STOP**: Vui lòng xem xét Kế hoạch Triển khai trên. Hãy cho tôi biết nếu bạn đã **ĐỒNG Ý (APPROVE)** để bắt đầu viết code theo từng Phase!
