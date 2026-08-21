# MVP API Contract

## Tổng quan

Tài liệu này là contract baseline cho các capability MVP dùng chung bởi Browser Extension và Web Application. Nó mô tả API **đề xuất** để review và làm đầu vào cho các feature SPEC sau này; không chứng minh endpoint đã được triển khai hoặc đang chạy.

### Phạm vi

- Bao phủ: analysis, kanji, grammar, translation, authentication, learning library, quick review, export và knowledge operations.
- Không bao phủ: standalone Dictionary Lookup đang tồn tại trong feature draft khác. Route đó không được sửa, thay thế hoặc lặp lại tại đây.
- Ngoài MVP: offline cache/outbox, SRS scheduling nâng cao, auto-translation, Anki/Quizlet two-way sync và grammar N3–N1.

### Trạng thái contract

| Nhãn | Ý nghĩa |
|---|---|
| `Required semantic` | Hành vi bắt buộc đã có trong REQUIREMENT.md. |
| `Proposed HTTP contract` | Route, method, schema hoặc status code được đề xuất trong tài liệu này; cần feature SPEC phê duyệt trước implementation. |
| `Deferred` | Có requirement nhưng chưa đủ quyết định để khóa API shape. |

## Authority, quyết định và xung đột

Thứ tự sử dụng nguồn:

1. `REQUIREMENT.md` — product behavior, ownership, privacy, error và acceptance criteria.
2. `AGENTS.md` + `SDD.md` — stack hiện hành: .NET 10, React 19, PostgreSQL 16 và Testcontainers.
3. `.sdd/constitution.md` — invariants về stable identity, privacy, provider isolation và stale response.
4. Feature specs và database documents — bằng chứng chi tiết; API shape ở đây vẫn là draft/proposed nếu chưa được phê duyệt riêng.

Routes mới bắt đầu bằng `/api/`, không version trong URL theo quyết định project owner ngày 2026-08-21. Điều này không loại bỏ yêu cầu `API-002`: clients gửi `X-Contract-Version: 1` (proposed) và server trả `contract_version` trong response metadata. Compatibility window và version-negotiation policy vẫn `Deferred`.

`.sdd/constraints/global.md` còn nêu SQL Server/SQLite, trong khi AGENTS.md và SDD.md đã chốt PostgreSQL 16/Testcontainers. Tài liệu này theo baseline hiện hành, không theo artifact cũ.

## Quy ước chung

### Transport và encoding

- Production public boundary dùng HTTPS (`SEC-001`).
- Request/response JSON dùng `application/json; charset=utf-8`, trừ export file.
- Field names là `snake_case`; timestamps là ISO 8601 UTC; IDs là opaque string.
- Canonical linguistic ID phải mang resource type, ví dụ `dictionary:12345`, `kanji:日`, `grammar:n5-te-iru` (`ID-005`). Display text không được dùng thay ID (`ID-003`).
- `interaction_id` là UUID do client tạo cho mọi thao tác phụ thuộc selection/input. Client chỉ render response nếu interaction đó vẫn current (`ASYNC-001`, `ASYNC-002`).
- Raw selected text chỉ dùng cho xử lý tức thời; không được persist vào ordinary logs hay interaction history (`PRIV-002`, `PRIV-006`).

### Response envelope — Proposed HTTP contract

Response JSON thành công dùng envelope sau, trừ khi endpoint ghi rõ khác:

```json
{
  "data": {},
  "meta": {
    "request_id": "9fa5e91a-95d1-4b2a-8a6d-9f6049dc6d4b",
    "contract_version": "1"
  }
}
```

`data` có thể là object hoặc array. `meta.interaction_id` phải được trả cho analysis/translation khi request đã gửi field đó.

### Structured error — Required semantic

Mọi lỗi client-visible có tối thiểu `error_code`, `message`, `request_id` (`API-004`). Error không được chứa stack trace, SQL detail, raw provider error hoặc raw selected text.

```json
{
  "error_code": "RATE_LIMITED",
  "message": "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.",
  "request_id": "9fa5e91a-95d1-4b2a-8a6d-9f6049dc6d4b",
  "retryable": true,
  "retry_after_seconds": 30,
  "interaction_id": "a1d3b846-c077-4b5a-8b97-1d4d583ab0ad"
}
```

