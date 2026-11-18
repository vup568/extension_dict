# Hợp đồng đề xuất cho đợt trải nghiệm đọc

Ngày 2026-11-04. Cập nhật 2026-11-08: scan và speech v1 đã có triển khai và kiểm thử với fixtures; trạng thái nghiệm thu theo [kế hoạch triển khai](../../../plans/261104-yomitan-reading-experience/plan.md). Quy tắc riêng có trong [SPEC](SPEC.md) và các [spec tính năng](features/pointer-scanning.md).

## Tương thích và span

Giữ `/api/analysis` versions 1/2/3. Selection tối đa 1000 đơn vị UTF-16 sau trim. `vocabulary_occurrences[].span` tính trên `normalized_text`; conjugation lồng trong occurrence tính tương đối `surface` của occurrence. Không dịch chuyển span chỉ vì UI đổi cách nhóm kết quả.

Mọi endpoint mới nhận UUID dạng D, không nhận ID tùy ý; response meta echo interaction_id và tạo request_id. Clients validate UUID/correlation/length/span/language/mime trước render. Lỗi dùng `{error_code,message,request_id,retryable}` không trả lỗi thô từ provider, text hoặc khóa. `Cache-Control: no-store` cho scan và speech; request/response body không được ghi log.

## POST /api/scan — contract 1

Header `X-Contract-Version: 1`. Body:

```json
{"text":"私は日本語を勉強しています。","offset":3,"interaction_id":"a1d3b846-c077-4b5a-8b97-1d4d583ab0ad"}
```

`text` là cửa sổ DOM bounded, tối đa 64 UTF-16; offset trỏ vào một code point Japanese, `0 <= offset < text.length`, không giữa surrogate. FE trim cửa sổ trước gửi và tính lại offset; backend không trim thêm âm thầm. Server reject đầu/cuối whitespace chưa normalize hoặc offset không hợp lệ, body thừa giới hạn 4 KiB; kiểm tra cả chunked stream. Không có URL trang, title, cookies, context ngoài cửa sổ hoặc credentials.

Response 200: `data = {analysis: <AnalysisResultDto hình dạng v3>, target_span: <{start,end,unit:"utf16"}> | null}`, `meta = {request_id,interaction_id,contract_version:"1"}`. `analysis.normalized_text` bằng chính text request. `target_span` phải trùng chính xác span của một `analysis.vocabulary_occurrences` và bao trùm offset (`start <= offset < end`); client kiểm cả equality, containment và surface substring trước dùng. Kết quả target không tìm được => null, không tự chuyển sang từ ở bên cạnh. Nếu trùng khả năng, ưu tiên occurrence dài nhất chứa điểm, sau đó start nhỏ nhất; giữ mọi canonical matches/giải thích thuộc occurrence đó.

Backend dùng tokenizer và SentenceVocabularyUseCase hiện có cho cửa sổ, không thêm suy luận NLP ở FE; nếu cả cửa sổ là từ đơn hợp lệ, wrap kết quả lookup vào occurrence span toàn từ. Giữ exact-first của word lookup; không cắt một kanji bên trong từ ghép để giả vờ đã tìm đúng từ. Các giới hạn/truncated/unavailable của analysis vẫn hiển thị được. Khi chưa có word target, phần kanji tại điểm có thể dùng kết quả kanji đã có, nhãn rõ kanji, không đổi target thành một từ khác.

Timeout analysis/scan: 5 giây ở client, hủy best effort; request cũ không cập nhật UI. Tối đa một request scan in-flight/frame, khoảng dispatch tối thiểu 300 ms, quét cùng cửa sổ/offset hiện hành không gửi lại. Backend đề xuất scan quota 60 request/phút/IP, không queue; 429 có `Retry-After` và `retry_after_seconds`. Đây là quota riêng dự kiến cho bản thử server, phải đo lại khi phát hành nhiều người dùng sau proxy đúng cấu hình.

## POST /api/speech — contract 1

Header `X-Contract-Version: 1`. Hai dạng input loại trừ nhau:

```json
{"interaction_id":"a1d3b846-c077-4b5a-8b97-1d4d583ab0ad","source":{"kind":"dictionary","canonical_id":"dictionary:jmdict:1358280","reading_id":123,"written_form_id":456}}
```

