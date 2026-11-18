# Kế hoạch code: trải nghiệm đọc tham khảo Yomitan

Ngày 2026-11-04. Phiên bản 1.0. Cập nhật 2026-11-10. Đang triển khai, 21/28 việc hoàn thành theo checklist. Xem [báo cáo kiểm chứng](evidence/test-report.md) và [baseline triển khai](evidence/provenance.md).

## 1. Mục tiêu và tài liệu nguồn

Hoàn thiện cùng một milestone gồm popup nội dung, quét từ bằng chuột, độ đúng thể chia, giọng đọc Gemini AI và resize/ghim popup. Giữ backend/PostgreSQL tập trung, extension React mỏng; ưu tiên tham khảo Yomitan và tự viết phù hợp kiến trúc dự án.

- [SPEC tổng v0.2](../../.sdd/specs/005-yomitan-reading-experience/SPEC.md) và [contracts](../../.sdd/specs/005-yomitan-reading-experience/contracts.md) quyết định hành vi, dữ liệu và giới hạn.
- Spec con: [YUI](../../.sdd/specs/005-yomitan-reading-experience/features/popup-content.md), [YSC](../../.sdd/specs/005-yomitan-reading-experience/features/pointer-scanning.md), [YCO](../../.sdd/specs/005-yomitan-reading-experience/features/conjugation-coverage.md), [YAU](../../.sdd/specs/005-yomitan-reading-experience/features/ai-pronunciation.md), [YPO](../../.sdd/specs/005-yomitan-reading-experience/features/popup-behavior.md).
- [task.md](task.md) là checklist thực thi và nơi cập nhật tiến độ T01–T28. Không đánh dấu xong từ việc đã viết tài liệu.
- [Kế hoạch tổng quan trong spec](../../.sdd/specs/005-yomitan-reading-experience/PLAN.md) và [checklist tóm tắt](../../.sdd/specs/005-yomitan-reading-experience/TASKS.md) là tham chiếu phạm vi; chi tiết code/tiến độ theo hai file trong thư mục này. Nếu thay đổi phạm vi phải cập nhật spec trước khi mở rộng task.

## 2. Chuẩn bị trước khi code

| Nội dung | Đầu ra cần có | Khi nào thực sự chặn? |
|---|---|---|
| Review default DD01–DD08 | Ghi quyết định về Shift/dwell, context window, resize, timeout và TTS; sửa tài liệu nếu chủ dự án chọn khác | Chặn phần code phụ thuộc quyết định chưa rõ; không diễn giải yêu cầu tạo kế hoạch là phê duyệt mọi default |
| Kiểm tra working tree/baseline | Ghi thay đổi đã có, source hiện tại và bộ tests liên quan; bảo toàn việc đang làm | Không reset working tree hoặc trộn sửa ngoài phạm vi |
| Reference Yomitan | Pin revision `d3c54c10264673eca0d66b970ef74cd5c8b048e3`, ghi source thực tham khảo | Nếu lấy code/data/asset trực tiếp thì cần manifest license/dependency trước khi ghép |
| Gemini speech | Probe bằng input tổng hợp, key server-side, model/voice/quota thực | Chặn live TTS và nghiệm thu YAU khi không gọi được; các phần UI/scan/corpus vẫn có thể làm |
| Corpus | Gap catalog và đáp án độc lập có phiên bản | Chặn thay rules nếu chưa đủ chứng cứ, không cản việc đối chiếu corpus |

Không thêm dependency tự động. Không đưa key vào chat, markdown, source, log hoặc extension. Không thay DB từ điển candidate thành kho dữ liệu học hoặc chạy lại import để thực hiện đợt UI này.

## 3. Thứ tự triển khai và phụ thuộc

