# F-09 — Chi tiết 408 ca, f09-expanded-20261013.2

Trạng thái: đề xuất hoàn chỉnh để người dùng duyệt. Thành phần là phân đoạn ngôn ngữ biên tập, độc lập với token provider; có thể gộp/tách token nhưng phải tái tạo đúng bề mặt và span UTF-16. Dữ liệu chuẩn nằm ở [cases.json](./cases.json).

## Fixture từ điển dùng cho test

| ID | Dạng ưu tiên | Dạng tương ứng | Nhóm | Nghĩa Việt tổng hợp |
|---|---|---|---|---|
| fixture:001 | 食べる | 食べる | verb | Ăn |
| fixture:002 | 書く | 書く | verb | Viết |
| fixture:003 | 読む | 読む | verb | Đọc |
| fixture:004 | 行く | 行く | verb | Đi |
| fixture:005 | する | する | verb | Làm |
| fixture:006 | 来る | 来る | verb | Đến |
| fixture:007 | 高い | 高い | i_adjective | Cao/đắt |
| fixture:008 | 安い | 安い | i_adjective | Rẻ |
| fixture:009 | よい | よい / いい / 良い | i_adjective | Tốt |
| fixture:010 | 静か | 静か | na_adjective | Yên tĩnh |
| fixture:011 | 便利 | 便利 | na_adjective | Tiện lợi |
| fixture:012 | きれい | きれい | na_adjective | Đẹp/sạch |
| fixture:013 | 学生 | 学生 | noun_copula | Học sinh/sinh viên |
| fixture:014 | 先生 | 先生 | noun_copula | Thầy/cô |
| fixture:015 | 𠮷 | 𠮷 | noun_copula | Tên riêng tổng hợp dùng để test UTF-16 |
| fixture:016 | 書ける | 書ける | verb | Mục từ khả năng dùng riêng cho exact-first |
| fixture:017 | できる | できる | verb | Mục từ dùng riêng cho exact-first |

Fixture được tạo riêng cho test; không giả định ID/cấu trúc dữ liệu JMdict thật.

## V00 — Từ điển, nguyên dạng

### V00-P1

Đầu vào: `食べる`; fixture: fixture:001; kết quả: **exact_first**.

### V00-P2

Đầu vào: `書く`; fixture: fixture:002; kết quả: **exact_first**.

## V01 — Lịch sự không quá khứ

### V01-P1

Đầu vào: `食べます`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-polite-nonpast@2`, gốc **食べる** (verb): Dạng lịch sự, không quá khứ. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| ます | 2–4 | Lịch sự, không quá khứ. |

### V01-P2

Đầu vào: `書きます`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-polite-nonpast@2`, gốc **書く** (verb): Dạng lịch sự, không quá khứ. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| ます | 2–4 | Lịch sự, không quá khứ. |

### V01-IRR-suru

Đầu vào: `します`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-polite-nonpast@2`, gốc **する** (verb): Dạng lịch sự, không quá khứ. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| ます | 1–3 | Lịch sự, không quá khứ. |

### V01-IRR-kuru

Đầu vào: `来ます`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-polite-nonpast@2`, gốc **来る** (verb): Dạng lịch sự, không quá khứ. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| ます | 1–3 | Lịch sự, không quá khứ. |

### V01-N1

Đầu vào: `食べます。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-polite-nonpast`.

## V02 — Lịch sự quá khứ

### V02-P1

Đầu vào: `食べました`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-polite-past@2`, gốc **食べる** (verb): Dạng lịch sự, quá khứ. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| まし | 2–4 | Phần lịch sự nối với quá khứ. |
| た | 4–5 | Đánh dấu quá khứ. |

### V02-P2

Đầu vào: `書きました`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-polite-past@2`, gốc **書く** (verb): Dạng lịch sự, quá khứ. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| まし | 2–4 | Phần lịch sự nối với quá khứ. |
| た | 4–5 | Đánh dấu quá khứ. |

### V02-IRR-suru

Đầu vào: `しました`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-polite-past@2`, gốc **する** (verb): Dạng lịch sự, quá khứ. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| まし | 1–3 | Phần lịch sự nối với quá khứ. |
| た | 3–4 | Đánh dấu quá khứ. |

### V02-IRR-kuru

Đầu vào: `来ました`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-polite-past@2`, gốc **来る** (verb): Dạng lịch sự, quá khứ. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| まし | 1–3 | Phần lịch sự nối với quá khứ. |
| た | 3–4 | Đánh dấu quá khứ. |

### V02-N1

Đầu vào: `食べました。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-polite-past`.

### MIS-01

Đầu vào: `食べました`; fixture: không có; kết quả: **completed_empty**.

Tình huống: `{"fault":"missing_base_entry"}`.

### ERR-01

Đầu vào: `食べました`; fixture: fixture:001; kết quả: **unavailable**.

Tình huống: `{"fault":"sidecar_unavailable"}`.

### ERR-02

Đầu vào: `食べました`; fixture: fixture:001; kết quả: **unavailable**.

Tình huống: `{"fault":"sidecar_timeout_500ms"}`.

### DATA-01

Đầu vào: `食べました`; fixture: fixture:001; kết quả: **unavailable**.

Tình huống: `{"fault":"dataset_missing"}`.

### VI-01

Đầu vào: `食べました`; fixture: fixture:001; kết quả: **unavailable**.

Tình huống: `{"fault":"vietnamese_content_missing"}`.

### DB-01

Đầu vào: `食べました`; fixture: không có; kết quả: **failed**.

Tình huống: `{"fault":"dictionary_repository_failure"}`.

### SENT-01

Đầu vào: `昨日食べました`; fixture: không có; kết quả: **unavailable**.

### CANCEL-01

Đầu vào: `食べました`; fixture: fixture:001; kết quả: **cancelled**.

Tình huống: `{"fault":"caller_cancelled"}`.

## V03 — Lịch sự phủ định

### V03-P1

Đầu vào: `食べません`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-polite-negative@2`, gốc **食べる** (verb): Dạng lịch sự, phủ định, không quá khứ. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| ませ | 2–4 | Phần lịch sự nối với phủ định. |
| ん | 4–5 | Phủ định trong chuỗi lịch sự. |

### V03-P2

Đầu vào: `書きません`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-polite-negative@2`, gốc **書く** (verb): Dạng lịch sự, phủ định, không quá khứ. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| ませ | 2–4 | Phần lịch sự nối với phủ định. |
| ん | 4–5 | Phủ định trong chuỗi lịch sự. |

### V03-IRR-suru

Đầu vào: `しません`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-polite-negative@2`, gốc **する** (verb): Dạng lịch sự, phủ định, không quá khứ. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| ませ | 1–3 | Phần lịch sự nối với phủ định. |
| ん | 3–4 | Phủ định trong chuỗi lịch sự. |

### V03-IRR-kuru

Đầu vào: `来ません`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-polite-negative@2`, gốc **来る** (verb): Dạng lịch sự, phủ định, không quá khứ. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| ませ | 1–3 | Phần lịch sự nối với phủ định. |
| ん | 3–4 | Phủ định trong chuỗi lịch sự. |

### V03-N1

Đầu vào: `食べません。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-polite-negative`.

## V04 — Lịch sự phủ định quá khứ

### V04-P1

Đầu vào: `食べませんでした`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-polite-negative-past@1`, gốc **食べる** (verb): Dạng lịch sự, phủ định, quá khứ. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| ませ | 2–4 | Phần lịch sự nối với phủ định. |
| ん | 4–5 | Phủ định trong chuỗi lịch sự. |
| でし | 5–7 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 7–8 | Đánh dấu quá khứ. |

### V04-P2

Đầu vào: `書きませんでした`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-polite-negative-past@1`, gốc **書く** (verb): Dạng lịch sự, phủ định, quá khứ. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| ませ | 2–4 | Phần lịch sự nối với phủ định. |
| ん | 4–5 | Phủ định trong chuỗi lịch sự. |
| でし | 5–7 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 7–8 | Đánh dấu quá khứ. |

### V04-IRR-suru

Đầu vào: `しませんでした`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-polite-negative-past@1`, gốc **する** (verb): Dạng lịch sự, phủ định, quá khứ. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| ませ | 1–3 | Phần lịch sự nối với phủ định. |
| ん | 3–4 | Phủ định trong chuỗi lịch sự. |
| でし | 4–6 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 6–7 | Đánh dấu quá khứ. |

### V04-IRR-kuru

Đầu vào: `来ませんでした`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-polite-negative-past@1`, gốc **来る** (verb): Dạng lịch sự, phủ định, quá khứ. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| ませ | 1–3 | Phần lịch sự nối với phủ định. |
| ん | 3–4 | Phủ định trong chuỗi lịch sự. |
| でし | 4–6 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 6–7 | Đánh dấu quá khứ. |

### V04-N1

Đầu vào: `食べませんでした。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-polite-negative-past`.

## V05 — Thông thường quá khứ

### V05-P1

Đầu vào: `食べた`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-past@2`, gốc **食べる** (verb): Dạng quá khứ thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| た | 2–3 | Đánh dấu quá khứ. |

### V05-P2

Đầu vào: `書いた`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-past@2`, gốc **書く** (verb): Dạng quá khứ thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書い | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| た | 2–3 | Đánh dấu quá khứ. |

### V05-IRR-suru

Đầu vào: `した`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-past@2`, gốc **する** (verb): Dạng quá khứ thông thường. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| た | 1–2 | Quá khứ. |

### V05-IRR-kuru

Đầu vào: `来た`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-past@2`, gốc **来る** (verb): Dạng quá khứ thông thường. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| た | 1–2 | Quá khứ. |

### V05-ONBIN-yomu

Đầu vào: `読んだ`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-past@2`, gốc **読む** (verb): Dạng quá khứ thông thường. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読ん | 0–2 | Phần động từ của 読む ở dạng quan sát này. |
| だ | 2–3 | Quá khứ sau biến âm mũi. |

### V05-N1

Đầu vào: `食べた。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-past`.

## V06 — Thông thường phủ định

### V06-P1

Đầu vào: `食べない`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-negative@2`, gốc **食べる** (verb): Dạng phủ định, không quá khứ thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| ない | 2–4 | Phủ định, không quá khứ. |

### V06-P2

Đầu vào: `書かない`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-negative@2`, gốc **書く** (verb): Dạng phủ định, không quá khứ thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| ない | 2–4 | Phủ định, không quá khứ. |

### V06-IRR-suru

Đầu vào: `しない`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-negative@2`, gốc **する** (verb): Dạng phủ định, không quá khứ thông thường. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| ない | 1–3 | Phủ định, không quá khứ. |

### V06-IRR-kuru

Đầu vào: `来ない`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-negative@2`, gốc **来る** (verb): Dạng phủ định, không quá khứ thông thường. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| ない | 1–3 | Phủ định, không quá khứ. |

### V06-N1

Đầu vào: `食べない。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-negative`.

## V07 — Thông thường phủ định quá khứ

### V07-P1

Đầu vào: `食べなかった`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-negative-past@2`, gốc **食べる** (verb): Dạng phủ định, quá khứ thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| なかっ | 2–5 | Phần phủ định nối với đuôi quá khứ. |
| た | 5–6 | Đánh dấu quá khứ. |

### V07-P2

Đầu vào: `書かなかった`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-negative-past@2`, gốc **書く** (verb): Dạng phủ định, quá khứ thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| なかっ | 2–5 | Phần phủ định nối với đuôi quá khứ. |
| た | 5–6 | Đánh dấu quá khứ. |

### V07-IRR-suru

Đầu vào: `しなかった`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-negative-past@2`, gốc **する** (verb): Dạng phủ định, quá khứ thông thường. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| なかっ | 1–4 | Phần phủ định nối với đuôi quá khứ. |
| た | 4–5 | Đánh dấu quá khứ. |

### V07-IRR-kuru

Đầu vào: `来なかった`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-negative-past@2`, gốc **来る** (verb): Dạng phủ định, quá khứ thông thường. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| なかっ | 1–4 | Phần phủ định nối với đuôi quá khứ. |
| た | 4–5 | Đánh dấu quá khứ. |

### V07-N1

Đầu vào: `食べなかった。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-negative-past`.

## V08 — Thể て/で

### V08-P1

Đầu vào: `食べて`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-te@1`, gốc **食べる** (verb): Thể nối て/で; quan hệ với phần câu tiếp theo phụ thuộc ngữ cảnh. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で; quan hệ với phần tiếp theo phụ thuộc câu. |

### V08-P2

Đầu vào: `書いて`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-te@1`, gốc **書く** (verb): Thể nối て/で; quan hệ với phần câu tiếp theo phụ thuộc ngữ cảnh. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書い | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で; quan hệ với phần tiếp theo phụ thuộc câu. |

### V08-IRR-suru

Đầu vào: `して`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-te@1`, gốc **する** (verb): Thể nối て/で; quan hệ với phần câu tiếp theo phụ thuộc ngữ cảnh. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| て | 1–2 | Thể nối て. |

### V08-IRR-kuru

Đầu vào: `来て`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-te@1`, gốc **来る** (verb): Thể nối て/で; quan hệ với phần câu tiếp theo phụ thuộc ngữ cảnh. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| て | 1–2 | Thể nối て. |

### V08-ONBIN-行く

Đầu vào: `行って`; fixture: fixture:004; kết quả: **supported**.

Phương án `conjugation:verb-te@1`, gốc **行く** (verb): Thể nối て/で; quan hệ với phần câu tiếp theo phụ thuộc ngữ cảnh. Dạng gốc: 行く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 行っ | 0–2 | Phần động từ của 行く ở dạng quan sát này. |
| て | 2–3 | Biến thể thể nối sau biến âm. |

### V08-ONBIN-読む

Đầu vào: `読んで`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-te@1`, gốc **読む** (verb): Thể nối て/で; quan hệ với phần câu tiếp theo phụ thuộc ngữ cảnh. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読ん | 0–2 | Phần động từ của 読む ở dạng quan sát này. |
| で | 2–3 | Biến thể thể nối sau biến âm. |