| HTTP | `error_code` đề xuất | Khi dùng |
|---|---|---|
| 400 | `VALIDATION_FAILED` | Input malformed, thiếu field hoặc vượt limit đã được SPEC chốt. |
| 401 | `AUTHENTICATION_REQUIRED` | Anonymous caller gọi capability learning/operator. |
| 403 | `FORBIDDEN` | Caller không sở hữu learning resource hoặc không có operator role. |
| 404 | `RESOURCE_NOT_FOUND` | Canonical resource không tồn tại/currently unavailable. |
| 409 | `STATE_CONFLICT` | State thay đổi không thể áp dụng an toàn; save idempotent không dùng lỗi này cho duplicate. |
| 426 | `UNSUPPORTED_CONTRACT_VERSION` | Version client ngoài compatibility window — proposed. |
| 429 | `RATE_LIMITED` | Có retry guidance machine-readable (`RATE-002`). |
| 503 | `SERVICE_UNAVAILABLE` / `PROVIDER_UNAVAILABLE` | Backend dependency/provider chưa sẵn sàng. |
| 504 | `REQUEST_TIMEOUT` | Request vượt timeout được phê duyệt. |

Partial result là HTTP 200 chỉ khi `data.capabilities` chỉ rõ capability nào `completed`, `unavailable` hoặc `failed`; nó không được ngụy trang thành complete success (`NET-006`).

### Authentication and authorization

Reading, analysis, kanji, grammar và translation là anonymous (`AUTH-001`). Routes dưới `/api/me/` yêu cầu session đã xác thực và phải kiểm tra ownership trên server (`AUTH-002`, `AUTH-004`). Cơ chế session cụ thể (secure cookie hay bearer token), refresh flow và OAuth provider protocol là `Deferred`; clients không phụ thuộc provider-specific shape.

## Endpoint catalog

| Capability | Method | Route | Access | Status |
|---|---|---|---|---|
| Unified analysis | POST | `/api/analysis` | Anonymous | Proposed HTTP contract |
| Kanji detail | GET | `/api/kanji/{character}` | Anonymous | Proposed HTTP contract |
| Grammar list | GET | `/api/grammar` | Anonymous | Proposed HTTP contract |
| Grammar detail | GET | `/api/grammar/{grammar_id}` | Anonymous | Proposed HTTP contract |
| Translation | POST | `/api/translations` | Anonymous | Proposed HTTP contract |
| Register/login/logout | POST | `/api/auth/register`, `/api/auth/login`, `/api/auth/logout` | Mixed | Proposed HTTP contract |
| External auth start/callback | GET | `/api/auth/providers/{provider}/authorize`, `/callback` | Anonymous | Deferred provider semantics |
| Vocabulary library | GET, PUT, DELETE | `/api/me/learning/vocabulary/{dictionary_id}` | Authenticated | Proposed HTTP contract |
| Grammar library | GET, PUT, DELETE | `/api/me/learning/grammar/{grammar_id}` | Authenticated | Proposed HTTP contract |
| Quick review | GET | `/api/me/review` | Authenticated | Proposed HTTP contract |
| Learning export | POST | `/api/me/exports` | Authenticated | Proposed HTTP contract |
| Knowledge releases | GET, POST | `/api/operator/knowledge-releases/*` | Operator | Proposed control-plane contract |

## Analysis

### `POST /api/analysis`

`Required semantic`: one analysis request can contain vocabulary, kanji, morphology, conjugation and grammar results (`EXT-006`, `WEB-003`). It must not call translation implicitly (`TRN-004`).

```json
{
  "interaction_id": "a1d3b846-c077-4b5a-8b97-1d4d583ab0ad",
  "text": "食べました",
  "context": "昨日、寿司を食べました。"
}
```

`context` is optional and must be omitted unless safely extracted and necessary for grammar analysis (`GRM-001`, `PRIV-001`). Exact input limits and shared offset convention are `Deferred`.

```json
{
  "data": {
    "normalized_text": "食べました",
    "tokens": [{
      "surface": "食べました",
      "base_form": "食べる",
      "part_of_speech": ["verb"],
      "span": { "start": 0, "end": 4, "unit": "deferred" }
    }],
    "dictionary_matches": [{
      "canonical_id": "dictionary:12345",
      "matched_written_form": "食べる",
      "matched_reading": "たべる",
      "senses": [{
        "sense_key": "1",
        "glosses": [{ "language_tag": "vi", "text": "ăn" }],
        "restrictions": { "written_forms": [], "readings": [] }
      }]
    }],
    "kanji": [],
    "conjugations": [{
      "surface": "食べました",
      "base_form": "食べる",
      "explanation": { "language_tag": "vi", "text": "Dạng lịch sự quá khứ." }
    }],
    "grammar_occurrences": [],
    "capabilities": {
      "vocabulary": "completed",
      "kanji": "completed",
      "morphology": "completed",
      "conjugation": "completed",
      "grammar": "completed"
    }
  },
  "meta": {
    "request_id": "9fa5e91a-95d1-4b2a-8a6d-9f6049dc6d4b",
    "interaction_id": "a1d3b846-c077-4b5a-8b97-1d4d583ab0ad",
    "contract_version": "1"
  }
}
```

