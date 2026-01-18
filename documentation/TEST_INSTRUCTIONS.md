# Hướng dẫn Kiểm thử Tự động (E2E Testing)

Tài liệu này hướng dẫn cách chạy bộ kiểm thử tự động End-to-End (E2E) cho hệ thống Frontend, sử dụng **Playwright** và tuân thủ mô hình Page Object Model (POM).

## 1. Yêu cầu Tiên quyết

Trước khi chạy kiểm thử, đảm bảo các dịch vụ sau đang hoạt động:

1.  **Backend API**: 
    *   Mở terminal tại thư mục `backend`.
    *   Chạy lệnh: `dotnet run --project Parking.API`.
    *   Đảm bảo API hoạt động tại `http://localhost:5000` (hoặc cổng cấu hình).

2.  **Frontend**:
    *   Playwright được cấu hình để tự động khởi động Frontend (`npm run dev`) tại `http://localhost:5173`.
    *   Tuy nhiên, bạn cần cài đặt dependencies trước:
        ```bash
        cd frontend
        npm install
        npx playwright install chromium
        ```

## 2. Cấu trúc Dự án Test

Bộ test được tổ chức theo kiến trúc Clean OOP & POM:

*   `tests/e2e/pages/`: **Page Objects**. Chứa các class đại diện cho từng trang màn hình (Login, Dashboard, CheckIn...). Mọi tương tác UI (selector, click, fill) được đóng gói tại đây (Encapsulation).
*   `tests/e2e/specs/`: **Test Cases**. Chứa kịch bản kiểm thử (Authentication, Full Parking Flow, Membership, Reports). Các file này chỉ gọi các phương thức nghiệp vụ từ Page Objects (Abstraction).
*   `playwright.config.ts`: Cấu hình runner, tự động chụp màn hình khi lỗi hoặc khi được yêu cầu.

## 3. Chạy Kiểm thử

Mở terminal tại thư mục `frontend` và chạy các lệnh sau:

### Chạy toàn bộ test
```bash
npx playwright test
```

### Chạy test cụ thể (ví dụ checkin)
```bash
npx playwright test checkin
```

### Chạy ở chế độ có giao diện (Headed mode)
Để quan sát trình duyệt thực thi:
```bash
npx playwright test --headed
```

## 4. Kết quả & Báo cáo

### Screenshots
Sau khi chạy test, các ảnh chụp màn hình (kết quả thành công hoặc lỗi) sẽ được tự động lưu vào thư mục:
`documentation/screenshots/`

### Báo cáo chi tiết (.md)
Để tạo file báo cáo tổng hợp dạng Markdown như yêu cầu:

1.  Đảm bảo file `test-results.json` đã được tạo ra (tự động khi chạy lệnh test ở trên).
2.  Chạy script tạo báo cáo:
    ```bash
    node generate_test_report.js
    ```
3.  Kết quả sẽ được lưu tại: `documentation/e2e_test_report.md`.

File báo cáo này sẽ liệt kê:
*   Tổng số test case.
*   Trạng thái (Pass/Fail).
*   Thời gian thực thi.
*   Liên kết đến các ảnh chụp màn hình minh chứng.

---
**Lưu ý**: Nếu gặp lỗi `Connection refused`, hãy kiểm tra kỹ Backend đã khởi động chưa.
