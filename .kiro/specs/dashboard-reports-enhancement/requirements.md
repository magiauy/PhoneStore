# Requirements Document

## Introduction

Tài liệu này mô tả các yêu cầu cho việc nâng cấp Dashboard và thêm trang Báo cáo mới cho PhoneStoreUser Admin. Các tính năng bao gồm: hiển thị doanh số theo tháng với bộ lọc, top 10 sản phẩm bán chạy, top 10 khách hàng mua nhiều, trang báo cáo mới với 2 tab (báo cáo nhập và báo cáo hóa đơn), biểu đồ chi tiết, xuất Excel, và nút logout cho admin.

## Glossary

- **Dashboard**: Trang tổng quan hiển thị các thống kê kinh doanh chính
- **PhoneStoreUser Admin**: Hệ thống quản trị của ứng dụng PhoneStoreUser
- **Báo cáo nhập**: Báo cáo thống kê các đơn nhập hàng từ nhà cung cấp (Purchase Orders)
- **Báo cáo hóa đơn**: Báo cáo thống kê các hóa đơn bán hàng (Invoices)
- **Top sản phẩm**: Danh sách sản phẩm được sắp xếp theo số lượng bán ra
- **Top khách hàng**: Danh sách khách hàng được sắp xếp theo tổng giá trị mua hàng
- **Excel Export**: Chức năng xuất dữ liệu báo cáo ra file định dạng Excel (.xlsx)

## Requirements

### Requirement 1

**User Story:** As an admin, I want to view monthly sales statistics on the dashboard, so that I can monitor business performance over time.

#### Acceptance Criteria

1. WHEN the admin navigates to the Dashboard page THEN the Dashboard SHALL display total revenue for the currently selected month
2. WHEN the admin selects a different month from the month picker THEN the Dashboard SHALL update all statistics to reflect data for the selected month
3. WHEN displaying monthly statistics THEN the Dashboard SHALL show total revenue, total orders count, and total new customers for the selected month
4. WHEN the Dashboard loads THEN the Dashboard SHALL default to displaying the current month's data

### Requirement 2

**User Story:** As an admin, I want to see top 10 best-selling products, so that I can understand which products are most popular.

#### Acceptance Criteria

1. WHEN the Dashboard displays product statistics THEN the Dashboard SHALL show a list of top 10 products sorted by quantity sold in descending order
2. WHEN displaying top products THEN the Dashboard SHALL show product name, quantity sold, and total revenue for each product
3. WHEN the admin changes the selected month THEN the Dashboard SHALL update the top products list to reflect sales for the selected month

### Requirement 3

**User Story:** As an admin, I want to see top 10 customers by purchase value, so that I can identify valuable customers.

#### Acceptance Criteria

1. WHEN the Dashboard displays customer statistics THEN the Dashboard SHALL show a list of top 10 customers sorted by total purchase value in descending order
2. WHEN displaying top customers THEN the Dashboard SHALL show customer name, number of orders, and total purchase value for each customer
3. WHEN the admin changes the selected month THEN the Dashboard SHALL update the top customers list to reflect purchases for the selected month

### Requirement 4

**User Story:** As an admin, I want to access a dedicated Reports page from the navigation menu, so that I can view detailed business reports.

#### Acceptance Criteria

1. WHEN the admin views the navigation sidebar THEN the AdminLayout SHALL display a "Báo cáo" menu item with an appropriate icon
2. WHEN the admin clicks on the "Báo cáo" menu item THEN the system SHALL navigate to the Reports page at route "/admin/reports"
3. WHEN the Reports page loads THEN the Reports page SHALL display two tabs: "Báo cáo nhập" and "Báo cáo hóa đơn"

### Requirement 5

**User Story:** As an admin, I want to view purchase order reports with charts, so that I can analyze import activities.

#### Acceptance Criteria

1. WHEN the admin selects the "Báo cáo nhập" tab THEN the Reports page SHALL display a summary of purchase orders including total count and total value
2. WHEN displaying purchase order reports THEN the Reports page SHALL show a bar chart or line chart visualizing purchase order data over time
3. WHEN the admin selects a date range THEN the Reports page SHALL filter purchase order data to the selected range
4. WHEN displaying purchase order details THEN the Reports page SHALL show a table with order date, supplier name, total items, and total value

### Requirement 6

**User Story:** As an admin, I want to view invoice reports with charts, so that I can analyze sales activities.

#### Acceptance Criteria

1. WHEN the admin selects the "Báo cáo hóa đơn" tab THEN the Reports page SHALL display a summary of invoices including total count and total revenue
2. WHEN displaying invoice reports THEN the Reports page SHALL show a bar chart or line chart visualizing invoice data over time
3. WHEN the admin selects a date range THEN the Reports page SHALL filter invoice data to the selected range
4. WHEN displaying invoice details THEN the Reports page SHALL show a table with invoice date, customer name, total items, and total value

### Requirement 7

**User Story:** As an admin, I want to export report data to Excel, so that I can perform further analysis or share reports.

#### Acceptance Criteria

1. WHEN the admin clicks the "Xuất Excel" button on the purchase order report tab THEN the system SHALL generate and download an Excel file containing purchase order data
2. WHEN the admin clicks the "Xuất Excel" button on the invoice report tab THEN the system SHALL generate and download an Excel file containing invoice data
3. WHEN generating Excel files THEN the system SHALL include all visible columns from the report table
4. WHEN generating Excel files THEN the system SHALL name the file with the report type and date range (e.g., "BaoCaoNhap_2024-01.xlsx")

### Requirement 8

**User Story:** As an admin, I want to logout from the admin panel, so that I can securely end my session.

#### Acceptance Criteria

1. WHEN the admin views the admin header or sidebar THEN the AdminLayout SHALL display a logout button with an appropriate icon
2. WHEN the admin clicks the logout button THEN the system SHALL clear the admin session data
3. WHEN the logout process completes THEN the system SHALL redirect the admin to the login page
