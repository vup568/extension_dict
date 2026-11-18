# Implementation Plan: Import đầy đủ dữ liệu từ điển V2

**Branch**: `feat/tokenizer-deinflection` (không chuyển branch) | **Date**: 2026-10-15 | **Spec**: [SPEC.md](SPEC.md)

**Input**: `.sdd/specs/002-import-dictionary-data/SPEC.md`

**Status**: User authorized implementation on 2026-10-15. P1 correctness and P2 parser/guarded import foundations implemented; source artifacts audited read-only, final integration verification in progress. Full real import, reviewed Vietnamese writer and activation remain incomplete. OD-009 is still mandatory before canonical use.

## Summary

Thay 17 fixture bằng pipeline backend cho toàn bộ JMdict Japanese/English, KANJIDIC2 và nguồn nghĩa Việt/overrides V1 được duyệt. Nạp thử vào DB PostgreSQL riêng, kiểm tra đầy đủ rồi user mới chuyển kết nối backend. Không dùng pointer KnowledgeRelease hiện tại để che partial import: repository chưa đọc theo release.

## Technical Context

**Language/Version**: C#14/.NET10; PowerShell7 cho operator trên Windows.
**Primary Dependencies**: EF Core/Npgsql đã có; System.Xml/System.Text.Json/GZipStream để đọc nguồn; Python/Sudachi gRPC hiện có cho F09, không thay tokenizer.
**Storage**: PostgreSQL16 trong Compose hiện tại; DB riêng đề xuất `jp_dict_real_<run-id>`, tên thực tế do operator xác nhận; artifacts lớn trong ignored `data/dictionary-import/<run-id>/`.
**Testing**: xUnit units, PostgreSQL Testcontainers integration, API contracts1/2/3, Vitest/typecheck/build và Chrome manual acceptance.
**Target Platform**: Windows local + Docker Linux containers; không sửa CI Ubuntu/deploy trong feature này.
**Project Type**: Operator CLI/scripts + backend read-path correctness + attribution presentation tối thiểu.
**Performance Goals**: Streaming/bounded batches, đo elapsed/peak memory/DB table+index bytes khi full import; đo warm lookup theo PERF-002, chưa chứng nhận SLA/OD-005.
**Constraints**: Không thêm dependency khi chưa duyệt; không reset, ghi DB gốc/demo, auto-import startup, hay commit datasets/secrets. Deadline Sidecar500ms và candidate F09 đã duyệt giữ nguyên.
**Scale/Scope**: Toàn bộ các nguồn đã pin, không common-only. Historical ~218k vocab/~10k kanji/~72k VI headwords chỉ là tham khảo, không là counts nghiệm thu.

## Constitution Check

*GATE trước research và sau design: PASS về thiết kế; không phải source approval hoặc production readiness.*

| Invariant | Thiết kế và điều kiện kiểm chứng |
| --- | --- |
| §§2–3 server-authoritative/Clean Architecture | Parser/persistence ở Infra, điều phối/ports ở UseCase, Domain thuần; extension chỉ nhận DTO. |
| §4 identity/restrictions | Giữ ent_seq/codepoint và semantic applicability, thêm no-kanji reading representation; negative tests qua API/F09. |
| §5 provenance/linguistic knowledge | Pins/hashes/manifests/counts; versioned mapping/overrides; candidate chưa duyệt không vào served tables. |
| §§7–8 privacy/providers | Không log selected text/connection string; source input validation và network chỉ trong fetch operator; gRPC adapter giữ nguyên. |
| §§9–10 human/test gates | Duyệt design → source/license → mapping/corpus → local cutover; không coi tests là approval. |
| DATA-005/F20 | Local candidate DB không tuyên bố production release; release-aware readers/publication hoàn chỉnh ngoài scope. |

Không có ngoại lệ hiến pháp. Việc giữ OD-009 chưa đóng là fail-closed execution gate, không mặc định cho phép dữ liệu Việt.

## Project Structure

### Documentation (this feature)

- `SPEC.md`, `PLAN.md`, [research.md](research.md), [data-model.md](data-model.md).
- [contracts/operator-import.md](contracts/operator-import.md), [quickstart.md](quickstart.md).
- [checklists/requirements.md](checklists/requirements.md), [validation.md](validation.md).
- `TASKS.md` sẽ tạo sau khi user duyệt; không được tạo bởi bước plan này.

### Source Code (repository root)

Các đường dẫn dưới đây mô tả thiết kế; trạng thái thực thi xem TASKS và progress cuối file. Scripts prepare/start-real và full validation/publication vẫn chưa triển khai:

