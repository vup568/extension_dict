# FEATURE SPEC: Grammar Detection API
# Version: 0.1.0 | Owner: @lead-dev | Date: 2026-08-24
# Inherits: .sdd/constraints/global.md
# Target Stack: .NET 10 Backend + Sudachi Python Sidecar (gRPC) + PostgreSQL 16

--------------------------------------------------------------------------------

## 1. Context & Goal

### 1.1 Business Problem

Khi người dùng đọc tiếng Nhật và gặp cấu trúc ngữ pháp không quen (ví dụ `〜ている`, `〜てはいけない`), họ cần biết ngay pattern đó là gì, cấp độ JLPT nào và nghĩa tiếng Việt — mà không phải rời ngữ cảnh đọc [REQUIREMENT.md §1.2]. Extension V1 đã có grammar detection offline cho N5-N4 nhưng chạy hoàn toàn trên client, coverage khoảng 50%, và matchers viết cho kuromoji (không còn phù hợp). V2 cần chuyển logic này sang Backend với coverage đầy đủ hơn, dữ liệu versioned và matching engine chính xác hơn.

### 1.2 Feature Goal

Xây dựng **Grammar Detection API** trên Backend để:

1. Nhận một câu tiếng Nhật, tokenize qua **Sudachi Sidecar** (gRPC), rồi so khớp token sequences với grammar patterns trong database.
2. Trả về danh sách **grammar occurrences** — mỗi occurrence gồm: canonical grammar ID, pattern name, JLPT level, giải thích tiếng Việt, vị trí (span) trong câu.
3. Hỗ trợ **repeated occurrences** (cùng pattern xuất hiện nhiều lần) và **meaningful overlaps** (hai pattern khác nhau chồng span).
4. Grammar knowledge là **versioned data** tách khỏi matching engine — thêm/sửa pattern không cần sửa code.

### 1.3 Relationship to Other Features

Feature này cung cấp capability cho:
- **F-05 (Unified Analysis)** — nhúng `grammar_occurrences` trong response tổng hợp [EXT-006, WEB-003].
- **F-12 (Grammar Library)** — Web App browse/search grammar patterns [WEB-004, WEB-005].
- **F-15 (Save Grammar)** — Learner lưu grammar rule vào library cá nhân [LEARN-003].

Feature này **KHÔNG** bao gồm:
- Grammar Library UI (F-12) — browse/search trên Web.
- Save Grammar (F-15) — persistence learning reference.
- Conjugation explanation (F-09) — giải thích biến đổi từ.
- Tokenizer Sidecar deployment/ops — chỉ spec giao diện gRPC, không spec infra.

### 1.4 Key Decision: Tokenizer (OD-010 — Resolved)

**Sudachi + Python** đã được chốt làm tokenizer cho project, giao tiếp với Backend .NET qua **gRPC**. Quyết định này unblock đồng thời F-05 (Unified Analysis), F-08 (Grammar Detection) và F-09 (Conjugation).

Lý do chọn Sudachi: deploy dễ (pip install), license sạch (Apache 2.0), actively maintained, dictionary cập nhật thường xuyên, multi-granularity tokenization, đủ nhanh cho use case (Rust backend).

--------------------------------------------------------------------------------

## Clarifications

### Session 2026-08-24

- Q: Grammar detection nên dùng tokenizer hay regex/pattern matching thuần? → A: Dùng **tokenizer** (Sudachi). Regex không đủ chính xác cho tiếng Nhật vì không có dấu cách giữa các từ.
- Q: Chọn tokenizer nào — MeCab hay Sudachi? → A: **Sudachi + Python**. Deploy dễ hơn (không cần compile C++), license Apache 2.0, actively maintained.
- Q: Sidecar giao tiếp với Backend bằng gì? → A: **gRPC**. Typed contract, nhanh hơn HTTP, phù hợp internal service communication.
- Q: Grammar data N5-N4 lấy từ đâu? → A: **Hybrid** — base data từ `jkindrix/japanese-language-data` (CC BY-SA 4.0), enriched bằng 103 Vietnamese descriptions từ V1 extension (original work), matchers viết mới cho Sudachi.

--------------------------------------------------------------------------------

## 2. Actors & Roles

| Actor | Mô tả | Quyền hạn | Evidence |
|---|---|---|---|
| **Anonymous Reader** | Người dùng vãng lai | Xem grammar occurrences trong analysis result, không cần đăng nhập | AUTH-001 |
| **Authenticated Learner** | Người dùng đã đăng nhập | Cùng quyền + Save Grammar (thuộc F-15, ngoài scope) | AUTH-001 |
| **Browser Extension** | Client system | Gửi text qua Unified Analysis, nhận grammar occurrences | EXT-006 |
| **Web Application** | Client system | Gửi text, browse Grammar Library (F-12, ngoài scope) | WEB-001, WEB-004 |
| **Backend** | Authoritative system | Điều phối tokenization và grammar matching | ARCH-002 |
| **Tokenizer Sidecar** | External runtime | Nhận text, trả token stream qua gRPC | ARCH-003, ARCH-008 |
| **Data Operator** | Operational actor | Chuẩn bị, review và publish grammar data | DATA-001, GRM-003 |

