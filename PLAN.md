# Plan

## Current Task — Trải nghiệm đọc tham khảo Yomitan (2026-11-08)

- [x] Đối chiếu SPEC 005 với nền tảng selection, tra từ trong câu, dịch và F09. Ghi các mặc định triển khai và provenance.
- [x] Triển khai nội dung popup, ghim/resize, quét Shift và phát âm AI. Xác minh build, unit, integration có phạm vi và các smoke với fixtures.
- [x] Xác minh popup và quét Shift qua PostgreSQL/Sudachi thật trên extension đóng gói Edge ngày 2026-11-10.
- [x] Kiểm chứng viewport, focus, resize và ranh giới frame của popup trên Chrome, Edge và Brave trong phạm vi T10.
- [ ] Kiểm vòng đời liên nhóm với audio thực tế và chốt giới hạn browser ở P6.
- [x] Chốt gap catalog thể chia `yco-gap-20261110.1` sau rà soát độc lập.
- [x] Tạo và rà soát corpus `yco-20261110.1` gồm 21 ca cho T17.
- [x] Bổ sung và kiểm quy tắc thể chia từ đơn trong dataset `candidate-20261110.1` cho T18.
- [ ] Kiểm toàn tuyến và hồi quy corpus cũ/mới trên môi trường cô lập cho T19 trước khi kích hoạt dataset.
- [ ] Ghi model, giọng, hạn mức quan sát được và kết quả nghe theo mẫu trước nghiệm thu audio.

Theo dõi [kế hoạch 005](plans/261104-yomitan-reading-experience/plan.md), [checklist 28 việc](plans/261104-yomitan-reading-experience/task.md) và [báo cáo kiểm chứng](plans/261104-yomitan-reading-experience/evidence/test-report.md).

## Current Task — Real Dictionary Import Execution (2026-10-15)

- [x] Receive implementation authorization; read SPEC/PLAN and preserve the existing working tree.
- [x] Create dependency-ordered feature TASKS; separate source/mapping/activation gates.
- [ ] P0: Audit-download permission received; artifacts/counts and 40 proposed cases prepared. Source/license/corpus owner decisions remain pending.
- [x] P1: Correct joint applicability/no-kanji and Vietnamese review filtering with regression tests.
- [ ] P2: Build fixture-tested secure parsers, guarded CLI and transactional resumable import mechanism.
- [x] P2 foundation: secure parsers, explicit inspect/import/resume/validate dispatch, forward schema, guarded fresh-target preparation library and transactional JMdict/KANJIDIC mechanics implemented; final integration verification pending.
- [ ] P2 remaining: operator prepare wrapper, reviewed Vietnamese canonical writer, complete validation/metrics and ready-receipt workflow.
- [ ] P3: Full inventories exist for 172,321 Vietnamese records and 50 V1 overrides; semantic/license/mapping decisions remain pending.
- [ ] P4: Full import into an explicitly authorized isolated database and verify all success criteria.
- [ ] P5: Obtain receipt-bound local cutover/Chrome acceptance and provide attribution/rollback.

### Issues Encountered — Import Execution

- G0a audit downloads authorized; all 218,914 JMdict records, 13,108 KANJIDIC2 characters and 172,321 Vietnamese records audited without real database writes. JMdict1339160/sense2 has contradictory no-kanji/written restrictions; canonical import fails closed.
- OD-009/source approval, semantic review, full real import and cutover remain explicit gates. Original OVDP archive audit is authorized, but SourceForge responses currently contain HTML rather than the requested archive.
- Current evidence and FR/SC traceability: `.sdd/specs/002-import-dictionary-data/reviews/execution-validation.md`; available commands/limitations: `docs/dictionary-import.md`.

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
- **Immutable Knowledge Release**: `knowledge_releases` có `published_at`; singleton `current_knowledge_release` giữ current-status tách khỏi snapshot (DATA-005)
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

- [x] 1.1 Thiết kế ERD (Mermaid diagram) bao phủ 5 nhóm bảng (now 18 tables):
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

