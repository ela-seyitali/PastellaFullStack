using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pastella.Backend.Core.Entities;
using Pastella.Backend.Infrastructure.Data;
using Pastella.Backend.Infrastructure.Repositories;
using Xunit;

namespace Pastella.Backend.UnitTests.Repositories
{
    public class CakeRepositoryTests
    {
        private readonly ApplicationDbContext _context;
        private readonly CakeRepository _cakeRepository;

        public CakeRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _cakeRepository = new CakeRepository(_context);
        }

        [Fact]
        public async Task Create_ShouldAddCakeToDatabase()
        {
            // Arrange
            var design = new SweetDesign { Id = 1, Name = "Test Design", Description = "Test" };
            await _context.SweetDesigns.AddAsync(design);
            await _context.SaveChangesAsync();

            var cake = new Cake
            {
                Name = "Chocolate Cake",
                Description = "Delicious chocolate cake",
                Price = 50.00m,
                SweetDesignId = 1,
                ImageUrl = "test.jpg"
            };

            // Act
            await _cakeRepository.Create(cake);

            // Assert
            var savedCake = await _context.Cakes.FindAsync(cake.Id);
            savedCake.Should().NotBeNull();
            savedCake!.Name.Should().Be("Chocolate Cake");
            savedCake.Price.Should().Be(50.00m);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsCake()
        {
            // Arrange
            var design = new SweetDesign { Id = 1, Name = "Test Design", Description = "Test" };
            await _context.SweetDesigns.AddAsync(design);

            var cake = new Cake
            {
                Name = "Vanilla Cake",
                Description = "Sweet vanilla cake",
                Price = 45.00m,
                SweetDesignId = 1,
                ImageUrl = "vanilla.jpg"
            };
            await _context.Cakes.AddAsync(cake);
            await _context.SaveChangesAsync();

            // Act
            var result = await _cakeRepository.GetById(cake.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Vanilla Cake");
            result.Price.Should().Be(45.00m);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _cakeRepository.GetById(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAll_ReturnsAllCakes()
        {
            // Arrange
            var design = new SweetDesign { Id = 1, Name = "Test Design", Description = "Test" };
            await _context.SweetDesigns.AddAsync(design);

            var cakes = new List<Cake>
            {
                new Cake { Name = "Cake 1", Description = "Desc 1", Price = 30m, SweetDesignId = 1, ImageUrl = "1.jpg" },
                new Cake { Name = "Cake 2", Description = "Desc 2", Price = 40m, SweetDesignId = 1, ImageUrl = "2.jpg" },
                new Cake { Name = "Cake 3", Description = "Desc 3", Price = 50m, SweetDesignId = 1, ImageUrl = "3.jpg" }
            };
            await _context.Cakes.AddRangeAsync(cakes);
            await _context.SaveChangesAsync();

            // Act
            var result = await _cakeRepository.GetAll();

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task Update_ShouldUpdateCakeInDatabase()
        {
            // Arrange
            var design = new SweetDesign { Id = 1, Name = "Test Design", Description = "Test" };
            await _context.SweetDesigns.AddAsync(design);

            var cake = new Cake
            {
                Name = "Original Name",
                Description = "Original Description",
                Price = 30m,
                SweetDesignId = 1,
                ImageUrl = "original.jpg"
            };
            await _context.Cakes.AddAsync(cake);
            await _context.SaveChangesAsync();

            // Act
            cake.Name = "Updated Name";
            cake.Price = 60m;
            await _cakeRepository.Update(cake.Id, cake);

            // Assert
            var updatedCake = await _context.Cakes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cake.Id);
            updatedCake.Should().NotBeNull();
            updatedCake!.Name.Should().Be("Updated Name");
            updatedCake.Price.Should().Be(60m);
        }

        [Fact]
        public async Task Delete_ShouldRemoveCakeFromDatabase()
        {
            // Arrange
            var design = new SweetDesign { Id = 1, Name = "Test Design", Description = "Test" };
            await _context.SweetDesigns.AddAsync(design);

            var cake = new Cake
            {
                Name = "To Delete",
                Description = "Will be deleted",
                Price = 25m,
                SweetDesignId = 1,
                ImageUrl = "delete.jpg"
            };
            await _context.Cakes.AddAsync(cake);
            await _context.SaveChangesAsync();

            // Act
            await _cakeRepository.Delete(cake.Id);

            // Assert
            var deletedCake = await _context.Cakes.FindAsync(cake.Id);
            deletedCake.Should().BeNull();
        }
    }
}