--------------------------------------------------------------------------------

## 3. Functional Requirements (EARS Notation)

### 3.1 Grammar Pattern Detection

*   **EARS[Event]:** WHEN Backend nhận text tiếng Nhật cần grammar analysis,
    **THE system SHALL** tokenize text qua Sudachi Sidecar, rồi so khớp token sequences với grammar patterns trong database để phát hiện các grammar occurrences [GRM-002].

*   **EARS[Ubiquitous]:** Quy trình detection gồm 3 bước:
    1. **Tokenize**: Gửi text tới Sudachi Sidecar (gRPC) → nhận danh sách tokens với surface, base form, POS tags.
    2. **Match**: Duyệt token stream, so khớp consecutive token sequences với matchers của grammar rules trong database.
    3. **Return**: Trả danh sách grammar occurrences kèm canonical ID, span và rule metadata.

### 3.2 Grammar Occurrence Identity

*   **EARS[Ubiquitous]:** Mỗi detected grammar occurrence **SHALL** giữ:
    - `grammar_id`: canonical ID của grammar rule (ví dụ `grammar:n5-te-iru`) [GRM-007].
    - `span`: vị trí bắt đầu và kết thúc trong text theo shared offset convention [GRM-007, OD-003].
    - Rule metadata: pattern display, JLPT level, localized meaning.

*   **EARS[Ubiquitous]:** Repeated grammar occurrences (cùng rule xuất hiện nhiều lần) **SHALL NOT** bị collapse — mỗi occurrence giữ span riêng [GRM-008].

*   **EARS[Ubiquitous]:** Meaningful overlaps (hai rule khác nhau có span giao nhau) **SHALL** đều được biểu diễn — không loại bỏ overlap [GRM-008].

### 3.3 Grammar Knowledge as Versioned Data

*   **EARS[Ubiquitous]:** Grammar patterns **SHALL** là versioned data trong database, tách khỏi matching engine code [GRM-005].

*   **EARS[Ubiquitous]:** Thêm grammar record mới **SHALL NOT** yêu cầu sửa code selection, transport hoặc popup architecture [GRM-005, AC-GRM-005].

*   **EARS[Ubiquitous]:** Mỗi grammar rule **SHOULD** hỗ trợ:
    - Canonical ID (ví dụ `grammar:n5-te-iru`)
    - Pattern display (ví dụ `〜ている`)
    - JLPT level (N5 hoặc N4 trong MVP)
    - Vietnamese meaning (ưu tiên)
    - English meaning (khi có reviewed data)
    - Formation (cách tạo thành)
    - Examples (câu ví dụ)
    - Matcher metadata (token patterns cho matching engine)
    - Provenance (nguồn gốc dữ liệu) [GRM-006].

### 3.4 Sentence Context

*   **EARS[Ubiquitous]:** Grammar analysis **SHOULD** dùng sentence context khi context có thể được trích xuất an toàn trong privacy boundary [GRM-001].

*   **EARS[Unwanted]:** WHERE context không thể trích xuất an toàn hoặc không rõ ràng,
    **THE system SHALL** fallback phân tích chỉ trên text chính — không mở rộng dữ liệu gửi đi [GRM-001].

### 3.5 Tokenizer Sidecar Communication

*   **EARS[Ubiquitous]:** Backend **SHALL** giao tiếp với Tokenizer Sidecar qua gRPC với provider-neutral adapter tại Infrastructure boundary [ARCH-008].

*   **EARS[Ubiquitous]:** Domain layer **SHALL NOT** import Sudachi packages hoặc provider-specific token types [ARCH-009]. Domain chỉ nhận normalized token representation.

*   **EARS[Unwanted]:** WHERE Tokenizer Sidecar unavailable hoặc timeout,
    **THE system SHALL** trả grammar capability status `"failed"` trong partial result thay vì crash toàn bộ analysis [NET-006].

### 3.6 MVP Coverage

*   **EARS[Ubiquitous]:** MVP **SHALL** giữ coverage của corpus N5-N4 hiện có sau khi corpus được review và version hóa [GRM-003].

*   **EARS[Ubiquitous]:** Grammar data **SHALL** có provenance record: source/publisher, version, license, transformation, validation result [DATA-001].

