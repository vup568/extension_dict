# Độ phủ thể chia YCO — Đối chiếu F‑09

Ngày 2026-11-08, cập nhật 2026-11-10. Phiên bản `yco-gap-20261110.1`. Danh mục T16 đã được rà soát độc lập và chốt làm đầu vào cho T17.

## Phạm vi chứng cứ

- [Manifest nghiệm thu F‑09](../../../.sdd/specs/feat-conjugation-explanation/evidence/acceptance-manifest.json) chốt corpus `f09-expanded-20261013.3` và dataset opt-in `candidate-20261013.1`. Đối chiếu tại checkout xác nhận 12/12 SHA-256 trong manifest, 60 ô, 412 ca và 17 fixture. Dataset có 909 biến thể tham số hóa thuộc 115 rule ID. Fixture từ điển là dữ liệu tổng hợp.
- ID ca và đáp án baseline dưới đây tra trong [cases.json](../../../.sdd/specs/feat-conjugation-explanation/corpus/expanded-20261013.3/cases.json). [Danh mục .3](../../../.sdd/specs/feat-conjugation-explanation/corpus/expanded-20261013.3/coverage-catalog.md) liệt kê từng ô. [Hồ sơ nguồn .3](../../../.sdd/specs/feat-conjugation-explanation/corpus/expanded-20261013.3/sources.json) ghi tài liệu Irodori, dữ liệu tự biên soạn và giới hạn sử dụng.
- [Khảo sát Yomitan đã lưu](../../../docs/yomitan-reuse-research.md) và [báo cáo đối chiếu](../../reports/261025-yomitan-adoption-comparison.md) ghi các điểm tham khảo tại revision `d3c54c10264673eca0d66b970ef74cd5c8b048e3`. Phiên này dựa trên danh mục đã lưu vì checkout tham khảo nằm ngoài phạm vi truy cập. Dùng các đường dẫn transform/test để định vị họ quy tắc khi có quyền đọc. Đáp án được xét theo nguồn ngôn ngữ và người duyệt.

## Ma trận baseline