- `src/domain/Entities/Reading.cs`: no-kanji flag thuần dữ liệu.
- `src/usecase/DictionaryImport/`: orchestration, validation/report ports/models.
- `src/infra/DictionaryImport/`: source parsers, normalizer, bounded DB writer/checkpoint/source policies.
- `src/infra/Persistence/{Configurations,Migrations}/`: forward migration tối thiểu cho reading/run control nếu cần.
- `src/interface/DictionaryImportCommand.cs`, `Program.cs`: explicit operator dispatch trước HTTP; không auto-import.
- `scripts/{prepare-real-dictionary,start-real-dictionary}.ps1`: operator safety/config wrappers.
- `src/usecase/Dictionary/LookupWordUseCase.cs`, DTOs/endpoint version boundary và `extension/src/{api,ui}/`: chỉ thay đổi được chứng minh cần cho correctness/no-kanji/attribution.
- `tests/{unit,integration}/`, `extension/tests/`: fixtures nhỏ có provenance; không copy full dataset vào tests.

**Structure Decision**: Tận dụng 4 layer và entities hiện có, không thêm service/microservice/NLP library.

## Ordered Implementation Phases

Các phase bên dưới là thứ tự thiết kế đã duyệt triển khai. TASKS và progress cuối file ghi bằng chứng đã có; quyền triển khai không thay source/license/mapping/activation approval.

### P0 — Source/license lock và tiêu chí nghiệm thu (G0a → G0b)

1. **G0a**: Kiểm tra metadata/source/license dự kiến; xin user cho phép tải đúng artifacts JMdict Japanese/English full XML (không examples), KANJIDIC2 XML và VI candidate phục vụ audit. G0a chưa là canonical-use approval;50 overrides lấy từ V1 commit đã pin.
2. Ghi exact source date/release/commit, artifact URL, compressed+expanded SHA256/schema, counts, transformation version, license evidence/notices và attribution/update policy. Không `latest` khi replay.
3. Nguồn Việt `star_nhatviet.tab` chỉ là candidate; kiểm tra upstream lineage/license cụ thể, không suy từ CC0 của mirror. User duyệt OD-009 bằng decision record riêng; nếu không chứng minh được, dừng nhánh VI canonical và hỏi nguồn thay thế, không gọi feature hoàn tất.
4. Chốt corpus độc lập từ nguồn: ít nhất30 ca (từ ngoài demo, homograph/restrictions/no-kanji, Unicode, kanji, VI mapping, F09). User duyệt expected meanings/canonical targets trước nghiệm thu.
5. Sau G0a mới fetch vào cache riêng, không DB writes; verify bytes/schema/license, hoàn thiện source lock và corpus từ pinned files để **G0b** duyệt. Không yêu cầu checksum của file chưa được phép tải; extra package/download ngoài exact scope phải hỏi.

**Exit G0b**: Source lock không còn giá trị chưa pin; license decision cho từng component, corpus có reviewer. Mapping hash cuối cùng bổ sung sau P3, trước full import P4. Sources chưa đạt không được bypass; nếu OD-009 chưa clear, P3 chỉ là investigation, không cấp canonical write/activation.

### P1 — Correctness foundation trước import đầy đủ

1. Viết red tests cho `re_restr`, `re_nokanji`, `stagk`/`stagr`, POS kế thừa, sense-level gloss và pending/rejected VI.
2. Bổ sung `Reading.IsNoKanji` bằng forward migration; parser và joint-pair logic phân biệt unrestricted với kana-only reading. Không sửa migrations cũ.
3. Sửa projection khi test chứng minh trả sense/reading không hợp lệ; kiểm tra cùng chính sách trên exact và F09 candidate path, không chỉ attach restriction DTO rồi coi đã filter.
4. Preserve POS codes tương thích candidate (`v1`, `v5*`, `vk`, `vs*`, `adj-i`, `adj-na`, `n`); kiểm tra thực tế với source. Sense keys ban đầu chỉ ổn định trong pinned snapshot; không coi số thứ tự sense là upstream ID xuyên mọi phiên bản.
5. Chỉ nghĩa Việt nguồn thật có `ReviewStatus=approved` và reviewed mapping bound đúng sense được phục vụ; null/unknown/pending/rejected ở ngoài live glosses. Giữ demo synthetic status riêng, không đổi `demo` thành production approval.
6. Không thêm API version: `IsNoKanji` chiếu qua `ReadingDto.Info` hiện có bằng marker chuẩn hóa `reading:no-kanji`, giữ raw source tags riêng; popup hiển thị “chỉ dùng kana”. Projection chỉ trả pairs hợp lệ, kana-only match không ghép với written forms. Tests xác nhận schemas1/2/3 không đổi và marker không làm các client cũ hiểu sai association.

**Exit**: Unit/integration negative fixtures green; migration replay an toàn, F09 frozen bytes không đổi.

### P2 — Triển khai importer và test bằng fixtures nhỏ

