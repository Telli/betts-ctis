using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using AutoMapper;

namespace BettsTax.Core.Tests.Services
{
    public class ClientServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly Mock<IMapper> _mapperMock;

        public ClientServiceTests()
        {
            // Configure in-memory database
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"BettsTaxTestDb_{Guid.NewGuid()}")
                .Options;

            // Mock AutoMapper
            _mapperMock = new Mock<IMapper>();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllClients()
        {
            // Arrange
            var clients = new List<Client>
            {
                new Client { ClientId = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" },
                new Client { ClientId = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com" }
            };

            var clientDtos = new List<ClientDto>
            {
                new ClientDto { ClientId = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" },
                new ClientDto { ClientId = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com" }
            };

            using (var context = new ApplicationDbContext(_options))
            {
                await context.Database.EnsureCreatedAsync();
                await context.Clients.AddRangeAsync(clients);
                await context.SaveChangesAsync();
            }

            _mapperMock.Setup(m => m.Map<IEnumerable<ClientDto>>(It.IsAny<List<Client>>()))
                .Returns(clientDtos);

            // Act
            using (var context = new ApplicationDbContext(_options))
            {
                var service = new ClientService(context, _mapperMock.Object);
                var result = await service.GetAllAsync();

                // Assert
                result.Should().NotBeNull();
                result.Should().HaveCount(2);
                _mapperMock.Verify(m => m.Map<IEnumerable<ClientDto>>(It.IsAny<List<Client>>()), Times.Once);
            }
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnClient()
        {
            // Arrange
            var clientId = 1;
            var client = new Client { ClientId = clientId, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" };
            var clientDto = new ClientDto { ClientId = clientId, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" };

            using (var context = new ApplicationDbContext(_options))
            {
                await context.Database.EnsureCreatedAsync();
                await context.Clients.AddAsync(client);
                await context.SaveChangesAsync();
            }

            _mapperMock.Setup(m => m.Map<ClientDto>(It.IsAny<Client>()))
                .Returns(clientDto);

            // Act
            using (var context = new ApplicationDbContext(_options))
            {
                var service = new ClientService(context, _mapperMock.Object);
                var result = await service.GetByIdAsync(clientId);

                // Assert
                result.Should().NotBeNull();
                result.ClientId.Should().Be(clientId);
                _mapperMock.Verify(m => m.Map<ClientDto>(It.IsAny<Client>()), Times.Once);
            }
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var invalidId = 999;

            // Act
            using (var context = new ApplicationDbContext(_options))
            {
                var service = new ClientService(context, _mapperMock.Object);
                var result = await service.GetByIdAsync(invalidId);

                // Assert
                result.Should().BeNull();
                _mapperMock.Verify(m => m.Map<ClientDto>(It.IsAny<Client>()), Times.Never);
            }
        }

        [Fact]
        public async Task CreateAsync_ShouldAddAndReturnNewClient()
        {
            // Arrange
            var clientDto = new ClientDto { FirstName = "New", LastName = "Client", Email = "new.client@example.com" };
            var client = new Client { FirstName = "New", LastName = "Client", Email = "new.client@example.com" };
            var createdClientDto = new ClientDto { ClientId = 1, FirstName = "New", LastName = "Client", Email = "new.client@example.com" };

            _mapperMock.Setup(m => m.Map<Client>(It.IsAny<ClientDto>()))
                .Returns(client);
            _mapperMock.Setup(m => m.Map<ClientDto>(It.IsAny<Client>()))
                .Returns(createdClientDto);

            // Act
            using (var context = new ApplicationDbContext(_options))
            {
                await context.Database.EnsureCreatedAsync();
                var service = new ClientService(context, _mapperMock.Object);
                var result = await service.CreateAsync(clientDto);

                // Assert
                result.Should().NotBeNull();
                result.ClientId.Should().Be(1);
                context.Clients.Count().Should().Be(1);
                _mapperMock.Verify(m => m.Map<Client>(It.IsAny<ClientDto>()), Times.Once);
                _mapperMock.Verify(m => m.Map<ClientDto>(It.IsAny<Client>()), Times.Once);
            }
        }

        [Fact]
        public async Task UpdateAsync_WithValidId_ShouldUpdateAndReturnClient()
        {
            // Arrange
            var clientId = 1;
            var existingClient = new Client 
            { 
                ClientId = clientId, 
                FirstName = "John", 
                LastName = "Doe", 
                Email = "john.doe@example.com"
            };

            var updatedClientDto = new ClientDto 
            { 
                FirstName = "John Updated", 
                LastName = "Doe Updated", 
                Email = "john.updated@example.com"
            };

            using (var context = new ApplicationDbContext(_options))
            {
                await context.Database.EnsureCreatedAsync();
                await context.Clients.AddAsync(existingClient);
                await context.SaveChangesAsync();
            }

            _mapperMock.Setup(m => m.Map(It.IsAny<ClientDto>(), It.IsAny<Client>()))
                .Callback<ClientDto, Client>((dto, entity) => 
                {
                    entity.FirstName = dto.FirstName;
                    entity.LastName = dto.LastName;
                    entity.Email = dto.Email;
                });

            _mapperMock.Setup(m => m.Map<ClientDto>(It.IsAny<Client>()))
                .Returns((Client c) => new ClientDto 
                {
                    ClientId = c.ClientId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email
                });

            // Act
            using (var context = new ApplicationDbContext(_options))
            {
                var service = new ClientService(context, _mapperMock.Object);
                var result = await service.UpdateAsync(clientId, updatedClientDto);

                // Assert
                result.Should().NotBeNull();
                result.ClientId.Should().Be(clientId);
                result.FirstName.Should().Be("John Updated");
                result.LastName.Should().Be("Doe Updated");
                result.Email.Should().Be("john.updated@example.com");
                
                _mapperMock.Verify(m => m.Map(It.IsAny<ClientDto>(), It.IsAny<Client>()), Times.Once);
                _mapperMock.Verify(m => m.Map<ClientDto>(It.IsAny<Client>()), Times.Once);
            }
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var invalidId = 999;
            var clientDto = new ClientDto { FirstName = "Test", LastName = "User", Email = "test@example.com" };

            // Act
            using (var context = new ApplicationDbContext(_options))
            {
                await context.Database.EnsureCreatedAsync();
                var service = new ClientService(context, _mapperMock.Object);
                var result = await service.UpdateAsync(invalidId, clientDto);

                // Assert
                result.Should().BeNull();
                _mapperMock.Verify(m => m.Map(It.IsAny<ClientDto>(), It.IsAny<Client>()), Times.Never);
            }
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var clientId = 1;
            var client = new Client { ClientId = clientId, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" };

            using (var context = new ApplicationDbContext(_options))
            {
                await context.Database.EnsureCreatedAsync();
                await context.Clients.AddAsync(client);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new ApplicationDbContext(_options))
            {
                var service = new ClientService(context, _mapperMock.Object);
                var result = await service.DeleteAsync(clientId);

                // Assert
                result.Should().BeTrue();
                (await context.Clients.FindAsync(clientId)).Should().BeNull();
            }
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var invalidId = 999;

            // Act
            using (var context = new ApplicationDbContext(_options))
            {
                await context.Database.EnsureCreatedAsync();
                var service = new ClientService(context, _mapperMock.Object);
                var result = await service.DeleteAsync(invalidId);

                // Assert
                result.Should().BeFalse();
            }
        }
    }
}
