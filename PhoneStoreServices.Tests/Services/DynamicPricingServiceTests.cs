using FluentAssertions;
using Moq;
using PhoneStore.Services.Implementations;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreServices.Tests.TestHelpers;
using Xunit;

namespace PhoneStoreServices.Tests.Services;

public class DynamicPricingServiceTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IPricingHistoryRepository> _mockPricingHistoryRepository;
    private readonly Mock<IPricingAlertRepository> _mockPricingAlertRepository;
    private readonly Mock<ISettingStringService> _mockSettingStringService;
    private readonly Mock<IProductSerialRepository> _mockProductSerialRepository;
    private readonly DynamicPricingService _service;

    public DynamicPricingServiceTests()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _mockPricingHistoryRepository = new Mock<IPricingHistoryRepository>();
        _mockPricingAlertRepository = new Mock<IPricingAlertRepository>();
        _mockSettingStringService = new Mock<ISettingStringService>();
        _mockProductSerialRepository = new Mock<IProductSerialRepository>();

        // Setup default pricing configuration
        SetupDefaultPricingConfiguration();

        _service = new DynamicPricingService(
            _mockProductRepository.Object,
            _mockPricingHistoryRepository.Object,
            _mockPricingAlertRepository.Object,
            _mockSettingStringService.Object,
            _mockProductSerialRepository.Object);
    }

    private void SetupDefaultPricingConfiguration()
    {
        _mockSettingStringService.Setup(x => x.GetValue("PRICING_DESIRED_MARGIN", It.IsAny<string>()))
            .Returns("0.10");
        _mockSettingStringService.Setup(x => x.GetValue("PRICING_MINIMUM_MARGIN", It.IsAny<string>()))
            .Returns("0.05");
        _mockSettingStringService.Setup(x => x.GetValue("PRICING_VARIANCE_THRESHOLD", It.IsAny<string>()))
            .Returns("-0.10");
        _mockSettingStringService.Setup(x => x.GetValue("PRICING_STABLE_RANGE_MAX", It.IsAny<string>()))
            .Returns("0");
        _mockSettingStringService.Setup(x => x.GetValue("PRICING_STABLE_RANGE_MIN", It.IsAny<string>()))
            .Returns("-0.05");
    }


    #region UpdateFifoCost Tests

    [Fact]
    public void UpdateFifoCost_NewProduct_SetsFifoEqualToNewCost()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(id: 1, costFifo: 0);
        _mockProductRepository.Setup(x => x.GetById(1)).Returns(product);
        _mockProductSerialRepository.Setup(x => x.GetInStockCountsByProductIds(It.IsAny<IEnumerable<int>>()))
            .Returns(new Dictionary<int, int> { { 1, 0 } });

        // Act
        var result = _service.UpdateFifoCost(1, 1000m, 10);

        // Assert
        result.Should().BeTrue();
        product.CostFifo.Should().Be(1000m);
        _mockProductRepository.Verify(x => x.Update(product), Times.Once);
    }

    [Fact]
    public void UpdateFifoCost_ExistingStock_CalculatesWeightedAverage()
    {
        // Arrange - OldFifo=800, OldStock=10, NewCost=1000, Qty=5
        // Expected: ((800 × 10) + (1000 × 5)) / (10 + 5) = 13000 / 15 = 866.67
        var product = TestDataFactory.CreateProduct(id: 1, costFifo: 800m);
        _mockProductRepository.Setup(x => x.GetById(1)).Returns(product);
        _mockProductSerialRepository.Setup(x => x.GetInStockCountsByProductIds(It.IsAny<IEnumerable<int>>()))
            .Returns(new Dictionary<int, int> { { 1, 10 } });

        // Act
        var result = _service.UpdateFifoCost(1, 1000m, 5);

        // Assert
        result.Should().BeTrue();
        product.CostFifo.Should().BeApproximately(866.67m, 0.01m);
        _mockProductRepository.Verify(x => x.Update(product), Times.Once);
    }

    [Fact]
    public void UpdateFifoCost_ProductNotFound_ReturnsFalse()
    {
        // Arrange
        _mockProductRepository.Setup(x => x.GetById(999)).Returns((Product)null!);

        // Act
        var result = _service.UpdateFifoCost(999, 1000m, 10);

        // Assert
        result.Should().BeFalse();
        _mockProductRepository.Verify(x => x.Update(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public void UpdateFifoCost_InvalidNewCost_ReturnsFalse()
    {
        // Arrange - newCost <= 0 should return false
        var product = TestDataFactory.CreateProduct(id: 1);
        _mockProductRepository.Setup(x => x.GetById(1)).Returns(product);

        // Act
        var result = _service.UpdateFifoCost(1, 0m, 10);

        // Assert
        result.Should().BeFalse();
        _mockProductRepository.Verify(x => x.Update(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public void UpdateFifoCost_InvalidQuantity_ReturnsFalse()
    {
        // Arrange - quantity <= 0 should return false
        var product = TestDataFactory.CreateProduct(id: 1);
        _mockProductRepository.Setup(x => x.GetById(1)).Returns(product);

        // Act
        var result = _service.UpdateFifoCost(1, 1000m, 0);

        // Assert
        result.Should().BeFalse();
        _mockProductRepository.Verify(x => x.Update(It.IsAny<Product>()), Times.Never);
    }

    #endregion


    #region UpdateNifoCost Tests

    [Fact]
    public void UpdateNifoCost_MarketUp_AutoProtect_IncreasesPrice()
    {
        // Arrange - Variance > 0 (Market UP) with AUTO_PROTECT mode
        var product = TestDataFactory.CreateProduct(
            id: 1, 
            price: 1000m, 
            costFifo: 800m, 
            costNifo: 800m,
            pricingMode: PricingMode.AUTO_PROTECT);
        _mockProductRepository.Setup(x => x.GetById(1)).Returns(product);

        // Act - New NIFO = 900 (higher than FIFO 800, variance = +12.5%)
        var result = _service.UpdateNifoCost(1, 900m);

        // Assert
        result.Success.Should().BeTrue();
        result.Action.Should().Be("PRICE_INCREASED");
        // New price = MAX(800, 900) × 1.10 = 990
        product.Price.Should().Be(990m);
        product.MarketTrend.Should().Be(MarketTrend.UP);
        _mockProductRepository.Verify(x => x.Update(product), Times.Once);
    }

    [Fact]
    public void UpdateNifoCost_MarketStable_HoldsPrice()
    {
        // Arrange - Variance between -5% and 0% (STABLE)
        var product = TestDataFactory.CreateProduct(
            id: 1, 
            price: 1000m, 
            costFifo: 1000m, 
            costNifo: 1000m,
            pricingMode: PricingMode.AUTO_PROTECT);
        _mockProductRepository.Setup(x => x.GetById(1)).Returns(product);

        // Act - New NIFO = 970 (variance = -3%, within stable range)
        var result = _service.UpdateNifoCost(1, 970m);

        // Assert
        result.Success.Should().BeTrue();
        result.Action.Should().Be("PRICE_HELD");
        product.Price.Should().Be(1000m); // Price unchanged
        product.MarketTrend.Should().Be(MarketTrend.STABLE);
    }

    [Fact]
    public void UpdateNifoCost_MarketCrash_CreatesAlert()
    {
        // Arrange - Variance <= -10% (CRASH)
        var product = TestDataFactory.CreateProduct(
            id: 1, 
            price: 1000m, 
            costFifo: 1000m, 
            costNifo: 1000m,
            pricingMode: PricingMode.AUTO_PROTECT);
        _mockProductRepository.Setup(x => x.GetById(1)).Returns(product);
        _mockProductSerialRepository.Setup(x => x.GetInStockCountsByProductIds(It.IsAny<IEnumerable<int>>()))
            .Returns(new Dictionary<int, int> { { 1, 50 } });

        // Act - New NIFO = 850 (variance = -15%)
        var result = _service.UpdateNifoCost(1, 850m);

        // Assert
        result.Success.Should().BeTrue();
        result.Action.Should().Be("ALERT_CREATED");
        product.MarketTrend.Should().Be(MarketTrend.DOWN);
        _mockPricingAlertRepository.Verify(x => x.Insert(It.IsAny<PricingAlert>()), Times.Once);
    }

    [Fact]
    public void UpdateNifoCost_NewProduct_NoCostFifo_InitializesFifo()
    {
        // Arrange - Product with CostFifo = 0
        var product = TestDataFactory.CreateProduct(
            id: 1, 
            price: 1100m, 
            costFifo: 0m, 
            costNifo: 0m,
            pricingMode: PricingMode.AUTO_PROTECT);
        _mockProductRepository.Setup(x => x.GetById(1)).Returns(product);

        // Act
        var result = _service.UpdateNifoCost(1, 1000m);

        // Assert
        result.Success.Should().BeTrue();
        product.CostFifo.Should().Be(1000m); // Initialized to newNifoCost
    }

    [Fact]
    public void UpdateNifoCost_NewProduct_NoPrice_InitializesPrice()
    {
        // Arrange - Product with Price = 0
        var product = TestDataFactory.CreateProduct(
            id: 1, 
            price: 0m, 
            costFifo: 0m, 
            costNifo: 0m,
            pricingMode: PricingMode.AUTO_PROTECT);
        _mockProductRepository.Setup(x => x.GetById(1)).Returns(product);

        // Act
        var result = _service.UpdateNifoCost(1, 1000m);

        // Assert
        result.Success.Should().BeTrue();
        // Price = MAX(1000, 1000) × 1.10 = 1100
        product.Price.Should().Be(1100m);
    }

    [Fact]
    public void UpdateNifoCost_InvalidNifoCost_ReturnsFailure()
    {
        // Arrange - newNifoCost <= 0 should return failure
        var product = TestDataFactory.CreateProduct(id: 1);
        _mockProductRepository.Setup(x => x.GetById(1)).Returns(product);

        // Act
        var result = _service.UpdateNifoCost(1, 0m);

        // Assert
        result.Success.Should().BeFalse();
        _mockProductRepository.Verify(x => x.Update(It.IsAny<Product>()), Times.Never);
    }

    #endregion


    #region CalculateAutoProtectPrice Tests

    [Fact]
    public void CalculateAutoProtectPrice_FifoGreaterThanNifo_UsesFifo()
    {
        // Arrange - Fifo=1000, Nifo=900
        // Expected: 1000 × 1.10 = 1100

        // Act
        var result = _service.CalculateAutoProtectPrice(1000m, 900m);

        // Assert
        result.Should().Be(1100m);
    }

    [Fact]
    public void CalculateAutoProtectPrice_NifoGreaterThanFifo_UsesNifo()
    {
        // Arrange - Fifo=900, Nifo=1000
        // Expected: 1000 × 1.10 = 1100

        // Act
        var result = _service.CalculateAutoProtectPrice(900m, 1000m);

        // Assert
        result.Should().Be(1100m);
    }

    [Fact]
    public void CalculateAutoProtectPrice_RoundsToTwoDecimals()
    {
        // Arrange - Fifo=999, Nifo=900
        // Expected: 999 × 1.10 = 1098.90

        // Act
        var result = _service.CalculateAutoProtectPrice(999m, 900m);

        // Assert
        result.Should().Be(1098.90m);
    }

    #endregion

    #region CalculateClearancePrice Tests

    [Fact]
    public void CalculateClearancePrice_CalculatesWithMinimumMargin()
    {
        // Arrange - Nifo=1000
        // Expected: 1000 × 1.05 = 1050

        // Act
        var result = _service.CalculateClearancePrice(1000m);

        // Assert
        result.Should().Be(1050m);
    }

    [Fact]
    public void CalculateClearancePrice_RoundsToTwoDecimals()
    {
        // Arrange - Nifo=999
        // Expected: 999 × 1.05 = 1048.95

        // Act
        var result = _service.CalculateClearancePrice(999m);

        // Assert
        result.Should().Be(1048.95m);
    }

    #endregion

    #region CalculateVariance Tests

    [Fact]
    public void CalculateVariance_PositiveVariance_MarketUp()
    {
        // Arrange - Fifo=1000, Nifo=1100
        // Expected: (1100 - 1000) / 1000 = 0.10 (10%)

        // Act
        var result = _service.CalculateVariance(1000m, 1100m);

        // Assert
        result.Should().Be(0.10m);
    }

    [Fact]
    public void CalculateVariance_NegativeVariance_MarketDown()
    {
        // Arrange - Fifo=1000, Nifo=900
        // Expected: (900 - 1000) / 1000 = -0.10 (-10%)

        // Act
        var result = _service.CalculateVariance(1000m, 900m);

        // Assert
        result.Should().Be(-0.10m);
    }

    [Fact]
    public void CalculateVariance_ZeroVariance_Stable()
    {
        // Arrange - Fifo=1000, Nifo=1000
        // Expected: (1000 - 1000) / 1000 = 0

        // Act
        var result = _service.CalculateVariance(1000m, 1000m);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateVariance_FifoZero_ReturnsZero()
    {
        // Arrange - Fifo=0, Nifo=1000
        // Expected: 0 (avoid division by zero)

        // Act
        var result = _service.CalculateVariance(0m, 1000m);

        // Assert
        result.Should().Be(0m);
    }

    #endregion
}
