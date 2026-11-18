# Feature Specification: <Tên tính năng>

Ngày: <YYYY-MM-DD>. Phiên bản: <version>. Trạng thái: Draft / Approved / Implemented / Accepted — chỉ chọn trạng thái có bằng chứng tương ứng.

Kế thừa: <link yêu cầu tổng, thiết kế, spec liên quan>. Các mặc định kỹ thuật chưa được chốt phải ghi là đề xuất.

## 1. Context & Goal — Bối cảnh và mục tiêu

- Bài toán và lý do tính năng tồn tại; người dùng cần đạt điều gì?
- Hiện trạng đã xác minh, khoảng thiếu và kết quả mong muốn; nêu nguồn/file liên quan khi có.

## 2. Actors & Roles — Tác nhân và quyền

| Tác nhân | Hành động/trách nhiệm | Quyền và giới hạn |
|---|---|---|
| <Reader/operator/service> | <Thao tác> | <Ẩn danh/xác thực/ownership/quyền hệ thống phù hợp> |

## 3. Functional Requirements — Yêu cầu chức năng

| ID | Điều kiện và hành vi quan sát được |
|---|---|
| FR-001 | WHEN <trigger>, hệ thống SHALL <behavior>. |

Mô tả luồng chính, trạng thái và tương tác với chức năng hiện có; không tự mở rộng phạm vi.

## 4. Non-functional Requirements — Chất lượng và ràng buộc

| Khía cạnh | Yêu cầu đo/kiểm chứng được | Cách kiểm chứng |
|---|---|---|
| <Hiệu năng/bảo mật/quyền riêng tư/tin cậy/accessibility> | <Giới hạn có căn cứ, hoặc đề xuất cần chốt> | <Profile/test/report> |

Nêu tương thích, ràng buộc kiến trúc, rủi ro và tiêu chí đo thực tế. Không biến best guess thành cam kết đã duyệt.

## 5. Data Model — Cấu trúc dữ liệu

| Đối tượng | Trường/identity/quan hệ | Validation và vòng đời |
|---|---|---|
| <Entity/DTO/state/preference> | <Trường và kiểu liên quan> | <Nguồn dữ liệu, lưu ở đâu, khi nào xóa> |

Phân biệt entity lưu lâu dài với DTO/state tạm thời; nêu migration có cần hay không. Link contract/schema đã có để tránh tự thiết kế lại.

## 6. Error Handling — Xử lý lỗi

| Tình huống | Phản hồi/trạng thái | Phục hồi và dữ liệu cần bảo toàn |
|---|---|---|
| <Input sai/network/timeout/quota/forbidden/cancel phù hợp> | <Error code/UI state an toàn> | <Retry có chủ động? hủy? giữ kết quả nào?> |

Nêu xử lý phản hồi cũ, lỗi từng phần, cleanup và giới hạn retry nếu liên quan; không chỉ có happy path.

## 7. Acceptance Criteria — Tiêu chí nghiệm thu

| AC | Given/When/Then hoặc input → output cụ thể | Requirement và bằng chứng |
|---|---|---|
| AC-001 | <Tình huống kiểm tra được> | <FR/NFR/ID và test/report dự kiến> |

Định nghĩa khi nào được gọi là hoàn thành. Chưa chạy hoặc chưa có bằng chứng thì ghi chưa xác minh.

## 8. Out of Scope — Ngoài phạm vi

- <Các chức năng/đối tượng/nền tảng không thuộc lần này và ranh giới với module khác>.

Nếu một mục thực sự không áp dụng, ghi ngắn lý do; không tạo schema, quyền hoặc công việc giả chỉ để lấp mẫu. Giữ thứ tự các mục còn áp dụng.
