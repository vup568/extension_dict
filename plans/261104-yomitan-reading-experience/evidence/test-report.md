# Kiểm chứng đợt code đầu của feature 005

Ngày 08/11/2026. [Checklist triển khai](../task.md) ghi những task đủ bằng chứng ở phạm vi hiện tại. Các gate nghiệm thu tích hợp G-A–G-G vẫn mở.

## Kết quả lượt 08/11/2026

| Phạm vi | Kết quả | Bằng chứng |
|---|---|---|
| .NET build | 0 cảnh báo, 0 lỗi | `dotnet build JPReadingPlatform.sln --no-restore -m:1 -p:UseSharedCompilation=false -nr:false` |
| .NET unit và HTTP integration | 130 unit và 48 integration đạt | xUnit direct runner. Integration chọn `ScanIntegrationTests`, `SpeechEndpointTests`, `GeminiSpeechProviderTests`, `TranslationIntegrationTests`, `GeminiTranslationProviderTests` trên HTTP loopback/adapter giả |
| Extension | Typecheck, 296 Vitest và build Chromium/Firefox đạt | `npm --prefix extension run typecheck`, `test`, `build` với dependencies hiện có |
| Chrome | 38 ca popup/translation và 8 ca scan/resize/audio đạt | Playwright headless, fixture Vite đóng gói nạp qua route fulfill. Runner webServer mặc định hết thời gian chờ Vite loopback |
| Edge MV3 đóng gói | Hai smoke dùng API giả lập đạt | Extension thật, profile riêng và backend localhost. Shift thật ở giữa viewport gửi `🐈日本語` với offset UTF‑16 4. Shift giả tạo zero request. Trang CSP `media-src 'none'` phát WAV hợp lệ qua Web Audio và nghe lại dùng buffer hiện hành |
| F09 đã nghiệm thu | 12/12 hash khớp, 60 ô và 412 ca | [Đối chiếu corpus](conjugation-gap-catalog.md) |

## Truy vết phần đã kiểm tra

| Nhóm | Mã và test | Giới hạn |
|---|---|---|
| YUI | `PopupShell`, `dictionarySpeechOptions`, `TranslationPanel`, fixtures popup/translation và tests chọn reading | Có fixtures cho từ lặp, đồng âm, restriction, kết quả cũ, lỗi riêng và dịch chủ động. T07 còn bộ response từ điển thật |
| YPO | `createPopupView`, `startSelectionPopup`, preference store, browser `reading-interaction` và `selection-popup` | Kiểm focus return, viewport 320 px, pointer blur, ghim, thay selection, nhả Shift, close. Ma trận zoom/text dọc/cross-frame/browser còn mở |
| YSC | `extractPointerWindow`, `startPointerScan`, `readingTransport`, `ScanTextUseCase`, `ScanEndpoints` | Kiểm bounded UTF‑16/ruby/nguồn editable, offset/correlation, debounce, stale, quota, cap chunked và tương thích analysis. T13/T15 cần chạy PostgreSQL/Sudachi thật |
| YAU | `SpeechPanel`, `readingContracts`, `SpeechUseCase`, `GeminiSpeechProvider`, `SpeechEndpoints` | Kiểm click thật, cặp canonical/reading/form, WAV/PCM sai, cap, deadline, quota, stale, CSP và replay. T20/T24 cần model/quota thật và người nghe review tiếng Nhật |
| YCO | Baseline F09 và gap catalog | Duyệt nguồn/đáp án cho T16, rồi tạo corpus phiên bản mới trước khi mở rộng rule |

## Giới hạn lượt 08/11/2026

