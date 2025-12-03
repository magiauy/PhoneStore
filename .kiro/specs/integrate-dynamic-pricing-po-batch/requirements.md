# Requirements Document

## Introduction

Tích hợp Dynamic Pricing Engine vào quy trình Purchase Order (PO) và Batch để tự động cập nhật giá sản phẩm (Product.Price) khi có hàng nhập mới. Hiện tại DynamicPricingService đã được implement nhưng chưa được tích hợp đầy đủ vào flow nhập hàng từ PO.

## Glossary

- **PurchaseOrderService**: Service quản lý Purchase Order, bao gồm tạo PO, cập nhật và đánh dấu đã nhận hàng
- **BatchesService**: Service quản lý Batch (lô hàng), tạo batch khi nhận hàng từ PO
- **DynamicPricingService**: Service tính toán và cập nhật giá bán tự động dựa trên biến động chi phí
- **CostFifo**: Giá vốn bình quân của lô hàng cũ nhất đang tồn kho (First In First Out)
- **CostNifo**: Giá thay thế - giá nhập của lô hàng mới nhất (Next In First Out)
- **UnitCost**: Giá nhập đơn vị từ PurchaseOrderLine hoặc BatchProduct

## Requirements

### Requirement 1: Tích hợp Dynamic Pricing khi nhận hàng từ PO

**User Story:** As a warehouse staff, I want the system to automatically update product pricing when I mark a purchase order as received, so that selling prices reflect current market conditions.

#### Acceptance Criteria

1. WHEN PurchaseOrderService.MarkAsReceived is called, THE PurchaseOrderService SHALL trigger DynamicPricingService.UpdateNifoCost for each product in the purchase order.
2. WHEN triggering pricing update, THE PurchaseOrderService SHALL pass the UnitCost from PurchaseOrderLine as the new NIFO cost.
3. WHEN pricing update is triggered, THE DynamicPricingService SHALL calculate variance and update Product.Price according to pricing rules.
4. IF pricing update fails for a product, THE PurchaseOrderService SHALL log the error and continue processing other products without failing the entire operation.

### Requirement 2: Cập nhật CostFifo khi nhập hàng mới

**User Story:** As a pricing manager, I want the system to update FIFO cost when new inventory is received, so that pricing calculations use accurate cost data.

#### Acceptance Criteria

1. WHEN a new batch is created with products, THE DynamicPricingService SHALL update CostFifo using weighted average calculation.
2. THE DynamicPricingService SHALL calculate new CostFifo using formula: ((OldFifo × OldStock) + (NewCost × NewQuantity)) / (OldStock + NewQuantity).
3. IF product has no existing stock (OldStock = 0), THE DynamicPricingService SHALL set CostFifo equal to the new UnitCost.
4. WHEN CostFifo is updated, THE DynamicPricingService SHALL log the change in PricingHistory.

### Requirement 3: Chiến lược tự động cập nhật giá Product.Price

**User Story:** As a pricing manager, I want the system to automatically update product prices based on market conditions, so that pricing reflects current market trends.

#### Acceptance Criteria

1. WHEN variance is greater than 0 (Market UP) AND PricingMode is AUTO_PROTECT, THE DynamicPricingService SHALL calculate Product.Price using CalculateAutoProtectPrice with formula MAX(CostFifo, CostNifo) × (1 + DesiredMargin).
2. WHEN variance is between StableRangeMin (-5%) and StableRangeMax (0%) (Market STABLE), THE DynamicPricingService SHALL keep current Product.Price unchanged.
3. WHEN variance is less than or equal to VarianceThreshold (-10%) (Market CRASH), THE DynamicPricingService SHALL create a PricingAlert and halt automatic price updates until Admin resolves.
4. WHEN Admin resolves alert with CLEARANCE action, THE DynamicPricingService SHALL calculate Product.Price using CalculateClearancePrice with formula CostNifo × (1 + MinimumMargin).
5. WHEN PricingMode is CLEARANCE, THE DynamicPricingService SHALL NOT auto-increase price even if market goes UP.

### Requirement 4: Phân biệt giá BatchProduct và Product.Price

**User Story:** As a system administrator, I want to understand the difference between batch expected price and actual selling price, so that pricing logic is clear.

#### Acceptance Criteria

1. THE BatchProduct.SellingPrice SHALL be calculated as UnitCost multiplied by (1 + ProfitMargin) for reference purposes only.
2. THE Product.Price SHALL be the actual selling price calculated by DynamicPricingService based on market conditions.
3. THE Product.Price SHALL be displayed to customers and used in invoices.
4. THE BatchProduct.SellingPrice SHALL NOT affect Product.Price calculation.

### Requirement 5: Xử lý trường hợp sản phẩm mới chưa có giá

**User Story:** As a warehouse staff, I want the system to handle new products without existing pricing data, so that first-time inventory can be priced correctly.

#### Acceptance Criteria

1. IF Product.CostFifo is 0 or null when updating NIFO, THE DynamicPricingService SHALL set CostFifo equal to the new NIFO cost.
2. IF Product.Price is 0 when updating NIFO, THE DynamicPricingService SHALL calculate and set initial price using AUTO_PROTECT formula.
3. WHEN initializing pricing for new product, THE DynamicPricingService SHALL log the initialization in PricingHistory with reason "INITIAL_PRICING".