- [x] 3.1 Generate `docs/database/schema.dbml` — now 18 tables, DBML chuẩn cho dbdiagram.io
- [x] 3.2 Generate `docs/database/schema-overview.md` — 5 sections: domains, relationships, constraints, core tables, review points
- [x] 3.3 Verify DBML khớp schema: now 18 DBML tables = 18 DDL tables = 18 Entities = 18 Configurations ✅ zero sai lệch
- [x] 3.4 Generate `DATABASE.md` tại root — index file với nguyên tắc thiết kế, liên kết tài liệu, migration strategy

### Phase 4 — Verification & Traceability (KHÔNG chỉnh sửa schema)

- [x] 4.1 Cross-check schema với requirement files:
  - `REQUIREMENT.md` §6 (Conceptual Data Model — Entity Catalog) — 19/19 entities covered
  - `REQUIREMENT.md` §9 (Functional Requirements: ID-*, VOC-*, KAN-*, GRM-*, CONJ-*, TRN-*, AUTH-*, LEARN-*, DATA-*) — all covered
  - `REQUIREMENT.md` §10 (Non-Functional: PRIV-*, SEC-*, I18N-*) — all covered
  - `MIGRATION_DECISION.md` §5 (Assets to Migrate) — 8/8 (6 full + 2 partial/runtime)
- [x] 4.2 Lập **Requirement Coverage Matrix**: 117 requirement IDs mapped to tables/columns (2026-08-21)
- [x] 4.3 Xác định và báo cáo **Gap Report**: 0 immediate gaps; 5 deferred behind approval gates (2026-08-21)
- [x] 4.4 Báo cáo **Potential Improvements**: 6 items — CHỈ báo cáo, KHÔNG sửa schema (2026-08-21)
- [x] 4.5 Cập nhật `SDD.md` với các quyết định database design được chấp thuận (2026-08-21)

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
| 8 | Requirement Coverage Matrix | Artifact `phase4_verification_traceability.md` |
| 9 | Gap Report & Potential Improvements | Artifact `phase4_verification_traceability.md` |

---
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

---

## Current Task — Ratify PRD and Platform Foundation (2026-08-21)

- [x] Receive Product Owner approvals for MVP scope, technical baseline, and deferred-decision gates.
- [x] Reconcile the PRD, API baseline, feature map, and roadmap after removing Export from MVP.
- [x] Record Product Owner `VuPM`, approval date `2026-08-21`, and the approved/deferred decision status.
- [x] Promote the Platform Foundation specification after the consistency review.
- [x] Validate the resulting authority, scope, traceability, and links; record the ratification in Project Memory.

---

## Current Task — Align Database Governance Baseline (2026-08-21)

- [x] Confirm the approved PostgreSQL/Testcontainers baseline and identify conflicting ADR-002/global constraints.
- [x] Receive Product Owner direction to rewrite ADR-002 in place rather than create a replacement ADR.
- [x] Rewrite ADR-002 for PostgreSQL 16, Npgsql EF Core Migrations and PostgreSQL Testcontainers.
- [x] Synchronize global constraints, PRD alignment notes, API contract and Project Memory.
- [x] Validate that no SQL Server/SQLite runtime or integration-test baseline remains in the governed documents.

---

## Current Task — Buildable .NET Foundation (2026-08-21)

- [x] Verify .NET 10 SDK is available locally and approve the scaffold scope plus NuGet dependencies.
- [x] Add solution, Clean Architecture project manifests, local EF CLI tooling and central package versions.
- [x] Restore dependencies and prove the solution builds on .NET 10.
- [x] Generate the initial PostgreSQL EF Core migration from the existing model.
- [x] Execute unit tests and PostgreSQL Testcontainers integration test after Docker is available.

### Issues Encountered

- Resolved: Docker Desktop is available. The Testcontainers integration test passed on 2026-08-21; the sandbox alone cannot access the Docker named pipe, so that verification was run with the required local Docker permission.

## Current Task — Secure Local PostgreSQL Compose Configuration (2026-08-21)

- [x] Inspect the existing PostgreSQL container and persistent volume before credential rotation.
- [x] Move actual local PostgreSQL credentials into ignored .env and remove all committed credentials.
- [x] Rotate the existing PostgreSQL role without deleting the persistent volume.
- [x] Validate the Compose configuration, healthcheck and secret hygiene.

