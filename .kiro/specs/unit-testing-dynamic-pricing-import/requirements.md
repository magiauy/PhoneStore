# Requirements Document

## Introduction

Tài liệu này mô tả các yêu cầu unit test cho chức năng nhập hàng (Purchase Order) và cơ chế Dynamic Pricing. Mục tiêu là đảm bảo tính chính xác của các logic nghiệp vụ phức tạp bao gồm: tính toán giá FIFO weighted average, cập nhật NIFO cost, xác định market trend, và tự động điều chỉnh giá bán.

## Glossary

- **DynamicPricingService**: Service tính toán và cập nhật giá bán tự động dựa trên biến động chi phí thị trường
- **PurchaseOrderService**: Service quản lý Purchase Order, bao gồm tạo PO và đánh dấu đã nhận hàng
- **BatchesService**: Service quản lý Batch (lô hàng), tạo batch khi nhập hàng
- **CostFifo**: Giá vốn bình quân gia quyền của hàng tồn kho (First In First Out)
- **CostNifo**: Giá thay thế - giá nhập của lô hàng mới nhất (Next In First Out)
- **Variance**: Tỷ lệ chênh lệch giữa NIFO và FIFO, công thức: (NIFO - FIFO) / FIFO
- **MarketTrend**: Xu hướng thị trường (UP, STABLE, DOWN) dựa trên variance
- **PricingMode**: Chế độ định giá (AUTO_PROTECT hoặc CLEARANCE)
- **Unit_Test**: Test đơn vị kiểm tra logic của một method/function độc lập

## Requirements

### Requirement 1: Unit Test cho UpdateFifoCost

**User Story:** As a developer, I want comprehensive unit tests for UpdateFifoCost method, so that I can ensure FIFO weighted average calculation is correct in all scenarios.

#### Acceptance Criteria

1. WHEN UpdateFifoCost is called with a new product (no existing stock), THE test SHALL verify CostFifo is set equal to newCost.
2. WHEN UpdateFifoCost is called with existing stock, THE test SHALL verify weighted average formula: ((OldFifo × OldStock) + (NewCost × NewQuantity)) / (OldStock + NewQuantity).
3. WHEN UpdateFifoCost is called with invalid productId, THE test SHALL verify method returns false without throwing exception.
4. WHEN UpdateFifoCost is called with newCost <= 0, THE test SHALL verify method returns false.
5. WHEN UpdateFifoCost is called with quantity <= 0, THE test SHALL verify method returns false.

### Requirement 2: Unit Test cho UpdateNifoCost

**User Story:** As a developer, I want comprehensive unit tests for UpdateNifoCost method, so that I can ensure pricing workflow is triggered correctly based on market conditions.

#### Acceptance Criteria

1. WHEN UpdateNifoCost is called with variance > 0 (Market UP) AND PricingMode is AUTO_PROTECT, THE test SHALL verify price is increased using CalculateAutoProtectPrice formula.
2. WHEN UpdateNifoCost is called with variance between StableRangeMin (-5%) and StableRangeMax (0%), THE test SHALL verify price is held unchanged.
3. WHEN UpdateNifoCost is called with variance <= VarianceThreshold (-10%), THE test SHALL verify PricingAlert is created.
4. WHEN UpdateNifoCost is called for new product without CostFifo, THE test SHALL verify CostFifo is initialized to newNifoCost.
5. WHEN UpdateNifoCost is called for new product without Price, THE test SHALL verify Price is initialized using AUTO_PROTECT formula.
6. WHEN UpdateNifoCost is called with newNifoCost <= 0, THE test SHALL verify method returns failure result.

### Requirement 3: Unit Test cho CalculateAutoProtectPrice

**User Story:** As a developer, I want unit tests for CalculateAutoProtectPrice method, so that I can ensure AUTO_PROTECT pricing formula is correct.

#### Acceptance Criteria

1. WHEN CalculateAutoProtectPrice is called, THE test SHALL verify formula: MAX(CostFifo, CostNifo) × (1 + DesiredMargin).
2. WHEN CostFifo > CostNifo, THE test SHALL verify CostFifo is used as base cost.
3. WHEN CostNifo > CostFifo, THE test SHALL verify CostNifo is used as base cost.
4. THE test SHALL verify result is rounded to 2 decimal places.

### Requirement 4: Unit Test cho CalculateClearancePrice

**User Story:** As a developer, I want unit tests for CalculateClearancePrice method, so that I can ensure CLEARANCE pricing formula is correct.

#### Acceptance Criteria

1. WHEN CalculateClearancePrice is called, THE test SHALL verify formula: CostNifo × (1 + MinimumMargin).
2. THE test SHALL verify result is rounded to 2 decimal places.

### Requirement 5: Unit Test cho CalculateVariance

**User Story:** As a developer, I want unit tests for CalculateVariance method, so that I can ensure variance calculation is correct.

#### Acceptance Criteria

1. WHEN CalculateVariance is called, THE test SHALL verify formula: (CostNifo - CostFifo) / CostFifo.
2. WHEN CostFifo <= 0, THE test SHALL verify method returns 0.
3. THE test SHALL verify positive variance when CostNifo > CostFifo (Market UP).
4. THE test SHALL verify negative variance when CostNifo < CostFifo (Market DOWN).

### Requirement 6: Unit Test cho TriggerPricingUpdate trong PurchaseOrderService

**User Story:** As a developer, I want unit tests for TriggerPricingUpdate integration, so that I can ensure pricing is updated correctly when receiving goods.

#### Acceptance Criteria

1. WHEN MarkAsReceived is called, THE test SHALL verify UpdateFifoCost is called for each product in PurchaseOrderLines.
2. WHEN MarkAsReceived is called, THE test SHALL verify UpdateNifoCost is called for each product after UpdateFifoCost.
3. WHEN pricing update fails for one product, THE test SHALL verify other products are still processed.
4. THE test SHALL verify pricing updates are triggered AFTER transaction commits.

### Requirement 7: Unit Test cho TriggerPricingUpdates trong BatchesService

**User Story:** As a developer, I want unit tests for TriggerPricingUpdates in BatchesService, so that I can ensure pricing is updated when creating batches.

#### Acceptance Criteria

1. WHEN Insert batch is called, THE test SHALL verify UpdateFifoCost is called for each BatchProduct.
2. WHEN Insert batch is called, THE test SHALL verify UpdateNifoCost is called for each BatchProduct after UpdateFifoCost.
3. WHEN pricing update fails for one product, THE test SHALL verify batch creation is not affected.

