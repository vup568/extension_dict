# So sánh lựa chọn nền tảng: Yomitan và JP Reading Platform

Ngày đánh giá: 2026-10-25, Asia/Saigon. Chế độ: phân tích mã nguồn, không triển khai.

## Kết luận

Với mục tiêu hiện đang được thảo luận — dữ liệu tập trung, học chủ yếu trên Web, Extension hỗ trợ tra khi đọc ở website khác — nên giữ JP Reading Platform làm nền tảng và tiếp nhận có chọn lọc cơ chế hoàn nguyên, bộ kiểm thử, kinh nghiệm quét DOM của Yomitan.

Đây là lựa chọn dựa trên trách nhiệm của từng hệ thống và phần phải thay khi đổi nền, không phải lý do giữ mã vì đã đầu tư. Nếu mục tiêu thu hẹp thành một Extension tra từ offline để ra sản phẩm nhanh, chấp nhận từ điển cục bộ và giao diện của Yomitan, fork Yomitan là lựa chọn mạnh hơn.

## Phạm vi và bằng chứng

- Yomitan: `E:/Road To N3/Yomitan`, nhánh `master`, commit `d3c54c10264673eca0d66b970ef74cd5c8b048e3`, cây làm việc sạch khi kiểm tra.
- Dự án hiện tại: `E:/jp-dict-extension`, nhánh `feat/tokenizer-deinflection`, HEAD `a42a323a3d62043fabbacd7365da32db014d151d`; phân tích bao gồm thay đổi và file chưa commit đang có, không chỉ HEAD.
- Nguồn sản phẩm: REQUIREMENT.md đã duyệt, Feature Map, README và mã hiện có. Mong muốn Web là trung tâm/Extension tùy chọn đang được trao đổi, chưa tự sửa phạm vi PRD.
- Đã đọc các đường quét văn bản, tra từ, hoàn nguyên, nhập từ điển, định dạng kết quả, định danh và API của cả hai bên. Không cài dependency, chạy code của repo, build, sửa code sản phẩm hoặc chạy benchmark.
- Repomix không có sẵn; dùng đọc trực tiếp các file liên quan và ba nhánh phân tích độc lập.

## Đối chiếu hai hướng

| Tiêu chí | Lấy Yomitan làm gốc | Giữ nền tảng hiện tại, tiếp nhận chọn lọc |
|---|---|---|
| Extension tra từ trưởng thành sớm | Lợi thế lớn: giữ scan, popup, settings, import, audio, Anki | Phải tiếp tục hoàn thiện trải nghiệm tra trên nền đang có |
| Backend giữ tri thức chung | Phải thay hoặc bổ sung đường tra local, schema kết quả và các phụ thuộc metadata | Đã có ranh giới API/use case/repository và PostgreSQL |
| Web đọc/học, tài khoản, thư viện | Vẫn phải xây, không tự có khi fork | Vẫn phải xây; có sẵn nền API và domain, chưa có ứng dụng hoàn chỉnh |
| Hoàn nguyên dạng chia | Có engine nối nhiều phép biến đổi, thử nhiều độ dài, nhiều ngôn ngữ | Có mẫu toàn từ có tham số và bằng chứng Sudachi; cần đánh giá mở rộng sinh ứng viên |
| Ngữ pháp N5–N4 và giải thích Việt | Chuỗi biến đổi không tự thay thế các yêu cầu này | Đã có grammar engine và F09 scoped explanation; dữ liệu/phạm vi vẫn chưa hoàn chỉnh |
| Canonical identity và restriction | Cần mapping từ dictionary-specific identity sang model học tập chung | Đã có typed IDs, mô hình sense/applicability và parser upstream |
| Cập nhật upstream | Ít sửa core thì dễ theo upstream; sửa sâu HTTP/schema/UI thì khó hơn | Sở hữu adapter/engine chuyển thể; phải quản lý rule version và kiểm thử đối chiếu |
| Từ điển Việt đầy đủ | Fork không tự giải quyết nguồn nghĩa Việt | Vẫn phải hoàn tất mapping, review và import dữ liệu thật |

Không có số ngày triển khai hay tỷ lệ tiết kiệm trong báo cáo này: chưa có thử nghiệm tích hợp để ước lượng đáng tin cậy.

## Thành phần nên tái sử dụng

