# JP Reading Platform — Product Requirements Document

## Document Control

| Thuộc tính | Giá trị |
|---|---|
| Product | JP Reading Platform V2 |
| Document type | Product Requirements Document (PRD) |
| Version | `2.1.0` |
| Status | **Approved** |
| Primary language | Vietnamese |
| Secondary content language | English |
| MVP clients | Browser Extension và Web Application |
| MVP browsers | Chrome, Edge, Brave, Firefox |
| Product owner | VuPM |
| Approval date | 2026-08-21 |
| Last updated | 2026-08-21 |

### Change Summary

Phiên bản này tái cấu trúc PRD 2.0.0 nhằm:

- tách product context, scope, business rule, functional requirement và non-functional requirement;
- chuẩn hóa requirement thành các phát biểu atomic có actor, release, priority và acceptance criteria;
- bổ sung glossary, actor catalog, domain/feature map và traceability matrix;
- hợp nhất requirement trùng lặp và cấp ID cho các quy tắc trước đây chưa có ID;
- làm rõ phạm vi English, notes/tags, soft delete, Save Vocabulary và Save Grammar;
- ghi nhận technical baseline đã được chốt trong `AGENTS.md` thay vì tiếp tục xem là open decision;
- bổ sung Conceptual Data Model sau năm clarification có tác động cao về sense, canonical identity, nguồn dữ liệu, release và retention;
- đưa tài liệu về trạng thái chờ Product Owner tái phê duyệt do có thay đổi chuẩn tắc.
- tái phê duyệt ngày 2026-08-21; Export/Anki/Quizlet interoperability được chuyển khỏi MVP.

### Document Authority

`REQUIREMENT.md` là nguồn sự thật cho product behavior và product scope mục tiêu. `.sdd/constitution.md` quản trị các invariant kỹ thuật và governance. `MIGRATION_DECISION.md` quản trị cách xử lý tài sản legacy. Feature `SPEC.md` làm rõ hành vi trong phạm vi requirement đã duyệt nhưng không tự mở rộng product scope.

`README.md` mô tả trạng thái triển khai được kiểm chứng tại từng thời điểm; nó không thay thế PRD này.

---

# 1. Product Context

## 1.1 Vision

JP Reading Platform là nền tảng hỗ trợ người học **đọc, hiểu và lưu lại kiến thức tiếng Nhật ngay trong ngữ cảnh thực tế**. Sản phẩm kết hợp một Browser Extension ít gây gián đoạn với Web Application dành cho tra cứu, quản lý nội dung đã lưu và ôn tập nhanh.

Hệ thống được định vị là **Japanese Reading Assistant + Learning Platform**, không chỉ là một dictionary extension.

## 1.2 Problem Statement

Người học tiếng Nhật thường phải chuyển qua nhiều công cụ để tra từ, tra kanji, tìm dạng gốc, nhận diện ngữ pháp, dịch câu và lưu nội dung. Việc chuyển context làm gián đoạn quá trình đọc và khiến kiến thức vừa gặp khó được đưa vào quy trình học lại.

Luồng giá trị mục tiêu:

```text
Gặp tiếng Nhật
      ↓
Hiểu ngay trong ngữ cảnh
      ↓
Lưu tài nguyên học tập quan trọng
      ↓
Ôn tập nhanh
```

## 1.3 Product Goals

| Goal | Kết quả mong đợi | Chỉ báo nghiệm thu MVP |
|---|---|---|
| G-01 — Reading First | Giảm thao tác và chuyển context khi đọc | Selection hợp lệ mở popup shell theo `PERF-001` |
| G-02 — Linguistic Correctness | Kết quả giữ đúng identity, restriction và occurrence | Corpus correctness đạt các AC của `ID-*`, `VOC-007`, `VOC-008`, `GRM-007`, `GRM-008` |
| G-03 — Shared Semantics | Extension và Web dùng cùng knowledge layer | Contract conformance đạt `API-001` đến `API-004` |
| G-04 — Optional Learning | Tra cứu ẩn danh; đăng nhập chỉ khi lưu dữ liệu cá nhân | Acceptance flow của `AUTH-001` và `AUTH-002` đạt |
| G-05 — Vietnamese First | Người Việt nhận được trải nghiệm chính bằng tiếng Việt | `I18N-001`, `VOC-004`, `CONJ-002` đạt |
| G-06 — Safe Evolution | Provider, client và content có thể thay đổi mà không phá identity | `ARCH-003`, `TRN-002`, `TRN-003`, `ID-001` đạt |

## 1.4 Non-Goals

MVP không nhằm thay thế hệ thống SRS chuyên dụng, không cung cấp full offline dictionary, không xây chatbot gia sư, không cung cấp mobile app, export sang Anki/Quizlet hoặc đồng bộ hai chiều tự động với Anki hoặc Quizlet.

## 1.5 Product Principles

Các principle dưới đây định hướng quyết định; requirement kiểm thử được định nghĩa ở các phần có ID chi tiết.

| ID | Principle | Ý nghĩa |
|---|---|---|
| P-01 | Reading First | Trải nghiệm đọc ưu tiên tốc độ và ít gián đoạn |
| P-02 | Vietnamese First | UI chính và content ưu tiên tiếng Việt; dữ liệu sẵn sàng đa ngôn ngữ |
| P-03 | Server-Authoritative Knowledge | Backend là nguồn sự thật runtime cho knowledge và learning data |
| P-04 | Cross-Browser Architecture | Core behavior không bị khóa vào một browser vendor |
| P-05 | Learning Is Optional | Reading không phụ thuộc account; learning persistence có account |

## Clarifications

### Session 2026-08-18

- Q: Trong Conceptual Data Model, mỗi `Dictionary Sense` nên được biểu diễn như thế nào? → A: `Dictionary Entry` có nhiều `Dictionary Sense`; mỗi Sense giữ part of speech và form/reading restrictions, đồng thời có nhiều localized gloss như `vi` và `en`.
- Q: Khi nhiều nguồn dữ liệu cùng mô tả một từ, kanji hoặc grammar rule, platform nên liên kết chúng thế nào? → A: Một canonical resource có thể liên kết nhiều `Source Record`; mọi mapping/merge liên nguồn cần reviewed rule hoặc editorial decision, không được suy ra chỉ từ written form hoặc reading trùng nhau.
- Q: Canonical ID nên được scope thế nào để Dictionary Entry, Kanji Record, Grammar Rule và Learning Resource không thể va chạm identity? → A: Canonical ID toàn cục có resource type rõ ràng, ví dụ `dictionary:…`, `kanji:…` và `grammar:…`.
- Q: Khi linguistic data được import lại hoặc chỉnh sửa, Canonical Resource nên thay đổi theo lifecycle nào? → A: Canonical ID giữ ổn định; mỗi Knowledge Release là immutable snapshot, và chỉ release đã publish mới trở thành current facts.
- Q: Analysis Interaction và Translation Interaction có nên trở thành bản ghi lưu trữ lâu dài của người dùng không? → A: Chúng là ephemeral mặc định; không có history và không persist raw selected text theo mặc định, ngoài telemetry phi nội dung đã được phép.

---

# 2. Requirement Governance

## 2.1 Normative Language

- **SHALL / SHALL NOT**: bắt buộc để nghiệm thu release đã nêu. Mọi ngoại lệ cần Product Owner phê duyệt và ghi nhận trong SPEC/plan tương ứng.
- **SHOULD / SHOULD NOT**: kỳ vọng mạnh. Có thể sai khác khi có bằng chứng về trade-off và quyết định được phê duyệt.
- **MAY**: khả năng được phép, không phải điều kiện bắt buộc của release.
- Các câu chứa từ khóa viết hoa trên chỉ mang tính chuẩn tắc khi nằm trong một requirement có ID hoặc trong mục governance này.

## 2.2 Priority

| Priority | Ý nghĩa |
|---|---|
| Must | Điều kiện bắt buộc của release |
| Should | Có giá trị cao; sai khác cần được giải trình |
| Could | Tùy chọn, chỉ thực hiện khi không ảnh hưởng Must/Should |

## 2.3 Requirement Lifecycle

Mỗi requirement có một ID duy nhất. Khi requirement thay đổi về nghĩa, lịch sử thay đổi và ảnh hưởng tới SPEC, test, migration phải được review. Summary, Definition of Done và roadmap chỉ tham chiếu ID; chúng không tạo thêm hành vi chuẩn tắc.

---

# 3. Actors and Roles

| Actor | Type | Mục tiêu / trách nhiệm |
|---|---|---|
| Anonymous Reader | Primary user | Đọc, tra cứu, phân tích và dịch mà không cần account |
| Authenticated Learner | Primary user | Lưu, quản lý và review learning resources |
| Product Owner | Governance | Phê duyệt scope, requirement và quyết định sản phẩm còn mở |
| Browser Host Page | External system | Cung cấp DOM, selection và page lifecycle cho Extension |
| Browser Extension | Client system | Tích hợp trang, hiển thị popup và gọi Backend |
| Web Application | Client system | Tra cứu, quản lý library và review |
| Backend | Authoritative system | Điều phối knowledge, analysis, identity, learning và provider |
| Translation Provider | External provider | Cung cấp dịch vụ dịch qua boundary do Backend quản lý |
| Tokenizer Sidecar | External runtime component | Cung cấp tokenization/morphology qua adapter chuẩn hóa |
| Authentication Provider | External/provider role | Cung cấp capability identity nếu được lựa chọn |
| Data Operator / Linguistic Editor | Operational actor | Chuẩn bị, kiểm tra, xuất bản và chỉnh sửa linguistic data |
| System Operator | Operational actor | Vận hành, quan sát, xử lý abuse và incident |
| Anki / Quizlet | External destination | Post-MVP: nhận dữ liệu do user export; không đồng bộ hai chiều tự động |
| Delivery Team | Delivery actor | Triển khai và xác minh các solution constraint đã duyệt |

