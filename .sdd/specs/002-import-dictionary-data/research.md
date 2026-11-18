# Research — nguồn V1 và import V2

Ngày khảo sát: 2026-10-15. Chỉ đọc Git/source và web documentation/license metadata; không tải dictionary archive, không truy vấn/sửa DB.

## R1 — Phạm vi V1 đã tồn tại, nhưng V2 chưa có importer thật

**Decision**: Giữ phạm vi đầy đủ Japanese/English vocabulary + kanji + Vietnamese enrichment; xây ETL backend, không khôi phục IndexedDB/packed output làm canonical.

**Evidence**: V1 pin `788e93156f56dd735e16c3c99a209c5eeca089dd` (`extension_v1`); đọc bằng `git show <commit>:<path>`:

| V1 path | Facts và disposition V2 |
| --- | --- |
| `scripts/prepare-dict.ts` | Fetch `jmdict-eng-*` full qua scriptin release/latest; bỏ sense restrictions, skip no-English entries. Giữ phạm vi nhưng sửa mất restriction; không giữ `latest` cho replay. |
| `scripts/prepare-kanji.ts` | `kanjidic2-en-*`; Hán Việt từ reading type=vietnam, JLPT cũ chuyển mức ước lượng. Giữ source/derived riêng. |
| `scripts/prepare-javi.ts` | `https://raw.githubusercontent.com/catusf/tudien/master/dict/star_nhatviet.tab`; pivot/cleaning, chọn duplicate đầu, cắt160ký tự/4 nghĩa. Kiểm kê lại mọi record, không sao chép mất dữ liệu. Có50 overrides ở V1 tip, không phải con số47 trong mô tả lịch sử. |
| `src/core/dictionary/{lookup,kanji}.ts` | VI lookup theo form/reading, kanji VI từ javi một ký tự. Đây không là semantic sense approval. |
| `src/core/dictionary/{db,importer}.ts` | IndexedDB3, per-chunk transaction/progress; chỉ tái sử dụng bài học resume atomic, không browser canonical store. |

Các commit `17ec6bd`, `cb0c5c0`, `0ca8e87` từng ghi ~218k vocab,10,384kanji/~9,761Hán Việt,~71.9k retained VI/~172.3k input. Đây là **con số lịch sử**, chưa đo lại. V2 cleanup `d1b1dea430d371a447d5178e49c8440818a38aa0` gỡ pipelines; SourceManifest/schema không chứng minh đã nạp data. Xem MIGRATION_DECISION §§5.1–5.2,5.5,6.2.

**Alternatives**: Copy packed V1 nhanh nhưng mất constraints; common-only/demo trái phạm vi; dịch máy thay dữ liệu Việt không được user yêu cầu.

## R2 — Source/license policy: tách dữ liệu chính và nghĩa Việt

**Decision**: Chọn JMdict Japanese/English full XML và KANJIDIC2 XML từ publisher; pin bytes/schema trước mỗi run. VI V1 giữ candidate tới khi OD-009 được duyệt bằng evidence cụ thể, không gán GPL/CC0 tùy suy đoán.

**Sources checked**:

