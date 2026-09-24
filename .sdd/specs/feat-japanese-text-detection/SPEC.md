# Feature Specification: Japanese Text Detection (F-01)

**Version:** 0.2.0
**Updated:** 2026-09-19
**Status:** Implemented — core evidence complete; Brave, Firefox, and OD-005 release evidence pending
**Feature directory:** .sdd/specs/feat-japanese-text-detection
**Product authority:** [REQUIREMENT.md](../../../REQUIREMENT.md) — glossary, JPN-001
**Other governing sources:** [MIGRATION_DECISION.md](../../../MIGRATION_DECISION.md) §6.4, §9.1; [Constitution](../../constitution.md); [Global constraints](../../constraints/global.md)

Đặc tả dùng cấu trúc câu hỏi do Product Owner yêu cầu. SHALL là bắt buộc; SHOULD là mục tiêu cần đo/giải trình. Các quyết định cụ thể hóa nằm ở §3.2, §6 và §9; không đồng nghĩa Product Owner đã phê duyệt implementation.

## 1. Context & Goal — Tại sao feature tồn tại?

### 1.1 Vấn đề và giá trị

Reader bôi chọn text khi đọc trang web và muốn nhận hỗ trợ với tiếng Nhật. F-01 cung cấp điều kiện nhận diện ban đầu để luồng đọc tiếp tục; nhận sai làm phát sinh kích hoạt không cần thiết, bỏ sót khiến người đọc không nhận được hỗ trợ.

Mục tiêu: với một chuỗi text được cung cấp, xác định có ít nhất một ký tự thuộc tập kana/Han được hỗ trợ hay không, bao gồm supplementary ideograph như 𠮟 (U+20B9F).

### 1.2 Ý nghĩa của kết quả

- **true:** chuỗi chứa ít nhất một ký tự kích hoạt theo §3.2.
- **false:** chuỗi hợp lệ nhưng không chứa ký tự kích hoạt.
- **Lỗi đầu vào/thực thi:** không phải một kết quả phân loại; quy định tại §6.

F-01 nhận diện sự hiện diện của hệ chữ viết, không xác nhận ngôn ngữ hoặc tính đúng ngữ pháp của cả câu. 中文 được chấp nhận vì chứa Han ideographs. Text dùng Han trong tiếng Trung hoặc Hanja trong tiếng Hàn có thể được chấp nhận theo cùng quy tắc; Hangul đứng riêng không được chấp nhận. Kết quả true không cam kết có mục từ hoặc kết quả phân tích ở Backend.

### 1.3 Phạm vi hoàn thành

Bàn giao F-01 gồm quy tắc phân loại có thể kiểm thử độc lập, regression corpus và bằng chứng tương đương trên browser mục tiêu. Trải nghiệm “bôi chọn rồi hiện popup” cần tích hợp thêm F-02/F-03; F-01 hoàn thành không có nghĩa toàn bộ trải nghiệm đó đã hoàn thành.

## 2. Actors & Roles — Ai tương tác, với quyền gì?

| Actor | Vai trò | Quyền và giới hạn |
|---|---|---|
| Anonymous Reader | Cung cấp selection qua thao tác đọc | Không cần tài khoản. Selected text có thể nhạy cảm. |
| Authenticated Learner | Cùng thao tác đọc | Kết quả và quyền dùng detector giống Anonymous Reader. |
| Extension selection boundary | Cung cấp chuỗi text, nhận kết quả hoặc lỗi | Chịu trách nhiệm lấy selection; không buộc detector tự đọc trang. |
| Browser Host Page | Nguồn text được chọn | F-01 không đọc thêm nội dung, sửa DOM/style hoặc thực thi text từ trang. |

Không cần role admin hoặc authorization riêng: feature chỉ xử lý text tức thời, không truy cập kho dữ liệu cá nhân.

## 3. Functional Requirements — Hệ thống phải làm gì?

### 3.1 Yêu cầu chuẩn tắc

