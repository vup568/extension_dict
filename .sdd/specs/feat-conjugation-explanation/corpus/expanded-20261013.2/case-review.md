# F-09 — Gói corpus hoàn chỉnh để duyệt

Phiên bản: `f09-expanded-20261013.2`. Trạng thái: **chờ duyệt gói cuối G1/G2**.
Bạn đã duyệt [bản .1](../expanded-20261013.1/case-review.md); [biên nhận](../../reviews/user-review-receipt-20261013.json) lưu đúng nội dung/hash được duyệt. Bản .1 được giữ nguyên.

Gói này có **59 ô, 408 ca, 107 định danh quy tắc đề xuất**: 303 ca có phân tích, 10 ca exact-first, 91 ca unavailable và 4 ca thiếu mục từ/lỗi/hủy/phản hồi trễ. Mỗi quy tắc có ít nhất 2 ca đúng + 1 ca âm; số quy tắc runtime cuối cùng có thể được tổ chức lại nhưng phải giữ ý nghĩa/coverage đã duyệt.

## Các phần bổ sung hoặc chỉnh cần xem

- Đã điền dạng gốc, fixture canonical giả lập, lời giải thích Việt, từng thành phần và span UTF-16 cho mọi phương án của ca có hỗ trợ; [chi tiết từng ca](./review-details.md) là bản đọc của [cases.json](./cases.json).
- Khả năng/bị động/sai khiến/sai khiến-bị động/ている/たい có danh sách hữu hạn đủ 8 trạng thái: thường/lịch sự × khẳng định/phủ định × không quá khứ/quá khứ. Có ca する/来る, biến âm 行く/読む, じゃ/では và hai cách phủ định lịch sự.
- **Sửa AMB-02:** `書け` được đề xuất là mệnh lệnh của `書く`. Sudachi trả base `書ける` không chứng minh thêm một nghĩa khả năng độc lập. Ca hồi quy giữ sự khác biệt này.
- `食べられる` và `来られる` có hai phương án khả năng/bị động. **Bổ sung cần duyệt:** `食べさせられる`/`書かせられる` cũng đề xuất hai cách hiểu: sai khiến-bị động và khả năng của dạng sai khiến. Phân tích thứ hai là suy luận biên tập từ quy tắc khả năng của động từ 一段, cần bạn xác nhận; không lấy POS của Sudachi làm chứng cứ ngữ nghĩa.
- **いい/よい:** fixture chứa `よい/いい/良い` dưới cùng canonical ID; chọn `よい` làm dạng gốc trong các ca `よかった/よくない/よければ`, giải thích Việt nhắc hai dạng tương ứng. Đây là quy ước fixture cần duyệt.
- **学生で:** chỉ trình bày cách hiểu copula với nhãn “có thể”; nội dung nói rõ bề mặt cũng có thể chứa trợ từ で và cần câu để xác định. `学生に/学生の` không được gán là dạng chia danh từ.
- Các ca âm đã bỏ ghi chú khỏi chuỗi đầu vào. Ca âm phân biệt nhóm A/N có thể cho ra lời giải đúng của nhóm còn lại; không ép mọi ca âm thành unavailable.
- 9 rule baseline giữ ID nhưng lời giải thích/thành phần mới là phiên bản đề xuất **2**; dữ liệu phát triển phiên bản 1 giữ nguyên.

## Kiểm chứng kỹ thuật

Gửi **365 đầu vào duy nhất qua gRPC thật trong image Sudachi có sẵn**, container chỉ đọc và không có mạng ra ngoài. Mọi token phải tái tạo đúng đầu vào và có span UTF-16 hợp lệ; `𠮷です` kiểm ký tự ngoài BMP. Quan sát được lưu [riêng](../../evidence/expanded-sudachi-observations-20261013.2.json), không sửa đáp án theo output.

Kết quả này kiểm chứng corpus/Sidecar, chưa chứng nhận matcher/backend đã hỗ trợ 408 ca. Code ứng dụng được triển khai sau khi gói cuối được duyệt. Fixture mang ID `fixture:*`, không phải JMdict ID thực.

