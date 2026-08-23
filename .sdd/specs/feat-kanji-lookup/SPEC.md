# FEATURE SPEC: Kanji Lookup API
# Version: 0.1.0 | Owner: @lead-dev | Date: 2026-08-23
# Inherits: .sdd/constraints/global.md
# Target Stack: .NET 10 (C# 14) Backend, PostgreSQL 16 (ARCH-005), Docker Testcontainers (ARCH-006)

--------------------------------------------------------------------------------

## 1. Context & Goal

### 1.1 Business Problem

Khi người dùng đọc văn bản tiếng Nhật và gặp kanji không quen, họ cần biết ngay cách đọc, nghĩa và thông tin liên quan của từng ký tự kanji đó mà không phải rời khỏi ngữ cảnh đọc [REQUIREMENT.md §1.2]. Hiện tại Backend đã có Dictionary Lookup (VOC-001–VOC-008), nhưng **chưa có capability nào trả thông tin per-character kanji** — tức là khi user nhìn thấy `食` trong `食べる`, họ chưa có cách tra chi tiết ký tự đó (onyomi, kunyomi, Hán Việt, nét, cấp độ).

### 1.2 Feature Goal

Xây dựng **Kanji Lookup API** trên Backend .NET 10 để:

1. Trích xuất các ký tự kanji unique từ một chuỗi Japanese text đầu vào.
2. Tra cứu từng ký tự kanji trong bảng `kanji_records` (dữ liệu gốc KANJIDIC2).
3. Trả kết quả canonical bao gồm: onyomi, kunyomi, nghĩa tiếng Việt (ưu tiên), nghĩa tiếng Anh, Hán Việt, stroke count, grade, radical, và JLPT level kèm provenance.
4. Phân biệt rõ ràng giữa **giá trị authoritative source** (grade, stroke count — từ KANJIDIC2) và **giá trị derived/approximate** (JLPT level) theo KAN-002.

### 1.3 Relationship to Other Features

Feature này là **backend capability** phục vụ:
- **Unified Analysis** (`POST /api/analysis`) — nhúng kanji information trong response tổng hợp cùng vocabulary, grammar, conjugation [EXT-006, WEB-003].
- **Web Application** — hiển thị kanji detail khi user tra cứu trên Web [WEB-003].

Feature này **KHÔNG** bao gồm: Dictionary Lookup (đã xong), Grammar Detection, Conjugation, Translation, Save Kanji (chưa có requirement cho MVP — `LEARN-002` chỉ target Dictionary Entry, `LEARN-003` chỉ target Grammar Rule).

--------------------------------------------------------------------------------

## Clarifications

### Session 2026-08-23

- Q: API Kanji Lookup riêng biệt nên dùng route HTTP và phương thức nào cho standalone endpoint? → A: `POST /api/kanji/lookup` với JSON body `{ "text": "..." }`.
- Q: Thứ tự các KanjiRecord trả về trong mảng kết quả của API nên sắp xếp theo tiêu chí nào? → A: Sắp xếp theo vị trí xuất hiện đầu tiên (first-occurrence) trong input text.
- Q: Response envelope cho endpoint `POST /api/kanji/lookup` nên theo cấu trúc JSON nào? → A: Envelope bọc field `data`: `{ "data": [...] }`.

--------------------------------------------------------------------------------

## 2. Actors & Roles

| Actor | Mô tả | Quyền hạn | Evidence |
|---|---|---|---|
| **Anonymous Reader** | Người dùng vãng lai trên Extension hoặc Web App | Tra cứu kanji không giới hạn, **SHALL NOT** yêu cầu đăng nhập | AUTH-001 |
| **Authenticated Learner** | Người dùng đã đăng nhập | Cùng quyền tra cứu kanji như Anonymous Reader; MVP không có Save Kanji | AUTH-001, LEARN-002 |
| **Browser Extension** | Client system | Gửi analyzed text chứa kanji qua Unified Analysis hoặc standalone endpoint | EXT-006, BROWSER-001 |
| **Web Application** | Client system | Gửi query qua cùng Backend API | WEB-001, WEB-002, WEB-003 |
| **Backend** | Authoritative system | Trích kanji, tra cứu DB, trả kết quả canonical | ARCH-002 |