### V08-N1

Đầu vào: `食べて。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-te`.

## V09 — Thể nối phủ định

### V09-P1

Đầu vào: `食べなくて`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-negative-connective@1`, gốc **食べる** (verb): Thể nối phủ định なくて; có thể nối trạng thái hoặc nêu lý do tùy câu. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| なく | 2–4 | Phủ định ở dạng nối. |
| て | 4–5 | Nối với phần câu tiếp theo. |

### V09-P2

Đầu vào: `書かなくて`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-negative-connective@1`, gốc **書く** (verb): Thể nối phủ định なくて; có thể nối trạng thái hoặc nêu lý do tùy câu. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| なく | 2–4 | Phủ định ở dạng nối. |
| て | 4–5 | Nối với phần câu tiếp theo. |

### V09-IRR-suru

Đầu vào: `しなくて`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-negative-connective@1`, gốc **する** (verb): Thể nối phủ định なくて; có thể nối trạng thái hoặc nêu lý do tùy câu. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| なく | 1–3 | Phủ định ở dạng nối. |
| て | 3–4 | Nối câu. |

### V09-IRR-kuru

Đầu vào: `来なくて`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-negative-connective@1`, gốc **来る** (verb): Thể nối phủ định なくて; có thể nối trạng thái hoặc nêu lý do tùy câu. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| なく | 1–3 | Phủ định ở dạng nối. |
| て | 3–4 | Nối câu. |

### V09-WITHOUT-P1

Đầu vào: `食べないで`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-negative-connective:without@1`, gốc **食べる** (verb): Dạng ないで: không thực hiện hành động, phân biệt với なくて. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| ない | 2–4 | Phủ định. |
| で | 4–5 | Không làm hành động này; quan hệ với phần tiếp theo tùy câu. |

### V09-WITHOUT-P2

Đầu vào: `書かないで`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-negative-connective:without@1`, gốc **書く** (verb): Dạng ないで: không thực hiện hành động, phân biệt với なくて. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| ない | 2–4 | Phủ định. |
| で | 4–5 | Không làm hành động này; quan hệ với phần tiếp theo tùy câu. |

### V09-N1

Đầu vào: `食べなくて。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-negative-connective`.

### NEG-RULE-verb-negative-connective:without

Đầu vào: `食べないで。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-negative-connective:without`.

## V10 — Khả năng

### V10-P1

Đầu vào: `書ける`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-potential@1`, gốc **書く** (verb): Thể khả năng: có thể thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書け | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| る | 2–3 | Đuôi không quá khứ của dạng đang được phân tích. |

### V10-P2

Đầu vào: `読める`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-potential@1`, gốc **読む** (verb): Thể khả năng: có thể thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読め | 0–2 | Phần động từ của 読む ở dạng quan sát này. |
| る | 2–3 | Đuôi không quá khứ của dạng đang được phân tích. |

### V10-C-plain_past-P1

Đầu vào: `書けた`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-potential:plain-past@1`, gốc **書く** (verb): Thể khả năng: có thể thực hiện hành động; quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書け | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| た | 2–3 | Đánh dấu quá khứ. |

### V10-C-plain_past-P2

Đầu vào: `読めた`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-potential:plain-past@1`, gốc **読む** (verb): Thể khả năng: có thể thực hiện hành động; quá khứ, thông thường. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読め | 0–2 | Phần động từ của 読む ở dạng quan sát này. |
| た | 2–3 | Đánh dấu quá khứ. |

### V10-C-plain_negative-P1

Đầu vào: `書けない`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-potential:plain-negative@1`, gốc **書く** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書け | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| ない | 2–4 | Phủ định, không quá khứ. |

### V10-C-plain_negative-P2

Đầu vào: `読めない`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-potential:plain-negative@1`, gốc **読む** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, không quá khứ, thông thường. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読め | 0–2 | Phần động từ của 読む ở dạng quan sát này. |
| ない | 2–4 | Phủ định, không quá khứ. |

### V10-C-plain_negative_past-P1

Đầu vào: `書けなかった`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-potential:plain-negative-past@1`, gốc **書く** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書け | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| なかっ | 2–5 | Phần phủ định nối với đuôi quá khứ. |
| た | 5–6 | Đánh dấu quá khứ. |

### V10-C-plain_negative_past-P2

Đầu vào: `読めなかった`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-potential:plain-negative-past@1`, gốc **読む** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, quá khứ, thông thường. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読め | 0–2 | Phần động từ của 読む ở dạng quan sát này. |
| なかっ | 2–5 | Phần phủ định nối với đuôi quá khứ. |
| た | 5–6 | Đánh dấu quá khứ. |

### V10-C-polite_nonpast-P1

Đầu vào: `書けます`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-potential:polite-nonpast@1`, gốc **書く** (verb): Thể khả năng: có thể thực hiện hành động; không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書け | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| ます | 2–4 | Lịch sự, không quá khứ. |

### V10-C-polite_nonpast-P2

Đầu vào: `読めます`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-potential:polite-nonpast@1`, gốc **読む** (verb): Thể khả năng: có thể thực hiện hành động; không quá khứ, lịch sự. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読め | 0–2 | Phần động từ của 読む ở dạng quan sát này. |
| ます | 2–4 | Lịch sự, không quá khứ. |

### V10-C-polite_past-P1

Đầu vào: `書けました`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-potential:polite-past@1`, gốc **書く** (verb): Thể khả năng: có thể thực hiện hành động; quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書け | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| まし | 2–4 | Phần lịch sự nối với quá khứ. |
| た | 4–5 | Đánh dấu quá khứ. |

### V10-C-polite_past-P2

Đầu vào: `読めました`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-potential:polite-past@1`, gốc **読む** (verb): Thể khả năng: có thể thực hiện hành động; quá khứ, lịch sự. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読め | 0–2 | Phần động từ của 読む ở dạng quan sát này. |
| まし | 2–4 | Phần lịch sự nối với quá khứ. |
| た | 4–5 | Đánh dấu quá khứ. |

### V10-C-polite_negative-P1

Đầu vào: `書けません`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-potential:polite-negative@1`, gốc **書く** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書け | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| ませ | 2–4 | Phần lịch sự nối với phủ định. |
| ん | 4–5 | Phủ định trong chuỗi lịch sự. |

### V10-C-polite_negative-P2

Đầu vào: `読めません`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-potential:polite-negative@1`, gốc **読む** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, không quá khứ, lịch sự. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読め | 0–2 | Phần động từ của 読む ở dạng quan sát này. |
| ませ | 2–4 | Phần lịch sự nối với phủ định. |
| ん | 4–5 | Phủ định trong chuỗi lịch sự. |

### V10-C-polite_negative_past-P1

Đầu vào: `書けませんでした`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-potential:polite-negative-past@1`, gốc **書く** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書け | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| ませ | 2–4 | Phần lịch sự nối với phủ định. |
| ん | 4–5 | Phủ định trong chuỗi lịch sự. |
| でし | 5–7 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 7–8 | Đánh dấu quá khứ. |

### V10-C-polite_negative_past-P2

Đầu vào: `読めませんでした`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-potential:polite-negative-past@1`, gốc **読む** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, quá khứ, lịch sự. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読め | 0–2 | Phần động từ của 読む ở dạng quan sát này. |
| ませ | 2–4 | Phần lịch sự nối với phủ định. |
| ん | 4–5 | Phủ định trong chuỗi lịch sự. |
| でし | 5–7 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 7–8 | Đánh dấu quá khứ. |

### V10-IRR-suru

Đầu vào: `できる`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-potential@1`, gốc **する** (verb): Thể khả năng: có thể thực hiện hành động. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| できる | 0–3 | Dạng khả năng bất quy tắc của する: có thể làm. |

### V10-IRR-kuru

Đầu vào: `来られる`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-passive@1`, gốc **来る** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| られ | 1–3 | Phần bị động trong cách hiểu này. |
| る | 3–4 | Đuôi không quá khứ của dạng đang được phân tích. |

Phương án `conjugation:verb-potential@1`, gốc **来る** (verb): Thể khả năng: có thể thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| られ | 1–3 | Phần khả năng trong cách hiểu này. |
| る | 3–4 | Đuôi không quá khứ của dạng đang được phân tích. |

### EXACT-DERIVED-書ける

Đầu vào: `書ける`; fixture: fixture:016; kết quả: **exact_first**.

### EXACT-DERIVED-できる

Đầu vào: `できる`; fixture: fixture:017; kết quả: **exact_first**.

### V10-N1

Đầu vào: `書ける。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-potential`.

### NEG-RULE-verb-potential:plain-past

Đầu vào: `書けた。`; fixture: fixture:002; kết quả: **unavailable**.

Không được gán: `conjugation:verb-potential:plain-past`.

### NEG-RULE-verb-potential:plain-negative

Đầu vào: `書けない。`; fixture: fixture:002; kết quả: **unavailable**.

Không được gán: `conjugation:verb-potential:plain-negative`.

### NEG-RULE-verb-potential:plain-negative-past

Đầu vào: `書けなかった。`; fixture: fixture:002; kết quả: **unavailable**.

Không được gán: `conjugation:verb-potential:plain-negative-past`.

### NEG-RULE-verb-potential:polite-nonpast

Đầu vào: `書けます。`; fixture: fixture:002; kết quả: **unavailable**.

Không được gán: `conjugation:verb-potential:polite-nonpast`.

### NEG-RULE-verb-potential:polite-past

Đầu vào: `書けました。`; fixture: fixture:002; kết quả: **unavailable**.

Không được gán: `conjugation:verb-potential:polite-past`.

### NEG-RULE-verb-potential:polite-negative

Đầu vào: `書けません。`; fixture: fixture:002; kết quả: **unavailable**.

Không được gán: `conjugation:verb-potential:polite-negative`.

### NEG-RULE-verb-potential:polite-negative-past

Đầu vào: `書けませんでした。`; fixture: fixture:002; kết quả: **unavailable**.

Không được gán: `conjugation:verb-potential:polite-negative-past`.

## V11 — Bị động

### V11-P1

Đầu vào: `食べられる`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-passive@1`, gốc **食べる** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần bị động trong cách hiểu này. |
| る | 4–5 | Đuôi không quá khứ của dạng đang được phân tích. |

Phương án `conjugation:verb-potential@1`, gốc **食べる** (verb): Thể khả năng: có thể thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần khả năng trong cách hiểu này. |
| る | 4–5 | Đuôi không quá khứ của dạng đang được phân tích. |

### V11-P2

Đầu vào: `書かれる`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-passive@1`, gốc **書く** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| れ | 2–3 | Phần bị động trong cách hiểu này. |
| る | 3–4 | Đuôi không quá khứ của dạng đang được phân tích. |

### V11-C-plain_past-P1

Đầu vào: `食べられた`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-passive:plain-past@1`, gốc **食べる** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần bị động trong cách hiểu này. |
| た | 4–5 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-potential:plain-past@1`, gốc **食べる** (verb): Thể khả năng: có thể thực hiện hành động; quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần khả năng trong cách hiểu này. |
| た | 4–5 | Đánh dấu quá khứ. |

### V11-C-plain_past-P2

Đầu vào: `書かれた`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-passive:plain-past@1`, gốc **書く** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| れ | 2–3 | Phần bị động trong cách hiểu này. |
| た | 3–4 | Đánh dấu quá khứ. |

### V11-C-plain_negative-P1

Đầu vào: `食べられない`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-passive:plain-negative@1`, gốc **食べる** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần bị động trong cách hiểu này. |
| ない | 4–6 | Phủ định, không quá khứ. |

Phương án `conjugation:verb-potential:plain-negative@1`, gốc **食べる** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần khả năng trong cách hiểu này. |
| ない | 4–6 | Phủ định, không quá khứ. |

### V11-C-plain_negative-P2

Đầu vào: `書かれない`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-passive:plain-negative@1`, gốc **書く** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| れ | 2–3 | Phần bị động trong cách hiểu này. |
| ない | 3–5 | Phủ định, không quá khứ. |

### V11-C-plain_negative_past-P1

Đầu vào: `食べられなかった`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-passive:plain-negative-past@1`, gốc **食べる** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần bị động trong cách hiểu này. |
| なかっ | 4–7 | Phần phủ định nối với đuôi quá khứ. |
| た | 7–8 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-potential:plain-negative-past@1`, gốc **食べる** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần khả năng trong cách hiểu này. |
| なかっ | 4–7 | Phần phủ định nối với đuôi quá khứ. |
| た | 7–8 | Đánh dấu quá khứ. |

### V11-C-plain_negative_past-P2

Đầu vào: `書かれなかった`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-passive:plain-negative-past@1`, gốc **書く** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| れ | 2–3 | Phần bị động trong cách hiểu này. |
| なかっ | 3–6 | Phần phủ định nối với đuôi quá khứ. |
| た | 6–7 | Đánh dấu quá khứ. |

### V11-C-polite_nonpast-P1

Đầu vào: `食べられます`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-passive:polite-nonpast@1`, gốc **食べる** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần bị động trong cách hiểu này. |
| ます | 4–6 | Lịch sự, không quá khứ. |

Phương án `conjugation:verb-potential:polite-nonpast@1`, gốc **食べる** (verb): Thể khả năng: có thể thực hiện hành động; không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần khả năng trong cách hiểu này. |
| ます | 4–6 | Lịch sự, không quá khứ. |

### V11-C-polite_nonpast-P2

Đầu vào: `書かれます`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-passive:polite-nonpast@1`, gốc **書く** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| れ | 2–3 | Phần bị động trong cách hiểu này. |
| ます | 3–5 | Lịch sự, không quá khứ. |

### V11-C-polite_past-P1

