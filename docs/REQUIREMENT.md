# JP Reading Platform — Product Requirements v2

**Status:** Draft for Approval
**Version:** 2.0.0-draft
**Product:** Japanese Reading & Learning Platform
**Primary Language:** Vietnamese
**Secondary Language:** English
**Target MVP Clients:** Browser Extension + Web Application
**Target Browsers:** Chrome, Edge, Brave, Firefox

---

## Document Authority

This document is the source of truth for the future product direction.

The existing README.md describes the currently implemented offline-first
extension and MUST NOT be interpreted as the target architecture.

Where README.md and this document conflict regarding future product behavior
or architecture, this document takes precedence.

README.md SHALL continue to describe the currently working implementation
until the corresponding migration has actually been completed.

---

# 1. Product Vision

JP Reading Platform là nền tảng hỗ trợ **đọc, hiểu và học tiếng Nhật trực tiếp trong ngữ cảnh thực tế**.

Người dùng có thể:

* chọn văn bản tiếng Nhật trên bất kỳ website nào và nhận phân tích ngay trong popup;
* tra cứu từ vựng, kanji và ngữ pháp;
* xem cách biến đổi/chia từ;
* dịch nội dung Nhật → Việt hoặc Nhật → Anh;
* lưu từ vựng và ngữ pháp mình gặp;
* quản lý nội dung đã lưu trên website;
* ôn tập hoặc xuất dữ liệu sang các công cụ học như Anki và Quizlet.

Sản phẩm không còn được định nghĩa đơn thuần là một dictionary extension mà là một **Japanese Reading Assistant + Learning Platform**.

Hệ thống sử dụng backend làm nguồn xử lý và dữ liệu trung tâm để Extension và Web Application có thể sử dụng chung một knowledge layer.

---

# 2. Problem Statement

Người học tiếng Nhật khi đọc nội dung thực tế thường phải chuyển qua nhiều công cụ khác nhau để:

* tra từ;
* tra kanji;
* kiểm tra dạng gốc của động từ/tính từ;
* tìm ngữ pháp;
* dịch câu;
* lưu từ để học lại.

Quá trình chuyển context làm gián đoạn việc đọc và khiến kiến thức vừa gặp khó được lưu lại để học sau.

JP Reading Platform giải quyết vấn đề này bằng workflow:

```text
Gặp tiếng Nhật
      ↓
Hiểu ngay trong context
      ↓
Lưu kiến thức quan trọng
      ↓
Ôn tập / Export để học
```

---

# 3. Product Principles

## P-01 — Reading First

Sản phẩm SHALL ưu tiên giảm tối đa gián đoạn khi người dùng đang đọc tiếng Nhật.

Extension SHALL cho phép người dùng nhận thông tin mà không cần rời khỏi website hiện tại.

---

## P-02 — Vietnamese First

MVP SHALL sử dụng tiếng Việt làm ngôn ngữ giao diện chính.

Dictionary và translation SHALL hỗ trợ kết quả tiếng Việt.

English SHALL được hỗ trợ như ngôn ngữ phụ ở những nơi dữ liệu đã có sẵn.

Kiến trúc dữ liệu SHALL cho phép bổ sung English UI đầy đủ trong tương lai mà không phải redesign domain model.

---

## P-03 — Server-Authoritative Knowledge

Dictionary, kanji, grammar knowledge và linguistic analysis SHALL lấy backend làm nguồn dữ liệu chính.

Người dùng SHALL NOT bị yêu cầu download dictionary, tokenizer hoặc translation language pack dung lượng lớn để sử dụng các chức năng chính.

---

## P-04 — Cross-Browser Architecture

Extension MVP SHALL hỗ trợ:

* Google Chrome;
* Microsoft Edge;
* Brave;
* Mozilla Firefox.

Browser-specific APIs SHALL được cô lập khỏi core application logic.

---

## P-05 — Learning Is Optional

Người dùng SHALL có thể:

* tra từ;
* phân tích kanji;
* xem grammar;
* dịch;

mà không cần tạo account.

Account SHALL chỉ bắt buộc cho những chức năng cần lưu dữ liệu lâu dài như:

