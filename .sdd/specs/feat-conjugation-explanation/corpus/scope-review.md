# F-09 — Gói đề xuất để duyệt phạm vi 9 quy tắc

Phiên bản dự kiến: `f09-nine-rules-20261013.1`. Trạng thái: **chưa duyệt**. Người dùng đã chọn 9 quy tắc hiện có và nhận vai trò kiểm duyệt; bảng dưới đây là ví dụ/đáp án đề xuất cần xem lại, không phải chữ ký nghiệm thu.

| Quy tắc | Ca đúng đề xuất (dạng quan sát → dạng gốc) | Ca âm: không giải thích | Ý nghĩa tiếng Việt đề xuất |
|---|---|---|---|
| Động từ lịch sự, không quá khứ | 食べます→食べる; 読みます→読む | 食べる (khớp từ điển nguyên dạng) | Dạng lịch sự, không quá khứ |
| Động từ lịch sự, quá khứ | 食べました→食べる; 書きました→書く; しました→する; 来ました→来る | 食べました。 (có dấu câu) | Dạng lịch sự, quá khứ |
| Động từ phủ định | 食べない→食べる; 読まない→読む | 食べられる (bị động, ngoài phạm vi) | Dạng phủ định, không quá khứ |
| Động từ phủ định, quá khứ | 食べなかった→食べる; 読まなかった→読む | 食べなかった。 (có dấu câu) | Dạng phủ định, quá khứ |
| Động từ quá khứ thông thường | 食べた→食べる; 書いた→書く; 読んだ→読む | 食べて (thể て, ngoài phạm vi) | Dạng quá khứ, thể thông thường |
| Động từ lịch sự, phủ định | 食べません→食べる; 書きません→書く | 食べませんでした (quá khứ phủ định lịch sự, ngoài phạm vi) | Dạng lịch sự, phủ định, không quá khứ |
| Tính từ い, quá khứ | 高かった→高い; 寒かった→寒い | 高かった。 (có dấu câu) | Dạng quá khứ của tính từ |
| Tính từ い, phủ định | 高くない→高い; 寒くない→寒い | 高くない。 (có dấu câu) | Dạng phủ định, không quá khứ của tính từ |
| Tính từ い, phủ định quá khứ | 高くなかった→高い; 寒くなかった→寒い | 高くなかった。 (có dấu câu) | Dạng phủ định, quá khứ của tính từ |

Tổng: 9 quy tắc bắt buộc, 21 ca đúng, 9 ca âm. Thêm 6 ca bảo vệ: không có mục từ gốc, Unicode ngoài BMP, Sidecar không khả dụng, thiếu nội dung Việt, nhiều quy tắc khớp và phản hồi giao diện đến muộn. Các chuỗi tiếng Việt và phần giải thích từng đoạn nằm trong [cases.json](./cases.json); cần bạn kiểm duyệt độc lập ở bước G2.

Phạm vi ngoài đợt này: thể て, bị động, sai khiến, tính từ な, phân tích cả câu và morphology tổng quát. Các ca âm chỉ kiểm tra không nhận nhầm trong giới hạn hiện có, không khẳng định chúng vô nghĩa trong tiếng Nhật.

## Điều cần duyệt ở G1

1. Giữ cả 9 quy tắc trên là bắt buộc; không bổ sung nhóm khác trong đợt này.
2. Chấp nhận bộ ca đề xuất làm phạm vi kiểm duyệt, hoặc chỉ rõ ca/nhóm cần sửa. Các đáp án tiếng Việt/chuỗi đoạn vẫn phải kiểm duyệt riêng ở G2.
3. Xác nhận người kiểm duyệt là bạn; cho biết tên/viết tắt muốn ghi trong hồ sơ nếu cần truy vết.

Hash của `cases.json`, `sources.json` và bản gói phạm vi được ghi vào [scope-material.json](./scope-material.json) sau kiểm tra cơ học. Phê duyệt bằng lời chỉ có hiệu lực với đúng phiên bản/hash đó; thay đổi corpus sau đó phải xin duyệt lại.
