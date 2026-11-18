# So sánh mã nguồn mở hỗ trợ đọc tiếng Nhật trên web

Ngày nghiên cứu: 2026-10-25, Asia/Saigon. Phạm vi: tìm sản phẩm đã phát hành có popup tra từ tiếng Nhật, bằng chứng sử dụng thực tế, khả năng tùy biến và mức tương đồng với JP Reading Platform V2. Đây là kết quả khảo sát tài liệu; chưa chạy thử hoặc kiểm toán khả năng tích hợp.

## Kết quả chính

Yomitan là lựa chọn ưu tiên để đánh giá một extension có thể tùy biến nhanh: có cộng đồng người dùng lớn, nhập nhiều từ điển, hoàn nguyên dạng chia và tích hợp Anki. Jiten + Jiten Reader gần hơn với định hướng cả web, backend và học từ; backend .NET/PostgreSQL gần dự án hiện tại, nhưng frontend là Nuxt/Vue và quy mô người dùng extension nhỏ hơn.

10ten và Rikaikun là các lựa chọn trưởng thành khác. Không dự án nào được khảo sát là bằng chứng về khả năng tra được 100% mọi từ tiếng Nhật, hoặc thay thế sẵn toàn bộ giải thích ngữ pháp bằng tiếng Việt của dự án.

## Phương pháp và cách đọc số liệu

- Dùng nguồn chính thức: GitHub, tài liệu nhà phát triển và các kho extension Chrome/Mozilla; đối chiếu chức năng với kiến trúc hiện có trong dự án.
- Ưu tiên số người dùng do kho extension hiển thị thay vì dùng GitHub stars làm đại diện cho người dùng. Không cộng số Chrome/Firefox thành số người duy nhất.
- Số người dùng và phiên bản là ảnh chụp tại thời điểm nghiên cứu, có thể thay đổi. Tài liệu chính thức không thay thế kiểm thử thực tế.
- Các khuyến nghị về chi phí tùy biến là nhận định sơ bộ, chưa phải ước lượng triển khai.

## So sánh

| Dự án | Người dùng hiển thị | Bảo trì được xác nhận | Giấy phép khai báo | Mức phù hợp |
|---|---|---|---|---|
| Yomitan | Chrome 100.000; Firefox 51.604 | Store stable 26.10.12.0, cập nhật 22/10/2026 | GPLv3 | Ưu tiên cho extension tùy biến và nhiều từ điển |
| 10ten Japanese Reader | Chrome 30.000; Firefox 22.468 | Release 1.28.0, 02/09/2026 | Họ GPL-3.0 | Popup gọn, dùng ngay, tra từ/tên riêng/kanji |
| Rikaikun | Chrome 200.000 | Store 3.2.42, 30/09/2026 | GPL-3.0 | Nền tra Nhật–Anh đơn giản, trưởng thành |
| Jiten + Jiten Reader | Chrome extension 9.000 | Store Reader 1.4.1, 15/10/2026 | Backend Apache-2.0; Reader MIT | Gần mô hình web + backend + học từ |

### Yomitan

Popup tra từ và kanji trên trang; phát âm, thông tin từ điển, nhập nhiều từ điển tùy chỉnh, tích hợp Anki qua AnkiConnect. Từ điển được lưu và tra trong trình duyệt, khác mô hình backend authoritative hiện tại. Có khả năng xây bộ Nhật–Việt theo định dạng nhập, nhưng chưa xác minh được một bộ Nhật–Việt sẵn có với chất lượng và giấy phép đủ rõ để phân phối.

Mã nguồn vẫn có bộ quy tắc biến đổi tiếng Nhật. Vì vậy, giá trị kế thừa nằm ở engine, hệ sinh thái từ điển và lịch sử xử lý ca thực tế. Việc lấy riêng engine sang C# cần khảo sát riêng; fork extension không phải cách cắm trực tiếp vào backend .NET.

