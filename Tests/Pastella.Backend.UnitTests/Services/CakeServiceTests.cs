using Xunit;
using Moq;
using FluentAssertions;
using Pastella.Backend.Application.Services;
using Pastella.Backend.Core.Interfaces;
using Pastella.Backend.Core.DTOs;
using Pastella.Backend.Core.Entities;

namespace Pastella.Backend.UnitTests.Services;

public class CakeServiceTests
{
    private readonly Mock<ICakeRepository> _cakeRepositoryMock;
    private readonly CakeService _sut;

    public CakeServiceTests()
    {
        _cakeRepositoryMock = new Mock<ICakeRepository>();
        _sut = new CakeService(_cakeRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsCakeDto()
    {
        // Arrange
        var createCakeDto = new CreateCakeDto
        {
            Name = "Chocolate Cake",
            Description = "Delicious chocolate cake",
            Price = 150.00m,
            ImageUrl = "https://example.com/cake.jpg",
            SweetDesignId = 1
        };

        _cakeRepositoryMock.Setup(x => x.Create(It.IsAny<Cake>()))
            .Callback<Cake>(c => c.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateAsync(createCakeDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(createCakeDto.Name);
        result.Description.Should().Be(createCakeDto.Description);
        result.Price.Should().Be(createCakeDto.Price);
        result.ImageUrl.Should().Be(createCakeDto.ImageUrl);
        
        _cakeRepositoryMock.Verify(x => x.Create(It.Is<Cake>(c => 
            c.Name == createCakeDto.Name &&
            c.Price == createCakeDto.Price
        )), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCakeList()
    {
        // Arrange
        var cakes = new List<Cake>
        {
            new Cake { Id = 1, Name = "Cake 1", Price = 100, Description = "Desc 1", ImageUrl = "url1" },
            new Cake { Id = 2, Name = "Cake 2", Price = 200, Description = "Desc 2", ImageUrl = "url2" }
        };

        _cakeRepositoryMock.Setup(x => x.GetAll())
            .ReturnsAsync(cakes);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Cake 1");
        result[1].Name.Should().Be("Cake 2");
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCakeDto()
    {
        // Arrange
        var cakeId = 1;
        var cake = new Cake
        {
            Id = cakeId,
            Name = "Test Cake",
            Description = "Test Description",
            Price = 150.00m,
            ImageUrl = "https://example.com/cake.jpg"
        };

        _cakeRepositoryMock.Setup(x => x.GetById(cakeId))
            .ReturnsAsync(cake);

        // Act
        var result = await _sut.GetByIdAsync(cakeId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(cakeId);
        result.Name.Should().Be(cake.Name);
        result.Price.Should().Be(cake.Price);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var cakeId = 999;

        _cakeRepositoryMock.Setup(x => x.GetById(cakeId))
            .ReturnsAsync((Cake?)null);

        // Act
        var result = await _sut.GetByIdAsync(cakeId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesCake()
    {
        // Arrange
        var cakeId = 1;
        var existingCake = new Cake
        {
            Id = cakeId,
            Name = "Old Name",
            Description = "Old Description",
            Price = 100.00m,
            ImageUrl = "old-url.jpg"
        };

        var updateDto = new CreateCakeDto
        {
            Name = "New Name",
            Description = "New Description",
            Price = 200.00m,
            ImageUrl = "new-url.jpg",
            SweetDesignId = 2
        };

        _cakeRepositoryMock.Setup(x => x.GetById(cakeId))
            .ReturnsAsync(existingCake);
        
        _cakeRepositoryMock.Setup(x => x.Update(cakeId, It.IsAny<Cake>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateAsync(cakeId, updateDto);

        // Assert
        _cakeRepositoryMock.Verify(x => x.Update(cakeId, It.Is<Cake>(c =>
            c.Name == updateDto.Name &&
            c.Description == updateDto.Description &&
            c.Price == updateDto.Price
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var cakeId = 999;
        var updateDto = new CreateCakeDto
        {
            Name = "New Name",
            Description = "New Description",
            Price = 200.00m
        };

        _cakeRepositoryMock.Setup(x => x.GetById(cakeId))
            .ReturnsAsync((Cake?)null);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.UpdateAsync(cakeId, updateDto));
        _cakeRepositoryMock.Verify(x => x.Update(It.IsAny<int>(), It.IsAny<Cake>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepositoryDelete()
    {
        // Arrange
        var cakeId = 1;

        _cakeRepositoryMock.Setup(x => x.Delete(cakeId))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAsync(cakeId);

        // Assert
        _cakeRepositoryMock.Verify(x => x.Delete(cakeId), Times.Once);
    }
}
