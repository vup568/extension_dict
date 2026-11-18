# Kế hoạch triển khai cùng đợt 5 nhóm trải nghiệm đọc

Ngày 2026-11-04. Trạng thái: planning; code chưa bắt đầu. Phạm vi/thiết kế tại [SPEC](SPEC.md), chi tiết HTTP tại [contracts](contracts.md).

Kế hoạch code chi tiết: [plan.md](../../../plans/261104-yomitan-reading-experience/plan.md). Tiến độ thực thi T01–T28 cập nhật tại [task.md](../../../plans/261104-yomitan-reading-experience/task.md); file này giữ vai trò tổng quan theo spec.

## Cách thực hiện một đợt

Cùng một milestone và nghiệm thu tổng, nhưng xây theo thứ tự phụ thuộc để có thể xác định lỗi và kiểm thử từng lớp. Không gộp tất cả thành một thay đổi không thể review. Các bước phân tích source, tạo tài liệu và rà soát không đồng nghĩa các tính năng đã hoàn thành.

| Bước | Công việc | Phụ thuộc | Trạng thái |
|---|---|---|---|
| P0 | Chốt source snapshot, defaults, contracts và corpus catalog | Spec 5 nhóm; phản hồi chủ dự án | Tài liệu đang review |
| P1 | Cấu trúc popup, danh sách occurrence, states và settings | P0 | Chưa triển khai |
| P2 | Resize/pin/focus và vòng đời tương tác chung | P1 | Chưa triển khai |
| P3 | Quét chuột bounded + endpoint scan + offset DOM | P2, contract scan | Chưa triển khai |
| P4 | Gap catalog thể chia, corpus phiên bản mới, data/engine chỉ nếu cần | P0; có thể nghiên cứu cùng P1–P3 | Chưa triển khai |
| P5 | Gemini speech adapter, speech endpoint và nút nghe | P1/P2, contract speech, live TTS probe | Chưa triển khai |
| P6 | Kiểm thử chéo đủ 5 nhóm + source/license inventory + đóng gói | P3/P4/P5 | Chưa triển khai |

Ưu tiên làm khung vòng đời trước hover/audio vì các khả năng này đều cần hủy đúng khi đổi từ và đóng popup. Đối chiếu corpus có thể làm riêng nhưng chỉ công bố sau kiểm duyệt. Không dùng Yomitan làm engine client cạnh tranh backend.

## Touchpoints hiện có và thay đổi dự kiến

| Lớp | File/module hiện có | Công việc dự kiến |
|---|---|---|
| UI React | `extension/src/ui/PopupShell.tsx`, `popup.css`, `TranslationPanel.tsx` | Group/active occurrence, phần chi tiết, nhãn nguồn, nút nghe; giữ dịch explicit |
| DOM/lifecycle | `extension/src/browser/popup/startSelectionPopup.ts`, `createPopupView.tsx` | Tách controller scan, resize/pin, hủy đúng mà không làm sai selection |
| Transport | `extension/src/platform/`, `extension/src/background.ts`, `extension/src/api/` | Thêm scan/speech namespaces, contracts, settings validation |
| Application | `src/usecase/Dictionary/`, `src/usecase/Conjugation/` | Tái dùng lookup, occurrence, POS/applicability; port speech/scan mới nếu cần |
| Infrastructure | `src/infra/Adapters/`, tokenizer và repositories hiện có | Adapter Gemini TTS, kiểm audio, config allowlist; không thêm ORM dependency vào Domain |
| HTTP | `src/interface/Endpoints/`, `ApiComposition.cs` | `/api/scan`, `/api/speech`, deadlines/quota; giữ endpoint cũ |
| Dữ liệu | Corpus F09 và source metadata hiện có | Bản catalog/corpus mới riêng, không sửa/xóa bản nghiệm thu cũ |
| Kiểm thử | `tests/unit`, `tests/integration`, `extension/tests` | Unit/contract/Postgres/Sudachi/browser và nghe audio thật |

Tên file mới chi tiết được xác định khi coding; bảng này là nơi thay đổi dự kiến, không giả làm source đã tồn tại.

## Chiến lược tái sử dụng