> **Quy tắc phân quyền cứng:**
> - Mọi thao tác đọc/tra cứu kanji là **Anonymous** [AUTH-001].
> - Không có thao tác ghi/lưu nào thuộc scope feature này.
> - Rate limiting áp dụng bình đẳng cho cả Guest và Authenticated [RATE-001].

--------------------------------------------------------------------------------

## 3. Functional Requirements (EARS Notation)

### 3.1 Kanji Extraction từ Input Text

*   **EARS[Event]:** WHEN client gửi yêu cầu tra cứu kanji với một chuỗi Japanese text,
    **THE system SHALL** trích xuất tất cả ký tự kanji unique từ input text. Kanji được xác định là các ký tự thuộc CJK Unified Ideographs block (bao gồm supplementary CJK ideographs dùng trong tiếng Nhật, ví dụ `𠮟`) [JPN-001, KAN-001].

*   **EARS[Ubiquitous]:** THE system **SHALL NOT** trả duplicate cho cùng một ký tự kanji xuất hiện nhiều lần trong input. Mảng kết quả các ký tự kanji **SHALL** được sắp xếp theo thứ tự xuất hiện đầu tiên (first occurrence) trong input text.

### 3.2 Kanji Information Lookup

*   **EARS[Event]:** WHEN hệ thống đã trích xuất danh sách kanji unique,
    **THE system SHALL** tra cứu từng ký tự trong bảng `kanji_records` và trả về canonical information [KAN-001].

*   **EARS[Ubiquitous]:** THE system SHALL trả về các thông tin sau cho mỗi kanji tìm thấy:

    | # | Thông tin | Source Property | Evidence | Loại giá trị |
    |---|---|---|---|---|
    | 1 | Canonical ID (`kanji:xxxxx`) | `CanonicalId` | ID-001, ID-005 | Authoritative |
    | 2 | Ký tự kanji | `Character` | KAN-001 | Authoritative |
    | 3 | Onyomi (音読み) | `OnReadings` | KAN-001 | Authoritative (KANJIDIC2) |
    | 4 | Kunyomi (訓読み) | `KunReadings` | KAN-001 | Authoritative (KANJIDIC2) |
    | 5 | Nghĩa tiếng Việt | `MeaningsVi` | KAN-001, I18N-001 | Reviewed/Editorial |
    | 6 | Nghĩa tiếng Anh | `MeaningsEn` | KAN-001 | Authoritative (KANJIDIC2) |
    | 7 | Âm Hán Việt | `HanViet` | KAN-001 | Reviewed/Editorial |
    | 8 | Số nét (stroke count) | `StrokeCount` | KAN-001 | Authoritative (KANJIDIC2) |
    | 9 | Cấp học (grade) | `Grade` | KAN-001 | Authoritative (KANJIDIC2) |
    | 10 | Số bộ thủ (radical) | `RadicalNumber` | KAN-001 | Authoritative (KANJIDIC2) |
    | 11 | JLPT Level | `JlptLevel` | KAN-002 | **Derived/Approximate** |
    | 12 | JLPT Provenance | `JlptProvenance` | KAN-002 | Metadata |
    | 13 | Tần suất sử dụng | `Frequency` | KAN-001 | Source-dependent |
    | 14 | Unicode Codepoint | `UnicodeCodepoint` | KAN-001 | Authoritative |

*   **EARS[Ubiquitous]:** THE system SHALL phân biệt rõ ràng giữa giá trị **authoritative source** (từ KANJIDIC2: grade, stroke count, readings, English meanings) với giá trị **derived/approximate** (JLPT level). Giá trị derived/approximate phải kèm `jlpt_provenance` để client biết đây không phải upstream fact [KAN-002].

### 3.3 Vietnamese-First Content Priority

*   **EARS[Ubiquitous]:** THE system SHALL ưu tiên trả nghĩa tiếng Việt (`MeaningsVi`) khi có dữ liệu. Nghĩa tiếng Anh (`MeaningsEn`) luôn được trả kèm như secondary content khi available [I18N-001, I18N-003].