| ID | EARS / hành vi bắt buộc |
|---|---|
| F01-FR-001 | WHEN nhận một chuỗi input, detector SHALL trả true khi và chỉ khi tồn tại ít nhất một ký tự thuộc tập kích hoạt §3.2; nếu không có SHALL trả false. |
| F01-FR-002 | WHEN input chứa kana hoặc Han ideograph supplementary hợp lệ, detector SHALL xét trọn ký tự đó; SHALL NOT chỉ kiểm tra BMP hoặc coi một nửa surrogate pair là ký tự kích hoạt. |
| F01-FR-003 | WHERE input trộn kana/Han với Latin, số, Hangul, emoji, dấu câu hoặc script khác, detector SHALL áp dụng cùng quy tắc “ít nhất một”; không có ngưỡng tỷ lệ hoặc blacklist phủ quyết cả chuỗi. |
| F01-FR-004 | WHERE input không có ký tự kích hoạt, bao gồm chuỗi rỗng, whitespace, dấu phụ độc lập, symbols hoặc Unicode chưa được hỗ trợ, detector SHALL trả false. |
| F01-FR-005 | Detector SHALL bảo toàn input; kết quả SHALL dựa trên chuỗi nguyên gốc, không dựa trên text được sửa encoding, giải mã HTML entities, phiên âm hoặc compatibility-normalize thành ký tự khác. |
| F01-FR-006 | WHEN nhận giá trị ngoài kiểu chuỗi, boundary SHALL báo lỗi đầu vào phân biệt được với false, không tự ép kiểu hoặc đánh giá nội dung của giá trị đó. |
| F01-FR-007 | WHEN chuỗi có UTF-16 surrogate đứng lẻ, detector SHALL coi đơn vị lỗi đó là không kích hoạt, không crash và tiếp tục xét các ký tự hợp lệ còn lại; SHALL NOT tự ghép các surrogate không liền nhau. |
| F01-FR-008 | Detector SHALL xử lý cục bộ với chỉ input được cung cấp; không phát network request, mở popup, đăng ký selection listener, đọc DOM/context hay đọc/ghi storage. |
| F01-FR-009 | Với cùng input và phiên bản quy tắc, detector SHALL cho cùng kết quả không phụ thuộc browser, ngôn ngữ trang, locale, đăng nhập, mạng, font hoặc lần gọi trước. |
| F01-FR-010 | Detector SHALL không giữ input sau lần xử lý, ghi raw text vào log/telemetry/error, hoặc tạo selection history/cache. |
| F01-FR-011 | Core detection SHALL độc lập với browser-vendor globals; browser integration SHALL nằm ngoài core. |
| F01-FR-012 | Detector SHALL áp dụng quy tắc cho toàn bộ input; không âm thầm chỉ xét prefix hoặc cắt text theo giới hạn request Backend. Ký tự kích hoạt ở cuối chuỗi vẫn phải được nhận. |

### 3.2 Tập ký tự kích hoạt: chính sách của F-01

Đây là định nghĩa hành vi quan sát được, không chỉ định thuật toán, thư viện hay biểu thức regular expression. Baseline cố định là **Unicode 17.0.0**; không tự nâng theo Unicode của browser.

Một Unicode scalar value kích hoạt khi thỏa **ít nhất một** dòng sau:

| Nhóm | Membership theo Unicode 17.0.0 | Ví dụ |
|---|---|---|
| Kana letters | General_Category = Lo (Other_Letter) và Script là Hiragana hoặc Katakana | あ, ア, ｶ, ㇰ, U+1B000 |
| Unified ideographs | Unified_Ideograph = Yes | 日, 一, 𠮟, U+20000 |
| Compatibility ideographs | General_Category = Lo, nằm trong CJK Compatibility Ideographs hoặc CJK Compatibility Ideographs Supplement | U+F900, U+FA10, U+2F800 |
| Ideographic zero | U+3007, ngoại lệ bổ sung tường minh | 〇 |