* Saved Vocabulary;
* Saved Grammar;
* learning progress;
* synchronization.

---

# 4. Target Users

## Persona A — Vietnamese Japanese Learner

Người Việt đang học tiếng Nhật từ N5 đến N1.

Nhu cầu:

* đọc website Nhật;
* hiểu nhanh từ/ngữ pháp;
* xem Hán Việt;
* lưu từ;
* học lại sau.

Đây là primary persona của MVP.

---

## Persona B — English-Speaking Japanese Learner

Người học tiếng Nhật sử dụng tiếng Anh.

MVP chưa cần cung cấp toàn bộ English UI nhưng architecture SHALL cho phép mở rộng sang đối tượng này.

---

# 5. MVP Scope Overview

```text
JP Reading Platform
│
├── Browser Extension
│   ├── Selection popup
│   ├── Vocabulary
│   ├── Kanji
│   ├── Grammar
│   ├── Translation
│   └── Save
│
├── Web Application
│   ├── Japanese Search / Analyze
│   ├── Dictionary
│   ├── Kanji
│   ├── Grammar Library
│   ├── Translation
│   ├── My Vocabulary
│   ├── My Grammar
│   ├── Quick Review
│   └── Export
│
└── Backend
    ├── Japanese Analysis
    ├── Dictionary
    ├── Kanji
    ├── Grammar
    ├── Translation
    ├── Authentication
    ├── Learning Library
    ├── Export
    ├── Rate Limiting
    └── Caching
```

---

# 6. Browser Extension Requirements

## EXT-001 — Automatic Selection Detection

**WHEN** người dùng chọn một đoạn text có chứa ký tự tiếng Nhật,

**THE EXTENSION SHALL** tự động mở popup mà không cần click icon hoặc sử dụng hotkey.

---

## EXT-002 — Ignore Non-Japanese Selection

**WHEN** selection không chứa nội dung tiếng Nhật,

**THE EXTENSION SHALL NOT** mở Japanese analysis popup.

---

## EXT-003 — Immediate Popup Shell

Popup shell SHALL xuất hiện trước khi backend trả kết quả.

Target ban đầu:

**p95 ≤ 150 ms** kể từ khi valid selection ổn định đến khi popup shell xuất hiện.

Popup SHALL hiển thị loading state trong thời gian chờ backend.

---

## EXT-004 — Selection Debounce

Extension SHALL debounce selection trước khi gọi backend để tránh gửi request trong lúc user vẫn đang thay đổi selection.

Baseline ban đầu:

**250–400 ms**.

Giá trị cuối cùng SHALL được benchmark trong implementation plan.

---

## EXT-005 — Cancel Stale Requests

**WHEN** user tạo selection mới trước khi request hiện tại hoàn thành,

**THE EXTENSION SHALL** bỏ qua hoặc cancel request cũ.

Kết quả cũ SHALL NOT overwrite popup của selection mới.

---

## EXT-006 — Analysis Request

Extension SHALL có khả năng gửi selection tới backend bằng một analysis request hợp nhất.

Analysis response MAY bao gồm:

* normalized text;
* tokens;
* base form;
* vocabulary entries;
* kanji;
* conjugation;
* detected grammar.

Extension SHOULD NOT tạo một network request riêng cho từng module nếu kết quả có thể được batch trong một analysis request.

---

# 7. Vocabulary Requirements

## VOC-001 — Exact Lookup

System SHALL hỗ trợ tra dictionary form trực tiếp.

Ví dụ:

```text
学生
→ 学生
→ がくせい
→ học sinh / student
```

---

## VOC-002 — Morphological Resolution

System SHALL có khả năng xác định dạng từ điển từ dạng biến đổi.

Ví dụ:

```text
食べました
→ 食べる
```

```text
高くなかった
→ 高い
```

---

## VOC-003 — Japanese Reading

Vocabulary result SHALL hiển thị reading khi dữ liệu có sẵn.

Ví dụ:

```text
食べる
たべる
```

---

## VOC-004 — Vietnamese Meaning

Vietnamese meaning SHALL được ưu tiên khi có dữ liệu đáng tin cậy.

---

## VOC-005 — English Meaning

English meaning SHOULD được cung cấp như secondary meaning khi data có sẵn.

