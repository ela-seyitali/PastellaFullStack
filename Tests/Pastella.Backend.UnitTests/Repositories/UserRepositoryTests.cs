using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pastella.Backend.Core.Entities;
using Pastella.Backend.Infrastructure.Data;
using Pastella.Backend.Infrastructure.Repositories;
using Xunit;

namespace Pastella.Backend.UnitTests.Repositories
{
    public class UserRepositoryTests
    {
        private readonly ApplicationDbContext _context;
        private readonly UserRepository _userRepository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _userRepository = new UserRepository(_context);
        }

        [Fact]
        public async Task Create_ShouldAddUserToDatabase()
        {
            // Arrange
            var user = new User
            {
                FullName = "Test User",
                Email = "test@test.com",
                PasswordHash = "hashedpassword",
                Role = "Customer"
            };

            // Act
            await _userRepository.Create(user);

            // Assert
            var savedUser = await _context.Users.FindAsync(user.Id);
            savedUser.Should().NotBeNull();
            savedUser!.FullName.Should().Be("Test User");
            savedUser.Email.Should().Be("test@test.com");
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                FullName = "John Doe",
                Email = "john@test.com",
                PasswordHash = "hash",
                Role = "Customer"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userRepository.GetById(user.Id);

            // Assert
            result.Should().NotBeNull();
            result!.FullName.Should().Be("John Doe");
            result.Email.Should().Be("john@test.com");
        }

        [Fact]
        public async Task GetByEmail_WithValidEmail_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                FullName = "Jane Doe",
                Email = "jane@test.com",
                PasswordHash = "hash",
                Role = "Customer"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userRepository.GetByEmail("jane@test.com");

            // Assert
            result.Should().NotBeNull();
            result!.FullName.Should().Be("Jane Doe");
        }

        [Fact]
        public async Task GetByEmail_WithInvalidEmail_ReturnsNull()
        {
            // Act
            var result = await _userRepository.GetByEmail("nonexistent@test.com");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAll_ReturnsAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { FullName = "User 1", Email = "user1@test.com", PasswordHash = "hash1", Role = "Customer" },
                new User { FullName = "User 2", Email = "user2@test.com", PasswordHash = "hash2", Role = "Admin" },
                new User { FullName = "User 3", Email = "user3@test.com", PasswordHash = "hash3", Role = "Customer" }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userRepository.GetAll();

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task Update_ShouldUpdateUserInDatabase()
        {
            // Arrange
            var user = new User
            {
                FullName = "Original Name",
                Email = "original@test.com",
                PasswordHash = "hash",
                Role = "Customer"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            user.FullName = "Updated Name";
            user.Email = "updated@test.com";
            await _userRepository.Update(user.Id, user);

            // Assert
            var updatedUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.FullName.Should().Be("Updated Name");
            updatedUser.Email.Should().Be("updated@test.com");
        }

        [Fact]
        public async Task Delete_ShouldRemoveUserFromDatabase()
        {
            // Arrange
            var user = new User
            {
                FullName = "To Delete",
                Email = "delete@test.com",
                PasswordHash = "hash",
                Role = "Customer"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _userRepository.Delete(user.Id);

            // Assert
            var deletedUser = await _context.Users.FindAsync(user.Id);
            deletedUser.Should().BeNull();
        }
    }
}