| Thành phần Yomitan | Giá trị | Hình thức phù hợp |
|---|---|---|
| `japanese-transforms.js` | Quy tắc thường, bất quy tắc, rút gọn và chuỗi nhiều lớp | Chuyển thể thành quy tắc có phiên bản, giữ điều kiện và ngoại lệ |
| `language-transformer.js` | Sinh ứng viên, nối phép biến đổi, lưu trace, chống vòng lặp | Chuyển thể hoặc đóng gói sau đánh giá ranh giới runtime; không cắm thẳng vào C# |
| Các test biến đổi tiếng Nhật | Cặp input/root/lớp từ/chuỗi lý do, cả ca âm tính | Corpus đối chiếu có provenance; không bê nguyên test runner sang .NET |
| Một phần `translator.js` | Tra theo lô, dedupe ứng viên, giữ nhiều đường hợp lệ, xếp hạng | Tham khảo và chuyển thể từng cơ chế |
| DOM scanner, text source và fixtures | Ruby, nội dung ẩn, tọa độ, chữ dọc, frame và tương tác chuột/chạm | Tiếp nhận hành vi và regression phù hợp với chính sách selection |
| Định dạng từ điển và metadata | Tần suất, pitch accent, nguồn bổ sung và import/export | Adapter dữ liệu tùy chọn, có nguồn và mapping riêng |

Không đề xuất thay PostgreSQL bằng IndexedDB, thay toàn bộ popup React bằng Display của Yomitan, hoặc nhận ZIP bất kỳ làm nguồn canonical duy nhất.

## Các phát hiện quyết định từ mã nguồn

### 1. Lõi hoàn nguyên có thể tách, toàn bộ hệ tra từ không phải drop-in

Quy tắc Nhật import hai helper `suffixInflection`/`wholeWordInflection`. Helper tạo RegExp và hàm biến đổi; descriptor còn sinh ngoại lệ và có nhóm điều kiện trung gian. Đây không phải JSON có thể sao chép vào DB rồi dùng ngay. Trong phiên bản được kiểm tra, các quy tắc Nhật dùng hai helper này; chuyển thành dữ liệu hữu hạn là khả thi về kỹ thuật nhưng cần bảo toàn semantics.

`LanguageTransformer` gần như độc lập trình duyệt; runtime import chính là logger. `Translator` nhận database qua constructor, nên cũng không nên mô tả nó như một class buộc Chrome API. Tuy nhiên nó phụ thuộc giao diện truy vấn, grouping, metadata và DTO riêng của Yomitan. Database mặc định dùng IndexedDB; API đó khác repository PostgreSQL của nền tảng.

Nguồn: [helpers](<E:/Road To N3/Yomitan/ext/js/language/language-transforms.js:27>), [Japanese descriptor](<E:/Road To N3/Yomitan/ext/js/language/ja/japanese-transforms.js:18>), [transformer](<E:/Road To N3/Yomitan/ext/js/language/language-transformer.js:118>), [DB injection](<E:/Road To N3/Yomitan/ext/js/language/translator.js:36>), [bulk lookup](<E:/Road To N3/Yomitan/ext/js/language/translator.js:475>).

### 2. Điểm nghẽn hiện tại là ràng buộc ứng viên với giải thích

`ConjugationCandidate` yêu cầu một `ConjugationExplanationDto` đầy đủ. Use case chỉ thêm kết quả cho ứng viên có giải thích Việt và nội dung các segment. Chỉ đưa dạng gốc và trace từ Yomitan vào luồng hiện tại chưa đủ để có fallback tra từ hữu ích.

Nên đánh giá tách khả năng xác định mục từ khỏi khả năng cung cấp giải thích chia thể, đồng thời giữ kiểm tra canonical identity, POS và sense applicability. Đây là đề xuất thay đổi cần đặc tả riêng trước triển khai, không tự coi là đã được duyệt trong F09.

Nguồn: [candidate](E:/jp-dict-extension/src/usecase/Conjugation/Models/ConjugationParadigm.cs:13), [content gate](E:/jp-dict-extension/src/usecase/Dictionary/LookupWordUseCase.cs:162), [adapter hiện tại](E:/jp-dict-extension/src/infra/Adapters/GrpcDictionaryTokenizerAdapter.cs:48).

### 3. Hoàn nguyên, morphology và giải thích Việt là ba khả năng khác nhau

Yomitan tạo ứng viên dạng gốc cùng lịch sử biến đổi. Sudachi hiện cung cấp token, base form, POS và span cho hệ thống. Chuỗi `causative` hoặc `potential or passive` không tự trở thành giải thích tiếng Việt, không quyết định nghĩa theo ngữ cảnh, và không thay engine phát hiện occurrence ngữ pháp N5–N4.