| Giai đoạn | Task | Công việc/đầu ra | Phụ thuộc | Tiêu chí chuyển bước | Trạng thái |
|---|---|---|---|---|---|
| P0 — Baseline và hợp đồng | T01–T03 | Xác nhận thiết kế, source inventory, test hợp đồng scan/speech và compatibility | Spec/contract đọc đầy đủ | Contract sai/đúng có fixture rõ, phạm vi được chốt đủ cho phần code | 3/3 |
| P1 — Nội dung popup | T04–T07 | Danh sách occurrence, chi tiết từ, reading/restrictions, trạng thái độc lập | P0 | YUI-AC01–08 đạt; legacy từ đơn còn chạy | 4/4, smoke Edge với từ điển thật đạt |
| P2 — Vòng đời popup | T08–T10 | Resize, preference, pin, keyboard/focus và cleanup | P1 | YPO-AC01–10 ở component/harness; audio dùng stub/lifecycle spy. E2E speech thật lặp ở P6 sau P5 | 3/3, Chrome/Edge/Brave đạt. Firefox đã build, chưa chạy runtime |
| P3 — Pointer scan | T11–T15 | DOM buffer, endpoint scan, transport và UI target | P0 + P2 | YSC-AC01–09 đạt với Postgres/Sudachi/browser | 5/5, kiểm thử toàn tuyến Edge và hồi quy Chrome đạt |
| P4 — Đối chiếu thể chia | T16–T19 | Catalog, corpus/rules phiên bản mới nếu cần, regression | P0 để nghiên cứu cùng P1–P3; T19 tích hợp scan cần T13 | YCO-AC01–08 đạt và corpus có nguồn/review | 3/4, dataset `candidate-20261110.1` đã qua kiểm từ đơn. Tiếp theo T19 |
| P5 — Phát âm Gemini | T20–T24 | Probe TTS, adapter/endpoint, nút nghe/stop/replay và kiểm âm thanh | P0 + P2; probe T20 có thể làm sớm | YAU-AC01–09 đạt, có nghe review thật ngoài fake tests | 3/5, đã nghe được trong popup, chờ ghi hạn mức và kết quả theo mẫu |
| P6 — Nghiệm thu tích hợp | T25–T28 | Cross-capability, privacy, performance, browser, docs và artifact | P3 + P4 + P5 | G-A–G-G đạt và mọi task có bằng chứng | 0/4, đang có kiểm thử từng phần |

Tách các thay đổi thành lát cắt có thể review và quay lại, nhưng nghiệm thu một bộ đủ 5 nhóm. Nếu một phần bị chặn, ghi rõ vào task và tiếp tục phần độc lập; không gọi toàn milestone hoàn thành chỉ vì feature flag ẩn phần còn thiếu.

P2 chỉ cần contract/adapter stub để kiểm vòng đời âm thanh, không phụ thuộc ngược P5. Live TTS và E2E audio với các khả năng khác phải đạt P5/P6 trước nghiệm thu cuối; stub pass không chứng minh audio thật hoạt động.

## 4. Thiết kế code và nơi thay đổi dự kiến

| Lớp | Module hiện có | Cách triển khai dự kiến |
|---|---|---|
| Nội dung UI | `extension/src/ui/PopupShell.tsx`, `popup.css`, `TranslationPanel.tsx` | Tách component theo trách nhiệm nếu cần; active occurrence dựa span, canonical/form/reading giữ nguyên; không auto dịch/nghe |
| Session/lifecycle | `extension/src/browser/popup/startSelectionPopup.ts`, `createPopupView.tsx`, `interaction/analysisController.ts` | Một current popup/frame; phân biệt pending scan với committed result, hủy theo capability và phiên current |
| Quét DOM | Ruby-safe extraction và detection trong `extension/src/browser/selection/`, `core/` | Module pointer scanner/window mapper mới được đề xuất; không sửa selection extractor thành đọc cả trang |
| Background/API client | `extension/src/platform/`, `api/`, `background.ts` | Namespace riêng scan/speech, response validation, tab/frame ownership, deadline và AbortController |
| Settings/build | `extension/build-extension.mjs` và popup adapter | Preference schema/version, storage tối thiểu nếu cần; không thêm Google host permission ở extension |
| Scan use case | `src/usecase/Dictionary/SentenceVocabularyUseCase.cs`, `LookupWordUseCase.cs` | Use case scan mới được đề xuất, reuse lookup/occurrence/POS; không engine NLP phía client |
| Speech use case | Pattern `src/usecase/Translation/TranslationUseCase.cs` | Port/use case speech mới, kiểm source dictionary/selection và reading pair; không đổi dịch câu thành API âm thanh |
| Infra/provider | `src/infra/Adapters/`, `DependencyInjection.cs` | Gemini speech adapter mới và WAV validation; model/voice allowlist, no redirect/retry; không dùng secret client |
| HTTP | `src/interface/Endpoints/`, `ApiComposition.cs` | Thêm `/api/scan`, `/api/speech` v1, payload cap cả chunked, error/quota/deadline theo contracts |
| Ngôn ngữ | `src/usecase/Conjugation/`, sidecar adapter và corpus F09 | Quy tắc versioned; chỉ đổi engine nếu catalog chứng minh cần; exact-first/applicability còn nguyên |

Tên module mới là đề xuất khi coding, không giả định các file đó đã tồn tại. Kiến trúc vẫn theo Domain ← Application ← Infrastructure ← Interface. API/DTO không được tự đổi khác contracts để thuận tiện cho UI.

## 5. Ràng buộc dễ làm sai cần kiểm trước mỗi lát cắt