## Ma trận 59 ô

| Ô | Dạng | Hai ví dụ | Có phân tích | Exact-first | Ca âm cho rule thuộc ô |
|---|---|---|---:|---:|---:|
| V00 | Từ điển, nguyên dạng | `食べる`; `書く` | 0 | 2 | 0 |
| V01 | Lịch sự không quá khứ | `食べます`; `書きます` | 4 | 0 | 1 |
| V02 | Lịch sự quá khứ | `食べました`; `書きました` | 4 | 0 | 1 |
| V03 | Lịch sự phủ định | `食べません`; `書きません` | 4 | 0 | 1 |
| V04 | Lịch sự phủ định quá khứ | `食べませんでした`; `書きませんでした` | 4 | 0 | 1 |
| V05 | Thông thường quá khứ | `食べた`; `書いた` | 5 | 0 | 1 |
| V06 | Thông thường phủ định | `食べない`; `書かない` | 4 | 0 | 1 |
| V07 | Thông thường phủ định quá khứ | `食べなかった`; `書かなかった` | 4 | 0 | 1 |
| V08 | Thể て/で | `食べて`; `書いて` | 6 | 0 | 1 |
| V09 | Thể nối phủ định | `食べなくて`; `書かなくて` | 6 | 0 | 2 |
| V10 | Khả năng | `書ける`; `読める` | 28 | 2 | 8 |
| V11 | Bị động | `食べられる`; `書かれる` | 20 | 0 | 8 |
| V12 | Sai khiến | `食べさせる`; `書かせる` | 18 | 0 | 8 |
| V13 | Sai khiến-bị động | `食べさせられる`; `書かせられる` | 20 | 0 | 17 |
| V14 | Điều kiện ば | `食べれば`; `書けば` | 4 | 0 | 1 |
| V15 | Điều kiện たら | `食べたら`; `書いたら` | 4 | 0 | 1 |
| V16 | Ý chí | `食べよう`; `書こう` | 4 | 0 | 1 |
| V17 | Mệnh lệnh | `食べろ`; `読め` | 5 | 0 | 1 |
| V18 | Cấm đoán | `食べるな`; `書くな` | 4 | 0 | 1 |
| V19 | Mong muốn たい | `食べたい`; `書きたい` | 18 | 0 | 8 |
| V20 | Tiếp diễn/trạng thái ている | `食べている`; `書いている` | 18 | 0 | 8 |
| I00 | Nguyên dạng | `高い`; `安い` | 0 | 2 | 0 |
| I01 | Quá khứ thường | `高かった`; `安かった` | 3 | 0 | 1 |
| I02 | Phủ định thường | `高くない`; `安くない` | 3 | 0 | 1 |
| I03 | Phủ định quá khứ thường | `高くなかった`; `安くなかった` | 3 | 0 | 1 |
| I04 | Không quá khứ lịch sự | `高いです`; `安いです` | 2 | 0 | 1 |
| I05 | Quá khứ lịch sự | `高かったです`; `安かったです` | 2 | 0 | 1 |
| I06 | Phủ định lịch sự | `高くないです`; `安くないです` | 4 | 0 | 1 |
| I07 | Phủ định quá khứ lịch sự | `高くなかったです`; `安くなかったです` | 4 | 0 | 1 |
| I08 | Thể nối くて | `高くて`; `安くて` | 3 | 0 | 1 |
| I09 | Trạng từ く | `高く`; `安く` | 4 | 0 | 1 |
| I10 | Điều kiện ければ | `高ければ`; `安ければ` | 3 | 0 | 1 |
| I11 | Điều kiện かったら | `高かったら`; `安かったら` | 3 | 0 | 1 |
| A00 | Thân từ điển | `静か`; `便利` | 0 | 2 | 0 |
| A01 | Khẳng định thường không quá khứ | `静かだ`; `便利だ` | 5 | 0 | 1 |
| A02 | Khẳng định thường quá khứ | `静かだった`; `便利だった` | 4 | 0 | 1 |
| A03 | Phủ định thường không quá khứ | `静かじゃない`; `便利じゃない` | 6 | 0 | 1 |
| A04 | Phủ định thường quá khứ | `静かじゃなかった`; `便利じゃなかった` | 6 | 0 | 1 |
| A05 | Khẳng định lịch sự không quá khứ | `静かです`; `便利です` | 4 | 0 | 1 |
| A06 | Khẳng định lịch sự quá khứ | `静かでした`; `便利でした` | 4 | 0 | 1 |
| A07 | Phủ định lịch sự không quá khứ | `静かじゃないです`; `便利じゃないです` | 8 | 0 | 1 |
| A08 | Phủ định lịch sự quá khứ | `静かじゃなかったです`; `便利じゃなかったです` | 8 | 0 | 1 |
| A09 | Bổ nghĩa 連体形 `な` | `静かな`; `便利な` | 2 | 0 | 1 |
| A10 | Trạng từ `に` | `静かに`; `便利に` | 3 | 0 | 1 |
| A11 | Thể nối `で` | `静かで`; `便利で` | 3 | 0 | 1 |
| A12 | Điều kiện `なら` | `静かなら`; `便利なら` | 4 | 0 | 1 |
| A13 | Điều kiện `だったら` | `静かだったら`; `便利だったら` | 4 | 0 | 1 |
| N00 | Danh từ nguyên dạng | `学生`; `先生` | 0 | 2 | 0 |
| N01 | `だ` thường không quá khứ | `学生だ`; `先生だ` | 4 | 0 | 1 |
| N02 | `だった` thường quá khứ | `学生だった`; `先生だった` | 4 | 0 | 1 |
| N03 | Phủ định thường không quá khứ | `学生じゃない`; `先生じゃない` | 6 | 0 | 1 |
| N04 | Phủ định thường quá khứ | `学生じゃなかった`; `先生じゃなかった` | 6 | 0 | 1 |
| N05 | `です` lịch sự không quá khứ | `学生です`; `先生です` | 5 | 0 | 1 |
| N06 | `でした` lịch sự quá khứ | `学生でした`; `先生でした` | 4 | 0 | 1 |
| N07 | Phủ định lịch sự không quá khứ | `学生じゃないです`; `先生じゃないです` | 8 | 0 | 1 |
| N08 | Phủ định lịch sự quá khứ | `学生じゃなかったです`; `先生じゃなかったです` | 9 | 0 | 1 |
| N09 | Điều kiện `なら` | `学生なら`; `先生なら` | 4 | 0 | 1 |
| N10 | Điều kiện `だったら` | `学生だったら`; `先生だったら` | 4 | 0 | 1 |
| N11 | `で` nối mệnh đề | `学生で`; `先生で` | 3 | 0 | 1 |

## Ranh giới được khai báo

- Các cách hiểu kính ngữ/tự phát của đuôi れる/られる không thuộc danh mục khả năng/bị động đang chốt; nhãn “có thể” không khẳng định đây là mọi cách hiểu trong mọi câu tiếng Nhật.
- Thể văn chương mệnh lệnh よ/せよ và tổ hợp đệ quy không được liệt kê chưa thuộc gói này. Các ô/tổ hợp đã liệt kê không được âm thầm bỏ khi test lỗi.
- [Nguồn](./sources.json) có checksum HTTP snapshot. PDF Japan Foundation được tham khảo/liên kết, không nhập hay phân phối nội dung; câu Việt/fixture được dự án soạn. Quyền xuất bản nội dung bên ngoài không được suy ra từ việc tham khảo.

## Xác nhận cuối

Bạn có thể duyệt phạm vi **G1** và nội dung/đáp án **G2** trong cùng một phản hồi, vì bạn là PO và reviewer đã chỉ định. Gói được nhận diện bằng [scope-material.json](./scope-material.json) và các hash; nội dung mới của bản .2 chưa được tự gắn phê duyệt từ lời duyệt .1.
