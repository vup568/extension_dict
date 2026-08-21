# .sdd/constraints/business.md — Ràng buộc Nghiệp vụ & Tri thức Miền (Business Constraints)
# Phiên bản: 1.0.0 | Cập nhật: 2026-08-17 | Trách nhiệm: Tech Lead / Product Owner
# Áp dụng cho: Mọi đặc tả tính năng (Spec), Thiết kế Database Schema, API Contracts, và Logic code

## 1. AUTHENTICATION & ACCESS CONTROL BOUNDARIES (Quy tắc Phân quyền)
Để đảm bảo trải nghiệm người dùng liền mạch và mượt mà nhất (triết lý "Reading First"), hệ thống phân cấp quyền truy cập thành hai vùng rõ rệt [REQ P-05]:

### 1.1 Tính năng Đọc & Tra cứu (Anonymous Allowed — Không yêu cầu đăng nhập)
*   **THE system SHALL** cho phép người dùng ẩn danh (chưa đăng nhập) thực hiện mọi tính năng đọc và tra cứu [REQ AUTH-001].
*   Bao gồm: Tra cứu từ điển (exact lookup), phân tích morphology, tra kanji, hiển thị biến đổi từ (conjugation), phát hiện ngữ pháp (grammar detection) và gọi dịch thuật on-demand [REQ AUTH-001].

### 1.2 Tính năng Lưu trữ & Cá nhân hóa (Authentication Required — Bắt buộc đăng nhập)
*   **THE system SHALL** yêu cầu người dùng xác thực tài khoản thành công trước khi thực hiện các thao tác thay đổi dữ liệu hoặc cá nhân hóa [REQ AUTH-002].
*   Bao gồm: Lưu từ vựng vào sổ tay (Save Vocabulary), lưu mẫu ngữ pháp (Save Grammar), và xem lịch sử học tập cá nhân [REQ AUTH-002, AUTH-003].
*   **THE system SHALL** đảm bảo tính cô lập dữ liệu người dùng (user isolation). Tài khoản này tuyệt đối không được phép đọc hoặc sửa đổi kho từ vựng/ngữ pháp của tài khoản khác [REQ PRIV-005, PRIV-006].

---

## 2. LINGUISTIC ACCURACY & CORRECTNESS (Luật Ngôn ngữ học bắt buộc)
Để bảo toàn tính chính xác tuyệt đối của dữ liệu ngôn ngữ học kế thừa từ V1, mã nguồn V2 phải tuân thủ nghiêm ngặt các ràng buộc sau [MIGRATION 2]:

### 2.1 Ruby-Safe DOM Selection Normalization (Bảo vệ vùng chọn khỏi Furigana)
*   **WHEN** văn bản tiếng Nhật được bóc tách từ DOM của trình duyệt để gửi lên Backend phân tích, **THE extension SHALL** tự động bỏ qua toàn bộ thẻ chú thích Ruby (`<rt>`, `<rp>`) [MIGRATION 5.8, REQ 59].
*   **THE system SHALL** bảo toàn nguyên vẹn văn bản cơ sở (base text) để tránh furigana làm nhiễu dữ liệu đầu vào của bộ tách từ (Tokenizer) [MIGRATION 5.8, REQ 59].

### 2.2 Stable Canonical Identifiers (Bảo toàn định danh từ điển)
*   Dữ liệu từ điển (JMdict, Kanjidic2) và mẫu ngữ pháp (Grammar Rules) được lưu trữ và tiếp xúc qua API bắt buộc phải sử dụng định danh canonical duy nhất và ổn định của Backend [MIGRATION 16, REQ 57].
*   **THE system SHALL** lưu trữ các tài nguyên học tập cá nhân của người dùng dưới dạng liên kết tham chiếu đến định danh canonical này, tuyệt đối không lưu dưới dạng chuỗi text thô hiển thị trên UI [MIGRATION 16]. Điều này giúp bảo vệ thư viện cá nhân của người dùng không bị hỏng liên kết (broken links) kể cả khi ban biên tập chỉnh sửa nội dung từ điển nền tảng [MIGRATION 16].

