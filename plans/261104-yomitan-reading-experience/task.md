# Công việc triển khai trải nghiệm đọc tham khảo Yomitan

Ngày 2026-11-04. Cập nhật 2026-11-10. Đang triển khai. Xem [báo cáo kiểm chứng](evidence/test-report.md) để phân biệt các ca đã đạt với các gate còn mở.

Đọc cùng [plan.md](plan.md), [SPEC tổng](../../.sdd/specs/005-yomitan-reading-experience/SPEC.md) và [contracts](../../.sdd/specs/005-yomitan-reading-experience/contracts.md). Đây là nguồn tiến độ triển khai hiện hành. Các ID T01–T28 kế thừa [checklist baseline trong bộ SPEC](../../.sdd/specs/005-yomitan-reading-experience/TASKS.md); chỉ cập nhật tiến độ tại file này để tránh hai checklist khác nhau. File này bổ sung phụ thuộc, đầu ra, touchpoints và bằng chứng để thực hiện. Checkbox thể hiện code/kiểm chứng, không tick chỉ vì đã viết tài liệu.

## Quy tắc thực hiện

- Mọi đường dẫn file mới nêu dưới đây là **đề xuất**, chỉ tạo khi phù hợp cấu trúc thực tế. Các module hiện có có thể được mở rộng thay vì thêm lớp không cần thiết.
- Tái sử dụng backend/PostgreSQL/Sudachi và React hiện có. Tham khảo Yomitan; nếu lấy code/rule/test/asset thực tế phải ghi nguồn và giấy phép trước tích hợp.
- Không reset database, xóa migration, sửa source lock, overwrite corpus đã nghiệm thu hoặc đưa key vào tài liệu/client/log. Bảo toàn các thay đổi hiện có của người dùng.
- Khi một phần bị chặn do quyền model, quota hoặc thiếu key TTS, tiếp tục các phần UI/scan/corpus và fake provider tests độc lập. T20 và phần nghe live của T24 là điều kiện nghiệm thu audio; không tự chuyển trả phí hoặc dùng fallback khác.
- Chỉ đánh dấu task xong khi có đầu ra và bằng chứng được liệt kê. Không coi test dùng provider giả là kiểm chứng giọng đọc thật.

## P0 — Nền tảng và hợp đồng

- [x] **T01 — Review thiết kế và xác định baseline thực hiện**
  - **Phụ thuộc:** không có.
  - **Touchpoints:** SPEC tổng 8 mục, 5 spec con, contracts; các baseline F02, F03/F04, F09 và analysis/translation hiện có.
  - **Đầu ra:** bản ghi baseline gồm defaults DD01–DD08, các điểm còn đề xuất và thay đổi sau review; không biến scope đã chốt thành yêu cầu phê duyệt lại. Ghi rõ selection/hover, timeout analysis và ngoại lệ speech.
  - **Kiểm chứng:** mọi nhóm có actor, FR/NFR, data/error, AC/out-of-scope; trace tới **G-A**, không có yêu cầu mới chưa được đánh dấu đề xuất.

- [x] **T02 — Lập manifest nguồn và chiến lược tái sử dụng**
  - **Phụ thuộc:** T01.
  - **Touchpoints:** checkout tham khảo Yomitan, các đường dẫn source đã pin trong 5 spec, PLAN chiến lược tái sử dụng. File mới đề xuất: `evidence/provenance.md` trong thư mục kế hoạch này.
  - **Đầu ra:** revision/file/hành vi tham khảo, lý do tự viết hoặc lấy lại; mọi phần lấy thực tế có phần sửa, copyright/license, dependency và điều kiện phân phối tương ứng. Không lấy nguồn âm thanh ngoài chỉ vì Yomitan có adapter.
  - **Kiểm chứng:** kiểm từng file/rule/test/asset lấy lại; không gắn nhãn tự viết nếu đã copy/adapt. Đáp ứng **G-D**, **YCO-AC01** về nguồn tham khảo không phải đáp án.

