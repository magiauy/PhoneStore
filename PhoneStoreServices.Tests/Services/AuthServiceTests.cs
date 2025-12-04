using FluentAssertions;
using Moq;
using PhoneStore.Services.Implementations;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreServices.Tests.TestHelpers;
using Xunit;

namespace PhoneStoreServices.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _mockAuthRepository;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockAuthRepository = new Mock<IAuthRepository>();
        _authService = new AuthService(_mockAuthRepository.Object);
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAccount()
    {
        // Arrange
        var expectedAccount = TestDataFactory.CreateAccount();
        _mockAuthRepository.Setup(x => x.AuthenticateAsync("testuser", "password123"))
            .ReturnsAsync(expectedAccount);
        _mockAuthRepository.Setup(x => x.UpdateLastLoginAsync(expectedAccount.Id))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync("testuser", "password123");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedAccount);
        _mockAuthRepository.Verify(x => x.UpdateLastLoginAsync(expectedAccount.Id), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsNull()
    {
        // Arrange
        _mockAuthRepository.Setup(x => x.AuthenticateAsync("testuser", "wrongpassword"))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _authService.LoginAsync("testuser", "wrongpassword");

        // Assert
        result.Should().BeNull();
        _mockAuthRepository.Verify(x => x.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
    }


    [Theory]
    [InlineData(null, "password")]
    [InlineData("", "password")]
    [InlineData("  ", "password")]
    public async Task LoginAsync_EmptyUsername_ReturnsNullWithoutCallingRepository(string? username, string password)
    {
        // Act
        var result = await _authService.LoginAsync(username!, password);

        // Assert
        result.Should().BeNull();
        _mockAuthRepository.Verify(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("testuser", null)]
    [InlineData("testuser", "")]
    [InlineData("testuser", "  ")]
    public async Task LoginAsync_EmptyPassword_ReturnsNullWithoutCallingRepository(string username, string? password)
    {
        // Act
        var result = await _authService.LoginAsync(username, password!);

        // Assert
        result.Should().BeNull();
        _mockAuthRepository.Verify(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        _mockAuthRepository.Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _authService.LoginAsync("testuser", "password123");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ValidateCredentialsAsync Tests

    [Fact]
    public async Task ValidateCredentialsAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        _mockAuthRepository.Setup(x => x.ValidateCredentialsAsync("testuser", "password123"))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.ValidateCredentialsAsync("testuser", "password123");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_InvalidCredentials_ReturnsFalse()
    {
        // Arrange
        _mockAuthRepository.Setup(x => x.ValidateCredentialsAsync("testuser", "wrongpassword"))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.ValidateCredentialsAsync("testuser", "wrongpassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_RepositoryThrowsException_ReturnsFalse()
    {
        // Arrange
        _mockAuthRepository.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _authService.ValidateCredentialsAsync("testuser", "password123");

        // Assert
        result.Should().BeFalse();
    }

    #endregion


    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_ValidCurrentPassword_ReturnsUpdatedAccount()
    {
        // Arrange
        var account = TestDataFactory.CreateAccount();
        var updatedAccount = TestDataFactory.CreateAccount(passwordHash: "newhash");
        
        _mockAuthRepository.Setup(x => x.ValidateCredentialsAsync("testuser", "currentpass"))
            .ReturnsAsync(true);
        _mockAuthRepository.Setup(x => x.AuthenticateAsync("testuser", "currentpass"))
            .ReturnsAsync(account);
        _mockAuthRepository.Setup(x => x.ChangePasswordAsync(account.Id, "newpassword"))
            .ReturnsAsync(updatedAccount);

        // Act
        var result = await _authService.ChangePasswordAsync("testuser", "currentpass", "newpassword");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(updatedAccount);
    }

    [Fact]
    public async Task ChangePasswordAsync_InvalidCurrentPassword_ReturnsNull()
    {
        // Arrange
        _mockAuthRepository.Setup(x => x.ValidateCredentialsAsync("testuser", "wrongpass"))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.ChangePasswordAsync("testuser", "wrongpass", "newpassword");

        // Assert
        result.Should().BeNull();
        _mockAuthRepository.Verify(x => x.ChangePasswordAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "currentpass", "newpass")]
    [InlineData("", "currentpass", "newpass")]
    [InlineData("  ", "currentpass", "newpass")]
    [InlineData("testuser", null, "newpass")]
    [InlineData("testuser", "", "newpass")]
    [InlineData("testuser", "  ", "newpass")]
    [InlineData("testuser", "currentpass", null)]
    [InlineData("testuser", "currentpass", "")]
    [InlineData("testuser", "currentpass", "  ")]
    public async Task ChangePasswordAsync_EmptyInputs_ReturnsNullWithoutCallingRepository(
        string? username, string? currentPassword, string? newPassword)
    {
        // Act
        var result = await _authService.ChangePasswordAsync(username!, currentPassword!, newPassword!);

        // Assert
        result.Should().BeNull();
        _mockAuthRepository.Verify(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_RepositoryThrowsException_ReturnsNull()
    {
        // Arrange
        _mockAuthRepository.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _authService.ChangePasswordAsync("testuser", "currentpass", "newpassword");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region VerifyPasswordAsync Tests

    [Fact]
    public async Task VerifyPasswordAsync_ValidPassword_ReturnsTrue()
    {
        // Arrange
        _mockAuthRepository.Setup(x => x.ValidateCredentialsAsync("testuser", "password123"))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.VerifyPasswordAsync("testuser", "password123");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyPasswordAsync_InvalidPassword_ReturnsFalse()
    {
        // Arrange
        _mockAuthRepository.Setup(x => x.ValidateCredentialsAsync("testuser", "wrongpassword"))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.VerifyPasswordAsync("testuser", "wrongpassword");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData("", "password")]
    [InlineData("  ", "password")]
    [InlineData("testuser", null)]
    [InlineData("testuser", "")]
    [InlineData("testuser", "  ")]
    public async Task VerifyPasswordAsync_EmptyInputs_ReturnsFalseWithoutCallingRepository(
        string? username, string? password)
    {
        // Act
        var result = await _authService.VerifyPasswordAsync(username!, password!);

        // Assert
        result.Should().BeFalse();
        _mockAuthRepository.Verify(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task VerifyPasswordAsync_RepositoryThrowsException_ReturnsFalse()
    {
        // Arrange
        _mockAuthRepository.Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _authService.VerifyPasswordAsync("testuser", "password123");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