Đầu vào: `食べられました`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-passive:polite-past@1`, gốc **食べる** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần bị động trong cách hiểu này. |
| まし | 4–6 | Phần lịch sự nối với quá khứ. |
| た | 6–7 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-potential:polite-past@1`, gốc **食べる** (verb): Thể khả năng: có thể thực hiện hành động; quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần khả năng trong cách hiểu này. |
| まし | 4–6 | Phần lịch sự nối với quá khứ. |
| た | 6–7 | Đánh dấu quá khứ. |

### V11-C-polite_past-P2

Đầu vào: `書かれました`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-passive:polite-past@1`, gốc **書く** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| れ | 2–3 | Phần bị động trong cách hiểu này. |
| まし | 3–5 | Phần lịch sự nối với quá khứ. |
| た | 5–6 | Đánh dấu quá khứ. |

### V11-C-polite_negative-P1

Đầu vào: `食べられません`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-passive:polite-negative@1`, gốc **食べる** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần bị động trong cách hiểu này. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |

Phương án `conjugation:verb-potential:polite-negative@1`, gốc **食べる** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần khả năng trong cách hiểu này. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |

### V11-C-polite_negative-P2

Đầu vào: `書かれません`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-passive:polite-negative@1`, gốc **書く** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| れ | 2–3 | Phần bị động trong cách hiểu này. |
| ませ | 3–5 | Phần lịch sự nối với phủ định. |
| ん | 5–6 | Phủ định trong chuỗi lịch sự. |

### V11-C-polite_negative_past-P1

Đầu vào: `食べられませんでした`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-passive:polite-negative-past@1`, gốc **食べる** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần bị động trong cách hiểu này. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |
| でし | 7–9 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 9–10 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-potential:polite-negative-past@1`, gốc **食べる** (verb): Thể khả năng: có thể thực hiện hành động; phủ định, quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần khả năng trong cách hiểu này. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |
| でし | 7–9 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 9–10 | Đánh dấu quá khứ. |

### V11-C-polite_negative_past-P2

Đầu vào: `書かれませんでした`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-passive:polite-negative-past@1`, gốc **書く** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| れ | 2–3 | Phần bị động trong cách hiểu này. |
| ませ | 3–5 | Phần lịch sự nối với phủ định. |
| ん | 5–6 | Phủ định trong chuỗi lịch sự. |
| でし | 6–8 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 8–9 | Đánh dấu quá khứ. |

### V11-IRR-suru

Đầu vào: `される`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-passive@1`, gốc **する** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| さ | 0–1 | Phần động từ của する ở dạng quan sát này. |
| れ | 1–2 | Phần bị động trong cách hiểu này. |
| る | 2–3 | Đuôi không quá khứ của dạng đang được phân tích. |

### V11-IRR-kuru

Đầu vào: `来られる`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-passive@1`, gốc **来る** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| られ | 1–3 | Phần bị động trong cách hiểu này. |
| る | 3–4 | Đuôi không quá khứ của dạng đang được phân tích. |

Phương án `conjugation:verb-potential@1`, gốc **来る** (verb): Thể khả năng: có thể thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| られ | 1–3 | Phần khả năng trong cách hiểu này. |
| る | 3–4 | Đuôi không quá khứ của dạng đang được phân tích. |

### AMB-01

Đầu vào: `食べられる`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-passive@1`, gốc **食べる** (verb): Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần bị động trong cách hiểu này. |
| る | 4–5 | Đuôi không quá khứ của dạng đang được phân tích. |

Phương án `conjugation:verb-potential@1`, gốc **食べる** (verb): Thể khả năng: có thể thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| られ | 2–4 | Phần khả năng trong cách hiểu này. |
| る | 4–5 | Đuôi không quá khứ của dạng đang được phân tích. |

### V11-N1

Đầu vào: `書かれる。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-passive`.

### NEG-RULE-verb-passive:plain-past

Đầu vào: `食べられた。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-passive:plain-past`.

### NEG-RULE-verb-passive:plain-negative

Đầu vào: `食べられない。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-passive:plain-negative`.

### NEG-RULE-verb-passive:plain-negative-past

Đầu vào: `食べられなかった。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-passive:plain-negative-past`.

### NEG-RULE-verb-passive:polite-nonpast

Đầu vào: `食べられます。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-passive:polite-nonpast`.

### NEG-RULE-verb-passive:polite-past

Đầu vào: `食べられました。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-passive:polite-past`.

### NEG-RULE-verb-passive:polite-negative

Đầu vào: `食べられません。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-passive:polite-negative`.

### NEG-RULE-verb-passive:polite-negative-past

Đầu vào: `食べられませんでした。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-passive:polite-negative-past`.

### ASYNC-01

Đầu vào: `食べられる`; fixture: fixture:001; kết quả: **discarded**.

Tình huống: `{"fault":"selection_changed_before_response","next_text":"学生です"}`.

## V12 — Sai khiến

### V12-P1

Đầu vào: `食べさせる`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative@1`, gốc **食べる** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| る | 4–5 | Đuôi không quá khứ của dạng đang được phân tích. |

### V12-P2

Đầu vào: `書かせる`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative@1`, gốc **書く** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| る | 3–4 | Đuôi không quá khứ của dạng đang được phân tích. |

### V12-C-plain_past-P1

Đầu vào: `食べさせた`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative:plain-past@1`, gốc **食べる** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| た | 4–5 | Đánh dấu quá khứ. |

### V12-C-plain_past-P2

Đầu vào: `書かせた`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative:plain-past@1`, gốc **書く** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| た | 3–4 | Đánh dấu quá khứ. |

### V12-C-plain_negative-P1

Đầu vào: `食べさせない`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative:plain-negative@1`, gốc **食べる** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; phủ định, không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| ない | 4–6 | Phủ định, không quá khứ. |

### V12-C-plain_negative-P2

Đầu vào: `書かせない`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative:plain-negative@1`, gốc **書く** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; phủ định, không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| ない | 3–5 | Phủ định, không quá khứ. |

### V12-C-plain_negative_past-P1

Đầu vào: `食べさせなかった`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative:plain-negative-past@1`, gốc **食べる** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; phủ định, quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| なかっ | 4–7 | Phần phủ định nối với đuôi quá khứ. |
| た | 7–8 | Đánh dấu quá khứ. |

### V12-C-plain_negative_past-P2

Đầu vào: `書かせなかった`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative:plain-negative-past@1`, gốc **書く** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; phủ định, quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| なかっ | 3–6 | Phần phủ định nối với đuôi quá khứ. |
| た | 6–7 | Đánh dấu quá khứ. |

### V12-C-polite_nonpast-P1

Đầu vào: `食べさせます`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative:polite-nonpast@1`, gốc **食べる** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| ます | 4–6 | Lịch sự, không quá khứ. |

### V12-C-polite_nonpast-P2

Đầu vào: `書かせます`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative:polite-nonpast@1`, gốc **書く** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| ます | 3–5 | Lịch sự, không quá khứ. |

### V12-C-polite_past-P1

Đầu vào: `食べさせました`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative:polite-past@1`, gốc **食べる** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| まし | 4–6 | Phần lịch sự nối với quá khứ. |
| た | 6–7 | Đánh dấu quá khứ. |

### V12-C-polite_past-P2

Đầu vào: `書かせました`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative:polite-past@1`, gốc **書く** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| まし | 3–5 | Phần lịch sự nối với quá khứ. |
| た | 5–6 | Đánh dấu quá khứ. |

### V12-C-polite_negative-P1

Đầu vào: `食べさせません`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative:polite-negative@1`, gốc **食べる** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; phủ định, không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |

### V12-C-polite_negative-P2

Đầu vào: `書かせません`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative:polite-negative@1`, gốc **書く** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; phủ định, không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| ませ | 3–5 | Phần lịch sự nối với phủ định. |
| ん | 5–6 | Phủ định trong chuỗi lịch sự. |

### V12-C-polite_negative_past-P1

Đầu vào: `食べさせませんでした`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative:polite-negative-past@1`, gốc **食べる** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; phủ định, quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |
| でし | 7–9 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 9–10 | Đánh dấu quá khứ. |

### V12-C-polite_negative_past-P2

Đầu vào: `書かせませんでした`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative:polite-negative-past@1`, gốc **書く** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; phủ định, quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| ませ | 3–5 | Phần lịch sự nối với phủ định. |
| ん | 5–6 | Phủ định trong chuỗi lịch sự. |
| でし | 6–8 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 8–9 | Đánh dấu quá khứ. |

### V12-IRR-suru

Đầu vào: `させる`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-causative@1`, gốc **する** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; không quá khứ, thông thường. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| さ | 0–1 | Phần động từ của する ở dạng quan sát này. |
| せ | 1–2 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| る | 2–3 | Đuôi không quá khứ của dạng đang được phân tích. |

### V12-IRR-kuru

Đầu vào: `来させる`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-causative@1`, gốc **来る** (verb): Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh; không quá khứ, thông thường. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| させ | 1–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| る | 3–4 | Đuôi không quá khứ của dạng đang được phân tích. |

### V12-N1

Đầu vào: `食べさせる。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative`.

### NEG-RULE-verb-causative:plain-past

Đầu vào: `食べさせた。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative:plain-past`.

### NEG-RULE-verb-causative:plain-negative

Đầu vào: `食べさせない。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative:plain-negative`.

### NEG-RULE-verb-causative:plain-negative-past

Đầu vào: `食べさせなかった。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative:plain-negative-past`.

### NEG-RULE-verb-causative:polite-nonpast

Đầu vào: `食べさせます。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative:polite-nonpast`.

### NEG-RULE-verb-causative:polite-past

Đầu vào: `食べさせました。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative:polite-past`.

### NEG-RULE-verb-causative:polite-negative

Đầu vào: `食べさせません。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative:polite-negative`.

### NEG-RULE-verb-causative:polite-negative-past

Đầu vào: `食べさせませんでした。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative:polite-negative-past`.

## V13 — Sai khiến-bị động

### V13-P1

Đầu vào: `食べさせられる`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive@1`, gốc **食べる** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần bị động trong cách hiểu này. |
| る | 6–7 | Đuôi không quá khứ của dạng đang được phân tích. |

Phương án `conjugation:verb-causative-potential@1`, gốc **食べる** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| る | 6–7 | Đuôi không quá khứ của dạng đang được phân tích. |

### V13-P2

Đầu vào: `書かせられる`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive@1`, gốc **書く** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần bị động trong cách hiểu này. |
| る | 5–6 | Đuôi không quá khứ của dạng đang được phân tích. |

Phương án `conjugation:verb-causative-potential@1`, gốc **書く** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| る | 5–6 | Đuôi không quá khứ của dạng đang được phân tích. |

### V13-C-plain_past-P1

Đầu vào: `食べさせられた`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:plain-past@1`, gốc **食べる** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần bị động trong cách hiểu này. |
| た | 6–7 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-causative-potential:plain-past@1`, gốc **食べる** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| た | 6–7 | Đánh dấu quá khứ. |

### V13-C-plain_past-P2

Đầu vào: `書かせられた`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:plain-past@1`, gốc **書く** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần bị động trong cách hiểu này. |
| た | 5–6 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-causative-potential:plain-past@1`, gốc **書く** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| た | 5–6 | Đánh dấu quá khứ. |

### V13-C-plain_negative-P1

Đầu vào: `食べさせられない`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:plain-negative@1`, gốc **食べる** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; phủ định, không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần bị động trong cách hiểu này. |
| ない | 6–8 | Phủ định, không quá khứ. |

Phương án `conjugation:verb-causative-potential:plain-negative@1`, gốc **食べる** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; phủ định, không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| ない | 6–8 | Phủ định, không quá khứ. |

### V13-C-plain_negative-P2

Đầu vào: `書かせられない`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:plain-negative@1`, gốc **書く** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; phủ định, không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần bị động trong cách hiểu này. |
| ない | 5–7 | Phủ định, không quá khứ. |

Phương án `conjugation:verb-causative-potential:plain-negative@1`, gốc **書く** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; phủ định, không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| ない | 5–7 | Phủ định, không quá khứ. |

### V13-C-plain_negative_past-P1

Đầu vào: `食べさせられなかった`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:plain-negative-past@1`, gốc **食べる** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; phủ định, quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần bị động trong cách hiểu này. |
| なかっ | 6–9 | Phần phủ định nối với đuôi quá khứ. |
| た | 9–10 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-causative-potential:plain-negative-past@1`, gốc **食べる** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; phủ định, quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| なかっ | 6–9 | Phần phủ định nối với đuôi quá khứ. |
| た | 9–10 | Đánh dấu quá khứ. |

### V13-C-plain_negative_past-P2

Đầu vào: `書かせられなかった`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:plain-negative-past@1`, gốc **書く** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; phủ định, quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần bị động trong cách hiểu này. |
| なかっ | 5–8 | Phần phủ định nối với đuôi quá khứ. |
| た | 8–9 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-causative-potential:plain-negative-past@1`, gốc **書く** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; phủ định, quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| なかっ | 5–8 | Phần phủ định nối với đuôi quá khứ. |
| た | 8–9 | Đánh dấu quá khứ. |

### V13-C-polite_nonpast-P1

Đầu vào: `食べさせられます`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:polite-nonpast@1`, gốc **食べる** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần bị động trong cách hiểu này. |
| ます | 6–8 | Lịch sự, không quá khứ. |

Phương án `conjugation:verb-causative-potential:polite-nonpast@1`, gốc **食べる** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| ます | 6–8 | Lịch sự, không quá khứ. |

### V13-C-polite_nonpast-P2

