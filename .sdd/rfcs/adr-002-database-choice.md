# ADR-002: Lựa chọn Hệ Quản trị Cơ sở Dữ liệu cho JP Reading Platform V2

*   **Status:** APPROVED
*   **Deciders:** VuPM (Product Owner) & AI Agent
*   **Date:** 2026-08-21

---

## 1. Bối cảnh (Context)

JP Reading Platform V2 chuyển từ kiến trúc Client-Side V1 sang Backend Server-Authoritative trên .NET 10 [MIGRATION 6.1, ADR-001]. Hệ thống lưu hai nhóm dữ liệu:

1. **Dữ liệu ngôn ngữ canonical:** JMdict, Kanjidic2, conjugations và grammar corpus. Đây là workload đọc nhiều, nạp qua ETL theo release và cần truy vấn độ trễ thấp.
2. **Dữ liệu người dùng:** tài khoản, tiến độ học, vocabulary và grammar đã lưu. Nhóm này yêu cầu relational integrity, user isolation và soft delete theo policy.

Schema đã chọn các semantics đặc thù PostgreSQL như `JSONB`, `TIMESTAMPTZ`, `TEXT[]`, partial unique index và biểu thức index. Vì vậy test với một database engine khác sẽ không chứng minh đúng hành vi production.

---

## 2. Quyết định (Decision)

Dự án chính thức sử dụng một baseline PostgreSQL thống nhất:

1. **Development & Production:** PostgreSQL 16 là RDBMS chính.
2. **ORM và schema:** Entity Framework Core sử dụng provider `Npgsql.EntityFrameworkCore.PostgreSQL`; schema được quản lý bằng EF Core Migrations sinh cho PostgreSQL.
3. **Integration testing:** Các integration test có phụ thuộc database chạy PostgreSQL 16 thật qua Docker Testcontainers. Unit test vẫn phải cô lập database.
4. **Configuration:** Connection string phải lấy từ biến môi trường hoặc secret store; không commit credential hay connection string thật vào repository.

SQL Server và SQLite in-memory không phải provider được hỗ trợ cho runtime, migrations hay integration test của V2.

---

## 3. Lý do lựa chọn (Rationale)

* **Nhất quán provider:** Migration, query translation và test chạy trên cùng PostgreSQL engine, nên phát hiện được khác biệt về transaction, index, collation và data type trước khi release.
* **Phù hợp schema V2:** PostgreSQL hỗ trợ trực tiếp `JSONB`, `TIMESTAMPTZ`, `TEXT[]` và partial index mà schema hiện hành cần; không bắt hệ thống hạ thấp thiết kế để tương thích SQLite.
* **Môi trường lặp lại được:** PostgreSQL 16 container hóa giúp máy local và CI có cùng major version với runtime dự kiến.
* **EF Core vẫn là abstraction chính:** Provider Npgsql tích hợp trực tiếp với EF Core, nên domain/application không phụ thuộc database driver; chi tiết provider ở Infrastructure.

---

## 4. Hệ quả & Ràng buộc thực thi (Consequences & Constraints)

### 4.1. Thay đổi kỹ thuật

* DbContext ở Infrastructure cấu hình `.UseNpgsql(connectionString)`.
* EF Core Migrations được tạo, review và apply cho PostgreSQL; được phép biểu đạt các kiểu dữ liệu và index semantics PostgreSQL khi schema yêu cầu.
* Integration suite khởi tạo PostgreSQL 16 bằng Testcontainers, apply migrations và kiểm tra query/mapping phụ thuộc provider.
* Không dùng SQLite in-memory để xác nhận behavior phụ thuộc provider; các test thuần nghiệp vụ phải là unit test và dùng mock/fake interface.

### 4.2. Ràng buộc kiểm soát (EARS Constraints)

* **THE** backend system **SHALL** execute database-dependent integration tests against PostgreSQL 16 through Docker Testcontainers [ARCH-006].
* **THE** backend database schema **SHALL** be managed through EF Core Migrations using the PostgreSQL provider [ARCH-007].
* **THE** backend database schema **SHALL** enforce strict relational foreign keys linking saved user items to canonical dictionary entity IDs where the relationship is relationally representable [MIGRATION 16].
* **WHERE** database credentials are required, **THE** system **SHALL** obtain them from environment variables or a secret store and never hardcode them in source control [SEC-002].
