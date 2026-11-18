# Tra từ vựng trong vùng chọn nhiều từ

Người dùng yêu cầu: khi chọn một câu hoặc đoạn tiếng Nhật, hiển thị những từ tìm được trong JMdict, cách đọc, nghĩa tiếng Anh và thể chia đã xác minh; giữ chức năng tra kanji, ngữ pháp và từ đơn.

## Thực hiện

- [x] Khảo sát API v1–v3, Sudachi và dữ liệu JMdict thực. `勉強` là mục từ trong snapshot, `勉強する` không phải một mục viết riêng.
- [x] Dùng tokenizer có sẵn và thêm luồng tra từ trong câu; giữ offset UTF-16, các lần xuất hiện lặp, ranh giới dấu câu/khoảng trắng và các khả năng lỗi riêng. Chỉ hiển thị giải thích chia thể được F09 và từ điển xác minh.
- [x] Mở rộng response v3 bằng `vocabulary_occurrences` tùy chọn: surface, span, các mục từ và giải thích thể chia theo vùng chọn con. Các trường và contract cũ vẫn hợp lệ; clients cũ vẫn nhận `dictionary_matches`.
- [x] Hiển thị kết quả theo cụm gốc trong popup; xác thực response và tránh trùng khóa với từ lặp.
- [x] Kiểm thử đơn vị, integration PostgreSQL/Sudachi, API/extension và browser với dữ liệu thật; không sửa database từ điển gốc.

## Kiểm chứng và giới hạn

- Câu `私は日本語を勉強しています。` cần có ít nhất 私, 日本語 và 勉強 theo đúng JMdict; không tạo mục `勉強する` giả.
- `猫を食べました。` cần tìm 猫 và `食べました` → `食べる`; kiểm tra câu có emoji/kanji ngoài BMP, dấu câu, từ lặp, nhiều mục từ đồng âm, tokenizer lỗi và hủy request.
- Kết quả từ đơn, ngữ pháp, kanji, contract cũ và khả năng hoạt động khi tokenizer lỗi không được thoái lui. Không log vùng chọn hay cài thêm dependency.
- Không hứa phân tích ngữ nghĩa hoàn hảo hoặc mọi từ ghép, mẫu chia thể. Nếu giới hạn xử lý hay provider lỗi phải báo trung thực, không nói hoàn thành khi đã cắt âm thầm.

## Kết quả xác minh

- 123 kiểm thử đơn vị backend, 49 kiểm thử tích hợp backend và 275 kiểm thử extension đạt; build backend và extension thành công.
- Kiểm thử extension đóng gói trên Brave với PostgreSQL và Sudachi thật đạt các câu có `日本語`, `勉強しています`, `食べました`, `お茶`, từ lặp và emoji.