Đầu vào: `書かせられます`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:polite-nonpast@1`, gốc **書く** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần bị động trong cách hiểu này. |
| ます | 5–7 | Lịch sự, không quá khứ. |

Phương án `conjugation:verb-causative-potential:polite-nonpast@1`, gốc **書く** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| ます | 5–7 | Lịch sự, không quá khứ. |

### V13-C-polite_past-P1

Đầu vào: `食べさせられました`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:polite-past@1`, gốc **食べる** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần bị động trong cách hiểu này. |
| まし | 6–8 | Phần lịch sự nối với quá khứ. |
| た | 8–9 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-causative-potential:polite-past@1`, gốc **食べる** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| まし | 6–8 | Phần lịch sự nối với quá khứ. |
| た | 8–9 | Đánh dấu quá khứ. |

### V13-C-polite_past-P2

Đầu vào: `書かせられました`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:polite-past@1`, gốc **書く** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần bị động trong cách hiểu này. |
| まし | 5–7 | Phần lịch sự nối với quá khứ. |
| た | 7–8 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-causative-potential:polite-past@1`, gốc **書く** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| まし | 5–7 | Phần lịch sự nối với quá khứ. |
| た | 7–8 | Đánh dấu quá khứ. |

### V13-C-polite_negative-P1

Đầu vào: `食べさせられません`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:polite-negative@1`, gốc **食べる** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; phủ định, không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần bị động trong cách hiểu này. |
| ませ | 6–8 | Phần lịch sự nối với phủ định. |
| ん | 8–9 | Phủ định trong chuỗi lịch sự. |

Phương án `conjugation:verb-causative-potential:polite-negative@1`, gốc **食べる** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; phủ định, không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| ませ | 6–8 | Phần lịch sự nối với phủ định. |
| ん | 8–9 | Phủ định trong chuỗi lịch sự. |

### V13-C-polite_negative-P2

Đầu vào: `書かせられません`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:polite-negative@1`, gốc **書く** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; phủ định, không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần bị động trong cách hiểu này. |
| ませ | 5–7 | Phần lịch sự nối với phủ định. |
| ん | 7–8 | Phủ định trong chuỗi lịch sự. |

Phương án `conjugation:verb-causative-potential:polite-negative@1`, gốc **書く** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; phủ định, không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| ませ | 5–7 | Phần lịch sự nối với phủ định. |
| ん | 7–8 | Phủ định trong chuỗi lịch sự. |

### V13-C-polite_negative_past-P1

Đầu vào: `食べさせられませんでした`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:polite-negative-past@1`, gốc **食べる** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; phủ định, quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần bị động trong cách hiểu này. |
| ませ | 6–8 | Phần lịch sự nối với phủ định. |
| ん | 8–9 | Phủ định trong chuỗi lịch sự. |
| でし | 9–11 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 11–12 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-causative-potential:polite-negative-past@1`, gốc **食べる** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; phủ định, quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| させ | 2–4 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 4–6 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| ませ | 6–8 | Phần lịch sự nối với phủ định. |
| ん | 8–9 | Phủ định trong chuỗi lịch sự. |
| でし | 9–11 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 11–12 | Đánh dấu quá khứ. |

### V13-C-polite_negative_past-P2

Đầu vào: `書かせられませんでした`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:polite-negative-past@1`, gốc **書く** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; phủ định, quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần bị động trong cách hiểu này. |
| ませ | 5–7 | Phần lịch sự nối với phủ định. |
| ん | 7–8 | Phủ định trong chuỗi lịch sự. |
| でし | 8–10 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 10–11 | Đánh dấu quá khứ. |

Phương án `conjugation:verb-causative-potential:polite-negative-past@1`, gốc **書く** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; phủ định, quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| せ | 2–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| ませ | 5–7 | Phần lịch sự nối với phủ định. |
| ん | 7–8 | Phủ định trong chuỗi lịch sự. |
| でし | 8–10 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 10–11 | Đánh dấu quá khứ. |

### V13-IRR-suru

Đầu vào: `させられる`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive@1`, gốc **する** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; không quá khứ, thông thường. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| さ | 0–1 | Phần động từ của する ở dạng quan sát này. |
| せ | 1–2 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 2–4 | Phần bị động trong cách hiểu này. |
| る | 4–5 | Đuôi không quá khứ của dạng đang được phân tích. |

Phương án `conjugation:verb-causative-potential@1`, gốc **する** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; không quá khứ, thông thường. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| さ | 0–1 | Phần động từ của する ở dạng quan sát này. |
| せ | 1–2 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 2–4 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| る | 4–5 | Đuôi không quá khứ của dạng đang được phân tích. |

### V13-IRR-kuru

Đầu vào: `来させられる`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive@1`, gốc **来る** (verb): Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| させ | 1–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần bị động trong cách hiểu này. |
| る | 5–6 | Đuôi không quá khứ của dạng đang được phân tích. |

Phương án `conjugation:verb-causative-potential@1`, gốc **来る** (verb): Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| させ | 1–3 | Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. |
| られ | 3–5 | Phần khả năng của dạng sai khiến trong cách hiểu này. |
| る | 5–6 | Đuôi không quá khứ của dạng đang được phân tích. |

### V13-SHORT-書く

Đầu vào: `書かされる`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:contracted@1`, gốc **書く** (verb): Sai khiến-bị động dạng rút gọn, không quá khứ. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書か | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| され | 2–4 | Dạng rút gọn sai khiến-bị động ở cách hiểu này. |
| る | 4–5 | Không quá khứ. |

### V13-SHORT-読む

Đầu vào: `読まされる`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-causative-passive:contracted@1`, gốc **読む** (verb): Sai khiến-bị động dạng rút gọn, không quá khứ. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読ま | 0–2 | Phần động từ của 読む ở dạng quan sát này. |
| され | 2–4 | Dạng rút gọn sai khiến-bị động ở cách hiểu này. |
| る | 4–5 | Không quá khứ. |

### V13-N1

Đầu vào: `食べさせられる。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-passive`.

### NEG-RULE-verb-causative-potential

Đầu vào: `食べさせられる。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-potential`.

### NEG-RULE-verb-causative-passive:plain-past

Đầu vào: `食べさせられた。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-passive:plain-past`.

### NEG-RULE-verb-causative-potential:plain-past

Đầu vào: `食べさせられた。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-potential:plain-past`.

### NEG-RULE-verb-causative-passive:plain-negative

Đầu vào: `食べさせられない。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-passive:plain-negative`.

### NEG-RULE-verb-causative-potential:plain-negative

Đầu vào: `食べさせられない。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-potential:plain-negative`.

### NEG-RULE-verb-causative-passive:plain-negative-past

Đầu vào: `食べさせられなかった。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-passive:plain-negative-past`.

### NEG-RULE-verb-causative-potential:plain-negative-past

Đầu vào: `食べさせられなかった。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-potential:plain-negative-past`.

### NEG-RULE-verb-causative-passive:polite-nonpast

Đầu vào: `食べさせられます。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-passive:polite-nonpast`.

### NEG-RULE-verb-causative-potential:polite-nonpast

Đầu vào: `食べさせられます。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-potential:polite-nonpast`.

### NEG-RULE-verb-causative-passive:polite-past

Đầu vào: `食べさせられました。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-passive:polite-past`.

### NEG-RULE-verb-causative-potential:polite-past

Đầu vào: `食べさせられました。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-potential:polite-past`.

### NEG-RULE-verb-causative-passive:polite-negative

Đầu vào: `食べさせられません。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-passive:polite-negative`.

### NEG-RULE-verb-causative-potential:polite-negative

Đầu vào: `食べさせられません。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-potential:polite-negative`.

### NEG-RULE-verb-causative-passive:polite-negative-past

Đầu vào: `食べさせられませんでした。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-passive:polite-negative-past`.

### NEG-RULE-verb-causative-potential:polite-negative-past

Đầu vào: `食べさせられませんでした。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-potential:polite-negative-past`.

### NEG-RULE-verb-causative-passive:contracted

Đầu vào: `書かされる。`; fixture: fixture:002; kết quả: **unavailable**.

Không được gán: `conjugation:verb-causative-passive:contracted`.

## V14 — Điều kiện ば

### V14-P1

Đầu vào: `食べれば`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-conditional-ba@1`, gốc **食べる** (verb): Thể điều kiện ば: nếu thỏa điều kiện. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べれ | 0–3 | Phần động từ của 食べる ở dạng quan sát này. |
| ば | 3–4 | Đánh dấu điều kiện ば. |

### V14-P2

Đầu vào: `書けば`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-conditional-ba@1`, gốc **書く** (verb): Thể điều kiện ば: nếu thỏa điều kiện. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書け | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| ば | 2–3 | Đánh dấu điều kiện ば. |

### V14-IRR-suru

Đầu vào: `すれば`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-conditional-ba@1`, gốc **する** (verb): Thể điều kiện ば: nếu thỏa điều kiện. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| すれ | 0–2 | Phần động từ của する ở dạng quan sát này. |
| ば | 2–3 | Điều kiện ば. |

### V14-IRR-kuru

Đầu vào: `来れば`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-conditional-ba@1`, gốc **来る** (verb): Thể điều kiện ば: nếu thỏa điều kiện. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来れ | 0–2 | Phần động từ của 来る ở dạng quan sát này. |
| ば | 2–3 | Điều kiện ば. |

### V14-N1

Đầu vào: `食べれば。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-conditional-ba`.

## V15 — Điều kiện たら

### V15-P1

Đầu vào: `食べたら`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-conditional-tara@1`, gốc **食べる** (verb): Thể điều kiện たら: nếu/khi sự việc xảy ra, tùy ngữ cảnh. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| たら | 2–4 | Đánh dấu điều kiện たら/だら. |

### V15-P2

Đầu vào: `書いたら`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-conditional-tara@1`, gốc **書く** (verb): Thể điều kiện たら: nếu/khi sự việc xảy ra, tùy ngữ cảnh. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書い | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| たら | 2–4 | Đánh dấu điều kiện たら/だら. |

### V15-IRR-suru

Đầu vào: `したら`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-conditional-tara@1`, gốc **する** (verb): Thể điều kiện たら: nếu/khi sự việc xảy ra, tùy ngữ cảnh. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| たら | 1–3 | Điều kiện たら. |

### V15-IRR-kuru

Đầu vào: `来たら`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-conditional-tara@1`, gốc **来る** (verb): Thể điều kiện たら: nếu/khi sự việc xảy ra, tùy ngữ cảnh. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| たら | 1–3 | Điều kiện たら. |

### V15-N1

Đầu vào: `食べたら。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-conditional-tara`.

## V16 — Ý chí

### V16-P1

Đầu vào: `食べよう`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-volitional@1`, gốc **食べる** (verb): Thể ý chí/đề nghị: dự định, quyết định hoặc đề nghị cùng thực hiện, tùy ngữ cảnh. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| よう | 2–4 | Ý chí, dự định hoặc đề nghị cùng làm tùy câu. |

### V16-P2

Đầu vào: `書こう`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-volitional@1`, gốc **書く** (verb): Thể ý chí/đề nghị: dự định, quyết định hoặc đề nghị cùng thực hiện, tùy ngữ cảnh. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書こ | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| う | 2–3 | Ý chí, dự định hoặc đề nghị cùng làm tùy câu. |

### V16-IRR-suru

Đầu vào: `しよう`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-volitional@1`, gốc **する** (verb): Thể ý chí/đề nghị: dự định, quyết định hoặc đề nghị cùng thực hiện, tùy ngữ cảnh. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| よう | 1–3 | Ý chí/đề nghị tùy câu. |

### V16-IRR-kuru

Đầu vào: `来よう`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-volitional@1`, gốc **来る** (verb): Thể ý chí/đề nghị: dự định, quyết định hoặc đề nghị cùng thực hiện, tùy ngữ cảnh. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| よう | 1–3 | Ý chí/đề nghị tùy câu. |

### V16-N1

Đầu vào: `食べよう。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-volitional`.

## V17 — Mệnh lệnh

### V17-P1

Đầu vào: `食べろ`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-imperative@1`, gốc **食べる** (verb): Thể mệnh lệnh trực tiếp. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べろ | 0–3 | Toàn bộ dạng mệnh lệnh của 食べる. |

### V17-P2

Đầu vào: `読め`; fixture: fixture:003; kết quả: **supported**.

Phương án `conjugation:verb-imperative@1`, gốc **読む** (verb): Thể mệnh lệnh trực tiếp. Dạng gốc: 読む.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 読め | 0–2 | Toàn bộ dạng mệnh lệnh của 読む. |

### V17-IRR-suru

Đầu vào: `しろ`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-imperative@1`, gốc **する** (verb): Thể mệnh lệnh trực tiếp. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| しろ | 0–2 | Dạng mệnh lệnh bất quy tắc của する. |

### V17-IRR-kuru

Đầu vào: `来い`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-imperative@1`, gốc **来る** (verb): Thể mệnh lệnh trực tiếp. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来い | 0–2 | Dạng mệnh lệnh bất quy tắc của 来る. |

### AMB-02

Đầu vào: `書け`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-imperative@1`, gốc **書く** (verb): Thể mệnh lệnh trực tiếp. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書け | 0–2 | Dạng mệnh lệnh trực tiếp của 書く. |

### V17-N1

Đầu vào: `食べろ。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-imperative`.

## V18 — Cấm đoán

### V18-P1

Đầu vào: `食べるな`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-prohibition@1`, gốc **食べる** (verb): Thể cấm đoán: đừng thực hiện hành động. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べる | 0–3 | Phần động từ của 食べる ở dạng quan sát này. |
| な | 3–4 | Cấm thực hiện hành động; đừng … |

### V18-P2

Đầu vào: `書くな`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-prohibition@1`, gốc **書く** (verb): Thể cấm đoán: đừng thực hiện hành động. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書く | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| な | 2–3 | Cấm thực hiện hành động; đừng … |

### V18-IRR-suru

