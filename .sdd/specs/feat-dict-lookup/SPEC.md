# FEATURE SPEC: Dictionary Lookup API
# Version: 1.0.0 | Owner: @lead-dev | Date: 2026-08-22
# Inherits: .sdd/constraints/global.md, .sdd/constraints/business.md, .sdd/constraints/safety.md
# Target Stack: .NET 10 (C# 14) Backend, PostgreSQL 16 (ARCH-005), Docker Testcontainers (ARCH-006)

--------------------------------------------------------------------------------

## 1. Context & Goal

### 1.1 Business Problem

Người học tiếng Nhật khi đọc văn bản thực tế phải tra cứu qua lại giữa nhiều công cụ rời rạc (từ điển, bộ phân tích từ loại, bộ biến đổi từ) gây đứt gãy luồng tập trung [REQUIREMENT.md §1.2]. Phiên bản V1 giải quyết bằng cách nhét toàn bộ dữ liệu từ điển (>100 MB) xuống client (IndexedDB), khiến Extension khởi động chậm và ngốn bộ nhớ [MIGRATION 7.1, 7.3].

### 1.2 Feature Goal

Xây dựng một **API tra cứu từ điển tập trung (Server-Authoritative)** trên Backend .NET 10 [MIGRATION 6.3, 15]:

1. Tra cứu từ vựng tiếng Nhật (JMdict) theo **exact match** trên `written_forms` và `readings`.
2. Trả kết quả **tiếng Việt ưu tiên** (Vietnamese-first), fallback tiếng Anh khi chưa có bản dịch Việt [VOC-004, VOC-005, I18N-003].
3. Bảo toàn **match provenance** — không trộn lẫn nghĩa của form/reading bị giới hạn bởi nguồn dữ liệu gốc [VOC-007, VOC-008].
4. Hỗ trợ **deinflection** qua Tokenizer Sidecar để resolve dạng biến đổi (食べました → 食べる) [CONJ-001, CONJ-002].

### 1.3 Success Metrics

| Metric | Target | Evidence |
|---|---|---|
| P95 Latency (exact lookup, warm) | ≤ 200 ms (backend ↔ DB LAN) | PERF-002 |
| P95 Latency (full analysis incl. sidecar) | ≤ 800 ms | PERF-002, OD-005 |
| Data Coverage | 100% canonical JMdict đã nạp | DATA-003 |
| Provenance Integrity | 0 false sense–form/reading associations | VOC-007, VOC-008 |

### 1.4 Relationship to Other Features

Feature này là **backend core** cho:
- **F-05 Unified Analysis** (`POST /api/analysis`) — nhúng dictionary matches trong response tổng hợp.
- **F-14 Save Vocabulary** — user lưu `canonical_id` từ kết quả lookup.
- **F-11 Web Search** — Web App gọi cùng API.

Feature này **KHÔNG** bao gồm Kanji lookup (F-07), Grammar detection (F-08), Translation (F-10).

--------------------------------------------------------------------------------

## 2. Actors & Roles

| Actor | Mô tả | Quyền hạn | Evidence |
|---|---|---|---|
| **Anonymous Reader** (Guest) | Người dùng vãng lai trên Extension hoặc Web App | Tra cứu từ điển không giới hạn, **SHALL NOT** yêu cầu đăng nhập | AUTH-001, PRIV-001 |
| **Authenticated Learner** | Người dùng đã đăng nhập | Cùng quyền tra cứu như Guest; thêm quyền lưu từ vựng (feature khác F-14) | AUTH-002 |
| **Browser Extension** | Client system | Gửi query qua Unified Analysis hoặc standalone lookup endpoint | EXT-006, BROWSER-001 |
| **Web Application** | Client system | Gửi query qua cùng Backend API | WEB-001, WEB-002 |
| **Tokenizer Sidecar** | External runtime (Go/Python) | Phân tích morphology, trả base form cho deinflection | MIGRATION 6.5, 17, OD-010 |
| **Backend** | Authoritative system | Thực hiện tra cứu DB, điều phối sidecar, trả kết quả | MIGRATION 6.3, 15 |