| Nhóm YCO | Đã có ca F‑09 cho từ đơn | Ranh giới cần giữ hoặc kiểm thêm |
|---|---|---|
| Động từ thường/lịch sự × khẳng định/phủ định × thời, thể て | V00–V09. `V00-P1` tra nguyên dạng 食べる theo exact-first. `V01-P1`–`V08-P1` có 食べます, 食べました, 食べません, 食べませんでした, 食べた, 食べない, 食べなかった và 食べて → 食べる (`v1`). Các ca 書く/読む phủ godan. | Lookup exact thắng ứng viên chia thể. Ca từ đơn kèm dấu câu như `V20-N1` phải từ chối mẫu phủ toàn input. |
| `する` / `来る` và biến âm `行く` | `V07-IRR-suru` しなかった → する (`vs-i`), `V07-IRR-kuru` 来なかった → 来る (`vk`), `V08-ONBIN-行く` 行って → 行く (`v5k-s`). Các biến thể `*-IRR-suru` / `*-IRR-kuru` nằm trong các ô tương ứng. | `行った` thiếu ca riêng trong .3. Dataset có `conjugation:verb-past` v2 dành cho `required_base=行く` và runtime đã đo được nhánh này. JMdict có mục riêng cho `行う`, cũng có thể viết quá khứ là `行った`. |
| Khả năng, bị động, kính ngữ | V10, V11, V21. `AMB-01` 食べられる → 食べる (`v1`) giữ khả năng, bị động và tôn kính. `V11-P2` 書かれる → 書く giữ bị động và tôn kính. | `V10-P1` 書ける → 書く chỉ cho khả năng khi thiếu mục exact, cấm tôn kính. `EXACT-DERIVED-書ける` giữ mục từ exact và không thêm phân tích. |
| Sai khiến, sai khiến-bị động và sai khiến-khả năng | V12–V13. `V12-P1` 食べさせる → 食べる. `V13-P1` 食べさせられる → 食べる giữ cả sai khiến-bị động và sai khiến-khả năng. | Không gộp hoặc chọn một cách hiểu theo thứ tự. Kính ngữ phức hợp đệ quy nằm ngoài phạm vi corpus .3. |
| Điều kiện, ý chí, mệnh lệnh, mong muốn | V14–V19. `V14-P2` 書けば → 書く; `V15-P1` 食べたら → 食べる; `V16-P2` 書こう → 書く; `V17-P1` 食べろ → 食べる; `V19-P1` 食べたい → 食べる. Có ca しろ và 来い. | `V17-N1` giữ ranh giới dấu câu ở matcher từ đơn. Kiểu mệnh lệnh văn viết よ/せよ nằm ngoài corpus .3. |
| Tính từ い, gồm `いい/よい` | I00–I11. `I03-P1` 高くなかった → 高い (`adj-i`). `I08-P1`, `I09-P1`, `I10-P1` lần lượt kiểm 高くて, 高く, 高ければ. `I01-IRR-ii`, `I03-IRR-ii` kiểm よかった, よくなかった → よい với fixture `adj-i`. | `I09-N1` きれいに được nhận theo tính từ な, cấm `adjective-adverbial` của い. Mẫu よい chấp nhận `adj-i` và alias `i_adjective`; cần đối chiếu với POS và cặp viết–đọc trong JMdict đang dùng, nhất là nếu sense thực chỉ có `adj-ix`. |
| Tính từ な và danh từ với copula | A00–A13, N00–N11. `A04-ALT-dewa-静か` 静かではなかった → 静か (`adj-na`); `N06-P1` 学生でした → 学生 (`n`) + copula lịch sự quá khứ. Bao phủ hai hệ だ/です, じゃ/では, なら/だったら và で nối. | `A06-N1` cấm quy tắc な cho 学生でした; `N06-N1` cấm quy tắc danh từ cho 静かでした. `A09-P1` 静かな và `A10-P1` 静かに hợp lệ; `A09-N1` 学生な và `A10-N1` 学生に không được gán quy tắc な/に. |
| Chuỗi `ている`, `ていた`, `ていない` | `V20-P1`, `V20-C-plain_past-P1` và `V20-C-plain_negative-P1` lần lượt kiểm 食べている, 食べていた và 食べていない → 食べる khi phân tích cả input từ đơn. | `ている` đã đo trong câu thật, gồm ca lặp. Ca `ていた` và `ていない` trong câu vẫn cần kiểm thêm. Giữ kiểm hồi quy F09 cho cả ba dạng. |
| Chuỗi `てしまう`, `てしまった` | **Thiếu mẫu giải thích chuyên biệt** trong [dataset hiện tại](../../../src/infra/Conjugation/Data/candidate-20261013.1.json). Cả hai dạng 食べてしまう và 食べてしまった đều vắng trong corpus .3. | Đã đo `食べてしまった` trong câu và có lemma `食べる` kèm danh sách giải thích trống. Ca không quá khứ và biến thể `で` cần kiểm thêm. |
| UTF-16, từ lặp, giới hạn cặp viết–đọc | [Test sentence vocabulary](../../../tests/unit/SentenceVocabularyUseCaseTests.cs) có fixture lặp 食べました sau emoji và fallback lemma. [Test lookup](../../../tests/unit/LookupConjugationCandidateTests.cs) và [test phép chiếu từ điển](../../../tests/unit/DictionaryReadCorrectnessTests.cs) kiểm các cặp sense/reading/written form bị giới hạn. | Đã đo hai span selection của YCO-AC06. Ca scan và span con cho quy tắc mới vẫn mở. Giữ các khoảng nửa mở trên câu và span con tương đối trên surface. |

## Bộ đáp án cho T17

Ký hiệu `J:<id>` trỏ tới `dictionary:jmdict:<id>`. Các ID, POS và cặp viết–đọc dưới đây được đối chiếu với JMdict đã pin của bản Nhật–Anh. Span đều là khoảng nửa mở UTF‑16. Các đáp án ngôn ngữ dựa vào nguồn độc lập, cột runtime phía dưới ghi riêng những gì backend thực sự đã trả.

