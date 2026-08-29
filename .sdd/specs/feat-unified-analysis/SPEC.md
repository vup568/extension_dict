# FEATURE SPEC: Unified Analysis API
# Version: 0.1.0 | Owner: @lead-dev | Date: 2026-08-24
# Inherits: .sdd/constraints/global.md
# Target Stack: .NET 10 (C# 14) Backend, PostgreSQL 16 (ARCH-005), Docker Testcontainers (ARCH-006)

--------------------------------------------------------------------------------

## 1. Context & Goal

### 1.1 Business Problem

Khi người dùng bôi chọn văn bản tiếng Nhật trên trang web hoặc nhập text trên Web App, họ cần nhận **toàn bộ phân tích ngôn ngữ** (từ vựng, kanji, ngữ pháp, biến đổi từ) trong **một lần tương tác duy nhất** thay vì phải gọi nhiều endpoint riêng lẻ [REQUIREMENT.md §1.2]. Hiện tại Backend đã có các capability riêng lẻ — Dictionary Lookup (`POST /api/dictionary/lookup`) và Kanji Lookup (`POST /api/kanji/lookup`) — nhưng **chưa có endpoint hợp nhất** để client gửi một request và nhận kết quả tổng hợp.

Việc bắt buộc client gọi nhiều request song song gây ra:
- Tăng độ phức tạp xử lý async ở client (nhiều promise, nhiều error state).
- Không có cơ chế chuẩn để biểu diễn **partial result** — khi một capability thất bại, client không biết capability nào thành công.
- Khó duy trì **interaction identity** nhất quán xuyên suốt toàn bộ phân tích [ASYNC-001].

### 1.2 Feature Goal

Xây dựng **Unified Analysis API** (`POST /api/analysis`) trên Backend .NET 10 để:

1. Nhận một chuỗi Japanese text đầu vào và **điều phối** tất cả analysis capabilities đã có (vocabulary lookup, kanji lookup) cùng các capabilities tương lai (morphology, conjugation, grammar detection) trong **một request duy nhất**.
2. Trả về kết quả tổng hợp trong **một response envelope** chứa tất cả sections: `dictionary_matches`, `kanji`, `conjugations`, `grammar_occurrences`, `tokens`.
3. Hỗ trợ **partial result**: khi một capability gặp lỗi (ví dụ grammar engine unavailable), các capabilities khác vẫn trả kết quả thành công — response phải **chỉ rõ** capability nào `completed`, `unavailable` hoặc `failed` [NET-006].
4. Mang **interaction identity** (`interaction_id`) để client xác định response nào là current [ASYNC-001, ASYNC-002].
5. **KHÔNG** tự động gọi translation — translation chỉ được kích hoạt sau explicit user action [TRN-004].

### 1.3 Relationship to Other Features

Feature này là **orchestrator endpoint** gom các capabilities đã có:
- **F-06 (Dictionary Lookup)** — `LookupWordUseCase` cung cấp `dictionary_matches`.
- **F-07 (Kanji Lookup)** — `LookupKanjiUseCase` cung cấp `kanji`.
- **F-08 (Grammar Detection)** — tương lai, cung cấp `grammar_occurrences`.
- **F-09 (Conjugation)** — tương lai, cung cấp `conjugations`.

Feature này **KHÔNG** bao gồm:
- Translation (F-10) — gọi riêng qua `POST /api/translations` sau explicit user action.
- Standalone Dictionary/Kanji endpoints — vẫn tồn tại song song, không bị thay thế.
- Grammar Detection logic (F-08) — chỉ gọi use case khi available.
- Save/Learning features (F-14, F-15).

### 1.4 Extensibility Design

Unified Analysis **phải** được thiết kế để **thêm capability mới mà không thay đổi contract structure**:
- Khi F-08 (Grammar) chưa được implement, `grammar_occurrences` trả `[]` và `capabilities.grammar` trả `"unavailable"`.
- Khi F-09 (Conjugation) chưa được implement, `conjugations` trả `[]` và `capabilities.conjugation` trả `"unavailable"`.
- Khi Tokenizer Sidecar (ARCH-003) chưa được triển khai, `tokens` trả `[]` và `capabilities.morphology` trả `"unavailable"`.

Điều này cho phép **ship Unified Analysis ngay** với vocabulary + kanji, rồi bổ sung grammar/conjugation/morphology sau mà không breaking change.

--------------------------------------------------------------------------------

## Clarifications

### Session 2026-08-24

