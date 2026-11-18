# Công việc triển khai — 5 nhóm Yomitan

Trạng thái 2026-11-04: chỉ bộ tài liệu được tạo; tất cả checkbox dưới đây là công việc code/kiểm chứng tương lai. Tham chiếu [SPEC](SPEC.md), [PLAN](PLAN.md) và từng spec con, không lấy checkbox tài liệu làm bằng chứng nghiệm thu runtime.

Danh sách dưới đây là baseline phạm vi. Bản chi tiết có phụ thuộc, nơi sửa code, output và nghiệm thu nằm ở [task.md](../../../plans/261104-yomitan-reading-experience/task.md); cập nhật tiến độ code tại bản chi tiết đó để không duy trì hai checklist độc lập.

## Nền tảng và hợp đồng

- [ ] T01 — Review defaults DD01–DD08 và 5 spec; ghi những thay đổi sau phản hồi chủ dự án. (G-A)
- [ ] T02 — Pin revision Yomitan, lập manifest nguồn tham khảo/copy nếu có, rà license/dependency trước ghép. (G-D)
- [ ] T03 — Contract tests scan/speech, payload limits, error envelope, correlation, quota và cancellation; giữ v1/v2/v3 cũ. (G-C/G-D)

## Nội dung popup — YUI

- [ ] T04 — Xây cấu trúc câu/danh sách từ/chi tiết occurrence, không lặp heading cùng giá trị. (YUI)
- [ ] T05 — Reading/meaning/restrictions/provenance và mọi alternative; chọn reading tường minh khi nhiều lựa chọn. (YUI/YAU)
- [ ] T06 — Thu gọn dịch/kanji/grammar/thể chia; trạng thái rỗng/partial/lỗi và navigation keyboard. (YUI)
- [ ] T07 — Kiểm thử cụm lặp, đồng âm, zero vocabulary, lỗi một capability, API text an toàn và preserved translation. (YUI/G-C)

## Hành vi popup — YPO

- [ ] T08 — Resize clamp, kích thước responsive và settings chỉ chứa preference. (YPO)
- [ ] T09 — Pin phiên hiện hành, vào popup không requery; Escape/selection replacement hủy đúng và stop audio. (YPO)
- [ ] T10 — Kiểm focus, drag end/blur, top layer fallback, zoom, scroll, iframe; không đổi hành vi trang. (YPO/G-E)

## Quét từ — YSC

- [ ] T11 — Opt-in + Shift/dwell, loại trừ editable/password/hidden/ruby annotations và boundary không hỗ trợ. (YSC)
- [ ] T12 — DOM window/anchor map bounded UTF-16, giữ surrogate và ranh giới block; không dùng selection fallback rộng. (YSC)
- [ ] T13 — `/api/scan` tái dùng backend occurrence; chọn target chứa offset; từ đơn không occurrence và no-match rõ. (YSC)
- [ ] T14 — Transport/cancel, dedupe current target, dispatch budget/quota, lỗi không giả word match. (YSC)
- [ ] T15 — Unit+Postgres/Sudachi+packaged browser cho pointer giữa từ ghép/thể chia, ruby/emoji, rê nhanh, selection override. (YSC/G-B)

## Độ đúng thể chia — YCO

- [ ] T16 — Lập và freeze gap catalog: source Yomitan chỉ gợi ý, đáp án ngôn ngữ độc lập và negative controls. (YCO)
- [ ] T17 — Corpus version mới, các nhóm có ví dụ phải giữ mọi phân tích hợp lệ; không overwrite bản nghiệm thu. (YCO)
- [ ] T18 — Cập nhật rules dạng dữ liệu, engine chỉ nếu có căn cứ; xác minh JMdict/POS/applicability, exact-first. (YCO)
- [ ] T19 — Corpus mới+cũ, normalization/offset và no-explanation fallback trong câu/scan, giới hạn runtime. (YCO/G-C)

## Phát âm AI — YAU

- [ ] T20 — Probe model TTS Gemini và voice Nhật bằng key cấu hình trên server; xác minh free eligibility/quota, ghi model thực dùng. (YAU/G-F)
- [ ] T21 — Implement port/adapter speech + config/host allowlist, no redirect/retry, timeout và format conversion validated. (YAU)
- [ ] T22 — Endpoint dictionary reading identity hoặc selected Japanese sentence; validate payload/deadline/quota, giữ key private. (YAU)
- [ ] T23 — UI nút nghe/stop/replay, disclosure và source AI; quản lý Blob lifecycle, stale, một playback hiện hành. (YAU/YPO)
- [ ] T24 — Fake provider failures + audio parser tests + nghe live độc lập, homophone/readings, không gọi tự động hay fallback tính phí. (YAU/G-F)

## Nghiệm thu cùng đợt

- [ ] T25 — Kiểm thử liên nhóm selection→hover→pin→audio→đổi từ→dịch→Escape, ignored abort và không gửi context sai. (G-B)
- [ ] T26 — Privacy/log/network/settings inspection; dependency/source manifest và không lưu raw text/audio. (G-D)
- [ ] T27 — Unit/type/build/integration/browser matrix và performance report; ghi rõ browser/live provider chưa test. (G-C/G-E)
- [ ] T28 — Review độc lập toàn bộ AC, hoàn thiện traceability, docs sử dụng và gói extension; chỉ đánh dấu xong khi đủ 5 nhóm. (G-A–G-G)