---

# 4. Glossary

| Thuật ngữ | Định nghĩa sử dụng trong PRD |
|---|---|
| Analysis interaction | Một thao tác phân tích ephemeral có identity riêng, gắn với input/selection và trạng thái loading/result/error tương ứng; không phải user history mặc định |
| Base form / dictionary form | Dạng từ điển chuẩn được suy ra từ một token hoặc word unit biến đổi |
| Cache | Bản sao có thời hạn của dữ liệu/kết quả; không phải nguồn canonical |
| Canonical identifier | ID ổn định, toàn cục và có resource type do platform quản lý cho một linguistic resource, ví dụ `dictionary:…`; không phụ thuộc chuỗi hiển thị đã localized |
| Canonical linguistic resource | Dictionary Entry, Kanji Record, Grammar Rule hoặc resource khác có canonical identifier ổn định và có thể liên kết nhiều Source Record |
| Dictionary entry | Mục từ canonical gồm written form, reading, sense và metadata liên quan |
| Dictionary sense | Đơn vị ngữ nghĩa độc lập theo ngôn ngữ thuộc một Dictionary Entry; giữ part of speech, form/reading restrictions và các localized gloss áp dụng |
| Dictionary match provenance | Quan hệ giải thích input khớp written form, reading, sense và restriction nào |
| Exact lookup | Tra cứu theo form/reading đã chuẩn hóa mà không bao gồm fuzzy search |
| Graceful failure | Trạng thái lỗi rõ ràng, có hành động phù hợp, không phá host page và không hiển thị dữ liệu stale như dữ liệu hiện tại |
| Grammar occurrence | Một lần xuất hiện cụ thể của canonical grammar rule trong analyzed text |
| Japanese text | Văn bản có kana hoặc ideograph hợp lệ dùng trong tiếng Nhật theo quy tắc Unicode-aware đã được kiểm thử; không suy luận chỉ từ một block Unicode rộng |
| Knowledge release | Snapshot bất biến của linguistic data đã được validate, có provenance và có thể được publish làm current knowledge cho Backend |
| Learning resource | Saved Vocabulary hoặc Saved Grammar thuộc một authenticated user |
| Localized gloss | Một diễn đạt của cùng Dictionary Sense theo language tag, ví dụ `vi` hoặc `en`; không tạo một Sense mới |
| Source record | Fact/record từ một source release cụ thể, giữ source identity, version và provenance riêng; không tự đồng nghĩa với canonical resource |
| Editorial mapping | Quyết định được review liên kết Source Record với canonical resource hoặc xác nhận equivalence giữa các record liên nguồn |
| Meaningful overlap | Hai grammar/expression occurrence hợp lệ có span giao nhau và đều cần được biểu diễn |
| Normalized token | Token theo contract nội bộ trung lập provider, không để raw provider label rò rỉ sang domain/client |
| Ordinary logs | Application/operational logs mặc định dùng cho vận hành; không bao gồm một luồng retention đặc biệt đã được phê duyệt riêng |
| Persist | Lưu vượt quá vòng đời xử lý request hiện tại vào database, file, log hoặc storage bền vững |
| Raw selected text | Nội dung selection chưa được redaction/aggregation, có thể chứa dữ liệu nhạy cảm từ trang người dùng |
| Release | Mốc phạm vi: Foundation, MVP hoặc Post-MVP |
| Resource type | Loại canonical resource, ví dụ dictionary, kanji hoặc grammar; là một phần semantics của canonical identifier |
| Resource revision | Phiên bản facts/presentation/provenance của một Canonical Linguistic Resource trong một Knowledge Release; không thay canonical identifier |
| Ruby-safe extraction | Bóc tách base text nhưng loại nội dung annotation từ `rt` và `rp` khỏi linguistic input |
| Selection ổn định | Selection hợp lệ không còn thay đổi trong debounce window đang áp dụng |
| Sense restriction | Ràng buộc upstream quyết định sense áp dụng cho written form/reading nào |
| Soft delete | Xóa khỏi active user views nhưng giữ bản ghi phục hồi được ở Backend theo retention policy |
| Span / offset | Cặp vị trí xác định một occurrence trong analyzed text theo một convention chung; convention cụ thể nằm ở `OD-003` |
| Stale response | Kết quả/error của interaction cũ đến sau khi interaction mới đã trở thành current |
| Structured error | Lỗi machine-readable có tối thiểu `error_code`, `message`, `request_id` và retry metadata khi phù hợp |
| Translation interaction | Thao tác dịch ephemeral cho một Analysis Interaction và target language; không phải user history mặc định |
| Vietnamese-first | UI MVP dùng tiếng Việt; content tiếng Việt được ưu tiên theo availability/quality rule |
| Word unit | Đơn vị từ vựng do normalized analysis biểu diễn; không đồng nhất bắt buộc với một DOM node |

---

# 5. Domain and Feature Map

| Domain | Primary actors | MVP features | Post-MVP references |
|---|---|---|---|
| Reading Capture | Anonymous Reader, Browser Host Page | Japanese detection, selection, ruby-safe extraction, debounce | Extended offline coordination |
| Popup Experience | Anonymous Reader, Extension | Immediate shell, loading, failure, retry | Enhanced personalization |
| Japanese Analysis | Reader, Backend, Tokenizer Sidecar | Normalization, morphology, base form, unified analysis | Provider improvements |
| Vocabulary | Reader, Learner | Exact lookup, reading, meaning, POS, provenance, save | Notes/tags |
| Kanji | Reader | Per-character information | Content expansion |
| Grammar | Reader, Learner | Detection, N5–N4, occurrence identity, library, save | N3–N1 expansion |
| Conjugation | Reader | Form explanation, Vietnamese content | English explanation expansion |
| Translation | Reader, Translation Provider | On-demand JP→VI và JP→EN | Optional auto-translate setting |
| Identity & Access | Reader, Learner | Anonymous reading, shared account identity, authorization | Account linking refinements |
| Learning Library | Learner | Save/list/delete vocabulary và grammar | Notes, tags, richer organization |
| Review | Learner | Quick Review, reveal | Advanced SRS, statistics |
| Export | Learner, Anki/Quizlet | — | UTF-8 CSV/TSV compatible export và two-way synchronization |
| Knowledge Operations | Data Operator | Provenance, reproducible pipeline, controlled data | Additional sources/editorial tooling |
| Platform/API | Extension, Web, Backend | Shared contracts, validation, structured error | Version lifecycle refinements |
| Reliability & Safety | All actors | Timeout, stale protection, cache, rate limit, privacy, security | Graceful offline mode |
| Localization | Reader | Vietnamese UI, bilingual translation, multilingual model | Full English UI |

---

# 6. Conceptual Data Model

Mô hình này xác định business entities, identity, ownership, relationship và lifecycle dùng chung cho mọi feature. Đây không phải database schema, API DTO hoặc quyết định ORM; feature SPEC và plan có thể chọn biểu diễn kỹ thuật khác nhau miễn là giữ các semantics bên dưới.

## 6.1 Model Principles

- Canonical linguistic knowledge do platform sở hữu; client chỉ tiêu thụ bản chiếu qua shared contract.
- Canonical identifier toàn cục có resource type và không thay đổi chỉ vì localized content, source facts hoặc revision thay đổi.
- Source facts, normalized facts, derived values và editorial decisions luôn phân biệt được.
- Published Knowledge Release là snapshot bất biến; chỉ controlled publication mới thay current facts.
- User-owned learning data là private và soft-delete; raw selected text không mặc định trở thành persistent model.
- Analysis/translation interaction là ephemeral state, không phải history feature của MVP.

## 6.2 Entity Catalog

