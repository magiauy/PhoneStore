using FluentAssertions;
using Moq;
using PhoneStore.Services.Implementations;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreServices.Tests.TestHelpers;
using Xunit;

namespace PhoneStoreServices.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly AccountService _accountService;

    public AccountServiceTests()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _accountService = new AccountService(_mockAccountRepository.Object);
    }

    #region GetAccountByIdAsync Tests

    [Fact]
    public async Task GetAccountByIdAsync_ValidId_ReturnsAccount()
    {
        // Arrange
        var expectedAccount = TestDataFactory.CreateAccount(id: 1);
        _mockAccountRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(expectedAccount);

        // Act
        var result = await _accountService.GetAccountByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedAccount);
    }

    [Fact]
    public async Task GetAccountByIdAsync_AccountNotFound_ReturnsNull()
    {
        // Arrange
        _mockAccountRepository.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _accountService.GetAccountByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }


    [Fact]
    public async Task GetAccountByIdAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        _mockAccountRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _accountService.GetAccountByIdAsync(1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAccountByUsernameAsync Tests

    [Fact]
    public async Task GetAccountByUsernameAsync_ValidUsername_ReturnsAccount()
    {
        // Arrange
        var expectedAccount = TestDataFactory.CreateAccount(username: "testuser");
        _mockAccountRepository.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(expectedAccount);

        // Act
        var result = await _accountService.GetAccountByUsernameAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetAccountByUsernameAsync_EmptyUsername_ReturnsNullWithoutCallingRepository(string? username)
    {
        // Act
        var result = await _accountService.GetAccountByUsernameAsync(username!);

        // Assert
        result.Should().BeNull();
        _mockAccountRepository.Verify(x => x.GetByUsernameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAccountByUsernameAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        _mockAccountRepository.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _accountService.GetAccountByUsernameAsync("testuser");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddAccountAsync Tests

    [Fact]
    public async Task AddAccountAsync_ValidAccount_ReturnsCreatedId()
    {
        // Arrange
        var newAccount = TestDataFactory.CreateAccount(id: 0, username: "newuser");
        var createdAccount = TestDataFactory.CreateAccount(id: 5, username: "newuser");
        _mockAccountRepository.Setup(x => x.AddAsync(It.IsAny<Account>()))
            .ReturnsAsync(createdAccount);

        // Act
        var result = await _accountService.AddAccountAsync(newAccount);

        // Assert
        result.Should().Be(5);
        _mockAccountRepository.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Once);
    }

    [Fact]
    public async Task AddAccountAsync_RepositoryReturnsNull_ReturnsZero()
    {
        // Arrange
        var newAccount = TestDataFactory.CreateAccount(id: 0, username: "newuser");
        _mockAccountRepository.Setup(x => x.AddAsync(It.IsAny<Account>()))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _accountService.AddAccountAsync(newAccount);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AddAccountAsync_RepositoryThrowsException_ReturnsZero()
    {
        // Arrange
        var newAccount = TestDataFactory.CreateAccount(id: 0, username: "newuser");
        _mockAccountRepository.Setup(x => x.AddAsync(It.IsAny<Account>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _accountService.AddAccountAsync(newAccount);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AddAccountAsync_AccountWithPersonId_PassesCorrectDataToRepository()
    {
        // Arrange - Simulates UI flow where account is created for an employee
        var newAccount = TestDataFactory.CreateAccount(id: 0, username: "employee_account", personId: 10);
        var createdAccount = TestDataFactory.CreateAccount(id: 7, username: "employee_account", personId: 10);
        Account? capturedAccount = null;
        
        _mockAccountRepository.Setup(x => x.AddAsync(It.IsAny<Account>()))
            .Callback<Account>(a => capturedAccount = a)
            .ReturnsAsync(createdAccount);

        // Act
        var result = await _accountService.AddAccountAsync(newAccount);

        // Assert
        result.Should().Be(7);
        capturedAccount.Should().NotBeNull();
        capturedAccount!.Username.Should().Be("employee_account");
        capturedAccount.PersonId.Should().Be(10);
    }

    [Fact]
    public async Task AddAccountAsync_AccountWithIsActiveTrue_PassesCorrectStatusToRepository()
    {
        // Arrange - Simulates UI checkbox "Activate account" checked
        var newAccount = TestDataFactory.CreateAccount(id: 0, username: "activeuser", isActive: true);
        var createdAccount = TestDataFactory.CreateAccount(id: 8, username: "activeuser", isActive: true);
        Account? capturedAccount = null;
        
        _mockAccountRepository.Setup(x => x.AddAsync(It.IsAny<Account>()))
            .Callback<Account>(a => capturedAccount = a)
            .ReturnsAsync(createdAccount);

        // Act
        var result = await _accountService.AddAccountAsync(newAccount);

        // Assert
        result.Should().Be(8);
        capturedAccount.Should().NotBeNull();
        capturedAccount!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddAccountAsync_AccountWithIsActiveFalse_PassesCorrectStatusToRepository()
    {
        // Arrange - Simulates UI checkbox "Activate account" unchecked
        var newAccount = TestDataFactory.CreateAccount(id: 0, username: "inactiveuser", isActive: false);
        var createdAccount = TestDataFactory.CreateAccount(id: 9, username: "inactiveuser", isActive: false);
        Account? capturedAccount = null;
        
        _mockAccountRepository.Setup(x => x.AddAsync(It.IsAny<Account>()))
            .Callback<Account>(a => capturedAccount = a)
            .ReturnsAsync(createdAccount);

        // Act
        var result = await _accountService.AddAccountAsync(newAccount);

        // Assert
        result.Should().Be(9);
        capturedAccount.Should().NotBeNull();
        capturedAccount!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task AddAccountAsync_RepositoryReturnsAccountWithZeroId_ReturnsZero()
    {
        // Arrange - Edge case where repository returns account but with Id = 0
        var newAccount = TestDataFactory.CreateAccount(id: 0, username: "newuser");
        var createdAccount = TestDataFactory.CreateAccount(id: 0, username: "newuser");
        _mockAccountRepository.Setup(x => x.AddAsync(It.IsAny<Account>()))
            .ReturnsAsync(createdAccount);

        // Act
        var result = await _accountService.AddAccountAsync(newAccount);

        // Assert
        result.Should().Be(0);
    }

    #endregion


    #region UpdateAccountAsync Tests

    [Fact]
    public async Task UpdateAccountAsync_ValidAccount_ReturnsTrue()
    {
        // Arrange
        var account = TestDataFactory.CreateAccount(id: 1);
        _mockAccountRepository.Setup(x => x.UpdateAsync(It.IsAny<Account>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _accountService.UpdateAccountAsync(account);

        // Assert
        result.Should().BeTrue();
        _mockAccountRepository.Verify(x => x.UpdateAsync(account), Times.Once);
    }

    [Fact]
    public async Task UpdateAccountAsync_NullAccount_ReturnsFalse()
    {
        // Act
        var result = await _accountService.UpdateAccountAsync(null!);

        // Assert
        result.Should().BeFalse();
        _mockAccountRepository.Verify(x => x.UpdateAsync(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAccountAsync_RepositoryThrowsException_ReturnsFalse()
    {
        // Arrange
        var account = TestDataFactory.CreateAccount(id: 1);
        _mockAccountRepository.Setup(x => x.UpdateAsync(It.IsAny<Account>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _accountService.UpdateAccountAsync(account);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region UpdateAccountRolesAsync Tests

    [Fact]
    public async Task UpdateAccountRolesAsync_ValidRoles_ReturnsTrue()
    {
        // Arrange
        var roleIds = new List<int> { 1, 2, 3 };
        _mockAccountRepository.Setup(x => x.UpdateRolesAsync(1, It.IsAny<IEnumerable<int>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _accountService.UpdateAccountRolesAsync(1, roleIds);

        // Assert
        result.Should().BeTrue();
        _mockAccountRepository.Verify(x => x.UpdateRolesAsync(1, It.IsAny<IEnumerable<int>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAccountRolesAsync_DuplicateRoles_NormalizesRoleIds()
    {
        // Arrange
        var roleIdsWithDuplicates = new List<int> { 1, 2, 2, 3, 3, 3 };
        List<int>? capturedRoleIds = null;
        _mockAccountRepository.Setup(x => x.UpdateRolesAsync(1, It.IsAny<IEnumerable<int>>()))
            .Callback<int, IEnumerable<int>>((id, roles) => capturedRoleIds = roles.ToList())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _accountService.UpdateAccountRolesAsync(1, roleIdsWithDuplicates);

        // Assert
        result.Should().BeTrue();
        capturedRoleIds.Should().NotBeNull();
        capturedRoleIds.Should().HaveCount(3);
        capturedRoleIds.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task UpdateAccountRolesAsync_NullRoleIds_PassesEmptyList()
    {
        // Arrange
        List<int>? capturedRoleIds = null;
        _mockAccountRepository.Setup(x => x.UpdateRolesAsync(1, It.IsAny<IEnumerable<int>>()))
            .Callback<int, IEnumerable<int>>((id, roles) => capturedRoleIds = roles.ToList())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _accountService.UpdateAccountRolesAsync(1, null!);

        // Assert
        result.Should().BeTrue();
        capturedRoleIds.Should().NotBeNull();
        capturedRoleIds.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAccountRolesAsync_RepositoryThrowsException_ReturnsFalse()
    {
        // Arrange
        var roleIds = new List<int> { 1, 2, 3 };
        _mockAccountRepository.Setup(x => x.UpdateRolesAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _accountService.UpdateAccountRolesAsync(1, roleIds);

        // Assert
        result.Should().BeFalse();
    }

    #endregion


    #region AddAccountWithRoles Integration Tests

    [Fact]
    public async Task AddAccountAndAssignRoles_ValidAccountAndRoles_BothOperationsSucceed()
    {
        // Arrange - Simulates complete UI flow: create account then assign roles
        var newAccount = TestDataFactory.CreateAccount(id: 0, username: "newemployee", personId: 15);
        var createdAccount = TestDataFactory.CreateAccount(id: 10, username: "newemployee", personId: 15);
        var roleIds = new List<int> { 1, 2 }; // Selected roles from UI

        _mockAccountRepository.Setup(x => x.AddAsync(It.IsAny<Account>()))
            .ReturnsAsync(createdAccount);
        _mockAccountRepository.Setup(x => x.UpdateRolesAsync(10, It.IsAny<IEnumerable<int>>()))
            .Returns(Task.CompletedTask);

        // Act - Step 1: Create account
        var accountId = await _accountService.AddAccountAsync(newAccount);
        
        // Act - Step 2: Assign roles (only if account creation succeeded)
        var rolesAssigned = false;
        if (accountId > 0)
        {
            rolesAssigned = await _accountService.UpdateAccountRolesAsync(accountId, roleIds);
        }

        // Assert
        accountId.Should().Be(10);
        rolesAssigned.Should().BeTrue();
        _mockAccountRepository.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Once);
        _mockAccountRepository.Verify(x => x.UpdateRolesAsync(10, It.IsAny<IEnumerable<int>>()), Times.Once);
    }

    [Fact]
    public async Task AddAccountAndAssignRoles_AccountCreationFails_RolesNotAssigned()
    {
        // Arrange - Account creation fails, roles should not be assigned
        var newAccount = TestDataFactory.CreateAccount(id: 0, username: "faileduser");
        var roleIds = new List<int> { 1, 2 };

        _mockAccountRepository.Setup(x => x.AddAsync(It.IsAny<Account>()))
            .ReturnsAsync((Account?)null);

        // Act - Step 1: Create account (fails)
        var accountId = await _accountService.AddAccountAsync(newAccount);
        
        // Act - Step 2: Should not assign roles since account creation failed
        var rolesAssigned = false;
        if (accountId > 0)
        {
            rolesAssigned = await _accountService.UpdateAccountRolesAsync(accountId, roleIds);
        }

        // Assert
        accountId.Should().Be(0);
        rolesAssigned.Should().BeFalse();
        _mockAccountRepository.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Once);
        _mockAccountRepository.Verify(x => x.UpdateRolesAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>()), Times.Never);
    }

    #endregion

    #region IsAccountActiveAsync Tests

    [Fact]
    public async Task IsAccountActiveAsync_ActiveAccount_ReturnsTrue()
    {
        // Arrange
        _mockAccountRepository.Setup(x => x.IsActiveAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _accountService.IsAccountActiveAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAccountActiveAsync_InactiveAccount_ReturnsFalse()
    {
        // Arrange
        _mockAccountRepository.Setup(x => x.IsActiveAsync(1))
            .ReturnsAsync(false);

        // Act
        var result = await _accountService.IsAccountActiveAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAccountActiveAsync_RepositoryThrowsException_ReturnsFalse()
    {
        // Arrange
        _mockAccountRepository.Setup(x => x.IsActiveAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _accountService.IsAccountActiveAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
