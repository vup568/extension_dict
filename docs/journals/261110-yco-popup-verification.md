# Đối chiếu thể chia YCO và kiểm chứng popup

Ngày 10/11/2026. T16 đã chốt catalog `yco-gap-20261110.1` sau rà soát độc lập. Catalog đối chiếu các nhóm thể chia, span UTF‑16, từ lặp và giới hạn cặp dạng viết–cách đọc với Irodori, NINJAL, Bunkachō và JMdict. Baseline F‑09 `f09-expanded-20261013.3` khớp cả 12 SHA‑256 trong manifest nghiệm thu. [Gap catalog](../../plans/261104-yomitan-reading-experience/evidence/conjugation-gap-catalog.md)

- `行った` có thể là quá khứ của cả `行く` và `行う`. Ca kiểm cho `行く` loại dạng biến âm đều `行いた` và giữ `行う` như một ứng viên riêng khi có chứng cứ.
- Phạm vi đã chốt cho `Vて/で + しまう` gồm dạng thường khẳng định không quá khứ và quá khứ. Sắc thái ngoài ý muốn cần ngữ cảnh. Ca đo với JMdict và Sudachi cho `食べてしまった` tìm được mục `食べる` mà phần giải thích thể chia còn trống.

T17 đã khóa corpus riêng `yco-20261110.1` sau rà soát độc lập. Corpus gồm 21 ca thuộc 9 nhóm và trỏ trực tiếp đến 14 ca F‑09. Validator qua 9 phép thử phát hiện sai lệch và xác minh 12 hash baseline. Các kỳ vọng dành cho bộ phân tích được chạy ở T18/T19. [Corpus T17](../../.sdd/specs/005-yomitan-reading-experience/corpus/yco-20261110.1/README.md)

T18 tạo `candidate-20261110.1` với bốn biến thể `て/で + しまう` và hai rule ID. Kết quả biên dịch giữ đủ 909 biến thể F‑09. Rà soát độc lập đạt. Bộ test ứng viên đạt 9/9 với dữ liệu thay thế, và gRPC Sudachi thật được quan sát riêng trên 17 đầu vào tổng hợp. T19 sẽ quyết định cách cài và chọn dataset phù hợp với xác minh lịch sử migration, rồi kiểm luồng API, PostgreSQL và popup. [Bằng chứng T18](../../plans/261104-yomitan-reading-experience/evidence/test-report.md#t18--kiểm-chứng-dataset-ứng-viên-ngày-10112026)

T10 đã sửa vị trí popup khi `body` có CSS transform và `showPopover` thiếu hoặc ném lỗi. Lượt hồi quy đạt 134 ca trên Chrome, Edge và Brave, gồm 17 ca T10 tập trung. Extension đóng gói trên Edge và Brave qua kiểm tra ranh giới frame và lưu kích thước. Một ca đóng gói được cấu hình bỏ qua trên Chrome. Môi trường kiểm thử thiếu Firefox. [Báo cáo kiểm chứng](../../plans/261104-yomitan-reading-experience/evidence/test-report.md)