- Docker Desktop Linux engine không chạy trong phiên này. Bộ integration PostgreSQL/Sudachi và packaged smoke từ điển thật vẫn chờ.
- Edge chạy được dưới cấu hình packaged riêng. Runner harness Edge mặc định lỗi khởi chạy trong môi trường Windows. Brave và Firefox runtime chưa có bằng chứng mới.
- Chưa thực hiện probe Gemini live hoặc benchmark p95. Số liệu fixture không đại diện cho giọng đọc Nhật, dataset thật hoặc hiệu năng phát hành.
- Còn kiểm chứng tích hợp liên nhóm, traceability đầy đủ và rà soát privacy/browser trước khi đánh dấu milestone hoàn thành.

## Kiểm chứng từ điển thật ngày 10/11/2026

- Docker Desktop, PostgreSQL 16 và Sudachi đã chạy. Database `jp_dict_real_jaen_20261030_v3` có 218.914 mục từ, 13.108 kanji và 5 mẫu ngữ pháp. Backend đã qua xác minh nguồn và dữ kiện canonical trước khi báo healthy. Lượt test dùng binary WebApi đã build ngày 08/11. Cách gán builder trong lệnh thử không đưa `default_transaction_read_only=on` vào chuỗi kết nối thực tế. Khi truyền `Options` trực tiếp, kiểm tra trên binary xác nhận `IsolatedImportTarget` từ chối chuỗi. Hướng dẫn hiện dùng chuỗi kết nối đã được guard chấp nhận. Cache NuGet hiện thiếu `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.4 nên lệnh build lại .NET chưa thành công.
- `npm --prefix extension run typecheck`, 296 Vitest và build Chromium/Firefox đạt. Playwright dùng Edge đã cài, profile riêng và extension Chromium vừa build.
- `playwright test --config playwright.real-dictionary.config.ts` đạt. Popup tra được `食べる`, `食べました`, `橋`, `箸`, giữ ID JMdict, nghĩa tương ứng và thông tin kanji `食`. Lượt đầu kẹt khi thư mục output mặc định chứa log backend đang mở. Config hiện dùng thư mục riêng và lượt chạy lại đã qua.
- `playwright test --config playwright.sentence-vocabulary.config.ts` đạt. Bốn ca bôi đen xác minh span, từ lặp và giải thích thể chia. Ba ca Shift xác minh `日本語` tại UTF‑16 offset 4 với emoji/ruby, `食べました → 食べる` tại offset 3 và trợ từ `を` tại offset 1 trả `target_span: null`. Request chỉ có text giới hạn, offset và UUID, không kèm cookie. Nhả Shift giữ popup. Cả lượt có zero request dịch và phát âm.
- Chrome qua 10 ca `reading-interaction` với fixture Playwright. Các ca bổ sung xác minh 100 lần di chuyển chuột khi tắt quét hoặc thiếu Shift, đổi A → B rồi bỏ phản hồi trễ của A, bôi đen khi scan đang chờ, bỏ qua closed shadow root và giữ audio hiện tại khi đã ghim popup.
- Các probe `/api/analysis` v3 bổ sung cho thể chia ghi tại [bảng đối chiếu corpus](conjugation-gap-catalog.md). Browser matrix đầy đủ, benchmark p95, kiểm thử Gemini live và duyệt corpus phiên bản mới vẫn là các điều kiện riêng.

### Xử lý lỗi Gemini khi nghe mẫu

Probe do người dùng chạy trả HTTP 400 `invalid_request` với chi tiết `Audio delivery mode is not supported.` Adapter và probe đã bỏ `response_format.delivery`, giữ yêu cầu WAV mono đã xác minh khi nhận. Bản thử đặt tại `src/interface/bin/tts-fix/net10.0/`, build lại Infrastructure từ dependencies đã có. 14 kiểm thử adapter và probe PowerShell 5.1 với HTTP giả lập đạt.

Ngày 10/11/2026, chủ dự án thử lại trong popup và xác nhận giọng đọc dùng ổn. Đây là bằng chứng nghe được từ luồng Gemini thật sau bản sửa. T20/T24 còn cần ghi model, giọng, hạn mức quan sát được và kết quả theo từng mẫu của YAU-AC09.

## T16 — Chốt danh mục thể chia ngày 10/11/2026

- [Danh mục `yco-gap-20261110.1`](conjugation-gap-catalog.md) đã qua rà soát độc lập về cách đọc, POS, ID JMdict, trường hợp âm và span UTF‑16. SHA‑256 của catalog là `b70b51ef3c76ca9ab7cba164ad79eaaa9242e4e37633354dcb175716b6686e91`.
- Baseline F‑09 khớp 12/12 SHA‑256 theo manifest nghiệm thu. Validator `validate-corpus-20261013.3.mjs` chạy với `--self-test --verify-freeze` đạt trên 412 ca, 60 ô, 115 rule ID, 17 fixture và 369 đầu vào có quan sát gRPC lưu trữ. T17 sẽ cụ thể hóa các đáp án thành corpus phiên bản riêng để rà soát trước T18.

## T10 — Kiểm chứng vòng đời popup ngày 10/11/2026

- Typecheck, 296 ca Vitest và build Chromium/Firefox đạt. Playwright chạy bốn bộ `reading-viewport-frames`, `reading-interaction`, `selection-popup` và `translation-popup` với Chrome, Edge và Brave. Kết quả 134 ca đạt, gồm 17/18 ca T10 tập trung. Một ca MV3 đóng gói được cấu hình bỏ qua ở Chrome và đã đạt ở Edge lẫn Brave.
- Khi `showPopover` thiếu hoặc ném lỗi, popup gắn vào `documentElement` để tránh tọa độ bị dịch theo `body` có CSS transform. Hai nhánh fallback qua bài kiểm tra mở và đóng ở viewport 320 × 400. Nhánh top layer mở trong `body` và giữ lề 12 px khi visual viewport giảm xuống 320 px ở mức scale 200% qua CDP.
- Fixture xác minh focus/Escape, bố cục chữ dọc, `pointercancel` nhả capture và giữ selection lẫn yêu cầu nghe đang chờ. Extension đóng gói xác minh popup nằm trong frame cùng nguồn và khác nguồn được phép, không mở trong frame `data:`, lưu kích thước và đặt lại ghim khi tải lại. Các ca frame dùng chốt độ dài cục bộ nên không gọi backend.
- Firefox có bản build, môi trường kiểm thử thiếu runtime. Các ca vòng đời dùng stub audio. T25/P6 còn lượt kiểm tích hợp với phát âm thật và đánh giá tương thích trước phát hành.

## T17 — Xác thực corpus thể chia ngày 10/11/2026

- [Corpus `yco-20261110.1`](../../../.sdd/specs/005-yomitan-reading-experience/corpus/yco-20261110.1/README.md) có 21 ca thuộc 9 nhóm, gồm 7 ca hồi quy, 10 ca cho dataset mới và 4 ca đối chiếu ngôn ngữ. Hồ sơ ghi 11 nguồn và trỏ trực tiếp đến 14 ca F‑09. Sáu trong tám fixture đã được đối chiếu ID JMdict đã pin, từ loại, cặp dạng viết–cách đọc và nghĩa. Hai fixture tổng hợp dùng cùng một canonical ID để kiểm giới hạn giữa các cặp.
- Rà soát độc lập đạt sau khi ghi rõ `match_policy: "any"` cho ca chỉ yêu cầu một mục từ hợp lệ tại span, sửa dạng viết `仕舞う` / `仕舞った`, bổ sung liên kết đáp án Irodori và neo ca hồi quy fallback `書きながら → 書く` cho YCO‑AC05.
- `node .sdd/specs/005-yomitan-reading-experience/corpus/validate-yco-corpus.mjs --self-test` đạt 9 phép thử phát hiện sai lệch dữ liệu và xác minh 12 hash baseline. [Manifest YCO](../../../.sdd/specs/005-yomitan-reading-experience/corpus/yco-20261110.1/manifest.json) có SHA‑256 `8845f0db6c5e8cd89338b483ff03f9634d9280dc941ce948129f141add5b90be` trên UTF‑8 xuống dòng LF. Validator F‑09 với `--self-test --verify-freeze` tiếp tục xác minh 412 ca, 60 ô và 115 rule ID.
- 48 ca hồi quy .NET hiện có đạt khi chạy trực tiếp binary xUnit đã build, gồm `SentenceVocabularyUseCaseTests`, `DictionaryReadCorrectnessTests` và `LookupConjugationCandidateTests`.
- Lượt này xác thực cấu trúc, tham chiếu, hash và span. Các kỳ vọng dành cho bộ phân tích được chạy ở T18/T19.

## T18 — Kiểm chứng dataset ứng viên ngày 10/11/2026

- `candidate-20261110.1` thêm bốn biến thể bằng dữ liệu cho `v1` và `v5m`, gồm dạng thường khẳng định không quá khứ và quá khứ của `Vて/で + しまう`. Hai rule ID mới nâng tổng lên 913 biến thể và 117 rule ID. Cả 909 biến thể F‑09 khớp bản gốc và 12 hash nghiệm thu khớp manifest. [Dữ liệu quy tắc](../../../src/infra/Conjugation/Data/candidate-20261110.1.rules.json)
- [Script biên dịch](../../../.sdd/specs/005-yomitan-reading-experience/corpus/compile-yco-candidate.mjs) tạo cùng dữ liệu JSON trên stdout với SHA‑256 `ef4fc15ff7c36ca0acce2023101bd268d0163714cfef33d4fb195fd580489c19`. Validator T17 xác minh 21 ca, 9 nhóm và 9 phép thử phát hiện sai lệch. Validator F‑09 xác minh 412 ca, 60 ô và 115 rule ID với `--verify-freeze`. Rà soát độc lập đạt.
- [Bộ test T18](../../../tests/integration/YcoConjugationCandidateTests.cs) đạt 9/9 ca với phản hồi gRPC và từ điển thay thế, gồm ưu tiên khớp chính xác, dạng biến âm sai, giới hạn cặp dạng viết–cách đọc, span và chọn phiên bản dataset. Các ca được biên dịch bằng Roslyn với DLL hiện có vì cache NuGet thiếu `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.4 cho build chuẩn `--no-restore`. Các binary có sẵn chạy riêng đạt 130 unit và 28 integration có phạm vi.
- Quan sát gRPC thật với 17 đầu vào tổng hợp khác nhau trong container tạm chỉ đọc, tắt mạng ngoài, dùng image có sẵn `jp-reading-tokenizer:verified-20261013`. Image dùng SudachiPy 0.7.0, từ điển core 20260723.1 và gRPC 1.84.0. Đối chiếu tự động bốn ca từ dương của T17 xác nhận gốc động từ chính, surface của token và ranh giới UTF‑16 khớp kỳ vọng. Kiểm tích hợp API, PostgreSQL và popup thuộc T19.
- Việc cài và chọn dataset ứng viên thuộc T19. Cơ chế xác minh từ điển đã nhập đối chiếu chính xác lịch sử migration, nên quyết định cài cần kiểm tương thích với cơ chế đó.