| Ca | Kỳ vọng cần chốt | Ca âm hoặc giới hạn | Nguồn |
|---|---|---|---|
| `YCO-IRR-01` · `行った` | Fixture có `行く`／`いく` (`J:1578850`, `v5k-s`) phải nhận dạng quá khứ thường `[0,3)`. | Từ `行く` loại ứng viên biến âm đều `行いた`. Với chữ `行った` đứng riêng, giữ khả năng `行う`／`おこなう` (`J:1589060`, `v5u`) khi có chứng cứ, và giữ hai canonical ID riêng. | Irodori, Bunkachō |
| `YCO-IRR-02` · `昨日、駅へ行った。` và `昨日、実験を行った。` | Câu đến ga cho cách hiểu `行く` tại `[5,8)`. Câu tiến hành thí nghiệm cho cách hiểu `行う` tại `[6,9)`. | Đây là ca đối chiếu ngôn ngữ. Chọn cách hiểu theo nghĩa cả câu là điều cần kiểm riêng nếu backend có bằng chứng từ tokenizer. | Bunkachō, JMdict |
| `YCO-ASP-01` · `昨日はパンを食べていた。` | `食べていた` `[6,11)` → `食べる`／`たべる` (`J:1358280`, `v1`), mẫu `ている` ở quá khứ thường. | Loại giải thích `てしまう` hoặc phủ định. | F‑09 V20, TUFS, Marugoto |
| `YCO-ASP-02` · `まだパンを食べていない。` | `食べていない` `[5,11)` → cùng mục từ, mẫu `ている` ở phủ định thường không quá khứ. | Loại giải thích khẳng định, quá khứ hoặc `てしまう`. | F‑09 V20, TUFS |
| `YCO-AUX-01` · `食べてしまう` và `食べてしまった` | `食べる` (`J:1358280`, `v1`), `て` + trợ động từ `しまう`. Ca đầu là dạng thường khẳng định không quá khứ, ca sau là dạng thường khẳng định quá khứ. | Loại việc gán chắc chắn tiếc nuối từ dạng chữ. Khi runtime chỉ xác minh được lemma, giữ kết quả tra từ và danh sách giải thích trống. | NINJAL |
| `YCO-AUX-02` · `読んでしまう` và `読んでしまった` | `読む`／`よむ` (`J:1456360`, `v5m`) dùng thể `で`, kiểm cùng hai dạng thời như ca trước. | Loại `読んて` và loại việc ghép cặp đọc/POS từ mục khác. | TUFS, NINJAL |
| `YCO-AUX-03` · `本を箱にしまった。` | `しまった` `[4,8)` dùng nghĩa ngoại động từ “cất” của `しまう` (`J:1305380`, `v5u`, `vt`). | Loại giải thích `Vてしまう` cho occurrence này. Cùng canonical ID có sense trợ động từ riêng. | NINJAL, JMdict |
| `YCO-SPAN-01` · `🐈食べている。食べてしまった。` | Hai occurrence `[2,7)`, `[8,15)` cùng `J:1358280`. Sau khi có quy tắc đã duyệt, span con toàn phần tương ứng `[0,5)`, `[0,7)`. Scan offset 6 hoặc 14 chọn đúng occurrence tương ứng. | Offset 1 ở nửa sau surrogate và offset 7/15 ở dấu câu phải bị từ chối. Loại việc dùng offset cả câu cho span con. | F‑09 V20, NINJAL, hợp đồng scan |
| `YCO-SPAN-02` · `食べて。しまった` và `食べている。食べている。` | Ca dấu câu kiểm hai vị trí `[0,3)`, `[4,8)` riêng. Ca lặp kiểm `[0,5)`, `[6,11)` cùng mục từ. | Loại ghép qua `。` và loại gộp vì trùng surface. `しまった` đứng riêng có thể là thán từ (`J:1005600`, `int`). | NINJAL, F‑09 V20, hợp đồng scan |

