# Báo cáo tổ chức mã nguồn – Parking Management System

## 1. Tổng quan
Dự án được tổ chức theo kiến trúc đa lớp (backend) và kiến trúc component-based (frontend), kèm dịch vụ nhận diện biển số và tài liệu hỗ trợ. Cấu trúc tổng thể phân tách rõ ràng theo miền chức năng:
- Backend ASP.NET Core Web API với các tầng Core, Infrastructure, Services.
- Frontend React/Vite chia theo layout, pages, cấu hình API.
- Công cụ OCR phục vụ nhận diện biển số và tài liệu/test assets.

## 2. Bố cục thư mục cấp cao
- Backend: [backend](backend)
- Frontend: [frontend](frontend)
- Tài liệu dự án: [documentation](documentation)
- Dịch vụ OCR/ALPR: [licens/LicensePlateRecognitionVNAPI](licens/LicensePlateRecognitionVNAPI)
- Script vận hành: [StartAll.ps1](StartAll.ps1), [StartAll.bat](StartAll.bat), [SeedData.ps1](SeedData.ps1)

## 3. Backend (ASP.NET Core)
### 3.1. Phân lớp kiến trúc
- API Host (presentation): [backend/Parking.API](backend/Parking.API)
- Domain/Contracts: [backend/Parking.Core](backend/Parking.Core)
- Data + External Adapters: [backend/Parking.Infrastructure](backend/Parking.Infrastructure)
- Business Services: [backend/Parking.Services](backend/Parking.Services)
- Unit Tests: [backend/Parking.Tests](backend/Parking.Tests)

Luồng phụ thuộc chính: API → Services → Infrastructure/Core. Các interface được định nghĩa trong Core, triển khai trong Infrastructure và Services, sau đó được cấu hình DI tại [backend/Parking.API/Program.cs](backend/Parking.API/Program.cs).

### 3.2. API Layer (Presentation)
Thành phần chính:
- Các controller REST cho check-in/out, membership, report, pricing, user, zone tại [backend/Parking.API/Controllers](backend/Parking.API/Controllers).
- Background service scheduler: [backend/Parking.API/BackgroundServices](backend/Parking.API/BackgroundServices).
- Cấu hình ứng dụng tại [backend/Parking.API/appsettings.json](backend/Parking.API/appsettings.json) và [backend/Parking.API/appsettings.Development.json](backend/Parking.API/appsettings.Development.json).
- Mẫu in vé và template tại [backend/Parking.API/Templates](backend/Parking.API/Templates).

### 3.3. Domain & Contracts (Core)
- Entity/DTO/value objects: [backend/Parking.Core/Entities](backend/Parking.Core/Entities), [backend/Parking.Core/DTOs](backend/Parking.Core/DTOs), [backend/Parking.Core/ValueObjects](backend/Parking.Core/ValueObjects).
- Interfaces hợp đồng cho repositories/services: [backend/Parking.Core/Interfaces](backend/Parking.Core/Interfaces).
- Cấu hình strongly-typed: [backend/Parking.Core/Configuration](backend/Parking.Core/Configuration).

### 3.4. Infrastructure
- Repositories JSON: [backend/Parking.Infrastructure/Repositories](backend/Parking.Infrastructure/Repositories).
- Lưu trữ JSON và trợ giúp serialize: [backend/Parking.Infrastructure/Data](backend/Parking.Infrastructure/Data).
- Adapter ngoại vi (cổng, thanh toán, OCR): [backend/Parking.Infrastructure/External](backend/Parking.Infrastructure/External).
- Templates/Services hỗ trợ: [backend/Parking.Infrastructure/Templates](backend/Parking.Infrastructure/Templates).