## T19 — Cài dataset và kiểm toàn tuyến ngày 18/11/2026

- Cài bằng migration tiến `20261118075656_AddYcoConjugationCandidate`, chèn `candidate-20261110.1` thành phiên bản mới cạnh `candidate-20261013.1`. Không sửa migration hay dữ liệu cũ; `Down` từ chối như bản F‑09. Model snapshot không đổi. Script khôi phục DB chỉ áp tới `20261014022755_AddConjugationCandidate`, phần sau do `MigrateAsync` áp lúc khởi động, nên kiểm lịch sử migration của script, import và demo vẫn khớp.
- Snapshot nhúng [`candidate-20261110.1.json`](../../../src/infra/Conjugation/Data/candidate-20261110.1.json) có SHA‑256 `ef4fc15ff7c36ca0acce2023101bd268d0163714cfef33d4fb195fd580489c19`, trùng hash T18. `EmbeddedSnapshotEqualsReviewedRuleCompilation` chạy lại bộ sinh và so khớp JSON để snapshot không lệch khỏi quy tắc đã rà soát.
- [`YcoCorpusEndToEndTests`](../../../tests/integration/YcoCorpusEndToEndTests.cs) chạy 21 ca YCO qua `/api/analysis` hợp đồng 3 và `/api/scan`, với PostgreSQL 16 và sidecar `jp-reading-tokenizer:verified-20261013` thật, mỗi ca chỉ thấy fixture JMdict của nó. Pin cũ đạt 7 ca hồi quy, kể cả `SPAN-01-BASELINE` lemma-only. Pin mới đạt 6 ca hồi quy và 10 ca ứng viên. Ràng buộc `forbidden` áp cho mọi ca và cả hai pin. Scan tại đầu mỗi occurrence trả đúng `target_span` và cùng giải thích; probe offset trong surrogate hoặc trên dấu câu trả 400 `VALIDATION_FAILED`.
- So sánh cũ/mới trên corpus YCO: chỉ bốn ca dương `て/で + しまう` đổi, từ lemma-only sang `conjugation:verb-te-shimau` / `:plain-past`. Các ca còn lại cho cùng kết quả. Dạng biến âm sai `読んて…` và cặp viết–đọc giới hạn không sinh giải thích.
- 4 ca `linguistic_only` chỉ ghi nhận: `行った` trong `昨日、実験を行った。` vẫn ra `行く`, không ra `行う`. Đây là giới hạn chọn cách đọc theo ngữ cảnh đã nêu trong corpus, không phải hồi quy của dataset mới.
- YCO‑AC08: `ApprovedCorpusMatchesRealSudachiAndCanonicalPostgreSqlFixtures` chạy đủ 404 ca F‑09 đã nghiệm thu với từng pin; hợp đồng 1/2/3 của `/api/analysis` đạt với cả hai pin. Nhóm `ConjugationCorpusIntegrationTests` đạt 21/21.
- Thời gian chạy chỉ mang tính tham khảo, không phải benchmark: 404 ca F‑09 mất 21–23 s với pin cũ và 17–19 s với pin mới qua hai lượt trên cùng máy; pin cũ chạy trước nên gồm khởi động nóng. Giới hạn ứng viên và hủy nằm ở engine (deadline tokenizer 500 ms, câu tối đa 128 lần tra trong 3,5 s) và không đổi; dataset chỉ thêm 4 biến thể. p95 thuộc T27.
- Đường quay lại: hai phiên bản cùng nằm trong DB và bất biến; đổi `ConjugationDatasetVersion` hoặc `-DatasetVersion` là quay lại, không cần migration.
- Popup không lọc `rule_id` và chỉ yêu cầu mọi giải thích trong một phản hồi cùng `dataset_version`, nên hiển thị rule mới mà không cần sửa extension.
- Còn mở: kiểm popup trên DB local thật với pin mới và quyết định đổi pin mặc định trong script khởi động.