| Entity | Identity and core meaning | Ownership and lifecycle |
|---|---|---|
| Canonical Linguistic Resource | Abstract resource có global typed canonical identifier; các concrete type trong MVP gồm Dictionary Entry, Kanji Record và Grammar Rule | Platform-owned; identity ổn định qua Resource Revision theo `ID-001`, `ID-005`, `ID-006` |
| Dictionary Entry | Mục từ canonical gồm một hoặc nhiều Written Form, Reading và Dictionary Sense | Platform-owned; thuộc một hoặc nhiều Knowledge Release thông qua revision |
| Written Form | Dạng biểu ký/orthography của một Dictionary Entry, ví dụ `食べる` | Source/normalized fact; không tự là identity của toàn entry |
| Reading | Dạng kana có thể áp dụng cho Dictionary Entry hoặc một subset form/sense | Source/normalized fact; không tự chứng minh equivalence với reading cùng chuỗi ở entry khác |
| Dictionary Sense | Đơn vị ngữ nghĩa language-neutral thuộc một Dictionary Entry; có sense key ổn định trong parent entry | Có một hoặc nhiều Localized Gloss và Sense Applicability; MVP Learning Reference vẫn target Dictionary Entry theo `LEARN-002` |
| Localized Gloss | Diễn đạt theo một language tag của Dictionary Sense, ví dụ `vi` hoặc `en` | Có source/editorial provenance và review status; không tạo Sense mới |
| Sense Applicability | Quan hệ giữa Dictionary Sense với Written Form và/hoặc Reading được phép | Giữ restriction để không hiển thị nghĩa cho form/reading không tương thích theo `VOC-007`, `VOC-008` |
| Kanji Record | Canonical record cho một character kanji, gồm source facts và derived/approximate values có phân biệt | Platform-owned canonical resource; character không đồng nghĩa với một Dictionary Entry |
| Grammar Rule | Canonical grammar pattern có localized meaning, formation, examples, matcher metadata và provenance | Platform-owned canonical resource, versioned independently from matching engine |
| Grammar Occurrence | Một match cụ thể của Grammar Rule trong Analyzed Text với span/offset | Ephemeral result của Analysis Interaction; không bị collapse khi lặp/overlap |
| Source Manifest | Provenance của một dataset release: publisher, version, license, attribution, transformation, checksum và validation evidence | Platform-owned; là evidence bắt buộc cho Knowledge Release |
| Source Record | Một fact/record từ Source Manifest cụ thể | Liên kết tới Canonical Linguistic Resource qua Editorial Mapping khi equivalence đã được review |
| Editorial Mapping | Quyết định review link/merge/split/retire giữa Source Record và Canonical Linguistic Resource | Platform-owned, auditable; không được suy ra chỉ bằng form/reading trùng |
| Resource Revision | Facts, presentation hoặc provenance của một Canonical Linguistic Resource trong một Knowledge Release | Immutable sau publish; không đổi canonical identifier |
| Knowledge Release | Snapshot bất biến của source records, revisions, mappings và validation đã được publish | Platform-owned; có identity/version riêng và một trạng thái current rõ ràng |
| Analysis Interaction | Một thao tác phân tích với request identity, normalized input và result/error/partial-state hiện hành | Ephemeral; raw selected text không persist mặc định theo `PRIV-006` |
| Translation Interaction | Một thao tác dịch gắn với Analysis Interaction và target language | Ephemeral; chỉ khởi tạo sau explicit user action trong MVP |
| User Account | Principal xác thực đại diện cho một learner trên Extension và Web | User-owned identity; detailed provider/session lifecycle thuộc `OD-002` |
| Learning Reference | Reference riêng tư của User Account tới một Canonical Linguistic Resource, kèm saved timestamp và soft-delete state | User-owned; MVP target Dictionary Entry hoặc Grammar Rule, không dùng localized text làm identity |

## 6.3 Conceptual Relationships

| Relationship | Cardinality and meaning |
|---|---|
| Knowledge Release → Source Manifest → Source Record | Một Knowledge Release dùng một hoặc nhiều Source Manifest; mỗi manifest có một hoặc nhiều Source Record từ một source version cụ thể |
| Canonical Linguistic Resource ↔ Source Record | Quan hệ many-to-many được biểu diễn qua Editorial Mapping; một canonical resource có thể tổng hợp nhiều record đã review |
| Dictionary Entry → Written Form / Reading / Dictionary Sense | Một entry có nhiều form, reading và sense; mỗi Sense thuộc đúng một entry |
| Dictionary Sense → Localized Gloss | Một Sense có zero hoặc nhiều gloss theo language tag; Vietnamese và English gloss biểu diễn cùng semantic sense |
| Dictionary Sense ↔ Written Form / Reading | Sense Applicability biểu diễn zero hoặc nhiều ràng buộc; absence of a relation không được suy diễn là compatibility |
| Grammar Rule → Grammar Occurrence | Một rule có thể tạo nhiều occurrence trong cùng Analysis Interaction, kể cả repeated hoặc overlap |
| Analysis Interaction → Translation Interaction | Một analysis có thể có zero hoặc nhiều translation interaction theo target language/user action; tất cả là ephemeral |
| User Account → Learning Reference → Canonical Linguistic Resource | Một user có nhiều Learning Reference; mỗi reference target đúng một typed canonical identifier và chỉ owner được truy cập |

## 6.4 Ownership, Retention and Publication

| Data class | Owner | Persistence and lifecycle |
|---|---|---|
| Canonical knowledge | Platform | Chỉ thay đổi qua reviewed Knowledge Release; historical release, provenance và mapping được giữ để truy vết |
| User learning data | Authenticated Learner | Private by default; active views lọc soft-deleted records; retention/restore/purge là `OD-013` |
| Interaction state | Current client/request | Ephemeral; không có user history và không persist raw selected text mặc định |
| Cache | Platform hoặc client boundary | Bounded copy có version/status; không là canonical store và không thay ownership/lifecycle của knowledge |
| Operational telemetry | System Operator | Chỉ non-content metadata theo `PRIV-002`, `PRIV-003`; không tái tạo selected content |

## 6.5 Model Invariants and MVP Boundary

- Dictionary results phải giữ liên kết từ matched input tới Written Form, Reading, Dictionary Sense và Sense Applicability phù hợp.
- Localized Gloss có thể được thêm hoặc sửa mà không thay canonical identity của Dictionary Sense hay Dictionary Entry.
- Form/reading giống nhau ở hai Source Record không đủ để merge; chỉ Editorial Mapping được review mới xác nhận equivalence.
- Published Knowledge Release không bị sửa tại chỗ. Release mới có thể thay current facts, còn canonical identity và historical provenance vẫn truy vết được.
- MVP Saved Vocabulary target canonical Dictionary Entry; MVP Saved Grammar target canonical Grammar Rule. Việc lưu riêng một Dictionary Sense là capability sau này và cần SPEC riêng.
- Grammar Occurrence, Analysis Interaction và Translation Interaction không được dùng làm Learning Reference hoặc persistent user history trong MVP.
- Detailed schema, exact sense key representation, review workflow, release storage và source-specific mapping algorithm vẫn là design concern của feature SPEC/plan.

---

# 7. Scope and Release Boundaries

## 7.1 MVP In Scope

- Browser Extension: selection popup, vocabulary, kanji, grammar, conjugation, on-demand translation, Save Vocabulary và Save Grammar.
- Web Application: Japanese search/analyze, dictionary, kanji, Grammar Library, translation, My Vocabulary, My Grammar và Quick Review.
- Backend: shared analysis, knowledge services, translation orchestration, authentication boundary, learning persistence, rate limiting, caching và structured API contracts.
- Linguistic coverage: current reviewed N5–N4 grammar knowledge, JMdict/KANJIDIC2-derived capabilities sau khi đáp ứng provenance/licensing.

## 7.2 MVP Out of Scope

- full N3/N2/N1 grammar corpus;
- full English UI;
- notes và tags cho saved resources;
- export sang Anki hoặc Quizlet;
- automatic Anki/Quizlet bidirectional synchronization;
- full offline dictionary hoặc offline translation engine;
- offline save queue;
- advanced SRS, statistics và personalized review;
- AI tutor/chatbot, social/community features;
- mobile app và Safari;
- microservice architecture;
- paid proprietary dataset như dependency bắt buộc.

---

# 8. Approved Technical Baseline

Chi tiết solution design thuộc Constitution, ADR, SPEC và plan. Các constraint dưới đây được đưa vào PRD để loại bỏ trạng thái “technology undecided” đã lỗi thời.

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| ARCH-001 | Delivery Team | Foundation | Must | Backend **SHALL** sử dụng .NET 10 và C# 14. | AC-ARCH-001: Backend build manifest target đúng .NET 10/C# 14. |
| ARCH-002 | Backend | Foundation | Must | Backend **SHALL** là runtime authority cho canonical dictionary, kanji, morphology, grammar analysis, translation orchestration và persistent learning data. | AC-ARCH-002: Extension/Web không có competing canonical store; semantic conformance dùng cùng Backend capability. |
| ARCH-003 | Backend, Tokenizer Sidecar | Foundation | Must | Native tokenizer/NLP **SHALL** chạy trong sidecar Go hoặc Python tách khỏi .NET Backend process. | AC-ARCH-003: deployment architecture và integration fixture chứng minh tokenizer chạy qua sidecar boundary. |
| ARCH-004 | Delivery Team | Foundation | Must | Browser Extension và Web Application **SHALL** sử dụng React 19, TypeScript và Vite. | AC-ARCH-004: client build manifests target đúng React 19/TypeScript/Vite. |
| ARCH-005 | Delivery Team, Backend | Foundation | Must | Primary relational database **SHALL** là PostgreSQL 16. | AC-ARCH-005: runtime configuration và production-like smoke test dùng PostgreSQL 16. |
| ARCH-006 | Delivery Team, Backend | Foundation | Must | Backend integration tests có hành vi phụ thuộc database **SHALL** chạy với PostgreSQL qua Docker Testcontainers. | AC-ARCH-006: integration suite khởi tạo PostgreSQL container và kiểm tra query/migration trên production-equivalent provider. |
| ARCH-007 | Delivery Team, Backend | Foundation | Must | Relational database schema **SHALL** được quản lý bằng EF Core Migrations. | AC-ARCH-007: database schema có thể tái tạo từ migration history đã version-control. |
| ARCH-008 | Backend, Tokenizer Sidecar | Foundation | Must | Backend **SHALL** giao tiếp với Tokenizer Sidecar qua provider-neutral adapter tại Infrastructure boundary. | AC-ARCH-008: provider contract test chạy qua adapter và Domain chỉ nhận normalized token representation. |
| ARCH-009 | Backend | Foundation | Must | Domain layer **SHALL NOT** tham chiếu native tokenizer package hoặc provider-specific token type. | AC-ARCH-009: architecture dependency test không phát hiện tokenizer/provider dependency trong Domain. |

