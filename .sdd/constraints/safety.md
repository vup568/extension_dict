# .sdd/constraints/safety.md — Ràng buộc An toàn & Guardrails cho AI (Safety Constraints)
# Phiên bản: 1.0.0 | Cập nhật: 2026-08-17 | Trách nhiệm: Tech Lead / Toàn đội ngũ
# Áp dụng cho: Mọi AI Agent hoạt động trong repository, các kịch bản thực thi Code & Terminal

Tài liệu này thiết lập **"Hàng rào an toàn cuối cùng" (Last Line of Defense)** để ngăn ngừa các hành vi phá hoại vô tình hoặc cố ý do AI Agents tự động thực hiện trong quá trình thiết kế, viết code, chạy lệnh terminal, và di trú dữ liệu [MIGRATION 13.5].

---

## 1. DATA SAFETY & DATABASE GUARDRAILS (An toàn Dữ liệu & Hệ quản trị DB)

### 1.1 Khóa Lệnh DROP và TRUNCATE (Database Destruction Block)
*   **AI Agent SHALL NOT** tự ý viết hoặc thực thi các câu lệnh `DROP TABLE`, `DROP DATABASE`, hoặc `TRUNCATE TABLE` trong các tệp tin di trú (migrations) hoặc lệnh terminal mà không có sự phê duyệt tường minh bằng văn bản từ con người.
*   Mọi câu lệnh thay đổi cấu trúc bảng (alter column type) có rủi ro làm mất mát dữ liệu hiện tại trên môi trường SQL Server bắt buộc phải được chuyển giao quyền thực thi cho kỹ sư con người [MIGRATION 13.5].

### 1.2 Cấm Lệnh UPDATE/DELETE Không Điều Kiện (Blind Modification Block)
*   **AI Agent SHALL NOT** viết bất kỳ câu lệnh SQL `UPDATE` hoặc `DELETE` nào mà không đi kèm với điều kiện ranh giới `WHERE` cụ thể.
*   **Enforcement:** Hệ thống CI/CD check **SHALL** tự động quét toàn bộ mã nguồn PR và báo lỗi "CONSTITUTION VIOLATION" nếu phát hiện lệnh sửa đổi bảng trống điều kiện.

### 1.3 Bảo vệ Thư mục Dữ liệu gốc (Reference Data Protection)
*   **AI Agent SHALL NOT** thực hiện hành động xóa (delete) hoặc đổi tên (rename) các tệp tin trong thư mục chứa dữ liệu từ điển thô, các bộ quy tắc biến đổi từ (conjugation constants) hay dữ liệu kiểm thử hồi quy ngôn ngữ học [MIGRATION 4.2].
*   Dữ liệu gốc là "nguồn sự thật" của dự án kế thừa từ V1, chỉ được phép di trú sang cấu trúc mới (Migrate as Data) chứ không được xóa bỏ [MIGRATION 5].

---

## 2. REPOSITORY & PACKAGE SAFETY (An toàn Mã nguồn & Thư viện ngoài)

### 2.1 Cấm Tự ý Cài đặt Thư viện ngoài (Dependency Lock)
*   **AI Agent SHALL NOT** tự ý cài đặt thêm bất kỳ thư viện ngoài nào thông qua NuGet (backend) hoặc npm (frontend) vào các file cấu hình dự án (`.csproj`, `package.json`) khi chưa có sự xác nhận rõ ràng từ con người [MIGRATION 19].
*   Khi phát hiện hệ thống thiếu thư viện xử lý, AI Agent **SHALL** dừng lại, đưa ra lý do tại sao cần cài đặt thư viện đó, phân tích các rủi ro bảo mật đi kèm (nếu có), và đợi con người gõ "Approved" mới được tiến hành cài đặt.

### 2.2 Cấm Ghi đè vào các Nhánh Bảo vệ (Protected Branches Isolation)
*   **AI Agent SHALL NOT** thực hiện hành động push hoặc commit mã nguồn trực tiếp vào các nhánh Git được bảo vệ (`main`, `master`, `develop`) [MIGRATION 4.1].
*   Mọi mã nguồn do AI Agent tạo ra bắt buộc phải nằm trên nhánh tính năng riêng biệt (ví dụ: `agent/feat-dict-lookup`) và chỉ được tích hợp thông qua quy trình tạo Pull Request (PR) để con người kiểm duyệt [MIGRATION 4.1].

### 2.3 Khóa cấu hình CI/CD và Script Bảo mật (Security Isolation)
*   **AI Agent SHALL NOT** chỉnh sửa bất kỳ tệp tin cấu hình tự động hóa nào (ví dụ: các file GitHub Actions `.github/workflows/*`, cấu hình pre-commit hooks) hoặc các file mô tả cấu hình an toàn cho chính Agent (`AGENTS.md`, `SDD.md`, `.sdd/constitution.md`) [MIGRATION 4.2].

---

## 3. REFFACTORING & RUNTIME ROLLBACK (Quy tắc Tái cấu trúc)

### 3.1 Git Checkpoint Bắt buộc (Safety Net)
*   Trước khi tiến hành các tác vụ tái cấu trúc (refactoring) quy mô lớn (thay đổi trên 5 tệp tin hoặc chỉnh sửa các lớp Domain lõi), **AI Agent SHALL** kiểm tra trạng thái Git hiện tại [MIGRATION 11].
*   Nếu có thay đổi chưa được lưu, AI Agent **SHALL** yêu cầu con người tạo commit checkpoint hoặc tự động thực hiện lệnh `git stash` để đảm bảo có thể rollback (hoàn tác) 100% về trạng thái an toàn gần nhất nếu quá trình refactor phát sinh lỗi hàng loạt [MIGRATION 11].

### 3.2 Loop Trap Interruption Protocol (Chặn lỗi lặp vô hạn)
*   Nếu trong quá trình tự động sửa lỗi và chạy thử nghiệm (Plan-Act-Check loop), AI Agent gặp cùng một lỗi biên dịch hoặc lỗi kiểm thử thất bại **quá 3 lần liên tiếp** trên cùng một module, **AI Agent SHALL**:
    1.  Dừng toàn bộ quá trình tự động sửa đổi.
    2.  Hủy bỏ các thay đổi tạm thời gây lỗi và rollback code về checkpoint Git gần nhất [MIGRATION 11].
    3.  Lập báo cáo phân tích nguyên nhân tại sao lỗi lặp lại liên tục và yêu cầu sự hỗ trợ từ kỹ sư con người (Human-in-the-loop) [MIGRATION 6.6].

---

## 4. PROTOCOL KHI KHÔNG CHẮC CHẮN (Stop-and-Ask Protocol)
*   **Nguyên tắc vàng:** "Hỏi để chậm rãi nhưng đúng đắn luôn tốt hơn tự giả định dẫn đến code sai" [REQ 35].
*   Mỗi khi gặp một ranh giới nghiệp vụ mập mờ không có trong spec, một lỗi môi trường bất thường, hoặc mâu thuẫn giữa hai tài liệu đặc tả, **AI Agent SHALL NOT tự ý đoán** [MIGRATION 5.1.2]. 
*   AI Agent **SHALL** dừng lại ngay lập tức, hiển thị rõ ràng những điểm mâu thuẫn và đưa ra câu hỏi có cấu trúc cụ thể để con người dễ dàng trả lời (ví dụ: lựa chọn dạng Yes/No hoặc trắc nghiệm phương án) trước khi tiếp tục thực thi hành động [MIGRATION 6.3].
