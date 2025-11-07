# Product Model & Attribute Options Plan

## Mục tiêu
Thiết kế lại mô hình dữ liệu sản phẩm để hỗ trợ gom nhóm theo model và tái sử dụng các giá trị thuộc tính phổ biến, giảm thao tác nhập liệu khi tạo sản phẩm mới.

## Bảng dữ liệu mới

### 1. `product_models`
- **Mục đích**: Lưu thông tin cốt lõi của một model sản phẩm (ví dụ: "iPhone 15 Pro Max").
- **Các cột chính**:
  - `id` (PK, GUID hoặc INT tự tăng)
  - `name` (tên model hiển thị)
  - `slug` (chuẩn hóa để tìm kiếm/gom nhóm)
  - `description`
  - `default_image_url`
  - `created_at`, `updated_at`
- **Quan hệ**:
  - Một model có nhiều sản phẩm/biến thể (`products` hoặc `product_variants`).
  - Sản phẩm hiện tại cần thêm khóa ngoại `model_id` để tham chiếu. 
  
### 2. `product_attribute_options`
- **Mục đích**: Định nghĩa sẵn các giá trị khả dụng cho từng thuộc tính (RAM, ROM, màu sắc...).
- **Các cột chính**:
  - `id` (PK)
  - `attribute_id` (FK tới `product_attributes`)
  - `display_value` (chuỗi hiển thị cho người dùng, ví dụ "128 GB")
  - `normalized_value` (giá trị chuẩn dùng cho so sánh/lưu trữ, ví dụ số 128 cho thuộc tính dạng số)
  - `sort_order`
  - `is_active`
- **Quan hệ**:
  - Một thuộc tính có thể có nhiều option.
  - Các option sẽ được ánh xạ vào `product_attribute_values` khi người dùng chọn.

## Cập nhật bảng hiện có
- Thêm cột `model_id` (FK tới `product_models`) vào bảng `products`.
- Cho phép một model có nhiều sản phẩm để hỗ trợ biến thể (mỗi biến thể vẫn là bản ghi `products`).

## Thay đổi trong dịch vụ/backend
1. **ProductService**
   - Khi tạo/sửa sản phẩm, bắt buộc chọn `model_id`.
   - Khi nạp thuộc tính, kiểm tra xem thuộc tính có option hay không:
     - Nếu có, trả về danh sách option để UI render `ComboBox`.
     - Nếu không, giữ nguyên hành vi hiện tại theo `data_type`.
   - Khi lưu giá trị, nếu thuộc tính có option thì lưu cả `option_id` và giá trị chuẩn tương ứng.

2. **ProductAttributeRepository**
   - Bổ sung phương thức lấy danh sách option theo `attribute_id`.
   - Hỗ trợ seed dữ liệu option mặc định (ví dụ RAM: 4/6/8 GB, ROM: 64/128/256 GB...).

3. **ProductModelRepository**
   - CRUD cơ bản cho `product_models`.
   - API/Service để liệt kê model phục vụ UI.

## Cập nhật giao diện quản trị

### Product Page UI Redesign
Thiết kế lại trang quản lý sản phẩm với cấu trúc **hai cấp độ**:

#### 1. **ProductPage** (Trang chính)
- **Hiển thị**: Danh sách tất cả **Product Models** (không hiển thị chi tiết từng product).
- **GridView/ListBox**: Mỗi item hiển thị thông tin cốt lõi của model:
  - `default_image_url` (thumbnail)
  - `name` (tên model, ví dụ "iPhone 15 Pro Max")
  - `description` (mô tả ngắn)
  - Số lượng product biến thể thuộc model này
- **Nút "Create Product Model"** để tạo mới một model (dialog hoặc form nhập).
  - Form tạo model gồm: `name`, `description`, `default_image_url`.

#### 2. **ProductPane** (Pane mở khi bấm vào một Product Model)
- **Hiển thị thông tin model được chọn**:
  - Basic info: name, description, image.
  - **Nút "Create Product"** để tạo sản phẩm mới với thông số chi tiết.
  
