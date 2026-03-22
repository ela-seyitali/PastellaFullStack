using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pastella.Backend.Infrastructure.Data;
using Pastella.Backend.Infrastructure.Repositories;
using Pastella.Backend.Core.Entities;

namespace Pastella.Backend.UnitTests.Repositories;

public class OrderRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrderRepository _sut;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _sut = new OrderRepository(_context);
    }

    [Fact]
    public async Task Create_AddsOrderToDatabase()
    {
        // Arrange
        var user = new User { FullName = "Test User", Email = "test@test.com", PasswordHash = "hash", Role = "User", CreatedDate = DateTime.UtcNow };
        var cake = new Cake { Name = "Test Cake", Price = 100, Description = "Test" };
        _context.Users.Add(user);
        _context.Cakes.Add(cake);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            CakeId = cake.Id,
            TotalPrice = 150.00m,
            Status = "Pending",
            OrderDate = DateTime.UtcNow,
            DeliveryAddress = "Test Address"
        };

        // Act
        await _sut.Create(order);

        // Assert
        var savedOrder = await _context.Orders.FirstOrDefaultAsync(o => o.UserId == user.Id);
        savedOrder.Should().NotBeNull();
        savedOrder!.TotalPrice.Should().Be(150.00m);
        savedOrder.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetById_ReturnsOrder()
    {
        // Arrange
        var user = new User { FullName = "Test User", Email = "test@test.com", PasswordHash = "hash", Role = "User", CreatedDate = DateTime.UtcNow };
        var cake = new Cake { Name = "Test Cake", Price = 100, Description = "Test" };
        _context.Users.Add(user);
        _context.Cakes.Add(cake);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            CakeId = cake.Id,
            TotalPrice = 150.00m,
            Status = "Pending",
            OrderDate = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetById(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task GetByUserId_ReturnsUserOrders()
    {
        // Arrange
        var user1 = new User { FullName = "User 1", Email = "user1@test.com", PasswordHash = "hash", Role = "User", CreatedDate = DateTime.UtcNow };
        var user2 = new User { FullName = "User 2", Email = "user2@test.com", PasswordHash = "hash", Role = "User", CreatedDate = DateTime.UtcNow };
        var cake1 = new Cake { Name = "Cake 1", Price = 100, Description = "Test" };
        var cake2 = new Cake { Name = "Cake 2", Price = 200, Description = "Test" };
        var cake3 = new Cake { Name = "Cake 3", Price = 300, Description = "Test" };
        
        _context.Users.AddRange(user1, user2);
        _context.Cakes.AddRange(cake1, cake2, cake3);
        await _context.SaveChangesAsync();

        var orders = new List<Order>
        {
            new Order { UserId = user1.Id, CakeId = cake1.Id, TotalPrice = 100, Status = "Pending", OrderDate = DateTime.UtcNow },
            new Order { UserId = user1.Id, CakeId = cake2.Id, TotalPrice = 200, Status = "Confirmed", OrderDate = DateTime.UtcNow },
            new Order { UserId = user2.Id, CakeId = cake3.Id, TotalPrice = 300, Status = "Pending", OrderDate = DateTime.UtcNow }
        };
        _context.Orders.AddRange(orders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByUserId(user1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.UserId == user1.Id);
    }

    [Fact]
    public async Task Update_UpdatesOrder()
    {
        // Arrange
        var user = new User { FullName = "Test User", Email = "test@test.com", PasswordHash = "hash", Role = "User", CreatedDate = DateTime.UtcNow };
        var cake = new Cake { Name = "Test Cake", Price = 100, Description = "Test" };
        _context.Users.Add(user);
        _context.Cakes.Add(cake);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            CakeId = cake.Id,
            TotalPrice = 150.00m,
            Status = "Pending",
            OrderDate = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        order.Status = "Confirmed";

        // Act
        await _sut.Update(order.Id, order);

        // Assert
        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task Delete_RemovesOrder()
    {
        // Arrange
        var user = new User { FullName = "Test User", Email = "test@test.com", PasswordHash = "hash", Role = "User", CreatedDate = DateTime.UtcNow };
        var cake = new Cake { Name = "Test Cake", Price = 100, Description = "Test" };
        _context.Users.Add(user);
        _context.Cakes.Add(cake);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            CakeId = cake.Id,
            TotalPrice = 150.00m,
            Status = "Pending",
            OrderDate = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        await _sut.Delete(order.Id);

        // Assert
        var deletedOrder = await _context.Orders.FindAsync(order.Id);
        deletedOrder.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
