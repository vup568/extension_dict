# Database Schema Overview — JP Reading Platform V2

> **Database:** PostgreSQL 16 | **Schema Management:** EF Core Migrations (ARCH-007)
> **Tổng cộng:** 18 bảng / 5 nhóm nghiệp vụ

---

## 1. Database Domains / Modules

### 1.1 Knowledge Core (6 bảng)

Nhóm trung tâm chứa toàn bộ dữ liệu từ điển canonical — là nền tảng cho mọi tính năng tra cứu, phân tích và học tập.

| Bảng | Vai trò |
|---|---|
| `dictionary_entries` | **Bảng trung tâm.** Mỗi row là một mục từ canonical có `canonical_id` toàn cục dạng `dictionary:xxxxx`. Không chứa text trực tiếp — text nằm ở written_forms/readings/senses. |
| `written_forms` | Dạng biểu ký (orthography) của entry, ví dụ `食べる`, `たべる`. Một entry có nhiều form. Có cờ `is_common` và thông tin bổ sung `info` (ateji, irregular kana). |
| `readings` | Dạng kana (đọc) của entry. Có thể bị giới hạn cho một số form cụ thể qua `restricted_to_forms` (JMdict `re_restr`). |
| `dictionary_senses` | Đơn vị ngữ nghĩa language-neutral. Mỗi sense có `sense_key` ổn định, `part_of_speech` (TEXT array) và thứ tự `position`. |
| `localized_glosses` | Nghĩa đã localized theo `language_tag` ("vi" hoặc "en"). Có `source_record_id` để truy vết nguồn gốc (provenance) và `review_status` cho editorial workflow. |
| `sense_applicabilities` | Bảng restriction: sense nào áp dụng cho form/reading nào. Không có row = sense áp dụng cho tất cả. Đảm bảo không gắn sai nghĩa cho form không tương thích (VOC-007, VOC-008). |

### 1.2 Kanji (1 bảng)

| Bảng | Vai trò |
|---|---|
| `kanji_records` | Record canonical cho từng ký tự kanji. Phân biệt rõ giá trị authoritative (grade, stroke_count từ KANJIDIC2) và giá trị derived/approximate (jlpt_level) qua `jlpt_provenance` (KAN-002). Lưu readings dạng TEXT array vì số lượng ít và luôn hiển thị cùng character. |

### 1.3 Grammar (1 bảng)

| Bảng | Vai trò |
|---|---|
| `grammar_rules` | Canonical grammar pattern (ví dụ `～ている`). Meaning inline (`meaning_vi`, `meaning_en`) vì chỉ ~100 rules. `matcher_metadata JSONB` linh hoạt cho engine phát hiện pattern (OD-008). `examples JSONB` chứa ví dụ có cấu trúc. Grammar knowledge là versioned data tách khỏi matching engine (GRM-005). |

### 1.4 Knowledge Operations (7 bảng)

Nhóm quản lý provenance, phiên bản và quy trình import dữ liệu ngôn ngữ.

| Bảng | Vai trò |
|---|---|
| `source_manifests` | Metadata provenance của một dataset release (ví dụ: JMdict v2026-08, KANJIDIC2 v2026). Ghi nhận publisher, version, license, checksum, pipeline version (DATA-001). |
| `source_records` | Từng record gốc trong một manifest. `record_data JSONB` giữ nguyên dữ liệu upstream. `source_identity` là upstream ID (ví dụ JMdict `ent_seq`). |
| `editorial_mappings` | Quyết định editorial liên kết source record với canonical resource. Polymorphic FK qua `target_resource_type` + `target_resource_id`. Mapping phải được review — không tự suy ra từ form/reading trùng (ID-004). |
| `knowledge_releases` | Candidate/published snapshot. Published row bị PostgreSQL trigger chặn UPDATE/DELETE; structural publication yêu cầu ít nhất một manifest membership (DATA-005). DATA-001 validation/approval gate chưa được triển khai trong schema này. |
| `current_knowledge_release` | Singleton pointer tới published release hiện hành. Tách current-status khỏi snapshot; trigger chỉ cho phép đổi trong controlled-publication transaction và không cho xóa pointer. |
| `knowledge_release_manifests` | Bảng trung gian M:N giữa release và manifest. `ON DELETE RESTRICT` cho manifest để ngăn xóa manifest đang thuộc release đã publish. |
| `resource_revisions` | Snapshot dữ liệu của một canonical resource trong một release. `revision_data JSONB` chứa facts tại thời điểm publish. Immutable sau khi publish (ID-006). |

### 1.5 User & Learning (3 bảng)

| Bảng | Vai trò |
|---|---|
| `users` | Account người dùng. Hybrid auth: `password_hash` (BCrypt/Argon2id, NULL cho OAuth-only) + Google OAuth qua `user_external_logins`. Email unique. |
| `user_external_logins` | Liên kết OAuth. Unique `(provider, provider_user_id)` ngăn 2 account dùng cùng Google ID. Một user có thể link nhiều provider. |
| `learning_references` | Saved vocabulary/grammar của user. Reference canonical ID (không dùng text làm identity). **Soft delete** qua `deleted_at`. Partial unique index `WHERE deleted_at IS NULL` đảm bảo không duplicate active items (LEARN-007). |

