# PhoneStoreUser - URLs & API Endpoints Documentation

Tài liệu này liệt kê toàn bộ các URL trang web và API endpoints của ứng dụng PhoneStoreUser.

---

## 📋 Tổng Quan

### API Endpoints

| Nhóm | Số Endpoints |
|------|-------------|
| Authentication (User) | 3 |
| Authentication (Admin) | 3 |
| PayOS Integration | 3 |
| **Tổng API Endpoints** | **9** |

### Blazor Pages (URLs)

| Nhóm | Số Trang |
|------|----------|
| Trang công khai (Public) | 12 |
| Trang yêu cầu đăng nhập (User) | 2 |
| Trang Admin | 14 |
| **Tổng số trang** | **28** |

---

# 🌐 PHẦN 1: BLAZOR PAGES (URLs)

## 🏠 Trang Công Khai (Public Pages)

| # | URL | Trang | Mô tả |
|---|-----|-------|-------|
| 1 | `/` | Home.razor | Trang chủ |
| 2 | `/products` | Products.razor | Danh sách sản phẩm |
| 3 | `/product/{slug}/{sku?}` | ProductDetail.razor | Chi tiết sản phẩm (với slug và SKU tùy chọn) |
| 4 | `/login` | Login.razor | Đăng nhập người dùng |
| 5 | `/register` | Register.razor | Đăng ký tài khoản |
| 6 | `/cart` | Cart.razor | Giỏ hàng |
| 7 | `/order-success` | OrderSuccess.razor | Thông báo đặt hàng thành công |
| 8 | `/order-error` | OrderError.razor | Thông báo lỗi đặt hàng |
| 9 | `/order-cancel` | OrderCancel.razor | Thông báo hủy đơn hàng |
| 10 | `/Error` | Error.razor | Trang lỗi hệ thống |
| 11 | `/counter` | Counter.razor | Demo counter (development) |
| 12 | `/weather` | Weather.razor | Demo weather (development) |
| 13 | `/tailwind-test` | TailwindTest.razor | Test Tailwind CSS (development) |

---

## 🔐 Trang Yêu Cầu Đăng Nhập (Authenticated User Pages)

| # | URL | Trang | Mô tả |
|---|-----|-------|-------|
| 1 | `/checkout` | Checkout.razor | Thanh toán đơn hàng |
| 2 | `/history` | History.razor | Lịch sử đơn hàng |

---

## 🛡️ Trang Admin

### Đăng nhập Admin

| # | URL | Trang | Mô tả |
|---|-----|-------|-------|
| 1 | `/admin` | AdminLogin.razor | Trang đăng nhập admin |

### Dashboard & Tổng quan

| # | URL | Trang | Mô tả |
|---|-----|-------|-------|
| 2 | `/admin/dashboard` | Dashboard.razor | Bảng điều khiển |

### Quản lý Sản phẩm

| # | URL | Trang | Mô tả |
|---|-----|-------|-------|
| 3 | `/admin/products` | Products.razor | Danh sách sản phẩm |
| 4 | `/admin/products/create` | ProductEditor.razor | Tạo sản phẩm mới |
| 5 | `/admin/products/edit/{Id:int}` | ProductEditor.razor | Chỉnh sửa sản phẩm |
| 6 | `/admin/inventory` | Inventory.razor | Quản lý kho hàng |

### Quản lý Đơn hàng

| # | URL | Trang | Mô tả |
|---|-----|-------|-------|
| 7 | `/admin/orders` | Orders.razor | Đơn hàng đang xử lý |
| 8 | `/admin/orders-history` | OrdersHistory.razor | Lịch sử đơn hàng |

### Quản lý Khách hàng & Đánh giá

| # | URL | Trang | Mô tả |
|---|-----|-------|-------|
| 9 | `/admin/customers` | Customers.razor | Quản lý khách hàng |
| 10 | `/admin/reviews` | Reviews.razor | Quản lý đánh giá |

### Quản lý Nhà cung cấp & Nhập hàng

| # | URL | Trang | Mô tả |
|---|-----|-------|-------|
| 11 | `/admin/suppliers` | Suppliers.razor | Quản lý nhà cung cấp |
| 12 | `/admin/purchase-orders` | PurchaseOrderList.razor | Danh sách đơn nhập hàng |
| 13 | `/admin/purchase-orders/create` | PurchaseOrderDetail.razor | Tạo đơn nhập hàng |
| 14 | `/admin/purchase-orders/edit/{Id:int}` | PurchaseOrderDetail.razor | Chỉnh sửa đơn nhập hàng |

### Quản lý Khuyến mãi