- [EDRDG licence](https://www.edrdg.org/edrdg/licence.html): Japanese/English JMdict và KANJIDIC2 theo CC BY-SA4.0; cần attribution/notice và quy trình cập nhật. Component ngôn ngữ khác có copyright riêng. Quyết định: không lấy multilingual glosses/examples ngoài phạm vi; lưu notice và update runbook cho từng snapshot.
- [JMdict DTD](https://www.edrdg.org/jmdict/jmdict_dtd_h.html): entry ID, reading restrictions/no-kanji, sense restrictions và POS kế thừa. Quyết định: giữ semantics và entity codes, không chỉ prose POS sau XML expansion. DTD đang dùng phải pin cùng source, không coi tài liệu HTML cũ là schema hiện hành.
- [KANJIDIC documentation](https://www.edrdg.org/wiki/KANJIDIC_Project.html): `r_type=vietnam` là **cách đọc**; meanings có language tags riêng. Quyết định: không suy nghĩa Việt từ âm Hán Việt.
- [SKIP publisher](https://www.kanji.org/dictionaries/skip_permission.htm) hiện ghi CC BY-SA4.0, khác giả định non-commercial trong code V1. SKIP vẫn ngoài trường hiển thị cần import này; nếu giữ trong raw source phải kiểm kê notice riêng, không lặp lại claim cũ.
- [catusf mirror LICENSE](https://raw.githubusercontent.com/catusf/tudien/master/LICENSE) ghi CC0 và giới hạn quyền bên thứ ba; không đủ chứng minh license riêng của `star_nhatviet.tab`. V1 README gán FVDP GNU GPL/OVDP GPLv2 là **claim lịch sử**, chưa là clearance.
- [FVDP project help](https://vietdict.viet.net/help.html) xác nhận project/contributors; địa chỉ gốc `https://www.informatik.uni-leipzig.de/~duc/Dict/` trả404 khi kiểm tra. Metadata mirror cụ thể chưa lấy được; không diễn giải “public URL” thành quyền dùng.

**Rationale**: REQUIREMENT DATA-004 nêu chính FVDP/OVDP phải non-canonical khi source/license/identity/quality chưa duyệt. Việc user chốt đủ3nhóm chỉ chốt phạm vi.

**Alternatives**: JSON từ [jmdict-simplified](https://github.com/scriptin/jmdict-simplified/blob/master/README.md) có full/common/language variants và license data riêng; là reference V1, không chọn packed V1 hay converter phụ làm canonical input. Không thêm Java/npm converter dependency; dùng upstream XML và .NET có sẵn.

**Gate G0 evidence cần có khi triển khai**: source snapshot+SHA256+license document đúng artifact; copyright/notice/redistribution decision cho VI; nếu không chứng minh được phải hỏi user đổi nguồn, không âm thầm bỏ nhóm VI. Đây là acceptance prerequisite đã thiết kế, không phải đã được giải quyết/duyệt trong nghiên cứu này; không có legal clearance được tạo thay user.

## R3 — Reuse model, sửa correctness gaps trước full import

**Decision**: Tái sử dụng DictionaryEntry/Reading/Sense/Gloss/Applicability, KanjiRecord, SourceManifest/Record, EditorialMapping; chỉ forward migration tối thiểu cho no-kanji và run control.

**Evidence**:

- `src/domain/Entities/Reading.cs`: RestrictedToForms null/empty=mọi form, thiếu `re_nokanji` flag. Raw JSON không bù được semantics sai ở runtime.
- `src/usecase/Dictionary/LookupWordUseCase.cs`: projection lấy mọi sense và mọi vi gloss bất kể ReviewStatus. Candidate F09 có joint applicability check, nhưng không tự bảo đảm mọi sense trả về đều phù hợp.
- `src/infra/Persistence/Repositories/Ef{Dictionary,Kanji}Repository.cs`: đọc trực tiếp tables, không scope theo release/current pointer.
- `src/infra/Persistence/KnowledgeReleases/EfKnowledgeReleasePublication.cs`: publication transaction của metadata/pointer không phải isolation cho canonical readers.
- `src/infra/Persistence/Repositories/EfGrammarRepository.cs`: lazy seed5 rules khi DB trống và nuốt seed exception. DB candidate cần bootstrap/verify seed hiện có trước seal/read-only launch; không đổi nội dung grammar hoặc coi migrations tự seed bảng này.
- `src/infra/Conjugation/Data/candidate-20261013.1.json`: chấp nhận JMdict POS codes; giữ source codes, không chỉ generic verb/noun.

**Alternatives**: New universal schema/F20 redesign quá rộng; status flag không được reader kiểm tra gây lộ candidate; không sửa freeze migration/dataset để thích ứng importer.

## R4 — Isolated local candidate, deterministic streaming/resume

**Decision**: Một DB riêng mỗi source lock/run; batch transaction gồm graph+checkpoint, khóa writer; không phục vụ khi loading. Sau validate/user approval mới config switch+restart. Rollback là quay cấu hình, không reset DB.

**Rationale**: Không có release-aware readers. Ownership+protected-target guards, canonical hash và explicit ready receipt giúp không lẫn original/demo hoặc batch dở. Cùng pins/mapping/transform tái tạo cùng facts; timestamps/surrogates không tham gia facts checksum.

**Alternatives**: Import trực tiếp DB đang dùng hoặc chỉ đổi KnowledgeRelease pointer không an toàn; full transaction lớn không cần thiết khi candidate DB chưa phục vụ. Production publication/immutable readers vẫn thuộc F20, không được gọi hoàn tất ở đây.

## R5 — Vietnamese mapping và completeness không được đánh tráo

**Decision**: Form/reading/POS chỉ đề xuất; reviewer duyệt semantic target sense và applicability bằng versioned artifact. All VI input và50 overrides có disposition, unresolved giữ quarantine. Kanji Vietnamese meaning cần decision riêng, không tự lấy từ headword một chữ.

**Rationale**: Source VI không chung stable identity/sense với JMdict. Không thể hứa100% JMdict có bản dịch Việt; cũng không được gọi “full” khi mới có JMdict/KANJIDIC hoặc bỏ qua candidates không thống kê. User duyệt coverage/disposition report; source bị license block không được miễn bằng quality signoff.

**Alternatives**: Auto-merge homographs, blanket approve hoặc reuse truncated javi vi phạm ID-004/VOC-008; hàng chục nghìn dòng expected không được dùng làm runtime dictionary.