---

# 9. Functional Requirements

## 9.1 Reading Capture, DOM and Async

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| JPN-001 | Browser Extension | MVP | Must | Japanese detector **SHALL** chấp nhận Japanese text hợp lệ, gồm supplementary CJK ideographs dùng trong tiếng Nhật, và không phân loại Hangul, emoji, musical symbols hoặc unrelated scripts là Japanese chỉ vì broad code-point range. | AC-JPN-001: provider-neutral Unicode corpus pass 100%, gồm kana, `𠮟`, mixed Latin, Hangul, emoji và supplementary symbols. |
| DOM-001 | Browser Extension, Browser Host Page | MVP | Must | Khi trích text từ DOM, Extension **SHALL** giữ base text và loại nội dung `rt`/`rp` khỏi linguistic input. | AC-DOM-001: ruby/nested-DOM fixtures tạo đúng base text, không chứa furigana annotation. |
| EXT-001 | Anonymous Reader, Extension | MVP | Must | **WHEN** user tạo selection có Japanese text hợp lệ, Extension **SHALL** tự mở popup mà không yêu cầu click icon/hotkey. | AC-EXT-001: selection hợp lệ mở popup một lần trên bốn browser mục tiêu. |
| EXT-002 | Anonymous Reader, Extension | MVP | Must | **WHEN** selection không chứa Japanese text hợp lệ, Extension **SHALL NOT** mở analysis popup. | AC-EXT-002: non-Japanese corpus không kích hoạt popup. |
| EXT-003 | Anonymous Reader, Extension | MVP | Must | **WHEN** selection hợp lệ ổn định, Extension **SHALL** render popup shell và loading state trước khi Backend hoàn thành. | AC-EXT-003: UI test quan sát shell/loading trước response; latency được đo theo `PERF-001`. |
| EXT-004 | Extension | MVP | Should | Extension **SHOULD** debounce selection trong khoảng baseline 250–400 ms trước analysis request. | AC-EXT-004: rapid selection fixture phát tối đa một request cho selection cuối; giá trị thực tế được ghi trong plan benchmark. |
| EXT-005 | Extension | MVP | Should | Khi interaction mới thay thế interaction đang xử lý, Extension **SHOULD** hủy request cũ nếu transport hỗ trợ cancellation. | AC-EXT-005: rapid-selection fixture quan sát cancellation attempt; correctness vẫn đạt `ASYNC-002` khi cancellation thất bại. |
| EXT-006 | Extension, Backend | MVP | Must | Extension **SHALL** hỗ trợ một analysis request hợp nhất cho selection thay vì bắt buộc một request riêng cho từng module. | AC-EXT-006: contract test xác nhận vocabulary/kanji/morphology/conjugation/grammar có thể nằm trong một analysis envelope. |
| ASYNC-001 | Extension, Web | MVP | Must | Mỗi selection/input-dependent interaction **SHALL** mang request identity đủ để xác định interaction nào đang current. | AC-ASYNC-001: analysis và translation response được correlate đúng với interaction khởi tạo. |
| ASYNC-002 | Extension, Web | MVP | Must | Response, error hoặc partial result của interaction cũ **SHALL NOT** ghi đè state của interaction mới, kể cả khi cancellation thất bại. | AC-ASYNC-002: A→B với A trả sau luôn giữ UI của B; áp dụng cho analysis, translation và target switching. |

## 9.2 Stable Identity and Vocabulary

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| ID-001 | Backend | Foundation | Must | Dictionary entries, grammar rules và linguistic resources được persist/reference **SHALL** có canonical identifier ổn định. | AC-ID-001: mọi resource trong contract fixture có canonical ID không phụ thuộc localized text. |
| ID-002 | Backend, Data Operator | Foundation | Should | Khi upstream có stable ID, normalized model **SHOULD** giữ quan hệ truy vết tới upstream ID đó. | AC-ID-002: sample JMdict/grammar records resolve được canonical ID về source identity. |
| ID-003 | Backend | MVP | Must | Saved learning resources **SHALL** reference canonical ID thay vì dùng copied presentation text làm primary identity. | AC-ID-003: save/read fixture vẫn resolve đúng sau khi localized display text thay đổi. |
| ID-004 | Backend, Data Operator | Foundation | Must | Canonical resource **SHALL** chỉ liên kết/merge nhiều Source Record qua reviewed mapping hoặc editorial decision; written form hoặc reading trùng nhau **SHALL NOT** tự chứng minh equivalence. | AC-ID-004: same-form/reading collision fixture không tự merge; reviewed mapping fixture giữ provenance của từng source. |
| ID-005 | Backend | Foundation, MVP | Must | Canonical identifier **SHALL** unique trên toàn platform và biểu diễn resource type; generic reference **SHALL** giữ nguyên canonical identifier đó. | AC-ID-005: dictionary/kanji/grammar fixtures không va chạm ID; Learning Reference resolve đúng typed target. |
| ID-006 | Backend, Data Operator | Foundation | Must | Canonical identifier **SHALL** giữ ổn định qua resource revision; split, merge hoặc retirement cần reviewed mapping giữ traceability cho reference hiện có. | AC-ID-006: revision fixture giữ canonical target; approved split/merge fixture giữ mapping từ reference cũ. |
| VOC-001 | Anonymous Reader, Backend | MVP | Must | Backend **SHALL** hỗ trợ exact lookup cho dictionary form hoặc normalized reading/form. | AC-VOC-001: fixtures như `学生` và kana-only lookup trả canonical entries phù hợp. |
| VOC-002 | Anonymous Reader, Backend | MVP | Must | **WHEN** input là dạng biến đổi được hỗ trợ, Backend **SHALL** resolve base form để lookup. | AC-VOC-002: `食べました→食べる`, `高くなかった→高い` pass regression corpus. |
| VOC-003 | Backend, Extension, Web | MVP | Must | Vocabulary result **SHALL** cung cấp Japanese reading khi canonical data có reading áp dụng. | AC-VOC-003: result fixture hiển thị reading gắn đúng written form/restriction. |
| VOC-004 | Backend, Extension, Web | MVP | Must | Vocabulary result **SHALL** ưu tiên Vietnamese meaning đã được reviewed khi meaning đó áp dụng cho matched form/reading. | AC-VOC-004: bilingual fixture đặt nghĩa Việt hợp lệ trước English và không gắn sai restriction. |
| VOC-005 | Backend, Extension, Web | MVP | Should | Vocabulary result **SHOULD** cung cấp English meaning như secondary content khi source có dữ liệu áp dụng. | AC-VOC-005: English gloss xuất hiện với language tag `en` khi available. |
| VOC-006 | Backend, Extension, Web | MVP | Should | Vocabulary result **SHOULD** cung cấp part of speech khi canonical source hỗ trợ. | AC-VOC-006: POS fixture giữ normalized value và quan hệ với sense. |
| VOC-007 | Backend | Foundation, MVP | Must | Dictionary match **SHALL** giữ matched written form, matched reading, applicable sense(s) và relevant upstream restrictions. | AC-VOC-007: alternate/restricted form corpus không mất provenance. |
| VOC-008 | Backend | Foundation, MVP | Must | Backend **SHALL NOT** flatten restriction theo cách gắn meaning vào written form hoặc reading không tương thích. | AC-VOC-008: negative restriction fixtures không trả invalid sense association. |

## 9.3 Kanji

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| KAN-001 | Anonymous Reader, Backend | MVP | Must | **WHEN** analyzed text chứa kanji, system **SHALL** cho phép xem canonical information của từng character. | AC-KAN-001: common/rare/supplementary fixtures trả character identity; các field available gồm reading, meaning, Hán Việt, radical, stroke và JLPT/derived status. |
| KAN-002 | Backend | MVP | Must | Kanji result **SHALL** phân biệt giá trị authoritative source với giá trị derived/approximate. | AC-KAN-002: derived JLPT fixture có provenance/status và không được trình bày như upstream fact. |

## 9.4 Grammar

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| GRM-001 | Anonymous Reader, Backend | MVP | Should | Grammar analysis **SHOULD** dùng sentence context chứa selection khi context có thể được trích an toàn trong privacy boundary. | AC-GRM-001: safe context fixture phân tích câu; unsafe/ambiguous context có fallback không mở rộng dữ liệu gửi đi. |
| GRM-002 | Anonymous Reader, Backend | MVP | Must | Backend **SHALL** phát hiện grammar pattern được hỗ trợ trong Japanese analysis input. | AC-GRM-002: approved positive/negative grammar corpus đạt expected result. |
| GRM-003 | Backend, Data Operator | MVP | Must | MVP **SHALL** giữ coverage của corpus N5–N4 hiện có sau khi corpus được review và version hóa. | AC-GRM-003: release manifest và regression corpus chứng minh toàn bộ approved N5–N4 records được biểu diễn. |
| GRM-004 | Data Operator, Backend | Post-MVP | May | Grammar subsystem **MAY** mở rộng coverage lần lượt tới N3, N2 và N1 bằng knowledge releases được review. | AC-GRM-004: không phải release gate của MVP; mỗi expansion có SPEC riêng. |
| GRM-005 | Data Operator, Backend | MVP | Must | Grammar knowledge **SHALL** là versioned data tách khỏi matching engine. | AC-GRM-005: thêm record grammar fixture không yêu cầu sửa selection, transport hoặc popup architecture. |
| GRM-006 | Backend | MVP | Should | Grammar entry **SHOULD** hỗ trợ canonical ID, pattern, JLPT level, localized meaning, formation, examples, matcher metadata và provenance. | AC-GRM-006: schema/contract fixture validate các field bắt buộc và optional language content. |
| GRM-007 | Backend | Foundation, MVP | Must | Mỗi detected grammar occurrence **SHALL** giữ canonical grammar ID và unambiguous span theo shared offset convention. | AC-GRM-007: occurrence fixture resolve chính xác ID/span trên text có supplementary Unicode. |
| GRM-008 | Backend | Foundation, MVP | Must | Repeated grammar occurrences **SHALL NOT** bị collapse và meaningful overlaps phải biểu diễn được. | AC-GRM-008: repeated/overlap fixtures giữ từng occurrence riêng. |