- Q: Khi user bôi chọn một câu dài (ví dụ `日本語を勉強する`), vocabulary lookup sẽ xử lý như thế nào khi chưa có tokenizer tách từ? → A: **Phương án A** — gửi nguyên `text` vào `LookupWordUseCase`. Vocabulary hoạt động cho **từ đơn** (exact match + deinflect), trả rỗng cho câu dài. Đây là giới hạn được chấp nhận cho đến khi Tokenizer Sidecar (OD-010) sẵn sàng. Khi có tokenizer, analysis sẽ tách câu thành tokens rồi lookup từng từ — không breaking change.

--------------------------------------------------------------------------------

## 2. Actors & Roles

| Actor | Mô tả | Quyền hạn | Evidence |
|---|---|---|---|
| **Anonymous Reader** | Người dùng vãng lai trên Extension hoặc Web App | Gọi analysis không giới hạn, **SHALL NOT** yêu cầu đăng nhập | AUTH-001 |
| **Authenticated Learner** | Người dùng đã đăng nhập | Cùng quyền analysis như Anonymous Reader | AUTH-001 |
| **Browser Extension** | Client system | Gửi unified analysis request cho text được bôi chọn | EXT-006, BROWSER-001 |
| **Web Application** | Client system | Gửi unified analysis request cho text nhập bởi user | WEB-001, WEB-002, WEB-003 |
| **Backend** | Authoritative system | Điều phối tất cả capabilities, trả kết quả tổng hợp | ARCH-002 |

> **Quy tắc phân quyền cứng:**
> - Mọi thao tác analysis là **Anonymous** [AUTH-001].
> - Không có thao tác ghi/lưu nào thuộc scope feature này.
> - Rate limiting áp dụng bình đẳng cho cả Guest và Authenticated [RATE-001].

--------------------------------------------------------------------------------

## 3. Functional Requirements (EARS Notation)

### 3.1 Unified Request Envelope

*   **EARS[Event]:** WHEN client gửi `POST /api/analysis` với JSON body chứa `text` (chuỗi Japanese) và optional `interaction_id`,
    **THE system SHALL** điều phối tất cả analysis capabilities đã registered và trả kết quả trong một response envelope duy nhất [EXT-006].

*   **EARS[Ubiquitous]:** Request body **SHALL** tuân thủ cấu trúc:
    ```json
    {
      "interaction_id": "uuid — optional, client-generated",
      "text": "食べました",
      "context": "optional — sentence context for grammar analysis"
    }
    ```
    - `text`: Bắt buộc. Chuỗi Japanese text cần phân tích.
    - `interaction_id`: Optional. UUID do client tạo để correlate response với interaction hiện tại [ASYNC-001].
    - `context`: Optional. Câu chứa selection để hỗ trợ grammar analysis khi có thể trích xuất an toàn [GRM-001, PRIV-001].

### 3.2 Capability Orchestration

*   **EARS[Ubiquitous]:** THE system SHALL điều phối các capabilities sau (khi available):
    1. **Vocabulary Lookup** — Gọi `LookupWordUseCase` với nguyên `text` → trả `dictionary_matches`. Vocabulary thực hiện exact match và deinflection; hoạt động tốt cho **từ đơn hoặc cụm từ ngắn** (ví dụ `食べました`, `学生`), trả rỗng cho **câu dài** chưa được tokenize (ví dụ `日本語を勉強する`). Đây là giới hạn được chấp nhận cho đến khi Tokenizer Sidecar (OD-010) sẵn sàng [Clarification 2026-08-24].
    2. **Kanji Lookup** — Gọi `LookupKanjiUseCase` với `text` → trả `kanji`. Kanji tự trích xuất per-character, hoạt động với mọi độ dài input.
    3. **Morphology/Tokenization** — Gọi Tokenizer adapter (khi available) → trả `tokens`. Khi có tokenizer, vocabulary sẽ được nâng cấp để lookup từng token thay vì nguyên text.
    4. **Conjugation** — Gọi conjugation use case (khi available) → trả `conjugations`.
    5. **Grammar Detection** — Gọi grammar use case (khi available) → trả `grammar_occurrences`.

*   **EARS[Ubiquitous]:** Các capabilities **SHOULD** được thực thi song song khi không có dependency giữa chúng. Vocabulary lookup và kanji lookup không phụ thuộc nhau và có thể chạy đồng thời.

