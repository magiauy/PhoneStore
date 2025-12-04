using FluentAssertions;
using Moq;
using PhoneStore.Services.Implementations;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreServices.Tests.TestHelpers;
using Xunit;

namespace PhoneStoreServices.Tests.Services;

public class EmployeeServiceTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly EmployeeService _employeeService;

    public EmployeeServiceTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _employeeService = new EmployeeService(_mockEmployeeRepository.Object);
    }

    #region GetEmployeeByIdAsync Tests

    [Fact]
    public async Task GetEmployeeByIdAsync_ValidId_ReturnsEmployee()
    {
        // Arrange
        var expectedEmployee = TestDataFactory.CreateEmployee(id: 1);
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(expectedEmployee);

        // Act
        var result = await _employeeService.GetEmployeeByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedEmployee);
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_EmployeeNotFound_ReturnsNull()
    {
        // Arrange
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Person?)null);

        // Act
        var result = await _employeeService.GetEmployeeByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }


    [Fact]
    public async Task GetEmployeeByIdAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _employeeService.GetEmployeeByIdAsync(1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetEmployeeByEmailAsync Tests

    [Fact]
    public async Task GetEmployeeByEmailAsync_ValidEmail_ReturnsEmployee()
    {
        // Arrange
        var expectedEmployee = TestDataFactory.CreateEmployee(email: "employee@test.com");
        _mockEmployeeRepository.Setup(x => x.GetByEmailAsync("employee@test.com"))
            .ReturnsAsync(expectedEmployee);

        // Act
        var result = await _employeeService.GetEmployeeByEmailAsync("employee@test.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("employee@test.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetEmployeeByEmailAsync_EmptyEmail_ReturnsNullWithoutCallingRepository(string? email)
    {
        // Act
        var result = await _employeeService.GetEmployeeByEmailAsync(email!);

        // Assert
        result.Should().BeNull();
        _mockEmployeeRepository.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetEmployeeByEmailAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        _mockEmployeeRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _employeeService.GetEmployeeByEmailAsync("employee@test.com");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetEmployeeByPhoneAsync Tests

    [Fact]
    public async Task GetEmployeeByPhoneAsync_ValidPhone_ReturnsEmployee()
    {
        // Arrange
        var expectedEmployee = TestDataFactory.CreateEmployee(phone: "0987654321");
        _mockEmployeeRepository.Setup(x => x.GetByPhoneAsync("0987654321"))
            .ReturnsAsync(expectedEmployee);

        // Act
        var result = await _employeeService.GetEmployeeByPhoneAsync("0987654321");

        // Assert
        result.Should().NotBeNull();
        result!.Phone.Should().Be("0987654321");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetEmployeeByPhoneAsync_EmptyPhone_ReturnsNullWithoutCallingRepository(string? phone)
    {
        // Act
        var result = await _employeeService.GetEmployeeByPhoneAsync(phone!);

        // Assert
        result.Should().BeNull();
        _mockEmployeeRepository.Verify(x => x.GetByPhoneAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetEmployeeByPhoneAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        _mockEmployeeRepository.Setup(x => x.GetByPhoneAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _employeeService.GetEmployeeByPhoneAsync("0987654321");

        // Assert
        result.Should().BeNull();
    }

    #endregion


    #region AddEmployeeAsync Tests

    [Fact]
    public async Task AddEmployeeAsync_ValidEmployee_ReturnsAddedEmployee()
    {
        // Arrange
        var newEmployee = TestDataFactory.CreateEmployee(id: 0, fullName: "New Employee");
        var addedEmployee = TestDataFactory.CreateEmployee(id: 5, fullName: "New Employee");
        _mockEmployeeRepository.Setup(x => x.AddAsync(It.IsAny<Employee>()))
            .ReturnsAsync(addedEmployee);

        // Act
        var result = await _employeeService.AddEmployeeAsync(newEmployee);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        _mockEmployeeRepository.Verify(x => x.AddAsync(It.IsAny<Employee>()), Times.Once);
    }

    [Fact]
    public async Task AddEmployeeAsync_ValidEmployee_SetsPersonTypeToEmployee()
    {
        // Arrange
        var newEmployee = TestDataFactory.CreateEmployee(id: 0);
        newEmployee.PersonType = PersonType.CUSTOMER; // Set wrong type initially
        Employee? capturedEmployee = null;
        _mockEmployeeRepository.Setup(x => x.AddAsync(It.IsAny<Employee>()))
            .Callback<Person>(e => capturedEmployee = e as Employee)
            .ReturnsAsync(newEmployee);

        // Act
        await _employeeService.AddEmployeeAsync(newEmployee);

        // Assert
        capturedEmployee.Should().NotBeNull();
        capturedEmployee!.PersonType.Should().Be(PersonType.EMPLOYEE);
    }

    [Fact]
    public async Task AddEmployeeAsync_NullEmployee_ReturnsNull()
    {
        // Act
        var result = await _employeeService.AddEmployeeAsync(null!);

        // Assert
        result.Should().BeNull();
        _mockEmployeeRepository.Verify(x => x.AddAsync(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task AddEmployeeAsync_RepositoryReturnsNull_ReturnsNull()
    {
        // Arrange
        var newEmployee = TestDataFactory.CreateEmployee(id: 0);
        _mockEmployeeRepository.Setup(x => x.AddAsync(It.IsAny<Employee>()))
            .ReturnsAsync((Person?)null);

        // Act
        var result = await _employeeService.AddEmployeeAsync(newEmployee);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddEmployeeAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        var newEmployee = TestDataFactory.CreateEmployee(id: 0);
        _mockEmployeeRepository.Setup(x => x.AddAsync(It.IsAny<Employee>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _employeeService.AddEmployeeAsync(newEmployee);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateEmployeeAsync Tests

    [Fact]
    public async Task UpdateEmployeeAsync_ValidEmployee_ReturnsTrue()
    {
        // Arrange
        var employee = TestDataFactory.CreateEmployee(id: 1);
        _mockEmployeeRepository.Setup(x => x.UpdateAsync(It.IsAny<Employee>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _employeeService.UpdateEmployeeAsync(employee);

        // Assert
        result.Should().BeTrue();
        _mockEmployeeRepository.Verify(x => x.UpdateAsync(employee), Times.Once);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_ValidEmployee_SetsPersonTypeToEmployee()
    {
        // Arrange
        var employee = TestDataFactory.CreateEmployee(id: 1);
        employee.PersonType = PersonType.CUSTOMER; // Set wrong type initially
        Employee? capturedEmployee = null;
        _mockEmployeeRepository.Setup(x => x.UpdateAsync(It.IsAny<Employee>()))
            .Callback<Person>(e => capturedEmployee = e as Employee)
            .Returns(Task.CompletedTask);

        // Act
        await _employeeService.UpdateEmployeeAsync(employee);

        // Assert
        capturedEmployee.Should().NotBeNull();
        capturedEmployee!.PersonType.Should().Be(PersonType.EMPLOYEE);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_NullEmployee_ReturnsFalse()
    {
        // Act
        var result = await _employeeService.UpdateEmployeeAsync(null!);

        // Assert
        result.Should().BeFalse();
        _mockEmployeeRepository.Verify(x => x.UpdateAsync(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_RepositoryThrowsException_ReturnsFalse()
    {
        // Arrange
        var employee = TestDataFactory.CreateEmployee(id: 1);
        _mockEmployeeRepository.Setup(x => x.UpdateAsync(It.IsAny<Employee>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _employeeService.UpdateEmployeeAsync(employee);

        // Assert
        result.Should().BeFalse();
    }

    #endregion


    #region GetEmployeesFiltered Tests

    [Fact]
    public void GetEmployeesFiltered_WithSearchTerm_FiltersCorrectly()
    {
        // Arrange
        var employees = TestDataFactory.CreateEmployeeList(5);
        _mockEmployeeRepository.Setup(x => x.GetAllEmployeesAsync())
            .ReturnsAsync(employees);

        // Act
        var result = _employeeService.GetEmployeesFiltered("Employee 1", 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Employees.Should().HaveCount(1);
        result.Employees.First().FullName.Should().Be("Employee 1");
    }

    [Fact]
    public void GetEmployeesFiltered_WithStatusFilterActive_ReturnsOnlyActiveEmployees()
    {
        // Arrange
        var employees = TestDataFactory.CreateEmployeeList(6); // 4 active, 2 inactive (every 3rd is inactive)
        _mockEmployeeRepository.Setup(x => x.GetAllEmployeesAsync())
            .ReturnsAsync(employees);
        var filterCriteria = new EmployeeFilterCriteria { Status = "Active" };

        // Act
        var result = _employeeService.GetEmployeesFiltered(null, 1, 10, filterCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Employees.All(e => e.IsActive).Should().BeTrue();
    }

    [Fact]
    public void GetEmployeesFiltered_WithStatusFilterInactive_ReturnsOnlyInactiveEmployees()
    {
        // Arrange
        var employees = TestDataFactory.CreateEmployeeList(6);
        _mockEmployeeRepository.Setup(x => x.GetAllEmployeesAsync())
            .ReturnsAsync(employees);
        var filterCriteria = new EmployeeFilterCriteria { Status = "Inactive" };

        // Act
        var result = _employeeService.GetEmployeesFiltered(null, 1, 10, filterCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Employees.All(e => !e.IsActive).Should().BeTrue();
    }

    [Fact]
    public void GetEmployeesFiltered_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        var employees = TestDataFactory.CreateEmployeeList(5);
        _mockEmployeeRepository.Setup(x => x.GetAllEmployeesAsync())
            .ReturnsAsync(employees);
        var filterCriteria = new EmployeeFilterCriteria
        {
            HireDateFrom = DateTime.UtcNow.AddMonths(-3),
            HireDateTo = DateTime.UtcNow
        };

        // Act
        var result = _employeeService.GetEmployeesFiltered(null, 1, 10, filterCriteria);

        // Assert
        result.Should().NotBeNull();
        result.Employees.Should().NotBeEmpty();
    }

    [Fact]
    public void GetEmployeesFiltered_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var employees = TestDataFactory.CreateEmployeeList(15);
        _mockEmployeeRepository.Setup(x => x.GetAllEmployeesAsync())
            .ReturnsAsync(employees);

        // Act
        var result = _employeeService.GetEmployeesFiltered(null, 2, 5);

        // Assert
        result.Should().NotBeNull();
        result.Employees.Should().HaveCount(5);
        result.Info.TotalRecords.Should().Be(15);
        result.Info.TotalPages.Should().Be(3);
    }

    [Fact]
    public void GetEmployeesFiltered_RepositoryThrowsException_ReturnsEmptyResult()
    {
        // Arrange
        _mockEmployeeRepository.Setup(x => x.GetAllEmployeesAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = _employeeService.GetEmployeesFiltered("test", 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Employees.Should().BeEmpty();
        result.Info.TotalRecords.Should().Be(0);
    }

    [Fact]
    public void GetEmployeesFiltered_SearchByEmail_FiltersCorrectly()
    {
        // Arrange
        var employees = TestDataFactory.CreateEmployeeList(5);
        _mockEmployeeRepository.Setup(x => x.GetAllEmployeesAsync())
            .ReturnsAsync(employees);

        // Act
        var result = _employeeService.GetEmployeesFiltered("employee2@test.com", 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Employees.Should().HaveCount(1);
        result.Employees.First().Email.Should().Be("employee2@test.com");
    }

    [Fact]
    public void GetEmployeesFiltered_SearchByPhone_FiltersCorrectly()
    {
        // Arrange
        var employees = TestDataFactory.CreateEmployeeList(5);
        _mockEmployeeRepository.Setup(x => x.GetAllEmployeesAsync())
            .ReturnsAsync(employees);

        // Act
        var result = _employeeService.GetEmployeesFiltered("0987654323", 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Employees.Should().HaveCount(1);
    }

    [Fact]
    public void GetEmployeesFiltered_SearchByCode_FiltersCorrectly()
    {
        // Arrange
        var employees = TestDataFactory.CreateEmployeeList(5);
        _mockEmployeeRepository.Setup(x => x.GetAllEmployeesAsync())
            .ReturnsAsync(employees);

        // Act
        var result = _employeeService.GetEmployeesFiltered("EMP00001", 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Employees.Should().HaveCount(1);
    }

    #endregion


    #region IsEmployeeActiveAsync Tests

    [Fact]
    public async Task IsEmployeeActiveAsync_ActiveEmployee_ReturnsTrue()
    {
        // Arrange
        var activeEmployee = TestDataFactory.CreateEmployee(id: 1, isActive: true);
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(activeEmployee);

        // Act
        var result = await _employeeService.IsEmployeeActiveAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmployeeActiveAsync_InactiveEmployee_ReturnsFalse()
    {
        // Arrange
        var inactiveEmployee = TestDataFactory.CreateEmployee(id: 1, isActive: false);
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(inactiveEmployee);

        // Act
        var result = await _employeeService.IsEmployeeActiveAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEmployeeActiveAsync_EmployeeNotFound_ReturnsFalse()
    {
        // Arrange
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Person?)null);

        // Act
        var result = await _employeeService.IsEmployeeActiveAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEmployeeActiveAsync_RepositoryThrowsException_ReturnsFalse()
    {
        // Arrange
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _employeeService.IsEmployeeActiveAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
