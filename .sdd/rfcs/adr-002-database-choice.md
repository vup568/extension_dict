# ADR-002: Lựa chọn Hệ Quản trị Cơ sở Dữ liệu cho JP Reading Platform V2

*   **Status:** APPROVED
*   **Deciders:** Kỹ sư Trưởng (Con người) & AI Agent
*   **Date:** 2026-08-17

---

## 1. Bối cảnh (Context)

Dự án **JP Reading Platform V2** đang chuyển đổi từ kiến trúc Client-Side cũ (V1) sang kiến trúc tập trung **Server-Authoritative** [MIGRATION 6.1]. Hệ thống Backend mới được chốt là **.NET 10 (C#)** [ADR-001]. 

Hệ thống cần một giải pháp lưu trữ dữ liệu mạnh mẽ để giải quyết hai nhóm bài toán cốt lõi:
1.  **Dữ liệu từ điển gốc (Static/Canonical Language Data):** Bao gồm dữ liệu từ điển JMdict, Kanjidic2, các bộ quy tắc biến đổi từ (Conjugations) và 102 mẫu ngữ pháp N5–N4 [MIGRATION 5.3, 8.1]. Nhóm dữ liệu này có đặc thù là kích thước trung bình (vài trăm MB), ghi cực kỳ ít (chỉ ghi khi import/ETL dữ liệu mới) nhưng đọc cực kỳ nhiều (Read-Heavy) với tần suất cao và yêu cầu độ trễ cực thấp (<200ms) [REQ PERF-001].
2.  **Dữ liệu người dùng (Dynamic User Data):** Bao gồm thông tin tài khoản, lịch sử học tập, từ vựng và ngữ pháp đã lưu (My Vocabulary, My Grammar) [REQ 57]. Nhóm này yêu cầu tính toàn vẹn dữ liệu (Relational Integrity) và cách ly dữ liệu giữa các người dùng (User Isolation).

Công nghệ Database cần phải tương thích tuyệt đối với stack .NET 10, dễ dàng quản trị, tối ưu hóa truy vấn trực quan và có giải pháp chạy kiểm thử tự động (Auto Testing) nhanh chóng [MIGRATION 13.4].

---

## 2. Quyết định (Decision)

Dự án chính thức chốt phương án sử dụng **mô hình cơ sở dữ liệu lai (Hybrid Database Strategy)** đáp ứng hoàn hảo hai môi trường phát triển và kiểm thử:

1.  **Môi trường Development & Production:** Sử dụng **Microsoft SQL Server (MSSQL)** (phiên bản SQL Server Express hoặc Developer Edition cho môi trường cục bộ).
2.  **Công cụ Quản trị:** Sử dụng **SQL Server Management Studio (SSMS)** làm công cụ trực quan hóa, cấu hình, debug và tối ưu hóa chỉ mục (indexing) duy nhất.
3.  **Môi trường Integration Testing:** Sử dụng **SQLite (In-Memory)** kết hợp với cơ chế hoán đổi Provider của Entity Framework Core (EF Core) để đảm bảo các bài test chạy độc lập và tức thời (<100ms/test case) [MIGRATION 13.4].

---

## 3. Lý do lựa chọn (Rationale)

*   **Tương thích tối đa với Hệ sinh thái .NET 10:** Microsoft SQL Server là "gà nhà" của Microsoft, được tối ưu hóa sâu sắc nhất khi đi kèm với Entity Framework Core. Mọi tính năng tối ưu hóa truy vấn, biên dịch trước (Compiled Queries) và quản lý Connection Pool của .NET 10 đều hoạt động đạt hiệu suất cao nhất trên SQL Server.
*   **Sức mạnh quản trị vượt trội từ SSMS:** SQL Server Management Studio cung cấp trình phân tích kế hoạch thực thi câu lệnh (Execution Plan Visualizer) tốt nhất thế giới. Điều này giúp đội ngũ phát triển dễ dàng phát hiện ra các truy vấn tra từ điển bị nghẽn (Table Scan) và tối ưu hóa chỉ mục (Index Tuning) một cách trực quan, chính xác.
*   **Hỗ trợ ngôn ngữ tiếng Nhật (Japanese Full-Text Search):** SQL Server tích hợp sẵn bộ tách từ (Word Breaker) cho tiếng Nhật trong tính năng Full-Text Search. Điều này cho phép hệ thống thực hiện các truy vấn tìm kiếm gần đúng (fuzzy search) trên dữ liệu từ điển gốc cực kỳ hiệu quả mà không cần cài đặt thêm các engine tìm kiếm cồng kềnh bên ngoài.
*   **Chi phí MVP bằng 0:** SQL Server Express hoàn toàn miễn phí và giới hạn dung lượng database lên tới **10GB**. Do toàn bộ dữ liệu từ điển thô và dữ liệu người dùng giai đoạn đầu chỉ nặng dưới 1GB, giới hạn 10GB của bản Express là quá dư dả cho giai đoạn MVP.
*   **Tối ưu hóa tốc độ CI/CD:** Việc sử dụng SQLite In-Memory cho tầng Integration Test giúp loại bỏ hoàn toàn sự phụ thuộc vào việc khởi động một Container SQL Server nặng nề trên Cloud CI/CD, giảm thời gian phản hồi kiểm thử xuống mức tối đa [MIGRATION 13.4].

---

## 4. Hệ quả & Ràng buộc thực thi (Consequences & Constraints)

### 4.1. Thay đổi về mặt Kỹ thuật:
*   Đội ngũ phát triển cần cấu hình DbContext của EF Core để nhận diện môi trường:
    *   Nếu là `Test`, sử dụng `.UseSqlite("DataSource=:memory:")`.
    *   Nếu là `Dev/Prod`, sử dụng `.UseSqlServer(connectionString)`.
*   Phải đảm bảo mọi câu lệnh Migration sinh ra bởi EF Core đều tương thích ngược với cả SQL Server và SQLite (tránh sử dụng các kiểu dữ liệu đặc thù của MSSQL mà SQLite không hỗ trợ).

### 4.2. Ràng buộc kiểm soát Agent (EARS Constraints):
*   **THE** backend system **SHALL** execute all integration tests against an in-memory SQLite database to guarantee decoupled, sub-second test execution [MIGRATION 13.4].
*   **THE** backend database schema **SHALL** enforce strict relational foreign keys linking saved user items (vocabulary, grammar) to canonical dictionary entity IDs to protect referential integrity [MIGRATION 16].
*   **WHERE** the database credentials are required, **THE** system **SHALL** fetch them dynamically from encrypted environment variables, never hardcoding connection strings in source code [REQ 35].