## 9.5 Conjugation

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| CONJ-001 | Anonymous Reader, Backend | MVP | Should | Khi word unit là dạng biến đổi được hỗ trợ, system **SHOULD** giải thích transformation từ observed form về base form. | AC-CONJ-001: approved verb/adjective/irregular fixtures có base form và explanation segments phù hợp. |
| CONJ-002 | Backend, Extension, Web | MVP | Must | Conjugation explanation trong MVP **SHALL** ưu tiên content tiếng Việt. | AC-CONJ-002: Vietnamese explanation có mặt cho corpus đã được duyệt; missing content có explicit unavailable state. |

## 9.6 Translation

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| TRN-001 | Anonymous Reader, Backend | MVP | Must | System **SHALL** hỗ trợ translation Nhật→Việt và Nhật→Anh. | AC-TRN-001: cả hai target language pass contract/acceptance fixtures. |
| TRN-002 | Backend, Translation Provider | Foundation, MVP | Must | Translation **SHALL** được điều phối qua provider-independent Backend boundary. | AC-TRN-002: client contract không chứa provider credential/protocol; provider substitution chạy cùng normalized contract. |
| TRN-003 | Extension, Web | MVP | Must | Thay translation provider **SHALL NOT** yêu cầu client thay provider-specific behavior. | AC-TRN-003: substitute provider pass client contract không sửa Extension/Web. |
| TRN-004 | Anonymous Reader, Extension, Web | MVP | Must | MVP **SHALL** chỉ request sentence translation sau explicit user action mở phần Dịch hoặc hành động tương đương. | AC-TRN-004: analysis happy path phát zero translation request; mở Dịch phát đúng một request current. |
| TRN-005 | Anonymous Reader | Post-MVP | May | Auto-translation **MAY** được cung cấp sau MVP dưới dạng user setting có default off. | AC-TRN-005: không phải release gate MVP; cần SPEC riêng. |

## 9.7 Web Application

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| WEB-001 | Anonymous Reader, Web | MVP | Must | Web Application **SHALL** cung cấp một input chính cho Japanese word, phrase hoặc sentence. | AC-WEB-001: valid inputs khởi tạo analysis; invalid input nhận structured validation state. |
| WEB-002 | Web, Backend | MVP | Must | Web Application **SHALL** dùng cùng authoritative Backend capabilities với Extension. | AC-WEB-002: semantic conformance fixtures trả cùng canonical identities/provenance. |
| WEB-003 | Anonymous Reader, Web | MVP | Must | Web result **SHALL** có thể trình bày vocabulary, kanji, morphology, conjugation, grammar và on-demand translation khi applicable. | AC-WEB-003: composite fixture hiển thị completed sections và explicit unavailable/partial state. |
| WEB-004 | Anonymous Reader, Web | MVP | Must | Web Application **SHALL** cung cấp Grammar Library cho browse theo JLPT, search và mở grammar detail. | AC-WEB-004: user có thể browse N5/N4, search pattern và mở detail từ result. |
| WEB-005 | Anonymous Reader, Web | MVP | Should | Grammar detail **SHOULD** hiển thị pattern, JLPT level, Vietnamese explanation, formation, examples và reviewed English explanation khi available. | AC-WEB-005: reviewed N5/N4 fixture hiển thị đúng available fields; thiếu English không làm mất Vietnamese detail. |

## 9.8 Shared API Contracts

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| API-001 | Extension, Web, Backend | Foundation | Must | Extension và Web **SHALL** giao tiếp với Backend bằng shared API contracts biểu diễn cùng normalized domain semantics. | AC-API-001: 100% shared conformance fixtures có semantic parity giữa clients. |
| API-002 | Backend | Foundation | Must | Shared contracts **SHALL** được version khi independently deployed consumers có thể cùng tồn tại. | AC-API-002: compatibility test xác nhận supported contract versions và explicit unsupported-version error. |
| API-003 | Extension, Web, Backend | Foundation | Must | Input/output tại untrusted system boundaries **SHALL** được runtime validate. | AC-API-003: malformed request/response fixtures bị từ chối bằng structured error, không đi vào domain flow. |
| API-004 | Backend | Foundation, MVP | Must | Client-visible Backend errors **SHALL** dùng machine-readable structure có tối thiểu `error_code`, `message`, `request_id` và retry metadata khi retryable. | AC-API-004: clients phân biệt validation/auth/authz/rate-limit/timeout/provider/unavailable mà không parse free-form prose. |

## 9.9 Authentication and Authorization

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| AUTH-001 | Anonymous Reader | MVP | Must | Reading, lookup, kanji, grammar analysis và translation **SHALL NOT** yêu cầu login. | AC-AUTH-001: anonymous acceptance flow hoàn tất mọi reading capability trong scope. |
| AUTH-002 | Authenticated Learner | MVP | Must | System **SHALL** yêu cầu authenticated identity trước thao tác persist hoặc synchronize user-owned learning data. | AC-AUTH-002: anonymous Save nhận authentication-required structured response; sau login user có thể tiếp tục theo UX đã được SPEC định nghĩa. |
| AUTH-003 | Authenticated Learner | MVP | Must | Một user account **SHALL** nhận diện cùng user trên Extension và Web Application. | AC-AUTH-003: resource saved từ Extension xuất hiện cho cùng account trên Web, không xuất hiện cho account khác. |
| AUTH-004 | Backend | MVP | Must | Authorization **SHALL** kiểm tra ownership cho mọi read/write learning resource. | AC-AUTH-004: cross-user read/update/delete attempts bị từ chối và không lộ private content. |

## 9.10 Learning Library

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| LEARN-001 | Authenticated Learner | MVP | Must | Learner **SHALL** có thể Save Vocabulary từ Extension và Web. | AC-LEARN-001: cả hai clients tạo/resolve cùng canonical vocabulary reference. |
| LEARN-002 | Backend | MVP | Must | Saved Vocabulary MVP **SHALL** lưu user ownership, canonical dictionary ID và saved timestamp; display fields được resolve từ canonical resource. | AC-LEARN-002: persisted fixture không dùng expression/meaning làm identity; notes/tags không là điều kiện MVP. |
| LEARN-003 | Authenticated Learner | MVP | Must | Learner **SHALL** có thể Save Grammar từ Extension và Web. | AC-LEARN-003: cả hai clients tạo/resolve cùng canonical grammar reference. |
| LEARN-004 | Authenticated Learner, Web | MVP | Must | Web **SHALL** cung cấp My Vocabulary hiển thị active vocabulary resources của current user. | AC-LEARN-004: list chỉ chứa active resources của current user. |
| LEARN-005 | Authenticated Learner, Web | MVP | Must | Web **SHALL** cung cấp My Grammar hiển thị active grammar resources của current user. | AC-LEARN-005: list chỉ chứa active resources của current user. |
| LEARN-006 | Authenticated Learner | MVP | Must | Learner **SHALL** có thể xóa resource của chính mình khỏi active My Vocabulary/My Grammar views. | AC-LEARN-006: sau success response, item không còn trong active list; user khác không thể xóa item. |
| LEARN-007 | Backend | MVP | Must | Save cùng user + canonical resource **SHALL NOT** mặc định tạo duplicate active item. | AC-LEARN-007: repeated/concurrent Save trả cùng logical resource hoặc idempotent outcome. |
| LEARN-008 | Backend | MVP | Must | Delete learning resource **SHALL** dùng soft-delete semantics thay vì physical deletion trực tiếp trong MVP. | AC-LEARN-008: deleted item bị loại khỏi active queries nhưng retained record còn phục hồi được; restore/purge UI không thuộc MVP. |

## 9.11 Quick Review

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| REV-001 | Authenticated Learner, Web | MVP | Must | Web **SHALL** cung cấp Quick Review từ active saved vocabulary và grammar của current user. | AC-REV-001: session không chứa resource của user khác hoặc soft-deleted resource. |
| REV-002 | Authenticated Learner, Web | MVP | Must | Quick Review **SHALL** cho phép user reveal answer/details sau khi xem prompt. | AC-REV-002: answer bị ẩn trước action và xuất hiện sau reveal cho vocabulary/grammar fixtures. |
| REV-003 | Authenticated Learner, Web | MVP | May | Quick Review **MAY** cung cấp basic actions `Again` và `Know`. | AC-REV-003: nếu triển khai, action behavior được định nghĩa trong feature SPEC trước khi lưu progress. |
| REV-004 | Web | MVP | Must | Quick Review MVP **SHALL NOT** phụ thuộc advanced spaced-repetition algorithm. | AC-REV-004: MVP review flow chạy không cần interval/ease/scheduling engine. |