*   **EARS[Ubiquitous]:** THE system SHALL trả Hán Việt (`HanViet`) khi có dữ liệu — đây là thông tin đặc biệt hữu ích cho người Việt học kanji.

### 3.4 Unrecognized Kanji Handling

*   **EARS[Unwanted]:** WHERE một ký tự kanji trong input không có trong `kanji_records`,
    **THE system SHALL** bỏ qua ký tự đó khỏi kết quả (không trả entry cho nó) mà không gây lỗi toàn bộ request. Response chỉ chứa các kanji đã tìm thấy.

### 3.5 Domain Layer Constraints

*   **EARS[Ubiquitous]:** THE Domain layer **SHALL NOT** import bất kỳ thư viện bên ngoài hoặc ORM nào [AGENTS.md §4.2]. Giao tiếp với database thông qua Domain Interface + Infrastructure Adapter.

*   **EARS[Ubiquitous]:** THE system **SHALL NOT** sử dụng ký tự kanji (`食`) hoặc display text làm primary identity. Mọi tham chiếu phải dùng `canonical_id` dạng `kanji:xxxxx` [ID-003, ID-005].

--------------------------------------------------------------------------------

## 4. Non-Functional Requirements

### 4.1 Performance & Latency

| ID | Requirement | Target | Evidence |
|---|---|---|---|
| NFR-PERF-01 | P95 response time — kanji lookup (backend ↔ DB) | ≤ 100 ms cho ≤ 30 ký tự unique | PERF-002 |
| NFR-PERF-02 | Client timeout | ≤ 5 giây trước explicit failure state | PERF-004 |

> **Lý do target thấp hơn Dictionary Lookup:** Kanji lookup là single-table query trên indexed `character` column, không có JOIN phức tạp như Dictionary Lookup. Input thực tế hiếm khi vượt 30 kanji unique.

### 4.2 Security & Privacy

| ID | Requirement | Evidence |
|---|---|---|
| NFR-SEC-01 | Validate và sanitize mọi input — chặn injection | AGENTS.md §4.1, SEC-003 |
| NFR-SEC-02 | Không lưu raw query text vào system logs mặc định | PRIV-002, PRIV-006 |
| NFR-SEC-03 | Không để lộ stack trace, SQL detail hoặc internal path trong error response | SEC-003, API-004 |
| NFR-SEC-04 | Production phải dùng encrypted transport | SEC-001 |

### 4.3 Cacheability

| ID | Requirement | Evidence |
|---|---|---|
| NFR-CACHE-01 | Kanji data là static/mostly-static — response phù hợp cho caching strategy tương lai | CACHE-001 |

> **Lưu ý:** Feature này không implement caching layer (chờ OD-006). Nhưng response design phải không cản trở caching sau này.

--------------------------------------------------------------------------------

## 5. Data Model

> **Database:** PostgreSQL 16 (ARCH-005) | **ORM:** EF Core + Npgsql (ARCH-007)
> **Schema Reference:** [`docs/database/schema-overview.md`](../../../docs/database/schema-overview.md)
> **Domain Entity:** [`src/domain/Entities/KanjiRecord.cs`](../../../src/domain/Entities/KanjiRecord.cs)

### 5.1 Bảng liên quan trực tiếp (1 bảng)

| Bảng | C# Entity | Vai trò | Key Columns |
|---|---|---|---|
| `kanji_records` | `KanjiRecord` | Canonical record cho mỗi ký tự kanji | `id` (PK), `canonical_id` (unique, format `kanji:xxxxx`), `character` (unique) |

### 5.2 Columns chi tiết