Thuộc tính và membership lấy từ [Scripts.txt](https://www.unicode.org/Public/17.0.0/ucd/Scripts.txt), [PropList.txt](https://www.unicode.org/Public/17.0.0/ucd/PropList.txt), [UnicodeData.txt](https://www.unicode.org/Public/17.0.0/ucd/UnicodeData.txt) và [Blocks.txt](https://www.unicode.org/Public/17.0.0/ucd/Blocks.txt). Lọc Lo ở compatibility blocks loại bỏ code point chưa gán; một dải mã rộng chứa CJK không đủ làm điều kiện chấp nhận.

Tất cả ký tự khác không tự kích hoạt. Những trường hợp dễ nhầm:

| Nhóm | Khi đứng riêng | Khi đi cùng ký tự kích hoạt |
|---|---|---|
| Dakuten/handakuten combining hoặc spacing; half-width voiced marks | false | true nhờ chữ gốc: が, か + U+3099, ｶﾞ |
| Prolonged sound marks ー, ｰ; iteration marks 々, ゝ, ヽ | false | true: スーパー, 時々 |
| Japanese punctuation 。、「」・; closing mark U+3006 | false | Không làm mất kết quả true |
| CJK radicals, ideographic description characters, circled/squared kana/Han | false | Không làm mất kết quả true |
| Emoji kể cả emoji chứa hình chữ Nhật: 🈂️, ㊗️ | false | 日😀 vẫn true |
| Variation selectors, ZWJ, control, unassigned, private-use, noncharacter, replacement character | false | Không làm mất chữ kích hoạt khác trong chuỗi |

Hệ quả được chủ động chọn:

- Chấp nhận kana half-width và kana lịch sử/supplementary thỏa định nghĩa; không cần từ điển Jōyō/Jinmeiyō.
- が và か + U+3099 cùng true do đều có kana letter; không cần thay đổi input.
- Không áp dụng NFKC làm bước quyết định: symbol như ㍿ hoặc ㋐ không được biến thành chữ rồi làm true.
- 〇 true nhưng chữ số ASCII/full-width thuần túy false.
- Không suy ra rằng mọi ký tự có Script_Extensions liên quan Japanese hoặc mọi ký tự có Script=Han đều kích hoạt.
- Tập trên là quy tắc đầy đủ; corpus §7 bảo vệ quy tắc, không thay nó bằng whitelist các ví dụ.

### 3.3 User Scenarios & Testing

**US-01 — Nhận diện selection phục vụ việc đọc (P1).** Reader muốn một chữ kanji, kana hoặc từ tiếng Nhật được nhận đúng để luồng đọc tiếp tục. Kiểm thử độc lập bằng input text, không cần popup hay Backend.

- Given input chỉ là 𠮟; when phân loại; then true.
- Given input 日本語 (Japanese); when phân loại; then true.
- Given input ｶﾀｶﾅ; when phân loại; then true.

**US-02 — Tránh kích hoạt với text không thuộc tập hỗ trợ (P1).** Reader đọc nhiều ngôn ngữ và không muốn symbol hoặc text không liên quan tạo kết quả nhận diện sai. Kiểm thử độc lập bằng negative corpus.

- Given input 안녕하세요, 😀 hoặc U+1D11E; when phân loại từng chuỗi; then false cho từng chuỗi.
- Given input chỉ là U+3099 hoặc ー; when phân loại; then false.
- Given input 中文; when phân loại; then true theo chính sách Han, không được ghi nhận đây là lỗi của US-02.

**US-03 — Nhất quán và riêng tư (P1).** Reader nhận cùng hành vi trên browser mục tiêu mà không phải gửi selection ra ngoài để phát hiện chữ.

- Given cùng corpus trên Chrome, Edge, Brave và Firefox; when phân loại; then từng case có cùng boolean/error category.
- Given mạng bị ngắt và Reader chưa đăng nhập; when phân loại; then kết quả như khi online/đăng nhập, không có network/storage/log chứa input.

## 4. Non-functional Requirements — Tốt đến mức nào?

| ID | Yêu cầu | Cách nghiệm thu |
|---|---|---|
| F01-NFR-001 | Đúng theo Unicode baseline cố định | 100% corpus bắt buộc pass; không nhận code point chưa gán chỉ vì cùng block. Unicode upgrade phải thay baseline có review và regression evidence. |
| F01-NFR-002 | Quyền riêng tư mặc định | Không có network request, raw-input log, storage record hay reference giữ input sau xử lý trong các đường success, negative và error. Test dùng text tổng hợp. |
| F01-NFR-003 | Cross-browser parity | Cùng corpus cho kết quả giống nhau trên Chrome, Edge, Brave và Firefox; report ghi version, OS và baseline. Chromium đơn thuần không thay thế bằng chứng cho cả ba browser Chromium-based. |
| F01-NFR-004 | Ít ảnh hưởng thao tác đọc | Không chờ mạng hoặc debounce bên trong detector. PLAN phải xác định benchmark profile, độ dài input và ngân sách thời gian riêng, đo p95 cho cả true/false và ký tự ở cuối chuỗi. |
| F01-NFR-005 | Không tạo side effect, không phụ thuộc trạng thái | Không sửa DOM/style/selection/focus; không đọc trang; không có kết quả bị ảnh hưởng bởi thứ tự gọi hoặc trạng thái người dùng. |

PERF-001 p95 ≤ 150 ms là mục tiêu từ selection ổn định tới popup shell, không phải ngân sách 150 ms riêng của detector. F-01 cần benchmark contribution để hỗ trợ mục tiêu này; profile/ngân sách kỹ thuật nằm ở PLAN và OD-005. Không tuyên bố đạt popup SLO chỉ từ benchmark F-01.

## 5. Data Model — Dữ liệu đi vào và ra có nghĩa gì?

Không có entity, bảng, migration hoặc dữ liệu người dùng được lưu. Vẫn cần contract dữ liệu tạm thời để người triển khai không tự suy diễn:

| Thành phần | Ý nghĩa và vòng đời |
|---|---|
| Input text | Chuỗi nguyên gốc do caller cung cấp, chỉ dùng trong lần gọi. Không bao gồm URL, toàn trang, tài khoản hay context tự thu thập. |
| Detection result | Boolean cho đúng input hiện tại; không có confidence, detected language, matched spans hoặc canonical ID. |
| Error category | Input sai kiểu hoặc lỗi thực thi bất ngờ; không mang raw text hay object input. Shape/type cụ thể do PLAN định nghĩa. |
| Unicode policy và regression fixtures | Dữ liệu phát triển không chứa selection thực, có version/provenance. Không phải history hay kho từ điển trên client. |

Sự kiện “không có selection” được selection boundary biểu diễn bằng chuỗi rỗng nếu muốn gọi detector. null, undefined, number hoặc object không đồng nghĩa chuỗi rỗng.

## 6. Error Handling — Khi sai thì làm gì?

| Trường hợp | Kết quả/hành vi | Chủ thể xử lý |
|---|---|---|
| Chuỗi rỗng, whitespace, không có chữ kích hoạt | false; không phải lỗi | Detector |
| Kiểu dữ liệu không phải chuỗi | Lỗi input sai kiểu, không ép kiểu hoặc trả false như kết quả bình thường | Boundary của detector |
| Một surrogate đứng lẻ | Không kích hoạt; tiếp tục xét phần hợp lệ còn lại, không sửa input | Detector |
| Chuỗi mojibake, ví dụ literal ð ®Ÿ | Áp dụng quy tắc trên ký tự thực nhận; ví dụ này false. Không tự phục hồi encoding | Detector |
| Lỗi thực thi bất ngờ | Báo lỗi phân biệt được với negative; không nuốt lỗi thành false. Diagnostic chỉ chứa loại lỗi và metadata không tái tạo input | Detector/boundary |
| Lấy selection thất bại, trang chặn quyền, selection đổi trong DOM | Ngoài core F-01; caller xử lý ở feature tích hợp | Browser selection boundary |

Caller chỉ coi true là kết quả đủ điều kiện đi tiếp; error không được coi là true. Điều này không đòi thêm giá trị thứ ba vào boolean result: PLAN chọn cách biểu diễn lỗi riêng, miễn consumer phân biệt được và test kiểm chứng được.

Input rất dài vẫn tuân theo §3.1; không mượn giới hạn payload Backend để thay đổi kết quả. Nếu cần giới hạn input toàn Extension, đó phải là quyết định riêng được thể hiện rõ với caller.

## 7. Acceptance Criteria — Thế nào là xong?

### 7.1 Quy ước corpus

Mỗi mẫu phân tách bởi dấu chấm phẩy dưới đây là một test case độc lập. Ký hiệu U+XXXX là ký tự thực tại code point đó, không phải chuỗi literal “U+XXXX”. Bảng surrogate dùng **UTF-16 code units** vì surrogate đứng lẻ không phải Unicode scalar.

Fixture phải ghi expected result độc lập với thuật toán production; ghi code points/code units khi ký tự không nhìn thấy, supplementary hoặc dễ nhầm. Không tạo expected result bằng cách gọi detector cần kiểm thử.

### 7.2 Corpus phân loại bắt buộc

| AC ID | Input độc lập | Expected | Mục đích |
|---|---|---|---|
| F01-AC-001 | 日; 日本語; 一 | true | Kanji không cần kana kèm theo |
| F01-AC-002 | あ; ひらがな; ア; カタカナ | true | Hai nhóm kana, gồm chữ đơn |
| F01-AC-003 | 𠮟 (U+20B9F); 𠮟る; U+20000 | true | Supplementary Han; đứng riêng ngăn kana che lỗi |
| F01-AC-004 | ｶ; ｶﾀｶﾅ; ｶﾞ; ㇰ; U+1B000 | true | Half-width, phonetic extension, supplementary kana |
| F01-AC-005 | が; U+304B + U+3099 | true | Composed/decomposed có chữ gốc |
| F01-AC-006 | U+F900; U+FA10; U+2F800; 〇 | true | Compatibility BMP/supplementary, zero ngoại lệ |
| F01-AC-007 | Hello 日本語 123; 日本語와 한국어; 日😀; 😀あ | true | “Ít nhất một”, không veto bằng script khác |
| F01-AC-008 | 中文; 漢字; スーパー; 時々 | true | Chính sách Han và dấu đi kèm chữ |
| F01-AC-009 | Chuỗi rỗng; U+0020; U+0009 + U+000A; U+3000; U+00A0 | false | Không có nội dung kích hoạt |
| F01-AC-010 | Hello; Tiếng Việt; 123; ＡＢＣ１２３; !?。、「」・ | false | Latin, số, punctuation cả full-width |
| F01-AC-011 | 안녕하세요; 한; U+1100; U+3131; U+FFA1 | false | Hangul syllable, Jamo, compatibility/half-width Jamo |
| F01-AC-012 | Привет; مرحبا; αβ; U+3105; U+A000; U+17000 | false | Cyrillic, Arabic, Greek, Bopomofo, Yi, Tangut |
| F01-AC-013 | 😀; 🈂️; ㊗️; U+1D11E; U+1D400; U+1F200 | false | Emoji, music, math, square kana symbol |
| F01-AC-014 | U+2F00; U+2E80; U+2FF0; ㋐; ㍿; U+3006 | false | Radical/description/compatibility symbols; không normalize để accept |
| F01-AC-015 | ー; ｰ; 々; ゝ; ヽ; U+3099; U+309A; U+309B; U+309C; U+FF9E; U+FF9F | false | Dấu độc lập không phải chữ kích hoạt |
| F01-AC-016 | U+FE0F; U+E0100; U+200D; U+0000; U+FFFD; U+E000; U+F0000; U+10FFFF | false | Selector, control, replacement, private-use, noncharacter |
| F01-AC-017 | 葛 + U+E0100; U+200D + 日 + U+FE0F | true | Selectors/format không che chữ gốc |
| F01-AC-018 | U+3097; U+FA6E; U+2A6E0 | false | Khoảng chưa gán theo baseline, sát dải hợp lệ |
| F01-AC-019 | Literal ð ®Ÿ = U+00F0 U+00A0 U+00AE U+0178 | false | Literal ở MIGRATION_DECISION §9.1; không sửa encoding |
| F01-AC-020 | Literal `&#x65E5;` (8 ký tự ASCII); literal `\u65E5` (6 ký tự ASCII) | false | Không giải mã thành 日 |
| F01-AC-021 | 100.000 lần a; cùng chuỗi đó nối 𠮟 ở cuối; 𠮟 nối cùng chuỗi đó | false; true; true tương ứng | Không chỉ xét prefix, không cắt theo payload limit |
| F01-AC-022 | Gọi tuần tự 日, Hello, 日, chuỗi rỗng, 𠮟, 😀; lặp 100 vòng | true, false, true, false, true, false ở mọi vòng | Không có trạng thái từ lần gọi trước |

### 7.3 Input lỗi, tích hợp boundary và privacy

| AC ID | Given / When | Then |
|---|---|---|
| F01-AC-023 | null, undefined, number, boolean, array hoặc object, mỗi loại một case | Báo lỗi sai kiểu; không trả boolean như classification bình thường, không ép kiểu object, không ghi raw input |
| F01-AC-024 | Chuỗi chỉ có unit D800; chỉ DC00; D800 + ASCII a + DC00 | false cho từng chuỗi; không crash hoặc ghép qua ký tự xen giữa |
| F01-AC-025 | Unit D800 + 日; 𠮟 + unit DC00 | true cho từng chuỗi; input không bị sửa |
| F01-AC-026 | Boundary nhận lỗi thực thi được mô phỏng tại seam do PLAN xác định | Consumer phân biệt lỗi với false; không khởi động analysis do lỗi; diagnostic không có input |
| F01-AC-027 | Chạy corpus trong môi trường cô lập không có DOM, browser-vendor globals, storage, network hoặc auth | Kết quả đúng; không truy cập dịch vụ đó; dependency inspection xác nhận FR-011 |
| F01-AC-028 | Harness tối thiểu trên Chrome, Edge, Brave, Firefox, cùng corpus/policy; ghi version | Mỗi boolean/error category giống nhau; browser chưa chạy được ghi pending, không coi là pass |
| F01-AC-029 | Dùng selection tổng hợp có dấu nhận diện riêng trên đường true/false/error; quan sát log, storage, request và retention | Không raw input được ghi/truyền/giữ lại; review xác nhận không cache/history hoặc tham chiếu giữ input sau xử lý |
| F01-AC-030 | Gọi detector trong host-page harness có DOM/selection/focus xác định trước; so sánh trước/sau | Không DOM/style/focus/selection mutation hoặc popup do F-01; input nguyên vẹn |
| F01-AC-031 | Chạy corpus khi online/offline, anonymous/authenticated, locale/ngôn ngữ trang/font khác nhau | Cùng kết quả; không cần auth/network, không đọc thêm context |
| F01-AC-032 | Benchmark input nhỏ, trung bình và 100.000 ký tự; positive đầu/cuối, negative, supplementary | Report ghi device/browser/version, độ dài, số mẫu, cold/warm và p95; đạt ngân sách đã chốt trong PLAN |
| F01-AC-033 | Review corpus khi chuyển sang implementation | Fixture bắt buộc giữ ID và expected; known legacy failures đã xác minh được thêm với provenance, không kế thừa kết quả sai cũ |

Mẫu dài là workload kiểm thử, không phải giới hạn input sản phẩm. Không yêu cầu zeroize hay kiểm tra thời điểm GC: retention ở AC-029 được đánh giá bằng review ownership/reference và quan sát side effects.

### 7.4 Success Criteria và delivery gate

| ID | Kết quả đo được |
|---|---|
| F01-SC-001 | 100% case bắt buộc ở AC-001–025 cho đúng classification/error; không bỏ qua supplementary đứng riêng hoặc negative corpus. |
| F01-SC-002 | 0 bất đồng kết quả giữa bốn browser đối với corpus đã chạy; AC-028 report ghi bằng chứng từng browser. |
| F01-SC-003 | 0 network request, raw-input log hoặc storage/history record do detector tạo; AC-027, AC-029–031 đạt. |
| F01-SC-004 | 100% ca lỗi đã định nghĩa không làm consumer hiểu là nhận diện thành công; AC-023–026 đạt. |
| F01-SC-005 | Benchmark đáp ứng ngân sách đã định trong PLAN, không cắt prefix để làm đẹp kết quả; AC-021 và AC-032 đạt. |

“Spec sẵn sàng cho PLAN” chỉ xác nhận quy tắc và tiêu chí đã được mô tả. “Feature hoàn thành” yêu cầu implementation, automated regression tests, mọi AC áp dụng, evidence và review thực tế. Chưa chạy browser/benchmark thì ghi pending; không suy ra pass từ checklist tài liệu đầy đủ.

## 8. Out of Scope — Hệ thống không làm gì?

- Selection listeners, đọc DOM, ruby-safe base-text/context extraction và hạn chế quyền trang: capture/F-02.
- Popup shell, positioning, style isolation, debounce, đóng popup: F-03.
- Async identity, stale response, cancellation: F-04.
- Gửi/validate payload analysis, tra từ/kanji, tokenizer, morphology, grammar, dịch: F-05 và các capability liên quan.
- Phân biệt câu tiếng Nhật với tiếng Trung/Hanja; scoring ngôn ngữ; giới hạn Jōyō/Jinmeiyō; tra dictionary để quyết định true.
- Sửa mojibake, romanization, HTML/entity decoding, thay đổi selection hoặc chuẩn hóa text cho Backend.
- Lưu history, preferences mới, thêm authentication, tải linguistic datasets cho Reader.
- Platform Foundation feature, Extension UI đầy đủ, thay API hoặc schema Backend.
- Tự nâng Unicode theo runtime; xóa/tái cấu trúc legacy.

## 9. Assumptions, Dependencies & Planning Handoff

### 9.1 Quyết định cụ thể hóa trong bản 0.2

| Quyết định | Căn cứ và giới hạn |
|---|---|
| Nhận diện presence theo script | Phù hợp glossary và JPN-001; language identification ngoài scope. Chấp nhận tập ideographs §3.2 thay vì tự đoán chữ “chỉ dùng ở Nhật”. |
| Một chữ đủ làm true, kể cả mixed input | Giữ single kanji/kana và mixed Latin; không tự thêm tỷ lệ chữ Nhật. |
| Unicode 17.0.0 cố định | Nguồn versioned giúp test/parity tái lập được; không khẳng định là phiên bản mới nhất hoặc browser đã hỗ trợ sẵn. |
| Marks/symbols không tự kích hoạt; U+3007 có ngoại lệ rõ | Cụ thể hóa “chứa chữ để đọc/tra”, tránh nhận cả CJK block hoặc compatibility symbols. |
| Lỗi kiểu tách false; lone surrogate không kích hoạt | Tránh che lỗi caller, xử lý chuỗi không hoàn hảo một cách xác định. |

Đây là feature decisions của draft để lập PLAN; nếu Product Owner đổi chính sách, phải sửa SPEC và expected fixtures trước khi đổi implementation.

### 9.2 Dependencies và thứ tự tích hợp

F-01 có thể xây dựng/kiểm thử với chuỗi tổng hợp trước khi có Extension hoàn chỉnh. PLAN được bố trí tooling/harness tối thiểu cho detector, không biến thành dự án Foundation riêng.

Đường tích hợp phải bảo đảm: lấy selection → trích base text không chứa ruby annotations (F-02) → dùng F-01 xác nhận input cuối → F-03 dùng kết quả. Precheck trước extraction có thể phục vụ tối ưu, nhưng không được quyết định cuối chỉ dựa vào text lẫn annotation. FEATURE_MAP mô tả F-01 trước F-02 ở mức hành trình; DOM-001 vẫn quyết định chất lượng input cuối.

Không phụ thuộc Backend/DB/Tokenizer đang chạy. Helper CJK Backend hiện có không thay thế nguồn sự thật ở §3.2; đối chiếu parity khi thực sự chia sẻ semantics, không mở scope sửa Backend trong F-01.

### 9.3 Những việc PLAN phải làm rõ

- Vị trí module, boundary input/error, harness và cách hiện thực Unicode baseline trên mọi target browser.
- Provenance của dữ liệu Unicode thực sự dùng: version, source URL, checksum, license/notice khi phân phối dữ liệu hoặc bảng sinh từ nó.
- Representation của fixture code points/code units để không mất surrogate/variation selector khi lưu/đọc.
- Benchmark profile/ngân sách riêng có căn cứ và browser/OS versions dùng làm bằng chứng.
- Kiểm thử độc lập, dependency/side-effect/error inspection; trace FR/NFR tới task/test.
- Quy trình nâng Unicode baseline có review. Không cài dependency chỉ vì SPEC định nghĩa một baseline dữ liệu.
- Finding legacy đã xác minh phải có expected behavior đúng; không cần xóa legacy để nghiệm thu.

Đây là các quyết định kỹ thuật cho bước plan. Không có câu hỏi nghiệp vụ bắt buộc còn treo trong phiên bản này.

## 10. Traceability & Constitution Alignment

### 10.1 Requirement → acceptance mapping

| Requirement F-01 | Acceptance evidence | Nguồn |
|---|---|---|
| FR-001 | AC-001–020 | JPN-001, glossary |
| FR-002 | AC-003, AC-004, AC-006, AC-024–025 | JPN-001 |
| FR-003 | AC-007–008, AC-017 | JPN-001, mixed input |
| FR-004 | AC-009–020 | JPN-001 |
| FR-005 | AC-005, AC-014, AC-019–020, AC-030 | Linguistic correctness, input preservation |
| FR-006 | AC-023, AC-026 | Boundary validation, no swallowed failures |
| FR-007 | AC-024–025 | Unicode correctness |
| FR-008 | AC-027, AC-029–031 | PRIV-006, scope |
| FR-009 | AC-022, AC-028, AC-031 | BROWSER-002, AUTH-001 |
| FR-010 | AC-029 | PRIV-002, PRIV-006 |
| FR-011 | AC-027–028 | BROWSER-001–002 |
| FR-012 | AC-021, AC-032 | Full-input classification |
| NFR-001 | AC-001–025, AC-033 | JPN-001, MIGRATION §6.4/§9.1 |
| NFR-002 | AC-023, AC-026–029 | PRIV-002, PRIV-006 |
| NFR-003 | AC-028 | BROWSER-002 |
| NFR-004 | AC-021, AC-032 | Contribution to PERF-001; OD-005 remains open globally |
| NFR-005 | AC-022, AC-027, AC-030–031 | Browser isolation, privacy |
| US-01 | AC-001–008, AC-017 | Positive reading scenarios |
| US-02 | AC-009–020 | Negative reading scenarios |
| US-03 | AC-027–031 | Parity and privacy |

FR/NFR/AC trong bảng đều có prefix F01-. EXT-001/EXT-002 dùng output F-01 ở tích hợp; spec này không tuyên bố đã nghiệm thu popup trong sản phẩm hoàn chỉnh.

### 10.2 Constitutional impact

| Nguyên tắc | Áp dụng |
|---|---|
| §2 Backend authority | Detector local là điều kiện phía client; không tạo kho kiến thức thay Backend. |
| §3, §6 Browser isolation | Core độc lập browser-vendor globals; extraction ở boundary; quyết định cuối tương thích base text ruby-safe. |
| §4 Linguistic correctness | Membership, policy version, Unicode edge cases và known limitations được nêu rõ. |
| §4 Stable identity | Không tạo/lưu linguistic resource, không phát sinh canonical IDs/spans. |
| §5 Data/provenance | Unicode references phục vụ chính sách; dữ liệu dùng khi triển khai phải ghi provenance/license trong PLAN. Không migration DB/sửa canonical data. |
| §7 Privacy/auth | Selected text có thể nhạy cảm; local, ephemeral, không log/lưu/truyền; quyền dùng không phụ thuộc đăng nhập. |
| §8 Providers | Không runtime external provider hoặc secret. Unicode Consortium cung cấp tài liệu/dữ liệu build-time, không nhận selection. |
| §9–10 Gates/testing | SPEC/checklist không thay test evidence; regression và parity phải được kiểm chứng trước tuyên bố done. |
| Proposed exceptions | Không đề xuất ngoại lệ Constitution. |

## 11. Reference & Revision Notes

- Nguồn sản phẩm/migration ở đầu tài liệu có thẩm quyền về scope; Unicode data có thẩm quyền về property values. Chính sách chọn tập ký tự thuộc F-01.
- [Unicode 17.0.0](https://www.unicode.org/versions/Unicode17.0.0/) là baseline draft; nguồn dữ liệu versioned liên kết tại §3.2.
- Literal ð ®Ÿ hiện diện thật trong MIGRATION_DECISION. Không đủ bằng chứng trong repo để khẳng định đây là bug history đã tái hiện hoặc khôi phục duy nhất thành 𠮟; giữ nó như negative fixture và giữ riêng U+20B9F positive fixture.
- Review trước đánh dấu “critical” cho thiếu mô tả không đồng nghĩa đã chứng minh runtime vi phạm Constitution. Bản này bổ sung độ rõ/traceability, không ghi nhận incident runtime chưa có bằng chứng.
- 0.2.0 thay draft 0.1.0: thêm membership policy, input/output tạm thời, error contract, acceptance corpus độc lập, success criteria và handoff gates.
