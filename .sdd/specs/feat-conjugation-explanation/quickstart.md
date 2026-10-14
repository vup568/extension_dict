# Quickstart: F-09 — Kiểm chứng khi triển khai

**Status**: Hướng dẫn dự kiến; lượt lập PLAN không chạy test/migration dưới đây.
[PLAN](./PLAN.md), [data model](./data-model.md), [contract](./contracts/conjugation-contract.md).

## Prerequisites

G1 approved corpus, G2 reviewed answers, người dùng đồng ý triển khai trước đổi code/data.
.NET10, Node tương thích lock, dependencies đã restore, Docker Engine và browsers đã cài.
Testcontainers PostgreSQL16 riêng; không reset/migrate DB người dùng để test.
Không tự cài package/browser. Thiếu Firefox executable thì ghi missing evidence.

## 1. Hồ sơ trước runtime

Manifest/cases/review của `corpus/expanded-20261013.3/`: 60 ô đề xuất của bốn nhóm
có quyết định, 9 baseline được đối chiếu, refs/source rights đầy đủ; mỗi ô không-exact
bắt buộc ≥1 ca đúng, mỗi rule ≥2 đúng+1 âm và ca phân biệt cho dạng mơ hồ.
Versions/hashes/PO/reviewer/date khớp, blocking issues=0.
Đáp án độc lập; actual không ghi đè expected. Hash mismatch chặn acceptance.

PowerShell root, sau khi corpus artifacts tương lai đã được tạo:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath .sdd/specs/feat-conjugation-explanation/corpus/expanded-20261013.3/cases.json
```

Ghi hash lowercase trong hồ sơ. Candidate mới cài bằng migration additively trên container,
giữ seed/version cũ. No-gap có thể giữ unchanged version hoặc đóng gói candidate được review.

## 2. Backend/Sidecar gates

Repo root, dependencies đã restore:

```powershell
docker build -t jp-reading-tokenizer:verified-20261013 sidecars/tokenizer
docker run --rm --read-only --cap-drop ALL --security-opt no-new-privileges jp-reading-tokenizer:verified-20261013 python -m unittest -v test_tokenizer
dotnet build JPReadingPlatform.sln --no-restore --configuration Release
dotnet test tests/unit/UnitTests.csproj --no-build --configuration Release
dotnet test tests/integration/IntegrationTests.csproj --no-build --configuration Release
dotnet test tests/unit/UnitTests.csproj --no-build --configuration Release --collect 'Code Coverage' --results-directory tests/TestResults/f09-coverage -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

Expected: build0 warnings/errors; candidate version và corpus frozen được harness nạp,
100% cases expected pass; modified usecases≥85% coverage; old replay/immutability pass.
Tests hiện chủ yếu development-version/InlineData; C1 phải nối frozen corpus vào tests
trước khi coi full suite là evidence cho corpus mới. Không reuse counts trước đây.

## 3. Client regression

Từ `extension/`:

```powershell
npm.cmd run typecheck
npm.cmd run test:coverage
npm.cmd run unicode:verify
npm.cmd run build
npm.cmd run test:browser -- --project=chrome --project=edge
```

Chạy thêm Brave/packaged và Firefox trên executable được hỗ trợ khi có; không tự tải.
Expected: v1/v2 giữ nguyên, v3 hiển thị nhiều phương án có chứng cứ cùng span;
candidate/base/VI/segments hiện đúng; Retry fresh identity,
late results ignored, close cancels. Firefox build không là evidence parity runtime.

## 4. Acceptance scenarios

- Supported có base-entry: đúng base/meaning/segments/spans, VI và candidate identity.
- Bốn nhóm và thể nâng cao: đối chiếu từng slot/biến thể, bao gồm copula, bị động,
  sai khiến, sai khiến-bị động, điều kiện và bất quy tắc; không đánh đồng noun/na-adjective.
- Supported thiếu base-entry: completed-empty, không bịa dictionary/conjugation.
- Exact hit: giữ exact và không deinflect để tạo explanation.
- Ambiguous được review: v3 hiển thị mọi cách hiểu với nhãn “có thể” (食べられる: khả năng/bị động/kính ngữ; 食べさせられる: sai khiến-bị động/sai khiến-khả năng); v2
  unavailable, không chọn một nghĩa tùy tiện. Unsupported/thiếu chứng cứ/missingVI/
  down/timeout: unavailable, không result giả.
- Internal/DB lỗi: partial/failure trung thực; caller cancellation không success partial.
- Candidate correction: new version mới, old version nguyên; UPDATE/DELETE/TRUNCATE bị chặn.
- Anonymous HTTP v1 empty/v2 tối đa một/v3 nhiều phương án/unknown400; logs không raw selected text.

## 5. Freeze evidence

Report ghi corpus/dataset versions/hashes, required/covered/excluded và case counts,
commands/results, environment limits; acceptance manifest bind đúng G1/G2/candidate/report,
missing_required=0. Scoped acceptance không quyền production. Thiếu publication integration
dataset→release thì ghi dependency F-20 trước bất kỳ production activation nào.