## Current Task — Knowledge Release Structural Immutability (2026-08-21)

- [x] Reproduce the missing immutability guards on PostgreSQL and confirm the approved DATA-005 scope.
- [x] Approve a separate current-release pointer and a new forward-only migration.
- [x] Add failing domain and PostgreSQL integration tests for release lifecycle and immutable children.
- [x] Implement the domain lifecycle, current pointer, persistence configuration and controlled publication transaction.
- [x] Generate and validate the immutability migration on PostgreSQL 16.
- [x] Run the full build/test suite, independent review and documentation sync.

### Scope Boundary

- This task enforces release metadata, resource revisions and release-manifest membership immutability. Freezing SourceManifest metadata, SourceRecord and EditorialMapping requires a separately approved snapshot-boundary design because mappings currently have no ReleaseId.

### Traceability Matrix

| Requirement / acceptance criterion | Implementation | Verification |
|---|---|---|
| DATA-005 — published release immutable | Domain one-way publish lifecycle, PostgreSQL release/child triggers, restrictive FKs | `KnowledgeReleaseLifecycleTests`, `KnowledgeReleaseImmutabilityTests` |
| AC-DATA-005 — switch current without modifying old release | Singleton `current_knowledge_release`, serializable EF publication transaction | `Switching_current_pointer_does_not_update_the_old_release`, `KnowledgeReleasePublicationTests` |
| DATA-005 — only controlled publication changes current facts | Transaction-local publication guard, pointer trigger, atomic publish service | direct-SQL rejection, pointer deletion, rollback/retry and concurrency integration tests |
| Migration compatibility | Forward migration backfills legacy `is_current` into singleton pointer | `KnowledgeReleaseMigrationUpgradeTests` |

## Current Task — F-01 Specification Refinement (2026-09-19)

- [x] Read the current specification, requirements, migration decisions, Constitution and Spec Kit template.
- [x] Verify Unicode semantics against versioned Unicode Character Database references.
- [x] Refine the existing F-01 spec using the user's question-based structure.
- [x] Validate requirement-to-acceptance traceability, boundary cases and the quality checklist.

### Scope and Risks

- User requested a detailed specification ready for subsequent planning; this task changes documentation only.
- Main risks: ambiguous Han/kana policy, supplementary tests masked by adjacent kana, accidental symbol acceptance, and confusing detection failure with a valid negative result.
- Feature decisions and limitations are recorded in the SPEC; no new project-wide architecture decision is introduced.

## Current Task — F-01 Implementation (2026-09-20)

- [x] Synchronize local dev with origin/dev and create feat/japanese-text-detection.
- [x] Record explicit approval for the exact Extension build/test dependencies.
- [x] Bootstrap and verify the Extension workspace.
- [x] Generate and validate the Unicode 17.0.0 policy table.
- [x] Implement the detector and boundary using test-first coverage.
- [x] Run unit, browser/privacy and performance evidence appropriate to available browsers.
- [x] Complete acceptance traceability and final review.

### Implementation Risks

- Brave and Firefox are not installed locally, so their parity evidence must remain pending unless an approved environment becomes available.
- The detector performance budget remains pending OD-005; measurements must not be presented as a passed threshold without approval.
- Unicode source generation must use pinned release URLs, verified checksums and deterministic output.

### Issues Encountered

- Environment-blocked: the existing .NET integration suite could not start because Testcontainers could not connect to the local Docker Engine pipe (`npipe://./pipe/docker_engine`). The Release build and all 51 .NET unit tests passed; the 50 integration failures occurred during fixture startup before feature assertions.
- Resolved: sandboxed Vite/Vitest revalidation could not write generated config modules under `extension/node_modules/.vite-temp`; the same locked commands passed outside the sandbox with the required local permission.

## Current Task — F-02 Ruby-Safe DOM Extraction Specification (2026-09-24)