- Giữ occurrence lặp, canonical ID, form/reading/sense restrictions; không chọn nghĩa đúng bằng thứ tự. Span occurrence trên normalized text, span con trên surface.
- Hover chỉ khi opt-in + Shift/dwell, tối đa 64 UTF-16; bỏ ruby annotation, không split surrogate, không đọc editable/password/DOM không có quyền. Nhả Shift hủy pending scan nhưng giữ result đã commit; pin hoặc vào popup giữ dịch/audio current.
- `target_span` bằng đúng một occurrence và chứa offset. Capability failed/unavailable/truncated không được biến thành kết luận không có từ.
- Không request dịch/TTS tự động; phát âm từ theo reading đã chọn, câu chỉ theo vùng bôi đen. Không đọc hover context âm thầm.
- Audio endpoint WAV hợp lệ ≤3 MiB/60 giây, không cắt rồi báo thành công, không lưu history/audio. Provider/model/voice/key là config server, không fallback tính phí.
- Đề xuất deadline/quota phải khớp contracts ở mọi lớp; analysis luôn 5 giây. Resize blur chỉ kết thúc resize; thay interaction/close/pagehide mới cleanup request/audio tương ứng.

## 6. Kiểm chứng

Thực hiện test thích hợp ở từng task; khi chia sẻ module/API thì chạy thêm regression của caller. Sau sửa mới hoặc lỗi mới mới cần lặp lại các kiểm tra đã đạt.

| Nhóm | Kiểm chứng bắt buộc | Loại bằng chứng |
|---|---|---|
| Contract/state | Validation, correlation, body caps, quota, timeout, ignored abort, cleanup | Unit + endpoint fake provider, không dùng fake để chứng nhận phát âm |
| Từ/ngôn ngữ | Unicode/ruby, từ lặp/đồng âm, POS/sense pair, exact-first, corpus cũ/mới | Unit + PostgreSQL/Sudachi integration |
| Popup/scan | Keyboard, focus, viewport/zoom, frame, resize/pin, Shift/dwell, host interaction | Browser harness và extension đóng gói |
| Speech | PCM/WAV đúng/sai, payload lớn, 429/timeout, replay, key không lộ | Unit/integration + nghe live bộ mẫu được duyệt |
| Chung | Không gửi dịch/audio khi chưa click; no selected text/audio persistence; p95 profile | Network/log/storage inspection + performance report |

Các lệnh dự kiến từ root, dùng dependencies đã có:

```powershell
dotnet test tests/unit/UnitTests.csproj --no-restore
dotnet test tests/integration/IntegrationTests.csproj --no-restore
npm --prefix extension run typecheck
npm --prefix extension test
npm --prefix extension run build
```

Integration cần Docker; chỉ dùng DB kiểm thử phù hợp và không reset DB người dùng. Playwright chạy project/browser đã cài, không tự tải browser; test file mới lựa chọn theo task. Browser chưa có/runtime chưa chạy phải ghi chưa xác minh. Chất lượng giọng TTS cần người nghe hiểu tiếng Nhật; không lấy HTTP 200 làm tiêu chí duy nhất.

## 7. Tiến độ, bàn giao và điều kiện hoàn thành

Mỗi Txx trong [task.md](task.md) chỉ tick khi output + test + bằng chứng của task đạt. File này chỉ tổng hợp giai đoạn để tránh hai checklist tiến độ độc lập. Kết quả khi coding lưu dưới `evidence/` của thư mục kế hoạch này, tạo khi có bằng chứng thực:

| Artifact dự kiến | Nội dung |
|---|---|
| `evidence/traceability.md` | Map đủ 50 requirements/44 AC và G-A–G-G tới code/test/kết quả |
| `evidence/test-report.md` | Test commands, pass/fail, backend/Sudachi/browser profile và giới hạn |
| `evidence/performance-report.md` | p95 cold/warm phân biệt, scan/TTS/core analysis riêng |
| `evidence/pronunciation-review.md` | Model/voice/quota/date, corpus tổng hợp, nghe đúng/sai và hạn chế |
| `evidence/provenance.md` | File Yomitan tham khảo/copy, revision/license/dependencies và thay đổi |
| `evidence/privacy-review.md` | Payload/log/storage/cancellation kiểm thực tế, không đưa secrets vào report |

Chỉ hoàn thành khi đủ 5 nhóm, mọi AC và gate đạt, package dùng được, docs cấu hình chính xác, không còn lỗi chặn và giới hạn kiểm thử được công bố. Đợt này không bao gồm deploy, tối ưu startup, web học/auth/lưu từ, offline dictionary, OCR, Anki, pitch accent hoặc dataset ngoài spec.
