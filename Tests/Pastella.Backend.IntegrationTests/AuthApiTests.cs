using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pastella.Backend.Core.DTOs;
using Pastella.Backend.Core.Entities;
using Pastella.Backend.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Pastella.Backend.IntegrationTests
{
    public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public AuthApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var registerDto = new UserRegistrationDto
            {
                FullName = "Test User",
                Email = $"test{Guid.NewGuid()}@test.com",
                Password = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Contain("token");
            result.Should().Contain("refreshToken");
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
        {
            // Arrange - Use a shared database by using the same factory
            var email = $"duplicate{Guid.NewGuid()}@test.com";
            
            var firstRegister = new UserRegistrationDto
            {
                FullName = "First User",
                Email = email,
                Password = "Test123!@#"
            };

            var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", firstRegister);
            firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var secondRegister = new UserRegistrationDto
            {
                FullName = "Second User",
                Email = email,
                Password = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", secondRegister);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsToken()
        {
            // Arrange
            var email = $"login{Guid.NewGuid()}@test.com";
            var password = "Test123!@#";

            var registerDto = new UserRegistrationDto
            {
                FullName = "Login Test User",
                Email = email,
                Password = password
            };

            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new UserLoginDto
            {
                Email = email,
                Password = password
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Contain("token");
            result.Should().Contain("refreshToken");
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Email = "nonexistent@test.com",
                Password = "WrongPassword123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RefreshToken_WithValidToken_ReturnsNewToken()
        {
            // Arrange
            var email = $"refresh{Guid.NewGuid()}@test.com";
            var password = "Test123!@#";

            var registerDto = new UserRegistrationDto
            {
                FullName = "Refresh Test User",
                Email = email,
                Password = password
            };

            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new UserLoginDto
            {
                Email = email,
                Password = password
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            
            // Extract refreshToken from JSON response (lowercase key)
            var loginJson = System.Text.Json.JsonDocument.Parse(loginContent);
            var refreshToken = loginJson.RootElement.GetProperty("refreshToken").GetString();

            var refreshDto = new RefreshTokenRequest
            {
                RefreshToken = refreshToken!
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", refreshDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Contain("token");
            result.Should().Contain("refreshToken");
        }
    }
}