`dictionary_matches` preserves matched form, reading, applicable senses and restrictions (`VOC-007`, `VOC-008`); it does not define or replace the standalone Dictionary Lookup route. Every `grammar_occurrences[]` item must keep `grammar_id` and a non-ambiguous span; repeated/overlapping occurrences remain separate (`GRM-007`, `GRM-008`).

## Kanji and grammar

### `GET /api/kanji/{character}`

`character` must resolve to one canonical kanji character. Response data includes `canonical_id`, `character`, readings, localized meanings, Hán Việt, radical, stroke count and `jlpt` with `provenance` (`authoritative`, `derived` or `approximate`) (`KAN-001`, `KAN-002`).

### `GET /api/grammar`

Query parameters (proposed): `jlpt_level` (`N5` or `N4` in MVP), `q`, `cursor`, `limit`. The result is a cursor-paginated list of grammar summaries: `canonical_id`, `pattern`, `jlpt_level`, `meaning_vi`, and reviewed `meaning_en` if available (`WEB-004`, `WEB-005`). Pagination encoding and maximum page size are `Deferred`.

### `GET /api/grammar/{grammar_id}`

Returns a canonical rule with `pattern`, `jlpt_level`, localized meanings, formation, examples, matcher metadata safe for clients, and provenance summary (`GRM-005`, `GRM-006`). Provider-internal matcher labels must not leak.

## Translation

### `POST /api/translations`

`Required semantic`: only called after the user explicitly requests translation; send the minimum text/context and show required third-party disclosure before provider transfer (`TRN-004`, `PRIV-004`).

```json
{
  "interaction_id": "a1d3b846-c077-4b5a-8b97-1d4d583ab0ad",
  "text": "昨日、寿司を食べました。",
  "target_language": "vi"
}
```

`target_language` is `vi` or `en` (`TRN-001`, `I18N-002`). Response data is `{ "source_language": "ja", "target_language": "vi", "translated_text": "Hôm qua tôi đã ăn sushi." }`. Provider identity, credentials and raw provider errors are not exposed (`TRN-002`, `TRN-003`).

## Authentication — proposed boundaries

| Route | Request | Success | Notes |
|---|---|---|---|
| `POST /api/auth/register` | `email`, `password` | authenticated-session representation | Password is never returned or logged; hash policy follows `SEC-005`. |
| `POST /api/auth/login` | `email`, `password` | authenticated-session representation | Same identity must work in Extension and Web (`AUTH-003`). |
| `POST /api/auth/logout` | none | `204 No Content` | Invalidates current session according to the future session model. |
| `GET /api/auth/providers/{provider}/authorize` | OAuth redirect parameters | redirect | External provider selection/protocol is deferred. |
| `GET /api/auth/providers/{provider}/callback` | provider callback parameters | redirect/session completion | Provider errors normalized before client display. |

Whether register/login is included in MVP depends on the approved identity provider and recovery/linking SPEC. The routes above are an intentionally provider-neutral proposal, not a provider commitment.

## Learning library

All these routes require authenticated current user. `PUT` is chosen for idempotent save: repeated or concurrent save of the same user plus canonical resource returns the same logical active reference instead of creating a duplicate (`LEARN-007`).

| Method | Route | Behavior |
|---|---|---|
| GET | `/api/me/learning/vocabulary` | List active vocabulary references owned by current user. |
| PUT | `/api/me/learning/vocabulary/{dictionary_id}` | Create or return active saved vocabulary reference. |
| DELETE | `/api/me/learning/vocabulary/{dictionary_id}` | Soft-delete current user’s active reference; return `204`. |
| GET | `/api/me/learning/grammar` | List active grammar references owned by current user. |
| PUT | `/api/me/learning/grammar/{grammar_id}` | Create or return active saved grammar reference. |
| DELETE | `/api/me/learning/grammar/{grammar_id}` | Soft-delete current user’s active reference; return `204`. |

List item shape:

```json
{
  "learning_reference_id": "lr_01J...",
  "resource_type": "dictionary",
  "canonical_id": "dictionary:12345",
  "saved_at": "2026-08-21T10:00:00Z",
  "resource": {
    "display": "食べる",
    "reading": "たべる",
    "localized_content": [{ "language_tag": "vi", "text": "ăn" }]
  }
}
```

The `resource` projection is display data resolved from the canonical resource, never the stored primary identity (`LEARN-002`). Soft-deleted records are excluded from active lists but are not physically deleted (`LEARN-008`).

## Quick review and export

