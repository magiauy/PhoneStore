using FluentAssertions;
using Moq;
using PhoneStore.Services.Implementations;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreServices.Tests.TestHelpers;
using Xunit;

namespace PhoneStoreServices.Tests.Services;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly CustomerService _customerService;

    public CustomerServiceTests()
    {
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _customerService = new CustomerService(_mockCustomerRepository.Object);
    }

    #region GetCustomerByIdAsync Tests

    [Fact]
    public async Task GetCustomerByIdAsync_ValidId_ReturnsCustomer()
    {
        // Arrange
        var expectedCustomer = TestDataFactory.CreateCustomer(id: 1);
        _mockCustomerRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(expectedCustomer);

        // Act
        var result = await _customerService.GetCustomerByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedCustomer);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_CustomerNotFound_ReturnsNull()
    {
        // Arrange
        _mockCustomerRepository.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Person?)null);

        // Act
        var result = await _customerService.GetCustomerByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }


    [Fact]
    public async Task GetCustomerByIdAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        _mockCustomerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _customerService.GetCustomerByIdAsync(1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetCustomerByEmailAsync Tests

    [Fact]
    public async Task GetCustomerByEmailAsync_ValidEmail_ReturnsCustomer()
    {
        // Arrange
        var expectedCustomer = TestDataFactory.CreateCustomer(email: "customer@test.com");
        _mockCustomerRepository.Setup(x => x.GetByEmailAsync("customer@test.com"))
            .ReturnsAsync(expectedCustomer);

        // Act
        var result = await _customerService.GetCustomerByEmailAsync("customer@test.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("customer@test.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetCustomerByEmailAsync_EmptyEmail_ReturnsNullWithoutCallingRepository(string? email)
    {
        // Act
        var result = await _customerService.GetCustomerByEmailAsync(email!);

        // Assert
        result.Should().BeNull();
        _mockCustomerRepository.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _customerService.GetCustomerByEmailAsync("customer@test.com");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetCustomerByPhoneAsync Tests

    [Fact]
    public async Task GetCustomerByPhoneAsync_ValidPhone_ReturnsCustomer()
    {
        // Arrange
        var expectedCustomer = TestDataFactory.CreateCustomer(phone: "0REMOVED_SECRET789");
        _mockCustomerRepository.Setup(x => x.GetByPhoneAsync("0REMOVED_SECRET789"))
            .ReturnsAsync(expectedCustomer);

        // Act
        var result = await _customerService.GetCustomerByPhoneAsync("0REMOVED_SECRET789");

        // Assert
        result.Should().NotBeNull();
        result!.Phone.Should().Be("0REMOVED_SECRET789");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetCustomerByPhoneAsync_EmptyPhone_ReturnsNullWithoutCallingRepository(string? phone)
    {
        // Act
        var result = await _customerService.GetCustomerByPhoneAsync(phone!);

        // Assert
        result.Should().BeNull();
        _mockCustomerRepository.Verify(x => x.GetByPhoneAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetCustomerByPhoneAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        _mockCustomerRepository.Setup(x => x.GetByPhoneAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _customerService.GetCustomerByPhoneAsync("0REMOVED_SECRET789");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetCustomerByAddressAsync Tests

    [Fact]
    public async Task GetCustomerByAddressAsync_ValidAddress_ReturnsCustomer()
    {
        // Arrange
        var expectedCustomer = TestDataFactory.CreateCustomer(address: "123 Test Street");
        _mockCustomerRepository.Setup(x => x.GetByAddressAsync("123 Test Street"))
            .ReturnsAsync(expectedCustomer);

        // Act
        var result = await _customerService.GetCustomerByAddressAsync("123 Test Street");

        // Assert
        result.Should().NotBeNull();
        result!.Address.Should().Be("123 Test Street");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetCustomerByAddressAsync_EmptyAddress_ReturnsNullWithoutCallingRepository(string? address)
    {
        // Act
        var result = await _customerService.GetCustomerByAddressAsync(address!);

        // Assert
        result.Should().BeNull();
        _mockCustomerRepository.Verify(x => x.GetByAddressAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetCustomerByAddressAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        _mockCustomerRepository.Setup(x => x.GetByAddressAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _customerService.GetCustomerByAddressAsync("123 Test Street");

        // Assert
        result.Should().BeNull();
    }

    #endregion


    #region AddCustomerAsync Tests

    [Fact]
    public async Task AddCustomerAsync_ValidCustomer_ReturnsAddedCustomer()
    {
        // Arrange
        var newCustomer = TestDataFactory.CreateCustomer(id: 0, fullName: "New Customer");
        var addedCustomer = TestDataFactory.CreateCustomer(id: 5, fullName: "New Customer");
        _mockCustomerRepository.Setup(x => x.AddAsync(It.IsAny<Customer>()))
            .ReturnsAsync(addedCustomer);

        // Act
        var result = await _customerService.AddCustomerAsync(newCustomer);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        _mockCustomerRepository.Verify(x => x.AddAsync(It.IsAny<Customer>()), Times.Once);
    }

    [Fact]
    public async Task AddCustomerAsync_ValidCustomer_SetsPersonTypeToCustomer()
    {
        // Arrange
        var newCustomer = TestDataFactory.CreateCustomer(id: 0);
        newCustomer.PersonType = PersonType.EMPLOYEE; // Set wrong type initially
        Customer? capturedCustomer = null;
        _mockCustomerRepository.Setup(x => x.AddAsync(It.IsAny<Customer>()))
            .Callback<Person>(c => capturedCustomer = c as Customer)
            .ReturnsAsync(newCustomer);

        // Act
        await _customerService.AddCustomerAsync(newCustomer);

        // Assert
        capturedCustomer.Should().NotBeNull();
        capturedCustomer!.PersonType.Should().Be(PersonType.CUSTOMER);
    }

    [Fact]
    public async Task AddCustomerAsync_NullCustomer_ReturnsNull()
    {
        // Act
        var result = await _customerService.AddCustomerAsync(null!);

        // Assert
        result.Should().BeNull();
        _mockCustomerRepository.Verify(x => x.AddAsync(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task AddCustomerAsync_RepositoryReturnsNull_ReturnsNull()
    {
        // Arrange
        var newCustomer = TestDataFactory.CreateCustomer(id: 0);
        _mockCustomerRepository.Setup(x => x.AddAsync(It.IsAny<Customer>()))
            .ReturnsAsync((Person?)null);

        // Act
        var result = await _customerService.AddCustomerAsync(newCustomer);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddCustomerAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        var newCustomer = TestDataFactory.CreateCustomer(id: 0);
        _mockCustomerRepository.Setup(x => x.AddAsync(It.IsAny<Customer>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _customerService.AddCustomerAsync(newCustomer);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateCustomerAsync Tests

    [Fact]
    public async Task UpdateCustomerAsync_ValidCustomer_ReturnsTrue()
    {
        // Arrange
        var customer = TestDataFactory.CreateCustomer(id: 1);
        _mockCustomerRepository.Setup(x => x.UpdateAsync(It.IsAny<Customer>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _customerService.UpdateCustomerAsync(customer);

        // Assert
        result.Should().BeTrue();
        _mockCustomerRepository.Verify(x => x.UpdateAsync(customer), Times.Once);
    }

    [Fact]
    public async Task UpdateCustomerAsync_ValidCustomer_SetsPersonTypeToCustomer()
    {
        // Arrange
        var customer = TestDataFactory.CreateCustomer(id: 1);
        customer.PersonType = PersonType.EMPLOYEE; // Set wrong type initially
        Customer? capturedCustomer = null;
        _mockCustomerRepository.Setup(x => x.UpdateAsync(It.IsAny<Customer>()))
            .Callback<Person>(c => capturedCustomer = c as Customer)
            .Returns(Task.CompletedTask);

        // Act
        await _customerService.UpdateCustomerAsync(customer);

        // Assert
        capturedCustomer.Should().NotBeNull();
        capturedCustomer!.PersonType.Should().Be(PersonType.CUSTOMER);
    }

    [Fact]
    public async Task UpdateCustomerAsync_NullCustomer_ReturnsFalse()
    {
        // Act
        var result = await _customerService.UpdateCustomerAsync(null!);

        // Assert
        result.Should().BeFalse();
        _mockCustomerRepository.Verify(x => x.UpdateAsync(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCustomerAsync_RepositoryThrowsException_ReturnsFalse()
    {
        // Arrange
        var customer = TestDataFactory.CreateCustomer(id: 1);
        _mockCustomerRepository.Setup(x => x.UpdateAsync(It.IsAny<Customer>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _customerService.UpdateCustomerAsync(customer);

        // Assert
        result.Should().BeFalse();
    }

    #endregion


    #region DeleteCustomerAsync Tests

    [Fact]
    public async Task DeleteCustomerAsync_ValidId_ReturnsTrue()
    {
        // Arrange
        _mockCustomerRepository.Setup(x => x.DeleteAsync(1))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _customerService.DeleteCustomerAsync(1);

        // Assert
        result.Should().BeTrue();
        _mockCustomerRepository.Verify(x => x.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteCustomerAsync_RepositoryThrowsException_ReturnsFalse()
    {
        // Arrange
        _mockCustomerRepository.Setup(x => x.DeleteAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _customerService.DeleteCustomerAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetCustomersFilteredAsync Tests

    [Fact]
    public async Task GetCustomersFilteredAsync_WithSearchTerm_ReturnsFilteredCustomers()
    {
        // Arrange
        var customers = TestDataFactory.CreateCustomerList(5);
        _mockCustomerRepository.Setup(x => x.GetCustomersFilteredAsync(
                "Customer", null, 1, 10))
            .ReturnsAsync((customers, 5));

        // Act
        var result = await _customerService.GetCustomersFilteredAsync("Customer", null, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Customers.Should().HaveCount(5);
        result.Info.TotalRecords.Should().Be(5);
    }

    [Fact]
    public async Task GetCustomersFilteredAsync_WithFilterCriteria_ReturnsFilteredCustomers()
    {
        // Arrange
        var activeCustomers = TestDataFactory.CreateCustomerList(3)
            .Where(c => c.IsActive).ToList();
        var filterCriteria = new CustomerFilterCriteria { Status = "Active" };
        _mockCustomerRepository.Setup(x => x.GetCustomersFilteredAsync(
                null, It.IsAny<CustomerFilterCriteria>(), 1, 10))
            .ReturnsAsync((activeCustomers, activeCustomers.Count));

        // Act
        var result = await _customerService.GetCustomersFilteredAsync(null, filterCriteria, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Customers.Should().HaveCount(activeCustomers.Count);
    }

    [Fact]
    public async Task GetCustomersFilteredAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var customers = TestDataFactory.CreateCustomerList(5);
        _mockCustomerRepository.Setup(x => x.GetCustomersFilteredAsync(
                null, null, 2, 5))
            .ReturnsAsync((customers, 15));

        // Act
        var result = await _customerService.GetCustomersFilteredAsync(null, null, 2, 5);

        // Assert
        result.Should().NotBeNull();
        result.Info.TotalRecords.Should().Be(15);
        result.Info.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetCustomersFilteredAsync_RepositoryThrowsException_ReturnsEmptyResult()
    {
        // Arrange
        _mockCustomerRepository.Setup(x => x.GetCustomersFilteredAsync(
                It.IsAny<string?>(), It.IsAny<CustomerFilterCriteria?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _customerService.GetCustomersFilteredAsync("test", null, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Customers.Should().BeEmpty();
        result.Info.TotalRecords.Should().Be(0);
    }

    #endregion

    #region IsCustomerActiveAsync Tests

    [Fact]
    public async Task IsCustomerActiveAsync_ActiveCustomer_ReturnsTrue()
    {
        // Arrange
        var activeCustomer = TestDataFactory.CreateCustomer(id: 1, isActive: true);
        _mockCustomerRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(activeCustomer);

        // Act
        var result = await _customerService.IsCustomerActiveAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsCustomerActiveAsync_InactiveCustomer_ReturnsFalse()
    {
        // Arrange
        var inactiveCustomer = TestDataFactory.CreateCustomer(id: 1, isActive: false);
        _mockCustomerRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(inactiveCustomer);

        // Act
        var result = await _customerService.IsCustomerActiveAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsCustomerActiveAsync_CustomerNotFound_ReturnsFalse()
    {
        // Arrange
        _mockCustomerRepository.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Person?)null);

        // Act
        var result = await _customerService.IsCustomerActiveAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsCustomerActiveAsync_RepositoryThrowsException_ReturnsFalse()
    {
        // Arrange
        _mockCustomerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _customerService.IsCustomerActiveAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
