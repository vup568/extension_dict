# Plan

- [x] Tạo cây thư mục đích.
- [x] Di chuyển tài liệu hiện có và tạo scaffold rỗng.
- [x] Cập nhật đường dẫn nội bộ và cấu hình Spec Kit.
- [x] Xác minh cấu trúc, liên kết root và trạng thái Git.

## Issues Encountered

- Sandbox ban đầu chặn ghi vào `.agents/`; đã xử lý bằng quyền ghi giới hạn đúng thư mục dự án.
- Windows không cấp quyền tạo symbolic link; dùng NTFS hard link cho `AGENTS.md` và `CLAUDE.md` tại root.
- PowerShell chặn chạy script theo policy mặc định; kiểm tra Spec Kit thành công với `ExecutionPolicy Bypass` chỉ cho tiến trình kiểm tra.

## Current Task — Chuẩn hóa Product Requirements v2 (2026-08-18)

- [x] Trình bày Shadow Plan và nhận xác nhận `Proceed`.
- [x] Đọc hiến pháp dự án, Project Memory và workflow cập nhật tài liệu.
- [x] Khảo sát các nguồn sự thật và lập bản đồ domain/actor/requirement.
- [x] Chuẩn hóa `REQUIREMENT.md` với requirement ID duy nhất và acceptance criteria.
- [x] Loại bỏ requirement trùng, giải quyết mâu thuẫn và bổ sung glossary.
- [x] Kiểm tra ID, từ khóa chuẩn tắc, liên kết và traceability matrix.
- [x] Cập nhật `SDD.md` với quyết định và bài học mới.

### Task Risks and Resolutions

- `REQUIREMENT.md` và `MIGRATION_DECISION.md` từng để tech stack là open decision, trong khi `AGENTS.md` đã chốt .NET 10, React 19 và PostgreSQL 16. Theo Shadow Plan đã được duyệt, `AGENTS.md` là nguồn có thẩm quyền hiện hành.
- `SDD.md` còn ghi SQL Server/SQLite. Nội dung này sẽ được đồng bộ sang PostgreSQL 16/Testcontainers; các quyết định lịch sử trong `MIGRATION_DECISION.md` được giữ nguyên.
- `.sdd/rfcs/adr-002-database-choice.md`, `.sdd/constraints/*` và `.sdd/specs/feat-dict-lookup/SPEC.md` cũng còn tham chiếu SQL Server/SQLite. Các artifact này nằm ngoài phạm vi chỉnh sửa đã duyệt và cần được supersede/cập nhật trong một nhiệm vụ riêng trước implementation.
- Repo không có `.claude/scripts/validate-docs.cjs` theo workflow `ck:docs`; đã thay bằng validation read-only kiểm tra 117 requirement ID, acceptance criteria, cross-reference, normative keyword, Markdown table và whitespace. Tất cả kiểm tra đều đạt.

## Current Task — Clarify Conceptual Data Model (2026-08-18)

- [x] Trình bày Shadow Plan và nhận xác nhận `Proceed`.
- [x] Xác nhận artifact đích là `REQUIREMENT.md`, không phải active feature SPEC.
- [x] Audit ambiguity về entity, identity, relationship, ownership và lifecycle.
- [x] Hỏi và ghi nhận 5 clarification có tác động cao.
- [x] Tích hợp Conceptual Data Model technology-agnostic vào `REQUIREMENT.md`.
- [x] Cập nhật glossary và requirement traceability liên quan; không phát sinh open decision product mới.
- [x] Validate consistency với Constitution và downstream feature specs (read-only).
- [x] Cập nhật `SDD.md` với các quyết định data model được chấp thuận.

### Clarification Scope Note

- `.specify/scripts/powershell/check-prerequisites.ps1 -Json -PathsOnly` xác định active feature là `feat-platform-foundation`; user đã chỉ định rõ workflow này phải cập nhật `REQUIREMENT.md`, vì vậy active feature SPEC chỉ được đọc làm bằng chứng và không bị chỉnh sửa.

---

## Current Task — Database Schema Design cho JP Reading Platform V2 (2026-08-19)

### Mục tiêu

Thiết kế Logical Database Schema tổng thể cho PostgreSQL 16, ánh xạ chính xác từ Conceptual Data Model (REQUIREMENT.md §6) và tuân thủ mọi ràng buộc từ AGENTS.md, SDD.md, MIGRATION_DECISION.md. Schema sẽ được triển khai dần dần qua EF Core Migrations (ARCH-007).

