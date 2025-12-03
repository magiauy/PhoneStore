# Requirements Document

## Introduction

Hệ thống Định giá Tồn kho Động (Dynamic Pricing Engine) tự động hóa việc định giá bán dựa trên biến động thị trường. Hệ thống hoạt động theo mô hình "Dual-Lens" tách biệt dữ liệu kế toán (FIFO) và kinh doanh (NIFO/Safe Price), nhằm bảo vệ dòng tiền khi thị trường tăng giá và cảnh báo rủi ro tồn kho khi thị trường giảm giá.

## Glossary

- **Dynamic_Pricing_Engine**: Hệ thống tự động tính toán và cập nhật giá bán sản phẩm dựa trên biến động chi phí thị trường
- **FIFO_Cost (cost_fifo)**: Giá vốn bình quân của lô hàng cũ nhất đang tồn kho, tuân thủ nguyên tắc Nhập trước Xuất trước
- **NIFO_Cost (cost_nifo)**: Giá thay thế (Replacement Cost) - giá nhập dự kiến của lô hàng mới nhất
- **Market_Trend**: Xu hướng thị trường được xác định qua so sánh FIFO và NIFO (UP, DOWN, STABLE)
- **Pricing_Mode**: Chế độ định giá của sản phẩm (AUTO_PROTECT hoặc CLEARANCE)
- **AUTO_PROTECT**: Chế độ định giá mặc định, tự động bảo vệ dòng tiền khi thị trường tăng
- **CLEARANCE**: Chế độ xả hàng, áp dụng khi Admin phê duyệt cắt lỗ
- **Variance**: Tỷ lệ chênh lệch giữa NIFO và FIFO, tính bằng công thức (NIFO - FIFO) / FIFO
- **Selling_Price**: Giá bán niêm yết cuối cùng của sản phẩm
- **Admin**: Quản trị viên có quyền phê duyệt quyết định định giá
- **Desired_Margin**: Biên lợi nhuận mong muốn (mặc định 10%)
- **Minimum_Margin**: Biên lợi nhuận tối thiểu khi xả hàng
- **Variance_Threshold**: Ngưỡng chênh lệch giá kích hoạt cảnh báo (mặc định -10%)

## Requirements

### Requirement 1: Quản lý dữ liệu giá sản phẩm

**User Story:** As a system administrator, I want the system to maintain dual pricing data (FIFO and NIFO) for each product, so that pricing decisions can be made based on both accounting and market perspectives.

#### Acceptance Criteria

1. THE Dynamic_Pricing_Engine SHALL store cost_fifo as a float value representing the weighted average cost of the oldest inventory lot for each product.
2. THE Dynamic_Pricing_Engine SHALL store cost_nifo as a float value representing the replacement cost from the latest supplier quote or goods receipt.
3. THE Dynamic_Pricing_Engine SHALL store market_trend as an enumeration with values UP, DOWN, or STABLE for each product.
4. THE Dynamic_Pricing_Engine SHALL store pricing_mode as an enumeration with values AUTO_PROTECT or CLEARANCE, defaulting to AUTO_PROTECT.
5. THE Dynamic_Pricing_Engine SHALL store selling_price as a float value representing the final listed price for each product.

### Requirement 2: Tính toán xu hướng thị trường

**User Story:** As a pricing manager, I want the system to automatically calculate market trend based on cost variance, so that appropriate pricing strategies can be applied.

#### Acceptance Criteria

1. WHEN cost_nifo is updated, THE Dynamic_Pricing_Engine SHALL calculate Variance using the formula: (cost_nifo - cost_fifo) / cost_fifo.
2. WHEN Variance is greater than 0, THE Dynamic_Pricing_Engine SHALL set market_trend to UP.
3. WHEN Variance is between -5% and 0 (inclusive), THE Dynamic_Pricing_Engine SHALL set market_trend to STABLE.
4. WHEN Variance is less than or equal to Variance_Threshold, THE Dynamic_Pricing_Engine SHALL set market_trend to DOWN.

### Requirement 3: Định giá tự động chế độ AUTO_PROTECT

**User Story:** As a business owner, I want the system to automatically increase selling price when market costs rise, so that cash flow is protected for restocking.

#### Acceptance Criteria

1. WHILE pricing_mode is AUTO_PROTECT AND market_trend is UP, THE Dynamic_Pricing_Engine SHALL calculate selling_price using formula: MAX(cost_fifo, cost_nifo) × (1 + Desired_Margin).
2. WHILE pricing_mode is AUTO_PROTECT AND market_trend is STABLE, THE Dynamic_Pricing_Engine SHALL maintain the current selling_price without changes.
3. WHEN cost_nifo increases AND pricing_mode is AUTO_PROTECT, THE Dynamic_Pricing_Engine SHALL update selling_price immediately without requiring approval.
4. WHEN selling_price is automatically updated in AUTO_PROTECT mode, THE Dynamic_Pricing_Engine SHALL log the price change with timestamp and variance percentage.

### Requirement 4: Cảnh báo rủi ro tồn kho

