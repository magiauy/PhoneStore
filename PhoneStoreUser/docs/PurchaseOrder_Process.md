# Tài liệu Quy trình Nhập hàng (Purchase Order) - PhoneStoreUser

Tài liệu này mô tả chi tiết quy trình nhập hàng trong hệ thống `PhoneStoreUser`, bao gồm các thao tác trên giao diện người dùng (Frontend) và luồng xử lý dữ liệu dưới Backend.

## 1. Quy trình trên Giao diện (Frontend)

Người dùng (Admin/Kho) thực hiện các bước sau để tạo và hoàn tất một phiếu nhập hàng.

### Các bước thực hiện:

1.  **Truy cập trang**: Vào menu **Quản lý nhập hàng** -> Nhấn **Tạo mới** hoặc chọn một phiếu nháp để sửa.
2.  **Thông tin chung**:
    *   Chọn **Nhà cung cấp** (Bắt buộc): Nhấn nút "Chọn", tìm kiếm và chọn nhà cung cấp từ danh sách.
    *   Nhập **Ghi chú** (Tùy chọn).
3.  **Thêm sản phẩm**:
    *   Chọn sản phẩm từ danh sách thả xuống.
    *   Nhấn nút **Thêm**.
    *   Điều chỉnh **Số lượng** và **Đơn giá** trực tiếp trên bảng.
4.  **Nhập Serial/IMEI** (Đối với sản phẩm có theo dõi Serial):
    *   Nhấn vào liên kết số lượng Serial (ví dụ: `0/5 Serial`) ở cột Serial.
    *   Hệ thống hiển thị popup nhập Serial tương ứng với số lượng sản phẩm.
    *   Nhập **Serial Number** (Bắt buộc).
    *   Tùy chọn bật/tắt nhập **IMEI 1**, **IMEI 2**.
    *   Nhấn **Kiểm tra & Lưu**: Hệ thống sẽ kiểm tra trùng lặp (trên giao diện và dưới DB). Nếu hợp lệ, dữ liệu sẽ được lưu tạm vào bộ nhớ.
5.  **Lưu / Hoàn tất**:
    *   **Lưu nháp**: Nhấn nút "Lưu nháp". Dữ liệu được lưu xuống server, trạng thái là `DRAFT`.
    *   **Hoàn tất nhập hàng**:
        *   Hệ thống kiểm tra xem đã nhập đủ Serial chưa.
        *   Hiển thị hộp thoại xác nhận.
        *   Nếu đồng ý, hệ thống lưu dữ liệu lần cuối và chuyển trạng thái sang `RECEIVED`. Tồn kho được cập nhật.

### Sequence Diagram (Frontend Interactions)

```mermaid
sequenceDiagram
    actor User
    participant UI as PurchaseOrderPage
    participant Modal as SerialModal
    participant Server as API Service

    User->>UI: Mở trang "Tạo phiếu nhập"
    
    rect rgb(240, 248, 255)
    note right of User: 1. Nhập thông tin chung
    User->>UI: Chọn Nhà cung cấp
    User->>UI: Nhập Ghi chú
    end

    rect rgb(255, 250, 240)
    note right of User: 2. Thêm sản phẩm
    User->>UI: Chọn Sản phẩm -> Nhấn "Thêm"
    User->>UI: Chỉnh sửa Số lượng = N, Đơn giá
    end

    rect rgb(240, 255, 240)
    note right of User: 3. Nhập Serial (Nếu sản phẩm yêu cầu)
    User->>UI: Click Link "Serial"
    UI->>Modal: Mở Modal (Tạo N dòng trống)
    User->>Modal: Nhập Serial/IMEI
    User->>Modal: Nhấn "Kiểm tra & Lưu"
    Modal->>Server: Validate Serial/IMEI (Check Exist)
    Server-->>Modal: Kết quả (OK/Fail)
    alt Valid
        Modal->>UI: Đóng Modal, Lưu tạm vào List
    else Invalid
        Modal->>User: Hiển thị lỗi trùng lặp
    end
    end

    rect rgb(255, 240, 245)
    note right of User: 4. Hoàn tất
    User->>UI: Nhấn "Hoàn tất nhập hàng"
    UI->>UI: Validate (Đủ Serial chưa?)
    UI->>User: Hiển thị Confirm Modal
    User->>UI: Xác nhận "Đồng ý"
    UI->>Server: UpdatePurchaseOrder (Lưu data mới nhất)
    UI->>Server: CompletePurchaseOrder (Chốt phiếu)
    Server-->>UI: Thành công
    UI->>User: Chuyển hướng về Danh sách
    end
```