| # | URL | Trang | Mô tả |
|---|-----|-------|-------|
| 15 | `/admin/promotions` | Promotions.razor | Danh sách khuyến mãi |
| 16 | `/admin/promotions/create` | PromotionDetail.razor | Tạo khuyến mãi mới |
| 17 | `/admin/promotions/edit/{Id:int}` | PromotionDetail.razor | Chỉnh sửa khuyến mãi |

---

# 🔌 PHẦN 2: API ENDPOINTS


## 🔐 Authentication Endpoints (User)

### 1. POST `/login`

**Mô tả:** Đăng nhập cho người dùng (Customer)

**Yêu cầu xác thực:** Không (AllowAnonymous)

**Request Body (Form Data):**
```
EmailOrUsername: string (required)
Password: string (required)  
RememberMe: boolean (optional)
```

**Query Parameters:**
| Tham số | Kiểu | Mô tả |
|---------|------|-------|
| ReturnUrl | string | URL redirect sau khi đăng nhập thành công |

**Response:**
- **Thành công:** Redirect đến `ReturnUrl` hoặc `/`
- **Thất bại:** Redirect đến `/login?error=<error_type>`
  - `missing_credentials`: Thiếu username/password
  - `invalid_credentials`: Sai thông tin đăng nhập

**Cookie được tạo:** `guzone.auth` (HttpOnly, Lax SameSite, hết hạn sau 7 ngày)

---

### 2. POST `/logout`

**Mô tả:** Đăng xuất người dùng (API call)

**Yêu cầu xác thực:** Không (AllowAnonymous)

**Response:** `200 OK`

**Hành động:** Xóa cookie `guzone.auth`

---

### 3. GET `/logout`

**Mô tả:** Đăng xuất người dùng (redirect)

**Yêu cầu xác thực:** Không (AllowAnonymous)

**Response:** Redirect đến `/`

**Hành động:** Xóa cookie `guzone.auth`

---

## 🔒 Authentication Endpoints (Admin)

### 4. POST `/admin/login`

**Mô tả:** Đăng nhập cho admin/nhân viên

**Yêu cầu xác thực:** Không (AllowAnonymous)

**Yêu cầu quyền:** Tài khoản phải có `PersonType = "EMPLOYEE"`

**Request Body (Form Data):**
```
EmailOrUsername: string (required)
Password: string (required)
RememberMe: boolean (optional)
```

**Response:**
- **Thành công:** Redirect đến `/admin/dashboard`
- **Thất bại:** Redirect đến `/admin?error=<error_type>`
  - `missing_credentials`: Thiếu username/password
  - `invalid_credentials`: Sai thông tin đăng nhập
  - `unauthorized`: Tài khoản không phải nhân viên

**Cookie được tạo:** `guzone.admin` (HttpOnly, Lax SameSite, hết hạn sau 7 ngày)

---

### 5. POST `/admin/logout`

**Mô tả:** Đăng xuất admin (API call)

**Yêu cầu xác thực:** Không (AllowAnonymous)

**Response:** `200 OK`

**Hành động:** Xóa cookie `guzone.admin`

---

### 6. GET `/admin/logout`

**Mô tả:** Đăng xuất admin (redirect)

**Yêu cầu xác thực:** Không (AllowAnonymous)

**Response:** Redirect đến `/admin`

**Hành động:** Xóa cookie `guzone.admin`

---

## 💳 PayOS Integration Endpoints

**Base Route:** `/api/payos`

### 7. POST `/api/payos/callback`

**Mô tả:** Webhook callback từ PayOS khi trạng thái thanh toán thay đổi

**Yêu cầu xác thực:** Không (Gọi từ PayOS server)

**Request Body (JSON):**
```json
{
  "orderCode": "string | number",
  "status": "string",
  "data": {
    "orderCode": "string | number",
    "status": "string"
  }
}
```

**Các trạng thái thanh toán:**
| PayOS Status | Invoice Status |
|--------------|----------------|
| `paid`, `success` | `paid` |
| `cancelled`, `canceled` | `cancelled` |
| Khác | Giữ nguyên |

**Response:**
- **Thành công:**
  ```json
  { "success": true }
  ```
- **Thất bại (Invalid order code):**
  ```json
  400 Bad Request - "Invalid order code"
  ```
- **Invoice không tồn tại:**
  ```json
  { "success": false, "message": "Invoice not found" }
  ```
- **Lỗi server:**
  ```json
  500 Internal Server Error - "Internal server error"
  ```

---

### 8. GET `/api/payos/callback`

**Mô tả:** Health check endpoint để PayOS xác minh khả năng tiếp cận webhook URL

