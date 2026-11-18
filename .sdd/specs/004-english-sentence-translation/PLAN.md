# Dịch câu Nhật–Anh theo yêu cầu

## Phạm vi

- Người dùng chủ động bấm dịch trong popup; hiển thị thông báo trước về việc gửi nội dung sang nhà cung cấp. Không dịch tự động, không làm gián đoạn tra từ.
- `POST /api/translations` nhận text, interaction_id và target_language `en`; trả ngôn ngữ nguồn `ja`, đích `en` và câu dịch. Đây là phần Nhật–Anh trước; Nhật–Việt để sau.
- Backend có port trung lập với provider, adapter HTTP chính thức DeepL Free/Pro, khóa qua biến môi trường, không gửi key/client hay ghi text vào log/database. Thiếu cấu hình/lỗi provider được báo rõ, không tạo bản dịch giả từ từ điển.
- Popup giữ trạng thái tải/thành công/lỗi riêng, hủy và loại phản hồi lỗi thời khi đổi vùng chọn hoặc đóng popup; giao tiếp qua background script.

## Kiểm chứng

- Test use case, endpoint, adapter bằng provider giả: hợp đồng, input, lỗi, timeout, cấu hình, cancellation, không rò text/key. Test frontend: không gọi dịch khi tra, gọi đúng khi click, lỗi không xóa tra cứu, stale response và đóng popup.
- Build backend và extension; test endpoint và popup với provider giả, cùng extension đóng gói với backend thật chưa có khóa. Kiểm thử DeepL live chỉ có thể thực hiện sau khi người dùng cấu hình khóa; không tuyên bố đã kiểm chứng dịch thật trước đó.
- Giữ contract analysis, dữ liệu JMdict/KANJIDIC2 và cấu hình hiện có; không cài dependency mới.

## Tiến độ

- [x] Xem xét kế hoạch và hợp đồng: validate UUID, chiều dài/target; timeout, không retry tự động, chặn redirect, chỉ cho phép host DeepL đã biết, không lộ lỗi thô.
- [x] Triển khai provider boundary, endpoint và kiểm thử backend.
- [x] Triển khai popup, transport và kiểm thử extension.
- [x] Review, build và kiểm thử tích hợp; hướng dẫn cấu hình DeepL.

## Kết quả và giới hạn

- 130 kiểm thử backend, 62 kiểm thử tích hợp, 287 kiểm thử extension, 5 kiểm thử popup và smoke test extension đóng gói khi backend chưa có khóa đạt.
- Chưa có khóa DeepL nên chưa thể xác minh dịch thật và endpoint phù hợp với gói tài khoản mới. Xem `docs/local-english-sentence-translation.md` để tự cấu hình khóa.