*   **EARS[Ubiquitous]:** Translation **SHALL NOT** được gọi bởi analysis endpoint. Translation chỉ được kích hoạt qua endpoint riêng (`POST /api/translations`) sau explicit user action [TRN-004].

### 3.3 Partial Result Semantics

*   **EARS[Unwanted]:** WHERE một capability gặp lỗi (ví dụ: database timeout cho vocabulary, grammar engine unavailable),
    **THE system SHALL** trả HTTP 200 với partial result chứa:
    - Kết quả thành công của các capabilities đã hoàn thành.
    - `capabilities` map chỉ rõ trạng thái từng capability: `"completed"`, `"unavailable"` hoặc `"failed"` [NET-006].

*   **EARS[Ubiquitous]:** Partial result **SHALL NOT** được trình bày như complete success. `capabilities` map là bắt buộc trong mọi response — client dựa vào đó để biết section nào đáng tin cậy.

*   **EARS[Unwanted]:** WHERE **tất cả** capabilities đều thất bại,
    **THE system SHALL** trả HTTP 503 (`SERVICE_UNAVAILABLE`) kèm structured error thay vì HTTP 200 với partial result rỗng.

### 3.4 Response Envelope

*   **EARS[Ubiquitous]:** Response thành công (full hoặc partial) **SHALL** tuân thủ cấu trúc:
    ```json
    {
      "data": {
        "normalized_text": "食べました",
        "tokens": [],
        "dictionary_matches": [],
        "kanji": [],
        "conjugations": [],
        "grammar_occurrences": [],
        "capabilities": {
          "vocabulary": "completed",
          "kanji": "completed",
          "morphology": "unavailable",
          "conjugation": "unavailable",
          "grammar": "unavailable"
        }
      },
      "meta": {
        "request_id": "server-generated-uuid",
        "interaction_id": "client-sent-uuid-or-null",
        "contract_version": "1"
      }
    }
    ```

*   **EARS[Ubiquitous]:** `meta.interaction_id` **SHALL** echo lại giá trị client gửi trong request (nếu có) để client correlate response với interaction hiện tại [ASYNC-001].

*   **EARS[Ubiquitous]:** `meta.request_id` **SHALL** là UUID do server tạo, duy nhất cho mỗi request, dùng cho logging và troubleshooting.

### 3.5 Input Validation

*   **EARS[Unwanted]:** WHERE input `text` rỗng, chỉ whitespace, hoặc vượt quá giới hạn ký tự (baseline 1000 ký tự — chờ OD-014 chốt chính thức),
    **THE system SHALL** trả HTTP 400 (`VALIDATION_FAILED`) kèm structured error [API-004].

*   **EARS[Unwanted]:** WHERE input `interaction_id` không phải UUID hợp lệ,
    **THE system SHALL** trả HTTP 400 (`VALIDATION_FAILED`).

*   **EARS[Ubiquitous]:** Input validation **SHALL** được thực hiện **trước** khi gọi bất kỳ capability nào [SEC-003].

### 3.6 Interaction Identity & Stale Response Protection

*   **EARS[Ubiquitous]:** Response **SHALL** mang `interaction_id` (khi client gửi) để client có thể xác định response nào thuộc interaction current [ASYNC-001].

*   **EARS[Ubiquitous]:** Client-side stale protection logic nằm ngoài scope Backend feature này — Backend chỉ đảm bảo echo `interaction_id` chính xác. Client chịu trách nhiệm reject response có `interaction_id` không còn current [ASYNC-002].

### 3.7 Privacy & Logging

*   **EARS[Ubiquitous]:** Raw selected text từ `text` và `context` **SHALL NOT** được persist trong ordinary application/operational logs [PRIV-002, PRIV-006].

*   **EARS[Ubiquitous]:** Operational telemetry **SHOULD** chỉ log non-content metadata: `request_id`, `interaction_id`, request size, duration, status code và capability outcomes [PRIV-003].

*   **EARS[Ubiquitous]:** `context` field (nếu có) **SHALL** chỉ được sử dụng cho grammar analysis trong immediate processing và **SHALL NOT** được lưu trữ hoặc gửi cho bên thứ ba [PRIV-001, GRM-001].

### 3.8 Domain Layer Constraints

*   **EARS[Ubiquitous]:** Domain layer **SHALL NOT** import bất kỳ thư viện bên ngoài hoặc ORM nào [AGENTS.md §4.2].

*   **EARS[Ubiquitous]:** Unified Analysis use case **SHALL** gọi các capability use cases hiện có (LookupWordUseCase, LookupKanjiUseCase) thay vì duplicate logic.