### Tài liệu nguồn (Read-only)

| File | Vai trò |
|---|---|
| `AGENTS.md` | Hiến pháp kỹ thuật — Tech Stack, Layer Rules, Naming |
| `SDD.md` | Project Memory — ADR, Lessons Learned, Sprint Notes |
| `REQUIREMENT.md` §6 | Conceptual Data Model — Entity Catalog, Relationships, Invariants |
| `REQUIREMENT.md` §9–10 | Functional & Non-Functional Requirements |
| `MIGRATION_DECISION.md` §5 | Dữ liệu cần Preserve/Migrate (JMdict, KANJIDIC2, Grammar, Vietnamese Overrides) |
| `ck:databases` skill | Quy chuẩn thiết kế DB — OLTP rules, PostgreSQL stack rules |

### Nguyên tắc thiết kế chốt cứng

- **PostgreSQL 16**: `TIMESTAMPTZ`, `JSONB`, `BIGSERIAL`, `TEXT`, `UUID` (postgres.md stack rules)
- **Canonical ID typed resource**: Format `dictionary:xxxxx`, `kanji:xxxxx`, `grammar:xxxxx` (ID-005)
- **Soft Delete**: `learning_references` dùng `deleted_at TIMESTAMPTZ NULL` + Partial Index (LEARN-008, AGENTS.md §5.4)
- **Audit columns**: Mọi bảng đều có `created_at`, `updated_at` với trigger auto-update
- **Multilingual-capable**: `localized_glosses` dùng `language_tag` (vi, en) (I18N-003, I18N-004)
- **Immutable Knowledge Release**: `knowledge_releases` có `published_at`, `is_current` (DATA-005)
- **EF Core Migrations**: Output cuối cùng là C# Entities + Fluent API, không phải raw SQL (ARCH-007)

### Rủi ro đã nhận diện

| Rủi ro | Giảm thiểu |
|---|---|
| OD-008 (Grammar matcher schema) còn mở | `grammar_rules.matcher_metadata` dùng JSONB để không khóa chết |
| OD-002 (Auth provider) còn mở | `users` thiết kế tối giản, chỉ giữ identity core |
| OD-003 (Span convention) còn mở | Grammar Occurrence là ephemeral, không persist vào DB |
| OD-009 (FVDP/OVDP) chưa approved | `localized_glosses` tách provenance, dễ thay nguồn sau |

---

### Phase 1 — Schema Design (Logical Design)

- [x] 1.1 Thiết kế ERD (Mermaid diagram) bao phủ 5 nhóm bảng (17 tables):
  - **Knowledge Core** (6): `dictionary_entries`, `written_forms`, `readings`, `dictionary_senses`, `localized_glosses`, `sense_applicabilities`
  - **Kanji** (1): `kanji_records`
  - **Grammar** (1): `grammar_rules`
  - **Knowledge Operations** (6): `source_manifests`, `source_records`, `editorial_mappings`, `knowledge_releases`, `knowledge_release_manifests`, `resource_revisions`
  - **User & Learning** (3): `users`, `user_external_logins`, `learning_references`
- [x] 1.2 Xác định PK, FK, Unique Constraints, Indexes, Check Constraints cho mọi bảng
- [x] 1.3 Xác định cardinalities và ON DELETE behavior cho mọi FK relationship
- [x] 1.4 Trình bày DDL PostgreSQL 16 hoàn chỉnh (tham chiếu, review)
- [x] 1.5 Human review hoàn thành — chốt Phase 1 (2026-08-20)

### Phase 2 — Schema Implementation (C# Entities + EF Core)

- [x] 2.1 Tạo 17 C# Domain Entities trong `src/domain/Entities/` — thuần C# 14, zero external imports
- [x] 2.2 Tạo EF Core DbContext (`AppDbContext.cs`) và 17 Fluent API Configurations trong `src/infra/Persistence/Configurations/`
- [x] 2.3 Clean Architecture boundary verified: Domain có 0 import EF Core, 0 Data Annotations, 0 using statements ngoài namespace riêng
- [x] 2.4 Human review hoàn thành — chốt Phase 2 (2026-08-20)

### Phase 3 — Visualization & Documentation (KHÔNG chỉnh sửa schema)