**Yêu cầu xác thực:** Không

**Response:**
```json
{ "success": true }
```

---

### 9. GET `/api/payos/return`

**Mô tả:** URL redirect khi người dùng hoàn thành/hủy thanh toán trên PayOS

**Yêu cầu xác thực:** Không

**Query Parameters:**
| Tham số | Kiểu | Mô tả |
|---------|------|-------|
| orderCode | string | Mã đơn hàng (Invoice ID) |
| status | string | Trạng thái thanh toán |
| cancel | boolean | `true` nếu người dùng hủy thanh toán |

**Response:** 
- **Thành công:** Redirect đến `/order-success?OrderId={invoiceId}&Cancel={cancel}&Status={status}`
- **Thất bại (Invalid orderCode):** Redirect đến `/order-error`

**Hành động phụ:** Cập nhật trạng thái invoice trong database (async)

---

## 🔧 Cấu hình

### Authentication Schemes

| Scheme | Cookie Name | Mục đích |
|--------|-------------|----------|
| `CookieAuthenticationDefaults.AuthenticationScheme` | `guzone.auth` | Xác thực người dùng |
| `guzone.admin` | `guzone.admin` | Xác thực admin |
| `guzone.policy` | - | Policy scheme để route xác thực |

### PayOS Configuration

Cấu hình trong `appsettings.json`:
```json
{
  "PayOS": {
    "ClientId": "...",
    "ApiKey": "...",
    "ChecksumKey": "...",
    "ApiUrl": "https://api-merchant.payos.vn",
    "ReturnUrl": "http://sgumc.meowsmp.net/api/payos/return",
    "CallbackUrl": "http://sgumc.meowsmp.net/api/payos/callback"
  }
}
```

---

## 📝 Ghi chú

1. **Anti-forgery Token:** Các endpoint login được tắt anti-forgery (`DisableAntiforgery()`)

2. **Admin Routes:** Tất cả routes `/admin/*` (trừ login/logout) yêu cầu policy `AdminOnly`:
   - Phải được xác thực
   - Phải sử dụng scheme `guzone.admin`
   - Phải có role `EMPLOYEE`

3. **Policy-based Routing:** Hệ thống sử dụng `guzone.policy` để tự động chọn authentication scheme dựa trên:
   - Path URL (routes bắt đầu bằng `/admin` → admin scheme)
   - Referer header (cho Blazor/SignalR connections)
   - Cookies (fallback)

4. **PayOS Callback Security:** Hiện tại chưa implement signature verification (TODO trong production)

---

## 📊 Sơ đồ URL Structure

```
/                                    ← Trang chủ
├── /products                        ← Danh sách sản phẩm  
│   └── /product/{slug}/{sku?}       ← Chi tiết sản phẩm
├── /cart                            ← Giỏ hàng
├── /checkout                        ← Thanh toán [🔒 Đăng nhập]
├── /history                         ← Lịch sử đơn hàng [🔒 Đăng nhập]
├── /order-success                   ← Kết quả đặt hàng thành công
├── /order-error                     ← Kết quả đặt hàng lỗi
├── /order-cancel                    ← Kết quả hủy đơn hàng
├── /login                           ← Đăng nhập
├── /register                        ← Đăng ký
├── /Error                           ← Trang lỗi
│
├── /api/payos/                      ← PayOS API
│   ├── callback (POST/GET)          ← Webhook callback
│   └── return (GET)                 ← Return URL
│
└── /admin/                          ← Admin Panel [🔒 Admin Only]
    ├── /login (POST)                ← Đăng nhập admin
    ├── /logout (POST/GET)           ← Đăng xuất admin
    ├── /dashboard                   ← Bảng điều khiển
    ├── /products                    ← Quản lý sản phẩm
    │   ├── /create                  ← Tạo mới
    │   └── /edit/{Id}               ← Chỉnh sửa
    ├── /inventory                   ← Quản lý kho
    ├── /orders                      ← Đơn hàng
    ├── /orders-history              ← Lịch sử đơn hàng
    ├── /customers                   ← Khách hàng
    ├── /reviews                     ← Đánh giá
    ├── /suppliers                   ← Nhà cung cấp
    ├── /purchase-orders             ← Đơn nhập hàng
    │   ├── /create                  ← Tạo mới
    │   └── /edit/{Id}               ← Chỉnh sửa
    └── /promotions                  ← Khuyến mãi
        ├── /create                  ← Tạo mới
        └── /edit/{Id}               ← Chỉnh sửa
```

*Cập nhật lần cuối: December 2025*