Mã hiện tại cũng dùng mẫu `{stem}` tổng quát, không phải bảng ghi từng từ chia sẵn. Khác biệt chủ yếu là khớp mẫu toàn từ có bằng chứng tokenizer so với sinh nhiều chuỗi hoàn nguyên nối tiếp.

Nguồn: [mẫu hiện tại](E:/jp-dict-extension/src/usecase/Conjugation/ConjugationParadigmMatcher.cs:25), [Sudachi output](E:/jp-dict-extension/sidecars/tokenizer/app.py:76), [ca nhiều lớp của Yomitan](<E:/Road To N3/Yomitan/test/language/japanese-transforms.test.js:1569>).

### 4. Định danh dữ liệu không tương đương

Yomitan term row có expression, reading, glossary, rules, score và sequence. Row ID là auto-increment trong IndexedDB; sequence hỗ trợ grouping và không được schema bảo đảm là canonical JMdict ID. Exporter có thể giữ cặp form/reading hợp lệ bằng cách tách row, nhưng định dạng chung không bảo đảm khôi phục lossless sense/restriction/source review của upstream.

Parser hiện tại giữ `ent_seq`, raw XML, reading restrictions và joint sense applicability. Vì vậy nguồn upstream JMdict vẫn phù hợp hơn để dựng dữ liệu canonical. Dữ liệu Yomitan có thể là nguồn bổ sung hoặc định dạng trao đổi sau mapping, không thay thế mặc định.

Nguồn: [term importer](<E:/Road To N3/Yomitan/ext/js/dictionary/dictionary-importer.js:825>), [local ID](<E:/Road To N3/Yomitan/ext/js/dictionary/dictionary-database.js:132>), [JmdictParser](E:/jp-dict-extension/src/infra/DictionaryImport/Parsing/JmdictParser.cs:27).

### 5. Giữ nền hiện tại không có nghĩa phần nền tảng đã hoàn thành

README ghi Web, dịch, authentication và thư viện cá nhân chưa triển khai. Full dictionary import và mapping Việt cũng còn dang dở. Có domain entity không đồng nghĩa có một ứng dụng học tập hoạt động.

Có thêm hai giới hạn cần giữ trong đánh giá: sense key của parser hiện dựa vào vị trí sense; repository lookup hiện đọc trực tiếp các bảng, chưa lọc theo current published KnowledgeRelease. Không dùng sự tồn tại của các model làm bằng chứng runtime hoàn chỉnh.

Nguồn: [trạng thái sản phẩm](E:/jp-dict-extension/README.md:26), [import tasks](E:/jp-dict-extension/.sdd/specs/002-import-dictionary-data/TASKS.md:23), [repository](E:/jp-dict-extension/src/infra/Persistence/Repositories/EfDictionaryRepository.cs:22).

## Những giả định đã phản biện

| Câu hỏi | Câu trả lời từ Yomitan | Câu trả lời hiện tại | Rủi ro nếu bỏ qua |
|---|---|---|---|
| Lõi tra có cần giữ trong browser? | Mặc định DB và worker trong extension | Backend cung cấp tri thức chung | Fork sâu rồi phải thay chính phần đang tạo lợi thế của Yomitan |
| Có cần toàn bộ UI/settings/Anki? | Có sẵn và tích hợp với model dictionary/options | Popup React đã có; Web chưa có; Anki ngoài MVP | Mang theo nhiều hệ thống không phục vụ mục tiêu gần nhất |
| Lemma có đủ để trả nghĩa? | Đối chiếu ứng viên với dictionary | Candidate hiện đi cùng explanation DTO | Thay engine nhưng vẫn rỗng do thiếu nội dung Việt |
| Row ID/sequence có dùng để lưu học tập? | Identity theo bộ dictionary/local database | Canonical ID có namespace và mapping nguồn | Tham chiếu học tập không ổn định hoặc gán nhầm mục từ |
| Có thể bê nguyên scan theo prefix/context? | Phù hợp hover, mở rộng vùng và thử độ dài | Hiện bảo toàn selection đã chọn | Nhận diện một phần như toàn bộ selection hoặc gửi thừa context |
| Logging có thể dùng nguyên? | Cycle warning chứa text/trace, logger có URL | Cấm log raw selected text mặc định | Mang dữ liệu trang vào log server |
| C# port có tự bỏ GPL? | Mã công bố GPL-3.0-or-later | Giấy phép việc tiếp nhận cần quyết định rõ | Chọn sai mô hình phát hành vì hiểu sai nghĩa vụ giấy phép |
| Có bằng chứng nhanh/bao phủ tốt hơn trên dữ liệu chung chưa? | Có test upstream và nhiều tính năng | Có corpus scoped riêng | Đánh đồng lượng tính năng với kết quả head-to-head đã đo |

