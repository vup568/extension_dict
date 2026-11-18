# YAU — Nghe tiếng Nhật bằng Gemini TTS

Ngày: 2026-11-04. Phiên bản: 0.2. Trạng thái: Draft để review, chưa triển khai. Thuộc [đặc tả tổng](../SPEC.md).

## 1. Context & Goal

Người dùng yêu cầu Gemini tạo giọng đọc AI. Popup cần nút nghe cách đọc của mục từ hoặc đọc vùng văn bản tiếng Nhật do người dùng bôi đen. Dự án hiện có Gemini dịch sang Anh, chưa có audio runtime. Bộ dịch hiện tại không tự sinh âm thanh; phát âm cần adapter TTS và cấu hình riêng.

### Tham khảo và điểm tích hợp

Yomitan revision `d3c54c10264673eca0d66b970ef74cd5c8b048e3`: `ext/js/display/display-audio.js`, `ext/js/media/audio-system.js`. Tham khảo quản lý term/reading, playback hiện hành, nguồn và vòng đời. Đợt này không lấy bản ghi audio hoặc fallback browser TTS từ Yomitan.

Theo [tài liệu TTS Gemini](https://ai.google.dev/gemini-api/docs/speech-generation) và [bảng giá](https://ai.google.dev/gemini-api/docs/pricing) được đọc ngày 2026-11-04, Gemini có các model TTS hỗ trợ tiếng Nhật và có mức miễn phí cho một số model. [Gemini 3.8 Flash-Lite TTS](https://aistudio.google.com/docs/models/gemini-3.8-flash-lite-tts) là ứng viên cấu hình đề xuất; quyền truy cập, quota và định dạng API phải xác minh trên tài khoản thực trước kích hoạt. Không cam kết tài khoản nào cũng có hạn mức hoặc dịch vụ luôn miễn phí.

Điểm tích hợp hiện tại: [PopupShell.tsx](../../../../extension/src/ui/PopupShell.tsx), [translationTransport.ts](../../../../extension/src/platform/translationTransport.ts), [DependencyInjection.cs](../../../../src/infra/DependencyInjection.cs), [TranslationUseCase.cs](../../../../src/usecase/Translation/TranslationUseCase.cs). Tạo port speech riêng khi triển khai; không dùng translation response như audio.

## 2. Actors & Roles

| Actor | Vai trò và quyền |
|---|---|
| Người đọc ẩn danh | Chọn reading hoặc vùng bôi đen và chủ động nhấn Nghe sau disclosure; không cần login. |
| Extension/background | Chỉ phát âm thanh đã xác thực của request current; không giữ API key hay chọn provider URL tùy ý. |
| Backend | Xác minh canonical pair, kiểm quota/deadline, gọi adapter và chuẩn hóa audio. |
| Google Gemini TTS | Provider ngoài tạo giọng AI cho payload tối thiểu theo cấu hình server. |
| Người vận hành/kiểm duyệt | Cấu hình key/model/voice allowlist, xác minh quota và nghe review trước kích hoạt. |

## 3. Functional Requirements

| ID | Hành vi bắt buộc |
|---|---|
| YAU-01 | WHEN người dùng nhấn Nghe sau thông báo dữ liệu gửi tới Google, hệ thống SHALL mới gọi speech. Không autoplay, prefetch hoặc tạo speech khi hover, chọn entry hay mở popup. |
| YAU-02 | WHILE có nhiều reading, hệ thống SHALL cho chọn cách đọc rõ ràng. Request dictionary dùng canonical ID, reading ID và written form ID tương ứng; backend kiểm joint applicability trước khi gọi model. |
| YAU-03 | WHEN đọc mục từ, backend SHALL phát sinh âm thanh từ kana của reading đã xác minh, không đoán reading bằng chữ kanji và không lấy reading của mục đồng âm khác. |
| YAU-04 | WHEN đọc câu, hệ thống SHALL chỉ gửi vùng bôi đen hiện hành, tối đa 1000 đơn vị UTF-16 sau trim; không mở rộng câu quanh con trỏ hoặc gửi bản dịch tiếng Anh. |
| YAU-05 | THE system SHALL ghi “Giọng đọc AI” và thông báo văn bản gửi sang Google, khả năng dùng dữ liệu để cải thiện sản phẩm trong gói miễn phí trước thao tác gửi. Không mô tả audio là bản thu người bản xứ hoặc chuẩn pitch accent. |
| YAU-06 | THE system SHALL giữ key, model/voice allowlist và endpoint Google cố định ở backend. Client không gửi URL/model/voice tùy ý, không chứa key; không redirect, retry tự động hoặc fallback tính phí. |
| YAU-07 | WHEN nhận audio, cả backend và client SHALL kiểm định dạng WAV, ngôn ngữ, dung lượng và correlation trước phát; audio không hợp lệ không được phát. |
| YAU-08 | WHILE speech đang chờ/phát, hệ thống SHALL chỉ có một playback/request current. Double click được coalesce; chuyển mục đọc, selection, đóng popup hoặc rời trang phải hủy và giải phóng audio cũ. |
| YAU-09 | THE system SHALL chỉ giữ audio hiện hành trong RAM tới khi đổi/đóng; replay audio đã có không gọi provider lại. Không lưu DB, disk, lịch sử hoặc cache nhiều người dùng. |
| YAU-10 | IF thiếu key, model không có quyền, hết quota, timeout hoặc provider chặn nội dung, hệ thống SHALL báo trạng thái riêng, giữ kết quả tra/dịch và không tự chuyển sang dịch vụ khác. |
| YAU-11 | BEFORE kích hoạt model/voice, kiểm thử SHALL xác nhận API tài khoản thực và nghe đánh giá câu/từ Nhật; fake provider tests không được tính là đã xác nhận chất lượng audio. |

## 4. Non-functional Requirements

- **Bảo mật:** key/model/voice/endpoint do server kiểm soát; không redirect hoặc retry/fallback tính phí tự động (YAU-06).
- **Riêng tư:** disclosure trước click; chỉ text nguồn đã chọn, không lưu audio/raw text vào DB/disk/history. Audio current chỉ ở RAM (YAU-01/04/05/09).
- **Giới hạn dữ liệu:** decoded ≤3 MiB, WAV mono PCM16, ≤60 giây; wire ≤4 MiB cộng envelope 4 KiB, validate thực tế trước phát (YAU-07).
- **Deadline đề xuất:** provider 25 giây, backend 28, background 30, UI 32; ngoại lệ PERF-004 cần review cùng SPEC, không kéo dài analysis 5 giây.
- **Quota đề xuất:** 10 requests/phút/IP, 2 in-flight/IP; kiểm proxy/hạn mức tài khoản thực. Không cam kết free vô hạn.
- **Chất lượng/độ tin cậy:** live gate và nghe review riêng, không lấy fake tests làm bằng chứng âm thanh đúng; AI có thể sai trọng âm hoặc ngắt nhịp, không chứng nhận pitch accent (YAU-10/11).

## 5. Data Model

| Dữ liệu | Cấu trúc | Vòng đời |
|---|---|---|
| Speech request | interaction_id + source dictionary canonical/reading/form IDs hoặc source selection text | Hai loại nguồn loại trừ nhau; ephemeral |
| Speech response | audio_base64, mime_type=audio/wav, language_tag=ja, source=ai, meta UUID/version | Không persist; validate trước tạo Blob |
| Playback state | idle/loading/playing/stopped/error, request identity, Web Audio buffer/context | Chỉ current, dừng và giải phóng khi đổi/đóng |
| Cấu hình server | Speech:Gemini:ApiKey/Model/Voice, allowlist | Secret môi trường server, không schema dữ liệu học |

### Interface và hợp đồng liên quan

[contracts.md](../contracts.md) định nghĩa mới `POST /api/speech` v1. Input dictionary giải quyết canonical pair ở backend; input selection gửi text hiện hành. Không nhận đồng thời hai loại nguồn. Success có base64 WAV, `mime_type=audio/wav`, `language_tag=ja`, `source=ai` và meta correlation/version.

Audio đã giải mã tối đa 3 MiB, thời lượng tối đa 60 giây, mono PCM16. Wire response tối đa 4 MiB cộng envelope 4 KiB. Client kiểm base64, WAV header và độ dài thực trước khi giải mã trong RAM bằng Web Audio. Backend dùng trực tiếp WAV hợp lệ hoặc đóng WAV cho PCM có thông số xác minh. Câu dưới 1000 UTF-16 vẫn có thể vượt 60 giây. Khi đó báo giới hạn và đề nghị chọn đoạn ngắn hơn. Giải phóng buffer/context khi kết thúc phiên.

Cấu hình đề xuất: `Speech:Gemini:ApiKey`, `Speech:Gemini:Model`, `Speech:Gemini:Voice`; chỉ khi key speech bỏ trống mới dùng key Gemini dịch hiện tại. Model/voice được allowlist theo tài liệu và live gate; tên model ứng viên chưa là lựa chọn đã nghiệm thu.

Deadline đề xuất: provider 25 giây, backend 28 giây, background 30 giây, UI 32 giây. Ngoại lệ mới của PERF-004 phải được review cùng SPEC; không kéo dài deadline analysis 5 giây. Đề xuất quota 10 request/phút/IP và tối đa 2 in-flight/IP trả 429 trước provider; cần xác minh proxy/quota thực khi phát hành. Các số này là thiết kế để review, không là cam kết miễn phí của Google.

## 6. Error Handling

| Trường hợp | Phản hồi và cách phục hồi |
|---|---|
| ID/form/reading không hợp, text quá dài hoặc Unicode sai | 400 trước provider, yêu cầu chọn nguồn hợp lệ. |
| Key/model/voice không dùng được hoặc nội dung bị chặn | SERVICE_UNAVAILABLE, giữ kết quả tra/dịch; không chuyển model trả phí. |
| Hết quota/429 | RATE_LIMITED, chỉ thử lại chủ động khi phù hợp, không retry tự động. |
| Provider/deadline hết | TIMEOUT, kết thúc loading đúng hạn; caller abort được giữ đúng semantics. |
| Base64/MIME/WAV sai, quá dung lượng hoặc 60 giây | Từ chối audio; đề nghị đoạn ngắn hơn khi đúng nguyên nhân, không cắt rồi báo thành công. |
| Đổi mục/selection/đóng hoặc response cũ | Hủy pending, dừng playback và revoke Blob; không phát âm thanh lỗi thời. |

## 7. Acceptance Criteria

| AC | Tình huống và kết quả cần quan sát | Yêu cầu |
|---|---|---|
| YAU-AC01 | Chọn câu/mở popup/hover/đổi entry tạo zero speech request; sau click có disclosure và đúng một request. | YAU-01/05/08 |
| YAU-AC02 | `橋` và `箸` đều có reading はし nhưng ID riêng; request dùng đúng entry đã chọn, không gộp cache theo kana. | YAU-02/03/09 |
| YAU-AC03 | Entry có hai reading giới hạn khác nhau: chọn reading B không gửi written form chỉ hợp A; request sai bị từ chối trước model. | YAU-02/03 |
| YAU-AC04 | Đọc `電気代とガス代に国がお金を出す` gửi nguyên vùng bôi đen tiếng Nhật; không gửi bản dịch Anh hoặc context ngoài selection. | YAU-04 |
| YAU-AC05 | Adapter có thể đóng gói PCM của Gemini nếu MIME và thông số hợp lệ; client chỉ nhận WAV đã normalize. Raw PCM gửi thẳng tới client, base64 sai, WAV thiếu header, quá 3 MiB hoặc quá 60 giây bị từ chối. | YAU-07 |
| YAU-AC06 | A đang tải, người dùng đổi sang B/đóng popup; A về sau không phát. Audio A đang phát phải dừng, buffer/context được giải phóng. | YAU-08/09 |
| YAU-AC07 | 429, timeout, missing key/model unavailable: trạng thái speech đúng, không lặp API tự động, nghĩa từ và dịch hiện hành còn nguyên. | YAU-06/10 |
| YAU-AC08 | Fake transport không trả callback: UI hết loading trong 32 giây; abort của người dùng không đổi thành lỗi provider. | YAU-08/10 |
| YAU-AC09 | Live smoke với khóa lưu kín xác minh quyền model, hạn mức và định dạng. Chủ dự án trực tiếp nghe bộ mẫu tiếng Nhật, đối chiếu cách đọc từ và xác nhận câu đọc đủ chữ, đúng thứ tự. Ghi model, giọng, ngày, hạn mức quan sát được và lỗi theo mẫu. | YAU-05/11 |

Liên hệ: PRIV-001/002/004/006, AUTH-001, ASYNC-001/002, NET-003/006 và ngoại lệ PERF-004 đề xuất. Nút nghe không biến thành thao tác lưu dữ liệu học.

## 8. Out of Scope

Giọng AI có thể phát âm sai kanji, ngắt nhịp hoặc trọng âm; reading dictionary được chỉ định giảm nhầm cách đọc nhưng không chứng nhận pitch accent. Quota/model có thể thay đổi. Không dùng giọng nhân bản, bản ghi người thật, TTS offline, dịch âm thanh trực tiếp hoặc trình phát nền trong đợt này. Khi live gate chưa đạt, trạng thái phát âm chưa nghiệm thu phải ghi rõ trong báo cáo release.
