# Implementation Plan

- [x] 1. Mở rộng IDynamicPricingService Interface






  - [x] 1.1 Thêm method UpdateFifoCost vào interface

    - Thêm method signature: `bool UpdateFifoCost(int productId, decimal newCost, int quantity)`
    - Thêm XML documentation mô tả weighted average formula
    - _Requirements: 2.1, 2.2, 2.3_

- [x] 2. Implement UpdateFifoCost trong DynamicPricingService





  - [x] 2.1 Implement logic tính weighted average FIFO


    - Lấy product từ repository
    - Lấy current stock từ ProductSerialRepository
    - Tính newFifo = ((OldFifo × OldStock) + (NewCost × NewQuantity)) / (OldStock + NewQuantity)
    - Nếu OldStock = 0 hoặc OldFifo <= 0, set newFifo = newCost
    - Update product.CostFifo và save
    - Log vào PricingHistory với reason "FIFO_WEIGHTED_AVG" hoặc "INITIAL_FIFO"
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 3. Enhance UpdateNifoCost để xử lý sản phẩm mới





  - [x] 3.1 Thêm logic khởi tạo CostFifo cho sản phẩm mới


    - Kiểm tra nếu product.CostFifo <= 0, set CostFifo = newNifoCost
    - _Requirements: 5.1_
  - [x] 3.2 Thêm logic khởi tạo Price cho sản phẩm mới


    - Kiểm tra nếu product.Price <= 0, tính giá bằng CalculateAutoProtectPrice
    - Log vào PricingHistory với reason "INITIAL_PRICING"
    - _Requirements: 5.2, 5.3_

- [x] 4. Tích hợp DynamicPricingService vào PurchaseOrderService





  - [x] 4.1 Inject IDynamicPricingService vào PurchaseOrderService


    - Thêm private field `_dynamicPricingService`
    - Thêm parameter vào constructor
    - _Requirements: 1.1_
  - [x] 4.2 Tạo method TriggerPricingUpdate


    - Tạo private method `TriggerPricingUpdate(int productId, decimal unitCost, int quantity)`
    - Gọi `_dynamicPricingService.UpdateFifoCost()` trước
    - Gọi `_dynamicPricingService.UpdateNifoCost()` sau
    - Wrap trong try-catch để không fail toàn bộ operation
    - Log kết quả pricing update
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  - [x] 4.3 Gọi TriggerPricingUpdate trong MarkAsReceived


    - Sau khi tạo batch và batch products, loop qua PurchaseOrderLines
    - Gọi TriggerPricingUpdate cho mỗi product
    - _Requirements: 1.1, 1.2_

- [x] 5. Cập nhật ServiceContainer để đăng ký dependency mới





  - [x] 5.1 Kiểm tra và cập nhật ServiceContainer nếu cần


    - Đảm bảo IDynamicPricingService được inject vào PurchaseOrderService
    - _Requirements: 1.1_