Nguồn scan và privacy: [Yomitan prefix scan](<E:/Road To N3/Yomitan/ext/js/language/translator.js:537>), [current selected Range](E:/jp-dict-extension/extension/src/browser/popup/startSelectionPopup.ts:42), [cycle log](<E:/Road To N3/Yomitan/ext/js/language/language-transformer.js:132>).

## Lựa chọn thứ ba: Yomitan client, backend của mình

Có điểm nối kỹ thuật ở `termsFind`. Tuy nhiên response hiện tại của nền tảng không tương thích sẵn với TermDictionaryEntry của Yomitan; còn metadata, media, options, frame messaging, grammar và canonical IDs cần xử lý. Đây là một hướng fork có adapter, không phải chỉ đổi URL API.

Giữ stock local lookup rồi thêm backend lưu học tập ít xâm lấn core hơn, nhưng chấp nhận thay nguyên tắc tri thức tập trung và cần mapping ID. Lựa chọn này đáng cân nhắc nếu ưu tiên đã đổi sang tận dụng tối đa extension hiện có; nó không phải mặc định cho mục tiêu Web/Backend đang mô tả.

Nguồn: [termsFind](<E:/Road To N3/Yomitan/ext/js/background/backend.js:555>), [Display phụ thuộc môi trường](<E:/Road To N3/Yomitan/ext/js/display/display.js:315>), [contract hiện tại](E:/jp-dict-extension/extension/src/api/analysisContract.ts:1).

## Giấy phép và mô hình tiếp nhận

Các file nguồn đã đọc của Yomitan ghi GPL-3.0-or-later. Sao chép hoặc dịch mã sang C# không tự làm mất nghĩa vụ của mã gốc. Việc phát hành extension cho người dùng cần được đánh giá khác với mã chỉ chạy nội bộ trên máy chủ; GPL và AGPL cũng có khác biệt về sử dụng qua mạng. Không kết luận rằng cứ gọi một service là mọi nghĩa vụ tự biến mất, hoặc rằng mọi backend liên quan bắt buộc phải công khai.

Nguồn: [license trong file](<E:/Road To N3/Yomitan/ext/js/language/language-transformer.js:1>), [GNU FAQ về dịch mã](https://www.gnu.org/licenses/gpl-faq.en.html#TranslateCode), [GNU FAQ về thay đổi dùng trên server](https://www.gnu.org/licenses/gpl-faq.en.html#UnreleasedMods). Giấy phép dataset/test fixtures cần kiểm tra riêng theo nguồn thực tế lấy về.

## Quyết định đề xuất và điều còn cần xác minh

Giữ backbone API/canonical data/React hiện tại. Giá trị tiếp nhận ưu tiên là cơ chế hoàn nguyên và corpus Nhật của Yomitan, sau đó mới đến hành vi DOM và metadata phù hợp. Tiếp tục dùng nguồn dictionary upstream cho canonical data; Yomitan ZIP là khả năng bổ sung, không phải lối tắt mặc định thay toàn bộ importer.

Trước khi chốt một phương án tích hợp engine cụ thể, cần thử trên cùng một nguồn từ điển và corpus chung: dạng gốc, chuỗi chia dài, khẩu ngữ, ngoại lệ, từ không có trong DB, các cách hiểu cạnh tranh, restriction và Unicode. Đo độ đúng, sai dương, latency, lượng ứng viên và khả năng tiếp tục tra nghĩa khi thiếu giải thích Việt. Đây là phép kiểm chứng để chọn thiết kế tiếp theo, chưa phải kế hoạch triển khai đã duyệt.

Các điểm còn mở: chính sách phát hành mã và GPL; nguồn nghĩa Việt; phạm vi lookup chính xác so với hover/prefix; chi phí bảo trì bản chuyển thể; benchmark và hành vi runtime trên dữ liệu thật. Không có thay đổi sản phẩm hoặc kiến trúc nào được thực hiện trong lần phân tích này.