--------------------------------------------------------------------------------

## 4. Non-Functional Requirements

### 4.1 Performance & Latency

| ID | Requirement | Target | Evidence |
|---|---|---|---|
| NFR-PERF-01 | P95 analysis response time (all available capabilities) | ≤ 800 ms khi service warm | PERF-002 |
| NFR-PERF-02 | Client timeout | ≤ 5 giây trước explicit failure state | PERF-004 |

> **Lý do:** Với MVP hiện tại chỉ có vocabulary + kanji (2 capabilities chạy song song), p95 target khá thoải mái. Khi thêm grammar/conjugation/morphology, cần benchmark lại.

### 4.2 Security & Privacy

| ID | Requirement | Evidence |
|---|---|---|
| NFR-SEC-01 | Validate và sanitize mọi input — chặn injection | AGENTS.md §4.1, SEC-003 |
| NFR-SEC-02 | Không lưu raw query text vào system logs mặc định | PRIV-002, PRIV-006 |
| NFR-SEC-03 | Không để lộ stack trace, SQL detail hoặc internal path trong error response | SEC-003, API-004 |
| NFR-SEC-04 | Production phải dùng encrypted transport | SEC-001 |
| NFR-SEC-05 | `context` không được gửi cho third-party provider | PRIV-001 |

### 4.3 Cacheability

| ID | Requirement | Evidence |
|---|---|---|
| NFR-CACHE-01 | Analysis response phù hợp cho caching strategy tương lai | CACHE-001 |

> **Lưu ý:** Feature này không implement caching. Response design phải không cản trở caching sau này.

--------------------------------------------------------------------------------

## 5. Data Model

> **Database:** PostgreSQL 16 (ARCH-005) | **ORM:** EF Core + Npgsql (ARCH-007)

### 5.1 Tables liên quan

Feature này **không tạo table mới**. Nó orchestrate các use cases đã có, truy vấn:

| Bảng | Qua Use Case | Vai trò |
|---|---|---|
| `dictionary_entries` + related | `LookupWordUseCase` | Vocabulary lookup |
| `kanji_records` | `LookupKanjiUseCase` | Kanji lookup |
| (future) `grammar_rules` | Grammar use case | Grammar detection |

### 5.2 Response Data Structures

Các DTO trong response sử dụng lại cấu trúc từ F-06 và F-07:

- `dictionary_matches[]` — Reuse `EntryMatchDto` từ F-06, giữ `matched_written_form`, `matched_reading`, `senses[]` với restrictions [VOC-007, VOC-008].
- `kanji[]` — Reuse `KanjiDetailDto` từ F-07, giữ canonical info với JLPT provenance [KAN-001, KAN-002].
- `tokens[]` — Future: morphology tokens với `surface`, `base_form`, `part_of_speech`, `span`.
- `conjugations[]` — Future: conjugation explanations với `surface`, `base_form`, `explanation` (Vietnamese-first) [CONJ-001, CONJ-002].
- `grammar_occurrences[]` — Future: detected grammar patterns với `grammar_id`, `span`, canonical rule info [GRM-007, GRM-008].
- `capabilities` — Map trạng thái mỗi capability: `"completed"` | `"unavailable"` | `"failed"`.

--------------------------------------------------------------------------------

## 6. Error Handling

### 6.1 Input Validation Errors (HTTP 400)

*   **EARS[Unwanted]:** WHERE input `text` rỗng, chỉ whitespace, hoặc vượt quá giới hạn (baseline 1000 ký tự — OD-014):
    ```json
    {
      "error_code": "VALIDATION_FAILED",
      "message": "Nội dung phân tích không được trống và không được vượt quá giới hạn cho phép.",
      "request_id": "9fa5e91a-..."
    }
    ```

### 6.2 No Analysis Result (HTTP 200 — Empty Sections)

*   **EARS[Unwanted]:** WHERE input text không chứa Japanese content analyzable (chỉ whitespace, symbols, Latin...),
    **THE system SHALL** trả HTTP 200 với tất cả sections rỗng (`[]`) và `capabilities` cho thấy các capabilities đã chạy thành công nhưng không có kết quả.

### 6.3 All Capabilities Failed (HTTP 503)

*   **EARS[Unwanted]:** WHERE tất cả capabilities đều thất bại:
    ```json
    {
      "error_code": "SERVICE_UNAVAILABLE",
      "message": "Hệ thống phân tích đang bận. Vui lòng thử lại sau giây lát.",
      "request_id": "9fa5e91a-...",
      "retryable": true
    }
    ```

