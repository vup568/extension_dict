# Kế hoạch tham khảo Yomitan cho trải nghiệm đọc

Ngày 2026-11-04. Đây là mục lục của bộ đặc tả và kế hoạch, chưa phải tính năng đã được triển khai.

Spec tổng và 5 spec tính năng được tổ chức cùng 8 mục theo yêu cầu: Context & Goal → Actors & Roles → Functional Requirements → Non-functional Requirements → Data Model → Error Handling → Acceptance Criteria → Out of Scope. Các hợp đồng API và kế hoạch thực hiện vẫn ở file riêng, được liên kết từ spec.

## Các lựa chọn đã chốt

Người dùng chọn đủ 5 nhóm trong một đợt; ưu tiên tham khảo Yomitan và tự viết cho kiến trúc dự án, chỉ lấy code khi cần và ghi giấy phép. Từ điển/logic vẫn ở backend và PostgreSQL, web là nơi học. Phát âm dùng AI Gemini theo yêu cầu bổ sung của người dùng.

## Đọc theo thứ tự

1. [Đặc tả tổng và phạm vi](../.sdd/specs/005-yomitan-reading-experience/SPEC.md).
2. [Nội dung popup](../.sdd/specs/005-yomitan-reading-experience/features/popup-content.md).
3. [Quét bằng chuột](../.sdd/specs/005-yomitan-reading-experience/features/pointer-scanning.md).
4. [Đối chiếu và bổ sung thể chia](../.sdd/specs/005-yomitan-reading-experience/features/conjugation-coverage.md).
5. [Phát âm bằng Gemini AI](../.sdd/specs/005-yomitan-reading-experience/features/ai-pronunciation.md).
6. [Resize và tương tác popup](../.sdd/specs/005-yomitan-reading-experience/features/popup-behavior.md).
7. [Hợp đồng API và dữ liệu](../.sdd/specs/005-yomitan-reading-experience/contracts.md), [kế hoạch](../.sdd/specs/005-yomitan-reading-experience/PLAN.md), [công việc](../.sdd/specs/005-yomitan-reading-experience/TASKS.md).
8. [Kết quả rà soát tài liệu và giới hạn kiểm chứng](../.sdd/specs/005-yomitan-reading-experience/documentation-review.md).

Để bắt đầu code, dùng [plan.md triển khai](../plans/261104-yomitan-reading-experience/plan.md) và [task.md chi tiết](../plans/261104-yomitan-reading-experience/task.md). Các task giữ ID T01–T28 từ spec và chỉ đánh dấu hoàn thành sau khi đã triển khai, kiểm chứng.

## Phần nào học từ Yomitan, phần nào viết cho dự án

| Nhóm | Học từ Yomitan | Thực hiện phù hợp dự án |
|---|---|---|
| Nội dung popup | Bố trí headword/reading/definitions, nhãn và chi tiết | Component React dùng DTO/canonical IDs hiện có |
| Quét chuột | Modifier, dwell, điểm chữ, giới hạn context | Luồng opt-in riêng, backend scan; không đưa từ điển/NLP sang client |
| Thể chia | Danh mục transforms và ca khó | Gap catalog và corpus có nguồn/đáp án review độc lập; versioned backend rules |
| Phát âm | Term+reading identity, điều khiển play/stop và vòng đời | Adapter Gemini TTS riêng; không sao chép nguồn audio ngoài |
| Hành vi popup | Resize, viewport, duy trì tương tác khi trỏ vào popup | Mở rộng Shadow DOM/lifecycle hiện có; không lấy cả iframe engine |

Không thể chỉ dán UI Yomitan vì nó dùng templates và data structures khác. Model TTS của Gemini là tích hợp mới do dự án tự thiết kế; Yomitan chỉ cung cấp kinh nghiệm tổ chức trải nghiệm phát âm.

## Quy mô và thứ tự

Một milestone gồm các lớp nhỏ có thể review: hợp đồng → nội dung popup → lifecycle/resize → hover → corpus và âm thanh → kiểm thử chéo. Corpus có thể nghiên cứu cùng lúc với UI. Không gọi bộ tính năng hoàn thành nếu chỉ có popup đẹp hoặc chỉ TTS đã phát được.

Các mặc định kỹ thuật như Shift, kích thước resize, model TTS và deadline đều là đề xuất trong SPEC để người dùng review. Tài liệu không tự thay đổi các quyết định baseline về quyền riêng tư hoặc đánh dấu đã thông qua.

## Nguồn

- [Khảo sát source](yomitan-reuse-research.md); checkout local `E:/Road To N3/Yomitan`, revision `d3c54c10264673eca0d66b970ef74cd5c8b048e3`.
- [Google: tạo giọng đọc](https://ai.google.dev/gemini-api/docs/speech-generation) và [giá API](https://ai.google.dev/gemini-api/docs/pricing), đối chiếu 2026-11-04. Free tier TTS phụ thuộc model/quota/tài khoản và có thể thay đổi; chưa thực hiện live TTS trên key của người dùng.