## 9.12 Export and Interoperability

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| EXP-001 | Authenticated Learner | Post-MVP | Must | Post-MVP **SHALL** ưu tiên export compatibility thay vì automatic synchronization. | AC-EXP-001: export flow độc lập external account/linking state. |
| EXP-002 | Authenticated Learner, Anki | Post-MVP | Must | Learner **SHALL** có thể export selected vocabulary/grammar theo format import được vào Anki. | AC-EXP-002: fixture import thành công theo mapping được phê duyệt tại `OD-007`. |
| EXP-003 | Authenticated Learner, Quizlet | Post-MVP | Must | Learner **SHALL** có thể export selected vocabulary/grammar theo format import được vào Quizlet. | AC-EXP-003: fixture import thành công theo mapping được phê duyệt tại `OD-007`. |
| EXP-004 | Authenticated Learner | Post-MVP | Must | Export **SHALL** hỗ trợ UTF-8 CSV hoặc TSV với escaping hợp lệ. | AC-EXP-004: Unicode, quote, delimiter và newline corpus round-trip không làm hỏng Japanese/Vietnamese text. |
| EXP-005 | Anki, Quizlet | MVP | Must | Automatic bidirectional synchronization với Anki/Quizlet **SHALL NOT** nằm trong MVP. | AC-EXP-005: MVP không yêu cầu external sync state, conflict resolution hoặc remote deletion handling. |

## 9.13 Knowledge and Data Operations

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| DATA-001 | Data Operator | Foundation, MVP | Must | Mỗi external production dataset **SHALL** có provenance record gồm source/publisher, exact version/commit, license/attribution, transformation identity, counts và validation result; checksum/rejected records được ghi khi applicable. | AC-DATA-001: production-candidate release thiếu required provenance bị validation gate từ chối. |
| DATA-002 | Product Owner, Data Operator | MVP | Must | Paid proprietary dataset **SHALL NOT** là dependency bắt buộc của MVP. | AC-DATA-002: clean deployment và acceptance corpus chạy không cần paid dataset license. |
| DATA-003 | Data Operator | Foundation, MVP | Must | Canonical dictionary/grammar data **SHALL** được tạo qua reproducible, validated và controlled data pipeline thay vì unreproducible manual production edits. | AC-DATA-003: rebuild từ pinned inputs tạo validated release; rejected records có report. |
| DATA-004 | Data Operator | Foundation, MVP | Must | Candidate data có unresolved source, license, redistribution, identity hoặc quality **SHALL NOT** trở thành canonical production data trước explicit approval. | AC-DATA-004: FVDP/OVDP và candidate tương tự giữ non-canonical status cho tới khi decision record được duyệt. |
| DATA-005 | Data Operator, Backend | Foundation | Must | Published Knowledge Release **SHALL** immutable, addressable và có current-status rõ ràng; chỉ controlled publication mới thay current facts. | AC-DATA-005: cùng release ID luôn tái tạo cùng facts; publish fixture chuyển current release không sửa release cũ. |

---

# 10. Non-Functional and Operational Requirements

## 10.1 Privacy

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| PRIV-001 | Backend | MVP | May | Backend **MAY** nhận selected Japanese text và minimum approved context để thực hiện user-requested analysis. | AC-PRIV-001: request minimization test không gửi page content ngoài input/context cần thiết. |
| PRIV-002 | Backend, System Operator | Foundation, MVP | Must | Ordinary application/operational logs **SHALL NOT** persist raw selected text mặc định. | AC-PRIV-002: privacy corpus kiểm tra logs và không tìm thấy raw fixture text. |
| PRIV-003 | Backend, System Operator | MVP | May | System **MAY** log non-content metadata như timestamp, endpoint, latency, status, request size và correlation ID. | AC-PRIV-003: telemetry hữu ích cho vận hành nhưng không tái tạo selected content. |
| PRIV-004 | Backend, Translation Provider | MVP | Must | Trước khi text được gửi tới third-party translation provider, product **SHALL** cung cấp disclosure phù hợp và chỉ gửi minimum context cần cho action. | AC-PRIV-004: translation flow có disclosure; provider payload inspection đạt minimization. |
| PRIV-005 | Backend | MVP | Must | Saved vocabulary, saved grammar và learning data **SHALL** private theo authenticated owner mặc định. | AC-PRIV-005: unauthenticated/cross-user access corpus bị từ chối. |
| PRIV-006 | Backend, Extension, Web | Foundation, MVP | Must | Analysis Interaction và Translation Interaction **SHALL** là ephemeral mặc định; system **SHALL NOT** tạo user history hoặc persist raw selected text ngoài immediate processing nếu chưa có feature riêng được phê duyệt. | AC-PRIV-006: storage inspection không có interaction history/raw fixture text; approved non-content telemetry vẫn hoạt động. |

## 10.2 Rate Limiting and Abuse Protection

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| RATE-001 | Backend, System Operator | MVP | Must | Production Backend **SHALL** áp dụng rate limiting cho analysis, translation và authenticated state-changing requests. | AC-RATE-001: quota fixtures giới hạn từng class request độc lập. |
| RATE-002 | Backend | MVP | Must | Khi quota bị vượt, Backend **SHALL** trả HTTP 429 bằng structured error và machine-readable retry guidance. | AC-RATE-002: clients đọc retry data không parse message. |
| RATE-003 | Backend | MVP | Should | Anonymous abuse principal **SHOULD** dùng random installation identifier hoặc cơ chế non-invasive tương đương kết hợp IP protection. | AC-RATE-003: identifier không dựa trên fingerprinting attributes; reset/lifecycle được document trong SPEC. |
| RATE-004 | Backend | MVP | Must | System **SHALL NOT** dùng invasive browser fingerprinting để tạo anonymous identifier. | AC-RATE-004: privacy/design review không có fingerprint collection. |

Giá trị benchmark ban đầu để planning: analysis khoảng 60 request/phút/installation, translation khoảng 10 request/phút/installation và IP protection khoảng 300–600 request/phút/IP. Các số này chưa phải release threshold cuối cùng; xem `OD-004`.

## 10.3 Caching

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| CACHE-001 | Backend | MVP | Should | Static/mostly-static dictionary entry, kanji metadata và grammar definition **SHOULD** có cache strategy không làm thay đổi canonical semantics. | AC-CACHE-001: cached/uncached conformance fixture trả cùng identity, provenance và content version. |
| CACHE-002 | Backend | MVP | Should | Backend **SHOULD** tránh lặp expensive computation cho identical normalized input khi cached result còn valid. | AC-CACHE-002: performance test chứng minh cache hit không chạy lại computation; invalidation không trả obsolete release. |

## 10.4 Failure and Network Behavior

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| NET-001 | Extension | MVP | Must | Popup shell **SHALL** xuất hiện dù Backend không reachable. | AC-NET-001: unavailable-backend fixture vẫn render shell. |
| NET-002 | Extension, Web | MVP | Must | Trong khi request current đang xử lý, UI **SHALL** hiển thị loading state tương ứng. | AC-NET-002: loading gắn đúng interaction và kết thúc khi current outcome đến. |
| NET-003 | Extension, Web | MVP | Must | Khi request vượt timeout window, UI **SHALL** chuyển sang explicit timeout/failure state. | AC-NET-003: simulated timeout chuyển state trước hoặc tại configured timeout; không chờ vô hạn. |
| NET-004 | Extension | MVP | Must | Backend/provider failure **SHALL NOT** phá host page, đóng băng popup hoặc làm Extension crash. | AC-NET-004: failure injection giữ host page usable và popup responsive. |
| NET-005 | Extension, Web | MVP | Must | Retryable failure state **SHALL** cung cấp action `Thử lại` và dùng current interaction identity. | AC-NET-005: Retry tạo interaction mới, không cho response cũ overwrite. |
| NET-006 | Backend, Extension, Web | Foundation, MVP | Must | Valid partial result **SHALL** chỉ rõ capability nào thành công và capability nào thất bại. | AC-NET-006: partial fixture không trình bày incomplete analysis như complete success. |

## 10.5 Performance Targets

Các target sau là initial product SLO; threshold cuối cùng cần benchmark và phê duyệt tại `OD-005`.

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| PERF-001 | Extension | MVP | Should | Selection ổn định đến popup shell **SHOULD** đạt p95 ≤ 150 ms trên benchmark profile được phê duyệt. | AC-PERF-001: benchmark report nêu browser/device/sample và p95. |
| PERF-002 | Backend | MVP | Should | Core analysis **SHOULD** đạt p95 ≤ 800 ms khi service warm và trong normal operating profile được phê duyệt. | AC-PERF-002: benchmark report nêu dataset, cache state, concurrency và p95. |
| PERF-003 | Backend, Translation Provider | MVP | Should | Translation **SHOULD** đạt p95 ≤ 2 giây khi provider hoạt động bình thường theo profile được phê duyệt. | AC-PERF-003: provider benchmark report nêu target language, payload và p95. |
| PERF-004 | Extension, Web | MVP | Must | Frontend **SHALL NOT** chờ request vô hạn; baseline request timeout là tối đa 5 giây trước explicit failure state. | AC-PERF-004: timeout test đạt `NET-003`; ngoại lệ endpoint-specific cần decision được duyệt. |