### 6.4 Rate Limiting (HTTP 429)

*   Rate limiting tuân theo quy ước chung [RATE-001, RATE-002]:
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

*   **EARS[Ubiquitous]:** Mọi response HTTP ≥ 400 **SHALL** tuân thủ `{ error_code, message, request_id }` [API-004].
*   **EARS[Ubiquitous]:** Error response **SHALL NOT** chứa stack trace, SQL statement, internal path, hoặc raw selected text [SEC-003, PRIV-002].

--------------------------------------------------------------------------------

## 7. Acceptance Criteria

### Request & Response Structure

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-001 | `POST /api/analysis` với `text=食べました` | HTTP 200, response chứa `dictionary_matches` (≥1), `kanji` (≥1 cho `食`), `capabilities` map |
| AC-002 | Response chứa `meta.request_id` và `meta.contract_version` | Cả hai field present và non-null |
| AC-003 | Request có `interaction_id`, response echo lại | `meta.interaction_id` khớp giá trị gửi |
| AC-004 | Request không có `interaction_id` | `meta.interaction_id` là `null` — không lỗi |

### Partial Result Semantics

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-005 | Vocabulary thành công, kanji thất bại (DB error) | HTTP 200, `dictionary_matches` có data, `kanji` là `[]`, `capabilities.vocabulary` = `"completed"`, `capabilities.kanji` = `"failed"` |
| AC-006 | Grammar/conjugation/morphology chưa implement | `capabilities.grammar` = `"unavailable"`, `capabilities.conjugation` = `"unavailable"`, `capabilities.morphology` = `"unavailable"`, sections tương ứng là `[]` |
| AC-007 | Tất cả capabilities thất bại | HTTP 503, structured error `SERVICE_UNAVAILABLE` |

### Input Validation

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-008 | Input rỗng `text=` hoặc chỉ whitespace | HTTP 400, `VALIDATION_FAILED` |
| AC-009 | Input vượt 1000 ký tự | HTTP 400, `VALIDATION_FAILED` |
| AC-010 | `interaction_id` không phải UUID hợp lệ | HTTP 400, `VALIDATION_FAILED` |

### Privacy & Security

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-011 | Kiểm tra logs sau analysis request | Không tìm thấy raw `text` value trong ordinary logs |
| AC-012 | Mọi error response ≥ 400 | Chứa `error_code`, `message`, `request_id`; không chứa stack trace |
| AC-013 | Analysis không yêu cầu authentication | Guest gọi API thành công không cần token/session |
| AC-014 | Translation không được gọi tự động | Analysis endpoint phát zero translation request [TRN-004] |

### Capability Reuse & Architecture

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-015 | Vocabulary results trong analysis | Semantic equivalent với standalone `POST /api/dictionary/lookup` cho cùng input |
| AC-016 | Kanji results trong analysis | Semantic equivalent với standalone `POST /api/kanji/lookup` cho cùng input |
| AC-017 | Domain layer zero external imports | `src/domain/` không có `using Microsoft.*`, `using Npgsql.*` |

--------------------------------------------------------------------------------

## 8. Out of Scope

| # | Excluded Feature | Lý do | Reference |
|---|---|---|---|
| 1 | Translation orchestration | Gọi riêng qua `POST /api/translations` sau explicit user action | TRN-004 |
| 2 | Grammar detection logic | Thuộc F-08, chưa implement; analysis sẽ tích hợp khi sẵn sàng | GRM-002 |
| 3 | Conjugation logic | Thuộc F-09, chưa implement | CONJ-001 |
| 4 | Morphology/Tokenization | Cần Tokenizer Sidecar (OD-010); analysis sẽ tích hợp khi sẵn sàng | ARCH-003 |
| 5 | Standalone Dictionary/Kanji endpoints | Không bị thay thế, vẫn hoạt động song song | — |
| 6 | Save/Learning features | Thuộc F-14, F-15 | LEARN-001, LEARN-003 |
| 7 | Caching layer | Chờ OD-006 | CACHE-001 |
| 8 | Rate limiting implementation | Thuộc F-22 (Reliability & Operations) | RATE-001 |
| 9 | Client-side stale response rejection | Client responsibility [ASYNC-002] | ASYNC-002 |