---

## 2. Key Relationships & Cardinalities

```
dictionary_entries (1) ──→ (N) written_forms        CASCADE
dictionary_entries (1) ──→ (N) readings              CASCADE
dictionary_entries (1) ──→ (N) dictionary_senses     CASCADE
dictionary_senses  (1) ──→ (N) localized_glosses     CASCADE
dictionary_senses  (1) ──→ (N) sense_applicabilities CASCADE
source_manifests   (1) ──→ (N) source_records        CASCADE
source_records     (1) ──→ (N) editorial_mappings    CASCADE
source_records     (1) ──→ (N) localized_glosses     SET NULL
knowledge_releases (M) ←──→ (N) source_manifests     via knowledge_release_manifests
current_knowledge_release (1) ──→ (1) knowledge_releases RESTRICT
knowledge_releases (1) ──→ (N) resource_revisions    RESTRICT
users              (1) ──→ (N) user_external_logins  CASCADE
users              (1) ──→ (N) learning_references   CASCADE
```

---

## 3. Important Constraints

| Loại | Bảng | Constraint | Mục đích |
|---|---|---|---|
| Singleton + FK | `current_knowledge_release` | `singleton_id = 1`, unique `release_id` | Tối đa một current pointer, chỉ trỏ tới release tồn tại |
| Trigger | `current_knowledge_release` | controlled transaction + target `published_at IS NOT NULL` | Draft/thao tác ngoài publication không thể đổi current; pointer không thể bị xóa |
| Trigger + row lock | release/revisions/manifests | serialize child write với publish; block mutation after publish | Published snapshot không thể sửa/xóa hoặc thêm/bớt child, kể cả transaction cạnh tranh |
| Partial Unique | `learning_references` | `WHERE deleted_at IS NULL` | Không duplicate active items (LEARN-007) |
| CHECK | `localized_glosses` | `language_tag IN ('vi','en')` | Chỉ chấp nhận 2 ngôn ngữ MVP |
| CHECK | `grammar_rules` | `jlpt_level IN ('N5'...'N1')` | Validate JLPT level |
| CHECK | `sense_applicabilities` | `written_form_id IS NOT NULL OR reading_id IS NOT NULL` | Ít nhất 1 target |
| CHECK | `editorial_mappings` | `target_resource_type IN ('dictionary','kanji','grammar')` | Validate resource type |
| CHECK | `editorial_mappings` | `mapping_type IN ('link','merge','split','retire')` | Validate mapping type |
| CHECK | `learning_references` | `resource_type IN ('dictionary','grammar')` | Validate resource type |
| CHECK | `kanji_records` | `jlpt_provenance IN ('authoritative','derived','approximate')` | Validate provenance |

---

## 4. High-Connectivity / Core Tables

Các bảng sau là **trung tâm** của schema, bị tham chiếu nhiều nhất:

1. **`dictionary_entries`** — Được tham chiếu bởi `written_forms`, `readings`, `dictionary_senses`, `editorial_mappings` (gián tiếp), `learning_references` (gián tiếp). Là bảng quan trọng nhất trong hệ thống.
2. **`dictionary_senses`** — Được tham chiếu bởi `localized_glosses`, `sense_applicabilities`. Là đơn vị ngữ nghĩa cốt lõi.
3. **`users`** — Được tham chiếu bởi `user_external_logins`, `learning_references`. Là trung tâm của tầng user.
4. **`source_records`** — Được tham chiếu bởi `editorial_mappings`, `localized_glosses`. Là trung tâm provenance.
5. **`knowledge_releases`** — Được tham chiếu bởi `knowledge_release_manifests`, `resource_revisions`. Là trung tâm versioning.

---

## 5. Potential Review Points

> [!NOTE]
> Các điểm dưới đây cần được human review trước khi đi vào production implementation.

1. **Polymorphic FK (`editorial_mappings`, `resource_revisions`, `learning_references`):** Các bảng này dùng `resource_type` + `resource_id` thay vì FK trực tiếp. DB không enforce referential integrity — cần validate ở application layer.

2. **`readings.restricted_to_forms TEXT[]`:** Dùng TEXT array thay vì join table riêng. Đơn giản hơn nhưng không có FK constraint tới `written_forms.form`. Phù hợp vì restriction hiếm khi xảy ra và dữ liệu là read-heavy.

3. **`sense_applicabilities` unique index với NULL:** Index `(sense_id, written_form_id, reading_id)` — PostgreSQL coi NULL là distinct trong unique index. Cần verify behavior khi cả hai đều NULL (bị chặn bởi CHECK constraint).

4. **JSONB columns (`examples`, `matcher_metadata`, `source_metadata`, `record_data`, `revision_data`):** Flexible nhưng không có schema validation ở DB level. Cần JSON schema validation ở application layer cho các trường quan trọng.

5. **`updated_at` auto-update:** DDL dùng PostgreSQL trigger function `trg_set_updated_at()`. Trong EF Core, cần đảm bảo trigger hoặc `SaveChanges` interceptor xử lý tương đương.