## 10.6 Browser Compatibility and Style Isolation

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| BROWSER-001 | Extension | MVP | Must | Core domain/application logic **SHALL NOT** trực tiếp phụ thuộc browser-specific global APIs. | AC-BROWSER-001: architecture dependency test giới hạn vendor API trong platform boundary. |
| BROWSER-002 | Extension | MVP | Must | Chromium/Firefox differences **SHALL** được xử lý tại browser adapter hoặc entry-point boundary. | AC-BROWSER-002: cross-browser corpus có equivalent semantics trên Chrome, Edge, Brave, Firefox. |
| BROWSER-003 | Extension, Browser Host Page | MVP | Must | Popup UI **SHALL** được cô lập khỏi host-page CSS bằng Shadow DOM. | AC-BROWSER-003: hostile CSS fixtures không thay đổi layout/semantics; focus/keyboard behavior pass accessibility tests. |

## 10.7 Localization

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| I18N-001 | Extension, Web | MVP | Must | MVP user interface **SHALL** dùng tiếng Việt làm primary UI language. | AC-I18N-001: critical MVP journeys có Vietnamese labels/messages. |
| I18N-002 | Backend, Extension, Web | MVP | Must | Translation contract và UI **SHALL** biểu diễn rõ target language bằng language tag `vi` hoặc `en`. | AC-I18N-002: JP→VI và JP→EN fixtures giữ đúng language tag xuyên suốt request, response và presentation. |
| I18N-003 | Backend | Foundation | Should | Localizable domain content **SHOULD** dùng multilingual-capable structure với explicit language tags. | AC-I18N-003: contract/schema fixture phân biệt `vi` và `en` mà không dùng localized string làm identity. |
| I18N-004 | Backend | Foundation | Must | Thêm full English UI/content sau MVP **SHALL NOT** yêu cầu redesign canonical domain identity. | AC-I18N-004: English projection fixture dùng cùng canonical resources. |

English scope được phân biệt như sau: JP→EN translation là bắt buộc trong MVP (`TRN-001`); full English UI nằm ngoài MVP; English lexical/grammar content chỉ hiển thị khi reviewed data có sẵn (`VOC-005`, `WEB-005`).

## 10.8 Security

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| SEC-001 | Backend, Extension, Web | MVP | Must | Production API communication **SHALL** dùng encrypted transport. | AC-SEC-001: production configuration/security test từ chối plaintext transport tại public boundary. |
| SEC-002 | Delivery Team | Foundation, MVP | Must | Secrets và private Backend credentials **SHALL NOT** được hard-code trong source, config committed hoặc client bundle. | AC-SEC-002: secret scan và bundle inspection không phát hiện credential. |
| SEC-003 | Backend | Foundation, MVP | Must | Backend **SHALL** validate untrusted input trước domain/data access. | AC-SEC-003: injection/XSS/path traversal/oversized input fixtures bị từ chối an toàn. |
| SEC-004 | Backend | MVP | Must | Authentication **SHALL NOT** thay thế authorization cho private learning resources. | AC-SEC-004: tham chiếu cross-user corpus `AUTH-004`. |
| SEC-005 | Backend | MVP | Must | Nếu system tự quản lý password credential, password **SHALL** được hash bằng Argon2id hoặc BCrypt với cấu hình được security review và không persist plaintext. | AC-SEC-005: storage inspection không có plaintext; algorithm, work factor và credential-upgrade strategy được security review. |

## 10.9 Offline Behavior

| ID | Actor | Release | Priority | Normative statement | Acceptance criteria |
|---|---|---|---|---|---|
| OFF-001 | Extension, Web | Post-MVP | May | Client **MAY** lưu bounded lookup cache để dùng offline và phải đánh dấu result là cached/offline. | AC-OFF-001: cached result có version/status; không trở thành canonical store. |
| OFF-002 | Extension, Web | Post-MVP | May | Khi offline và không có cache, client **MAY** hiển thị explicit no-cached-data state kèm Retry. | AC-OFF-002: no-cache fixture không fabricate result. |
| OFF-003 | Extension, Web | Post-MVP | May | Client **MAY** dùng local outbox cho Save Vocabulary/Grammar offline và synchronize khi có network. | AC-OFF-003: cần SPEC riêng về idempotency, conflict và privacy. |

---

# 11. MVP User Journeys

Các journey là minh họa để liên kết requirement; requirement có ID vẫn là nguồn nghiệm thu.

## 11.1 Read and Analyze in Extension

1. Anonymous Reader chọn `食べました` trên website.
2. `JPN-001`, `DOM-001`, `EXT-001` và `EXT-003` tạo popup/loading.
3. `EXT-006` gửi analysis; `VOC-002`, `VOC-003`, `VOC-004`, `CONJ-001` tạo kết quả.
4. Reader mở Grammar để xem occurrence theo `GRM-002`, `GRM-007`.
5. Reader mở Dịch; `TRN-004` mới kích hoạt JP→VI hoặc JP→EN.

## 11.2 Save Vocabulary

1. Reader chọn vocabulary result và nhấn Save Vocabulary.
2. Nếu anonymous, `AUTH-002` yêu cầu authentication.
3. Sau authentication, `LEARN-001`, `ID-003`, `LEARN-007` persist canonical vocabulary reference.
4. Resource xuất hiện trong My Vocabulary theo `LEARN-004`.

## 11.3 Save Grammar

1. Reader chọn một grammar occurrence/detail và nhấn Save Grammar.
2. Nếu anonymous, `AUTH-002` yêu cầu authentication.
3. Sau authentication, `LEARN-003`, `ID-003`, `LEARN-007` persist canonical grammar reference.
4. Resource xuất hiện trong My Grammar theo `LEARN-005`.

## 11.4 Web Learning Flow

1. Reader nhập một Japanese sentence theo `WEB-001`.
2. Web hiển thị shared analysis theo `WEB-002`, `WEB-003`.
3. Learner lưu vocabulary/grammar, mở library và chạy Quick Review theo `REV-001`, `REV-002`.

---

# 12. MVP Definition of Done by Requirement Reference

| Gate | Requirement references |
|---|---|
| Extension reading | `JPN-001`, `DOM-001`, `EXT-001`–`EXT-006`, `ASYNC-001`–`ASYNC-002` |
| Vocabulary/Kanji | `ID-001`–`ID-006`, `VOC-001`–`VOC-008`, `KAN-001`–`KAN-002` |
| Grammar/Conjugation | `GRM-001`–`GRM-003`, `GRM-005`–`GRM-008`, `CONJ-001`–`CONJ-002` |
| Translation | `TRN-001`–`TRN-004` |
| Web | `WEB-001`–`WEB-005` |
| Contracts/platform | `ARCH-001`–`ARCH-009`, `API-001`–`API-004` |
| Authentication/Learning | `AUTH-001`–`AUTH-004`, `LEARN-001`–`LEARN-008` |
| Review | `REV-001`–`REV-004` |
| Knowledge/data | `DATA-001`–`DATA-005` |
| Privacy/operations | `PRIV-001`–`PRIV-006`, `RATE-001`–`RATE-004`, `CACHE-001`–`CACHE-002` |
| Reliability/performance | `NET-001`–`NET-006`, `PERF-001`–`PERF-004` |
| Browser/localization/security | `BROWSER-001`–`BROWSER-003`, `I18N-001`–`I18N-004`, `SEC-001`–`SEC-005` |

Post-MVP requirements (`GRM-004`, `TRN-005`, `OFF-001`–`OFF-003`) không thuộc MVP release gate.

---

# 13. Traceability Matrix

Acceptance evidence dùng mã AC được định nghĩa cùng requirement; test artifact cụ thể được feature SPEC/TASKS ánh xạ ở giai đoạn triển khai.