> **Nguyên tắc:** Nếu có nghi ngờ một tính năng có thuộc scope hay không, mặc định là **OUT**. Chỉ đưa vào khi có requirement ID cụ thể trong `REQUIREMENT.md` và được human approve.

--------------------------------------------------------------------------------

## 9. Assumptions

- `LookupWordUseCase` và `LookupKanjiUseCase` đã được implement và đăng ký DI (F-06, F-07 đã xong).
- **Vocabulary nhận nguyên `text` (Phương án A)**: `LookupWordUseCase` xử lý input như exact match + deinflect. Hoạt động cho từ đơn (`食べました` → tìm `食べる`), trả rỗng cho câu dài chưa tokenize. Khi Tokenizer Sidecar sẵn sàng, analysis sẽ tách câu thành tokens rồi lookup từng từ — không breaking change.
- Vocabulary và kanji capabilities không phụ thuộc nhau và có thể chạy song song.
- Grammar, conjugation và morphology capabilities sẽ được tích hợp khi F-08, F-09 và Tokenizer Sidecar hoàn thành — analysis endpoint không cần thay đổi contract.
- `interaction_id` là client-generated UUID; server chỉ echo lại, không validate semantics (chỉ validate format UUID).
- Input length limit baseline là 1000 ký tự, chờ OD-014 chốt chính thức. Vocabulary use case có giới hạn riêng 255 ký tự — nếu `text` vượt 255 chars, vocabulary sẽ bị cắt hoặc skip.
- `context` field hiện tại không được sử dụng (grammar chưa implement); khi grammar sẵn sàng, context sẽ được truyền cho grammar use case.

--------------------------------------------------------------------------------

## 10. Open Questions (Chờ Human Decision)

| # | Question | Impact | Status |
|---|---|---|---|
| 1 | **OD-014:** Maximum input length chính thức cho analysis? | Ảnh hưởng validation rule | ⏳ Pending — dùng baseline 1000 ký tự |
| 2 | **OD-003:** Shared offset convention (UTF-16 code units vs Unicode code points) cho `tokens[].span`? | Grammar/client parity | ⏳ Pending — deferred cho grammar/morphology features |

--------------------------------------------------------------------------------

## 11. Traceability Matrix

| Requirement ID | Mô tả | Covered by Section |
|---|---|---|
| EXT-006 | Unified analysis request thay vì nhiều request riêng | §3.1, §3.2 |
| WEB-003 | Web result có thể trình bày vocabulary, kanji, morphology, conjugation, grammar | §3.2, §3.4 |
| NET-006 | Partial result chỉ rõ capability nào thành công/thất bại | §3.3 |
| ASYNC-001 | Interaction identity đủ để correlate response | §3.1, §3.6 |
| ASYNC-002 | Response cũ không ghi đè state mới | §3.6 |
| AUTH-001 | Analysis không yêu cầu đăng nhập | §2 |
| TRN-004 | Translation chỉ sau explicit user action | §3.2 |
| API-004 | Structured error response | §6.5 |
| SEC-003 | Không leak stack trace | §6.5 |
| PRIV-001 | Minimum context cho processing | §3.7 |
| PRIV-002 | Không log raw selected text | §3.7 |
| PRIV-006 | Analysis interaction ephemeral | §3.7 |
| GRM-001 | Sentence context cho grammar khi safe | §3.1 |
| PERF-002 | P95 ≤ 800 ms | §4.1 |
| PERF-004 | Client timeout ≤ 5 giây | §4.1 |
| AGENTS.md §4.2 | Domain layer zero external imports | §3.8, AC-017 |

--------------------------------------------------------------------------------

## 12. Constitution Alignment

- **Backend authority** (Constitution §2): Unified Analysis là Backend capability, client chỉ tiêu thụ qua shared contract.
- **Shared client semantics** (Constitution §2): Extension và Web dùng chung endpoint, nhận cùng canonical identities/provenance.
- **Stable identity** (Constitution §4): Reuse canonical IDs từ vocabulary (`dictionary:xxxxx`) và kanji (`kanji:xxxxx`).
- **Privacy** (Constitution §7): Raw selected text không được persist trong logs; context field bị giới hạn cho immediate processing.
- **Provider isolation** (Constitution §8): Unified Analysis không phụ thuộc external provider trực tiếp; translation provider nằm ngoài scope.
- **Partial result transparency** (Constitution §6): `capabilities` map đảm bảo client không nhầm partial result là complete success.
- **Spec Kit gates** (Constitution §9): SPEC này surface constitutional impact trước implementation.