> **Quy tắc phân quyền cứng:**
> - Mọi thao tác đọc/tra cứu trong feature này là **Anonymous** [AUTH-001].
> - Không có thao tác ghi/lưu nào thuộc scope feature này.
> - Rate limiting áp dụng bình đẳng cho cả Guest và Authenticated [RATE-001].

--------------------------------------------------------------------------------

## 3. Functional Requirements (EARS Notation)

### 3.1 Exact Vocabulary Lookup (Tra cứu theo chuẩn hóa)

*   **EARS[Event]:** WHEN client gửi yêu cầu tra cứu với một từ tiếng Nhật dạng chuẩn hóa (Dictionary Form) qua API,
    **THE system SHALL** tìm kiếm chính xác trong `written_forms.form` và `readings.reading` (C# property: `ReadingText`), trả về danh sách `DictionaryEntry` khớp [VOC-001].

*   **EARS[Ubiquitous]:** THE system SHALL trả về đầy đủ thông tin ngữ nghĩa cho mỗi entry khớp:

    | # | Thông tin | Source Entity / Column | Evidence |
    |---|---|---|---|
    | 1 | Canonical ID (`dictionary:xxxxx`) | `dictionary_entries.canonical_id` | ID-001, ID-005 |
    | 2 | Written Forms (dạng viết Kanji) | `written_forms.form`, `is_common`, `priority`, `info` | VOC-001 |
    | 3 | Readings (dạng đọc Kana) | `readings.reading` (C#: `ReadingText`), `is_common`, `restricted_to_forms` | VOC-003 |
    | 4 | Senses (nghĩa language-neutral) | `dictionary_senses.sense_key`, `part_of_speech`, `position` | VOC-001 |
    | 5 | Localized Glosses (nghĩa đã dịch) | `localized_glosses.gloss_text`, `language_tag` | VOC-004, I18N-003 |
    | 6 | Sense Applicabilities (restrictions) | `sense_applicabilities.written_form_id`, `reading_id` | VOC-007, VOC-008 |

*   **EARS[Ubiquitous]:** THE system SHALL ưu tiên hiển thị nghĩa tiếng Việt (`language_tag = 'vi'`). Nếu một sense chỉ có nghĩa tiếng Anh (`language_tag = 'en'`), hệ thống SHALL fallback sang hiển thị nghĩa Anh [VOC-004, VOC-005].

*   **EARS[Ubiquitous]:** THE system SHALL bảo toàn match provenance — tuyệt đối **SHALL NOT** trộn lẫn nghĩa của sense bị restrict chỉ cho một số form/reading cụ thể [VOC-007, VOC-008]. Client phải nhận đủ dữ liệu `sense_applicabilities` để render đúng.

### 3.2 Deinflection & Morphological Resolution

*   **EARS[Event]:** WHEN client gửi một từ đang ở dạng chia/biến đổi (ví dụ: `食べました`, `高くなかった`),
    **THE system SHALL** gọi Tokenizer Sidecar qua Interface Adapter ở tầng Infrastructure để lấy base form (`食べる`, `高い`) [MIGRATION 6.5, 17], rồi tra cứu từ điển theo base form [CONJ-001].

*   **EARS[Ubiquitous]:** THE system SHALL đồng thời trả về Conjugation Explanation bằng **tiếng Việt ưu tiên** — giải thích chuỗi biến đổi từ observed form về base form [CONJ-002].

*   **EARS[State]:** WHILE Tokenizer Sidecar không khả dụng (timeout, crash),
    **THE system SHALL** trả về partial result chỉ gồm exact match (nếu có), kèm `capabilities.morphology = "unavailable"` [NET-006]. Hệ thống **SHALL NOT** crash hoặc trả 500 chỉ vì sidecar gặp sự cố.

### 3.3 Domain Layer Constraints

*   **EARS[Ubiquitous]:** THE Domain layer **SHALL NOT** import bất kỳ thư viện bên ngoài, ORM (EF Core), hoặc raw tokenizer labels nào [AGENTS.md §4.2, MIGRATION 6.5]. Mọi giao tiếp với Sidecar phải thông qua Domain Interface + Infrastructure Adapter.

*   **EARS[Ubiquitous]:** THE system **SHALL NOT** sử dụng display text (`食べる`, `ăn`) làm primary identity. Mọi tham chiếu phải dùng `canonical_id` dạng `dictionary:xxxxx` [ID-003, ID-005].

--------------------------------------------------------------------------------

## 4. Non-Functional Requirements

### 4.1 Performance & Latency

| ID | Requirement | Target | Evidence |
|---|---|---|---|
| NFR-PERF-01 | P95 response time — exact lookup (backend ↔ DB) | < 200 ms | PERF-002 |
| NFR-PERF-02 | P95 response time — lookup with deinflection (incl. sidecar) | < 800 ms | PERF-002, OD-005 |
| NFR-PERF-03 | Throughput minimum | ≥ 500 RPS, error rate < 0.1% | PERF-001 |
| NFR-PERF-04 | Client timeout | ≤ 5 giây, sau đó trả 504 | PERF-004 |

### 4.2 Security & Privacy

| ID | Requirement | Evidence |
|---|---|---|
| NFR-SEC-01 | Validate và sanitize mọi query input — chặn SQL Injection, XSS, Path Traversal | AGENTS.md §4.1, SEC-004 |
| NFR-SEC-02 | Không lưu raw query text vào system logs mặc định | PRIV-002, PRIV-006 |
| NFR-SEC-03 | Không để lộ stack trace, SQL detail hoặc internal path trong error response | SEC-003, API-004 |
| NFR-SEC-04 | Production phải dùng HTTPS/TLS | SEC-001 |

### 4.3 Extensibility & Provider Isolation

| ID | Requirement | Evidence |
|---|---|---|
| NFR-EXT-01 | Domain layer không tham chiếu raw tokenizer labels — giao tiếp qua normalized interface | MIGRATION 6.5, 17 |
| NFR-EXT-02 | Response phân tách rõ `language_tag` (`vi`, `en`) — sẵn sàng multi-language | I18N-003, I18N-004 |
| NFR-EXT-03 | Tokenizer Sidecar có thể thay đổi (MeCab ↔ Sudachi) mà không sửa Domain/Application | ADR-003 |

### 4.4 Reliability

| ID | Requirement | Evidence |
|---|---|---|
| NFR-REL-01 | Sidecar failure → partial result, không crash toàn bộ | NET-006 |
| NFR-REL-02 | Database connection loss → 503 kèm structured error, không leak internal info | NET-003, NET-005 |
| NFR-REL-03 | Rate limit → 429 kèm `retry_after_seconds` machine-readable | RATE-001, RATE-002 |

--------------------------------------------------------------------------------

## 5. Data Model

> **Database:** PostgreSQL 16 (ARCH-005) | **ORM:** EF Core + Npgsql (ARCH-007)
> **Schema Reference:** [`docs/database/schema-overview.md`](../../../docs/database/schema-overview.md)
> **Domain Entities:** [`src/domain/Entities/`](../../../src/domain/Entities/)

### 5.1 Bảng liên quan trực tiếp (Knowledge Core — 6 bảng)

```
dictionary_entries (1) ──→ (N) written_forms         CASCADE
dictionary_entries (1) ──→ (N) readings               CASCADE
dictionary_entries (1) ──→ (N) dictionary_senses      CASCADE
dictionary_senses  (1) ──→ (N) localized_glosses      CASCADE
dictionary_senses  (1) ──→ (N) sense_applicabilities  CASCADE
```

| Bảng | C# Entity | Vai trò trong Lookup | Key Columns |
|---|---|---|---|
| `dictionary_entries` | `DictionaryEntry` | Bảng trung tâm, mỗi row = 1 mục từ canonical | `id`, `canonical_id` (unique, format `dictionary:xxxxx`) |
| `written_forms` | `WrittenForm` | Dạng viết (Kanji/orthography) của entry | `entry_id` (FK), `form`, `is_common`, `priority`, `info` (TEXT[]) |
| `readings` | `Reading` | Dạng đọc (Kana) của entry | `entry_id` (FK), `reading` (C#: `ReadingText`), `is_common`, `restricted_to_forms` (TEXT[]) |
| `dictionary_senses` | `DictionarySense` | Đơn vị ngữ nghĩa language-neutral | `entry_id` (FK), `sense_key`, `part_of_speech` (TEXT[]), `position` |
| `localized_glosses` | `LocalizedGloss` | Nghĩa đã localize theo language tag | `sense_id` (FK), `language_tag` (`vi`/`en`), `gloss_text`, `source_record_id`, `review_status` |
| `sense_applicabilities` | `SenseApplicability` | Restriction: sense áp dụng cho form/reading nào | `sense_id` (FK), `written_form_id` (FK nullable), `reading_id` (FK nullable) |

### 5.2 Constraints quan trọng

| Constraint | Bảng | Mục đích |
|---|---|---|
| `CHECK language_tag IN ('vi','en')` | `localized_glosses` | Chỉ 2 ngôn ngữ MVP |
| `CHECK written_form_id IS NOT NULL OR reading_id IS NOT NULL` | `sense_applicabilities` | Ít nhất 1 target |
| Unique `canonical_id` | `dictionary_entries` | Định danh toàn cục ổn định |
| Unique `(entry_id, sense_key)` tại application level | `dictionary_senses` | Sense key ổn định trong entry |

### 5.3 Query Strategy

- **Exact lookup:** Index trên `written_forms.form` và `readings.reading_text` → JOIN lên `dictionary_entries` → eager load senses, glosses, applicabilities.
- **Deinflection:** Sidecar trả base form → exact lookup như trên.
- **N+1 Prevention:** Dùng EF Core `.Include()` / `.ThenInclude()` hoặc split query.

--------------------------------------------------------------------------------

## 6. Error Handling

### 6.1 Input Validation Errors (HTTP 400)

*   **EARS[Unwanted]:** WHERE query `q` rỗng, chỉ whitespace, hoặc vượt quá 255 ký tự,
    **THE system SHALL** trả HTTP 400:

    ```json
    {
      "error_code": "VALIDATION_FAILED",
      "message": "Từ khóa tra cứu không được trống và không được vượt quá 255 ký tự.",
      "request_id": "9fa5e91a-..."
    }
    ```

*   **EARS[Unwanted]:** WHERE query chứa ký tự control characters hoặc input có dấu hiệu injection,
    **THE system SHALL** sanitize input trước khi xử lý và trả HTTP 400 nếu input không hợp lệ sau sanitize [SEC-004].

### 6.2 Not Found (HTTP 200 — Empty Array)

*   **EARS[Unwanted]:** WHERE query không khớp bất kỳ entry nào,
    **THE system SHALL** trả HTTP 200 với `data: []` — **KHÔNG** trả 404. Lý do: tránh vỡ popup UI và giảm logic biệt lệ ở Frontend.

### 6.3 Database Failure (HTTP 503)

*   **EARS[Unwanted]:** WHERE kết nối PostgreSQL bị mất hoặc query timeout > 5 giây,
    **THE system SHALL** trả HTTP 503 kèm structured error, ghi log `request_id` kèm exception detail (nội bộ), **SHALL NOT** leak DB connection string, SQL hoặc stack trace ra client [SEC-003, NET-003]:

    ```json
    {
      "error_code": "SERVICE_UNAVAILABLE",
      "message": "Hệ thống tra cứu đang bận. Vui lòng thử lại sau giây lát.",
      "request_id": "9fa5e91a-...",
      "retryable": true
    }
    ```

### 6.4 Sidecar Failure (HTTP 200 — Partial Result)

*   **EARS[Unwanted]:** WHERE Tokenizer Sidecar không phản hồi hoặc trả lỗi khi xử lý deinflection,
    **THE system SHALL** trả HTTP 200 với kết quả exact match (nếu có) và đánh dấu `capabilities.morphology = "unavailable"` [NET-006]. **SHALL NOT** trả 500 chỉ vì sidecar gặp sự cố.

### 6.5 Rate Limiting (HTTP 429)

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

### 6.6 Request Timeout (HTTP 504)

*   **EARS[Unwanted]:** WHERE tổng thời gian xử lý vượt 5 giây,
    **THE system SHALL** trả HTTP 504 kèm structured error [PERF-004].

### 6.7 Error Contract chung

*   **EARS[Ubiquitous]:** Mọi response HTTP ≥ 400 **SHALL** tuân thủ cấu trúc JSON `{ error_code, message, request_id }` [API-004].
*   **EARS[Ubiquitous]:** Error response **SHALL NOT** chứa stack trace, SQL statement, internal path, hoặc raw selected text [SEC-003, PRIV-002].

--------------------------------------------------------------------------------

## 7. Acceptance Criteria

### Exact Match & Data Completeness

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-001 | Query `q=食べる` | HTTP 200, ≥ 1 entry với `canonical_id` dạng `dictionary:xxxxx`, written form `食べる`, reading `たべる` |
| AC-002 | Query `q=日本語` | HTTP 200, entry chứa Kanji `日本語`, reading `にほんご`, và nghĩa tiếng Việt |
| AC-003 | Query `q=猫` — language priority | Nghĩa tiếng Việt (`vi`) xuất hiện trước; nếu sense chỉ có English → fallback `en` |

### Restriction & Provenance Integrity

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-004 | Entry có sense bị restrict cho 1 form/reading cụ thể | Response chứa `sense_applicabilities` đầy đủ; client có thể phân biệt sense nào áp dụng cho form/reading nào |
| AC-005 | Entry có nhiều readings, một số restricted | `restricted_to_forms` xuất hiện đúng trong response; không trộn lẫn |

### Error Handling & Validation

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-006 | Query rỗng `q=` hoặc query > 255 ký tự | HTTP 400, JSON `{ error_code: "VALIDATION_FAILED", ... }` |
| AC-007 | Query không khớp entry nào (ví dụ: `q=asdfghjkl`) | HTTP 200, `data: []` |
| AC-008 | Database connection lost | HTTP 503, structured error, no stack trace leak |
| AC-009 | Mọi error response ≥ 400 | Chứa `error_code`, `message`, `request_id`; không chứa stack trace |

### Deinflection (khi Sidecar khả dụng)

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-010 | Query `q=食べました` | Sidecar resolve → `食べる` → HTTP 200 với entry cho `食べる` kèm conjugation explanation tiếng Việt |
| AC-011 | Sidecar timeout/crash | HTTP 200 partial result, `capabilities.morphology = "unavailable"`, exact match vẫn trả bình thường |

### Architecture & Security

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-012 | Domain layer zero external imports | `src/domain/` không có `using Microsoft.*`, `using Npgsql.*`, hoặc bất kỳ ORM nào |
| AC-013 | Lookup không yêu cầu authentication | Guest gọi API thành công mà không cần token/session |

--------------------------------------------------------------------------------

## 8. Out of Scope

Các tính năng sau **SHALL NOT** được triển khai trong feature này:

| # | Excluded Feature | Lý do | Thuộc Feature |
|---|---|---|---|
| 1 | AutoComplete / Fuzzy Search / Full-Text Search | Ngoài MVP scope, cần thiết kế search engine riêng | — |
| 2 | Save Vocabulary (My Vocabulary) | Yêu cầu Authentication infrastructure | F-14 |
| 3 | Kanji Lookup | Feature riêng biệt, dữ liệu khác (`kanji_records`) | F-07 |
| 4 | Grammar Detection | Feature riêng biệt, cần matcher engine | F-08 |
| 5 | Translation | Feature riêng biệt, cần external provider | F-10 |
| 6 | Offline Cache/Sync xuống Extension | Cấm nhét data xuống client | MIGRATION 7.3 |
| 7 | Wildcard search (`食べ*`) hoặc regex query | Chưa có requirement | — |
| 8 | Pagination cho kết quả lookup | Single query trả ≤ vài chục entries — không cần paginate | — |
| 9 | Caching layer (Redis/Memory) | Cần chốt OD-006 trước | F-22 |
| 10 | Tokenizer Sidecar implementation | Cần chốt OD-010 (Go/Python, MeCab/Sudachi) | Separate deliverable |

> **Nguyên tắc:** Nếu có nghi ngờ một tính năng có thuộc scope hay không, mặc định là **OUT**. Chỉ đưa vào khi có requirement ID cụ thể trong `REQUIREMENT.md` và được human approve.

--------------------------------------------------------------------------------

## 9. Open Questions (Chờ Human Decision)

| # | Question | Impact | Status |
|---|---|---|---|
| 1 | **OD-010:** Tokenizer Sidecar dùng Go hay Python? MeCab hay Sudachi? | Blocker cho §3.2 Deinflection | ⏳ Pending |
| 2 | **OD-005:** Final P95 latency SLO sau benchmark? | Nghiệm thu NFR-PERF-01/02 | ⏳ Pending |
| 3 | **OD-009:** Nguồn dữ liệu nghĩa Việt (FVDP/OVDP) — bản quyền và chất lượng? | Chất lượng `localized_glosses` vi | ⏳ Pending |
| 4 | API route: dùng standalone `/api/dictionary/lookup` hay chỉ embed trong `/api/analysis`? | Quyết định route structure | ⏳ Pending |

--------------------------------------------------------------------------------

## 10. Traceability Matrix

| Requirement ID | Mô tả | Covered by Section |
|---|---|---|
| VOC-001 | Tra cứu từ vựng theo form/reading chuẩn hóa | §3.1 |
| VOC-003 | Trả về reading (Kana) | §3.1 |
| VOC-004 | Vietnamese meaning ưu tiên | §3.1 |
| VOC-005 | English fallback khi thiếu Vietnamese | §3.1 |
| VOC-007 | Bảo toàn match provenance | §3.1 |
| VOC-008 | Không gán sai nghĩa cho restricted form/reading | §3.1 |
| CONJ-001 | Resolve dạng biến đổi về base form | §3.2 |
| CONJ-002 | Giải thích conjugation bằng tiếng Việt | §3.2 |
| AUTH-001 | Tra cứu SHALL NOT yêu cầu đăng nhập | §2 |
| ID-001, ID-005 | Canonical ID dạng `dictionary:xxxxx` | §3.1, §5.1 |
| ID-003 | Không dùng display text làm identity | §3.3 |
| I18N-003, I18N-004 | Language tag phân tách `vi`/`en` | §3.1, §4.3 |
| PERF-002 | P95 latency target | §4.1 |
| PERF-004 | Client timeout ≤ 5s | §4.1, §6.6 |
| API-004 | Structured error response | §6.7 |
| SEC-003 | Không leak stack trace | §6.7 |
| SEC-004 | Input validation/sanitization | §6.1 |
| PRIV-002, PRIV-006 | Không log raw query text | §4.2 |
| NET-003, NET-006 | Graceful degradation khi dependency fail | §3.2, §6.3, §6.4 |
| RATE-001, RATE-002 | Rate limiting với retry guidance | §6.5 |
| DATA-003 | 100% coverage JMdict canonical | §1.3 |
| AGENTS.md §4.2 | Domain layer zero external imports | §3.3, AC-012 |
| MIGRATION 6.5, 17 | Sidecar via Interface Adapter | §3.2, §4.3 |