Yomitan snapshot `d3c54c10264673eca0d66b970ef74cd5c8b048e3` tại `E:/Road To N3/Yomitan`, checkout sạch khi khảo sát. Ghi source/file/hành vi quan sát; viết component và nghiệp vụ phù hợp cấu trúc dự án. Nếu cần lấy code/asset/rule/test thực tế: trước khi ghép phải ghi manifest origin gồm revision, file, phần lấy, thay đổi, dependencies, copyright/license và quyết định sử dụng. Không xóa copyright để gắn nhãn tự viết.

Tài liệu quy hoạch này không sao chép mã hoặc dữ liệu Yomitan. Quyết định giấy phép cho các phần kết hợp cần dựa trên nội dung thực tế và cách phân phối; không mặc định chỉ ghi nguồn là đủ. Cấm lấy nguyên nguồn âm thanh/ảnh/biểu tượng bên ngoài mà chưa kiểm tra quyền riêng.

## Kiểm thử và hồ sơ cần bàn giao

- Unit: DOM window/UTF-16 mapping, matching anchor, settings, response validation, reducer lifecycle, reading identity, audio parser, timeout, corpus negatives.
- Integration: PostgreSQL/Sudachi thật cho scan và POS/applicability; HTTP fake Gemini cho decode/WAV, blocked/429/401/malformed, cancellation, no retry và no secrets. Fake test không chứng minh đọc đúng tiếng Nhật.
- Browser: packaged extension, ruby/emoji/từ lặp, input excluded, Shift/dwell, selection override, resize/pin, Escape, host style, zoom/text dọc/frames và playback state.
- Live audio: ít nhất bộ từ đa cách đọc và từ đồng âm, câu Nhật có kanji/kana/số; nghe và ghi nhận đạt/không đạt bởi người hiểu tiếng Nhật. Không log raw sample người dùng; dùng câu tổng hợp được duyệt.
- Regression: toàn bộ tests chạm code chung; phân tích vẫn có timeout 5 giây, dịch không tự gọi, audio không hủy analysis mới, mọi analysis contract cũ giữ shape/semantics.
- Hiệu năng: ghi mẫu máy/browser/dataset và p95 riêng cho shell, core analysis, scan, TTS. Tách cold startup dictionary khỏi warm request; không xử lý startup/deploy trong đợt này.

Hồ sơ triển khai dự kiến đặt tại `plans/261104-yomitan-reading-experience/evidence/`, theo [kế hoạch code chi tiết](../../../plans/261104-yomitan-reading-experience/plan.md): traceability, test report, performance report, browser limitations, pronunciation review, provenance manifest. Các hồ sơ đó chưa có ở bước planning; không tạo kết quả giả.

## Điều kiện bắt đầu và dừng

- Review bộ SPEC và các default DD01–DD08; đây là review kết quả thiết kế cụ thể, không yêu cầu người dùng phê duyệt từng thao tác nhỏ.
- Trước P5, xác minh key được phép gọi model TTS và quota miễn phí tại AI Studio bằng câu thử tổng hợp. Nếu không dùng được, báo blocker thực tế; không tự chuyển sang trả phí/browser speech hay model khác mà thay đổi ý người dùng.
- Trước thêm forms vào P4, freeze catalog và nguồn đáp án. Không suy luận rằng tất cả transforms Yomitan đều đúng trong mọi ngữ cảnh.
- P6 chưa đạt bất kỳ gate G-A–G-G thì milestone chưa hoàn thành. Có thể rollback code từng phần mà không rollback/xóa dữ liệu từ điển hay corpus cũ.

## Rủi ro chính

Context 64 UTF-16 có thể cắt từ/cấu trúc dài, API có độ trễ, hover có thể gây nhiều request; xử lý bằng opt-in, bounded input, quota, cancellation và trạng thái no-match trung thực. Model TTS và free tier có thể thay đổi hoặc không khả dụng cho key; config server và live gate là bắt buộc. AI có thể đọc sai; chọn reading dictionary và nghe review độc lập, không quảng bá pitch accent chuẩn. Kết hợp 5 nhóm tăng nguy cơ lifecycle conflict; P6 kiểm thử liên nhóm bắt buộc.