P2 chỉ xây/test cơ chế với fixtures có license rõ và DB test cô lập; **chưa chạy full import nguồn thật**. Không tạo run thật rồi đổi mapping ở P3; source/mapping hash cuối phải khóa trước full run ở P4.

1. Explicit CLI: inspect → import/resume → validate, mặc định không fetch/ghi. Wrapper kiểm tra native `docker.exe`, PowerShell7 và loopback target.
2. Bắt buộc target mới riêng do workflow sở hữu + exact-name confirmation; từ chối DB gốc, demo, system DB, non-loopback, unknown/mixed data, target đang phục vụ. Tạo DB/ownership qua explicit prepare, không dùng ownership marker đơn lẻ để nhận DB lạ.
3. Khóa run theo DB; xác minh pins/checksums/schema trước ghi; XML resolver không network/file entities, giới hạn expanded input/entity/depth/record size; dùng internal entity table đã pin cho JMdict POS. Không tải external DTD khi parse.
4. Stream entry/character → normalize → write theo batch bounded (mặc định500 records, có bounds được test); transaction gồm source record + canonical graph + checkpoint của batch. Không giữ toàn bộ EF graph trong memory.
5. Canonical ID dự kiến `dictionary:jmdict:<ent_seq>`, `kanji:<UPPERCASE_HEX>`; surrogates không là identity public. Lưu raw có kiểm soát/phù hợp license cùng provenance; preserve correctness fields, phần khác trong source metadata.
6. Resume chỉ cùng sources/transformation/mapping hashes; validate checkpoint+DB state; cùng completed run là verified no-op, conflict/different inputs phải tạo DB run mới, không upsert phá data cũ.
7. Không merge toàn nguồn vào một transaction khổng lồ; candidate DB luôn chưa phục vụ nên batch commits không lộ partial data.

**Exit**: Tái tạo/idempotency/resume/failure/concurrent/target-refusal tests đạt; old/demo DB không đổi.

### P3 — Chuẩn bị offline nghĩa Việt và kanji enrichment có review

Đọc pinned source để tạo/review mapping artifacts trước full import; target dùng source identity/canonical key/sense facts hash, không phụ thuộc surrogate DB ID chưa tồn tại.

1. Parse toàn bộ VI records losslessly; record identity theo source snapshot+ordinal, không lấy headword làm unique ID và không cắt160ký tự/4 nghĩa.
2. Gợi ý liên kết bằng form/reading/POS rồi review semantic sense; reviewer quyết định theo record và target sense/form/reading. Batch approval được phép khi có evidence từng mapping và rule/policy rõ; không dùng cùng spelling làm approval.
3. Giữ missing/ambiguous/English-residue/unlicensed ở quarantine có reason; approved mới vào LocalizedGloss và editorial mappings. Chốt disposition toàn bộ50 overrides, không copy runtime constants.
4. Hán Việt lấy `r_type=vietnam` riêng; Kanji.MeaningsVi cần mapping nghĩa riêng tới character sense, không lấy mọi nghĩa từ vựng một chữ. Giữ rmgroup/variants/source refs trong metadata, JLPT approximate có recipe/version.
5. Nếu chưa đạt coverage/disposition user chốt hoặc nguồn license còn vướng, báo partial và dừng activation; không tự giảm phạm vi thành English-only.

**Exit**: Coverage/reject/quarantine report và reviewed mapping artifact có hash, user duyệt; bind hash vào final source lock trước P4, không fabricated approval.

### P4 — Full import nguồn thật, đối soát và E2E

Sau P0–P3, user duyệt quyền chạy exact pinned sources/mappings và target mới. Prepare DB riêng → inspect → full import/resume → validate; không phục vụ khi loading. Source lock/mapping khác phải tạo run/DB mới, không enrich run cũ bằng inputs đã đổi.

Bootstrap trước validation/seal: apply migrations nguyên bản để có F09 datasets và cài/verify đúng5 grammar rules từ seed hiện có (hash/provenance ghi report), không thêm nội dung grammar. Không dựa vào lazy seed trong request đầu: `EfGrammarRepository` đang nuốt seed failure, runtime canonical-read-only có thể thiếu grammar âm thầm. Thiếu/khác seed hoặc dataset pin thì fail readiness.

