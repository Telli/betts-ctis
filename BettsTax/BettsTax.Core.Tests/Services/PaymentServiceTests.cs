using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BettsTax.Core.Tests.Services
{
    public class PaymentServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<PaymentService>> _loggerMock;
        private readonly PaymentService _service;

        public PaymentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<PaymentService>>();
            _service = new PaymentService(_context, _mapperMock.Object, _loggerMock.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var clients = new List<Client>
            {
                new Client
                {
                    Id = 1,
                    BusinessName = "ABC Corp",
                    CompanyRegistrationNumber = "REG001",
                    Email = "abc@corp.com",
                    PhoneNumber = "1234567890",
                    Address = "123 Main St",
                    TaxpayerCategory = TaxpayerCategory.Large,
                    IsActive = true
                },
                new Client
                {
                    Id = 2,
                    BusinessName = "XYZ Ltd",
                    CompanyRegistrationNumber = "REG002",
                    Email = "xyz@ltd.com",
                    PhoneNumber = "0987654321",
                    Address = "456 Oak Ave",
                    TaxpayerCategory = TaxpayerCategory.Medium,
                    IsActive = true
                }
            };

            var taxYears = new List<TaxYear>
            {
                new TaxYear
                {
                    Id = 1,
                    ClientId = 1,
                    Year = 2024,
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = new DateTime(2024, 12, 31),
                    Status = "Active"
                },
                new TaxYear
                {
                    Id = 2,
                    ClientId = 2,
                    Year = 2024,
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = new DateTime(2024, 12, 31),
                    Status = "Active"
                }
            };

            var payments = new List<Payment>
            {
                new Payment
                {
                    Id = 1,
                    ClientId = 1,
                    TaxYearId = 1,
                    Amount = 500_000,
                    PaymentDate = DateTime.UtcNow.AddDays(-30),
                    PaymentMethod = "Bank Transfer",
                    Status = PaymentStatus.Approved,
                    TransactionReference = "TXN001",
                    Description = "Income tax payment Q1 2024"
                },
                new Payment
                {
                    Id = 2,
                    ClientId = 1,
                    TaxYearId = 1,
                    Amount = 150_000,
                    PaymentDate = DateTime.UtcNow.AddDays(-15),
                    PaymentMethod = "Check",
                    Status = PaymentStatus.Pending,
                    TransactionReference = "TXN002",
                    Description = "GST payment Q1 2024"
                },
                new Payment
                {
                    Id = 3,
                    ClientId = 2,
                    TaxYearId = 2,
                    Amount = 75_000,
                    PaymentDate = DateTime.UtcNow.AddDays(-5),
                    PaymentMethod = "Bank Transfer",
                    Status = PaymentStatus.Rejected,
                    TransactionReference = "TXN003",
                    Description = "Withholding tax payment"
                }
            };

            _context.Clients.AddRange(clients);
            _context.TaxYears.AddRange(taxYears);
            _context.Payments.AddRange(payments);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetPaymentsAsync_WithValidClientId_ShouldReturnClientPayments()
        {
            // Arrange
            var clientId = 1;
            var expectedPayments = new List<PaymentDto>
            {
                new PaymentDto { Id = 1, ClientId = 1, Amount = 500_000, Status = PaymentStatus.Approved },
                new PaymentDto { Id = 2, ClientId = 1, Amount = 150_000, Status = PaymentStatus.Pending }
            };

            _mapperMock.Setup(m => m.Map<IEnumerable<PaymentDto>>(It.IsAny<IEnumerable<Payment>>()))
                .Returns(expectedPayments);

            // Act
            var result = await _service.GetPaymentsAsync(clientId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(p => p.ClientId == clientId).Should().BeTrue();
        }

        [Fact]
        public async Task GetPaymentsAsync_WithInvalidClientId_ShouldReturnEmpty()
        {
            // Arrange
            var clientId = 999;
            var emptyPayments = new List<PaymentDto>();

            _mapperMock.Setup(m => m.Map<IEnumerable<PaymentDto>>(It.IsAny<IEnumerable<Payment>>()))
                .Returns(emptyPayments);

            // Act
            var result = await _service.GetPaymentsAsync(clientId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPaymentByIdAsync_WithValidId_ShouldReturnPayment()
        {
            // Arrange
            var paymentId = 1;
            var expectedPayment = new PaymentDto 
            { 
                Id = 1, 
                ClientId = 1, 
                Amount = 500_000, 
                Status = PaymentStatus.Approved,
                TransactionReference = "TXN001"
            };

            _mapperMock.Setup(m => m.Map<PaymentDto>(It.IsAny<Payment>()))
                .Returns(expectedPayment);

            // Act
            var result = await _service.GetPaymentByIdAsync(paymentId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(paymentId);
            result.TransactionReference.Should().Be("TXN001");
        }

        [Fact]
        public async Task GetPaymentByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var paymentId = 999;

            _mapperMock.Setup(m => m.Map<PaymentDto>(It.IsAny<Payment>()))
                .Returns((PaymentDto)null);

            // Act
            var result = await _service.GetPaymentByIdAsync(paymentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreatePaymentAsync_WithValidData_ShouldCreatePayment()
        {
            // Arrange
            var createPaymentDto = new CreatePaymentDto
            {
                ClientId = 1,
                TaxYearId = 1,
                Amount = 200_000,
                PaymentMethod = "Online Transfer",
                Description = "New payment test"
            };

            var payment = new Payment
            {
                ClientId = createPaymentDto.ClientId,
                TaxYearId = createPaymentDto.TaxYearId,
                Amount = createPaymentDto.Amount,
                PaymentMethod = createPaymentDto.PaymentMethod,
                Description = createPaymentDto.Description,
                Status = PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow
            };

            var expectedPaymentDto = new PaymentDto
            {
                Id = 4,
                ClientId = 1,
                Amount = 200_000,
                PaymentMethod = "Online Transfer",
                Status = PaymentStatus.Pending
            };

            _mapperMock.Setup(m => m.Map<Payment>(createPaymentDto))
                .Returns(payment);
            _mapperMock.Setup(m => m.Map<PaymentDto>(It.IsAny<Payment>()))
                .Returns(expectedPaymentDto);

            // Act
            var result = await _service.CreatePaymentAsync(createPaymentDto);

            // Assert
            result.Should().NotBeNull();
            result.ClientId.Should().Be(createPaymentDto.ClientId);
            result.Amount.Should().Be(createPaymentDto.Amount);
            result.PaymentMethod.Should().Be(createPaymentDto.PaymentMethod);

            // Verify payment was saved to database
            var savedPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Description == "New payment test");
            savedPayment.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdatePaymentStatusAsync_WithValidId_ShouldUpdateStatus()
        {
            // Arrange
            var paymentId = 2;
            var newStatus = PaymentStatus.Approved;
            var userId = "test-user";

            // Act
            var result = await _service.UpdatePaymentStatusAsync(paymentId, newStatus, userId);

            // Assert
            result.Should().BeTrue();

            var updatedPayment = await _context.Payments.FindAsync(paymentId);
            updatedPayment.Should().NotBeNull();
            updatedPayment.Status.Should().Be(newStatus);
        }

        [Fact]
        public async Task UpdatePaymentStatusAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var paymentId = 999;
            var newStatus = PaymentStatus.Approved;
            var userId = "test-user";

            // Act
            var result = await _service.UpdatePaymentStatusAsync(paymentId, newStatus, userId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetPendingPaymentsAsync_ShouldReturnOnlyPendingPayments()
        {
            // Arrange
            var expectedPayments = new List<PaymentDto>
            {
                new PaymentDto { Id = 2, ClientId = 1, Amount = 150_000, Status = PaymentStatus.Pending }
            };

            _mapperMock.Setup(m => m.Map<IEnumerable<PaymentDto>>(It.IsAny<IEnumerable<Payment>>()))
                .Returns(expectedPayments);

            // Act
            var result = await _service.GetPendingPaymentsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.All(p => p.Status == PaymentStatus.Pending).Should().BeTrue();
        }

        [Fact]
        public async Task GetPaymentsByStatusAsync_WithSpecificStatus_ShouldReturnFilteredPayments()
        {
            // Arrange
            var status = PaymentStatus.Completed;
            var expectedPayments = new List<PaymentDto>
            {
                new PaymentDto { Id = 1, ClientId = 1, Amount = 500_000, Status = PaymentStatus.Approved }
            };

            _mapperMock.Setup(m => m.Map<IEnumerable<PaymentDto>>(It.IsAny<IEnumerable<Payment>>()))
                .Returns(expectedPayments);

            // Act
            var result = await _service.GetPaymentsByStatusAsync(status);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.All(p => p.Status == status).Should().BeTrue();
        }

        [Fact]
        public async Task GetPaymentsByDateRangeAsync_WithValidRange_ShouldReturnFilteredPayments()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-20);
            var endDate = DateTime.UtcNow.AddDays(-10);
            var expectedPayments = new List<PaymentDto>
            {
                new PaymentDto { Id = 2, ClientId = 1, Amount = 150_000, Status = PaymentStatus.Pending }
            };

            _mapperMock.Setup(m => m.Map<IEnumerable<PaymentDto>>(It.IsAny<IEnumerable<Payment>>()))
                .Returns(expectedPayments);

            // Act
            var result = await _service.GetPaymentsByDateRangeAsync(startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetTotalPaymentAmountAsync_WithValidClientId_ShouldReturnCorrectTotal()
        {
            // Arrange
            var clientId = 1;

            // Act
            var result = await _service.GetTotalPaymentAmountAsync(clientId);

            // Assert
            result.Should().Be(650_000); // 500,000 + 150,000
        }

        [Fact]
        public async Task GetTotalPaymentAmountAsync_WithInvalidClientId_ShouldReturnZero()
        {
            // Arrange
            var clientId = 999;

            // Act
            var result = await _service.GetTotalPaymentAmountAsync(clientId);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task DeletePaymentAsync_WithValidId_ShouldDeletePayment()
        {
            // Arrange
            var paymentId = 3;

            // Act
            var result = await _service.DeletePaymentAsync(paymentId);

            // Assert
            result.Should().BeTrue();

            var deletedPayment = await _context.Payments.FindAsync(paymentId);
            deletedPayment.Should().BeNull();
        }

        [Fact]
        public async Task DeletePaymentAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var paymentId = 999;

            // Act
            var result = await _service.DeletePaymentAsync(paymentId);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(PaymentStatus.Pending)]
        [InlineData(PaymentStatus.Approved)]
        [InlineData(PaymentStatus.Rejected)]
        public async Task GetPaymentsByStatusAsync_WithDifferentStatuses_ShouldReturnCorrectPayments(PaymentStatus status)
        {
            // Arrange
            var expectedCount = await _context.Payments.CountAsync(p => p.Status == status);
            var expectedPayments = Enumerable.Range(1, expectedCount)
                .Select(i => new PaymentDto { Id = i, Status = status })
                .ToList();

            _mapperMock.Setup(m => m.Map<IEnumerable<PaymentDto>>(It.IsAny<IEnumerable<Payment>>()))
                .Returns(expectedPayments);

            // Act
            var result = await _service.GetPaymentsByStatusAsync(status);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(expectedCount);
            result.All(p => p.Status == status).Should().BeTrue();
        }

        [Fact]
        public async Task CreatePaymentAsync_WithInvalidClientId_ShouldThrowException()
        {
            // Arrange
            var createPaymentDto = new CreatePaymentDto
            {
                ClientId = 999, // Non-existent client
                TaxYearId = 1,
                Amount = 100_000,
                PaymentMethod = "Bank Transfer",
                Description = "Invalid client test"
            };

            var payment = new Payment
            {
                ClientId = createPaymentDto.ClientId,
                TaxYearId = createPaymentDto.TaxYearId,
                Amount = createPaymentDto.Amount
            };

            _mapperMock.Setup(m => m.Map<Payment>(createPaymentDto))
                .Returns(payment);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => 
                _service.CreatePaymentAsync(createPaymentDto));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}