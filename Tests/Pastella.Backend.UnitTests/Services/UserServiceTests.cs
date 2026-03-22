using FluentAssertions;
using Moq;
using Pastella.Backend.Application.Services;
using Pastella.Backend.Core.DTOs;
using Pastella.Backend.Core.Entities;
using Pastella.Backend.Core.Interfaces;
using Xunit;

namespace Pastella.Backend.UnitTests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _userService = new UserService(_userRepositoryMock.Object);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithValidId_ReturnsUserDto()
        {
            // Arrange
            int userId = 1;
            var user = new User
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@test.com",
                PasswordHash = "hash",
                Role = "Customer"
            };

            _userRepositoryMock
                .Setup(x => x.GetById(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(userId);
            result.FullName.Should().Be("Test User");
            result.Email.Should().Be("test@test.com");
            result.Role.Should().Be("Customer");
        }

        [Fact]
        public async Task GetUserByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            int userId = 999;
            _userRepositoryMock
                .Setup(x => x.GetById(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateUserAsync_WithValidId_UpdatesUser()
        {
            // Arrange
            int userId = 1;
            var existingUser = new User
            {
                Id = userId,
                FullName = "Old Name",
                Email = "old@test.com",
                PasswordHash = "hash",
                Role = "Customer"
            };

            var updateDto = new UserDto
            {
                Id = userId,
                FullName = "New Name",
                Email = "new@test.com",
                Role = "Admin"
            };

            _userRepositoryMock
                .Setup(x => x.GetById(userId))
                .ReturnsAsync(existingUser);

            _userRepositoryMock
                .Setup(x => x.Update(userId, It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            // Act
            await _userService.UpdateUserAsync(userId, updateDto);

            // Assert
            _userRepositoryMock.Verify(x => x.Update(userId, It.Is<User>(u =>
                u.FullName == "New Name" &&
                u.Email == "new@test.com" &&
                u.Role == "Admin"
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_WithInvalidId_DoesNotUpdate()
        {
            // Arrange
            int userId = 999;
            var updateDto = new UserDto
            {
                Id = userId,
                FullName = "New Name",
                Email = "new@test.com",
                Role = "Admin"
            };

            _userRepositoryMock
                .Setup(x => x.GetById(userId))
                .ReturnsAsync((User?)null);

            // Act
            await _userService.UpdateUserAsync(userId, updateDto);

            // Assert
            _userRepositoryMock.Verify(x => x.Update(It.IsAny<int>(), It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldCallRepositoryDelete()
        {
            // Arrange
            int userId = 1;
            _userRepositoryMock
                .Setup(x => x.Delete(userId))
                .Returns(Task.CompletedTask);

            // Act
            await _userService.DeleteUserAsync(userId);

            // Assert
            _userRepositoryMock.Verify(x => x.Delete(userId), Times.Once);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = 1, FullName = "User 1", Email = "user1@test.com", PasswordHash = "hash1", Role = "Customer" },
                new User { Id = 2, FullName = "User 2", Email = "user2@test.com", PasswordHash = "hash2", Role = "Admin" },
                new User { Id = 3, FullName = "User 3", Email = "user3@test.com", PasswordHash = "hash3", Role = "Customer" }
            };

            _userRepositoryMock
                .Setup(x => x.GetAll())
                .ReturnsAsync(users);

            // Act
            var result = await _userService.GetAllUsers();

            // Assert
            result.Should().HaveCount(3);
            result[0].FullName.Should().Be("User 1");
            result[1].FullName.Should().Be("User 2");
            result[2].FullName.Should().Be("User 3");
        }
    }
}
