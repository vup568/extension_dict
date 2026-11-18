# Đối chiếu ứng viên Nhật–Việt với JMdict

Đây là báo cáo kiểm tra nguồn, không phải dữ liệu đã được duyệt để hiển thị. Các tệp nguồn được xác nhận bằng SHA-256 trước khi xử lý. Cách đối chiếu chỉ so sánh chính xác headword của dòng Nhật–Việt với chữ viết hoặc cách đọc trong JMdict; không chuẩn hóa, suy đoán nghĩa, gán sense, hoặc tự phê duyệt.

| Kết quả | Số dòng nguồn Nhật–Việt |
| --- | ---: |
| Không có mục JMdict nào trùng | 6.813 |
| Có đúng một mục JMdict trùng | 151.305 |
| Có nhiều mục JMdict trùng | 14.203 |
| Tổng | 172.321 |

Một mục JMdict có thể có nhiều nghĩa, và một dòng nguồn có thể gộp nhiều từ hay nghĩa. Vì vậy, ngay cả 151.305 dòng có một ứng viên cũng không được coi là đã ghép nghĩa đúng. Mỗi kết quả đều `quarantined`; số mapping được duyệt là 0. Chưa kiểm tra tương đương ngữ nghĩa, từ loại hoặc giấy phép qua phép đối chiếu này.

- JMdict: `JMdict_e.gz`, 218.914 mục, SHA-256 `1e89ac398ec0230b7bb1daa246ce0095421bf3667bb6509aab93e26e7fdfd1b5`.
- Nguồn Nhật–Việt: `star_nhatviet.tab`, 172.321 dòng, SHA-256 `0ab16ce4121f5228a529203506bb771392b33dcd64eef1b109f91facad433f0e`. Bằng chứng hiện có gợi ý nguồn gốc liên quan OVDP nhưng chưa xác nhận được quyền sử dụng của chính bộ dữ liệu này.
- Kết quả chi tiết `data/dictionary-import/audit-20261015/vietnamese-candidates.jsonl`, SHA-256 `48f6e23d61524542fe081b0356a98f63b6a67f1220ee99ee072b747fe39627a6`; kết quả thống kê cùng thư mục có đuôi `.summary.json`. Mỗi dòng giữ định danh theo hash nguồn và số thứ tự; nội dung gốc nằm trong nguồn và inventory riêng.
- Công cụ: `scripts/audit-vietnamese-candidates.py`; kiểm thử: `tests/unit/test_vietnamese_candidate_audit.py`.

Nguồn Japanese/English JMdict và KANJIDIC2 do EDRDG cung cấp; thông tin giấy phép và yêu cầu ghi nguồn xem [EDRDG licence](https://www.edrdg.org/edrdg/licence.html). Thông tin về nguồn tiếng Việt và mâu thuẫn restriction của JMdict được ghi chi tiết trong [source audit](source-audit.md). Kết quả này không giải quyết mâu thuẫn hoặc thay thế quyết định về nguồn, sense và giấy phép.