**User Story:** As an administrator, I want to receive alerts when market prices drop significantly, so that I can make informed decisions about inventory pricing strategy.

#### Acceptance Criteria

1. WHEN Variance is less than or equal to Variance_Threshold, THE Dynamic_Pricing_Engine SHALL halt automatic price updates for the affected product.
2. WHEN Variance is less than or equal to Variance_Threshold, THE Dynamic_Pricing_Engine SHALL generate an alert notification containing: product name, variance percentage, current inventory quantity, and recommended actions.
3. WHEN an inventory risk alert is generated, THE Dynamic_Pricing_Engine SHALL send the notification to all users with Admin role within 1 minute.
4. WHILE an unresolved inventory risk alert exists for a product, THE Dynamic_Pricing_Engine SHALL maintain the existing selling_price until Admin action is taken.

### Requirement 5: Xử lý quyết định Admin cho cảnh báo giảm giá

**User Story:** As an administrator, I want to choose between holding price or activating clearance mode when market crashes, so that I can control the business response to market conditions.

#### Acceptance Criteria

1. WHEN Admin selects "Hold Price" action for an inventory risk alert, THE Dynamic_Pricing_Engine SHALL maintain current selling_price and mark the alert as resolved with action "HOLD".
2. WHEN Admin selects "Activate Clearance" action for an inventory risk alert, THE Dynamic_Pricing_Engine SHALL change pricing_mode to CLEARANCE for the affected product.
3. WHEN pricing_mode changes to CLEARANCE, THE Dynamic_Pricing_Engine SHALL recalculate selling_price using formula: cost_nifo × (1 + Minimum_Margin).
4. WHEN pricing_mode changes to CLEARANCE, THE Dynamic_Pricing_Engine SHALL mark the alert as resolved with action "CLEARANCE_ACTIVATED".

### Requirement 6: Cập nhật giá khi nhập hàng

**User Story:** As a warehouse staff, I want the system to automatically update replacement cost when I create goods receipt, so that pricing reflects current market conditions.

#### Acceptance Criteria

1. WHEN a goods receipt is created with a new unit cost, THE Dynamic_Pricing_Engine SHALL update cost_nifo to the new unit cost value.
2. WHEN a supplier quote is updated for a product, THE Dynamic_Pricing_Engine SHALL update cost_nifo to the quoted price.
3. WHEN cost_nifo is updated, THE Dynamic_Pricing_Engine SHALL trigger the variance calculation and appropriate pricing workflow within 5 seconds.

### Requirement 7: Giao diện Admin - Dashboard biến động giá

**User Story:** As an administrator, I want to view a real-time dashboard comparing FIFO and NIFO costs, so that I can monitor market trends across all products.

#### Acceptance Criteria

1. THE Dynamic_Pricing_Engine SHALL display a chart comparing cost_fifo and cost_nifo trends over time for each product.
2. THE Dynamic_Pricing_Engine SHALL update the dashboard data at intervals not exceeding 5 minutes.
3. THE Dynamic_Pricing_Engine SHALL display notifications with color coding: green for automatic price increases, red for inventory risk alerts requiring action.
4. WHEN a green notification is displayed, THE Dynamic_Pricing_Engine SHALL show message format: "Đã tự động tăng giá sản phẩm [product_name] theo thị trường (+[amount])."
5. WHEN a red notification is displayed, THE Dynamic_Pricing_Engine SHALL show message format: "CẢNH BÁO: [product_name] giá nhập giảm [variance]%. Tồn kho hiện tại: [quantity]. Yêu cầu hành động."

### Requirement 8: Giao diện khách hàng - Hiển thị giá và tồn kho

**User Story:** As a customer, I want to see clear pricing labels and stock availability, so that I understand the value and urgency of my purchase.

#### Acceptance Criteria

1. WHILE pricing_mode is AUTO_PROTECT AND selling_price was recently updated, THE Dynamic_Pricing_Engine SHALL display label "Giá thị trường (Cập nhật 24h)" on the product.
2. WHILE pricing_mode is CLEARANCE, THE Dynamic_Pricing_Engine SHALL display label "Giá ưu đãi xả kho (Số lượng có hạn)" on the product.
3. THE Dynamic_Pricing_Engine SHALL display actual available inventory quantity for each product.
4. WHEN inventory quantity is 5 or fewer, THE Dynamic_Pricing_Engine SHALL display message "Chỉ còn [quantity] sản phẩm với mức giá này".

### Requirement 9: Cấu hình hệ thống

**User Story:** As a system administrator, I want to configure pricing parameters, so that the system can be tuned to business requirements.

#### Acceptance Criteria

1. THE Dynamic_Pricing_Engine SHALL allow configuration of Desired_Margin with a default value of 10%.
2. THE Dynamic_Pricing_Engine SHALL allow configuration of Minimum_Margin with a default value of 5%.
3. THE Dynamic_Pricing_Engine SHALL allow configuration of Variance_Threshold with a default value of -10%.
4. WHEN configuration parameters are updated, THE Dynamic_Pricing_Engine SHALL apply new values to subsequent pricing calculations without requiring system restart.