Đầu vào: `するな`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-prohibition@1`, gốc **する** (verb): Thể cấm đoán: đừng thực hiện hành động. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| する | 0–2 | Phần động từ của する ở dạng quan sát này. |
| な | 2–3 | Cấm thực hiện hành động. |

### V18-IRR-kuru

Đầu vào: `来るな`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-prohibition@1`, gốc **来る** (verb): Thể cấm đoán: đừng thực hiện hành động. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来る | 0–2 | Phần động từ của 来る ở dạng quan sát này. |
| な | 2–3 | Cấm thực hiện hành động. |

### V18-N1

Đầu vào: `食べるな。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-prohibition`.

## V19 — Mong muốn たい

### V19-P1

Đầu vào: `食べたい`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-desiderative@1`, gốc **食べる** (verb): Dạng mong muốn: muốn thực hiện hành động. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| たい | 2–4 | Mong muốn thực hiện hành động. |

### V19-P2

Đầu vào: `書きたい`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-desiderative@1`, gốc **書く** (verb): Dạng mong muốn: muốn thực hiện hành động. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| たい | 2–4 | Mong muốn thực hiện hành động. |

### V19-C-plain_past-P1

Đầu vào: `食べたかった`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:plain-past@1`, gốc **食べる** (verb): Dạng mong muốn: muốn thực hiện hành động; quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| たかっ | 2–5 | Phần mong muốn たい ở dạng nối tương ứng. |
| た | 5–6 | Quá khứ. |

### V19-C-plain_past-P2

Đầu vào: `書きたかった`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:plain-past@1`, gốc **書く** (verb): Dạng mong muốn: muốn thực hiện hành động; quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| たかっ | 2–5 | Phần mong muốn たい ở dạng nối tương ứng. |
| た | 5–6 | Quá khứ. |

### V19-C-plain_negative-P1

Đầu vào: `食べたくない`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:plain-negative@1`, gốc **食べる** (verb): Dạng mong muốn: muốn thực hiện hành động; phủ định, không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| たく | 2–4 | Phần mong muốn たい ở dạng nối tương ứng. |
| ない | 4–6 | Phần phủ định. |

### V19-C-plain_negative-P2

Đầu vào: `書きたくない`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:plain-negative@1`, gốc **書く** (verb): Dạng mong muốn: muốn thực hiện hành động; phủ định, không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| たく | 2–4 | Phần mong muốn たい ở dạng nối tương ứng. |
| ない | 4–6 | Phần phủ định. |

### V19-C-plain_negative_past-P1

Đầu vào: `食べたくなかった`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:plain-negative-past@1`, gốc **食べる** (verb): Dạng mong muốn: muốn thực hiện hành động; phủ định, quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| たく | 2–4 | Phần mong muốn たい ở dạng nối tương ứng. |
| なかっ | 4–7 | Phần phủ định. |
| た | 7–8 | Quá khứ. |

### V19-C-plain_negative_past-P2

Đầu vào: `書きたくなかった`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:plain-negative-past@1`, gốc **書く** (verb): Dạng mong muốn: muốn thực hiện hành động; phủ định, quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| たく | 2–4 | Phần mong muốn たい ở dạng nối tương ứng. |
| なかっ | 4–7 | Phần phủ định. |
| た | 7–8 | Quá khứ. |

### V19-C-polite_nonpast-P1

Đầu vào: `食べたいです`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:polite-nonpast@1`, gốc **食べる** (verb): Dạng mong muốn: muốn thực hiện hành động; không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| たい | 2–4 | Phần mong muốn たい ở dạng nối tương ứng. |
| です | 4–6 | Lịch sự. |

### V19-C-polite_nonpast-P2

Đầu vào: `書きたいです`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:polite-nonpast@1`, gốc **書く** (verb): Dạng mong muốn: muốn thực hiện hành động; không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| たい | 2–4 | Phần mong muốn たい ở dạng nối tương ứng. |
| です | 4–6 | Lịch sự. |

### V19-C-polite_past-P1

Đầu vào: `食べたかったです`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:polite-past@1`, gốc **食べる** (verb): Dạng mong muốn: muốn thực hiện hành động; quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| たかっ | 2–5 | Phần mong muốn たい ở dạng nối tương ứng. |
| た | 5–6 | Quá khứ. |
| です | 6–8 | Lịch sự. |

### V19-C-polite_past-P2

Đầu vào: `書きたかったです`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:polite-past@1`, gốc **書く** (verb): Dạng mong muốn: muốn thực hiện hành động; quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| たかっ | 2–5 | Phần mong muốn たい ở dạng nối tương ứng. |
| た | 5–6 | Quá khứ. |
| です | 6–8 | Lịch sự. |

### V19-C-polite_negative-P1

Đầu vào: `食べたくないです`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:polite-negative@1`, gốc **食べる** (verb): Dạng mong muốn: muốn thực hiện hành động; phủ định, không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| たく | 2–4 | Phần mong muốn たい ở dạng nối tương ứng. |
| ない | 4–6 | Phần phủ định. |
| です | 6–8 | Lịch sự. |

### V19-C-polite_negative-P2

Đầu vào: `書きたくないです`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:polite-negative@1`, gốc **書く** (verb): Dạng mong muốn: muốn thực hiện hành động; phủ định, không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| たく | 2–4 | Phần mong muốn たい ở dạng nối tương ứng. |
| ない | 4–6 | Phần phủ định. |
| です | 6–8 | Lịch sự. |

### V19-C-polite_negative_past-P1

Đầu vào: `食べたくなかったです`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:polite-negative-past@1`, gốc **食べる** (verb): Dạng mong muốn: muốn thực hiện hành động; phủ định, quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| たく | 2–4 | Phần mong muốn たい ở dạng nối tương ứng. |
| なかっ | 4–7 | Phần phủ định. |
| た | 7–8 | Quá khứ. |
| です | 8–10 | Lịch sự. |

### V19-C-polite_negative_past-P2

Đầu vào: `書きたくなかったです`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-desiderative:polite-negative-past@1`, gốc **書く** (verb): Dạng mong muốn: muốn thực hiện hành động; phủ định, quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書き | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| たく | 2–4 | Phần mong muốn たい ở dạng nối tương ứng. |
| なかっ | 4–7 | Phần phủ định. |
| た | 7–8 | Quá khứ. |
| です | 8–10 | Lịch sự. |

### V19-IRR-suru

Đầu vào: `したい`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-desiderative@1`, gốc **する** (verb): Dạng mong muốn: muốn thực hiện hành động. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| たい | 1–3 | Mong muốn. |

### V19-IRR-kuru

Đầu vào: `来たい`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-desiderative@1`, gốc **来る** (verb): Dạng mong muốn: muốn thực hiện hành động. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| たい | 1–3 | Mong muốn. |

### V19-N1

Đầu vào: `食べたい。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-desiderative`.

### NEG-RULE-verb-desiderative:plain-past

Đầu vào: `食べたかった。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-desiderative:plain-past`.

### NEG-RULE-verb-desiderative:plain-negative

Đầu vào: `食べたくない。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-desiderative:plain-negative`.

### NEG-RULE-verb-desiderative:plain-negative-past

Đầu vào: `食べたくなかった。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-desiderative:plain-negative-past`.

### NEG-RULE-verb-desiderative:polite-nonpast

Đầu vào: `食べたいです。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-desiderative:polite-nonpast`.

### NEG-RULE-verb-desiderative:polite-past

Đầu vào: `食べたかったです。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-desiderative:polite-past`.

### NEG-RULE-verb-desiderative:polite-negative

Đầu vào: `食べたくないです。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-desiderative:polite-negative`.

### NEG-RULE-verb-desiderative:polite-negative-past

Đầu vào: `食べたくなかったです。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-desiderative:polite-negative-past`.

## V20 — Tiếp diễn/trạng thái ている

### V20-P1

Đầu vào: `食べている`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-te-iru@1`, gốc **食べる** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| る | 4–5 | Đuôi không quá khứ của dạng đang được phân tích. |

### V20-P2

Đầu vào: `書いている`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-te-iru@1`, gốc **書く** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書い | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| る | 4–5 | Đuôi không quá khứ của dạng đang được phân tích. |

### V20-C-plain_past-P1

Đầu vào: `食べていた`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:plain-past@1`, gốc **食べる** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| た | 4–5 | Đánh dấu quá khứ. |

### V20-C-plain_past-P2

Đầu vào: `書いていた`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:plain-past@1`, gốc **書く** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書い | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| た | 4–5 | Đánh dấu quá khứ. |

### V20-C-plain_negative-P1

Đầu vào: `食べていない`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:plain-negative@1`, gốc **食べる** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; phủ định, không quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| ない | 4–6 | Phủ định, không quá khứ. |

### V20-C-plain_negative-P2

Đầu vào: `書いていない`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:plain-negative@1`, gốc **書く** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; phủ định, không quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書い | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| ない | 4–6 | Phủ định, không quá khứ. |

### V20-C-plain_negative_past-P1

Đầu vào: `食べていなかった`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:plain-negative-past@1`, gốc **食べる** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; phủ định, quá khứ, thông thường. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| なかっ | 4–7 | Phần phủ định nối với đuôi quá khứ. |
| た | 7–8 | Đánh dấu quá khứ. |

### V20-C-plain_negative_past-P2

Đầu vào: `書いていなかった`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:plain-negative-past@1`, gốc **書く** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; phủ định, quá khứ, thông thường. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書い | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| なかっ | 4–7 | Phần phủ định nối với đuôi quá khứ. |
| た | 7–8 | Đánh dấu quá khứ. |

### V20-C-polite_nonpast-P1

Đầu vào: `食べています`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:polite-nonpast@1`, gốc **食べる** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| ます | 4–6 | Lịch sự, không quá khứ. |

### V20-C-polite_nonpast-P2

Đầu vào: `書いています`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:polite-nonpast@1`, gốc **書く** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書い | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| ます | 4–6 | Lịch sự, không quá khứ. |

### V20-C-polite_past-P1

Đầu vào: `食べていました`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:polite-past@1`, gốc **食べる** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| まし | 4–6 | Phần lịch sự nối với quá khứ. |
| た | 6–7 | Đánh dấu quá khứ. |

### V20-C-polite_past-P2

Đầu vào: `書いていました`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:polite-past@1`, gốc **書く** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書い | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| まし | 4–6 | Phần lịch sự nối với quá khứ. |
| た | 6–7 | Đánh dấu quá khứ. |

### V20-C-polite_negative-P1

Đầu vào: `食べていません`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:polite-negative@1`, gốc **食べる** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; phủ định, không quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |

### V20-C-polite_negative-P2

Đầu vào: `書いていません`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:polite-negative@1`, gốc **書く** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; phủ định, không quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書い | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |

### V20-C-polite_negative_past-P1

Đầu vào: `食べていませんでした`; fixture: fixture:001; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:polite-negative-past@1`, gốc **食べる** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; phủ định, quá khứ, lịch sự. Dạng gốc: 食べる.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 食べ | 0–2 | Phần động từ của 食べる ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |
| でし | 7–9 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 9–10 | Đánh dấu quá khứ. |

### V20-C-polite_negative_past-P2

Đầu vào: `書いていませんでした`; fixture: fixture:002; kết quả: **supported**.

Phương án `conjugation:verb-te-iru:polite-negative-past@1`, gốc **書く** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh; phủ định, quá khứ, lịch sự. Dạng gốc: 書く.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 書い | 0–2 | Phần động từ của 書く ở dạng quan sát này. |
| て | 2–3 | Thể nối て/で trước いる. |
| い | 3–4 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |
| でし | 7–9 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 9–10 | Đánh dấu quá khứ. |

### V20-IRR-suru

Đầu vào: `している`; fixture: fixture:005; kết quả: **supported**.

Phương án `conjugation:verb-te-iru@1`, gốc **する** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh. Dạng gốc: する.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| し | 0–1 | Phần động từ của する ở dạng quan sát này. |
| て | 1–2 | Thể nối て/で trước いる. |
| い | 2–3 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| る | 3–4 | Đuôi không quá khứ của dạng đang được phân tích. |

### V20-IRR-kuru

Đầu vào: `来ている`; fixture: fixture:006; kết quả: **supported**.

Phương án `conjugation:verb-te-iru@1`, gốc **来る** (verb): Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh. Dạng gốc: 来る.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 来 | 0–1 | Phần động từ của 来る ở dạng quan sát này. |
| て | 1–2 | Thể nối て/で trước いる. |
| い | 2–3 | Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu. |
| る | 3–4 | Đuôi không quá khứ của dạng đang được phân tích. |

### V20-N1

Đầu vào: `食べている。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:verb-te-iru`.

### NEG-RULE-verb-te-iru:plain-past

Đầu vào: `食べていた。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-te-iru:plain-past`.

### NEG-RULE-verb-te-iru:plain-negative

Đầu vào: `食べていない。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-te-iru:plain-negative`.

### NEG-RULE-verb-te-iru:plain-negative-past

Đầu vào: `食べていなかった。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-te-iru:plain-negative-past`.

### NEG-RULE-verb-te-iru:polite-nonpast

Đầu vào: `食べています。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-te-iru:polite-nonpast`.

### NEG-RULE-verb-te-iru:polite-past

Đầu vào: `食べていました。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-te-iru:polite-past`.

### NEG-RULE-verb-te-iru:polite-negative

Đầu vào: `食べていません。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-te-iru:polite-negative`.

### NEG-RULE-verb-te-iru:polite-negative-past

Đầu vào: `食べていませんでした。`; fixture: fixture:001; kết quả: **unavailable**.

Không được gán: `conjugation:verb-te-iru:polite-negative-past`.

## I00 — Nguyên dạng

### I00-P1

Đầu vào: `高い`; fixture: fixture:007; kết quả: **exact_first**.

### I00-P2

Đầu vào: `安い`; fixture: fixture:008; kết quả: **exact_first**.

## I01 — Quá khứ thường

### I01-P1

Đầu vào: `高かった`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-past@2`, gốc **高い** (i_adjective): Tính từ い ở dạng quá khứ thông thường. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高かっ | 0–3 | Phần tính từ い của 高い ở dạng quan sát này. |
| た | 3–4 | Quá khứ. |