---

## VOC-006 — Part of Speech

Vocabulary result SHOULD cung cấp part-of-speech information khi data source hỗ trợ.

---

# 8. Kanji Requirements

## KAN-001

Khi selection chứa kanji, system SHALL cho phép xem thông tin của từng kanji.

Thông tin MAY bao gồm:

* character;
* Hán Việt;
* Vietnamese meaning;
* English meaning;
* On reading;
* Kun reading;
* stroke count;
* JLPT level;
* radical.

Dữ liệu hiện tại của project đã hỗ trợ phần lớn các trường này thông qua KANJIDIC2.

---

# 9. Grammar Requirements

## GRM-001 — Sentence Context

Grammar analysis SHOULD sử dụng câu chứa selection thay vì chỉ phân tích riêng từ được chọn khi context có thể xác định an toàn.

---

## GRM-002 — Grammar Detection

System SHALL phát hiện grammar patterns trong câu tiếng Nhật.

---

## GRM-003 — MVP Coverage

MVP SHALL giữ được ít nhất phạm vi grammar N5–N4 đang tồn tại trong project.

Codebase hiện tại có corpus khoảng 102 pattern N5–N4.

---

## GRM-004 — Grammar Roadmap

Grammar subsystem SHALL có khả năng mở rộng coverage theo roadmap:

```text
N5
↓
N4
↓
N3
↓
N2
↓
N1
```

---

## GRM-005 — Data-Driven Grammar

Grammar knowledge SHALL được thiết kế theo hướng data-driven.

Thêm grammar pattern mới SHOULD NOT yêu cầu thay đổi:

* browser selection logic;
* popup architecture;
* API transport layer.

---

## GRM-006 — Grammar Information

Grammar entry SHOULD có khả năng chứa:

```text
id
pattern
JLPT level
meaning.vi
meaning.en
formation
examples
matching metadata
```

English content MAY chưa đầy đủ trong MVP.

---

# 10. Conjugation Requirements

## CONJ-001

Khi một word unit là dạng biến đổi, system SHOULD giải thích cách biến đổi.

Ví dụ:

```text
食べました

食べ
+
まし
+
た
```

---

## CONJ-002

Conjugation explanation MVP SHALL ưu tiên tiếng Việt.

Architecture SHALL cho phép bổ sung English explanation sau này.

---

# 11. Translation Requirements

## TRN-001

System SHALL hỗ trợ:

```text
Japanese → Vietnamese
Japanese → English
```

---

## TRN-002 — Translation Is Separate

Translation SHALL sử dụng một provider abstraction tại backend.

Extension và website SHALL NOT phụ thuộc trực tiếp vào một translation provider cụ thể.

---

## TRN-003 — No Provider Lock-In

Thay đổi translation provider SHOULD NOT yêu cầu thay đổi extension.

---

## TRN-004 — Translation On Demand

Dictionary/grammar analysis SHALL NOT bắt buộc phải gọi translation service.

Để giảm:

* cost;
* latency;
* network traffic;
* privacy exposure;

MVP SHOULD chỉ gọi sentence translation khi user mở phần/tab Dịch hoặc thực hiện hành động tương đương.

---

## TRN-005 — Future Auto Translation

Auto-translate MAY được cung cấp sau này như user setting.

Default MVP: **OFF**.

---

# 12. Website MVP

## WEB-001 — Japanese Search

Website SHALL có một input chính cho phép người dùng nhập:

* từ;
* cụm từ;
* câu tiếng Nhật.

---

## WEB-002 — Shared Analysis Backend

Website SHALL sử dụng cùng backend knowledge services với Extension.

Business logic SHALL NOT được duplicate giữa website và extension.

---

## WEB-003 — Analysis Result

Website SHALL có khả năng hiển thị:

* vocabulary;
* kanji;
* morphology;
* conjugation;
* grammar;
* translation.

---

## WEB-004 — Grammar Library

Website SHALL cung cấp Grammar Library.

MVP SHALL hỗ trợ ít nhất N5–N4 data hiện có.

User SHALL có thể:

* browse theo JLPT;
* search grammar;
* mở grammar detail.

---

## WEB-005 — Grammar Detail