- **GridView/ListBox các Product thuộc model này**:
  - Mỗi product item hiển thị: SKU, tên, giá, tồn kho, thuộc tính chính (RAM, ROM, màu sắc...).
  - Cho phép edit/delete từng product.

#### 3. **ProductDialog** (Tạo/Sửa Product)
- **Thêm bước chọn `Model`** (combobox) - bắt buộc.
- **Render động thuộc tính với checkbox lựa chọn**:
  - Mỗi thuộc tính hiển thị trên một hàng với cấu trúc: `[Checkbox] [Tên Attribute] [Input Control]`
  - Checkbox nằm ngang với input field, cho phép chọn thuộc tính nào cần lưu.
  - Chỉ những attribute được tích checkbox mới được lưu vào database.
  - Nếu thuộc tính có option: sử dụng `ComboBox`/`Select` với danh sách từ backend.
  - Nếu không: sử dụng control hiện tại (textbox, number input, date picker, checkbox).
- Lưu cả `option_id` trong state để gửi lên backend.

#### 4. **ProductModelDialog** (Tạo/Sửa Model)
- Form nhập: `name`, `description`, `default_image_url`.
- Có tùy chọn "Duplicate Model" để copy một model hiện có (tái sử dụng cấu hình).

#### Luồng thao tác:
1. **Tạo Product Model mới**:
   - User bấm "Create Product Model" trên ProductPage.
   - ProductModelDialog mở, nhập name/description/image → Save.
   - Model tạo mới xuất hiện trong danh sách ProductPage.

2. **Xem chi tiết Product Model và quản lý Product**:
   - User bấm vào model trong danh sách ProductPage.
   - ProductPane mở hiển thị thông tin model và danh sách product biến thể.
   - Nút "Create Product" trong ProductPane dùng để tạo product mới.

3. **Tạo Product từ Model**:
   - User bấm "Create Product" trong ProductPane.
   - ProductDialog mở, model_id được set sẵn.
   - User chọn cấu hình (RAM, ROM, màu sắc từ option list).
   - Save product → Xuất hiện trong danh sách product của ProductPane.

4. **Quản lý Product**:
   - Bấm vào product trong ProductPane để edit chi tiết thuộc tính/giá/tồn kho.
   - Bấm xóa để xóa product (không ảnh hưởng đến model).

## Cập nhật website bán hàng
- Khi hiển thị sản phẩm, group theo `model_id` để người dùng thấy một model với nhiều cấu hình (biến thể).
- Hiển thị danh sách biến thể (RAM/ROM/màu sắc) bằng cách đọc từ thuộc tính và option đã chọn.

## Kế hoạch triển khai
1. Tạo migration cho `product_models`, `product_attribute_options`, thêm `model_id` vào `products`.
2. Seed dữ liệu model mẫu và các option phổ biến.
3. Cập nhật repository/dịch vụ để hỗ trợ model và option.
4. Điều chỉnh UI admin để sử dụng combo khi có option, nhập tự do khi không.
5. Cập nhật UI người dùng để group sản phẩm theo model và hiển thị danh sách cấu hình.
6. Viết test/kiểm thử đảm bảo luồng tạo sản phẩm, hiển thị sản phẩm hoạt động đúng với model + option.

## Lợi ích
- Giảm thao tác nhập liệu nhờ tái sử dụng option.
- Cho phép gom nhóm sản phẩm theo model, phục vụ hiển thị nhiều biến thể trên web bán hàng.
- Vẫn giữ được sự linh hoạt cho những thuộc tính chưa có option sẵn.

## Rủi ro & lưu ý
- Cần migration dữ liệu để gán `model_id` cho sản phẩm hiện có.
- Đảm bảo không phá vỡ logic liên quan đến tồn kho/đơn hàng vốn phụ thuộc vào `product_id`.
- Cần cập nhật API tích hợp bên ngoài (nếu có) để bao gồm thông tin `model_id` hoặc danh sách option.