### I01-P2

Đầu vào: `安かった`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-past@2`, gốc **安い** (i_adjective): Tính từ い ở dạng quá khứ thông thường. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安かっ | 0–3 | Phần tính từ い của 安い ở dạng quan sát này. |
| た | 3–4 | Quá khứ. |

### I01-IRR-ii

Đầu vào: `よかった`; fixture: fixture:009; kết quả: **supported**.

Phương án `conjugation:adjective-past@2`, gốc **よい** (i_adjective): Tính từ い ở dạng quá khứ thông thường; よい và いい là hai dạng tương ứng của tính từ này. Dạng gốc: よい.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| よかっ | 0–3 | Phần tính từ い của よい ở dạng quan sát này. |
| た | 3–4 | Quá khứ. |

### I01-N1

Đầu vào: `高かった。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:adjective-past`.

## I02 — Phủ định thường

### I02-P1

Đầu vào: `高くない`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-negative@2`, gốc **高い** (i_adjective): Tính từ い ở dạng phủ định, không quá khứ. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高く | 0–2 | Phần tính từ い của 高い ở dạng quan sát này. |
| ない | 2–4 | Phủ định, không quá khứ. |

### I02-P2

Đầu vào: `安くない`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-negative@2`, gốc **安い** (i_adjective): Tính từ い ở dạng phủ định, không quá khứ. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安く | 0–2 | Phần tính từ い của 安い ở dạng quan sát này. |
| ない | 2–4 | Phủ định, không quá khứ. |

### I02-IRR-ii

Đầu vào: `よくない`; fixture: fixture:009; kết quả: **supported**.

Phương án `conjugation:adjective-negative@2`, gốc **よい** (i_adjective): Tính từ い ở dạng phủ định, không quá khứ; よい và いい là hai dạng tương ứng của tính từ này. Dạng gốc: よい.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| よく | 0–2 | Phần tính từ い của よい ở dạng quan sát này. |
| ない | 2–4 | Phủ định, không quá khứ. |

### I02-N1

Đầu vào: `高くない。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:adjective-negative`.

## I03 — Phủ định quá khứ thường

### I03-P1

Đầu vào: `高くなかった`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-negative-past@2`, gốc **高い** (i_adjective): Tính từ い ở dạng phủ định quá khứ. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高く | 0–2 | Phần tính từ い của 高い ở dạng quan sát này. |
| なかっ | 2–5 | Phần phủ định nối với quá khứ. |
| た | 5–6 | Quá khứ. |

### I03-P2

Đầu vào: `安くなかった`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-negative-past@2`, gốc **安い** (i_adjective): Tính từ い ở dạng phủ định quá khứ. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安く | 0–2 | Phần tính từ い của 安い ở dạng quan sát này. |
| なかっ | 2–5 | Phần phủ định nối với quá khứ. |
| た | 5–6 | Quá khứ. |

### I03-IRR-ii

Đầu vào: `よくなかった`; fixture: fixture:009; kết quả: **supported**.

Phương án `conjugation:adjective-negative-past@2`, gốc **よい** (i_adjective): Tính từ い ở dạng phủ định quá khứ; よい và いい là hai dạng tương ứng của tính từ này. Dạng gốc: よい.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| よく | 0–2 | Phần tính từ い của よい ở dạng quan sát này. |
| なかっ | 2–5 | Phần phủ định nối với quá khứ. |
| た | 5–6 | Quá khứ. |

### I03-N1

Đầu vào: `高くなかった。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:adjective-negative-past`.

## I04 — Không quá khứ lịch sự

### I04-P1

Đầu vào: `高いです`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-polite-nonpast@1`, gốc **高い** (i_adjective): Tính từ い ở dạng không quá khứ lịch sự. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高い | 0–2 | Phần tính từ い của 高い ở dạng quan sát này. |
| です | 2–4 | Lịch sự. |

### I04-P2

Đầu vào: `安いです`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-polite-nonpast@1`, gốc **安い** (i_adjective): Tính từ い ở dạng không quá khứ lịch sự. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安い | 0–2 | Phần tính từ い của 安い ở dạng quan sát này. |
| です | 2–4 | Lịch sự. |

### I04-N1

Đầu vào: `高いです。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:adjective-polite-nonpast`.

## I05 — Quá khứ lịch sự

### I05-P1

Đầu vào: `高かったです`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-polite-past@1`, gốc **高い** (i_adjective): Tính từ い ở dạng quá khứ lịch sự. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高かっ | 0–3 | Phần tính từ い của 高い ở dạng quan sát này. |
| た | 3–4 | Quá khứ. |
| です | 4–6 | Lịch sự. |

### I05-P2

Đầu vào: `安かったです`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-polite-past@1`, gốc **安い** (i_adjective): Tính từ い ở dạng quá khứ lịch sự. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安かっ | 0–3 | Phần tính từ い của 安い ở dạng quan sát này. |
| た | 3–4 | Quá khứ. |
| です | 4–6 | Lịch sự. |

### I05-N1

Đầu vào: `高かったです。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:adjective-polite-past`.

## I06 — Phủ định lịch sự

### I06-P1

Đầu vào: `高くないです`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-polite-negative@1`, gốc **高い** (i_adjective): Tính từ い ở dạng phủ định lịch sự. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高く | 0–2 | Phần tính từ い của 高い ở dạng quan sát này. |
| ない | 2–4 | Phủ định, không quá khứ. |
| です | 4–6 | Lịch sự. |

### I06-P2

Đầu vào: `安くないです`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-polite-negative@1`, gốc **安い** (i_adjective): Tính từ い ở dạng phủ định lịch sự. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安く | 0–2 | Phần tính từ い của 安い ở dạng quan sát này. |
| ない | 2–4 | Phủ định, không quá khứ. |
| です | 4–6 | Lịch sự. |

### I06-ALT-高い

Đầu vào: `高くありません`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-polite-negative@1`, gốc **高い** (i_adjective): Tính từ い ở dạng phủ định lịch sự. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高く | 0–2 | Phần tính từ い của 高い ở dạng quan sát này. |
| あり | 2–4 | Phần ある trong cách phủ định lịch sự. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |

### I06-ALT-安い

Đầu vào: `安くありません`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-polite-negative@1`, gốc **安い** (i_adjective): Tính từ い ở dạng phủ định lịch sự. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安く | 0–2 | Phần tính từ い của 安い ở dạng quan sát này. |
| あり | 2–4 | Phần ある trong cách phủ định lịch sự. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |

### I06-N1

Đầu vào: `高くないです。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:adjective-polite-negative`.

## I07 — Phủ định quá khứ lịch sự

### I07-P1

Đầu vào: `高くなかったです`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-polite-negative-past@1`, gốc **高い** (i_adjective): Tính từ い ở dạng phủ định quá khứ lịch sự. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高く | 0–2 | Phần tính từ い của 高い ở dạng quan sát này. |
| なかっ | 2–5 | Phần phủ định nối với quá khứ. |
| た | 5–6 | Quá khứ. |
| です | 6–8 | Lịch sự. |

### I07-P2

Đầu vào: `安くなかったです`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-polite-negative-past@1`, gốc **安い** (i_adjective): Tính từ い ở dạng phủ định quá khứ lịch sự. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安く | 0–2 | Phần tính từ い của 安い ở dạng quan sát này. |
| なかっ | 2–5 | Phần phủ định nối với quá khứ. |
| た | 5–6 | Quá khứ. |
| です | 6–8 | Lịch sự. |

### I07-ALT-高い

Đầu vào: `高くありませんでした`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-polite-negative-past@1`, gốc **高い** (i_adjective): Tính từ い ở dạng phủ định quá khứ lịch sự. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高く | 0–2 | Phần tính từ い của 高い ở dạng quan sát này. |
| あり | 2–4 | Phần ある trong cách phủ định lịch sự. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |
| でし | 7–9 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 9–10 | Đánh dấu quá khứ. |

### I07-ALT-安い

Đầu vào: `安くありませんでした`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-polite-negative-past@1`, gốc **安い** (i_adjective): Tính từ い ở dạng phủ định quá khứ lịch sự. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安く | 0–2 | Phần tính từ い của 安い ở dạng quan sát này. |
| あり | 2–4 | Phần ある trong cách phủ định lịch sự. |
| ませ | 4–6 | Phần lịch sự nối với phủ định. |
| ん | 6–7 | Phủ định trong chuỗi lịch sự. |
| でし | 7–9 | Phần lịch sự nối với quá khứ sau phủ định. |
| た | 9–10 | Đánh dấu quá khứ. |

### I07-N1

Đầu vào: `高くなかったです。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:adjective-polite-negative-past`.

## I08 — Thể nối くて

### I08-P1

Đầu vào: `高くて`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-connective@1`, gốc **高い** (i_adjective): Thể nối くて của tính từ い. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高く | 0–2 | Phần tính từ い của 高い ở dạng quan sát này. |
| て | 2–3 | Nối với phần tiếp theo. |

### I08-P2

Đầu vào: `安くて`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-connective@1`, gốc **安い** (i_adjective): Thể nối くて của tính từ い. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安く | 0–2 | Phần tính từ い của 安い ở dạng quan sát này. |
| て | 2–3 | Nối với phần tiếp theo. |

### I08-IRR-ii

Đầu vào: `よくて`; fixture: fixture:009; kết quả: **supported**.

Phương án `conjugation:adjective-connective@1`, gốc **よい** (i_adjective): Thể nối くて của tính từ い; よい và いい là hai dạng tương ứng của tính từ này. Dạng gốc: よい.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| よく | 0–2 | Phần tính từ い của よい ở dạng quan sát này. |
| て | 2–3 | Nối với phần tiếp theo. |

### I08-N1

Đầu vào: `高くて。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:adjective-connective`.

## I09 — Trạng từ く

### I09-P1

Đầu vào: `高く`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-adverbial@1`, gốc **高い** (i_adjective): Dạng trạng từ く của tính từ い. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高く | 0–2 | Phần tính từ い của 高い ở dạng quan sát này. |

### I09-P2

Đầu vào: `安く`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-adverbial@1`, gốc **安い** (i_adjective): Dạng trạng từ く của tính từ い. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安く | 0–2 | Phần tính từ い của 安い ở dạng quan sát này. |

### I09-IRR-ii

Đầu vào: `よく`; fixture: fixture:009; kết quả: **supported**.

Phương án `conjugation:adjective-adverbial@1`, gốc **よい** (i_adjective): Dạng trạng từ く của tính từ い; よい và いい là hai dạng tương ứng của tính từ này. Dạng gốc: よい.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| よく | 0–2 | Phần tính từ い của よい ở dạng quan sát này. |

### I09-N1

Đầu vào: `きれいに`; fixture: fixture:012; kết quả: **supported**.

Không được gán: `conjugation:adjective-adverbial`.

Phương án `conjugation:na-adverbial@1`, gốc **きれい** (na_adjective): Dạng に dùng làm trạng từ của tính từ な. Dạng gốc: きれい.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| きれい | 0–3 | Phần tính từ な của きれい ở dạng quan sát này. |
| に | 3–4 | Chuyển tính từ な sang cách dùng trạng từ. |

## I10 — Điều kiện ければ

### I10-P1

Đầu vào: `高ければ`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-conditional-ba@1`, gốc **高い** (i_adjective): Thể điều kiện ければ của tính từ い. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高けれ | 0–3 | Phần tính từ い của 高い ở dạng quan sát này. |
| ば | 3–4 | Điều kiện ば. |

### I10-P2

Đầu vào: `安ければ`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-conditional-ba@1`, gốc **安い** (i_adjective): Thể điều kiện ければ của tính từ い. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安けれ | 0–3 | Phần tính từ い của 安い ở dạng quan sát này. |
| ば | 3–4 | Điều kiện ば. |

### I10-IRR-ii

Đầu vào: `よければ`; fixture: fixture:009; kết quả: **supported**.

Phương án `conjugation:adjective-conditional-ba@1`, gốc **よい** (i_adjective): Thể điều kiện ければ của tính từ い; よい và いい là hai dạng tương ứng của tính từ này. Dạng gốc: よい.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| よけれ | 0–3 | Phần tính từ い của よい ở dạng quan sát này. |
| ば | 3–4 | Điều kiện ば. |

### I10-N1

Đầu vào: `高ければ。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:adjective-conditional-ba`.

## I11 — Điều kiện かったら

### I11-P1

Đầu vào: `高かったら`; fixture: fixture:007; kết quả: **supported**.

Phương án `conjugation:adjective-conditional-tara@1`, gốc **高い** (i_adjective): Thể điều kiện かったら của tính từ い. Dạng gốc: 高い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 高かっ | 0–3 | Phần tính từ い của 高い ở dạng quan sát này. |
| たら | 3–5 | Điều kiện たら. |

### I11-P2

Đầu vào: `安かったら`; fixture: fixture:008; kết quả: **supported**.

Phương án `conjugation:adjective-conditional-tara@1`, gốc **安い** (i_adjective): Thể điều kiện かったら của tính từ い. Dạng gốc: 安い.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 安かっ | 0–3 | Phần tính từ い của 安い ở dạng quan sát này. |
| たら | 3–5 | Điều kiện たら. |

### I11-IRR-ii

Đầu vào: `よかったら`; fixture: fixture:009; kết quả: **supported**.

Phương án `conjugation:adjective-conditional-tara@1`, gốc **よい** (i_adjective): Thể điều kiện かったら của tính từ い; よい và いい là hai dạng tương ứng của tính từ này. Dạng gốc: よい.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| よかっ | 0–3 | Phần tính từ い của よい ở dạng quan sát này. |
| たら | 3–5 | Điều kiện たら. |

### I11-N1

Đầu vào: `高かったら。`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:adjective-conditional-tara`.