---

## 2. Quy trình Xử lý dưới Backend

Phần này mô tả cách dữ liệu được xử lý, lưu trữ và luân chuyển khi người dùng gọi các API.

### Luồng dữ liệu chính:

1.  **Lưu Nháp (Create/Update)**:
    *   Mục tiêu: Lưu trạng thái làm việc hiện tại, chưa ảnh hưởng tồn kho thực tế.
    *   Dữ liệu `PurchaseOrder` và `PurchaseOrderLine` được lưu/cập nhật.
    *   Dữ liệu `ProductSerial` được lưu với trạng thái **`RESERVED`** (Đã giữ chỗ, nhưng chưa bán được).

2.  **Hoàn tất (Complete)**:
    *   Mục tiêu: Xác nhận hàng đã về kho, cộng số lượng tồn kho, cho phép bán.
    *   **Tạo Lô (Batch)**: Tạo một bản ghi `BatchEntity` để quản lý đợt nhập hàng này.
    *   **Cập nhật Serial**: Chuyển trạng thái `ProductSerial` từ `RESERVED` sang **`in_stock`**. Gán `BatchId` cho các Serial này.
    *   **Tạo Tồn kho (BatchProduct)**: Tạo các bản ghi vào bảng `BatchProduct` (Đây là bảng quản lý số lượng tồn kho thực tế theo lô và giá vốn).
    *   **Cập nhật Trạng thái Phiếu**: Đổi Status của PO sang **`RECEIVED`**.

### Sequence Diagram (Backend Processing)

```mermaid
sequenceDiagram
    participant Controller as PO Controller
    participant Service as PurchaseOrderService
    participant DB as Database (SQL)

    note over Controller, DB: Hành động: Hoàn tất phiếu nhập (CompletePurchaseOrder)

    Controller->>Service: CompletePurchaseOrderAsync(id)
    
    Service->>DB: Get PO by ID (Include Lines)
    DB-->>Service: PO Data (Status: DRAFT)
    
    alt Status != DRAFT
        Service-->>Controller: Error ("Only draft orders can be completed")
    end

    Service->>Service: Start Transaction (Implicit EF)

    rect rgb(200, 230, 255)
    note right of Service: 1. Tạo Lô hàng (Batch)
    Service->>DB: Insert BatchEntity (Code, CreatedAt)
    end

    rect rgb(220, 255, 220)
    note right of Service: 2. Kích hoạt Serial
    Service->>DB: Select ProductSerials where POLineId IN (...)
    DB-->>Service: List Serials (Status: RESERVED)
    loop Each Serial
        Service->>Service: Set Status = 'in_stock'
        Service->>Service: Set BatchId = NewBatch.Id
    end
    end

    rect rgb(255, 250, 205)
    note right of Service: 3. Cập nhật Tồn kho
    loop Each PO Line
        Service->>DB: Insert BatchProductEntity (Qty, CostPrice)
    end
    end

    rect rgb(255, 220, 220)
    note right of Service: 4. Chốt Phiếu
    Service->>Service: Set PO.Status = 'RECEIVED'
    Service->>DB: SaveChangesAsync()
    end

    Service-->>Controller: Success
```

### Chi tiết thay đổi Model

*   **ProductSerialEntity**:
    *   Khi tạo PO (Draft): Status = `RESERVED`
    *   Khi hoàn tất PO: Status = `in_stock`, `BatchId` được cập nhật.
*   **BatchProductEntity**:
    *   Được tạo mới hoàn toàn khi hoàn tất PO. Đây là nguồn dữ liệu để tính "Số lượng tồn" hiện tại của sản phẩm.
