using Xunit;
using Moq;
using FluentAssertions;
using Pastella.Backend.Application.Services;
using Pastella.Backend.Core.Interfaces;
using Pastella.Backend.Core.DTOs;
using Pastella.Backend.Core.Entities;
using Microsoft.Extensions.Configuration;

namespace Pastella.Backend.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _configurationMock = new Mock<IConfiguration>();
        
        // Setup JWT configuration
        _configurationMock.Setup(x => x["Jwt:Key"]).Returns("ThisIsAVerySecureKeyForJWTTokenGenerationWithAtLeast32Characters");
        _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("PastellaBackend");
        _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("PastellaClients");
        
        _sut = new AuthService(_userRepositoryMock.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccessWithToken()
    {
        // Arrange
        var registrationDto = new UserRegistrationDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock.Setup(x => x.GetByEmail(registrationDto.Email))
            .ReturnsAsync((User?)null);
        
        _userRepositoryMock.Setup(x => x.Create(It.IsAny<User>()))
            .Callback<User>(u => u.Id = 1)
            .Returns(Task.CompletedTask);
        
        _userRepositoryMock.Setup(x => x.Update(It.IsAny<int>(), It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Register(registrationDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Errors.Should().BeNullOrEmpty();
        
        _userRepositoryMock.Verify(x => x.Create(It.IsAny<User>()), Times.Once);
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<int>(), It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var registrationDto = new UserRegistrationDto
        {
            FullName = "Test User",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        var existingUser = new User { Id = 1, Email = registrationDto.Email };
        _userRepositoryMock.Setup(x => x.GetByEmail(registrationDto.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.Register(registrationDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Bu email adresi zaten kullanılıyor");
        result.Token.Should().BeNullOrEmpty();
        
        _userRepositoryMock.Verify(x => x.Create(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var loginDto = new UserLoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = 1,
            Email = loginDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(loginDto.Password),
            Role = "User"
        };

        _userRepositoryMock.Setup(x => x.GetByEmail(loginDto.Email))
            .ReturnsAsync(user);
        
        _userRepositoryMock.Setup(x => x.Update(It.IsAny<int>(), It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Login(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Errors.Should().BeNullOrEmpty();
        
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<int>(), It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsFailure()
    {
        // Arrange
        var loginDto = new UserLoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock.Setup(x => x.GetByEmail(loginDto.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.Login(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password");
        result.Token.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var loginDto = new UserLoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = 1,
            Email = loginDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            Role = "User"
        };

        _userRepositoryMock.Setup(x => x.GetByEmail(loginDto.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.Login(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password");
        result.Token.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            RefreshToken = refreshToken,
            RefreshTokenExpires = DateTime.UtcNow.AddDays(15),
            Role = "User"
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshToken(refreshToken))
            .ReturnsAsync(user);
        
        _userRepositoryMock.Setup(x => x.Update(It.IsAny<int>(), It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RefreshToken(refreshToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(refreshToken); // Should be a new token
    }

    [Fact]
    public async Task RefreshToken_WithExpiredToken_ReturnsFailure()
    {
        // Arrange
        var refreshToken = "expired-refresh-token";
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            RefreshToken = refreshToken,
            RefreshTokenExpires = DateTime.UtcNow.AddDays(-1), // Expired
            Role = "User"
        };

        _userRepositoryMock.Setup(x => x.GetByRefreshToken(refreshToken))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.RefreshToken(refreshToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Geçersiz veya süresi dolmuş refresh token");
    }

    [Fact]
    public async Task ForgotPassword_WithValidEmail_ReturnsSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Id = 1,
            Email = email,
            Role = "User"
        };

        _userRepositoryMock.Setup(x => x.GetByEmail(email))
            .ReturnsAsync(user);
        
        _userRepositoryMock.Setup(x => x.Update(It.IsAny<int>(), It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ForgotPassword(email);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<int>(), 
            It.Is<User>(u => u.ResetPasswordToken != null && u.ResetPasswordExpires != null)), 
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmail_ReturnsFailure()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _userRepositoryMock.Setup(x => x.GetByEmail(email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.ForgotPassword(email);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Bu email adresi ile kayıtlı kullanıcı bulunamadı");
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var resetToken = "valid-reset-token";
        var newPassword = "NewPassword123!";
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            ResetPasswordToken = resetToken,
            ResetPasswordExpires = DateTime.UtcNow.AddHours(1),
            Role = "User"
        };

        _userRepositoryMock.Setup(x => x.GetByResetToken(resetToken))
            .ReturnsAsync(user);
        
        _userRepositoryMock.Setup(x => x.Update(It.IsAny<int>(), It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ResetPassword(resetToken, newPassword);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<int>(), 
            It.Is<User>(u => u.ResetPasswordToken == null && u.ResetPasswordExpires == null)), 
            Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithExpiredToken_ReturnsFailure()
    {
        // Arrange
        var resetToken = "expired-reset-token";
        var newPassword = "NewPassword123!";
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            ResetPasswordToken = resetToken,
            ResetPasswordExpires = DateTime.UtcNow.AddHours(-1), // Expired
            Role = "User"
        };

        _userRepositoryMock.Setup(x => x.GetByResetToken(resetToken))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.ResetPassword(resetToken, newPassword);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Geçersiz veya süresi dolmuş token");
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var resetToken = "invalid-token";
        var newPassword = "NewPassword123!";

        _userRepositoryMock.Setup(x => x.GetByResetToken(resetToken))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.ResetPassword(resetToken, newPassword);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Geçersiz veya süresi dolmuş token");
    }
}