## A00 — Thân từ điển

### A00-P1

Đầu vào: `静か`; fixture: fixture:010; kết quả: **exact_first**.

### A00-P2

Đầu vào: `便利`; fixture: fixture:011; kết quả: **exact_first**.

## A01 — Khẳng định thường không quá khứ

### A01-P1

Đầu vào: `静かだ`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-nonpast@1`, gốc **静か** (na_adjective): Tính từ な: khẳng định thông thường, không quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| だ | 2–3 | Copula khẳng định thông thường, không quá khứ. |

### A01-P2

Đầu vào: `便利だ`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-nonpast@1`, gốc **便利** (na_adjective): Tính từ な: khẳng định thông thường, không quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| だ | 2–3 | Copula khẳng định thông thường, không quá khứ. |

### POS-01

Đầu vào: `きれいだ`; fixture: fixture:012; kết quả: **supported**.

Phương án `conjugation:na-copula-nonpast@1`, gốc **きれい** (na_adjective): Tính từ な: khẳng định thông thường, không quá khứ. Dạng gốc: きれい.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| きれい | 0–3 | Phần tính từ な của きれい ở dạng quan sát này. |
| だ | 3–4 | Copula khẳng định thông thường, không quá khứ. |

### A01-N1

Đầu vào: `学生だ`; fixture: fixture:013; kết quả: **supported**.

Không được gán: `conjugation:na-copula-nonpast`.

Phương án `conjugation:noun-copula-nonpast@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định thông thường, không quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| だ | 2–3 | Copula khẳng định thông thường, không quá khứ. |

## A02 — Khẳng định thường quá khứ

### A02-P1

Đầu vào: `静かだった`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-past@1`, gốc **静か** (na_adjective): Tính từ な: khẳng định thông thường, quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với quá khứ. |
| た | 4–5 | Quá khứ. |

### A02-P2

Đầu vào: `便利だった`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-past@1`, gốc **便利** (na_adjective): Tính từ な: khẳng định thông thường, quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với quá khứ. |
| た | 4–5 | Quá khứ. |

### A02-N1

Đầu vào: `学生だった`; fixture: fixture:013; kết quả: **supported**.

Không được gán: `conjugation:na-copula-past`.

Phương án `conjugation:noun-copula-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định thông thường, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với quá khứ. |
| た | 4–5 | Quá khứ. |

## A03 — Phủ định thường không quá khứ

### A03-P1

Đầu vào: `静かじゃない`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-negative@1`, gốc **静か** (na_adjective): Tính từ な: phủ định thông thường, không quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では trước phủ định. |
| ない | 4–6 | Phủ định. |

### A03-P2

Đầu vào: `便利じゃない`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-negative@1`, gốc **便利** (na_adjective): Tính từ な: phủ định thông thường, không quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では trước phủ định. |
| ない | 4–6 | Phủ định. |

### A03-ALT-dewa-静か

Đầu vào: `静かではない`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-negative@1`, gốc **静か** (na_adjective): Tính từ な: phủ định thông thường, không quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| では | 2–4 | Phần では trong phủ định; じゃ là dạng nói tương ứng. |
| ない | 4–6 | Phủ định. |

### A03-ALT-dewa-便利

Đầu vào: `便利ではない`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-negative@1`, gốc **便利** (na_adjective): Tính từ な: phủ định thông thường, không quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| では | 2–4 | Phần では trong phủ định; じゃ là dạng nói tương ứng. |
| ない | 4–6 | Phủ định. |

### A03-N1

Đầu vào: `学生じゃない`; fixture: fixture:013; kết quả: **supported**.

Không được gán: `conjugation:na-copula-negative`.

Phương án `conjugation:noun-copula-negative@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định thông thường, không quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では trước phủ định. |
| ない | 4–6 | Phủ định. |

## A04 — Phủ định thường quá khứ

### A04-P1

Đầu vào: `静かじゃなかった`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-negative-past@1`, gốc **静か** (na_adjective): Tính từ な: phủ định thông thường, quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |

### A04-P2

Đầu vào: `便利じゃなかった`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-negative-past@1`, gốc **便利** (na_adjective): Tính từ な: phủ định thông thường, quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |

### A04-ALT-dewa-静か

Đầu vào: `静かではなかった`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-negative-past@1`, gốc **静か** (na_adjective): Tính từ な: phủ định thông thường, quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| では | 2–4 | Phần では trong phủ định; じゃ là dạng nói tương ứng. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |

### A04-ALT-dewa-便利

Đầu vào: `便利ではなかった`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-negative-past@1`, gốc **便利** (na_adjective): Tính từ な: phủ định thông thường, quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| では | 2–4 | Phần では trong phủ định; じゃ là dạng nói tương ứng. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |

### A04-N1

Đầu vào: `学生じゃなかった`; fixture: fixture:013; kết quả: **supported**.

Không được gán: `conjugation:na-copula-negative-past`.

Phương án `conjugation:noun-copula-negative-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định thông thường, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |

## A05 — Khẳng định lịch sự không quá khứ

### A05-P1

Đầu vào: `静かです`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-nonpast@1`, gốc **静か** (na_adjective): Tính từ な: khẳng định lịch sự, không quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| です | 2–4 | Copula khẳng định lịch sự, không quá khứ. |

### A05-P2

Đầu vào: `便利です`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-nonpast@1`, gốc **便利** (na_adjective): Tính từ な: khẳng định lịch sự, không quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| です | 2–4 | Copula khẳng định lịch sự, không quá khứ. |

### A05-N1

Đầu vào: `学生です`; fixture: fixture:013; kết quả: **supported**.

Không được gán: `conjugation:na-copula-polite-nonpast`.

Phương án `conjugation:noun-copula-polite-nonpast@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định lịch sự, không quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| です | 2–4 | Copula khẳng định lịch sự, không quá khứ. |

## A06 — Khẳng định lịch sự quá khứ

### A06-P1

Đầu vào: `静かでした`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-past@1`, gốc **静か** (na_adjective): Tính từ な: khẳng định lịch sự, quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| でし | 2–4 | Copula lịch sự nối quá khứ. |
| た | 4–5 | Quá khứ. |

### A06-P2

Đầu vào: `便利でした`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-past@1`, gốc **便利** (na_adjective): Tính từ な: khẳng định lịch sự, quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| でし | 2–4 | Copula lịch sự nối quá khứ. |
| た | 4–5 | Quá khứ. |

### A06-N1

Đầu vào: `学生でした`; fixture: fixture:013; kết quả: **supported**.

Không được gán: `conjugation:na-copula-polite-past`.

Phương án `conjugation:noun-copula-polite-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định lịch sự, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| でし | 2–4 | Copula lịch sự nối quá khứ. |
| た | 4–5 | Quá khứ. |

## A07 — Phủ định lịch sự không quá khứ

### A07-P1

Đầu vào: `静かじゃないです`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative@1`, gốc **静か** (na_adjective): Tính từ な: phủ định lịch sự, không quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では. |
| ない | 4–6 | Phủ định. |
| です | 6–8 | Lịch sự. |

### A07-P2

Đầu vào: `便利じゃないです`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative@1`, gốc **便利** (na_adjective): Tính từ な: phủ định lịch sự, không quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では. |
| ない | 4–6 | Phủ định. |
| です | 6–8 | Lịch sự. |

### A07-ALT-ja_arimasen-静か

Đầu vào: `静かじゃありません`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative@1`, gốc **静か** (na_adjective): Tính từ な: phủ định lịch sự, không quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |

### A07-ALT-ja_arimasen-便利

Đầu vào: `便利じゃありません`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative@1`, gốc **便利** (na_adjective): Tính từ な: phủ định lịch sự, không quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |

### A07-ALT-dewa_arimasen-静か

Đầu vào: `静かではありません`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative@1`, gốc **静か** (na_adjective): Tính từ な: phủ định lịch sự, không quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |

### A07-ALT-dewa_arimasen-便利

Đầu vào: `便利ではありません`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative@1`, gốc **便利** (na_adjective): Tính từ な: phủ định lịch sự, không quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |

### A07-N1

Đầu vào: `学生ではありません`; fixture: fixture:013; kết quả: **supported**.

Không được gán: `conjugation:na-copula-polite-negative`.

Phương án `conjugation:noun-copula-polite-negative@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, không quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |

## A08 — Phủ định lịch sự quá khứ

### A08-P1

Đầu vào: `静かじゃなかったです`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative-past@1`, gốc **静か** (na_adjective): Tính từ な: phủ định lịch sự, quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |
| です | 8–10 | Lịch sự. |

### A08-P2

Đầu vào: `便利じゃなかったです`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative-past@1`, gốc **便利** (na_adjective): Tính từ な: phủ định lịch sự, quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |
| です | 8–10 | Lịch sự. |

### A08-ALT-ja_arimasen-静か

Đầu vào: `静かじゃありませんでした`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative-past@1`, gốc **静か** (na_adjective): Tính từ な: phủ định lịch sự, quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |
| でし | 9–11 | Phần lịch sự nối với quá khứ. |
| た | 11–12 | Quá khứ. |

### A08-ALT-ja_arimasen-便利

Đầu vào: `便利じゃありませんでした`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative-past@1`, gốc **便利** (na_adjective): Tính từ な: phủ định lịch sự, quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |
| でし | 9–11 | Phần lịch sự nối với quá khứ. |
| た | 11–12 | Quá khứ. |

### A08-ALT-dewa_arimasen-静か

Đầu vào: `静かではありませんでした`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative-past@1`, gốc **静か** (na_adjective): Tính từ な: phủ định lịch sự, quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |
| でし | 9–11 | Phần lịch sự nối với quá khứ. |
| た | 11–12 | Quá khứ. |

### A08-ALT-dewa_arimasen-便利

Đầu vào: `便利ではありませんでした`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-copula-polite-negative-past@1`, gốc **便利** (na_adjective): Tính từ な: phủ định lịch sự, quá khứ. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |
| でし | 9–11 | Phần lịch sự nối với quá khứ. |
| た | 11–12 | Quá khứ. |

### A08-N1

Đầu vào: `学生ではありませんでした`; fixture: fixture:013; kết quả: **supported**.

Không được gán: `conjugation:na-copula-polite-negative-past`.

Phương án `conjugation:noun-copula-polite-negative-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |
| でし | 9–11 | Phần lịch sự nối với quá khứ. |
| た | 11–12 | Quá khứ. |

## A09 — Bổ nghĩa 連体形 `な`

### A09-P1

Đầu vào: `静かな`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-attributive@1`, gốc **静か** (na_adjective): Dạng な để bổ nghĩa cho danh từ theo sau. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| な | 2–3 | Nối tính từ な với danh từ theo sau. |

### A09-P2

Đầu vào: `便利な`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-attributive@1`, gốc **便利** (na_adjective): Dạng な để bổ nghĩa cho danh từ theo sau. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| な | 2–3 | Nối tính từ な với danh từ theo sau. |

### A09-N1

Đầu vào: `学生な`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:na-attributive`.

## A10 — Trạng từ `に`

### A10-P1

Đầu vào: `静かに`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-adverbial@1`, gốc **静か** (na_adjective): Dạng に dùng làm trạng từ của tính từ な. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| に | 2–3 | Chuyển tính từ な sang cách dùng trạng từ. |

### A10-P2

Đầu vào: `便利に`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-adverbial@1`, gốc **便利** (na_adjective): Dạng に dùng làm trạng từ của tính từ な. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| に | 2–3 | Chuyển tính từ な sang cách dùng trạng từ. |

### A10-N1

Đầu vào: `学生に`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:na-adverbial`.

## A11 — Thể nối `で`

### A11-P1

Đầu vào: `静かで`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-connective@1`, gốc **静か** (na_adjective): Dạng nối bằng copula で của tính từ な. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| で | 2–3 | Ở cách hiểu copula, nối với phần câu theo sau; cần ngữ cảnh để phân biệt trợ từ で. |

### A11-P2

Đầu vào: `便利で`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-connective@1`, gốc **便利** (na_adjective): Dạng nối bằng copula で của tính từ な. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| で | 2–3 | Ở cách hiểu copula, nối với phần câu theo sau; cần ngữ cảnh để phân biệt trợ từ で. |

### A11-N1

Đầu vào: `学生で`; fixture: fixture:013; kết quả: **supported**.

Không được gán: `conjugation:na-connective`.

Phương án `conjugation:noun-copula-connective@1`, gốc **学生** (noun_copula): Cách hiểu có thể: danh từ nối bằng copula で; cùng bề mặt cũng có thể chứa trợ từ で, cần câu để xác định. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| で | 2–3 | Ở cách hiểu copula, nối với phần câu theo sau; cần ngữ cảnh để phân biệt trợ từ で. |

## A12 — Điều kiện `なら`

### A12-P1

Đầu vào: `静かなら`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-conditional-nara@1`, gốc **静か** (na_adjective): Điều kiện なら của tính từ な. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| なら | 2–4 | Điều kiện なら. |

### A12-P2

Đầu vào: `便利なら`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-conditional-nara@1`, gốc **便利** (na_adjective): Điều kiện なら của tính từ な. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| なら | 2–4 | Điều kiện なら. |

### A12-N1

Đầu vào: `学生なら`; fixture: fixture:013; kết quả: **supported**.

Không được gán: `conjugation:na-conditional-nara`.

Phương án `conjugation:noun-conditional-nara@1`, gốc **学生** (noun_copula): Danh từ với điều kiện なら. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| なら | 2–4 | Điều kiện なら. |

## A13 — Điều kiện `だったら`

### A13-P1

Đầu vào: `静かだったら`; fixture: fixture:010; kết quả: **supported**.

