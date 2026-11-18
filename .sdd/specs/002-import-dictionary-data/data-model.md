# Data Model — Import từ điển thật

Thiết kế dự kiến, **chưa migration/schema/runtime changes**. Theo [PLAN](PLAN.md), mọi candidate ở DB riêng chưa phục vụ.

## 1. Existing canonical graph và mapping nguồn

| Entity | Mapping/invariant |
| --- | --- |
| DictionaryEntry | CanonicalId=`dictionary:jmdict:<ent_seq>`; giữ source identity; không ghép hai entry vì cùng chữ. |
| WrittenForm | `keb`, info/priority/common; giữ source ordering metadata, không common-only filter. |
| Reading | `reb`, `re_restr`, info/priority; **thêm IsNoKanji=false** bằng forward migration. NoKanji=true không ghép với WrittenForm; null/empty restrictions không được thay thế flag này. |
| DictionarySense | SenseKey lần đầu từ source ordinal trong snapshot; Position/POS giữ source codes và inheritance. Không hứa ordinal không đổi sau source update; future cross-release equivalence cần review. |
| SenseApplicability | Hợp lệ theo joint written+reading pairs: (stagk AND stagr) AND reading restriction/no-kanji. Nhiều giá trị cùng chiều là OR; không tạo hai wildcard rows thành OR nới lỏng cả hai chiều. |
| LocalizedGloss | LanguageTag en/vi; SourceRecordId bắt buộc với imported gloss, ReviewStatus approved mới được phục vụ. Unknown/pending/rejected vi không nhập vào served senses. |
| KanjiRecord | CanonicalId=`kanji:<UPPERCASE_HEX>` từ Unicode scalar, giữ Character. Nét chính/classical radical có source evidence; variants/rmgroup/rawJLPT giữ metadata. HanViet riêng MeaningsVi; nghĩa Việt chỉ sau mapping duyệt. |

Với reading-only/no-kanji lookup, pair (Written=null, Reading=id) được phép; sense đòi written form cụ thể không match pair này. Với written-form lookup, reading no-kanji không trở thành reading hợp lệ của chữ đó. API projection/F09 phải cùng semantics, có negative tests. `ReadingDto.Info` hiện có mang marker chuẩn hóa `reading:no-kanji`; không thêm field/version. Raw source info lưu riêng; popup gắn label “chỉ dùng kana”, projection không ghép written forms cho no-kanji match.

## 2. Provenance và editorial artifacts

- **SourceManifest** hiện có: source/publisher/version/license/attribution/source checksum/counts/transformation/validation; Metadata chứa artifact/schema hashes, normalized hash, quarantine count, notice refs.
- **SourceRecord** hiện có: unique(ManifestId,SourceIdentity), RecordData lossless facts/raw reference. JMdict ID=ent_seq; kanji=codepoint; VI không có upstream ID dùng snapshot+record ordinal, không dedupe theo headword.
- **EditorialMapping** hiện có chỉ link resource cấp entry/kanji; **không đủ mô tả sense**. Mapping artifact versioned bổ sung exact SourceRecord identity + canonical target + SenseKey/sense facts hash + allowed form/reading + decision/reason/reviewer/date + review evidence. LocalizedGloss.SenseId xác định đích cuối; field-level kanji provenance lưu SourceMetadata tham chiếu manifest/record/decision.
- **Override inventory**:50 items lấy từ pinned V1; disposition approved/rejected/quarantined, canonical target/reason/provenance/version. Không auto-approve từ V1 runtime constants.
- **Source lock**: artifact URLs, exact versions/commits, source/compressed/expanded/schema/license hashes, transform/policy/mapping hashes, component license approval references. Không placeholder hoặc latest trong run được phép ghi.

## 3. Import control trong DB riêng (mới, không là KnowledgeRelease)

- **ImportRun**: RunId, lockHash, transformHash, mappingHash, TargetIdentity, ownership version, State, start/end, validationHash, factsHash. TargetIdentity kiểm tra server/DB name thực tế qua DB session, không tin CLI name đơn lẻ.
- **ImportCheckpoint**: RunId+source+ordinal, last committed source identity, batch checksum/counts. Graph+checkpoint một transaction; lock độc quyền ngăn hai writers; re-read và đối chiếu counts/hashes khi resume.
- **ImportReport**: per-source input=accepted+quarantined+rejected; mapping/gloss counts tách khỏi input; overrides disposition; failures, normalized facts hash, sizes/time/memory, independent corpus result, user approval refs.

Run/checkpoints phải có forward schema, không dùng file tiến độ thay DB transaction. Có thể lưu report/control payload JSONB nhưng trường khóa/trạng thái và uniqueness phải enforce. Timestamps/DB surrogate keys không thuộc canonical facts hash.

## 4. State và safety

`new → loading → imported → validated → approved_for_local_test → sealed`.

`loading → failed/cancelled → loading` chỉ cùng inputs+unserved owned target; checksum/conflict/unknown schema thì fail closed. Validate và activation không được giả lập approval; receipt bound vào run+target+lock+facts+validation hash. Dữ liệu đổi thì receipt vô hiệu và phải validate/review lại.

Importer không ghi target sealed/đang phục vụ; launcher chỉ chạy target có receipt đúng, dùng quyền đọc canonical (không dùng quyền writer của importer). Bootstrap migrations/F09 datasets và verify5 grammar rules hiện có trước seal, không trông chờ request đầu ghi lazy seed; report giữ seed hash/provenance. Startup schema đã up-to-date, account runtime có quyền đọc EF history và các tables cần thiết; thiếu quyền/bootstrap fail trước local readiness. Freeze là cam kết workflow local, không thay production immutable revisions F20. Cập nhật nguồn bằng DB/run mới; không sửa release cũ. Original/demo/system DB luôn bị chặn, không tự drop/reset khi lỗi.
