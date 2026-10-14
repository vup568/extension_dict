# F-09 — Gói kiểm duyệt corpus .3

Ngày chuẩn bị: 2026-10-13. Version: `f09-expanded-20261013.3`.

Đã ghi nhận hai lựa chọn của bạn: kính ngữ của れる/られる; hiển thị và giải thích cả hai nghĩa sai khiến. Không suy ra bạn đã duyệt những câu giải thích/thành phần mới chưa được trình bày.

## Nội dung người đọc sẽ thấy

| Từ | Dạng gốc | Các cách hiểu có thể |
|---|---|---|
| 食べさせられる | 食べる | Sai khiến-bị động: bị yêu cầu/bắt ăn. Sai khiến-khả năng: có thể cho phép/khiến ai đó ăn. |
| 食べられる | 食べる | Khả năng: có thể ăn. Bị động: chủ thể chịu tác động của hành động ăn, ý nghĩa cụ thể cần ngữ cảnh. Kính ngữ: nói hành động ăn của người được kính trọng. |
| 書かれる | 書く | Bị động: chịu tác động của hành động viết. Kính ngữ: nói hành động viết của người được kính trọng. |
| される | する | Bị động hoặc kính ngữ; không gán khả năng của する cho される. |
| 来られる | 来る | Khả năng, bị động hoặc kính ngữ; cần ngữ cảnh để xác định. |

Mỗi cách hiểu có thẻ/giải thích thành phần riêng; không tự chọn một nghĩa hoặc xếp hạng độ đúng. Kính ngữ tôn kính không đồng nhất với đuôi lịch sự ます/です.

## Phần thay đổi so với .2

- Giữ 408 ca trước, thêm cách hiểu kính ngữ vào 20 ca và thêm 4 ca loại nhận nhầm. Các ca sai khiến đã có hai nghĩa được giữ nguyên.
- Tổng 60 ô, 412 ca, 115 rule IDs đề xuất, 17 fixtures. Mỗi rule có ít nhất 2 ca đúng + 1 ca âm. Đây là quy mô test, không phải bảng runtime tra đáp án cố định.
- V21 có 8 trạng thái thì/phủ định/lịch sự, cộng する/来る. 書ける/読める vẫn là các ca khả năng, không bị tự gán nghĩa kính ngữ. 静かです/学生です không phải kính ngữ れる/られる.
- Các dạng できられる/できられた/書けられる/書けられた không được sinh nhánh tôn kính theo đuôi.
- JSON mới được viết gọn (mỗi ca một dòng); bản .1/.2 và biên nhận cũ giữ nguyên.

Đáp án chi tiết: [review-details.md](./review-details.md). Phạm vi: [coverage-catalog.md](./coverage-catalog.md). Nguồn: [sources.json](./sources.json). G1/G2 của snapshot này còn pending; chưa phải F09 hoàn tất.

## Kiểm tra và giới hạn

Kiểm cấu trúc/refs/UTF-16/ca âm và 8 guard tests đạt; [validation report](../../evidence/expanded-corpus-validation-20261013.3.md) có bằng chứng riêng. Sau khi người dùng bật Docker, cả 369 đầu vào .3 được quan sát qua gRPC trong container chỉ đọc/không mạng; không dùng 365 quan sát .2 làm chứng nhận .3. Kiểm tra này không so đáp án với backend F09. Runtime/API/popup chưa được mở rộng theo corpus này.

