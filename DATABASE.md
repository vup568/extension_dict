# DATABASE.md — JP Reading Platform V2

## Mục đích

Tài liệu trung tâm về thiết kế cơ sở dữ liệu **PostgreSQL 16** cho hệ thống JP Reading Platform V2. Schema được quản lý bằng **EF Core Migrations** (ARCH-007).

---

## Tổng quan

| Thuộc tính | Giá trị |
|---|---|
| Database Engine | PostgreSQL 16 (ARCH-005) |
| Schema Management | EF Core Migrations (ARCH-007) |
| Integration Tests | Docker Testcontainers + PostgreSQL (ARCH-006) |
| Tổng số bảng | 18 |
| Nhóm nghiệp vụ | 5 |

### Phân nhóm bảng

| Nhóm | Bảng | Mô tả |
|---|---|---|
| **Knowledge Core** (6) | `dictionary_entries`, `written_forms`, `readings`, `dictionary_senses`, `localized_glosses`, `sense_applicabilities` | Dữ liệu từ điển canonical |
| **Kanji** (1) | `kanji_records` | Thông tin ký tự kanji |
| **Grammar** (1) | `grammar_rules` | Mẫu ngữ pháp canonical |
| **Knowledge Operations** (7) | `source_manifests`, `source_records`, `editorial_mappings`, `knowledge_releases`, `current_knowledge_release`, `knowledge_release_manifests`, `resource_revisions` | Provenance, versioning, import pipeline |
| **User & Learning** (3) | `users`, `user_external_logins`, `learning_references` | Account, OAuth, saved resources |

---

## Nguyên tắc thiết kế

- **PostgreSQL 16 Stack Rules:** `TIMESTAMPTZ`, `JSONB`, `BIGSERIAL`, `TEXT`, `UUID` — không dùng `VARCHAR(n)`.
- **Canonical ID:** Format typed toàn cục `dictionary:xxxxx`, `kanji:xxxxx`, `grammar:xxxxx` (ID-005).
- **Soft Delete:** `learning_references` dùng `deleted_at TIMESTAMPTZ NULL` + Partial Unique Index (LEARN-008).
- **Audit Columns:** Mọi bảng đều có `created_at`, `updated_at` với trigger auto-update.
- **Multilingual-capable:** `localized_glosses` dùng `language_tag` ("vi", "en") (I18N-003, I18N-004).
- **Structural Knowledge Release immutability:** Published rows và các revision/manifest memberships bị PostgreSQL guard không cho sửa hoặc xóa; per-release row locks serialize child writes với publish. `current_knowledge_release` giữ con trỏ current riêng và chỉ đổi trong controlled-publication transaction, không sửa snapshot cũ (DATA-005). Validation/approval pipeline và full provenance snapshot boundary vẫn là bước riêng.
- **Hybrid Auth:** Email/password (BCrypt/Argon2id) + Google OAuth (AUTH-003, SEC-005).
- **Clean Architecture:** Domain Entities thuần C# 14 — zero import EF Core/ORM (AGENTS.md §4.2).

---

## Tài liệu chi tiết

| Tài liệu | Vị trí | Nội dung |
|---|---|---|
| **Schema Overview** | [`docs/database/schema-overview.md`](docs/database/schema-overview.md) | Mô tả nghiệp vụ: vai trò từng bảng, relationships, constraints, core tables, review points |
| **Schema DBML** | [`docs/database/schema.dbml`](docs/database/schema.dbml) | Định nghĩa kỹ thuật đầy đủ — render trực quan trên [dbdiagram.io](https://dbdiagram.io) |
| **Domain Entities** | [`src/domain/Entities/`](src/domain/Entities/) | 18 C# POCO classes — thuần C# 14, không phụ thuộc EF Core |
| **EF Core Configurations** | [`src/infra/Persistence/Configurations/`](src/infra/Persistence/Configurations/) | 18 Fluent API configurations ánh xạ Entity → PostgreSQL |
| **DbContext** | [`src/infra/Persistence/AppDbContext.cs`](src/infra/Persistence/AppDbContext.cs) | EF Core DbContext chính với 18 DbSet |

---

## EF Core Migration Strategy

Schema được quản lý hoàn toàn qua EF Core Migrations (ARCH-007):

1. **Tạo migration:** `dotnet ef migrations add <MigrationName> --project src/infra`
2. **Áp dụng migration:** `dotnet ef database update --project src/infra`
3. **Rollback:** `dotnet ef database update <PreviousMigrationName>`
4. **Script SQL:** `dotnet ef migrations script --project src/infra`

Mọi migration đều được version-control trong Git. Database schema có thể tái tạo 100% từ migration history (AC-ARCH-007).

---

## Nguồn sự thật

| Tài liệu | Vai trò |
|---|---|
| [`REQUIREMENT.md`](REQUIREMENT.md) §6 | Conceptual Data Model — Entity Catalog, Relationships, Invariants |
| [`REQUIREMENT.md`](REQUIREMENT.md) §9–10 | Functional & Non-Functional Requirements |
| [`AGENTS.md`](AGENTS.md) §2 | Tech Stack chốt cứng — PostgreSQL 16, EF Core, Clean Architecture |
| [`MIGRATION_DECISION.md`](MIGRATION_DECISION.md) §5 | Dữ liệu cần Preserve/Migrate (JMdict, KANJIDIC2, Grammar, Vietnamese Overrides) |
| [`SDD.md`](SDD.md) | Project Memory — ADR, Lessons Learned |