Nguồn: [repo](https://github.com/yomidevs/yomitan), [Chrome](https://chromewebstore.google.com/detail/yomitan-popup-dictionary/likgccmbimhjbgkjambclfkhldnlhbnn), [Firefox](https://addons.mozilla.org/en-US/firefox/addon/yomitan/), [releases](https://github.com/yomidevs/yomitan/releases), [từ điển](https://yomitan.wiki/dictionaries/), [quy tắc tiếng Nhật](https://github.com/yomidevs/yomitan/blob/master/ext/js/language/ja/japanese-transforms.js), [giấy phép](https://github.com/yomidevs/yomitan/blob/master/LICENSE).

### 10ten Japanese Reader

Tra từ, tên riêng và kanji bằng rê chuột/chạm, có pitch accent, xử lý nhiều dạng rút gọn và chia bất quy tắc. README ghi cập nhật dữ liệu hai lần mỗi tuần. Dùng TypeScript/Preact và dữ liệu cục bộ IndexedDB. Khả năng tra từ cơ bản trưởng thành; chưa thấy hướng nhập nhiều từ điển tùy ý tương đương Yomitan trong tài liệu đã khảo sát.

Changelog có bổ sung cách đọc kanji tiếng Việt, nhưng không được diễn giải thành có sẵn toàn bộ nghĩa Nhật–Việt. Yêu cầu thêm từ điển tiếng Việt #1420 vẫn mở tại thời điểm nghiên cứu. Metadata có khác biệt GPL-3.0-or-later và nhãn Mozilla GPLv3-only; cần đối chiếu đúng phiên bản nếu lấy mã.

Nguồn: [repo](https://github.com/birchill/10ten-ja-reader), [Chrome](https://chromewebstore.google.com/detail/10ten-japanese-reader-rik/pnmaklegiibbioifkmfkgpfnmdehdfan), [Firefox](https://addons.mozilla.org/en-US/firefox/addon/10ten-ja-reader/), [releases](https://github.com/birchill/10ten-ja-reader/releases), [package](https://github.com/birchill/10ten-ja-reader/blob/main/package.json), [yêu cầu tiếng Việt](https://github.com/birchill/10ten-ja-reader/issues/1420).

### Rikaikun

Extension có lịch sử lâu năm, vẫn được cập nhật: popup Nhật–Anh, cách đọc, tên riêng và kanji; phát âm qua Chrome TTS, sao chép kết quả, tùy chỉnh giao diện/phím. Dữ liệu đóng gói trong extension. Engine đọc deinflect.dat để thử biến đổi theo quy tắc rồi tra từ điển. Chưa xác minh có cơ chế nhập từ điển Nhật–Việt tùy ý trong chức năng công bố.

Nguồn: [repo chính thức](https://github.com/melink14/rikaikun), [Chrome](https://chromewebstore.google.com/detail/rikaikun/jipdnfibhldikgcjhfnomkfpcebammhp), [mã tra từ](https://raw.githubusercontent.com/melink14/rikaikun/main/extension/data.ts).

### Jiten + Jiten Reader

Jiten Reader phân tích tiếng Nhật trên trang, popup nghĩa/cách đọc/tần suất/pitch accent, đánh dấu từ biết/chưa biết và thao tác học từ đồng bộ tài khoản. Có phân tích vùng chọn hoặc tự động trên một số trang. Bản dùng dịch vụ chính thức cần tài khoản/API key và gửi nội dung cần phân tích đến API Jiten; đây là khác biệt với yêu cầu đọc ẩn danh hiện tại.

Backend Jiten có API, parser và web, tài liệu phát triển dùng .NET, PostgreSQL và Redis; frontend Nuxt/Vue. Có hướng dẫn tự triển khai. Dự án dùng Sudachi.rs và engine hoàn nguyên có nguồn gốc Nazeka/JL. Đây là ứng viên gần kiến trúc hiện tại hơn, nhưng không có bằng chứng đã đáp ứng mọi yêu cầu tiếng Việt/ngữ pháp của JP Reading Platform V2.

Nguồn: [backend](https://github.com/Sirush/Jiten), [Reader](https://github.com/Sirush/JitenReader), [Chrome](https://chromewebstore.google.com/detail/jiten-reader/fkegmlkjkenojfiplaclhlmncfeooaeo), [hướng dẫn Reader](https://jiten.moe/guides/browser-extension), [phát triển](https://github.com/Sirush/Jiten/blob/master/DEVELOPMENT.md), [engine hoàn nguyên](https://github.com/Sirush/Jiten/blob/master/Jiten.Parser/Deconjugator.cs), [Apache-2.0](https://github.com/Sirush/Jiten/blob/master/LICENSE), [MIT Reader](https://github.com/Sirush/JitenReader/blob/dev/LICENSE.md).

## Khuyến nghị và bước tiếp theo

1. Ưu tiên thử Yomitan nếu mục tiêu trước mắt là một extension tra từ tốt để Việt hóa và phát triển thêm UX/học từ.
2. Khảo sát Jiten song song nếu cần giữ mô hình backend C#/.NET và xây nền tảng web có tài khoản.
3. Trước khi quyết định chuyển nền, so sánh hai lựa chọn trên cùng một tập nội dung web thực tế: từ nguyên dạng, chuỗi chia dài, khẩu ngữ, tên riêng, ruby và vùng chọn có trợ từ. Đo số tra được đúng, độ trễ và cách ứng xử khi không có nghĩa Việt.
4. Tách mục tiêu tra nghĩa từ khỏi yêu cầu giải thích chi tiết cách chia bằng tiếng Việt khi đánh giá sản phẩm; đó là hai tiêu chí độc lập.

Giấy phép mã nguồn và giấy phép dữ liệu từ điển là các phạm vi riêng. Các nhãn giấy phép trong bảng mô tả khai báo của dự án, chưa phải kết luận kiểm toán toàn bộ dependency và dữ liệu để phát hành sản phẩm tùy biến.

## Câu hỏi còn mở

- Chất lượng và quyền phân phối bộ Nhật–Việt phù hợp cho Yomitan hoặc Jiten.
- Khối lượng công việc khi fork extension so với lấy riêng engine/dữ liệu cho backend hiện tại.
- Độ bao phủ và tốc độ trên tập trang web thực tế của người dùng; chưa chạy benchmark trong lần khảo sát này.
- Những yêu cầu ngữ pháp tiếng Việt, đọc ẩn danh và lưu dữ liệu cá nhân cần giữ khi chọn nền mới.