Grammar detail SHOULD hiển thị:

* pattern;
* JLPT level;
* Vietnamese explanation;
* formation;
* examples;
* English explanation nếu có.

---

# 13. Authentication Requirements

## AUTH-001

Reading features SHALL NOT yêu cầu login.

---

## AUTH-002

Account SHALL được yêu cầu khi user muốn lưu dữ liệu persistent.

---

## AUTH-003

Authentication design SHALL hỗ trợ cùng một account được sử dụng từ:

* Extension;
* Web Application.

---

# 14. Learning MVP

## LEARN-001 — Save Vocabulary

Logged-in user SHALL có thể lưu vocabulary từ:

* Extension;
* Website.

---

## LEARN-002 — Saved Vocabulary Model

Một vocabulary item SHOULD có khả năng lưu:

```text
dictionary entry identifier
expression
reading
Vietnamese meaning
English meaning
part of speech
savedAt
user note
tags
```

Raw page context SHALL NOT mặc định được lưu nếu chưa có product decision riêng cho chức năng đó.

---

## LEARN-003 — Save Grammar

Logged-in user SHALL có thể lưu grammar pattern.

---

## LEARN-004 — My Vocabulary

Website SHALL có trang cho phép user xem các vocabulary item đã lưu.

---

## LEARN-005 — My Grammar

Website SHALL có trang cho phép user xem các grammar pattern đã lưu.

---

## LEARN-006 — Delete Saved Item

User SHALL có thể xóa saved vocabulary hoặc saved grammar của chính mình.

---

## LEARN-007 — Duplicate Save

Lưu cùng một canonical vocabulary/grammar nhiều lần SHALL NOT mặc định tạo duplicate item.

System SHOULD xử lý request theo hướng idempotent.

---

# 15. Quick Review MVP

Website SHALL cung cấp một Quick Review đơn giản.

MVP không yêu cầu advanced spaced repetition.

Ví dụ:

```text
┌──────────────────┐
│      食べる       │
│                  │
│   Hiện đáp án    │
└──────────────────┘
```

Sau khi reveal:

```text
たべる
ăn
to eat
```

MVP MAY hỗ trợ basic actions như:

```text
Again
Know
```

Advanced SRS SHALL nằm ngoài MVP.

---

# 16. Anki / Quizlet Interoperability

## EXP-001 — Export First

MVP SHALL ưu tiên **export compatibility**, không phải automatic two-way synchronization.

---

## EXP-002 — Anki-Compatible Export

User SHALL có thể export vocabulary/grammar ở format có thể import vào Anki.

---

## EXP-003 — Quizlet-Compatible Export

User SHALL có thể export vocabulary/grammar thành text/CSV/TSV phù hợp với import workflow của Quizlet.

---

## EXP-004 — CSV/TSV

System SHALL hỗ trợ UTF-8 CSV hoặc TSV export.

---

## EXP-005 — Two-Way Sync

Automatic bidirectional synchronization với Anki hoặc Quizlet SHALL **không nằm trong MVP**.

Đây sẽ là feature riêng vì cần giải quyết:

* external item identity;
* conflict resolution;
* deletion semantics;
* duplicate prevention;
* retry;
* synchronization state.

---

# 17. Backend Responsibilities

Backend SHALL chịu trách nhiệm cho:

```text
Japanese analysis
Dictionary lookup
Kanji lookup
Morphology
Grammar detection
Translation orchestration
Authentication
Saved vocabulary
Saved grammar
Export
Rate limiting
Caching
```

---

# 18. Backend Architecture Direction

MVP SHOULD sử dụng một deployable backend đơn giản thay vì bắt đầu với distributed microservices.

Logical modules SHOULD được tách rõ:

```text
dictionary
kanji
analysis
grammar
translation
auth
learning
export
```

Việc lựa chọn:

* programming language;
* backend framework;
* database;
* cache technology;
* hosting;

SHALL được quyết định ở implementation plan, không phải Product Requirements.

---

# 19. Data Sources

MVP SHOULD ưu tiên reuse các data source miễn phí/open hiện có trong project:

* JMdict;
* KANJIDIC2;
* FVDP / OVDP;
* IPADIC / Japanese morphological data;
* grammar corpus hiện tại.