| Requirement | Actor(s) | Acceptance evidence | Release |
|---|---|---|---|
| ARCH-001–ARCH-009 | Delivery Team, Backend, Tokenizer Sidecar | AC-ARCH-001–AC-ARCH-009 | Foundation |
| JPN-001 | Extension | AC-JPN-001 | MVP |
| DOM-001 | Extension, Host Page | AC-DOM-001 | MVP |
| EXT-001–EXT-006 | Reader, Extension | AC-EXT-001–AC-EXT-006 | MVP |
| ASYNC-001–ASYNC-002 | Extension, Web | AC-ASYNC-001–AC-ASYNC-002 | MVP |
| ID-001–ID-006 | Backend, Data Operator | AC-ID-001–AC-ID-006 | Foundation/MVP |
| VOC-001–VOC-008 | Reader, Backend, Clients | AC-VOC-001–AC-VOC-008 | Foundation/MVP |
| KAN-001–KAN-002 | Reader, Backend | AC-KAN-001–AC-KAN-002 | MVP |
| GRM-001–GRM-003 | Reader, Backend, Data Operator | AC-GRM-001–AC-GRM-003 | MVP |
| GRM-004 | Data Operator, Backend | AC-GRM-004 | Post-MVP |
| GRM-005–GRM-008 | Reader, Backend | AC-GRM-005–AC-GRM-008 | Foundation/MVP |
| CONJ-001–CONJ-002 | Reader, Backend, Clients | AC-CONJ-001–AC-CONJ-002 | MVP |
| TRN-001–TRN-004 | Reader, Backend, Provider, Clients | AC-TRN-001–AC-TRN-004 | Foundation/MVP |
| TRN-005 | Reader | AC-TRN-005 | Post-MVP |
| WEB-001–WEB-005 | Reader, Web, Backend | AC-WEB-001–AC-WEB-005 | MVP |
| API-001–API-004 | Extension, Web, Backend | AC-API-001–AC-API-004 | Foundation/MVP |
| AUTH-001–AUTH-004 | Reader, Learner, Backend | AC-AUTH-001–AC-AUTH-004 | MVP |
| LEARN-001–LEARN-008 | Learner, Backend, Web | AC-LEARN-001–AC-LEARN-008 | MVP |
| REV-001–REV-004 | Learner, Web | AC-REV-001–AC-REV-004 | MVP |
| EXP-001–EXP-004 | Learner, Anki, Quizlet | AC-EXP-001–AC-EXP-004 | Post-MVP |
| EXP-005 | Anki, Quizlet | AC-EXP-005 | MVP |
| DATA-001–DATA-005 | Data Operator, Product Owner | AC-DATA-001–AC-DATA-005 | Foundation/MVP |
| PRIV-001–PRIV-006 | Backend, Operator, Provider | AC-PRIV-001–AC-PRIV-006 | Foundation/MVP |
| RATE-001–RATE-004 | Backend, Operator | AC-RATE-001–AC-RATE-004 | MVP |
| CACHE-001–CACHE-002 | Backend | AC-CACHE-001–AC-CACHE-002 | MVP |
| NET-001–NET-006 | Extension, Web, Backend | AC-NET-001–AC-NET-006 | Foundation/MVP |
| PERF-001–PERF-004 | Extension, Web, Backend, Provider | AC-PERF-001–AC-PERF-004 | MVP |
| BROWSER-001–BROWSER-003 | Extension, Host Page | AC-BROWSER-001–AC-BROWSER-003 | MVP |
| I18N-001–I18N-004 | Reader, Clients, Backend | AC-I18N-001–AC-I18N-004 | Foundation/MVP |
| SEC-001–SEC-005 | Delivery Team, Backend, Clients | AC-SEC-001–AC-SEC-005 | Foundation/MVP |
| OFF-001–OFF-003 | Extension, Web | AC-OFF-001–AC-OFF-003 | Post-MVP |

---

# 14. Roadmap

Roadmap là planning summary và không tạo requirement mới.

| Phase | Nội dung | Requirement references |
|---|---|---|
| Phase 1 — MVP | Extension, Web, Backend, N5/N4, Save, Review | MVP references tại mục 11 |
| Phase 2 — Learning Improvements | offline save queue, cached lookup, notes, tags, review improvements, optional export interoperability | `OFF-001`–`OFF-003`; notes/tags và export cần SPEC mới |
| Phase 3 — Knowledge Expansion | N3, N2, N1, examples, Vietnamese explanations | `GRM-004` và SPEC theo từng knowledge release |
| Phase 4 — Internationalization | Full English UI, English grammar content | `I18N-003`, `I18N-004` và SPEC riêng |
| Phase 5 — Advanced Learning | SRS, statistics, personalized review, synchronization | SPEC mới; ngoài scope PRD MVP hiện tại |

---

# 15. Open Decisions

Các mục dưới đây được Product Owner phê duyệt để **deferred có kiểm soát** ngày 2026-08-21. Delivery Team chuẩn bị đề xuất/evidence; VuPM phê duyệt quyết định tại blocking gate tương ứng.

| ID | Decision | Tác động | Status | Owner | Blocking gate |
|---|---|---|---|---|---|
| OD-001 | Translation provider cụ thể và fallback strategy | Privacy, cost, quality, latency | Deferred | Delivery Team / VuPM | Translation production readiness |
| OD-002 | Authentication provider, session lifecycle, recovery và account-linking flow | AUTH UX, security | Deferred | Delivery Team / VuPM | Auth feature approval |
| OD-003 | Shared span convention: UTF-16 code unit, Unicode code point hoặc convention khác | Grammar/client parity | Deferred | Delivery Team / VuPM | Grammar/API contract freeze |
| OD-004 | Final rate-limit thresholds và anonymous identifier lifecycle | Abuse, privacy, cost | Deferred | Delivery Team / VuPM | Production operations approval |
| OD-005 | Final benchmark profiles và locked SLO thresholds | Performance release gate | Deferred | Delivery Team / VuPM | MVP performance sign-off |
| OD-006 | Cache technology, TTL, invalidation và version-key policy | Correctness, operations | Deferred | Delivery Team / VuPM | Cache implementation plan |
| OD-007 | Exact Anki/Quizlet field mapping, delimiter default và export template version | Interoperability | Deferred | Delivery Team / VuPM | Post-MVP Export SPEC approval |
| OD-008 | Grammar matcher/storage schema và safe sentence-boundary strategy | Linguistic correctness | Deferred | Delivery Team / VuPM | Grammar SPEC/plan |
| OD-009 | FVDP/OVDP source, license, redistribution và linking quality | Vietnamese meanings | Deferred | Data Operator / VuPM | Production data approval |
| OD-010 | Tokenizer sidecar implementation language/provider (Go/Python, Sudachi/MeCab hoặc candidate approved khác) | Morphology quality/operations | Deferred | Delivery Team / VuPM | NLP adapter selection |
| OD-011 | Shadow DOM open/closed mode và detailed focus/positioning behavior | Accessibility, host integration | Deferred | Delivery Team / VuPM | Extension UI plan |
| OD-012 | Quick Review ordering và semantics/persistence của optional `Again`/`Know` | Learning behavior | Deferred | Delivery Team / VuPM | Review SPEC approval nếu actions được triển khai |
| OD-013 | Soft-delete retention, restore policy và eventual purge | Privacy, support, data lifecycle | Deferred | Delivery Team / VuPM | Learning data lifecycle approval |
| OD-014 | Maximum input/selection/context length và truncation/rejection behavior | Privacy, latency, linguistic correctness | Deferred | Delivery Team / VuPM | Analysis/API contract freeze |

## 15.1 Document Alignment Issues

Đây không phải product decisions mới nhưng cần được đồng bộ sau khi PRD được phê duyệt:

- `ADR-002`, `.sdd/constraints/global.md`, SDD và feature-spec inheritance đã thống nhất PostgreSQL 16, Npgsql EF Core Migrations và PostgreSQL Testcontainers ngày 2026-08-21. SQL Server/SQLite không thuộc baseline V2.
- `MIGRATION_DECISION.md` đã ghi PostgreSQL 16 trong resolved decisions; mọi implementation phải tuân theo ADR-002 thay vì dual-provider compatibility.
- `.sdd/specs/feat-dict-lookup/SPEC.md` đã cùng baseline PostgreSQL/Testcontainers; performance target riêng vẫn cần reconcile với `ARCH-001` và `PERF-002` trước khi phê duyệt feature spec.
- `.sdd/specs/feat-auth/SPEC.md` hiện rỗng; auth business flows chi tiết chưa được đặc tả.

---

# 16. Approval Record and Next Steps

## 16.1 Product Owner Checklist

- [x] VuPM xác nhận product identity là Japanese Reading Assistant + Learning Platform.
- [x] VuPM xác nhận Extension và Web đều thuộc MVP.
- [x] VuPM xác nhận Vietnamese UI là primary; JP→EN translation bắt buộc; full English UI ngoài MVP.
- [x] VuPM xác nhận anonymous reading và authenticated learning persistence.
- [x] VuPM xác nhận Save Vocabulary và Save Grammar là hai flow riêng.
- [x] VuPM xác nhận notes/tags nằm ở Phase 2, không thuộc MVP.
- [x] VuPM xác nhận soft delete user-visible semantics và chấp nhận `OD-013` deferred.
- [x] VuPM xác nhận N5–N4 là grammar coverage MVP sau review corpus.
- [x] VuPM xác nhận export và Anki/Quizlet synchronization đều ngoài MVP.
- [x] VuPM xác nhận Chrome, Edge, Brave, Firefox là browser targets; Safari ngoài MVP.
- [x] VuPM xác nhận on-demand translation và privacy disclosure.
- [x] VuPM xác nhận technical baseline `ARCH-001`–`ARCH-009`.
- [x] VuPM review toàn bộ Open Decisions; các mục được deferred với owner và blocking gate tại §15.
- [x] VuPM được ghi là Product Owner; approval date 2026-08-21; Status là Approved.

## 16.2 Next Steps After Approval

1. Duy trì ADR-002 và `.sdd/constraints/*` theo PostgreSQL 16/Npgsql/Testcontainers; rà soát lại khi có thay đổi provider hoặc major database version.
2. Tạo hoặc cập nhật feature SPEC theo từng domain và ánh xạ AC tới automated/manual evidence.
3. Giải quyết open decisions trước blocking gate tương ứng.
4. Chạy consistency analysis giữa PRD, Constitution, Migration Decision, SPEC, PLAN và TASKS trước implementation.

## 16.3 References

- `AGENTS.md` — approved engineering baseline và execution protocol.
- `.sdd/constitution.md` — project-wide invariants.
- `MIGRATION_DECISION.md` — legacy asset disposition.
- `.sdd/specs/feat-platform-foundation/SPEC.md` — platform boundary specification.
- `.sdd/specs/feat-dict-lookup/SPEC.md` — draft dictionary lookup specification requiring alignment.
