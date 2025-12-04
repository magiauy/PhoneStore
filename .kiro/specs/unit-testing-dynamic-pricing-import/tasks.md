# Implementation Plan

- [x] 1. Mở rộng TestDataFactory với factory methods mới





  - Thêm CreateProduct method với các pricing fields (CostFifo, CostNifo, MarketTrend, PricingMode)
  - Thêm CreatePurchaseOrder, CreatePurchaseOrderLine methods
  - Thêm CreateBatch, CreateBatchProduct methods
  - _Requirements: 1.1, 2.1, 6.1, 7.1_

- [x] 2. Tạo DynamicPricingServiceTests




  - [x] 2.1 Setup test class với mock dependencies

    - Mock IProductRepository, IPricingHistoryRepository, IPricingAlertRepository
    - Mock ISettingStringService với default pricing configuration
    - Mock IProductSerialRepository
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1_

  - [x] 2.2 Implement UpdateFifoCost tests

    - Test NewProduct_SetsFifoEqualToNewCost
    - Test ExistingStock_CalculatesWeightedAverage
    - Test ProductNotFound_ReturnsFalse
    - Test InvalidNewCost_ReturnsFalse
    - Test InvalidQuantity_ReturnsFalse
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_


  - [x] 2.3 Implement UpdateNifoCost tests
    - Test MarketUp_AutoProtect_IncreasesPrice
    - Test MarketStable_HoldsPrice
    - Test MarketCrash_CreatesAlert
    - Test NewProduct_NoCostFifo_InitializesFifo
    - Test NewProduct_NoPrice_InitializesPrice
    - Test InvalidNifoCost_ReturnsFailure
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x] 2.4 Implement CalculateAutoProtectPrice tests
    - Test FifoGreaterThanNifo_UsesFifo
    - Test NifoGreaterThanFifo_UsesNifo
    - Test RoundsToTwoDecimals
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

  - [x] 2.5 Implement CalculateClearancePrice tests
    - Test CalculatesWithMinimumMargin
    - Test RoundsToTwoDecimals
    - _Requirements: 4.1, 4.2_

  - [x] 2.6 Implement CalculateVariance tests

    - Test PositiveVariance_MarketUp
    - Test NegativeVariance_MarketDown
    - Test ZeroVariance_Stable
    - Test FifoZero_ReturnsZero
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [x] 3. Tạo PurchaseOrderServiceTests cho pricing integration






  - [x] 3.1 Setup test class với mock dependencies

    - Mock IDynamicPricingService và các repository dependencies
    - _Requirements: 6.1_

  - [x] 3.2 Implement TriggerPricingUpdate tests


    - Test MarkAsReceived_CallsUpdateFifoCost_ForEachProduct
    - Test MarkAsReceived_CallsUpdateNifoCost_ForEachProduct
    - Test PricingUpdateFailure_DoesNotFailOperation
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [x] 4. Tạo BatchesServiceTests cho pricing integration






  - [x] 4.1 Setup test class với mock dependencies

    - Mock IDynamicPricingService và các repository dependencies
    - _Requirements: 7.1_


  - [x] 4.2 Implement TriggerPricingUpdates tests

    - Test Insert_CallsUpdateFifoCost_ForEachBatchProduct
    - Test Insert_CallsUpdateNifoCost_ForEachBatchProduct
    - Test PricingUpdateFailure_DoesNotFailBatchCreation
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 5. Chạy và verify tất cả tests





  - Chạy dotnet test để verify tất cả tests pass
  - Kiểm tra test coverage cho các methods chính
  - _Requirements: 1.1-7.3_

