# Implementation Plan

- [x] 1. Tạo Enums và Models cho Dynamic Pricing





  - [x] 1.1 Tạo enum MarketTrend trong PhoneStoreRepository/Models/Enums


    - Tạo file `MarketTrend.cs` với values: STABLE = 0, UP = 1, DOWN = 2
    - _Requirements: 2.2, 2.3, 2.4_
  - [x] 1.2 Tạo enum PricingMode trong PhoneStoreRepository/Models/Enums


    - Tạo file `PricingMode.cs` với values: AUTO_PROTECT = 0, CLEARANCE = 1
    - _Requirements: 1.4_
  - [x] 1.3 Tạo enum AlertStatus trong PhoneStoreRepository/Models/Enums


    - Tạo file `AlertStatus.cs` với values: PENDING = 0, RESOLVED_HOLD = 1, RESOLVED_CLEARANCE = 2
    - _Requirements: 4.4, 5.1, 5.2_
  - [x] 1.4 Mở rộng Product model với các trường pricing


    - Thêm CostFifo, CostNifo, MarketTrend, PricingMode, PriceUpdatedAt vào Product.cs
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [x] 1.5 Tạo model PricingAlert

    - Tạo file `PricingAlert.cs` với các trường: Id, ProductId, VariancePercent, CostFifoSnapshot, CostNifoSnapshot, CurrentStock, Status, ResolvedBy, ResolvedAt, ResolvedNote, CreatedAt
    - _Requirements: 4.2, 4.3_
  - [x] 1.6 Tạo model PricingHistory


    - Tạo file `PricingHistory.cs` với các trường: Id, ProductId, OldPrice, NewPrice, CostFifo, CostNifo, ChangeReason, ChangedBy, CreatedAt
    - _Requirements: 3.4_

- [x] 2. Tạo Repository Layer cho Pricing





  - [x] 2.1 Tạo interface IPricingAlertRepository


    - Tạo file trong PhoneStoreRepository/Repositories/Interfaces
    - Định nghĩa methods: GetById, GetPendingAlerts, GetAlertsByProduct, Insert, Update
    - _Requirements: 4.2, 4.3, 5.1, 5.2_
  - [x] 2.2 Tạo implementation PricingAlertRepository


    - Tạo file trong PhoneStoreRepository/Repositories/Implementations
    - Implement các methods với Entity Framework
    - _Requirements: 4.2, 4.3, 5.1, 5.2_

  - [x] 2.3 Tạo interface IPricingHistoryRepository

    - Tạo file trong PhoneStoreRepository/Repositories/Interfaces
    - Định nghĩa methods: Insert, GetByProduct, GetRecent
    - _Requirements: 3.4_

  - [x] 2.4 Tạo implementation PricingHistoryRepository

    - Tạo file trong PhoneStoreRepository/Repositories/Implementations
    - Implement các methods với Entity Framework
    - _Requirements: 3.4_
  - [ ]* 2.5 Viết unit tests cho repository operations
    - Test CRUD operations cho PricingAlert và PricingHistory
    - _Requirements: 4.2, 3.4_

- [x] 3. Cập nhật Database Context và Migration







  - [x] 3.1 Cập nhật DbContext với các DbSet mới
    - Thêm DbSet<PricingAlert> và DbSet<PricingHistory>
    - Cấu hình relationships và constraints


    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_
  - [x] 3.2 Tạo SQL migration script cho schema changes
    - ALTER TABLE product thêm các cột pricing
    - CREATE TABLE pricing_alert và pricing_history
    - INSERT setting_string cho pricing configuration
    - _Requirements: 9.1, 9.2, 9.3_

- [x] 4. Tạo Service Layer cho Dynamic Pricing





  - [x] 4.1 Tạo ViewModels cho Pricing


    - Tạo PricingUpdateResult, PricingConfiguration, PricingDashboardData, PricingAlertViewModel, PricingHistoryViewModel trong PhoneStoreServices/ViewModels
    - _Requirements: 7.1, 7.3_

  - [x] 4.2 Tạo interface IDynamicPricingService

    - Tạo file trong PhoneStoreServices/Services/Interfaces
    - Định nghĩa methods: UpdateNifoCost, CalculateAutoProtectPrice, CalculateClearancePrice, GetConfiguration, UpdateConfiguration, GetDashboardData, GetPriceHistory
    - _Requirements: 2.1, 3.1, 3.2, 3.3, 5.3, 9.4_
  - [x] 4.3 Tạo implementation DynamicPricingService


    - Implement variance calculation logic
    - Implement AUTO_PROTECT pricing formula: MAX(FIFO, NIFO) × (1 + DesiredMargin)
    - Implement CLEARANCE pricing formula: NIFO × (1 + MinimumMargin)
    - Implement market trend detection based on variance thresholds
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 5.3_

  - [x] 4.4 Tạo interface IPricingAlertService

    - Tạo file trong PhoneStoreServices/Services/Interfaces
    - Định nghĩa methods: GetPendingAlerts, ResolveAsHold, ResolveAsClearance, CreateAlert
    - _Requirements: 4.1, 4.2, 4.3, 5.1, 5.2, 5.4_

  - [x] 4.5 Tạo implementation PricingAlertService

    - Implement alert creation với product snapshot
    - Implement ResolveAsHold: giữ nguyên giá, update status
    - Implement ResolveAsClearance: chuyển PricingMode, tính lại giá
    - _Requirements: 4.1, 4.2, 4.3, 5.1, 5.2, 5.3, 5.4_
  - [ ]* 4.6 Viết unit tests cho DynamicPricingService
    - Test variance calculation
    - Test pricing formulas
    - Test market trend detection
    - _Requirements: 2.1, 3.1, 5.3_