README hiện tại đã mô tả provenance và license của các nguồn này.

---

## DATA-001 — Provenance

Mọi external dataset được đưa vào production SHALL có record về:

* source;
* version;
* license;
* attribution requirements.

---

## DATA-002 — Cost

MVP SHOULD ưu tiên:

* open datasets;
* free services;
* free tier;
* low-cost infrastructure.

Paid proprietary datasets SHALL NOT là dependency bắt buộc của MVP.

---

## DATA-003 — Data Pipeline

Dictionary/grammar source data SHOULD được import qua một reproducible ETL/data preparation pipeline.

Data SHALL NOT được sửa thủ công trực tiếp trong production database nếu thay đổi đó không thể tái tạo.

---

# 20. Privacy Requirements

## PRIV-001 — Selected Text

Backend MAY nhận selected Japanese text để thực hiện analysis.

---

## PRIV-002 — Raw Query Logging

Backend SHALL NOT mặc định persist raw selected text trong application logs.

---

## PRIV-003 — Operational Logging

System MAY log metadata như:

```text
timestamp
endpoint
latency
status
request size
```

mà không cần lưu nội dung text của user.

---

## PRIV-004 — Third-Party Translation

Nếu translation yêu cầu gửi text tới third-party provider, product SHALL cung cấp disclosure phù hợp cho user.

---

## PRIV-005 — Learning Data

Saved vocabulary, saved grammar và learning information SHALL thuộc về authenticated user tương ứng và SHALL NOT được public mặc định.

---

# 21. Rate Limiting & Abuse Protection

Backend SHALL có rate limiting.

Rate limit SHOULD phân biệt:

* analysis requests;
* translation requests;
* authenticated requests;
* anonymous installations;
* IP-level abuse protection.

Baseline để benchmark, **không phải hard requirement cuối cùng**:

```text
POST /v1/analyze
~60 requests/minute/installation

POST /v1/translate
~10 requests/minute/installation

IP protection
~300–600 requests/minute/IP
```

System SHALL trả:

```text
HTTP 429 Too Many Requests
```

khi quota bị vượt.

Response SHOULD cung cấp thông tin retry phù hợp.

Các giới hạn cuối cùng SHALL được điều chỉnh theo benchmark và infrastructure cost.

---

# 22. Anonymous Client Identification

Anonymous rate limiting SHOULD sử dụng random installation identifier hoặc cơ chế tương tự.

System SHALL NOT sử dụng invasive browser fingerprinting để tạo identifier.

Sau login, user identity MAY trở thành rate-limit principal chính.

---

# 23. Caching Requirements

Static hoặc mostly-static knowledge SHOULD có khả năng được cache.

Ví dụ:

* dictionary entry;
* kanji metadata;
* grammar definition.

System SHOULD tránh thực hiện lại expensive computation cho các input giống nhau khi cached result còn valid.

Caching implementation SHALL được quyết định ở Plan.

---

# 24. Failure & Network Behaviour — MVP

## NET-001

Popup SHALL xuất hiện ngay cả khi backend không reachable.

---

## NET-002

Nếu backend đang xử lý, UI SHALL hiển thị loading state.

---

## NET-003

Nếu backend không trả kết quả trong timeout window, UI SHALL chuyển sang explicit failure state.

Baseline timeout:

**5 seconds**.

---

## NET-004

Failure SHALL NOT:

* phá webpage;
* đóng băng popup;
* làm extension crash.

---

## NET-005

User SHALL có action:

```text
Thử lại / Retry
```

---

# 25. Offline Behaviour — Post-MVP

Full offline dictionary SHALL **không nằm trong MVP mới**.

Post-MVP MAY triển khai Graceful Offline Mode.

---

## OFF-001 — Cached Lookup

Result đã tra trước đó MAY được lưu local để sử dụng khi mất mạng.

Cached result SHALL được đánh dấu rõ là offline/cached.

---

## OFF-002 — Unknown Offline Lookup

Nếu user offline và item chưa có cache:

```text
Bạn đang offline.
Chưa có dữ liệu được cache cho nội dung này.
[Thử lại]
```

---

