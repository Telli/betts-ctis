using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BettsTax.Web.Tests.Controllers
{
    public class DocumentsControllerTests
    {
        private readonly Mock<IDocumentService> _serviceMock;
        private readonly DocumentsController _controller;

        public DocumentsControllerTests()
        {
            _serviceMock = new Mock<IDocumentService>();
            _controller = new DocumentsController(_serviceMock.Object);
        }

        [Fact]
        public async Task Upload_WithValidFile_ShouldReturnOkWithDocumentDto()
        {
            // Arrange
            var clientId = 1;
            var taxYearId = 2;
            var fileContent = "This is a test file";
            var fileName = "test.pdf";

            // Create mock file using a memory stream
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            var formFile = new FormFile(
                baseStream: ms,
                baseStreamOffset: 0,
                length: ms.Length,
                name: "file",
                fileName: fileName
            );

            var documentDto = new DocumentDto
            {
                DocumentId = 1,
                ClientId = clientId,
                FileName = fileName,
                TaxYearId = taxYearId
            };

            _serviceMock.Setup(s => s.UploadAsync(clientId, taxYearId, formFile))
                .ReturnsAsync(documentDto);

            // Act
            var result = await _controller.Upload(clientId, formFile, taxYearId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDocument = okResult.Value.Should().BeAssignableTo<DocumentDto>().Subject;
            
            returnedDocument.DocumentId.Should().Be(1);
            returnedDocument.ClientId.Should().Be(clientId);
            returnedDocument.FileName.Should().Be(fileName);
            returnedDocument.TaxYearId.Should().Be(taxYearId);
            
            _serviceMock.Verify(s => s.UploadAsync(clientId, taxYearId, formFile), Times.Once);
        }

        [Fact]
        public async Task Upload_WithNullFile_ShouldReturnBadRequest()
        {
            // Arrange
            var clientId = 1;
            IFormFile file = null;
            int? taxYearId = 2;

            // Act
            var result = await _controller.Upload(clientId, file, taxYearId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _serviceMock.Verify(s => s.UploadAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<IFormFile>()), Times.Never);
        }

        [Fact]
        public async Task List_ShouldReturnOkWithDocumentsList()
        {
            // Arrange
            var clientId = 1;
            var documents = new List<DocumentDto>
            {
                new DocumentDto { DocumentId = 1, ClientId = clientId, FileName = "doc1.pdf", TaxYearId = 2021 },
                new DocumentDto { DocumentId = 2, ClientId = clientId, FileName = "doc2.pdf", TaxYearId = 2022 }
            };

            _serviceMock.Setup(s => s.GetClientDocumentsAsync(clientId))
                .ReturnsAsync(documents);

            // Act
            var result = await _controller.List(clientId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDocuments = okResult.Value.Should().BeAssignableTo<IEnumerable<DocumentDto>>().Subject;
            returnedDocuments.Should().HaveCount(2);
            _serviceMock.Verify(s => s.GetClientDocumentsAsync(clientId), Times.Once);
        }

        [Fact]
        public async Task Delete_WithValidId_ShouldReturnNoContent()
        {
            // Arrange
            var clientId = 1;
            var documentId = 2;
            _serviceMock.Setup(s => s.DeleteAsync(documentId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(clientId, documentId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _serviceMock.Verify(s => s.DeleteAsync(documentId), Times.Once);
        }

        [Fact]
        public async Task Delete_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var clientId = 1;
            var invalidDocumentId = 999;
            _serviceMock.Setup(s => s.DeleteAsync(invalidDocumentId)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(clientId, invalidDocumentId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            _serviceMock.Verify(s => s.DeleteAsync(invalidDocumentId), Times.Once);
        }
    }
}