Phương án `conjugation:na-conditional-tara@1`, gốc **静か** (na_adjective): Điều kiện だったら của tính từ な. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với điều kiện. |
| たら | 4–6 | Điều kiện たら. |

### A13-P2

Đầu vào: `便利だったら`; fixture: fixture:011; kết quả: **supported**.

Phương án `conjugation:na-conditional-tara@1`, gốc **便利** (na_adjective): Điều kiện だったら của tính từ な. Dạng gốc: 便利.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 便利 | 0–2 | Phần tính từ な của 便利 ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với điều kiện. |
| たら | 4–6 | Điều kiện たら. |

### A13-N1

Đầu vào: `学生だったら`; fixture: fixture:013; kết quả: **supported**.

Không được gán: `conjugation:na-conditional-tara`.

Phương án `conjugation:noun-conditional-tara@1`, gốc **学生** (noun_copula): Danh từ với điều kiện だったら. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với điều kiện. |
| たら | 4–6 | Điều kiện たら. |

## N00 — Danh từ nguyên dạng

### N00-P1

Đầu vào: `学生`; fixture: fixture:013; kết quả: **exact_first**.

### N00-P2

Đầu vào: `先生`; fixture: fixture:014; kết quả: **exact_first**.

## N01 — `だ` thường không quá khứ

### N01-P1

Đầu vào: `学生だ`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-nonpast@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định thông thường, không quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| だ | 2–3 | Copula khẳng định thông thường, không quá khứ. |

### N01-P2

Đầu vào: `先生だ`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-nonpast@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định thông thường, không quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| だ | 2–3 | Copula khẳng định thông thường, không quá khứ. |

### N01-N1

Đầu vào: `静かだ`; fixture: fixture:010; kết quả: **supported**.

Không được gán: `conjugation:noun-copula-nonpast`.

Phương án `conjugation:na-copula-nonpast@1`, gốc **静か** (na_adjective): Tính từ な: khẳng định thông thường, không quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| だ | 2–3 | Copula khẳng định thông thường, không quá khứ. |

## N02 — `だった` thường quá khứ

### N02-P1

Đầu vào: `学生だった`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định thông thường, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với quá khứ. |
| た | 4–5 | Quá khứ. |

### N02-P2

Đầu vào: `先生だった`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-past@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định thông thường, quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với quá khứ. |
| た | 4–5 | Quá khứ. |

### N02-N1

Đầu vào: `静かだった`; fixture: fixture:010; kết quả: **supported**.

Không được gán: `conjugation:noun-copula-past`.

Phương án `conjugation:na-copula-past@1`, gốc **静か** (na_adjective): Tính từ な: khẳng định thông thường, quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với quá khứ. |
| た | 4–5 | Quá khứ. |

## N03 — Phủ định thường không quá khứ

### N03-P1

Đầu vào: `学生じゃない`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-negative@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định thông thường, không quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では trước phủ định. |
| ない | 4–6 | Phủ định. |

### N03-P2

Đầu vào: `先生じゃない`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-negative@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định thông thường, không quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では trước phủ định. |
| ない | 4–6 | Phủ định. |

### N03-ALT-dewa-学生

Đầu vào: `学生ではない`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-negative@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định thông thường, không quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| では | 2–4 | Phần では trong phủ định; じゃ là dạng nói tương ứng. |
| ない | 4–6 | Phủ định. |

### N03-ALT-dewa-先生

Đầu vào: `先生ではない`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-negative@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định thông thường, không quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| では | 2–4 | Phần では trong phủ định; じゃ là dạng nói tương ứng. |
| ない | 4–6 | Phủ định. |

### N03-N1

Đầu vào: `静かじゃない`; fixture: fixture:010; kết quả: **supported**.

Không được gán: `conjugation:noun-copula-negative`.

Phương án `conjugation:na-copula-negative@1`, gốc **静か** (na_adjective): Tính từ な: phủ định thông thường, không quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では trước phủ định. |
| ない | 4–6 | Phủ định. |

## N04 — Phủ định thường quá khứ

### N04-P1

Đầu vào: `学生じゃなかった`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-negative-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định thông thường, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |

### N04-P2

Đầu vào: `先生じゃなかった`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-negative-past@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định thông thường, quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |

### N04-ALT-dewa-学生

Đầu vào: `学生ではなかった`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-negative-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định thông thường, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| では | 2–4 | Phần では trong phủ định; じゃ là dạng nói tương ứng. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |

### N04-ALT-dewa-先生

Đầu vào: `先生ではなかった`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-negative-past@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định thông thường, quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| では | 2–4 | Phần では trong phủ định; じゃ là dạng nói tương ứng. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |

### N04-N1

Đầu vào: `静かじゃなかった`; fixture: fixture:010; kết quả: **supported**.

Không được gán: `conjugation:noun-copula-negative-past`.

Phương án `conjugation:na-copula-negative-past@1`, gốc **静か** (na_adjective): Tính từ な: phủ định thông thường, quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói rút gọn của では. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |

## N05 — `です` lịch sự không quá khứ

### N05-P1

Đầu vào: `学生です`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-nonpast@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định lịch sự, không quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| です | 2–4 | Copula khẳng định lịch sự, không quá khứ. |

### N05-P2

Đầu vào: `先生です`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-nonpast@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định lịch sự, không quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| です | 2–4 | Copula khẳng định lịch sự, không quá khứ. |

### UNI-02

Đầu vào: `𠮷です`; fixture: fixture:015; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-nonpast@1`, gốc **𠮷** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định lịch sự, không quá khứ. Dạng gốc: 𠮷.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 𠮷 | 0–2 | Phần danh từ của 𠮷 ở dạng quan sát này. |
| です | 2–4 | Copula khẳng định lịch sự, không quá khứ. |

### N05-N1

Đầu vào: `静かです`; fixture: fixture:010; kết quả: **supported**.

Không được gán: `conjugation:noun-copula-polite-nonpast`.

Phương án `conjugation:na-copula-polite-nonpast@1`, gốc **静か** (na_adjective): Tính từ な: khẳng định lịch sự, không quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| です | 2–4 | Copula khẳng định lịch sự, không quá khứ. |

### UNI-01

Đầu vào: `𠮷さんです`; fixture: không có; kết quả: **unavailable**.

## N06 — `でした` lịch sự quá khứ

### N06-P1

Đầu vào: `学生でした`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định lịch sự, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| でし | 2–4 | Copula lịch sự nối quá khứ. |
| た | 4–5 | Quá khứ. |

### N06-P2

Đầu vào: `先生でした`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-past@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định lịch sự, quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| でし | 2–4 | Copula lịch sự nối quá khứ. |
| た | 4–5 | Quá khứ. |

### N06-N1

Đầu vào: `静かでした`; fixture: fixture:010; kết quả: **supported**.

Không được gán: `conjugation:noun-copula-polite-past`.

Phương án `conjugation:na-copula-polite-past@1`, gốc **静か** (na_adjective): Tính từ な: khẳng định lịch sự, quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| でし | 2–4 | Copula lịch sự nối quá khứ. |
| た | 4–5 | Quá khứ. |

## N07 — Phủ định lịch sự không quá khứ

### N07-P1

Đầu vào: `学生じゃないです`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, không quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では. |
| ない | 4–6 | Phủ định. |
| です | 6–8 | Lịch sự. |

### N07-P2

Đầu vào: `先生じゃないです`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, không quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では. |
| ない | 4–6 | Phủ định. |
| です | 6–8 | Lịch sự. |

### N07-ALT-ja_arimasen-学生

Đầu vào: `学生じゃありません`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, không quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |

### N07-ALT-ja_arimasen-先生

Đầu vào: `先生じゃありません`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, không quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |

### N07-ALT-dewa_arimasen-学生

Đầu vào: `学生ではありません`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, không quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |

### N07-ALT-dewa_arimasen-先生

Đầu vào: `先生ではありません`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, không quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |

### N07-N1

Đầu vào: `静かではありません`; fixture: fixture:010; kết quả: **supported**.

Không được gán: `conjugation:noun-copula-polite-negative`.

Phương án `conjugation:na-copula-polite-negative@1`, gốc **静か** (na_adjective): Tính từ な: phủ định lịch sự, không quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |

## N08 — Phủ định lịch sự quá khứ

### N08-P1

Đầu vào: `学生じゃなかったです`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |
| です | 8–10 | Lịch sự. |

### N08-P2

Đầu vào: `先生じゃなかったです`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative-past@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では. |
| なかっ | 4–7 | Phủ định nối quá khứ. |
| た | 7–8 | Quá khứ. |
| です | 8–10 | Lịch sự. |

### N08-ALT-ja_arimasen-学生

Đầu vào: `学生じゃありませんでした`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |
| でし | 9–11 | Phần lịch sự nối với quá khứ. |
| た | 11–12 | Quá khứ. |

### N08-ALT-ja_arimasen-先生

Đầu vào: `先生じゃありませんでした`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative-past@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| じゃ | 2–4 | Dạng nói của では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |
| でし | 9–11 | Phần lịch sự nối với quá khứ. |
| た | 11–12 | Quá khứ. |

### N08-ALT-dewa_arimasen-学生

Đầu vào: `学生ではありませんでした`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |
| でし | 9–11 | Phần lịch sự nối với quá khứ. |
| た | 11–12 | Quá khứ. |

### N08-ALT-dewa_arimasen-先生

Đầu vào: `先生ではありませんでした`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative-past@1`, gốc **先生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, quá khứ. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |
| でし | 9–11 | Phần lịch sự nối với quá khứ. |
| た | 11–12 | Quá khứ. |

### BND-01

Đầu vào: `学生ではありませんでした`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-polite-negative-past@1`, gốc **学生** (noun_copula): Danh từ đi với copula; bản thân danh từ không tự chia: phủ định lịch sự, quá khứ. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |
| でし | 9–11 | Phần lịch sự nối với quá khứ. |
| た | 11–12 | Quá khứ. |

### N08-N1

Đầu vào: `静かではありませんでした`; fixture: fixture:010; kết quả: **supported**.

Không được gán: `conjugation:noun-copula-polite-negative-past`.

Phương án `conjugation:na-copula-polite-negative-past@1`, gốc **静か** (na_adjective): Tính từ な: phủ định lịch sự, quá khứ. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| では | 2–4 | Phần では trước phủ định. |
| あり | 4–6 | Phần ある dùng trong cách nói phủ định lịch sự. |
| ませ | 6–8 | Lịch sự trước phủ định. |
| ん | 8–9 | Phủ định. |
| でし | 9–11 | Phần lịch sự nối với quá khứ. |
| た | 11–12 | Quá khứ. |

## N09 — Điều kiện `なら`

### N09-P1

Đầu vào: `学生なら`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-conditional-nara@1`, gốc **学生** (noun_copula): Danh từ với điều kiện なら. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| なら | 2–4 | Điều kiện なら. |

### N09-P2

Đầu vào: `先生なら`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-conditional-nara@1`, gốc **先生** (noun_copula): Danh từ với điều kiện なら. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| なら | 2–4 | Điều kiện なら. |

### N09-N1

Đầu vào: `静かなら`; fixture: fixture:010; kết quả: **supported**.

Không được gán: `conjugation:noun-conditional-nara`.

Phương án `conjugation:na-conditional-nara@1`, gốc **静か** (na_adjective): Điều kiện なら của tính từ な. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| なら | 2–4 | Điều kiện なら. |

## N10 — Điều kiện `だったら`

### N10-P1

Đầu vào: `学生だったら`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-conditional-tara@1`, gốc **学生** (noun_copula): Danh từ với điều kiện だったら. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với điều kiện. |
| たら | 4–6 | Điều kiện たら. |

### N10-P2

Đầu vào: `先生だったら`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-conditional-tara@1`, gốc **先生** (noun_copula): Danh từ với điều kiện だったら. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với điều kiện. |
| たら | 4–6 | Điều kiện たら. |

### N10-N1

Đầu vào: `静かだったら`; fixture: fixture:010; kết quả: **supported**.

Không được gán: `conjugation:noun-conditional-tara`.

Phương án `conjugation:na-conditional-tara@1`, gốc **静か** (na_adjective): Điều kiện だったら của tính từ な. Dạng gốc: 静か.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 静か | 0–2 | Phần tính từ な của 静か ở dạng quan sát này. |
| だっ | 2–4 | Copula nối với điều kiện. |
| たら | 4–6 | Điều kiện たら. |

## N11 — `で` nối mệnh đề

### N11-P1

Đầu vào: `学生で`; fixture: fixture:013; kết quả: **supported**.

Phương án `conjugation:noun-copula-connective@1`, gốc **学生** (noun_copula): Cách hiểu có thể: danh từ nối bằng copula で; cùng bề mặt cũng có thể chứa trợ từ で, cần câu để xác định. Dạng gốc: 学生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 学生 | 0–2 | Phần danh từ của 学生 ở dạng quan sát này. |
| で | 2–3 | Ở cách hiểu copula, nối với phần câu theo sau; cần ngữ cảnh để phân biệt trợ từ で. |

### N11-P2

Đầu vào: `先生で`; fixture: fixture:014; kết quả: **supported**.

Phương án `conjugation:noun-copula-connective@1`, gốc **先生** (noun_copula): Cách hiểu có thể: danh từ nối bằng copula で; cùng bề mặt cũng có thể chứa trợ từ で, cần câu để xác định. Dạng gốc: 先生.

| Thành phần | Span UTF-16 | Giải thích Việt |
|---|---|---|
| 先生 | 0–2 | Phần danh từ của 先生 ở dạng quan sát này. |
| で | 2–3 | Ở cách hiểu copula, nối với phần câu theo sau; cần ngữ cảnh để phân biệt trợ từ で. |

### N11-N1

Đầu vào: `学生に`; fixture: không có; kết quả: **unavailable**.

Không được gán: `conjugation:noun-copula-connective`.