| Column | Type | Nullable | Mô tả | Value Category (KAN-002) |
|---|---|---|---|---|
| `id` | BIGSERIAL | No | Surrogate PK | — |
| `canonical_id` | TEXT | No | Canonical identifier `kanji:xxxxx` | Authoritative |
| `character` | TEXT | No | Ký tự kanji, ví dụ `食` | Authoritative |
| `stroke_count` | SMALLINT | Yes | Số nét từ KANJIDIC2 | Authoritative |
| `grade` | SMALLINT | Yes | Cấp học Nhật Bản | Authoritative |
| `jlpt_level` | SMALLINT | Yes | JLPT level ước lượng | **Derived/Approximate** |
| `jlpt_provenance` | TEXT | Yes | Nguồn gốc giá trị JLPT | Metadata |
| `frequency` | SMALLINT | Yes | Thứ hạng tần suất | Source-dependent |
| `unicode_codepoint` | TEXT | No | Unicode codepoint, ví dụ `U+98DF` | Authoritative |
| `radical_number` | SMALLINT | Yes | Số bộ thủ (1-214) | Authoritative |
| `han_viet` | TEXT | Yes | Âm Hán Việt | Reviewed/Editorial |
| `on_readings` | TEXT[] | Yes | Danh sách onyomi | Authoritative (KANJIDIC2) |
| `kun_readings` | TEXT[] | Yes | Danh sách kunyomi | Authoritative (KANJIDIC2) |
| `meanings_en` | TEXT[] | Yes | Nghĩa tiếng Anh | Authoritative (KANJIDIC2) |
| `meanings_vi` | TEXT[] | Yes | Nghĩa tiếng Việt | Reviewed/Editorial |
| `nanori_readings` | TEXT[] | Yes | Nanori readings | Authoritative (KANJIDIC2) |
| `source_metadata` | JSONB | Yes | Metadata bổ sung từ nguồn | Metadata |
| `created_at` | TIMESTAMPTZ | No | Thời điểm tạo | — |
| `updated_at` | TIMESTAMPTZ | No | Thời điểm cập nhật | — |

### 5.3 Query Strategy

- **Kanji lookup:** Batch query trên `kanji_records.character` với `WHERE character = ANY(@characters)` — single round-trip cho tất cả kanji unique trong input.
- **Index:** Unique index trên `character` column đảm bảo O(1) lookup per character.
- **No JOIN:** Feature này chỉ query 1 bảng, không cần JOIN hoặc eager loading phức tạp.

--------------------------------------------------------------------------------

## 6. Error Handling

### 6.1 Input Validation Errors (HTTP 400)

*   **EARS[Unwanted]:** WHERE input `text` rỗng, chỉ whitespace, hoặc vượt quá giới hạn ký tự (sẽ tuân theo convention chung của analysis API, hiện tại baseline là 1000 ký tự — chờ OD-014 chốt chính thức),
    **THE system SHALL** trả HTTP 400:

    ```json
    {
      "error_code": "VALIDATION_FAILED",
      "message": "Nội dung tra cứu không được trống và không được vượt quá giới hạn cho phép.",
      "request_id": "9fa5e91a-..."
    }
    ```

*   **EARS[Unwanted]:** WHERE input chứa control characters hoặc dấu hiệu injection,
    **THE system SHALL** sanitize input và trả HTTP 400 nếu input không hợp lệ sau sanitize [SEC-003].

### 6.2 No Kanji Found (HTTP 200 — Empty Array)

*   **EARS[Unwanted]:** WHERE input text không chứa bất kỳ ký tự kanji nào, hoặc không có kanji nào trong `kanji_records`,
    **THE system SHALL** trả HTTP 200 với `data: []` — **KHÔNG** trả 404.

### 6.3 Database Failure (HTTP 503)

*   **EARS[Unwanted]:** WHERE kết nối PostgreSQL bị mất hoặc query timeout,
    **THE system SHALL** trả HTTP 503 kèm structured error, **SHALL NOT** leak DB connection string, SQL hoặc stack trace [SEC-003, NET-003]:

    ```json
    {
      "error_code": "SERVICE_UNAVAILABLE",
      "message": "Hệ thống tra cứu đang bận. Vui lòng thử lại sau giây lát.",
      "request_id": "9fa5e91a-...",
      "retryable": true
    }
    ```

### 6.4 Rate Limiting (HTTP 429)