- [x] Read F-02 product authority, migration cases, Constitution, Spec Kit template and current F-01 boundary.
- [x] Resolve product decisions for line boundaries, iframe locality and multi-range behavior with the owner.
- [x] Create a dedicated specification branch and feature directory.
- [x] Create `feat/ruby-safe-dom-extraction` from the completed specification state for planning and later implementation.
- [x] Write the F-02 specification using the requested eight-question structure plus mandatory Spec Kit sections.
- [x] Validate requirements, acceptance coverage, privacy, browser parity, scope and constitutional impact.
- [x] Generate Phase 0 research and Phase 1 design artifacts for F-02.
- [x] Create implementation-ready PLAN.md and dependency-ordered TASKS.md on feat/ruby-safe-dom-extraction.
- [x] Implement the content-free result contract, pure text accumulator and isolated DOM selection adapter.
- [x] Add exact ruby, partial range, structure, Unicode, contenteditable, frame, error, privacy and side-effect regressions.
- [x] Verify the complete Extension suite on installed Chrome, Edge and Brave without downloading browsers.
- [x] Record performance, parity, final review and requirement-level traceability evidence.
- [x] Run Extension and .NET regressions, including PostgreSQL Testcontainers with Docker.

### Scope and Decisions

- Structural boundaries use `LF`: selected `br` and block transitions preserve meaningful line separation without artificial outer breaks.
- Extraction is local to the frame where the selection occurs and never aggregates parent/sibling-frame content.
- MVP accepts exactly one non-collapsed range; multi-range selection fails explicitly without partial text.
- Brave and Chrome are primary manual-validation browsers, while the approved Chrome/Edge/Brave/Firefox parity contract remains unchanged.
- Runtime F-02 is implemented without new dependencies; popup behavior, selection listeners, debounce and Backend integration remain outside scope for later features.

### Implementation Status and Limitations

- F-02 returns complete ruby-safe text or one of six content-free error codes and exports the boundary without changing F-01 behavior.
- Chrome, Edge and Brave pass the complete local suite; Firefox evidence remains pending because no compatible executable is installed.
- Node coverage is above the 85% gate, browser-only behavior has real-browser coverage, and all existing .NET unit/integration regressions pass.
- PERF-001 contribution measurements are recorded without claiming an unapproved standalone threshold.

### Issues Encountered

- Resolved: sandboxed Vitest required approved local write access for Vite temporary files.
- Resolved: final design review restored `hr`/`search` to the fixed semantic block policy and made multi-range validation authoritative before collapsed-state handling.
- Pending evidence only: Firefox parity cannot be executed on the current machine.

## Current Task — F-02 Delivery and F-03/F-04 Selection Popup (2026-10-11)

- [x] Review product/design authority, F-01/F-02 boundaries and actual unified API DTOs; approve feature-local plan.
- [x] Merge F-02 into `dev`, push remote and verify `e01ee19d2458403d3dc9d9a18df0ffc4e0426dc4`.
- [x] Create `feat/selection-popup-stale-protection` and approved feature SPEC/PLAN/TASKS.
- [x] Install approved React 19.3.0/Tailwind 4.3.3 and locked transitives; patch `source-map-js` to 1.2.2, audit 0 vulnerabilities.
- [x] Implement actual DTO/runtime validation, frame-local selection and immediate interaction invalidation.
- [x] Implement React/open Shadow DOM popup with loading, capability results, errors/Retry, keyboard/focus and selection anchoring.
- [x] Add restricted background transport, Chromium/Firefox packaging; preserve `dist/index.js` library build.
- [x] Verify 223 unit tests and coverage, typecheck, Unicode policy and production builds.
- [x] Verify .NET Release build with no warnings/errors and 51 unit tests.
- [x] Verify packaged real MV3 transport on Brave from an ordinary HTTP host page.
- [x] Complete full browser regression/popup gates: 112/112 pass (Chrome37, Edge37, Brave37, packaged MV3 Brave1).
- [x] Complete independent review with no outstanding P1/P2 findings and update project documentation.
- [x] Finish feature traceability/evidence sync in the feature directory.
- [x] Commit/push the finished feature branch and verify remote state as authorized.

### Scope, decisions and remaining release limits

- Shell/loading appears before request; request debounce 300ms and independent
  5s deadline. New selection invalidates generation/UUID immediately, including
  during debounce; stale success/error/partial cannot replace current state.
- Feature-local open Shadow DOM/nonmodal popup uses manual native popover top
  layer to isolate ancestor transform/overflow, with fixed-host fallback.
  Lifecycle/positioning include visualViewport events and BFCache recovery.