## OFF-003 — Offline Save Queue

Post-MVP MAY cho phép Save Vocabulary/Grammar khi offline bằng local outbox.

Khi network trở lại, client MAY synchronize pending operations với backend.

---

# 26. Performance Targets

Các số sau là **initial product SLOs** và SHALL được benchmark trước khi lock.

## PERF-001 — Popup

Selection → popup shell:

```text
p95 ≤ 150 ms
```

---

## PERF-002 — Core Analysis

Backend analysis:

```text
p95 ≤ 800 ms
```

sau khi backend đã hoạt động bình thường.

---

## PERF-003 — Translation

Translation:

```text
p95 ≤ 2 seconds
```

trong điều kiện provider hoạt động bình thường.

---

## PERF-004 — Timeout

Frontend SHALL NOT chờ vô hạn.

Initial request timeout:

```text
≤ 5 seconds
```

---

# 27. Cross-Browser Requirements

MVP SHALL support:

```text
Chrome
Edge
Brave
Firefox
```

Safari SHALL không nằm trong MVP.

---

## BROWSER-001

Core domain logic SHALL NOT trực tiếp phụ thuộc vào browser-specific global APIs.

---

## BROWSER-002

Các khác biệt giữa Chromium và Firefox SHALL được xử lý tại browser/platform adapter hoặc entry-point boundary.

---

# 28. Localization Requirements

## I18N-001

MVP user interface SHALL là Vietnamese-first.

---

## I18N-002

Translation SHALL hỗ trợ output:

```text
Vietnamese
English
```

---

## I18N-003

Domain models SHOULD sử dụng multilingual-capable structure khi phù hợp.

Ví dụ:

```text
meaning.vi
meaning.en

explanation.vi
explanation.en
```

thay vì hard-code Vietnamese vào một field không thể mở rộng.

---

## I18N-004

Full English interface SHALL không nằm trong MVP nhưng SHALL có thể được thêm mà không redesign backend entities chính.

---

# 29. Security Requirements

System SHALL:

* sử dụng HTTPS cho production API;
* không hard-code secrets vào Extension hoặc frontend bundle;
* không đưa private backend credentials xuống client;
* validate untrusted inputs ở backend;
* kiểm soát authorization với learning resources;
* không cho user đọc/sửa library của user khác.

Constitution sau này sẽ định nghĩa các security hard rules chi tiết hơn.

---

# 30. MVP Out of Scope

Các feature sau SHALL NOT nằm trong MVP:

* full N3/N2/N1 grammar corpus;
* full English user interface;
* automatic Anki bidirectional synchronization;
* automatic Quizlet bidirectional synchronization;
* full offline dictionary;
* offline translation engine;
* advanced spaced repetition algorithm;
* AI Japanese tutor/chatbot;
* social/community features;
* mobile app;
* Safari support;
* microservice architecture;
* paid proprietary dictionary requirement.

Việc không nằm trong MVP không có nghĩa là bị cấm vĩnh viễn.

---

# 31. MVP User Journey — Extension

```text
1. User mở một website tiếng Nhật

2. User select:
   食べました

3. Popup xuất hiện ngay

4. Popup hiển thị loading

5. Backend trả:
   Base form: 食べる
   Reading: たべる
   VI: ăn
   EN: to eat
   Conjugation information

6. User mở Grammar
   → xem grammar liên quan

7. User mở Dịch
   → JP → VI hoặc EN

8. User click Save

9. Nếu chưa login:
   → yêu cầu authentication

10. Saved item xuất hiện trong My Vocabulary trên website
```

---

# 32. MVP User Journey — Website

```text
1. User mở Web App

2. Nhập:
   日本語を勉強したことがあります。

3. System phân tích câu

4. User xem:
   Vocabulary
   Kanji
   Grammar
   Translation

5. System nhận diện:
   〜たことがある

6. User lưu grammar

7. User vào My Library

8. User review vocabulary/grammar

9. User export sang Anki/Quizlet-compatible format
```

---

# 33. MVP Definition of Done

MVP được coi là đạt khi:

### Extension

