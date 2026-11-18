# Planning Validation — 2026-10-15

**Scope**: Chỉ SPEC/PLAN/design; không phải evidence importer hoặc dữ liệu thật hoạt động.

## Workflow và kiểm tra

- User xác nhận đủ vocabulary/kanji/VI tương đương V1 và Proceed Shadow Plan tài liệu.
- Main đọc skills specify/plan, AGENTS, Constitution, memory và templates; resolved core spec-template/plan-template. Sequential directory tiếp sau001 là002; feature pointer chuyển sang thư mục mới, không sửa F09 SPEC/PLAN/TASKS.
- `setup-plan.ps1 -Json` chạy đúng feature dir; branch resolver trả directory context, Git branch thực tế vẫn `feat/tokenizer-deinflection`, không tạo/chuyển branch.
- `check-prerequisites.ps1 -Json` tìm được research, model, contracts và quickstart. Không dùng RequireTasks: TASKS chưa được yêu cầu/tạo.
-14 functional requirements,6 success criteria,3 user stories; mandatory sections/traceability có đủ, không template placeholder/clarification marker bỏ sót.
- Local Markdown links13/13 và whitespace8files đạt read-only checks; lần kiểm tra sớm thấy link validation.md chưa tạo và1trailing space, đã sửa rồi kiểm tra lại.
-249 file runtime/source/test/script được fingerprint trước-sau: không đổi.12 hash bindings F09 acceptance-manifest đều khớp.
- Không chạy lại build/runtime tests vì chỉ chỉnh tài liệu; các lệnh quickstart được kiểm tên solution/projects/npm scripts, **chưa được chạy như evidence feature import**.
- `.specify/extensions.yml` không tồn tại ở pre/post checks nên không có hook specify/plan phải dispatch.

## Independent design review

Research agents chỉ đọc V1/V2, không viết code/DB. Review phát hiện và design đã sửa:

1. No-kanji reading dùng persisted flag + existing Info marker; API1/2/3 giữ schema, lọc joint pairs; không mở version4 chưa cần.
2. Real VI chỉ approved+mapping-bound, unknown/null/pending/rejected nonserved; demo không là production approval.
3. G0a cho metadata/license/download authorization, G0b verify pinned bytes/corpus sau download: bỏ vòng lặp bắt checksum trước tải.
4. P2 chỉ implementation/test fixtures; P3 review mapping offline; full real import P4 sau khóa final mapping hash, không đổi input giữa run.
5. Candidate bootstrap/verify existing5grammar rules và F09 migration datasets trước read-only launch; không dựa vào lazy seed trong analysis.

## Traceability outcomes

| Acceptance group | Design location | Runtime status |
| --- | --- | --- |
| Full vocab+kanji, restrictions | PLAN P0–P2/P4, data-model §1 | Chưa triển khai |
| Full VI source/50 overrides, safe sense mapping | PLAN P0/P3, research R1/R2/R5 | Chưa review/download/import |
| Safe deterministic import/resume | PLAN P2/P4/P5, operator contract | Chưa triển khai |
| F06/F09/API/Chrome/attribution | PLAN P1/P4/P5, quickstart | Chưa real-data test |

**Result**: Bộ thiết kế sẵn để user duyệt/tạo TASKS. Không đồng nghĩa nguồn Việt đã clear OD-009, full dataset đã pin, corpus đã duyệt, importer hoàn tất, production KnowledgeRelease/F20 hoặc Git delivery.
