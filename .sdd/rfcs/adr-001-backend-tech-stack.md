# ADR-001: Backend & Frontend Tech Stack Selection for JP Reading Platform V2

## 1. Status
**APPROVED** (Quyết định đã thống nhất)

## 2. Context (Bối cảnh)
Hệ thống **JP Reading Platform V2** đang chuyển đổi từ phiên bản Legacy V1 (prototype chạy hoàn toàn dưới client) sang kiến trúc **Server-Authoritative** (tập trung hóa logic tại Backend). 
Để triển khai thành công, chúng ta cần lựa chọn một bộ Tech Stack (Backend & Frontend) đáp ứng đầy đủ các tiêu chuẩn nghiêm ngặt sau:
1.  **Hiệu năng vượt trội:** Phải đạt chỉ số độ trễ P95 < 200ms cho các tác vụ tra cứu từ điển và phân tích ngữ pháp liên tục từ Extension Popup [REQ PERF-001].
2.  **Kỷ luật Kiến trúc:** Phải hỗ trợ hoàn hảo mô hình **Clean Architecture 4 tầng** (`Domain`, `Usecase`, `Interface`, `Infra`) [MIGRATION 13.4]. Ranh giới giữa các tầng phải cực kỳ rõ ràng, bảo vệ tầng Domain lõi khỏi sự rò rỉ hoặc phụ thuộc vào các thư viện ngoài [MIGRATION 13.5].
3.  **Tương thích tối đa với AI Agent (ADD-friendly):** Hệ thống kiểu dữ liệu phải chặt chẽ (Strong Type-safety) để giảm thiểu tối đa hiện tượng ảo tưởng (hallucination) của AI khi viết code tự động.
4.  **Khả năng tái sử dụng UI:** Đảm bảo mã nguồn giao diện có thể dùng chung 100% giữa Trình duyệt Extension (đa nền tảng) và Ứng dụng Web để triệt tiêu trùng lặp code [REQ WEB-002, BROWSER-001].

## 3. Decision (Quyết định)
Chúng ta quyết định lựa chọn bộ phối hợp công nghệ sau cho phiên bản V2:

### 3.1 Backend: .NET 10 (C# 14)
*   **.NET 10** được chọn làm công nghệ phát triển dịch vụ Backend chính.
*   **Kiến trúc:** Triển khai theo mô hình Clean Architecture (Hexagonal Architecture / Ports and Adapters).
*   **Database & ORM:** Lựa chọn chính thức sẽ được quyết định ở ADR-002 (dự kiến sử dụng PostgreSQL 16 và Entity Framework Core 10).

### 3.2 Frontend: React 19 + TypeScript + Vite
*   **React** được chọn làm thư viện UI chính cho cả Ứng dụng Web và Extension Popup.
*   **Bundler (Vite):** Sử dụng Vite để build ra các gói Single Page Application (SPA) siêu nhẹ, tối ưu hóa thời gian tải và khởi động popup extension [REQ PERF-002].
*   **TypeScript:** Bắt buộc sử dụng hệ thống kiểu chặt chẽ cho toàn bộ mã nguồn Frontend.

### 3.3 Giải pháp Tích hợp Tokenizer (NLP) Tiếng Nhật
Vì hệ sinh thái .NET không có các thư viện phân tích từ loại (Morphology/Tokenizer) tiếng Nhật native xuất sắc như Python hay Java, chúng ta quyết định áp dụng triết lý **Provider Independence (ADR-003 / Mục 17)**:
*   Xây dựng một **Tokenizer Adapter** ở tầng Infra của .NET Backend [MIGRATION 6.5, 13.4].
*   Adapter này sẽ giao tiếp qua gRPC hoặc REST API siêu nhẹ tới một **Sidecar Tokenizer Service** viết bằng Go hoặc Python sử dụng engine Sudachi hoặc MeCab [MIGRATION 8.2].
*   Toàn bộ Domain logic của .NET Backend chỉ làm việc với cấu trúc dữ liệu Token chuẩn hóa trung gian (Normalized Token Representation) và hoàn toàn không biết đến sự tồn tại của Sidecar Tokenizer bên dưới [MIGRATION 6.5, 17].

## 4. Consequences (Hệ quả & Đánh giá)

### 4.1 Điểm mạnh (Pros)
*   **Type-safety Tuyệt đối:** Sự kết hợp giữa C# trên Backend và TypeScript trên Frontend tạo ra một rào chắn lỗi cực kỳ vững chắc. AI Agent khi thực thi (ADD) sẽ bị ràng buộc bởi hệ thống Type chặt chẽ, giảm 90% lỗi runtime vặt.
*   **Tốc độ Thực thi Tối đa:** .NET 10 cung cấp hiệu năng tiệm cận ngôn ngữ biên dịch hệ thống nhờ cơ chế tối ưu hóa JIT/AOT vượt trội, đáp ứng hoàn hảo yêu cầu độ trễ cực thấp (<200ms) [REQ PERF-001].
*   **Tái sử dụng Code Giao diện:** Giao diện popup Extension và Web App sử dụng chung component React, giúp giảm một nửa thời gian bảo trì và kiểm thử [REQ WEB-002].
*   **Cô lập rủi ro NLP:** Cơ chế Sidecar Tokenizer giúp chúng ta dễ dàng nâng cấp, thay thế hoặc hoán đổi engine phân tích ngôn ngữ học (ví dụ: chuyển từ MeCab sang Sudachi) mà không cần chạm vào một dòng code nghiệp vụ nào của Backend .NET [MIGRATION 17].

### 4.2 Điểm yếu & Biện pháp khắc phục (Cons & Mitigations)
*   **Hạn chế chia sẻ Code gốc (No Shared Types):** Không giống như Node.js (dùng chung TypeScript cho cả FE và BE), .NET và React sử dụng hai ngôn ngữ khác nhau (C# và TS).
    *   *Khắc phục:* Chúng ta sẽ áp dụng quy trình **API-First**. Định nghĩa toàn bộ API Contract bằng OpenAPI/Swagger [MIGRATION 18 (Step 10)]. Sau đó, sử dụng các công cụ tự động phát sinh mã nguồn (e.g., NSwag hoặc openapi-generator) để sinh ra các TypeScript DTOs và API Clients cho Frontend một cách tự động mỗi khi Backend thay đổi.
*   **Tốn tài nguyên khởi chạy Sidecar:** Việc duy trì thêm một dịch vụ phụ (Sidecar Tokenizer) làm tăng độ phức tạp khi deploy và tốn thêm RAM/CPU trên server.
    *   *Khắc phục:* Sidecar Tokenizer sẽ được viết tối giản bằng Go (sử dụng kagome) hoặc Python đóng gói gọn trong Docker Container cục bộ để tối ưu hóa tài nguyên phần cứng.

## 5. EARS Constraints for Implementation (Ràng buộc Thực thi)
Để đảm bảo AI Agent tuân thủ đúng quyết định này khi viết code, chúng ta ban hành các quy tắc EARS sau:

1.  **THE** .NET backend system **SHALL** strictly enforce Clean Architecture boundaries, preventing any dependency from `Domain` or `Usecase` layer to external framework libraries or ORMs [MIGRATION 13.5].
2.  **THE** React frontend **SHALL** bundle all popup extension logic into a self-contained, single-page application (SPA) capable of running entirely offline for local preference caching [REQ BROWSER-001].
3.  **WHEN** calling external translation or tokenization services, **THE** backend **SHALL** delegate requests through defined Interface Adapters to guarantee engine independence [MIGRATION 17].