## Bố cục tab của popup ngày 23/11/2026

- Chủ dự án duyệt bản mẫu và chọn: 4 tab Từ vựng, Kanji, Ngữ pháp, Câu; bấm từ ngay trong câu ở đầu popup; tab Kanji chỉ hiện kanji của từ đang chọn; chế độ tối theo hệ thống; nhãn từ loại tiếng Việt; tô mẫu ngữ pháp trong câu. [YUI 0.3](../../../.sdd/specs/005-yomitan-reading-experience/features/popup-content.md) ghi lại hành vi.
- Mã: `PopupShell` chia thành `PopupTabs`, `VocabularyPanel`, `ReferencePanels`, `sentenceSegments`, `partOfSpeechLabels`, `popupIcons`. Panel không hiện vẫn mounted nên đổi tab không hủy bản dịch hay âm thanh. Phân tích thất bại không hiện tab, phần dịch nằm dưới thông báo lỗi; tra bằng con trỏ không có tab Câu và không hiện phần dịch kể cả khi lỗi. Không thêm dependency, không đổi contract API.
- `npm --prefix extension run typecheck`, 305 ca Vitest và build Chromium/Firefox đạt. Bộ test mới `popupLayout.test.ts` kiểm tách câu, nhãn từ loại, panel ẩn, phạm vi kanji theo từ, tab khi quét và khi lỗi.
- Playwright với bản cuối: 167 ca đạt trên Chrome, Edge và Brave cho bảy bộ popup (`selection-popup`, `translation-popup`, `conjugation-popup`, `kana-only-popup`, `reading-interaction`, `reading-viewport-frames`, `host-page-no-side-effects`). Một ca MV3 đóng gói được cấu hình bỏ qua ở Chrome và đạt ở Edge lẫn Brave. Trước bản sửa chặn dịch khi quét lỗi, toàn bộ 71 ca `tests/browser` trên Chrome cho 70 đạt, 1 bỏ qua.
- Các test đã sửa theo bố cục mới: mở tab thay cho bấm tiêu đề thu gọn, nhóm từ dùng `role="group"`, kiểm tô ngữ pháp và mục tiêu quét, thêm ca bản dịch đang chờ vẫn còn sau khi đổi tab.
- Giới hạn: bốn smoke `tests/demo` đã sửa bộ chọn nhưng chưa chạy vì cần Docker và backend. Chưa xem popup mới trên DB từ điển thật hoặc với Gemini thật. Firefox vẫn chưa có runtime để kiểm.

