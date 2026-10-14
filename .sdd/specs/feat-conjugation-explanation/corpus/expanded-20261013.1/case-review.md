# F-09 — Ca đề xuất để người dùng kiểm duyệt (phạm vi B)

**Trạng thái:** bản nháp ngôn ngữ, chưa G1/G2. Mỗi dòng là một ô trong [danh mục](./coverage-catalog.md):
P1/P2 là ca đúng được đề xuất; N là ca không được gán **quy tắc của dòng đó** (có thể
đúng ở ô khác). Dạng `X。` kiểm ranh giới nguyên từ, không có nghĩa bản thân `X` sai.
Chữ trước dấu `→` là bề mặt được chọn; chữ sau là dạng gốc kỳ vọng. Ví dụ và câu Việt
do dự án soạn để người dùng duyệt, chưa được coi là đáp án chuẩn. Nguồn tham khảo
[Japan Foundation Irodori](https://www.irodori.jpf.go.jp/assets/data/Grammar_all.pdf)
và [bảng bốn nhóm](https://www.irodori.jpf.go.jp/assets/data/pre-intermediate/pdf/ZZ_L09.pdf);
quyền dùng/ấn bản vẫn pending trong [sources.json](./sources.json).

## Động từ — V00–V20

| Ô | P1; P2 → gốc | Nghĩa Việt cần duyệt | N: không gán ô này |
|---|---|---|---|
| V00 | 食べる→食べる; 書く→書く | Từ điển đã khớp chính xác; không tạo lời giải thích chia từ | — |
| V01 | 食べます→食べる; 書きます→書く | Lịch sự, không quá khứ | 食べます。 |
| V02 | 食べました→食べる; 書きました→書く | Lịch sự, quá khứ | 食べました。 |
| V03 | 食べません→食べる; 書きません→書く | Lịch sự, phủ định, không quá khứ | 食べません。 |
| V04 | 食べませんでした→食べる; 書きませんでした→書く | Lịch sự, phủ định, quá khứ | 食べませんでした。 |
| V05 | 食べた→食べる; 書いた→書く | Thông thường, quá khứ | 食べた。 |
| V06 | 食べない→食べる; 書かない→書く | Thông thường, phủ định, không quá khứ | 食べない。 |
| V07 | 食べなかった→食べる; 書かなかった→書く | Thông thường, phủ định, quá khứ | 食べなかった。 |
| V08 | 食べて→食べる; 書いて→書く | Thể nối て/で; ý nghĩa liên kết phụ thuộc câu | 食べて。 |
| V09 | 食べなくて→食べる; 書かなくて→書く | Phủ định rồi nối; `ないで` cần phân biệt sắc thái | 食べなくて。 |
| V10 | 書ける→書く; 読める→読む | Khả năng “có thể …”; không suy từ hậu tố đơn thuần | 書ける。 |
| V11 | 食べられる→食べる; 書かれる→書く | Bị động “được/bị …”; P1 còn có nghĩa khả năng, xem AMB-01 | 書かれる。 |
| V12 | 食べさせる→食べる; 書かせる→書く | Sai khiến “cho/bắt/khiến …” tùy ngữ cảnh | 食べさせる。 |
| V13 | 食べさせられる→食べる; 書かせられる→書く | Sai khiến-bị động “bị bắt/cho …” | 食べさせられる。 |
| V14 | 食べれば→食べる; 書けば→書く | Điều kiện ば “nếu …” | 食べれば。 |
| V15 | 食べたら→食べる; 書いたら→書く | Điều kiện たら “nếu/khi …” tùy ngữ cảnh | 食べたら。 |
| V16 | 食べよう→食べる; 書こう→書く | Ý chí/đề nghị “hãy/cùng …” | 食べよう。 |
| V17 | 食べろ→食べる; 読め→読む | Mệnh lệnh trực tiếp | 食べろ。 |
| V18 | 食べるな→食べる; 書くな→書く | Cấm đoán “đừng …” | 食べるな。 |
| V19 | 食べたい→食べる; 書きたい→書く | Mong muốn “muốn …” | 食べたい。 |
| V20 | 食べている→食べる; 書いている→書く | Tiếp diễn hoặc trạng thái kết quả, cần ngữ cảnh | 食べている。 |

**Biến thể/bất quy tắc cần duyệt thêm** (tất cả còn là đề xuất):

| Mã | Bề mặt → gốc | Điều cần xác nhận |
|---|---|---|
| V-IRR-01 | しました→する; 来ました→来る | V02 lịch sự quá khứ cho hai động từ bất quy tắc |
| V-IRR-02 | した→する; 来た→来る | V05 quá khứ thông thường |
| V-IRR-03 | して→する; 来て→来る; 行って→行く; 読んで→読む | V08, biến âm và dạng で |
| V-IRR-04 | しよう→する; 来よう→来る; したい→する; 来たい→来る | V16/V19; cách giải thích của tổ hợp ý chí/mong muốn |
| V09-ALT | 食べないで→食べる; 書かないで→書く | “Không … mà/không … rồi”; không đồng nhất hoàn toàn với `なくて` |
| V10-COMB | 書けた→書く; 書けない→書く; 書けます→書く | Khả năng + quá khứ/phủ định/lịch sự; tránh nhận nhầm mục từ 書ける |
| V11-COMB | 書かれた→書く; 書かれない→書く; 書かれます→書く | Bị động + quá khứ/phủ định/lịch sự |
| V12-COMB | 書かせた→書く; 書かせない→書く; 書かせます→書く | Sai khiến + quá khứ/phủ định/lịch sự |
| V13-COMB | 食べさせられた→食べる; 食べさせられない→食べる; 食べさせられます→食べる | Sai khiến-bị động + quá khứ/phủ định/lịch sự |
| V20-COMB | 食べていた→食べる; 食べていない→食べる; 食べています→食べる | Tiếp diễn/trạng thái + quá khứ/phủ định/lịch sự; nghĩa tùy ngữ cảnh |

Những tổ hợp có cùng bề mặt nhưng nhiều nghĩa vẫn cần nhiều phương án, không tự
suy ra mọi tổ hợp chỉ có một nghĩa. Từng cách tách/giải thích thành phần chờ duyệt.

## Tính từ い — I00–I11

| Ô | P1; P2 → gốc | Nghĩa Việt cần duyệt | N: không gán ô này |
|---|---|---|---|
| I00 | 高い→高い; 安い→安い | Từ điển chính xác; không tạo lời giải thích chia từ | — |
| I01 | 高かった→高い; 安かった→安い | Quá khứ thông thường | 高かった。 |
| I02 | 高くない→高い; 安くない→安い | Phủ định thông thường | 高くない。 |
| I03 | 高くなかった→高い; 安くなかった→安い | Phủ định quá khứ thông thường | 高くなかった。 |
| I04 | 高いです→高い; 安いです→安い | Không quá khứ lịch sự | 高いです。 |
| I05 | 高かったです→高い; 安かったです→安い | Quá khứ lịch sự | 高かったです。 |
| I06 | 高くないです→高い; 安くありません→安い | Phủ định lịch sự; hai cách nói | 高くないです。 |
| I07 | 高くなかったです→高い; 安くありませんでした→安い | Phủ định quá khứ lịch sự; hai cách nói | 高くなかったです。 |
| I08 | 高くて→高い; 安くて→安い | Thể nối くて | 高くて。 |
| I09 | 高く→高い; 安く→安い | Dạng trạng từ く; cần kiểm ranh giới từ | きれいに (không phải tính từ い) |
| I10 | 高ければ→高い; 安ければ→安い | Điều kiện ければ | 高ければ。 |
| I11 | 高かったら→高い; 安かったら→安い | Điều kiện かったら | 高かったら。 |

**Ca bất quy tắc/biến thể:** `よかった→いい/よい`, `よくない→いい/よい`,
`よければ→いい/よい`, `高くありません→高い`,
`高くありませんでした→高い`, `安くないです→安い`,
`安くなかったです→安い`. Cần người dùng chọn dạng gốc hiển thị của
`いい/よい` theo mục từ chuẩn; không tự đoán.

## Tính từ な — A00–A13

| Ô | P1; P2 → gốc | Nghĩa Việt cần duyệt | N: không gán ô này |
|---|---|---|---|
| A00 | 静か→静か; 便利→便利 | Từ điển chính xác; không tạo lời giải thích chia từ | — |
| A01 | 静かだ→静か; 便利だ→便利 | Khẳng định thông thường, không quá khứ | 学生だ (danh từ) |
| A02 | 静かだった→静か; 便利だった→便利 | Khẳng định thông thường, quá khứ | 学生だった |
| A03 | 静かじゃない→静か; 便利ではない→便利 | Phủ định thông thường, không quá khứ | 学生じゃない |
| A04 | 静かじゃなかった→静か; 便利ではなかった→便利 | Phủ định thông thường, quá khứ | 学生じゃなかった |
| A05 | 静かです→静か; 便利です→便利 | Khẳng định lịch sự, không quá khứ | 学生です |
| A06 | 静かでした→静か; 便利でした→便利 | Khẳng định lịch sự, quá khứ | 学生でした |
| A07 | 静かじゃないです→静か; 便利ではありません→便利 | Phủ định lịch sự, không quá khứ | 学生ではありません |
| A08 | 静かじゃなかったです→静か; 便利ではありませんでした→便利 | Phủ định lịch sự, quá khứ | 学生ではありませんでした |
| A09 | 静かな→静か; 便利な→便利 | Dạng bổ nghĩa trước danh từ | 学生な (không phải dạng tính từ な) |
| A10 | 静かに→静か; 便利に→便利 | Dạng trạng từ に | 学生に (hạt chỉ quan hệ, không phải tính từ な) |
| A11 | 静かで→静か; 便利で→便利 | Dạng nối で; phân biệt hạt で | 学生で (không phải tính từ な) |
| A12 | 静かなら→静か; 便利なら→便利 | Điều kiện なら | 学生なら |
| A13 | 静かだったら→静か; 便利だったら→便利 | Điều kiện だったら | 学生だったら |

**Ca biến thể riêng:** `静かではない→静か`, `便利じゃない→便利`,
`静かではなかった→静か`, `便利じゃなかった→便利`,
`静かじゃありません→静か`, `静かではありません→静か`,
`便利じゃないです→便利`, `便利じゃありません→便利`,
`静かじゃありませんでした→静か`, `静かではありませんでした→静か`,
`便利じゃなかったです→便利`, `便利じゃありませんでした→便利`.
`きれい` dù kết thúc bằng い là tính từ な, không được nhận là nhóm I.

## Danh từ + copula — N00–N11

| Ô | P1; P2 → gốc | Nghĩa Việt cần duyệt | N: không gán ô này |
|---|---|---|---|
| N00 | 学生→学生; 先生→先生 | Danh từ chính xác; không tạo lời giải thích chia từ | — |
| N01 | 学生だ→学生; 先生だ→先生 | Khẳng định thông thường qua `だ`; danh từ không tự đổi đuôi | 静かだ (tính từ な) |
| N02 | 学生だった→学生; 先生だった→先生 | Quá khứ thông thường qua `だった` | 静かだった |
| N03 | 学生じゃない→学生; 先生ではない→先生 | Phủ định thông thường qua copula/phủ định | 静かじゃない |
| N04 | 学生じゃなかった→学生; 先生ではなかった→先生 | Phủ định quá khứ qua copula/phủ định | 静かじゃなかった |
| N05 | 学生です→学生; 先生です→先生 | Khẳng định lịch sự qua `です` | 静かです |
| N06 | 学生でした→学生; 先生でした→先生 | Quá khứ lịch sự qua `でした` | 静かでした |
| N07 | 学生じゃないです→学生; 先生ではありません→先生 | Phủ định lịch sự qua copula/phủ định | 静かではありません |
| N08 | 学生じゃなかったです→学生; 先生ではありませんでした→先生 | Phủ định quá khứ lịch sự | 静かではありませんでした |
| N09 | 学生なら→学生; 先生なら→先生 | Điều kiện なら | 静かなら |
| N10 | 学生だったら→学生; 先生だったら→先生 | Điều kiện だったら | 静かだったら |
| N11 | 学生で→学生; 先生で→先生 | Dạng nối copula で, **chỉ khi có chứng cứ** | 学生に (hạt khác) |

**Ca biến thể riêng:** `学生ではない→学生`, `先生じゃない→先生`,
`学生ではなかった→学生`, `先生じゃなかった→先生`,
`学生じゃありません→学生`, `学生ではありません→学生`,
`先生じゃないです→先生`, `先生じゃありません→先生`,
`学生じゃありませんでした→学生`, `学生ではありませんでした→学生`,
`先生じゃなかったです→先生`, `先生じゃありませんでした→先生`.
`学生に`/`学生の` không phải dạng chia của danh từ. `学生で` có thể cần ngữ cảnh để
phân biệt copula và hạt `で`; nếu word-unit không đủ chứng cứ, không khẳng định một
nghĩa duy nhất.

## Ca đa nghĩa và bảo vệ xuyên nhóm

| Mã | Đầu vào | Đáp án đề xuất cần duyệt |
|---|---|---|
| AMB-01 | 食べられる | Hai phương án cùng gốc 食べる: (1) khả năng “có thể ăn”; (2) bị động “được/bị ăn”; hiển thị “có thể” cho cả hai, không xếp hạng. Contract2 vẫn không tự chọn một. |
| AMB-02 | 書け | Có thể là mệnh lệnh của 書く hoặc bề mặt liên quan mục từ 書ける; chỉ hiện phương án nào đủ mục từ/nguồn, không suy từ hậu tố. |
| BND-01 | 学生ではありませんでした | Kiểm tách nhiều trợ từ/trợ động từ; gốc 学生 nếu phân tích đầy đủ và duyệt. |
| POS-01 | きれいだ | Gốc きれい thuộc nhóm A, không thuộc nhóm I dù kết thúc い. |
| MIS-01 | 食べました | Thiếu mục từ 食べる trong fixture: không tạo liên kết hay giải thích giả. |
| ERR-01 | 食べました | Sidecar timeout/down: unavailable/failure trung thực; không đoán. |
| UNI-01 | 𠮷さんです | Kiểm UTF-16 ngoài BMP và ranh giới span; không tự gán gốc nếu fixture không có. |
| ASYNC-01 | 食べられる | Đổi vùng chọn trước khi phản hồi: không hiển thị kết quả cũ. |

## Người dùng cần duyệt

1. Các dạng và dạng gốc P1/P2, kể cả trường hợp bất quy tắc và nhiều phương án.
2. Ý nghĩa tiếng Việt từng ô và việc giữ N11/những ô cần ngữ cảnh trong phạm vi bắt buộc.
3. Các biến thể/tổ hợp đã liệt kê: bổ sung/sửa/loại kèm lý do; không coi im lặng là duyệt.

Sau phản hồi, nhóm triển khai sẽ cập nhật `cases.json` đầy đủ expected segments/spans,
khóa hash và trình G1 chính thức. Chọn phạm vi B không phải chữ ký G1 cho tài liệu này.