*   **EARS[Unwanted]:** WHERE client vượt ngưỡng rate limit,
    **THE system SHALL** trả HTTP 429 kèm `retry_after_seconds` machine-readable [RATE-001, RATE-002]:

    ```json
    {
      "error_code": "RATE_LIMITED",
      "message": "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.",
      "request_id": "9fa5e91a-...",
      "retryable": true,
      "retry_after_seconds": 30
    }
    ```

### 6.5 Error Contract chung

*   **EARS[Ubiquitous]:** Mọi response HTTP ≥ 400 **SHALL** tuân thủ cấu trúc JSON `{ error_code, message, request_id }` [API-004].
*   **EARS[Ubiquitous]:** Error response **SHALL NOT** chứa stack trace, SQL statement, internal path, hoặc raw selected text [SEC-003, PRIV-002].

--------------------------------------------------------------------------------

## 7. Acceptance Criteria

### Kanji Extraction & Data Completeness

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-001 | Input `text=食べる` | HTTP 200, 1 kanji result cho `食` với `canonical_id` dạng `kanji:xxxxx`, onyomi, kunyomi, meanings |
| AC-002 | Input `text=日本語を勉強する` | HTTP 200, 6 kanji results: `日`, `本`, `語`, `勉`, `強` (unique, `を` và `す`, `る` bị loại vì không phải kanji) |
| AC-003 | Input `text=食食食` (duplicate kanji) | HTTP 200, chỉ 1 kanji result cho `食` — không trả duplicate |
| AC-004 | Input `text=𠮟る` (supplementary CJK ideograph) | HTTP 200, `𠮟` được trích xuất thành công nếu có trong `kanji_records` |

### Value Category & Provenance (KAN-002)

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-005 | Kanji result cho `食` | `stroke_count` và `grade` không có provenance caveat; `jlpt_level` có `jlpt_provenance` field cho biết nguồn gốc |
| AC-006 | Kanji có JLPT level derived | `jlpt_provenance` trả giá trị như `"derived"` hoặc `"approximate"` — không trả `null` khi `jlpt_level` có giá trị |

### Vietnamese-First Content

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-007 | Kanji `食` có cả nghĩa Việt và Anh | `meanings_vi` và `meanings_en` đều present; `han_viet` trả `"THỰC"` hoặc tương đương |
| AC-008 | Kanji chỉ có nghĩa Anh, không có nghĩa Việt | `meanings_en` present; `meanings_vi` là `null` hoặc empty array — không fabricate nghĩa Việt |

### Error Handling & Validation

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-009 | Input rỗng `text=` hoặc chỉ whitespace | HTTP 400, JSON `{ error_code: "VALIDATION_FAILED", ... }` |
| AC-010 | Input chỉ chứa hiragana/katakana (không có kanji) | HTTP 200, `data: []` |
| AC-011 | Database connection lost | HTTP 503, structured error, no stack trace leak |
| AC-012 | Mọi error response ≥ 400 | Chứa `error_code`, `message`, `request_id`; không chứa stack trace |

### Architecture & Security

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-013 | Domain layer zero external imports | `src/domain/` không có `using Microsoft.*`, `using Npgsql.*`, hoặc bất kỳ ORM nào |
| AC-014 | Kanji lookup không yêu cầu authentication | Guest gọi API thành công mà không cần token/session |

### Integration with Unified Analysis (Pre-condition for future)

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-015 | Kanji lookup use case có thể được gọi độc lập từ Application layer | Use case nhận danh sách characters, trả danh sách `KanjiRecord` — không phụ thuộc HTTP context |

--------------------------------------------------------------------------------

## 8. Out of Scope

Các tính năng sau **SHALL NOT** được triển khai trong feature này:

| # | Excluded Feature | Lý do | Reference |
|---|---|---|---|
| 1 | Save Kanji (My Kanji) | MVP `LEARN-002` chỉ target Dictionary Entry; `LEARN-003` chỉ target Grammar Rule. Chưa có requirement cho Save Kanji | REQUIREMENT.md §9.10 |
| 2 | Kanji stroke order animation | Không có requirement | — |
| 3 | Kanji component/radical decomposition (bộ thủ chi tiết) | Chỉ trả `radical_number`; full decomposition ngoài MVP | — |
| 4 | Kanji writing practice | Không có requirement | — |
| 5 | Kanji search by radical/stroke count | Feature tìm kiếm, không phải lookup từ text | — |
| 6 | Caching layer (Redis/Memory) | Chờ OD-006 | — |
| 7 | Dictionary Lookup, Grammar Detection, Translation | Thuộc feature khác | — |
| 8 | Nanori reading display logic | Trả raw data; display priority thuộc client | — |
| 9 | Pagination | Một chuỗi text thực tế hiếm khi có > 50 kanji unique | — |

