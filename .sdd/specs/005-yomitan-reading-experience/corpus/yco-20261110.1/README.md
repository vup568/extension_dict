# Corpus thể chia `yco-20261110.1`

Bộ ca T17 mở rộng [F‑09 đã nghiệm thu](../../../feat-conjugation-explanation/evidence/acceptance-manifest.json) theo [danh mục T16](../../../../../plans/261104-yomitan-reading-experience/evidence/conjugation-gap-catalog.md). `manifest.json` khóa nội dung ca, nguồn và phiên bản baseline. Hash của hồ sơ YCO tính trên UTF‑8 với xuống dòng LF. Hồ sơ F‑09 dùng nguyên byte đã nghiệm thu.

## Cách đọc đáp án

- `regression` ghi kỳ vọng cho dataset F‑09 và các luồng hiện có. `baseline_sentence` dùng riêng `candidate-20261013.1` để giữ chứng cứ lemma-only của `食べてしまった`.
- `candidate` ghi kỳ vọng phải chạy khi T18 có dataset mới. Bốn ca dương kiểm dạng thường khẳng định của `て/で + しまう` với `v1` và `v5m`, ở không quá khứ và quá khứ. Sắc thái ngoài ý muốn tùy ngữ cảnh. [NINJAL](https://mmsrv.ninjal.ac.jp/kamus/data/item2981.html)
- `linguistic_only` ghi cách đọc hoặc nghĩa có căn cứ. Ví dụ đứng riêng `行った` có hai cách đọc có thể. Hai câu ví dụ minh họa cách hiểu theo ngữ cảnh. Các ca này không buộc tokenizer chọn nghĩa. [Bunkachō, trang 41](https://www.bunka.go.jp/tokei_hakusho_shuppan/hakusho_nenjihokokusho/archive/pdf/93728801_03.pdf#page=41)
- `fixture_ids` chọn dữ kiện từ điển để thiết lập ca. `base_only` loại mục exact của surface. `exact_surface` cung cấp mục exact theo dạng viết hoặc cách đọc cùng với lemma cạnh tranh. `restricted_entry` dựng cùng một mục với hai cặp viết–đọc–sense riêng. `sense_ordinal` là thứ tự 1-based trong nguồn JMdict đã pin.
- `occurrences` liệt kê các vị trí cần đối chiếu trong kết quả. `match_policy: "any"` yêu cầu có occurrence đúng surface và span với ít nhất một mục từ, cho phép mọi mục từ hợp lệ. `matches: []` trong ca đó là danh sách điều kiện định danh rỗng. `forbidden` cấm cách phân tích chỉ định, có thể giới hạn theo mục từ và span. Kỳ vọng `lemma-only` yêu cầu mục từ hợp lệ với danh sách giải thích rỗng. Kỳ vọng `exact` yêu cầu ưu tiên mục trùng surface và không phân tích thể chia.
- Span là khoảng nửa mở UTF‑16. `occurrences[].span` tính trên văn bản đã chuẩn hóa. `segments[].span` tính trên surface của từng occurrence. Scan trả đúng `target_span` hoặc HTTP 400 `VALIDATION_FAILED` cho offset trong surrogate hay trên dấu câu đã nêu.

## Truy vết kiểm thử

| Tiêu chí | Ca liên quan |
|---|---|
| YCO-AC01 | Chín nhóm trong `manifest.required_gap_ids` và các `source_refs` theo ca |
| YCO-AC02 | `V04-P1`, `V07-IRR-kuru` từ F‑09 và `IRR-01-PAST` / `IRR-01-WRONG-ONBIN` |
| YCO-AC03 | `I03-P1`, `I03-IRR-ii`, `A04-ALT-dewa-静か`, `N06-P1` từ F‑09 |
| YCO-AC04 | `AMB-01`, `V13-P1` từ F‑09 và ba ca đối chiếu cách đọc `行った` |
| YCO-AC05 | `SPAN-01-BASELINE` giữ lemma và danh sách giải thích trống. Ca hồi quy `書きながら → 書く` tiếp tục kiểm fallback ngoài họ quy tắc mới. [SentenceVocabularyUseCaseTests](../../../../../tests/unit/SentenceVocabularyUseCaseTests.cs#L275) |
| YCO-AC06 | `SPAN-01-BASELINE`, `SPAN-01-CANDIDATE` và hai ca `SPAN-02` |
| YCO-AC07 | `I09-N1`, `EXACT-DERIVED-書ける` từ F‑09 cùng `AUX-02-RESTRICTED-SENSE`, hai ca biến âm sai và `AUX-03-EXACT-INTERJECTION` |
| YCO-AC08 | Toàn bộ F‑09 được khóa bằng manifest đã nghiệm thu. T19 chạy hồi quy và các contract 1/2/3 trước khi kích hoạt dataset mới. |

Từ thư mục gốc của repo, kiểm tra corpus và các ca cố tình làm sai dữ liệu bằng lệnh sau.

```sh
node .sdd/specs/005-yomitan-reading-experience/corpus/validate-yco-corpus.mjs --self-test
```

Lệnh kiểm schema, tham chiếu, hash và hình học span.

## Dataset ứng viên T18

[Bộ sinh](../compile-yco-candidate.mjs) kiểm hash corpus đã rà soát và dataset F‑09 đã nghiệm thu, rồi xuất JSON UTF‑8 của `candidate-20261110.1` ra stdout. Dataset ghép [bốn paradigm mới](../../../../../src/infra/Conjugation/Data/candidate-20261110.1.rules.json) với 909 paradigm F‑09.

`YcoConjugationCandidateTests` đọc JSON được sinh và chạy các kỳ vọng từ đơn qua adapter và luồng tra từ với repository thử nghiệm. T19 dùng bản JSON đầy đủ để kiểm câu, scan và hợp đồng API trên môi trường cô lập trước khi quyết định kích hoạt.