### 3.5. Services (Business Logic)
- Các service điều phối nghiệp vụ: [backend/Parking.Services/Services](backend/Parking.Services/Services).
- Validator/Policies/Strategies: [backend/Parking.Services/Validators](backend/Parking.Services/Validators), [backend/Parking.Services/Policies](backend/Parking.Services/Policies), [backend/Parking.Services/Strategies](backend/Parking.Services/Strategies).
- Factories: [backend/Parking.Services/Factories](backend/Parking.Services/Factories).

### 3.6. Dữ liệu và trạng thái
Hệ thống dùng JSON datastore đặt tại [backend/Parking.API/DataStore](backend/Parking.API/DataStore) để lưu phiên gửi xe, vé, người dùng, khu vực, membership, incident, audit. Cấu trúc này hỗ trợ seed dữ liệu qua [SeedData.ps1](SeedData.ps1).

### 3.7. Bảo mật và tích hợp
- JWT Authentication cấu hình tại [backend/Parking.API/Program.cs](backend/Parking.API/Program.cs).
- Mã hóa mật khẩu thông qua BCrypt trong tầng services.
- OCR/ALPR: client cấu hình qua [backend/Parking.Core/Configuration](backend/Parking.Core/Configuration) và adapter tại [backend/Parking.Infrastructure/External](backend/Parking.Infrastructure/External).
- Payment/Gate adapters nằm trong Infrastructure (mock).

## 4. Frontend (React + Vite)
### 4.1. Cấu trúc chính
- Entry + router: [frontend/src/main.jsx](frontend/src/main.jsx), [frontend/src/App.jsx](frontend/src/App.jsx).
- Layout chung: [frontend/src/layouts/MainLayout.jsx](frontend/src/layouts/MainLayout.jsx).
- Trang chức năng: [frontend/src/pages](frontend/src/pages).
- Cấu hình API: [frontend/src/config](frontend/src/config).
- Styling: [frontend/src/App.css](frontend/src/App.css), [frontend/src/index.css](frontend/src/index.css), [frontend/tailwind.config.js](frontend/tailwind.config.js).

### 4.2. Điều hướng và phân quyền
Hệ thống định tuyến theo vai trò người dùng (Admin vs Attendant) tại [frontend/src/App.jsx](frontend/src/App.jsx). `MainLayout` quản lý menu theo gate type và role.

### 4.3. Tích hợp API
Frontend dùng Axios và cấu hình base URL qua [frontend/src/config/api.js](frontend/src/config/api.js). Token JWT được gắn vào header khi đăng nhập.

### 4.4. Kiểm thử giao diện
Playwright e2e tests tại [frontend/tests](frontend/tests) và cấu hình tại [frontend/playwright.config.ts](frontend/playwright.config.ts). Báo cáo test nằm trong [frontend/playwright-report](frontend/playwright-report).

## 5. OCR/License Plate Recognition
Dịch vụ Python được đặt trong [licens/LicensePlateRecognitionVNAPI](licens/LicensePlateRecognitionVNAPI), bao gồm mô hình, script main và requirements. Backend gọi qua HTTP client theo cấu hình.

## 6. Tài liệu và báo cáo
- Tài liệu phân tích: [documentation](documentation).
- Hướng dẫn test: [documentation/TEST_INSTRUCTIONS.md](documentation/TEST_INSTRUCTIONS.md).

## 7. Nhận xét về tổ chức mã nguồn
- Phân tách tầng rõ ràng ở backend, giảm coupling và tăng khả năng mở rộng.
- Frontend tách layout/pages/config giúp dễ quản trị routing và UI.
- JSON datastore phù hợp demo và dễ seed dữ liệu nhưng cần cân nhắc DB thật nếu production.
- Hệ thống test đã có cả unit (backend) và e2e (frontend).

## 8. Đề xuất tổ chức bổ sung (không thay đổi code)
- Chuẩn hóa README frontend để phản ánh cấu trúc thực tế của dự án (thay template mặc định).
- Bổ sung sơ đồ kiến trúc hệ thống trong [documentation](documentation) để đồng bộ với codebase.