- [x] 3.1 Generate `docs/database/schema.dbml` — 17 tables, DBML chuẩn cho dbdiagram.io
- [x] 3.2 Generate `docs/database/schema-overview.md` — 5 sections: domains, relationships, constraints, core tables, review points
- [x] 3.3 Verify DBML khớp schema: 17 DBML tables = 17 DDL tables = 17 Entities = 17 Configurations ✅ zero sai lệch
- [x] 3.4 Generate `DATABASE.md` tại root — index file với nguyên tắc thiết kế, liên kết tài liệu, migration strategy

### Phase 4 — Verification & Traceability (KHÔNG chỉnh sửa schema)

- [ ] 4.1 Cross-check schema với requirement files:
  - `REQUIREMENT.md` §6 (Conceptual Data Model — Entity Catalog)
  - `REQUIREMENT.md` §9 (Functional Requirements: ID-*, VOC-*, KAN-*, GRM-*, CONJ-*, TRN-*, AUTH-*, LEARN-*, DATA-*)
  - `REQUIREMENT.md` §10 (Non-Functional: PRIV-*, SEC-*, I18N-*)
  - `MIGRATION_DECISION.md` §5 (Assets to Migrate)
- [ ] 4.2 Lập **Requirement Coverage Matrix**: Mỗi requirement ID có bảng/cột nào phục vụ
- [ ] 4.3 Xác định và báo cáo **Gap Report**: Requirement nào chưa được schema cover rõ ràng
- [ ] 4.4 Báo cáo **Potential Improvements** riêng biệt (nếu phát hiện) — CHỈ báo cáo, KHÔNG tự sửa schema
- [ ] 4.5 Cập nhật `SDD.md` với các quyết định database design được chấp thuận

### Deliverables

| # | Output | Vị trí |
|---|---|---|
| 1 | ERD (Mermaid diagram) | Trong artifact report |
| 2 | DDL PostgreSQL 16 | Trong artifact report |
| 3 | C# Domain Entities | `src/domain/` |
| 4 | EF Core Configuration | `src/infra/` |
| 5 | DBML cho dbdiagram.io | `docs/database/schema.dbml` |
| 6 | Schema Overview | `docs/database/schema-overview.md` |
| 7 | DATABASE.md (Root Index) | `DATABASE.md` |
| 8 | Requirement Coverage Matrix | Trong artifact report |
| 9 | Gap Report & Potential Improvements | Trong artifact report |

---

## Current Task — MVP API Contract Documentation (2026-08-21)

- [x] Shadow Plan approved; scope excludes the existing standalone Dictionary Lookup endpoint.
- [x] Reconcile authoritative product, architecture, privacy, data, and database sources.
- [x] Author the proposed MVP API contract without a URL version segment.
- [x] Validate endpoint coverage, requirement traceability, and route consistency.

### Scope and Decision Record

- New API routes use the `/api/` prefix. Contract compatibility is represented separately from the URL path.
- The existing Dictionary Lookup draft route is intentionally outside this documentation task and is not modified.
- This task adds documentation only; it does not approve or implement any unratified API behavior.

---

## Current Task — UI/UX Design Direction (2026-08-21)

- [x] Trình bày Shadow Plan và nhận xác nhận `Proceed`.
- [x] Đọc Project Memory, Product Requirements, active feature specifications và migration baseline liên quan đến UI/UX.
- [x] Truy vấn thư viện reference thiết kế trong repo bằng tiêu chí product/interaction, không dựa vào aesthetic similarity.
- [x] Derive tiêu chí đánh giá, xếp hạng references và xác định design direction.
- [x] Tạo `DESIGN.md` có traceability requirement/reference và ghi rõ các design decision mới.
- [x] Kiểm tra traceability, scope exclusion và cập nhật Project Memory với quyết định lớn.

---

## Current Task — Refactor DESIGN.md thành Project Design Constitution (2026-08-21)

- [x] Đọc yêu cầu refactor đính kèm và trình bày Shadow Plan.
- [x] Nhận xác nhận `Proceed`.
- [x] Refactor `DESIGN.md` theo source authority và project-level scope.
- [x] Xóa research artifacts, premature visual decisions/tokens và feature-specific design.
- [x] Thêm Visual System Status, Feature Design Contract, decision classification và Promotion Rule.
- [x] Validate Globality, Evidence, Prematurity, Authority và Future freedom.
- [x] Đổi title sang `Project Design Principles` để tránh nhầm với `.sdd/constitution.md`.
- [x] Làm rõ project-level governance principles nằm ngoài feature-decision taxonomy, không tạo category thứ tư.
