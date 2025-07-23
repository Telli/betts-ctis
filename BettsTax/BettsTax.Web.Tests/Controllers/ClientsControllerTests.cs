using System.Collections.Generic;
using System.Threading.Tasks;
using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BettsTax.Web.Tests.Controllers
{
    public class ClientsControllerTests
    {
        private readonly Mock<IClientService> _serviceMock;
        private readonly ClientsController _controller;

        public ClientsControllerTests()
        {
            _serviceMock = new Mock<IClientService>();
            _controller = new ClientsController(_serviceMock.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnOkWithClients()
        {
            // Arrange
            var clients = new List<ClientDto>
            {
                new ClientDto { ClientId = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" },
                new ClientDto { ClientId = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com" }
            };

            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(clients);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedClients = okResult.Value.Should().BeAssignableTo<IEnumerable<ClientDto>>().Subject;
            returnedClients.Should().HaveCount(2);
            _serviceMock.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetById_WithValidId_ShouldReturnOkWithClient()
        {
            // Arrange
            var clientId = 1;
            var client = new ClientDto 
            { 
                ClientId = clientId, 
                FirstName = "John", 
                LastName = "Doe", 
                Email = "john.doe@example.com" 
            };

            _serviceMock.Setup(s => s.GetByIdAsync(clientId)).ReturnsAsync(client);

            // Act
            var result = await _controller.GetById(clientId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedClient = okResult.Value.Should().BeAssignableTo<ClientDto>().Subject;
            returnedClient.ClientId.Should().Be(clientId);
            _serviceMock.Verify(s => s.GetByIdAsync(clientId), Times.Once);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = 999;
            _serviceMock.Setup(s => s.GetByIdAsync(invalidId)).ReturnsAsync((ClientDto)null);

            // Act
            var result = await _controller.GetById(invalidId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            _serviceMock.Verify(s => s.GetByIdAsync(invalidId), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldReturnCreatedAtActionWithNewClient()
        {
            // Arrange
            var newClientDto = new ClientDto 
            { 
                FirstName = "New", 
                LastName = "Client", 
                Email = "new.client@example.com" 
            };

            var createdClientDto = new ClientDto 
            { 
                ClientId = 1, 
                FirstName = "New", 
                LastName = "Client", 
                Email = "new.client@example.com"
            };

            _serviceMock.Setup(s => s.CreateAsync(newClientDto)).ReturnsAsync(createdClientDto);

            // Act
            var result = await _controller.Create(newClientDto);

            // Assert
            var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAtActionResult.ActionName.Should().Be(nameof(ClientsController.GetById));
            createdAtActionResult.RouteValues["id"].Should().Be(1);
            
            var returnedClient = createdAtActionResult.Value.Should().BeAssignableTo<ClientDto>().Subject;
            returnedClient.ClientId.Should().Be(1);
            
            _serviceMock.Verify(s => s.CreateAsync(newClientDto), Times.Once);
        }

        [Fact]
        public async Task Update_WithValidId_ShouldReturnOkWithUpdatedClient()
        {
            // Arrange
            var clientId = 1;
            var clientDto = new ClientDto 
            { 
                FirstName = "Updated", 
                LastName = "Client", 
                Email = "updated.client@example.com" 
            };

            var updatedClientDto = new ClientDto 
            { 
                ClientId = clientId, 
                FirstName = "Updated", 
                LastName = "Client", 
                Email = "updated.client@example.com" 
            };

            _serviceMock.Setup(s => s.UpdateAsync(clientId, clientDto)).ReturnsAsync(updatedClientDto);

            // Act
            var result = await _controller.Update(clientId, clientDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedClient = okResult.Value.Should().BeAssignableTo<ClientDto>().Subject;
            returnedClient.ClientId.Should().Be(clientId);
            returnedClient.FirstName.Should().Be("Updated");
            
            _serviceMock.Verify(s => s.UpdateAsync(clientId, clientDto), Times.Once);
        }

        [Fact]
        public async Task Update_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = 999;
            var clientDto = new ClientDto 
            { 
                FirstName = "Updated", 
                LastName = "Client", 
                Email = "updated.client@example.com" 
            };

            _serviceMock.Setup(s => s.UpdateAsync(invalidId, clientDto)).ReturnsAsync((ClientDto)null);

            // Act
            var result = await _controller.Update(invalidId, clientDto);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            _serviceMock.Verify(s => s.UpdateAsync(invalidId, clientDto), Times.Once);
        }

        [Fact]
        public async Task Delete_WithValidId_ShouldReturnNoContent()
        {
            // Arrange
            var clientId = 1;
            _serviceMock.Setup(s => s.DeleteAsync(clientId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(clientId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _serviceMock.Verify(s => s.DeleteAsync(clientId), Times.Once);
        }

        [Fact]
        public async Task Delete_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = 999;
            _serviceMock.Setup(s => s.DeleteAsync(invalidId)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(invalidId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            _serviceMock.Verify(s => s.DeleteAsync(invalidId), Times.Once);
        }
    }
}
