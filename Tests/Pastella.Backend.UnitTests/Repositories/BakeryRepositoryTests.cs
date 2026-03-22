using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pastella.Backend.Core.Entities;
using Pastella.Backend.Infrastructure.Data;
using Pastella.Backend.Infrastructure.Repositories;
using Xunit;

namespace Pastella.Backend.UnitTests.Repositories
{
    public class BakeryRepositoryTests
    {
        private readonly ApplicationDbContext _context;
        private readonly BakeryRepository _bakeryRepository;

        public BakeryRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _bakeryRepository = new BakeryRepository(_context);
        }

        [Fact]
        public async Task Create_ShouldAddBakeryToDatabase()
        {
            // Arrange
            var bakery = new Bakery
            {
                Name = "Sweet Bakery",
                Address = "123 Main St",
                Phone = "555-1234",
                Description = "Best bakery in town"
            };

            // Act
            await _bakeryRepository.Create(bakery);

            // Assert
            var savedBakery = await _context.Bakeries.FindAsync(bakery.Id);
            savedBakery.Should().NotBeNull();
            savedBakery!.Name.Should().Be("Sweet Bakery");
            savedBakery.Phone.Should().Be("555-1234");
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsBakery()
        {
            // Arrange
            var bakery = new Bakery
            {
                Name = "Pastella Bakery",
                Address = "456 Oak Ave",
                Phone = "555-5678"
            };
            await _context.Bakeries.AddAsync(bakery);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bakeryRepository.GetById(bakery.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Pastella Bakery");
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _bakeryRepository.GetById(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAll_ReturnsAllBakeries()
        {
            // Arrange
            var bakeries = new List<Bakery>
            {
                new Bakery { Name = "Bakery 1", Address = "Address 1", Phone = "111-1111" },
                new Bakery { Name = "Bakery 2", Address = "Address 2", Phone = "222-2222" },
                new Bakery { Name = "Bakery 3", Address = "Address 3", Phone = "333-3333" }
            };
            await _context.Bakeries.AddRangeAsync(bakeries);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bakeryRepository.GetAll();

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task Update_ShouldUpdateBakeryInDatabase()
        {
            // Arrange
            var bakery = new Bakery
            {
                Name = "Original Bakery",
                Address = "Original Address",
                Phone = "000-0000"
            };
            await _context.Bakeries.AddAsync(bakery);
            await _context.SaveChangesAsync();

            // Act
            bakery.Name = "Updated Bakery";
            bakery.Phone = "999-9999";
            await _bakeryRepository.Update(bakery.Id, bakery);

            // Assert
            var updatedBakery = await _context.Bakeries.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bakery.Id);
            updatedBakery.Should().NotBeNull();
            updatedBakery!.Name.Should().Be("Updated Bakery");
            updatedBakery.Phone.Should().Be("999-9999");
        }

        [Fact]
        public async Task Delete_ShouldRemoveBakeryFromDatabase()
        {
            // Arrange
            var bakery = new Bakery
            {
                Name = "To Delete",
                Address = "Delete Address",
                Phone = "000-0000"
            };
            await _context.Bakeries.AddAsync(bakery);
            await _context.SaveChangesAsync();

            // Act
            await _bakeryRepository.Delete(bakery.Id);

            // Assert
            var deletedBakery = await _context.Bakeries.FindAsync(bakery.Id);
            deletedBakery.Should().BeNull();
        }
    }
}