1. Validate counts per source: input=accepted+quarantined+rejected (categories rời nhau); normalized counts/links riêng, overrides riêng; JMdict/KANJIDIC hợp lệ không được mất entry.
2. Kiểm tra source IDs/FKs/applicability/POS, sanitized output, attribution. Hash canonical facts theo stable keys, bỏ timestamps/surrogate IDs/checkpoints; hai rebuild cùng pins phải giống.
3. Đo elapsed/peak memory/input-expanded bytes/table+index sizes; disk preflight tính cả raw, staging, report và rollback DB. Ghi số thật, không hứa dung lượng/free-tier hoặc SLA.
4. Chạy Release build, units, PostgreSQL integration, FE typecheck/tests/build và API1/2/3 (không thêm version4); F09 immutable corpus chạy nguyên bản, real-source corpus là bộ bổ sung riêng.
5. Test endpoint trên candidate connection riêng không ghi .env; Chrome load unpacked thủ công với corpus đã duyệt (Brave automation không thay Chrome evidence). Giữ case 食べました/base, cả ambiguity và na/noun.
6. Lưu report provenance/hashes/actual responses đã redacted; chứng minh DB gốc/demo unchanged; performance samples không là OD-005 certification.

**Exit**: Tất cả SC có evidence và user duyệt; pending VI/source gate thì không đánh dấu full-ready.

### P5 — Local cutover và handoff (G1)

1. Chỉ sau user approval bound vào target/validation hash mới `start-real-dictionary.ps1`; recheck state/hashes, tạm set connection+candidate F09 pin và IPv4 Sidecar như launcher hiện tại, restore env finally.
2. Dừng/restart backend theo operator, không đổi .env/DB gốc/demo; không import/migrate canonical data trong HTTP startup. Startup chỉ dùng migrations như hiện tại, với schema candidate đã validate.
3. Rollback bằng Ctrl+C rồi launcher cũ/config cũ, không drop/rename/reset DB. Target đã ready/served không nhận resume hay mutations; update dùng DB run mới.
4. Cung cấp nguồn/attribution links truy cập từ popup, update runbook; production F20 và server deployment vẫn chưa được phép.

**Exit**: User xác nhận Chrome/lookup/kanji/F09; giữ DB/artifacts để quay lại. Không commit/push/merge nếu chưa được yêu cầu.

## Traceability and Verification

| Spec | Phase | Bằng chứng yêu cầu |
| --- | --- | --- |
| FR-001,002,005 | P0–P2,P4 | Full source/counts, kanji field provenance, no-kanji/restriction tests |
| FR-003,004,007 | P0,P3 | License decision, VI mapping/quarantine, 50 override dispositions |
| FR-006,008,010 | P0,P2,P4 | Source lock, deterministic rebuild/resume, reconciliation/size report |
| FR-009 | P2,P5 | Target guards, ownership, hashes old/demo unchanged, rollback |
| FR-011–013 | P1,P4 | Exact-first/POS/F09, contracts, privacy, approved real corpus/Chrome |
| FR-014 | P0,P4,P5 | Attribution UI + notices/update runbook |

## Complexity Tracking

Không có vi phạm cần ngoại lệ. DB candidate riêng là giải pháp local nhỏ nhất vì readers chưa release-aware; không thiết kế lại F20 hoặc xây hệ thống deployment ở đây.

## Planning Progress and Issues

- [x] Scope/Shadow Plan/Proceed; template resolution và setup-plan đúng thư mục mới.
- [x] Phase0 research: V1, V2, metadata/license; quyết định candidate isolation và fail-closed gates.
- [x] Phase1 design: model, operator contract, quickstart, traceability và Constitution recheck.
- [x] User requested implementation against SPEC/PLAN; TASKS created with independent source/mapping/activation gates.
- [x] G0a audit downloads and complete read-only inventories: JMdict218914, KANJIDIC13108, VI172321, V1overrides50.
- [x] P1 no-kanji/joint applicability/review filtering; forward migration and compatible popup marker.
- [x] P2 parser/CLI/run-control/target/batch/resume/facts foundations implemented; fixture tests and final suite evidence tracked in [execution validation](reviews/execution-validation.md).
- [x] Independent proposed40-case source corpus and lossless VI/override inventories prepared; none of these artifacts is owner semantic approval.
- [ ] G0b/OD-009 decisions, corpus approval and explicit correction decision for JMdict1339160/sense2.
- [ ] P2/P3 remaining: operator prepare wrapper, reviewed VI canonical mapping writer and complete readiness/metrics/receipt workflow.
- [ ] P4 full import and independent approved corpus/Chrome acceptance; original/demo preservation fingerprints for that run.
- [ ] P5 target-bound approved local cutover/attribution/rollback launcher.

Issues: nguồn Việt chưa clear OD-009; original OVDP archive retrieval currently yields HTML rather than a ZIP. JMdict1339160/sense2 requires a written form and a no-kanji reading simultaneously; it remains raw-inventoried and canonical-blocked, not auto-corrected or dropped. SourceManifest/KnowledgeRelease and successful fixture tests do not prove full import or publication safety. Setup-plan returned feature directory as branch context; actual Git branch remains feat/tokenizer-deinflection. Final execution evidence is separate from the historical documentation-only [validation](validation.md).