### `GET /api/me/review`

Proposed query: `resource_type` (`vocabulary`, `grammar`, or `all`) and `limit`. It returns active, current-user review cards with `prompt` and hidden `answer`. The client reveals the answer locally after user action (`REV-001`, `REV-002`). `Again`/`Know` persistence is deferred because it is optional and requires its own behavior SPEC (`REV-003`).

### `POST /api/me/exports`

```json
{
  "format": "anki_csv",
  "resource_ids": ["dictionary:12345", "grammar:n5-te-iru"]
}
```

Allowed format identifiers are proposed: `anki_csv`, `quizlet_tsv`. Successful response is a UTF-8 downloadable CSV/TSV attachment; quote, delimiter and newline escaping must round-trip Japanese and Vietnamese (`EXP-002`–`EXP-004`). No endpoint may synchronize with Anki or Quizlet (`EXP-005`). Exact approved column mapping is deferred to `OD-007`.

## Knowledge operations control plane

These routes are not public client APIs. They require a future operator authorization model and must not expose raw source data, secrets or unpublished knowledge releases.

| Method | Route | Required semantic |
|---|---|---|
| GET | `/api/operator/knowledge-releases` | List addressable release metadata. |
| POST | `/api/operator/knowledge-releases/validate` | Validate candidate provenance, licensing, counts and rejected records (`DATA-001`, `DATA-004`). |
| POST | `/api/operator/knowledge-releases` | Create a validated, non-current candidate release. |
| POST | `/api/operator/knowledge-releases/{release_id}/publish` | Controlled publish; preserves immutability and changes current release only after validation (`DATA-005`). |
| GET | `/api/operator/knowledge-releases/{release_id}` | Read release metadata, manifest summaries and validation evidence. |

ETL transport, source upload/storage, operator identity, publish concurrency and rollback policy are `Deferred`; no route permits uncontrolled mutation of an already published release.

## Cross-cutting operational behavior

- Rate limits apply independently to analysis, translation and authenticated state-changing routes. `429` includes `retry_after_seconds` (`RATE-001`, `RATE-002`).
- Clients use a finite timeout; current baseline is at most five seconds unless a future endpoint-specific SPEC approves an exception (`PERF-004`).
- Retries create a new `interaction_id` for selection-dependent requests. Server behavior for state-changing retries relies on idempotent `PUT` save semantics.
- Cache may optimize dictionary, kanji and grammar data only if canonical identity, provenance and current knowledge release semantics are unchanged (`CACHE-001`, `CACHE-002`).
- Server telemetry uses non-content metadata (request ID, duration, status, request size) and must not record the selected text (`PRIV-002`, `PRIV-003`).

## Traceability matrix

| Contract area | Primary requirements |
|---|---|
| Shared contract, validation, error and versioning | API-001–API-004 |
| Unified analysis and stale safety | EXT-006, ASYNC-001–ASYNC-002, NET-006 |
| Vocabulary result semantics inside analysis | ID-001–ID-006, VOC-001–VOC-008 |
| Kanji and grammar | KAN-001–KAN-002, GRM-001–GRM-008, WEB-004–WEB-005 |
| Translation | TRN-001–TRN-004, I18N-002, PRIV-004 |
| Authentication and learning | AUTH-001–AUTH-004, LEARN-001–LEARN-008, PRIV-005 |
| Review and export | REV-001–REV-004, EXP-001–EXP-005 |
| Knowledge operations | DATA-001–DATA-005 |
| Privacy, security, rate and failure | PRIV-001–PRIV-006, RATE-001–RATE-004, NET-001–NET-006, SEC-001–SEC-005 |

## Deferred decisions before implementation

1. Contract-version negotiation, support window and deprecation policy.
2. Shared string offset/span convention for supplementary Unicode.
3. Exact request-size/page-size limits and pagination token format.
4. Session transport, external provider selection, recovery and account-linking flows.
5. Final translation provider, disclosure UX and provider timeout/retry policy.
6. Grammar matcher metadata projection safe for clients.
7. Export column mapping (`OD-007`) and whether large exports need an asynchronous job.
8. Operator authorization and data-ingestion transport.

## References

- `REQUIREMENT.md` §§9–10 — canonical product requirements and acceptance criteria.
- `.sdd/constitution.md` §§2–10 — durable contract, identity, privacy and provider invariants.
- `AGENTS.md` §§2, 4–6 and `SDD.md` — approved platform baseline and project memory.
- `DATABASE.md` and `docs/database/schema-overview.md` — canonical IDs, learning references, soft delete and knowledge-release model.
- `.sdd/specs/feat-platform-foundation/SPEC.md` — shared-contract and deferred-decision constraints.