*   **EARS[Ubiquitous]:** MVP grammar data source: base từ `jkindrix/japanese-language-data` (CC BY-SA 4.0), enriched bằng Vietnamese descriptions từ V1 (original work), matchers viết mới cho Sudachi [Clarification 2026-08-24].

### 3.7 Privacy

*   **EARS[Ubiquitous]:** Text gửi tới Tokenizer Sidecar **SHALL** chỉ dùng cho immediate processing và **SHALL NOT** được persist trong logs [PRIV-002, PRIV-006].

*   **EARS[Ubiquitous]:** Tokenizer Sidecar **SHALL NOT** gửi text ra bên thứ ba [PRIV-001].

--------------------------------------------------------------------------------

## 4. Non-Functional Requirements

| ID | Requirement | Target | Evidence |
|---|---|---|---|
| NFR-PERF-01 | P95 grammar detection (tokenize + match) | ≤ 800 ms tổng analysis time | PERF-002 |
| NFR-PERF-02 | Sudachi tokenization P95 | ≤ 100 ms cho một câu ngắn (< 100 chars) | Benchmark |
| NFR-SEC-01 | Domain layer zero tokenizer imports | Không có Sudachi dependency trong Domain | ARCH-009 |
| NFR-SEC-02 | Không log raw text vào sidecar logs | Privacy compliance | PRIV-002 |
| NFR-REL-01 | Sidecar failure graceful degradation | Grammar = `"failed"`, other capabilities unaffected | NET-006 |

--------------------------------------------------------------------------------

## 5. Data Model

### 5.1 Grammar Rule Entity (bảng `grammar_rules` — đã có schema)

| Field | Mô tả | Bắt buộc? |
|---|---|---|
| `canonical_id` | ID ổn định, ví dụ `grammar:n5-te-iru` | ✅ |
| `pattern` | Display name, ví dụ `〜ている` | ✅ |
| `jlpt_level` | `N5` hoặc `N4` (MVP) | ✅ |
| `meaning_vi` | Giải thích tiếng Việt | ✅ |
| `meaning_en` | Giải thích tiếng Anh (khi có reviewed data) | Optional |
| `formation` | Cách tạo thành pattern | Optional |
| `examples` | Câu ví dụ minh họa | Optional |
| `matcher_metadata` | Token patterns cho matching engine (JSONB) | ✅ |
| `provenance` | Nguồn gốc dữ liệu | ✅ |

### 5.2 Grammar Occurrence (ephemeral — không persist)

| Field | Mô tả |
|---|---|
| `grammar_id` | Canonical ID của matched rule |
| `span.start` | Vị trí bắt đầu trong text |
| `span.end` | Vị trí kết thúc trong text |
| `span.unit` | Offset convention (chờ OD-003) |
| `matched_text` | Surface text đã match |
| `rule` | Metadata của grammar rule (pattern, level, meaning...) |

### 5.3 Normalized Token (from Sidecar — domain contract)

| Field | Mô tả |
|---|---|
| `surface` | Dạng gốc trong text |
| `base_form` | Dictionary form |
| `part_of_speech` | POS tags (normalized, provider-neutral) |
| `span` | Vị trí trong text |

--------------------------------------------------------------------------------

## 6. Error Handling

*   **Sidecar unavailable**: Grammar trả `capabilities.grammar = "failed"`, analysis tiếp tục với các capabilities khác [NET-006].
*   **Sidecar timeout**: Tương tự — graceful degradation, không block analysis.
*   **No grammar matches**: HTTP 200 với `grammar_occurrences = []`, `capabilities.grammar = "completed"` — đây là kết quả hợp lệ.
*   **Invalid text**: Validation ở analysis level (F-05), không ở grammar level riêng.
*   **Matcher error** (corrupt rule data): Log error, skip rule, trả partial result — không crash.

--------------------------------------------------------------------------------

## 7. Acceptance Criteria

### Grammar Detection Core

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-001 | Input `勉強している` | Phát hiện `〜ている` (N5) với correct span |
| AC-002 | Input `食べてはいけない` | Phát hiện `〜てはいけない` (N4) — longer match ưu tiên |
| AC-003 | Input có 2 lần `〜ている` | Trả 2 occurrences riêng biệt, mỗi cái có span riêng [GRM-008] |
| AC-004 | Input có 2 patterns overlap span | Cả 2 occurrences đều được trả, không loại bỏ overlap [GRM-008] |
| AC-005 | Input không chứa grammar pattern nào | `grammar_occurrences = []`, `capabilities.grammar = "completed"` |

### Tokenizer Integration

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-006 | Sudachi Sidecar unavailable | `capabilities.grammar = "failed"`, các capabilities khác không bị ảnh hưởng |
| AC-007 | Domain layer imports | Không có `using Sudachi.*` hoặc provider-specific types trong Domain |
| AC-008 | Sidecar nhận text và trả tokens | gRPC call thành công, tokens có surface, base_form, POS |