## T19 — DB thật thiếu migration YCO ngày 23/11/2026

- Truy vấn chỉ đọc: `jp_dict_real_jaen_20261030_v3` dừng ở `20261015154123_AddDictionaryImportControl`; code từ commit `3cb0e01` có thêm `20261118075656_AddYcoConjugationCandidate`. Với DB `jp_dict_real_*`, `DatabaseStartup` chỉ gọi `DictionaryImportStore.ValidateAsync`, trong đó `RequireOwnedAsync` đòi danh sách migration đã áp khớp chính xác với code (`IMPORT_SCHEMA_MISMATCH`) và cố ý không migrate.
- So sánh A/B trên cùng DB, chạy `WebApi.dll` có sẵn vì cache NuGet đã bị xóa: bản build 18/11 với pin `candidate-20261110.1` thoát sau khoảng 5 giây với `Database initialization failed (InvalidOperationException)`; bản build 08/11 (chưa có migration YCO) với pin `candidate-20261013.1` qua xác minh nguồn, dữ liệu và facts sau 131 giây rồi mở cổng. Dữ liệu và file nguồn của DB thật còn nguyên; lỗi chỉ do lịch sử migration. Vì kiểm schema chạy trước kiểm pin, `start-real-dictionary.ps1` với code hiện tại không khởi động được DB thật dù chọn pin nào.
- Popup dạng tab trên từ điển thật, dùng backend 08/11 và pin cũ, extension đóng gói trong Brave headless: `real-dictionary-smoke` và `sentence-vocabulary-smoke` đạt. `translation-no-key-smoke` có lỗi sẵn từ khi popup mở từ đầu tiên có kết quả (猫): test chưa chọn `食べました` trước khi tìm mục `食べる`. Đã sửa test và chạy lại đạt.
- Pin mới trên demo: tạo `jp_dict_demo`, nạp 17 fixture bằng `--seed-demo-dictionary` của bản build 18/11 (bản Release 15/10 chưa có migration YCO), chạy backend với `candidate-20261110.1`. DB demo ở migration YCO; `demo-dictionary-smoke` đạt, gồm kiểm `dataset_version` của mọi giải thích là `candidate-20261110.1`.
- Chưa chạy: `DemoScriptTests` và `LocalBackendScriptTests` cần build lại project integration (thiếu cache NuGet) và gọi `pwsh.exe`, trong khi máy này chưa cài PowerShell 7. Các bước chạy backend trên dùng lệnh PowerShell 5.1 tương đương script, không sửa script.
- T19 chưa tick: cần chọn cách đưa migration YCO vào DB ứng viên rồi kiểm lại popup trên DB thật với pin mới.