- Only background fetches configured backend origin; HTTPS except local loopback,
  omitted cookies, rejected redirects, no page-context/history/raw-content logs.
- Existing API/schema/datasets remain linguistic authority. Morphology and
  conjugation remain unavailable; deinflection is null-adapter, vocabulary is
  whole-selection lookup and grammar seed contains 5 patterns.
- Input/textarea selection has no DOM Range and is unsupported. Backend limit
  1000 trimmed UTF-16 units is rejected visibly, never truncated.
- Firefox execution and approved PERF-001 benchmark remain pending. Existing
  Docker startup failure prevents current PostgreSQL Testcontainers verification;
  no Docker reset or user-data deletion was performed.

### Evidence

Feature completion gates and AC mapping:
`.sdd/specs/feat-selection-popup/{SPEC.md,PLAN.md,TASKS.md,evidence/}`.
Coverage: 96.74% statements, 93.58% branches, 98.24% functions, 97.85% lines.
Report: `.sdd/specs/feat-selection-popup/evidence/validation-report.md`.
Feature delivery verified on origin/feat/selection-popup-stale-protection at
`86cd930c1e05bbc8c68a590b452a63d2d3f5e8df` (implementation `99c2634`, tests `18035fa`,
configuration `5a0ff1a`). Remote SHA equals local HEAD. Feature stays on its reviewable
branch; dev retains the authorized F-02 merge `e01ee19`.

## Current Task — Docker Revalidation, F-06 Sync and Feature Merge (2026-10-12)

- [x] Confirm Docker Engine and PostgreSQL 16 Compose service are healthy.
- [x] Run Release build and full unit suite: 0 warnings/errors, 53/53 passed.
- [x] Run full PostgreSQL Testcontainers integration suite: 51/51 passed.
- [x] Run focused F-06 unit/integration slices: 13/13 and 6/6 passed.
- [x] Reproduce and fix NullTokenizer capability mismatch with a PostgreSQL regression test.
- [x] Synchronize F-06 SPEC/PLAN/TASKS, validation evidence and Project Memory.
- [x] Review, commit and push the revalidated feature branch.
- [x] Merge remote feature branch locally into `dev` at b3d187d and run post-merge gates.

### Remote delivery