- [x] **T03 — Khóa hợp đồng và lập kiểm thử ranh giới**
  - **Phụ thuộc:** T01, T02.
  - **Touchpoints:** `extension/src/api/analysisContract.ts`, `src/interface/Endpoints/`, `tests/integration/`, `extension/tests/unit/`. File mới đề xuất: `scanContract.ts`, `speechContract.ts` và các bộ test tương ứng.
  - **Đầu ra:** fixtures và contract tests cho `/api/scan` v1, `/api/speech` v1; xác định input/output/error, body limit cả chunked, UUID, offsets, MIME, quota và cancellation. Giữ analysis v1/v2/v3 và translation hiện hành.
  - **Kiểm chứng:** **YSC-AC08**, **YAU-AC03/05/07/08**, **YUI-AC08**, **G-C/G-D**. Test hành vi mới có thể chưa đạt trước implementation; task chỉ xong khi hợp đồng và kỳ vọng test rõ, không che failure bằng fixture giả success.

## P1 — Nội dung popup

- [x] **T04 — Tạo bố cục câu, danh sách occurrence và chi tiết hiện hành**
  - **Phụ thuộc:** T03.
  - **Touchpoints:** `extension/src/ui/PopupShell.tsx`, `popup.css`, `extension/src/api/analysisContract.ts`; component mới nếu cần là đề xuất.
  - **Đầu ra:** danh sách theo thứ tự xuất hiện; identity dùng span/canonical pair; chọn mục đầu có kết quả hoặc target scan; không lặp heading giống nhau; từ đơn không có occurrences vẫn hiển thị.
  - **Kiểm chứng:** **YUI-AC01/02/08**; `猫、猫。` giữ hai mục riêng, `電気代` không lặp tiêu đề.

- [x] **T05 — Giữ đầy đủ nghĩa, reading và applicability**
  - **Phụ thuộc:** T04.
  - **Touchpoints:** UI từ vựng trong `PopupShell.tsx`, DTO `EntryMatch`/reading/sense và backend lookup hiện có.
  - **Đầu ra:** xem mọi mục đồng âm/alternative, giới hạn nghĩa và nguồn sẵn có; chọn reading tường minh để dùng cho speech về sau; phân biệt surface và base form, không dựng explanation từ lemma-only.
  - **Kiểm chứng:** **YUI-AC03/04**, **YAU-AC02/03**; fixture cặp viết–đọc A/B không lẫn, không thêm ruby khi chưa có ánh xạ được xác minh.

- [x] **T06 — Thu gọn nội dung và trạng thái từng khả năng**
  - **Phụ thuộc:** T04, T05.
  - **Touchpoints:** `PopupShell.tsx`, `TranslationPanel.tsx`, `popup.css`.
  - **Đầu ra:** các mục nghĩa/Kanji/Ngữ pháp/thể chia mở bằng bàn phím; Dịch câu thu gọn và chỉ nút Dịch gửi API; completed-empty, unavailable, failed và truncated khác nhau.
  - **Kiểm chứng:** **YUI-AC05/06/07**; mở/đóng mục dịch không gửi request, lỗi một phần không làm mất phần thành công.

- [x] **T07 — Kiểm thử UI và tương thích kết quả cũ**
  - **Phụ thuộc:** T06.
  - **Touchpoints:** `extension/tests/unit/`, `extension/tests/browser/`, packaged smoke có sẵn.
  - **Đầu ra:** fixtures/báo cáo về từ lặp, đồng âm, từ đơn, zero vocabulary, partial/error, text an toàn và translation độc lập. Đối chiếu một bộ response backend thực với UI.
  - **Kiểm chứng:** toàn bộ **YUI-AC01–YUI-AC08**, regression analysis contract; mã HTML trong dữ liệu không được thực thi.

## P2 — Hành vi popup

- [x] **T08 — Resize responsive và lưu preference hợp lệ**
  - **Phụ thuộc:** T04.
  - **Touchpoints:** `extension/src/browser/popup/createPopupView.tsx`, `PopupShell.tsx`, `popup.css`, `extension/build-extension.mjs`. Module settings mới là đề xuất.
  - **Đầu ra:** kích thước mặc định và giới hạn theo SPEC; pointer/keyboard resize; clamp viewport; chỉ persist width/height và các preference được cho phép. Chỉ thêm quyền storage nếu implementation cần.
  - **Kiểm chứng:** **YPO-AC02/03/06/07**; preference sai hoặc màn hình hẹp không làm nút đóng mất truy cập.