Ranh giới đã rà soát là dạng thường khẳng định của `Vて/で + しまう`, gồm không quá khứ và quá khứ, với mẫu `v1` và `v5m` trong bảng. Với `食べてしまった`, phần `しまっ[3,6)` và `た[6,7)` thể hiện quá khứ của trợ động từ. Nội dung giải thích tiếng Việt đề xuất là “Mẫu て/で + しまう nhấn vào việc hành động đi đến hoàn tất hoặc kết quả xảy ra. Tùy ngữ cảnh, kết quả có thể ngoài ý muốn.” Các lớp godan khác, dạng rút gọn, lịch sự, phủ định và chuỗi đệ quy cần bộ ca riêng.

Lượt rà soát độc lập của phiên ngày 10/11/2026 đã kiểm các diễn giải ngôn ngữ, 9 ID ca, các span UTF‑16 và 6 mục JMdict. Kết quả rà soát sửa ca âm `行った` và xác nhận ranh giới dấu câu, lemma và sense. T17 dùng danh mục này để tạo corpus phiên bản riêng. T18 chỉ triển khai quy tắc sau khi corpus T17 đã qua duyệt.

### Nguồn độc lập bổ sung ngày 2026-11-10

- Bảng thể タ của Irodori xếp `行った` vào ngoại lệ của nhóm động từ đuôi `く`, hỗ trợ đáp án `行った → 行く` trong ca kiểm bổ sung. [The Japan Foundation — Irodori Grammar Worksheets Answer](https://www.irodori.jpf.go.jp/assets/data/resources/Grammar_Worksheets_Answer.pdf)
- NINJAL phân biệt cách dùng `てしまう` cho hành động đi đến kết quả với cách dùng có kết quả bất lợi. Phân tích corpus nhận dạng trợ động từ và thời của chuỗi. Diễn giải sắc thái cần ngữ cảnh câu. [NINJAL — Mục しまう, phần 補動](https://mmsrv.ninjal.ac.jp/kamus/data/item2981.html) · [NINJAL — Giáo trình, tr. 24–25](https://repository.ninjal.ac.jp/record/3141/files/kk_nkek_k_unit3.pdf)
- Tài liệu của Cơ quan Văn hóa Nhật Bản ghi nhận `行った` có thể đọc `いった` hoặc `おこなった`. [Bunkachō — Hồ sơ về okurigana, trang 41 của PDF](https://www.bunka.go.jp/tokei_hakusho_shuppan/hakusho_nenjihokokusho/archive/pdf/93728801_03.pdf#page=41)
- Đối chiếu mẫu tiếp diễn, phủ định và biến đổi thời của `ている` theo nguồn giảng dạy. [TUFS — Vています／Vていません](https://www.coelang.tufs.ac.jp/mt/ja/gmod/contents/explanation/044.html) · [Japan Foundation — Marugoto, phần Vていた](https://marugoto.jpf.go.jp/assets/docs/teacher/resource/intermediate1/intermediate1_teaching_topic.pdf#page=154)
- Bảng thể thông thường của TUFS ghi `読む` thành `よんで`. [TUFS — Bảng chia động từ](https://www.coelang.tufs.ac.jp/modules/ja/gmod/courses/c01/lesson11/step1/card/038.html)

Các câu thử trong bảng được biên soạn cho dự án. Nguồn trên dùng làm tài liệu tham khảo và đối chiếu.

### Kết quả runtime trên JMdict và Sudachi ngày 2026-11-10

| Input | Quan sát từ `/api/analysis` v3 |
|---|---|
| `行った` | Giải thích `行った → 行く`, canonical `dictionary:jmdict:1578850`. |
| `よくなかった` | Giải thích `よくなかった → よい`, canonical `dictionary:jmdict:1605820`. |
| `🐈食べている。食べてしまった。` | `食べている` ở `[2,7)` có giải thích. `食べてしまった` ở `[8,15)` tra được `食べる` cùng canonical `dictionary:jmdict:1358280`, với danh sách giải thích trống. |
| `食べて。しまった` | Hai vị trí `[0,3)` và `[4,8)`, mỗi vị trí có mục từ riêng. |
| `食べている。食べている。` | Hai vị trí `[0,5)` và `[6,11)`, cùng giữ giải thích `conjugation:verb-te-iru`. |

Kết quả runtime ghi nhận hành vi của dataset hiện tại. T17 đối chiếu corpus mới với các đáp án đã rà soát trong danh mục T16.