### 2.3 Dictionary Match Provenance (Nguồn gốc khớp từ vựng)
*   Bộ công cụ tra cứu từ điển trên Backend **SHALL** bảo toàn thông tin nguồn gốc khớp (match provenance) cho từng kết quả [MIGRATION 17, REQ 60]. 
*   Bao gồm: Từ gốc được viết thế nào (written form), cách đọc tương ứng (reading), phân nghĩa áp dụng (sense matches) và các ràng buộc ngữ cảnh đi kèm của từ điển (form restrictions) [MIGRATION 17, REQ 60]. Không gộp chung nghĩa của các từ đồng âm khác nghĩa một cách vô căn cứ.

---

## 3. ASYNCHRONOUS & RUNTIME SAFETY (Ràng buộc Thời gian chạy)

### 3.1 Stale Asynchronous Response Protection (Hủy Request trễ)
*   **WHEN** người dùng thực hiện bôi chọn (select) một vùng văn bản mới trong khi request phân tích của vùng chọn cũ trước đó chưa hoàn thành, **THE extension SHALL** ngay lập tức kích hoạt cơ chế hủy bỏ (cancel) request cũ [MIGRATION 6.10, REQ 60].
*   **THE client UI SHALL** bỏ qua các phản hồi trễ của request cũ để ngăn ngừa hiện tượng race condition ghi đè đè dữ liệu sai lệch lên popup shell của vùng chọn mới [MIGRATION 6.10, REQ 60].

### 3.2 Idempotent Learning Saves (Lưu dữ liệu trùng lặp)
*   Mọi API nghiệp vụ thực hiện lưu tài nguyên (Save Vocabulary/Grammar) bắt buộc phải được thiết kế theo cơ chế **Idempotent** [REQ LEARN-007].
*   **WHERE** người dùng cố tình gửi yêu cầu lưu một từ vựng đã tồn tại sẵn trong sổ tay cá nhân của họ, **THE system SHALL** bỏ qua thao tác tạo bản ghi mới, cập nhật lại trường thời gian tương tác gần nhất (nếu cần), và trả về mã thành công (HTTP 200 OK) thay vì báo lỗi trùng lặp (Duplicate Error) [REQ LEARN-007].

---

## 4. DATA RETENTION & SECURITY (Ràng buộc Dữ liệu & Bảo mật)

### 4.1 Soft Delete Policy (Quy tắc xóa mềm)
*   **THE database schema SHALL** sử dụng trường thời gian xóa mềm (`DeletedAt` kiểu Nullable DateTime) cho mọi bảng dữ liệu quan trọng liên quan đến người dùng và tài nguyên học tập cá nhân (users, saved_vocabularies, saved_grammars) [MIGRATION 13.5].
*   Nghiêm cấm thực hiện lệnh xóa cứng (`DELETE`) trực tiếp trên database của môi trường production. Xóa cứng chỉ được phép áp dụng cho dữ liệu logs quá hạn hoặc dữ liệu tạm thời (temp files) [MIGRATION 13.5].

### 4.2 PII Protection & Operational Logging (Bảo vệ thông tin cá nhân)
*   **THE system SHALL NOT** ghi nhận hoặc lưu trữ (persist) nội dung văn bản tiếng Nhật thô (raw selected text) do người dùng bôi chọn vào tệp tin logs hệ thống mặc định để bảo vệ quyền riêng tư [REQ PRIV-002, PRIV-003].
*   Logs vận hành chỉ được phép lưu trữ metadata phi cá nhân (như độ dài văn bản, thời gian phản hồi, mã lỗi, ID người dùng đã mã hóa) [REQ PRIV-003].
*   Thông tin cá nhân nhạy cảm của người dùng (như email, số điện thoại đăng ký) khi ghi vào log bắt buộc phải được che (masking) theo đúng chuẩn: email -> `us***@domain.com`, số điện thoại -> `0912***456`.

### 4.3 Structured Machine-Readable Error Responses (Lỗi có cấu trúc)
*   Mọi API Endpoint khi xảy ra lỗi ở ranh giới hệ thống **SHALL** trả về lỗi dưới dạng một cấu trúc JSON đồng nhất có thể phân tích được bằng máy thay vì trả về text thô hoặc html lỗi [MIGRATION 6.12, REQ 60]:
    ```json
    {
      "error_code": "RESOURCE_NOT_FOUND",
      "message": "Không tìm thấy từ vựng tương ứng trong hệ thống.",
      "request_id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"
    }
    ```
*   **THE system SHALL NOT** trả trực tiếp thông tin stack trace hoặc chi tiết lỗi của hệ quản trị SQL Server về phía Client UI để tránh nguy cơ rò rỉ lỗ hổng bảo mật [MIGRATION 6.12].