Các số ID chỉ minh họa; phải dùng ID thật trong response. `reading_id` bắt buộc; written_form_id có thể bỏ nếu là kana-only. Backend resolve canonical ID và kiểm tra joint applicability trước khi lấy kana đọc; ID/form/reading không thuộc nhau => 400, không gọi Gemini.

```json
{"interaction_id":"a1d3b846-c077-4b5a-8b97-1d4d583ab0ad","source":{"kind":"selection","text":"私は日本語を勉強しています。"}}
```

Text selection 1–1000 UTF-16 sau trim, Unicode hợp lệ. Chỉ nhận source hiện hành do người dùng chọn đọc; dictionary text giải quyết trên server ≤255 UTF-16. Body giới hạn 16 KiB cả Content-Length và chunked, reject trước gọi provider. Không nhận provider URL, model, voice tùy ý hoặc instruction từ client.

Response 200:

```json
{"data":{"audio_base64":"<WAV base64>","mime_type":"audio/wav","language_tag":"ja","source":"ai"},"meta":{"request_id":"<UUID>","interaction_id":"<UUID>","contract_version":"1"}}
```

Backend chuẩn hóa Gemini output thành WAV mono PCM16 hợp lệ. Audio đã giải mã ≤3 MiB và ≤60 giây, response wire ≤4 MiB + envelope 4 KiB. Kiểm tra RIFF/header/sample rate/channels/data lengths. Phiên bản 1 nhận một WAV hoàn chỉnh. FE kiểm kích thước/base64/header rồi giải mã bằng Web Audio trong RAM để hoạt động trên trang chặn media URL. Khi thay hoặc đóng popup, FE dừng nguồn phát và giải phóng audio buffer/context. Audio chỉ tồn tại trong bộ nhớ phiên.

Timeout đề xuất: provider 25 giây, handler deadline 28 giây, background 30 giây, UI 32 giây; mọi lớp đều kết thúc loading đúng hạn kể cả transport không trả callback. Đây là ngoại lệ mới của PERF-004 để review cùng SPEC; không đổi timeout analysis. 504 TIMEOUT khi provider/deadline hết, 429 RATE_LIMITED khi quota cạn, 503 SERVICE_UNAVAILABLE khi key/model/voice không dùng được hoặc audio sai; `retryable` đúng nguyên nhân. Không retry tự động hoặc fallback dịch vụ tính phí. Caller cancellation không bị đổi thành lỗi provider.

Khóa chỉ ở backend. Tên cấu hình đề xuất: `Speech:Gemini:ApiKey` (nếu bỏ trống mới dùng key dịch Gemini hiện có), `Speech:Gemini:Model`, `Speech:Gemini:Voice`; model/voice allowlist và host Google chính thức cố định, không redirect. Kiểm tra tài liệu provider trước khi coding giao thức; model TTS tách riêng model dịch. Không thay model dịch đang chạy.

Đề xuất quota speech local/server thử: 10 requests/phút/IP, tối đa 2 in-flight/IP; vượt trả 429 trước provider. UI một audio request hiện hành, coalesce double click; một bản âm thanh đã tải chỉ replay trong phiên không gọi provider thêm. Không cache raw câu/âm thanh nhiều người dùng; hạn mức miễn phí của Google xác minh riêng trong tài khoản thực.

## Background và settings

Scan, analysis, translation và speech có message namespace/cancel key riêng, ràng buộc extension ID + tab + frame + interaction UUID. Chỉ background gọi backend; không thêm arbitrary fetch bridge từ trang. Bắt stale theo cả phiên popup lẫn mục đọc, không chỉ canonical ID vì cùng từ có thể xuất hiện lặp.

Settings v1 đề xuất: `scan.enabled=false`, `scan.modifier=Shift`, `popup.width=380`, `popup.height=480`. Clamp size như spec YPO, chỉ lưu những preference này qua extension storage, không giữ text/history/token/API key. Manifest chỉ thêm quyền storage nếu implementation thực sự cần; không thêm quyền host âm thanh/Google vì request do backend thực hiện.