## T19 — Nâng cấp DB thật và nghiệm thu ngày 23/11/2026

- Chủ dự án chọn thêm lệnh nâng cấp thay vì nhập lại. Spec 002 có FR-015. Lệnh `--dictionary-import upgrade` (bắt buộc `--confirm-target`) chỉ áp migration còn thiếu khi lịch sử đã áp là phần đầu đúng thứ tự và mọi migration thiếu chỉ gồm `InsertData` vào `conjugation_datasets`. Trước khi ghi, nó chạy cùng kiểm tra sở hữu, checkpoint và facts hash như `validate`; sau khi ghi, nó kiểm lại với lịch sử đầy đủ. Khởi động backend vẫn không migrate DB ứng viên.
- Kiểm thử: build .NET 0 cảnh báo; 131 unit đạt (thêm ca chuyển verb `upgrade`); 60 integration `DictionaryImport*` và `DatabaseStartup*` đạt, gồm nâng cấp thành công rồi chạy lại là no-op, và ba ca từ chối không ghi gì (migration đổi bảng, lịch sử có lỗ hổng, gloss bị sửa). Toàn bộ integration trừ hai bộ script: 327 đạt, 9 lỗi đều ở `LocalRecoveryScriptTests` vì không khởi động được `pwsh.exe` (máy chưa cài PowerShell 7).
- Sao lưu trước khi ghi: `pg_dump -Fc` của `jp_dict_real_jaen_20261030_v3` ra `E:\jp-dict-backups\jp_dict_real_jaen_20261030_v3-before-upgrade-20261123.dump` (33,8 MB, 22 bảng dữ liệu theo `pg_restore --list`).
- Nâng cấp DB thật bằng `WebApi.dll` vừa build: thoát mã 0, `verified_no_op: false`, 218.914 mục từ, 13.108 kanji, `facts_hash` `168480cd1fd390f37fcbf4d2d30dccd24496175ae4916eed1084ac69501728e7`, trùng hash ghi khi nhập ngày 30/10. DB giờ ở `20261118075656_AddYcoConjugationCandidate` và có cả `candidate-20261013.1` lẫn `candidate-20261110.1`.
- Backend hiện tại với pin `candidate-20261110.1` qua xác minh nguồn, dữ liệu và facts sau 136 giây rồi mở cổng. Probe `/api/analysis` hợp đồng 3: `猫が魚を食べてしまった。` cho `食べてしまった → 食べる` với `conjugation:verb-te-shimau:plain-past` thuộc `candidate-20261110.1`; `食べました` cho `verb-polite-past` cùng dataset. `real-dictionary-smoke`, `sentence-vocabulary-smoke` và `translation-no-key-smoke` đạt trong Brave headless với extension đóng gói và popup dạng tab.
- Giới hạn: `DemoScriptTests`, `LocalBackendScriptTests` và `LocalRecoveryScriptTests` chưa chạy được vì thiếu PowerShell 7; backend được chạy bằng lệnh PowerShell 5.1 tương đương script. Probe `curl` từ Git Bash gửi sai mã hóa chữ Nhật nên đã thay bằng Node; đây là lỗi công cụ thử, không phải backend.