### Grammar Data

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-009 | N5-N4 regression corpus | Toàn bộ approved N5-N4 records pass detection [GRM-003] |
| AC-010 | Thêm grammar record mới vào DB | Detection hoạt động cho record mới mà không cần sửa code [GRM-005] |
| AC-011 | Grammar rule có Vietnamese meaning | `meaning_vi` present và non-empty cho corpus đã review |
| AC-012 | Grammar data có provenance | Source, license, version được ghi nhận [DATA-001] |

### Privacy

| ID | Tiêu chí | Expected Result |
|---|---|---|
| AC-013 | Kiểm tra sidecar logs | Không tìm thấy raw input text |
| AC-014 | Grammar occurrence response | Chứa `grammar_id` và span, không chứa raw sidecar output |

--------------------------------------------------------------------------------

## 8. Out of Scope

| # | Excluded Feature | Lý do | Reference |
|---|---|---|---|
| 1 | Grammar Library UI (browse/search) | Thuộc F-12 | WEB-004, WEB-005 |
| 2 | Save Grammar | Thuộc F-15 | LEARN-003 |
| 3 | N3-N1 grammar expansion | Post-MVP, cần SPEC riêng | GRM-004 |
| 4 | Conjugation explanation | Thuộc F-09 | CONJ-001 |
| 5 | Sidecar deployment/Docker setup | Infrastructure concern | — |
| 6 | Auto-translation of grammar explanations | Translation riêng biệt | TRN-004 |
| 7 | Standalone grammar detection endpoint | Truy cập qua Unified Analysis (F-05) | EXT-006 |

--------------------------------------------------------------------------------

## 9. Assumptions

- Bảng `grammar_rules` đã có schema trong database (từ Phase 1 migrations). Data chưa được import.
- Sudachi Sidecar là Python process riêng, giao tiếp qua gRPC. Backend .NET là gRPC client, Sidecar là gRPC server.
- Matcher metadata lưu dưới dạng JSONB trong `grammar_rules.matcher_metadata` — chứa token patterns mà matching engine đọc runtime.
- Matching engine ưu tiên **longest match** tại mỗi vị trí (ví dụ `〜てはいけない` thắng `〜ては`).
- Offset convention (OD-003) chưa chốt — sử dụng character index (0-based) làm baseline, update khi OD-003 resolved.
- Grammar analysis nhận text từ Unified Analysis (F-05) orchestrator, không expose standalone endpoint riêng.

--------------------------------------------------------------------------------

## 10. Open Questions

| # | Question | Impact | Status |
|---|---|---|---|
| 1 | **OD-003**: Shared offset convention (UTF-16 code units vs Unicode code points) | Grammar/client span parity | ⏳ Pending — dùng character index baseline |
| 2 | **OD-008**: Grammar matcher/storage schema chi tiết và safe sentence-boundary strategy | Linguistic correctness | ⏳ Pending — define trong PLAN |

--------------------------------------------------------------------------------

## 11. Traceability Matrix

| Requirement ID | Mô tả | Covered by Section |
|---|---|---|
| GRM-001 | Sentence context cho grammar analysis | §3.4 |
| GRM-002 | Detect supported grammar patterns | §3.1 |
| GRM-003 | MVP N5-N4 corpus coverage | §3.6 |
| GRM-005 | Grammar knowledge = versioned data | §3.3 |
| GRM-006 | Grammar entry fields | §3.3, §5.1 |
| GRM-007 | Occurrence canonical ID + span | §3.2 |
| GRM-008 | Repeated occurrences + overlaps | §3.2 |
| ARCH-003 | Tokenizer trong sidecar | §3.5 |
| ARCH-008 | Provider-neutral adapter | §3.5 |
| ARCH-009 | Domain zero tokenizer imports | §3.5 |
| DATA-001 | Provenance record | §3.6 |
| NET-006 | Partial result semantics | §3.5, §6 |
| PRIV-002 | Không log raw text | §3.7 |
| PRIV-006 | Interaction ephemeral | §3.7 |

--------------------------------------------------------------------------------

## 12. Constitution Alignment

- **Backend authority** (§2): Grammar detection chạy trên Backend, client chỉ nhận kết quả qua contract.
- **Stable identity** (§4): Mỗi grammar rule có canonical ID ổn định; occurrences giữ unambiguous span.
- **Provider isolation** (§8): Sudachi isolated sau gRPC adapter; Domain không biết provider cụ thể.
- **Knowledge versioned** (§3, §5): Grammar data là versioned data, thêm record không cần sửa code.
- **Privacy** (§7): Text chỉ dùng cho immediate processing, không persist trong logs.
- **Provenance** (§5): Grammar data có source, license, transformation rõ ràng.