* Chrome hoạt động;
* Edge hoạt động;
* Brave hoạt động;
* Firefox hoạt động;
* selection → popup hoạt động;
* vocabulary lookup hoạt động;
* kanji lookup hoạt động;
* morphology/conjugation hoạt động;
* grammar N5–N4 hiện tại hoạt động;
* JP→VI translation hoạt động;
* JP→EN translation hoạt động;
* Save Vocabulary hoạt động;
* Save Grammar hoạt động;
* backend failure có graceful UI.

### Website

* Japanese search/analyze hoạt động;
* dictionary view hoạt động;
* kanji view hoạt động;
* grammar view/library hoạt động;
* translation hoạt động;
* authentication hoạt động;
* My Vocabulary hoạt động;
* My Grammar hoạt động;
* Quick Review hoạt động;
* export hoạt động.

### Backend

* shared analysis API hoạt động;
* translation abstraction hoạt động;
* authentication hoạt động;
* learning data persistence hoạt động;
* rate limiting hoạt động;
* basic cache strategy hoạt động;
* tests cho critical business logic pass;
* privacy requirements được đáp ứng.

---

# 34. Roadmap After MVP

## Phase 1 — MVP

```text
Extension
+
Website
+
Backend
+
N5/N4
+
Save
+
Review
+
Export
```

## Phase 2 — Learning Improvements

```text
offline save queue
cached lookup
tags
notes
better review
AnkiConnect integration
```

## Phase 3 — Language Knowledge Expansion

```text
N3
N2
N1
more examples
better Vietnamese explanations
```

## Phase 4 — Internationalization

```text
Full English UI
English grammar explanations
localized learning experience
```

## Phase 5 — Advanced Learning

Potential features:

```text
SRS
learning statistics
personalized review
Anki synchronization
device synchronization
```

Các feature trong Phase 5 SHALL cần specification riêng trước implementation.

---

# 35. Open Decisions — Not Blocking Requirements Approval

Các quyết định sau intentionally chưa được khóa trong PRD:

1. Backend programming language/framework.
2. Database technology.
3. Hosting provider.
4. Cache implementation.
5. Translation provider.
6. Authentication provider hoặc self-hosted auth.
7. Final API endpoint structure.
8. Exact grammar storage schema.
9. Final rate-limit numbers.
10. Final performance SLOs sau benchmark.

Các mục này SHALL được giải quyết trong:

```text
$speckit-plan
```

hoặc feature-specific specification nếu cần.

---

# 36. Requirement Governance

Product requirements là living artifacts.

Thay đổi requirement SHALL được cập nhật vào specification trước khi implementation thay đổi đáng kể.

Feature implementation SHALL không được tự mở rộng scope ngoài approved specification.

Spec Kit SHALL được sử dụng theo workflow:

```text
Constitution
      ↓
Specify
      ↓
Clarify
      ↓
Human Approval
      ↓
Plan
      ↓
Human Approval
      ↓
Tasks
      ↓
Analyze
      ↓
Implement
      ↓
Converge
      ↓
Validate
```

Playbook nhấn mạnh rằng mỗi Spec Kit command là một human/AI handoff point và người dùng phải review trước khi chuyển pha.

---

# 37. Product Requirement Approval Gate

Trước khi Product Requirements v2 được coi là approved, Product Owner cần xác nhận:

* [ ] Product identity là Japanese Reading & Learning Platform.
* [ ] Extension và Website đều nằm trong MVP.
* [ ] Backend là shared knowledge source.
* [ ] Vietnamese-first, English-ready.
* [ ] Chrome + Edge + Brave + Firefox là browser targets.
* [ ] Core analysis yêu cầu internet ở MVP.
* [ ] Full offline mode không thuộc MVP.
* [ ] Translation gọi on-demand.
* [ ] Reading không yêu cầu account.
* [ ] Account dùng cho learning persistence.
* [ ] Vocabulary và grammar đều có thể Save.
* [ ] MVP dùng Export thay vì two-way Anki/Quizlet sync.
* [ ] Grammar MVP giữ N5–N4, roadmap đến N1.
* [ ] Paid proprietary data không phải dependency của MVP.
* [ ] Raw selected text không được persist trong logs mặc định.
* [ ] Các technology choices chưa chốt sẽ được quyết định ở Plan.