Direct push to `dev` was rejected because GitHub requires a pull request.
Delivery proceeds through [PR #10](https://github.com/vup568/extension_dict/pull/10);
its merged state is the authoritative remote completion record.
Post-merge gates: Release build 0 warnings/errors, 53/53 .NET units, 51/51 PostgreSQL
Testcontainers tests, Extension typecheck/223 units/production build.

### Remaining scope

- OD-010 is resolved as Python + Sudachi/gRPC in F-08; Dictionary deinflection and F-09 integration remain unimplemented. Exact lookup does not imply AC-010 completion.
- Approved performance certification remains blocked by OD-005.
- Public HTTP response shapes and database schema are unchanged by the capability fix.

## Current Task — OD-010 Documentation Alignment (2026-10-12)

- [x] Confirm the resolved F-08 decision and existing Python Sudachi/gRPC Sidecar code.
- [x] Correct F-06 SPEC/PLAN/TASKS/evidence and the PRD decision register.
- [x] Align Feature Map, architecture references, README, journal and Project Memory.
- [x] Verify remaining OD-010 references, documentation links/anchors, existing code paths and diff hygiene; changes are documentation-only.

This task records the existing decision; it does not implement Dictionary/F-09 integration
or claim live Sidecar verification.

## Current Task — Dictionary Deinflection & Initial F-09 (2026-10-13)

- [x] Commit reviewed OD-010 corrections separately (`c0cb500`).
- [x] Smoke unchanged Sudachi Docker image and inspect actual token chains.
- [x] Review approved feature SPEC/PLAN and scoped contract evolution.
- [x] Implement UTF-16/privacy/concurrency, immutable data-driven F-06 adapter and initial F-09 API/popup.
- [x] Verify caller cancellation/provider failures, whole-chain negatives and immutable/new datasets.
- [x] Run Docker12, .NET58 unit/83 integration, FE236 unit/121 browser tests; Release/typecheck/build/Unicode and coverage gates pass.
- [x] Complete independent review with no outstanding P1/P2.
- [x] Synchronize feature/F-05/F-06 tasks, API, Feature Map, README and Project Memory.

All16 scoped feature tasks complete; no separate phase files under this feature.
Evidence: `.sdd/specs/feat-tokenizer-deinflection/evidence/validation-report.md`.
Production linguistic signoff/full corpus, generic morphology, sentence segmentation,
Firefox execution and OD-005 certification remain separate. Current feature changes
not pushed/merged/deployed; local Composer Sidecar is healthy and user DB preserved.

### Issues Encountered

Resolved: new integration harness lifecycle duplicated seed data; shared class fixture
now owns provider/DB initialization and cleanup. SQL JSON brace escaping and generated
test token type corrected. Fresh full suites pass; no tests suppressed.
Final Git audit also corrected a broad data-directory ignore with a narrow exception
for the source JSON knowledge dataset; generated dictionary artifacts stay ignored.

## Current Task — F-09 Corpus Specification (2026-10-13)

- [x] Receive approval for the documentation-only Shadow Plan.
- [x] Compare product/constitutional authority with the initial nine-rule implementation.
- [x] Create `.sdd/specs/feat-conjugation-explanation/SPEC.md` for corpus scope, linguistic review and conditional gap completion.
- [x] Validate requirements/acceptance traceability and the separate quality checklist.
- [x] Set active Spec Kit feature directory and record the documentation boundary in memory.

Specification only: no feature PLAN/TASKS, runtime edits, data approval, commit or merge.
The existing nine-rule implementation is reused, not rewritten. Corpus approval is the
first future deliverable; supplementation is required only for approved missing cases.

## Current Task — F-09 Corpus Implementation Planning (2026-10-13)

- [x] Receive Proceed for the planning-only Shadow Plan.
- [x] Run Spec Kit setup against the pinned F-09 feature and read generated template.
- [x] Research current runtime/schema, review-state and production-publication boundaries.
- [x] Write feature PLAN with ordered A/B/C steps, owner approvals, exact files, outputs and tests.
- [x] Generate research/data-model/contracts/quickstart artifacts and recheck Constitution.
- [x] Validate links, placeholders, FR-001–010 traceability, Spec Kit prerequisites and source-file fingerprint.

Design complete; execution status pending. No feature TASKS, approved corpus/review data,
runtime edits, migration execution, new dependency, commit/merge or production activation.
Production dataset eligibility needs a separate controlled-publication gate; the current
parser still expects development data. Corpus selection/review are future human gates,
not unresolved technical assumptions or approvals implied by this planning request.

## Current Task — F-09 Task Generation (2026-10-13)

- [x] Receive explicit user request to generate detailed TASKS from the existing PLAN.
- [x] Resolve active F-09 feature/template with setup-tasks and inspect design artifacts.
- [x] Generate 42 dependency-ordered tasks: setup/foundation4, US1 scope8, US2 review8, US3 conditional completion18, handoff4.
- [x] Preserve human G1/G2/G3, implementation authorization, no-op evidence and separate production-publication boundary.
- [x] Validate checklist IDs/labels/paths/story counts, design links/traceability, prerequisites and source fingerprint.

Generation complete; all42 execution tasks remain unchecked. No corpus/review receipts,
runtime/test code, dataset/migration, installed dependencies or Git writes changed.

## Current Task — F-09 Corpus Execution (2026-10-13)

- [x] Trình Shadow Plan và nhận `Proceed` để chuẩn bị corpus 9 quy tắc.
- [x] Ghi trạng thái đầu vào, môi trường, baseline dataset/seed/migration; giữ nguyên working tree trước đó.
- [x] Soạn 9 quyết định phạm vi, 21 ca đúng, 9 ca âm và 6 ca bảo vệ với đáp án/UTF-16/nguồn ở trạng thái đề xuất.
- [x] Kiểm tra cấu trúc, tham chiếu, số ca tối thiểu và hash gói phạm vi.
- [x] Người dùng thay phạm vi bằng B: bốn nhóm từ, cả thể nâng cao bị động/sai khiến/điều kiện; lưu gói 9-rule cũ như draft chưa duyệt và mở lại T005–T010.
- [x] Người dùng chọn hiển thị cả các cách hiểu có chứng cứ cho dạng mơ hồ như `食べられる`; sửa SPEC/PLAN/TASKS cho contract3 tương thích ngược, chưa code trước G1/G2.
- [x] Soạn `corpus/expanded-20261013.1/case-review.md` với 59 ô (hai ca đúng đề xuất và ca âm/ranh giới mỗi ô), ca đa nghĩa/bất quy tắc/xuyên nhóm; kiểm đếm ID, giữ trạng thái nháp để người dùng duyệt.
- [x] Tạo `corpus/expanded-20261013.1/cases.json` bản nháp 173 ca/59 ô để đối chiếu máy; chưa có segmentation/fixture được duyệt và chưa gồm hết biến thể phụ, không tick T007/G1.
- [x] Ghi nhận người dùng duyệt bản .1 bằng biên nhận/hash; giữ nguyên các file đã trình bày.
- [x] Hoàn thiện/freeze f09-expanded-20261013.2: 59 ô, 408 ca, 107 rule IDs đề xuất và 17 fixtures; từng ca hỗ trợ đủ VI/base/components/UTF-16.
- [x] Đối chiếu 365 đầu vào duy nhất qua gRPC Docker; kiểm 13 ca baseline, 8 guard tests và binding 11 file/hash đạt.
- [x] Ghi nhận người dùng chọn kính ngữ れる/られる và cả hai cách hiểu của 食べさせられる; tạo corpus .3 với60 ô/412 ca/115 rule IDs, giữ .1/.2 nguyên vẹn.
- [x] Quan sát369 đầu vào .3 qua gRPC Docker; kiểm cấu trúc/UTF-16 và8 guard tests đạt, không coi là runtime semantic acceptance.
- [x] G1: Người dùng duyệt gói .3; reviews/user-review-receipt-20261013.3.json bind đúng hashes.
- [x] G2: Người dùng duyệt batch mọi ô/quy tắc/nội dung .3, gồm9 baseline; nguồn tham khảo-only không import ngoại văn.
- [x] G3: PO duyệt2026-10-14;acceptance-manifest.json bind đúng artifacts/hash;không production/Firefox authorization.

### Issues Encountered

- Docker pipe bị sandbox chặn ở lần kiểm tra đầu; truy cập được Docker Engine 29.6.1 sau quyền cục bộ giới hạn cho lệnh kiểm tra/image đã có. Không xóa container/volume.
- Corpus gốc mới có 13 ca đúng và chưa có đáp án ngôn ngữ được duyệt; gói mới là proposed, không tự đánh dấu approved dù kiểm tra cấu trúc đạt.
- Lựa chọn B xuất hiện sau gói 9-rule proposed; cần bộ ca/nguồn/manifest B mới trước G1, và có khả năng cần sửa engine chứ không chỉ JSON data.

## Current Task — F-09 Candidate Completion (2026-10-14)

- [x] Nhận yêu cầu làm nốt v3/popup,dataset/migration,verification;giữ corpus frozen/G1/G2 đã duyệt.
- [x] Tạo parameterized candidate/reader/resource;EF migration mới INSERT-only,không migrate DB người dùng.
- [x] Canonical POS/joint sense applicability;contract1/2 compatible,contract3 mọi nghĩa;popup/validator/header3.
- [x] Corpus404 real-gRPC/SQL +7 faults +browser ASYNC mapping;kiểm VI/segments/UTF16,old replay/immutability,HTTP→TS interoperability.
- [x] FE251 units và browser136,Sidecar12;modified usecase coverage≥85%;không thêm dependency.
- [x] Independent review P1 class-dedupe đã sửa với regression red→green;cycle2 không còn P1/P2. Final79 units/118integration pass,0 failures/skips.
- [x] PO G3 accepted_for_scoped_corpus;F09 plan42/42 complete. Firefox runtime/production publication riêng,không commit/push/merge mặc định.

Issues/resolutions chi tiết ở feature PLAN.md;giữ toàn bộ bằng chứng/hashes cũ.

## Current Task — Windows Local Backend Startup Fix (2026-10-14)

- [x] Nhận Proceed;chẩn đoán thiếu DefaultConnection/AppDbContext và startup nuốt lỗi.
- [x] Test red→green;DI fail-fast,startup guard,helper nạp literal .env và khôi phục process environment.
- [x] Release build0 warning/error;17 focused,79 backend unit,135 integration,251 frontend unit và typecheck đạt.
- [x] Independent review PASS_WITH_RISK cho Windows;người dùng loại Ubuntu khỏi scope,test CI Linux chưa sửa.
- [x] README/local guide,journal,memory và artifact evidence;giữ F09 hash-bound snapshots.

Windows handoff complete;user tự Ctrl+C backend cũ rồi chạy scripts/start-local.ps1.
Không restart/migrate DB người dùng,cài dependencies,commit/push/merge. CI Ubuntu
vẫn là giới hạn trước Git delivery;không tuyên bố gate hook đạt vì hook không có.

## Follow-up — Authorized Local Git Checkpoint (2026-10-14)

- [x] Người dùng cho phép commit cả F06/F09 và bản sửa Windows, chỉ local; không push/merge.
- [x] Giữ nguyên12 hash bindings nghiệm thu; pin line endings bằng .gitattributes, không sửa whitespace trong snapshots frozen.
- [x] Ghi coherent commits cho SPEC/corpus, implementation/tests/script và tài liệu; không đưa secrets/generated test results/harness JSON vào Git.

Checkpoint local không giải quyết CI Ubuntu hoặc thay cho workflow hook chưa có; các bằng chứng nghiệm thu cũ giữ nguyên.

## Current Task — Legacy Local Database Recovery (2026-10-14)

- [x] User duyệt backup/clone/repair giữ dữ liệu;import từ điển hoãn.
- [x] Catalog guard/explicit CLI/script/recovery regressions,review PASS local-only.
- [x] Backup restore rehearsal và Apply DB gốc;17 fingerprints/counts giữ nguyên,4 migrations/5grammar/2datasets.
- [x] Build0warning/error,79unit/164integration/251FE/typecheck và33target đạt;12F09acceptedhashes giữ nguyên.
- [x] Script startup2lần DB up-to-date;health200,APIv1/v2/v3 200,blank400 vàgRPC base-form smoke.
- [x] Docs/journal/memory/report cập nhật;backup vàDBrehearsal giữ local,không reset/import/commit/push/merge.

Recovery completed;dictionary0rows vẫn cần bước import riêng. Backend test của agent đã dừng;
người dùng chạy scripts/start-local.ps1 để dùng session riêng. Các lỗi Docker/timing và giới hạn
workflow hook/Ubuntu được ghi rõ,không coi fullsuitepass là latency certification.

## Current Task — Real Dictionary Import Specification and Plan (2026-10-15)

- [x] User chốt đầy đủ từ vựng, kanji, nghĩa Việt tương đương V1; duyệt Shadow Plan/Proceed chỉ viết tài liệu.
- [x] Đọc Constitution, pipeline V1, schema/usecase V2; nghiên cứu nguồn và giấy phép bằng metadata/documentation.
- [x] Viết SPEC/checklist tại `.sdd/specs/002-import-dictionary-data/`.
- [x] Viết PLAN/research/data-model/contracts/quickstart, kiểm tra traceability và safeguards.
- [x] Kiểm tra tài liệu/fingerprint249 runtime files/12 F09 bindings và cập nhật Project Memory.

### Issues Encountered — Import Planning

- OD-009 vẫn cần duyệt nguồn/license và semantic mapping FVDP/OVDP; scope approval không thay source clearance.
- Reader hiện không release-filter; `ReviewStatus` cũng chưa lọc gloss. Plan phải cô lập DB và chặn candidate chưa duyệt khỏi dữ liệu đang phục vụ.
- Không code/import/download dataset/DB/Git mutation trong nhiệm vụ này; dữ liệu F09 đã nghiệm thu giữ nguyên.
- Review sửa ordering: G0a cho tải audit, G0b verify/duyệt nguồn; P2 test importer bằng fixtures nhỏ, P3 review mapping offline, P4 mới full import với final lock. Nguồn Việt chưa clear thì không gọi đủ phạm vi hoàn tất.