- [x] **T09 — Pin và quản lý vòng đời interaction chung**
  - **Phụ thuộc:** T08.
  - **Touchpoints:** `startSelectionPopup.ts`, `createPopupView.tsx`, `extension/src/interaction/analysisController.ts`; lớp state dùng chung mới chỉ khi cần.
  - **Đầu ra:** pin trong phiên, vào popup không requery; nhả Shift hủy pending scan nhưng giữ kết quả đã commit; selection mới/Escape thay hoặc đóng và hủy đúng namespace. Có hook stop/release speech để nối tại T23.
  - **Kiểm chứng:** **YPO-AC04/05/08/10**, **YSC-AC09**; ở bước này dùng fake audio/dịch để kiểm vòng đời, T23/T25 xác nhận tích hợp thật.

- [x] **T10 — Kiểm focus, viewport, pointer cleanup và browser**
  - **Phụ thuộc:** T09.
  - **Touchpoints:** browser popup tests, `createPopupView.tsx`, `startSelectionPopup.ts`.
  - **Đầu ra:** báo cáo top-layer/fallback, host CSS, focus return, blur/cancel, scroll/zoom/text dọc và frame được phép; ghi browser chưa chạy.
  - **Kiểm chứng:** toàn bộ **YPO-AC01–YPO-AC10** trên fixtures thích hợp; ở P2 dùng stub/lifecycle spies cho audio tại AC08/10, không chờ speech thật để hoàn thành nền tảng resize/pin. T25 chạy lại các ca này bằng tích hợp audio sau P5. Resize không tạo lookup hoặc phá selection/trang.
  - **Bằng chứng:** [Báo cáo T10](evidence/test-report.md#t10--kiểm-chứng-vòng-đời-popup-ngày-10112026) ghi 134 ca Playwright đạt trên Chrome, Edge và Brave. Ca MV3 đóng gói chạy trên Edge và Brave. Firefox chưa xác minh runtime.

## P3 — Tra từ bằng chuột

- [x] **T11 — Bật quét chủ động và bộ lọc nguồn DOM**
  - **Phụ thuộc:** T09.
  - **Touchpoints:** `startSelectionPopup.ts`, `extension/src/browser/selection/`, module settings; module pointer scanner mới là đề xuất.
  - **Đầu ra:** mặc định tắt, bật có thông báo bounded context; Shift+dwell theo SPEC, loại input/textarea/password/editable/popup và nguồn không truy cập được.
  - **Kiểm chứng:** **YSC-AC01/04**; rê qua 100 điểm khi chưa bật hoặc không Shift tạo zero scan request.

- [x] **T12 — Map điểm DOM và cửa sổ UTF-16 giới hạn**
  - **Phụ thuộc:** T11.
  - **Touchpoints:** `extractRubySafeSelection.ts` và helpers DOM hiện có để tham khảo; module lấy cửa sổ con trỏ mới là đề xuất, không đổi semantics selection-only.
  - **Đầu ra:** buffer tối đa 64 UTF-16 trong cùng block, 16 trước/48 từ điểm, loại rt/rp; map sau trim, giữ surrogate; không tự mở rộng cả câu/trang.
  - **Kiểm chứng:** **YSC-AC02/03/04/08**, có emoji/kanji ngoài BMP và điểm nằm giữa từ ghép.

- [x] **T13 — Endpoint scan tái sử dụng backend**
  - **Phụ thuộc:** T03.
  - **Touchpoints:** `src/usecase/Dictionary/SentenceVocabularyUseCase.cs`, `LookupWordUseCase.cs`, `src/interface/Endpoints/`, `ApiComposition.cs`; use case/endpoint scan mới là đề xuất.
  - **Đầu ra:** `/api/scan` kiểm input và trả analysis v3 với target_span đúng bằng occurrence chứa offset; wrap từ đơn khi phù hợp, null khi không có target; giữ nghĩa trạng thái và exact-first.
  - **Kiểm chứng:** **YSC-AC02/07/08**, **YUI-AC08**; PostgreSQL/Sudachi xác minh `日本語`, từ chia và no-match, không trả từ bên cạnh.

- [x] **T14 — Scan transport, cancellation, dedupe và quota**
  - **Phụ thuộc:** T12, T13.
  - **Touchpoints:** `extension/src/platform/`, `extension/src/background.ts`, API validators, HTTP scan handler; transport/namespace scan mới là đề xuất.
  - **Đầu ra:** ràng buộc extension/tab/frame/UUID, validate target equality+containment, một scan in-flight/frame và nhịp tối thiểu300ms; quota/deadline theo contracts; selection ưu tiên, pin bỏ scan mới.
  - **Kiểm chứng:** **YSC-AC05/06/07/08/09**, fake ignored-abort/timeout/429; scan không tự gọi translation/speech.

- [x] **T15 — Kiểm thử quét từ toàn tuyến**
  - **Phụ thuộc:** T14.
  - **Touchpoints:** backend unit/integration tests, `extension/tests/unit`, `extension/tests/browser`, packaged extension tests.
  - **Đầu ra:** kết quả unit+PostgreSQL/Sudachi+browser cho điểm giữa từ/cụm đã chia, ruby/emoji, rê nhanh, nhả Shift, vào popup/pin và selection override.
  - **Kiểm chứng:** toàn bộ **YSC-AC01–YSC-AC09**, **G-B/G-C**; kiểm payload thực không có URL hay context ngoài buffer.

## P4 — Độ đúng thể chia

- [x] **T16 — Lập và freeze gap catalog có đáp án độc lập**
  - **Phụ thuộc:** T01, T02.
  - **Touchpoints:** `.sdd/specs/feat-conjugation-explanation/`, YCO spec; catalog/version mới dưới spec005 là đề xuất.
  - **Đầu ra:** từng ô hỗ trợ/thiếu/loại trừ cho động từ, tính từ, copula và chuỗi trong câu; input/base/POS/phân tích nhận-từ chối, nguồn và review. Yomitan chỉ gợi ý trường hợp khó.
  - **Kiểm chứng:** **YCO-AC01**, đối chiếu danh mục bắt buộc; không lấy output Yomitan hoặc engine làm đáp án duy nhất.
  - **Bằng chứng:** [Danh mục `yco-gap-20261110.1`](evidence/conjugation-gap-catalog.md), đã rà soát độc lập, xác minh ID từ điển và span UTF‑16. [Kiểm chứng baseline](evidence/test-report.md).

- [x] **T17 — Tạo corpus phiên bản mới**
  - **Phụ thuộc:** T16.
  - **Touchpoints:** pipeline/corpus F09 hiện có; file corpus và manifest version mới là đề xuất, giữ bản cũ.
  - **Đầu ra:** ca dương/âm cho từng ô đã freeze, alternatives có căn cứ, ranh giới word/sentence, bất quy tắc và restrictions; đáp án được review trước implementation rules.
  - **Kiểm chứng:** **YCO-AC01/02/03/04/06/07**; schema/canonical identities hợp lệ, không overwrite corpus nghiệm thu.
  - **Bằng chứng:** [Corpus `yco-20261110.1`](../../.sdd/specs/005-yomitan-reading-experience/corpus/yco-20261110.1/README.md) có 21 ca cho 9 nhóm. Đã rà soát độc lập, kiểm 6 mục JMdict đã pin và xác minh đủ 12 hash F‑09. Các kỳ vọng cho dataset mới sẽ chạy ở T18/T19.

- [x] **T18 — Bổ sung dữ liệu quy tắc và xác minh ứng viên**
  - **Phụ thuộc:** T17.
  - **Touchpoints:** versioned dataset/pipeline F09, `src/usecase/Conjugation/`, `LookupWordUseCase.cs`, tokenizer adapter hiện có.
  - **Đầu ra:** tri thức dạng dữ liệu có version; engine chỉ sửa nếu có căn cứ. Xác minh JMdict/POS/joint applicability và exact-first; giữ mọi alternative hợp lệ, không hardcode thêm bảng hậu tố vào C#.
  - **Kiểm chứng:** **YCO-AC02/03/04/07**; rejected candidates không trộn reading/form/meaning và không tự gán một nghĩa chắc chắn.
  - **Bằng chứng:** `candidate-20261110.1` thêm bốn paradigm `て/で + しまう` và qua 9 ca adapter/lookup từ corpus T17. [Báo cáo T18](evidence/test-report.md) ghi phép kiểm F‑09 và quan sát token từ sidecar Sudachi đã pin. Kích hoạt và kiểm toàn tuyến thuộc T19.

- [x] **T19 — Nghiệm thu corpus và tích hợp câu/scan**
  - **Phụ thuộc:** T18, T13.
  - **Touchpoints:** regression F09, sentence/scan use cases, backend integration and contract tests.
  - **Đầu ra:** so sánh corpus cũ/mới, lemma-only fallback, UTF-16 nested spans, candidate budget/cancellation/latency và đường quay lại dataset đã xác minh.
  - **Kiểm chứng:** toàn bộ **YCO-AC01–YCO-AC08**, **G-C**; không kích hoạt dataset chưa qua validation hoặc lấy số lượng test làm độ phủ mọi tiếng Nhật.
  - **Tiến độ 18/11:** đã cài bằng migration tiến `AddYcoConjugationCandidate`; 21 ca YCO qua `/api/analysis` và `/api/scan` với PostgreSQL và Sudachi thật đạt trên cả hai pin; 404 ca F‑09 và hợp đồng 1/2/3 đạt với pin mới. [Báo cáo T19](evidence/test-report.md). Còn kiểm popup trên DB local với pin mới và quyết định đổi pin mặc định trước khi tick.
  - **Tiến độ 23/11:** pin mặc định đã đổi sang `candidate-20261110.1` (commit `ceb7376`). DB demo tạo mới bằng bản build 18/11 có migration YCO; smoke demo đạt với pin mới. **Chặn:** DB thật `jp_dict_real_jaen_20261030_v3` thiếu migration `20261118075656_AddYcoConjugationCandidate`, mà xác minh DB ứng viên đòi lịch sử migration khớp chính xác và không tự migrate, nên code hiện tại không khởi động được trên DB thật với bất kỳ pin nào. Cần chủ dự án chọn cách đưa migration vào DB ứng viên trước khi tick. [Báo cáo 23/11](evidence/test-report.md#t19--db-thật-thiếu-migration-yco-ngày-23112026)
  - **Hoàn tất 23/11:** chủ dự án chọn thêm lệnh `--dictionary-import upgrade` (spec 002 FR-015). DB thật được sao lưu rồi nâng cấp; facts hash giữ nguyên `168480cd…`. Backend hiện tại qua xác minh với `candidate-20261110.1`, giải thích `食べてしまった → 食べる` bằng `verb-te-shimau:plain-past`, và 3 smoke từ điển thật đạt. [Báo cáo nâng cấp](evidence/test-report.md#t19--nâng-cấp-db-thật-và-nghiệm-thu-ngày-23112026)

## P5 — Phát âm AI Gemini

- [ ] **T20 — Probe quyền model, quota và giọng Nhật thực tế**
  - **Phụ thuộc:** T01.
  - **Touchpoints:** cấu hình server và tài liệu Gemini TTS; script probe mới chỉ nếu cần, không lưu key vào source.
  - **Đầu ra:** xác minh model/voice được allowlist và API key thực có quyền/hạn mức; gọi câu tổng hợp ngắn, ghi ngày/model/voice/trạng thái không chứa key/raw text người dùng. Model trong SPEC vẫn là ứng viên cho đến xác minh.
  - **Kiểm chứng:** **YAU-AC09**, **G-F**. Không dùng được thì ghi blocker; không cản T04–T19 hoặc fake-adapter work, không tự chuyển trả phí hay giọng browser.

- [x] **T21 — Port và adapter TTS kiểm soát định dạng**
  - **Phụ thuộc:** T03; T20 là gate trước kích hoạt live, không chặn viết và test adapter giả.
  - **Touchpoints:** `src/usecase/Translation/` để tham khảo boundary, `src/infra/Adapters/`, `src/infra/DependencyInjection.cs`; `Speech` port/adapter/config mới là đề xuất.
  - **Đầu ra:** request Google HTTPS cố định và model/voice allowlist, secret server, timeout25s, không redirect/retry tự động; nhận WAV hợp lệ hoặc đóng PCM đúng thông số, không double-header; kiểm giới hạn thực tế.
  - **Kiểm chứng:** **YAU-AC05/07/08**, fake provider malformed/oversized/blocked/429/cancel và không lộ khóa/nội dung.

- [x] **T22 — Speech endpoint và nguồn đọc có xác minh**
  - **Phụ thuộc:** T21, T05.
  - **Touchpoints:** `src/interface/Endpoints/`, `ApiComposition.cs`, repositories lookup; speech use case/handler mới là đề xuất.
  - **Đầu ra:** `/api/speech` dictionary canonical/reading/form pair hoặc selected Japanese text; body cap cả chunked, quota và backend deadline28s, response WAV envelope; không nhận provider URL/model/instruction tùy ý từ client.
  - **Kiểm chứng:** **YAU-AC02/03/04/05/07/08**; request sai bị từ chối trước provider và câu vượt giới hạn audio không bị cắt rồi báo thành công.

- [x] **T23 — Nút Nghe, Stop, Replay và quản lý playback**
  - **Phụ thuộc:** T05, T09, T22.
  - **Touchpoints:** `PopupShell.tsx`, `extension/src/platform/`, `extension/src/background.ts`, API validator và popup lifecycle; speech panel/transport mới là đề xuất.
  - **Đầu ra:** disclosure Google và nhãn AI trước click, một request/playback current, coalesce double click, validate WAV/correlation, Web Audio buffer/context current trong RAM, replay dùng lại buffer, deadline background 30s và UI 32s. Hover chỉ thực hiện tra từ.
  - **Kiểm chứng:** **YAU-AC01/02/03/04/06/07/08**, **YPO-AC08/10**; đóng/đổi từ dừng audio, pin/rê chuột giữ audio current.

- [ ] **T24 — Kiểm thử audio giả lập và nghe live độc lập**
  - **Phụ thuộc:** T21, T22, T23 cho test giả; T20 cho nghe live và hoàn thành task.
  - **Touchpoints:** unit/integration adapter+parser, browser speech fixtures, hồ sơ nghe review đề xuất dưới `evidence/`.
  - **Đầu ra:** parser/cancel/quota/deadline tests và bộ mẫu tiếng Nhật có từ đồng âm, mục từ có nhiều cách đọc và câu có kanji, kana, số. Chủ dự án nghe trực tiếp, đối chiếu cách đọc đã chọn và kiểm câu đọc đủ chữ, đúng thứ tự. Ghi lỗi theo mẫu. Bộ 4 mẫu và cách mở trang có trong [hướng dẫn local](../../docs/local-reading-experience.md).
  - **Kiểm chứng:** toàn bộ **YAU-AC01–YAU-AC09**, **G-F**. Thiếu quyền/quota hoặc người review thì phần live vẫn pending; không dùng fake success để tick xong.

## P6 — Kiểm chứng và bàn giao cùng đợt

- [ ] **T25 — Kiểm thử lifecycle liên nhóm**
  - **Phụ thuộc:** T07, T10, T15, T19, T24.
  - **Touchpoints:** controllers/view/background namespaces và packaged browser suites.
  - **Đầu ra:** luồng selection→hover→vào popup→pin→nghe→đổi từ→dịch→Escape; test ignored abort, response trễ, callback treo và không gửi context nhầm sang provider.
  - **Kiểm chứng:** **G-B**, **YSC-AC05/06/09**, **YPO-AC04/05/08/10**, **YAU-AC01/06/08**, **YUI-AC05/06**.

- [ ] **T26 — Kiểm riêng tư, payload và provenance thực tế**
  - **Phụ thuộc:** T25.
  - **Touchpoints:** HTTP/background logs, network captures chứa mẫu synthetic, settings storage, manifests và inventory T02.
  - **Đầu ra:** báo cáo không persist raw text/audio/key/history, quyền host tối thiểu, namespace bảo vệ theo tab/frame, source/license đầy đủ cho nội dung thực lấy lại; không lưu payload nhạy cảm trong evidence.
  - **Kiểm chứng:** **G-D**, **YSC-AC01/03/04**, **YAU-AC01/04/07**, **YPO-AC07**, **YUI-AC05** và ràng buộc chung G04–G07.

- [ ] **T27 — Chạy kiểm thử, build và benchmark có giới hạn rõ**
  - **Phụ thuộc:** T25, T26.
  - **Touchpoints:** `.NET` unit/integration, extension unit/type/build/browser, regression corpus và hồ sơ benchmark đề xuất.
  - **Đầu ra:** kết quả các suite cần thiết, browser matrix Chrome/Edge/Brave/Firefox cài sẵn, p95 shell/analysis/scan/TTS kèm máy/dataset/sample/cache; browser thiếu ghi chưa xác minh. Tách cold startup khỏi warm latency.
  - **Kiểm chứng:** **G-C/G-E**, **YUI-AC01–08**, **YSC-AC01–09**, **YCO-AC01–08**, **YAU-AC01–09**, **YPO-AC01–10**; mục tiêu hiệu năng không được ghi thành số đo đã đạt nếu chưa đo.

- [ ] **T28 — Review độc lập và bàn giao đủ 5 nhóm**
  - **Phụ thuộc:** T27.
  - **Touchpoints:** traceability/evidence đề xuất dưới `plans/261104-yomitan-reading-experience/evidence/`, liên kết từ spec005 khi cần; tài liệu sử dụng và packaged extension artifacts; plan/task status.
  - **Đầu ra:** reviewer đối chiếu tất cả FR/NFR/errors/AC, xác nhận không mở rộng scope; tài liệu cài/cấu hình/quota, giới hạn browser/model và cách dùng. Tick milestone chỉ khi đủ5 nhóm; không tự deploy/commit/push nếu chưa được yêu cầu.
  - **Kiểm chứng:** **G-A–G-G**, mọi AC có bằng chứng và không còn Must thiếu; spec còn đề xuất phải ghi tình trạng chính xác.

## Traceability nhóm tính năng

| Nhóm | SPEC | Tasks thực hiện chính | Tiêu chí cần có bằng chứng |
|---|---|---|---|
| YUI | [Popup content](../../.sdd/specs/005-yomitan-reading-experience/features/popup-content.md) | T04–T07, T25, T27–T28 | YUI-AC01–YUI-AC08 |
| YSC | [Pointer scanning](../../.sdd/specs/005-yomitan-reading-experience/features/pointer-scanning.md) | T11–T15, T25–T28 | YSC-AC01–YSC-AC09 |
| YCO | [Conjugation coverage](../../.sdd/specs/005-yomitan-reading-experience/features/conjugation-coverage.md) | T16–T19, T27–T28 | YCO-AC01–YCO-AC08 |
| YAU | [AI pronunciation](../../.sdd/specs/005-yomitan-reading-experience/features/ai-pronunciation.md) | T20–T24, T25–T28 | YAU-AC01–YAU-AC09 |
| YPO | [Popup behavior](../../.sdd/specs/005-yomitan-reading-experience/features/popup-behavior.md) | T08–T10, T23, T25, T27–T28 | YPO-AC01–YPO-AC10 |

## Traceability nghiệm thu chung

| Gate | Tasks cung cấp bằng chứng | Điều kiện hoàn thành |
|---|---|---|
| G-A — Đủ yêu cầu | T01, T28 | Mỗi FR/NFR/error/AC có đối chiếu và không thiếu Must |
| G-B — Lifecycle liên nhóm | T09, T14, T15, T23, T25 | Không ghi đè/hủy nhầm, kể cả ignored abort và pin |
| G-C — Độ đúng và regression | T03, T07, T15, T19, T27 | Ruby/Unicode/restrictions/corpus và contracts cũ đạt |
| G-D — Privacy, giới hạn, giấy phép | T02, T03, T21, T22, T26 | Không lộ nội dung/key, đúng payload/timeout, đầy đủ nguồn |
| G-E — Browser và accessibility | T10, T27 | Bàn phím, viewport/frame/browser có bằng chứng và giới hạn rõ |
| G-F — Audio live | T20, T24 | Quyền/quota/format và chất lượng nghe được xác minh, không dùng fake thay live |
| G-G — Milestone đủ 5 nhóm | T28 | G-A–G-F đạt và một gói release có đủ nhóm được yêu cầu |
