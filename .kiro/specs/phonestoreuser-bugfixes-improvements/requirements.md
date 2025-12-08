# Requirements Document

## Introduction

Tài liệu này mô tả các yêu cầu sửa lỗi và cải tiến cho ứng dụng PhoneStoreUser. Các vấn đề bao gồm: lỗi đăng ký (hash password), lỗi đăng nhập (cookie authentication), UI hóa đơn không scroll, thêm thống kê xuất hóa đơn, sửa lỗi tìm kiếm/sửa khách hàng, loại bỏ action gửi mail, sửa lỗi thêm thuộc tính sản phẩm, validate serial khi tạo hóa đơn, và giới hạn tồn kho khi thêm vào giỏ hàng.

## Glossary

- **PhoneStoreUser**: Ứng dụng web Blazor Server cho khách hàng và admin quản lý cửa hàng điện thoại
- **Cookie Authentication**: Cơ chế xác thực người dùng sử dụng cookie để lưu trữ session
- **Hash Password**: Mã hóa mật khẩu một chiều sử dụng thuật toán SHA256
- **Serial Number**: Mã số định danh duy nhất cho từng sản phẩm vật lý
- **Cart Service**: Dịch vụ quản lý giỏ hàng của khách hàng
- **Invoice**: Hóa đơn bán hàng
- **Product Attribute**: Thuộc tính kỹ thuật của sản phẩm (màu sắc, dung lượng, RAM...)
- **Inventory**: Tồn kho sản phẩm

## Requirements

### Requirement 1: Sửa lỗi đăng ký tài khoản

**User Story:** As a khách hàng, I want to đăng ký tài khoản mới với mật khẩu được mã hóa an toàn, so that I can tạo tài khoản và đăng nhập vào hệ thống.

#### Acceptance Criteria

1. WHEN a user submits the registration form with valid data THEN the System SHALL hash the password using SHA256 and store the account in the database
2. WHEN a user submits the registration form THEN the System SHALL validate all required fields (username, password, email, phone, fullname) before processing
3. WHEN a user registers successfully THEN the System SHALL redirect the user to the login page with a success message
4. IF a user submits a registration form with an existing username or email THEN the System SHALL display an appropriate error message and prevent duplicate account creation

### Requirement 2: Sửa lỗi đăng nhập và Cookie Authentication

**User Story:** As a user, I want to đăng nhập với một cookie duy nhất thay vì hai cookie, so that I can có trải nghiệm đăng nhập đơn giản và nhất quán.

#### Acceptance Criteria

1. WHEN a user logs in successfully THEN the System SHALL create a single authentication cookie containing all necessary claims (user ID, username, role, permissions)
2. WHEN a user accesses a protected page THEN the System SHALL verify the authentication cookie and check user permissions from claims
3. WHEN a user logs out THEN the System SHALL delete the authentication cookie and redirect to the login page
4. WHEN a user with admin role logs in THEN the System SHALL include admin permissions in the cookie claims for authorization checks

### Requirement 3: Sửa UI tạo hóa đơn - Thêm scroll

**User Story:** As an admin, I want to scroll trong modal tạo hóa đơn, so that I can xem và thao tác với tất cả các trường dữ liệu khi danh sách sản phẩm dài.

#### Acceptance Criteria

1. WHEN the create invoice modal contains more content than the viewport height THEN the System SHALL enable vertical scrolling within the modal body
2. WHEN scrolling the invoice modal THEN the System SHALL keep the modal header and action buttons fixed in position
3. WHEN the product list in the invoice exceeds 5 items THEN the System SHALL display a scrollable container for the product list

### Requirement 4: Thêm thống kê xuất hóa đơn

**User Story:** As an admin, I want to xem thống kê xuất hóa đơn, so that I can theo dõi hiệu suất bán hàng và doanh thu.

#### Acceptance Criteria

1. WHEN an admin views the orders page THEN the System SHALL display summary statistics including total invoices count, total revenue, and average order value
2. WHEN an admin filters orders by date range THEN the System SHALL update the statistics to reflect the filtered data
3. WHEN an admin views the dashboard THEN the System SHALL display invoice statistics for the current day, week, and month

### Requirement 5: Sửa lỗi tìm kiếm và sửa khách hàng

**User Story:** As an admin, I want to tìm kiếm và sửa thông tin khách hàng, so that I can quản lý hồ sơ khách hàng hiệu quả.

#### Acceptance Criteria

1. WHEN an admin enters a search term in the customer search box THEN the System SHALL filter customers by name, phone number, or email matching the search term
2. WHEN an admin clicks the edit button on a customer row THEN the System SHALL open an edit form populated with the customer's current data
3. WHEN an admin submits the customer edit form with valid data THEN the System SHALL update the customer record and refresh the customer list
4. WHEN an admin searches with an empty search term THEN the System SHALL display all customers without filtering

### Requirement 6: Loại bỏ action gửi mail khách hàng

**User Story:** As an admin, I want to không thấy nút gửi email trong trang khách hàng, so that I can có giao diện gọn gàng hơn và tránh nhầm lẫn với tính năng chưa hoàn thiện.

#### Acceptance Criteria

1. WHEN an admin views the customer list THEN the System SHALL not display the send email action button
2. WHEN an admin creates a new customer THEN the System SHALL not display the "Send welcome email" checkbox option

### Requirement 7: Sửa lỗi thêm thuộc tính sản phẩm

**User Story:** As an admin, I want to thêm thuộc tính cho sản phẩm mới, so that I can định nghĩa đầy đủ thông số kỹ thuật khi tạo sản phẩm.

#### Acceptance Criteria

1. WHEN an admin creates a new product and selects a model THEN the System SHALL load and display all attributes associated with that model
2. WHEN an admin fills in attribute values for a new product THEN the System SHALL save the attribute values along with the product record
3. WHEN an admin saves a new product with attributes THEN the System SHALL persist all attribute values to the database correctly
4. IF an admin changes the product model during creation THEN the System SHALL reload the attributes for the new model and clear previous attribute values

### Requirement 8: Validate Serial khi xử lý hóa đơn

**User Story:** As an admin, I want to validate serial number khi xử lý hóa đơn ở trang xử lý hóa đơn, so that I can đảm bảo serial hợp lệ trước khi hoàn tất đơn hàng.

#### Acceptance Criteria

1. WHEN an admin enters a serial number in the order processing page THEN the System SHALL validate that the serial exists in the database
2. WHEN an admin enters a serial number THEN the System SHALL validate that the serial belongs to the correct product in the order line
3. WHEN an admin enters a serial number THEN the System SHALL validate that the serial status is 'in_stock' (available for sale)
4. IF a serial validation fails THEN the System SHALL display a specific error message indicating the validation failure reason (not found, wrong product, or not in stock)

### Requirement 9: Giới hạn tồn kho khi thêm vào giỏ hàng

**User Story:** As a customer, I want to không thể thêm sản phẩm vượt quá số lượng tồn kho, so that I can tránh đặt hàng sản phẩm không có sẵn.

#### Acceptance Criteria

1. WHEN a customer adds a product to cart THEN the System SHALL check the available inventory quantity before adding
2. IF a customer attempts to add more quantity than available inventory THEN the System SHALL display a warning message and limit the quantity to available stock
3. WHEN a customer increases quantity in cart THEN the System SHALL validate against current inventory and prevent exceeding available stock
4. WHEN inventory changes while product is in cart THEN the System SHALL validate inventory at checkout and notify customer of any unavailable items