- [x] 5. Tích hợp Pricing vào BatchesService





  - [x] 5.1 Cập nhật BatchesService để trigger pricing update


    - Sau khi tạo Goods Receipt, gọi DynamicPricingService.UpdateNifoCost
    - Truyền unit cost từ BatchProduct vào NIFO
    - _Requirements: 6.1, 6.3_
  - [ ]* 5.2 Viết integration test cho pricing flow
    - Test end-to-end: Goods Receipt → NIFO update → Price calculation
    - _Requirements: 6.1, 6.3_

- [x] 6. Đăng ký Services trong ServiceContainer






  - [x] 6.1 Cập nhật ServiceContainer.cs

    - Đăng ký IDynamicPricingService và IPricingAlertService
    - Đăng ký IPricingAlertRepository và IPricingHistoryRepository
    - _Requirements: 3.3, 4.3, 5.1, 5.2_

- [x] 7. Tạo Admin UI - Pricing Dashboard Page






  - [x] 7.1 Tạo PricingDashboardPage.xaml

    - Layout với Summary Cards (tổng sản phẩm, tăng giá, xả kho, pending alerts)
    - Khu vực biểu đồ FIFO vs NIFO trend
    - Recent Activity list với color coding (green/red)
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 7.2 Tạo PricingDashboardPage.xaml.cs

    - Load dashboard data từ DynamicPricingService
    - Implement refresh logic
    - Handle navigation đến Alert Center
    - _Requirements: 7.1, 7.2_

  - [x] 7.3 Tạo PricingDashboardViewModel

    - Bind data cho Summary Cards
    - Bind data cho Recent Activity list
    - _Requirements: 7.1, 7.3_

- [x] 8. Tạo Admin UI - Pricing Alert Center Page






  - [x] 8.1 Tạo PricingAlertCenterPage.xaml

    - DataGrid hiển thị pending alerts với columns: Product, Variance%, FIFO, NIFO, Stock, Potential Loss, Date
    - Detail panel khi chọn alert
    - Action buttons: "Giữ giá" và "Kích hoạt xả hàng"
    - _Requirements: 4.2, 7.5_
  - [x] 8.2 Tạo PricingAlertCenterPage.xaml.cs


    - Load pending alerts từ PricingAlertService
    - Handle "Giữ giá" action với confirmation dialog
    - Handle "Kích hoạt xả hàng" action với confirmation dialog
    - Refresh list sau khi resolve
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [x] 8.3 Tạo PricingAlertCenterViewModel

    - Bind alerts list
    - Bind selected alert detail
    - Commands cho Hold và Clearance actions
    - _Requirements: 5.1, 5.2_

- [x] 9. Tạo Admin UI - Pricing Settings Page





  - [x] 9.1 Tạo PricingSettingsPage.xaml


    - Input fields cho Desired Margin (%), Minimum Margin (%), Variance Threshold (%)
    - Save button
    - Preview calculator section
    - _Requirements: 9.1, 9.2, 9.3_

  - [x] 9.2 Tạo PricingSettingsPage.xaml.cs
    - Load current configuration từ DynamicPricingService
    - Validate input values
    - Save configuration
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [x] 9.3 Tạo PricingSettingsViewModel

    - Bind configuration values
    - Validation logic
    - Save command
    - _Requirements: 9.1, 9.2, 9.3_

- [x] 10. Tích hợp Navigation và Menu





  - [x] 10.1 Thêm menu items cho Pricing trong MainWindow


    - Thêm "Định giá động" section với sub-items: Dashboard, Cảnh báo, Cài đặt
    - _Requirements: 7.1_

  - [x] 10.2 Cấu hình navigation routes

    - Register PricingDashboardPage, PricingAlertCenterPage, PricingSettingsPage
    - _Requirements: 7.1_

- [x] 11. Hiển thị Pricing Labels trên Customer UI (PhoneStoreUser)






  - [x] 11.1 Cập nhật Product display component

    - Luôn hiển thị Product.Price (giá đã được tính toán ở backend)
    - Hiển thị label "Giá thị trường (Cập nhật 24h)" khi PricingMode = AUTO_PROTECT
    - Hiển thị label "Giá ưu đãi xả kho (Số lượng có hạn)" khi PricingMode = CLEARANCE
    - _Requirements: 8.1, 8.2_

- [ ] 12. Notification System cho Admin
  - [ ] 12.1 Tạo PricingNotificationService
    - Method để tạo green notification khi auto-increase price
    - Method để tạo red notification khi tạo alert
    - _Requirements: 7.3, 7.4, 7.5_
  - [ ] 12.2 Tích hợp notification vào DynamicPricingService
    - Gọi notification service sau khi update price hoặc create alert
    - _Requirements: 4.3, 7.4, 7.5_