> **Nguyên tắc:** Nếu có nghi ngờ một tính năng có thuộc scope hay không, mặc định là **OUT**. Chỉ đưa vào khi có requirement ID cụ thể trong `REQUIREMENT.md` và được human approve.

--------------------------------------------------------------------------------

## 9. Assumptions

- `kanji_records` table đã được tạo và có EF Core migration hoạt động (đã xong trong database schema design phase).
- Dữ liệu KANJIDIC2 sẽ được nạp thông qua Knowledge Release pipeline (DATA-001–DATA-005) — feature này xây dựng **read path**, không xây dựng data ingestion.
- Input text sẽ đến từ standalone endpoint hoặc qua Unified Analysis envelope — feature này xây dựng use case layer có thể được gọi từ cả hai.
- Kanji character trong `kanji_records` là unique (1 row per character) theo schema constraint hiện tại.
- Supplementary CJK ideographs (`𠮟` = U+20B9F) cần Unicode-aware extraction logic, không chỉ dựa vào BMP range.

--------------------------------------------------------------------------------

## 10. Open Questions (Chờ Human Decision)

| # | Question | Impact | Status |
|---|---|---|---|
| 1 | **OD-014:** Maximum input length chính thức cho kanji extraction? | Ảnh hưởng validation rule | ⏳ Pending — dùng baseline 1000 ký tự |
| 2 | API route standalone | Route structure | ✅ Resolved: `POST /api/kanji/lookup` với body `{ "text": "..." }` |

--------------------------------------------------------------------------------

## 11. Traceability Matrix

| Requirement ID | Mô tả | Covered by Section |
|---|---|---|
| KAN-001 | Xem canonical information cho từng kanji character | §3.2 |
| KAN-002 | Phân biệt authoritative vs derived/approximate values | §3.2, §5.2 |
| AUTH-001 | Kanji lookup SHALL NOT yêu cầu đăng nhập | §2 |
| ID-001, ID-005 | Canonical ID dạng `kanji:xxxxx` | §3.5 |
| ID-003 | Không dùng display text làm identity | §3.5 |
| I18N-001 | Vietnamese-first content | §3.3 |
| I18N-003, I18N-004 | Multilingual-capable structure | §3.3, §5.2 |
| API-004 | Structured error response | §6.5 |
| SEC-003 | Không leak stack trace | §6.5 |
| PRIV-002, PRIV-006 | Không log raw query text | §4.2 |
| NET-003 | Graceful failure khi DB unavailable | §6.3 |
| RATE-001, RATE-002 | Rate limiting với retry guidance | §6.4 |
| CACHE-001 | Static data phù hợp cache strategy | §4.3 |
| JPN-001 | Unicode-correct kanji extraction (supplementary CJK) | §3.1 |
| ARCH-002 | Backend là authoritative source | §2 |
| AGENTS.md §4.2 | Domain layer zero external imports | §3.5, AC-013 |

--------------------------------------------------------------------------------

## 12. Constitution Alignment

- **Backend authority** (Constitution §2): Kanji lookup là Backend capability, client chỉ tiêu thụ qua shared contract.
- **Stable identity** (Constitution §4): Canonical ID `kanji:xxxxx` giữ ổn định; không dùng ký tự kanji làm primary identity.
- **Authoritative vs derived** (Constitution §4): JLPT level được phân biệt rõ với authoritative source values.
- **Privacy** (Constitution §7): Raw query text không được persist trong logs mặc định.
- **Provider isolation** (Constitution §8): Feature này không có external provider dependency.
- **Spec Kit gates** (Constitution §9): SPEC này surface constitutional impact trước implementation.
