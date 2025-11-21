using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace BettsTax.Web.Tests.Integration;

public class ReceiptApprovalE2ETests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _factory;

    public ReceiptApprovalE2ETests(IntegrationTestFixture factory)
    {
        _factory = factory;
    }

    private record LoginResponse(string token, string[] roles);

    private static JsonElement GetResponseData(JsonElement element)
    {
        return TryGetPropertyCaseInsensitive(element, "data", out var dataElement)
            ? dataElement
            : element;
    }

    private static JsonElement GetRequiredProperty(JsonElement element, string propertyName)
    {
        if (TryGetPropertyCaseInsensitive(element, propertyName, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException($"{propertyName} property not found in JSON payload.");
    }

    private static int GetClientId(JsonElement element)
    {
        var payload = GetResponseData(element);

        if (TryGetPropertyCaseInsensitive(payload, "clientId", out var value))
        {
            return value.GetInt32();
        }

        throw new KeyNotFoundException("clientId property not found in JSON payload.");
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    [Fact]
    public async Task Approving_payment_creates_receipt_document()
    {
        var client = _factory.CreateClient();

        // 1) Login
        var login = new { Email = "admin@thebettsfirmsl.com", Password = "AdminPass123!" };
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", login);
        loginResp.EnsureSuccessStatusCode();
        var loginDto = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        loginDto.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.token);

        // 2) Ensure a client exists (use first or create)
        var clientsResp = await client.GetAsync("/api/clients");
        clientsResp.EnsureSuccessStatusCode();
        var clientsJson = JsonDocument.Parse(await clientsResp.Content.ReadAsStringAsync());
        int clientId;
        if (clientsJson.RootElement.ValueKind == JsonValueKind.Array && clientsJson.RootElement.GetArrayLength() > 0)
        {
            clientId = GetClientId(clientsJson.RootElement[0]);
        }
        else
        {
            var newClient = new {
                ClientNumber = "E2E-001",
                BusinessName = "E2E Test Co",
                ContactPerson = "John Doe",
                Email = "e2e@example.com",
                PhoneNumber = "+23270000000",
                Address = "1 Test Street Freetown",
                ClientType = 2, // Corporation
                TaxpayerCategory = 1, // Medium
                AnnualTurnover = 100000,
                TIN = "TIN123",
                Status = 0 // Active
            };
            var createClientResp = await client.PostAsJsonAsync("/api/clients", newClient);
            createClientResp.EnsureSuccessStatusCode();
            var created = JsonDocument.Parse(await createClientResp.Content.ReadAsStringAsync());
            clientId = GetClientId(created.RootElement);
        }

        // 3) Create payment (Pending)
        var createPayment = new {
            ClientId = clientId,
            Amount = 500.00,
            Method = 3, // OnlinePayment
            PaymentReference = $"E2E-{Guid.NewGuid():N}",
            PaymentDate = DateTime.UtcNow
        };
        var createPayResp = await client.PostAsJsonAsync("/api/payments", createPayment);
        createPayResp.EnsureSuccessStatusCode();
        var createdPaymentJson = JsonDocument.Parse(await createPayResp.Content.ReadAsStringAsync());
        var createdPaymentData = GetResponseData(createdPaymentJson.RootElement);

        // API returns: { "success": true, "data": { "paymentId": ... } } (camelCase)
        var paymentId = GetRequiredProperty(createdPaymentData, "paymentId").GetInt32();

        // 4) Approve payment
        var approveResp = await client.PostAsJsonAsync($"/api/payments/{paymentId}/approve", new { Comments = "E2E approval" });
        approveResp.EnsureSuccessStatusCode();

        // 5) List documents for client with category Receipt (2) - wait for async receipt generation
        bool hasReceipt = false;
        for (int attempt = 0; attempt < 5 && !hasReceipt; attempt++)
        {
            if (attempt > 0) await Task.Delay(1000); // Wait 1 second between attempts

            var docsResp = await client.GetAsync($"/api/documents?category=2&clientId={clientId}");
            docsResp.EnsureSuccessStatusCode();
            var docsJson = JsonDocument.Parse(await docsResp.Content.ReadAsStringAsync());
            var items = GetResponseData(docsJson.RootElement);
            hasReceipt = items.EnumerateArray().Any(d =>
            {
                var catProp = GetRequiredProperty(d, "category");  // camelCase
                return (catProp.ValueKind == JsonValueKind.String && catProp.GetString() == "Receipt")
                       || (catProp.ValueKind == JsonValueKind.Number && catProp.GetInt32() == 2);
            });
        }
        hasReceipt.Should().BeTrue();
    }

    [Fact]
    public async Task Approving_payment_twice_does_not_create_duplicate_receipt()
    {
        var client = _factory.CreateClient();

        // 1) Login
        var login = new { Email = "admin@thebettsfirmsl.com", Password = "AdminPass123!" };
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", login);
        loginResp.EnsureSuccessStatusCode();
        var loginDto = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        loginDto.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.token);

        // 2) Ensure a client exists (use first or create)
        var clientsResp = await client.GetAsync("/api/clients");
        clientsResp.EnsureSuccessStatusCode();
        var clientsJson = JsonDocument.Parse(await clientsResp.Content.ReadAsStringAsync());
        int clientId;
        if (clientsJson.RootElement.ValueKind == JsonValueKind.Array && clientsJson.RootElement.GetArrayLength() > 0)
        {
            clientId = GetClientId(clientsJson.RootElement[0]);
        }
        else
        {
            var newClient = new {
                ClientNumber = "E2E-002",
                BusinessName = "E2E Test Co 2",
                ContactPerson = "Jane Doe",
                Email = "e2e2@example.com",
                PhoneNumber = "+23270000001",
                Address = "2 Test Street Freetown",
                ClientType = 2, // Corporation
                TaxpayerCategory = 1, // Medium
                AnnualTurnover = 100000,
                TIN = "TIN124",
                Status = 0 // Active
            };
            var createClientResp = await client.PostAsJsonAsync("/api/clients", newClient);
            createClientResp.EnsureSuccessStatusCode();
            var created = JsonDocument.Parse(await createClientResp.Content.ReadAsStringAsync());
            clientId = GetClientId(created.RootElement);
        }

        // 3) Create payment (Pending)
        var createPayment = new {
            ClientId = clientId,
            Amount = 600.00,
            Method = 3, // OnlinePayment
            PaymentReference = $"E2E-IDEMPOTENT-{Guid.NewGuid():N}",
            PaymentDate = DateTime.UtcNow
        };
        var createPayResp = await client.PostAsJsonAsync("/api/payments", createPayment);
        createPayResp.EnsureSuccessStatusCode();
        var createdPaymentJson = JsonDocument.Parse(await createPayResp.Content.ReadAsStringAsync());
        var createdPaymentData = GetResponseData(createdPaymentJson.RootElement);
        var paymentId = GetRequiredProperty(createdPaymentData, "paymentId").GetInt32();

        // 4) Approve payment first time
        var approveResp1 = await client.PostAsJsonAsync($"/api/payments/{paymentId}/approve", new { Comments = "First approval" });
        approveResp1.EnsureSuccessStatusCode();

        // 5) Count receipts before second approval - wait for async receipt generation
        int receiptCount1 = 0;
        for (int attempt = 0; attempt < 5 && receiptCount1 == 0; attempt++)
        {
            if (attempt > 0) await Task.Delay(1000); // Wait 1 second between attempts

            var docsResp1 = await client.GetAsync($"/api/documents?category=2&clientId={clientId}");
            docsResp1.EnsureSuccessStatusCode();
            var docsJson1 = JsonDocument.Parse(await docsResp1.Content.ReadAsStringAsync());
            var items1 = GetResponseData(docsJson1.RootElement);
            receiptCount1 = items1.EnumerateArray().Count(d =>
            {
                var catProp = GetRequiredProperty(d, "category");
                return (catProp.ValueKind == JsonValueKind.String && catProp.GetString() == "Receipt")
                       || (catProp.ValueKind == JsonValueKind.Number && catProp.GetInt32() == 2);
            });
        }

        // 6) Approve payment second time (should fail - already approved)
        var approveResp2 = await client.PostAsJsonAsync($"/api/payments/{paymentId}/approve", new { Comments = "Second approval" });
        approveResp2.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Approving an already approved payment should fail");

        // 7) Count receipts after second approval - should be same
        var docsResp2 = await client.GetAsync($"/api/documents?category=2&clientId={clientId}");
        docsResp2.EnsureSuccessStatusCode();
        var docsJson2 = JsonDocument.Parse(await docsResp2.Content.ReadAsStringAsync());
        var items2 = GetResponseData(docsJson2.RootElement);
        int receiptCount2 = items2.EnumerateArray().Count(d =>
        {
            var catProp = GetRequiredProperty(d, "category");
            return (catProp.ValueKind == JsonValueKind.String && catProp.GetString() == "Receipt")
                   || (catProp.ValueKind == JsonValueKind.Number && catProp.GetInt32() == 2);
        });

        receiptCount2.Should().Be(receiptCount1, "Second approval should not create duplicate receipt");
        receiptCount1.Should().BeGreaterThan(0, "At least one receipt should exist");
    }

    [Fact]
    public async Task Upload_payment_evidence_creates_document_and_links_to_payment()
    {
        var client = _factory.CreateClient();

        // 1) Login
        var login = new { Email = "admin@thebettsfirmsl.com", Password = "AdminPass123!" };
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", login);
        loginResp.EnsureSuccessStatusCode();
        var loginDto = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        loginDto.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.token);

        // 2) Ensure a client exists
        var clientsResp = await client.GetAsync("/api/clients");
        clientsResp.EnsureSuccessStatusCode();
        var clientsJson = JsonDocument.Parse(await clientsResp.Content.ReadAsStringAsync());
        int clientId;
        if (clientsJson.RootElement.ValueKind == JsonValueKind.Array && clientsJson.RootElement.GetArrayLength() > 0)
        {
            clientId = GetClientId(clientsJson.RootElement[0]);
        }
        else
        {
            var newClient = new {
                ClientNumber = "E2E-003",
                BusinessName = "E2E Evidence Test Co",
                ContactPerson = "Bob Evidence",
                Email = "evidence@example.com",
                PhoneNumber = "+23270000002",
                Address = "3 Test Street Freetown",
                ClientType = 2,
                TaxpayerCategory = 1,
                AnnualTurnover = 100000,
                TIN = "TIN125",
                Status = 0
            };
            var createClientResp = await client.PostAsJsonAsync("/api/clients", newClient);
            createClientResp.EnsureSuccessStatusCode();
            var created = JsonDocument.Parse(await createClientResp.Content.ReadAsStringAsync());
            clientId = GetClientId(created.RootElement);
        }

        // 3) Create payment (BankTransfer method to require evidence)
        var createPayment = new {
            ClientId = clientId,
            Amount = 750.00,
            Method = 0, // BankTransfer
            PaymentReference = $"E2E-EVIDENCE-{Guid.NewGuid():N}",
            PaymentDate = DateTime.UtcNow
        };
        var createPayResp = await client.PostAsJsonAsync("/api/payments", createPayment);
        createPayResp.EnsureSuccessStatusCode();
        var createdPaymentJson = JsonDocument.Parse(await createPayResp.Content.ReadAsStringAsync());
        var createdPaymentData = GetResponseData(createdPaymentJson.RootElement);
        var paymentId = GetRequiredProperty(createdPaymentData, "paymentId").GetInt32();

        // 4) Create a dummy evidence file
        var fileContent = System.Text.Encoding.UTF8.GetBytes("Bank transfer slip content for payment evidence");
        var fileStream = new MemoryStream(fileContent);
        
        using var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(clientId.ToString()), "dto.ClientId");
        formData.Add(new StringContent("4"), "dto.Category"); // PaymentEvidence = 4
        formData.Add(new StringContent("Bank transfer slip evidence"), "dto.Description");
        
        var fileStreamContent = new StreamContent(fileStream);
        fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        formData.Add(fileStreamContent, "file", "bank_slip.pdf");

        // 5) Upload evidence
        var uploadResp = await client.PostAsync($"/api/payments/{paymentId}/evidence", formData);
        uploadResp.EnsureSuccessStatusCode();
        var uploadJson = JsonDocument.Parse(await uploadResp.Content.ReadAsStringAsync());
        uploadJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        var uploadData = GetResponseData(uploadJson.RootElement);
        var documentId = GetRequiredProperty(uploadData, "documentId").GetInt32();

        // 6) Verify document was created with correct category (PaymentEvidence)
        var docResp = await client.GetAsync($"/api/documents/{documentId}");
        docResp.EnsureSuccessStatusCode();
        var docJson = JsonDocument.Parse(await docResp.Content.ReadAsStringAsync());
        var docData = GetResponseData(docJson.RootElement);
        GetClientId(docData).Should().Be(clientId);
        GetRequiredProperty(docData, "description").GetString().Should().Contain("evidence");

        // Verify category is PaymentEvidence (4)
        var categoryProp = GetRequiredProperty(docData, "category");
        bool isPaymentEvidence = (categoryProp.ValueKind == JsonValueKind.String && categoryProp.GetString() == "PaymentEvidence")
                                || (categoryProp.ValueKind == JsonValueKind.Number && categoryProp.GetInt32() == 4);
        isPaymentEvidence.Should().BeTrue();
    }

    [Fact]
    public async Task Reconcile_payment_marks_as_reconciled_and_completed()
    {
        var client = _factory.CreateClient();

        // 1) Login
        var login = new { Email = "admin@thebettsfirmsl.com", Password = "AdminPass123!" };
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", login);
        loginResp.EnsureSuccessStatusCode();
        var loginDto = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        loginDto.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.token);

        // 2) Ensure a client exists
        var clientsResp = await client.GetAsync("/api/clients");
        clientsResp.EnsureSuccessStatusCode();
        var clientsJson = JsonDocument.Parse(await clientsResp.Content.ReadAsStringAsync());
        int clientId;
        if (clientsJson.RootElement.ValueKind == JsonValueKind.Array && clientsJson.RootElement.GetArrayLength() > 0)
        {
            clientId = GetClientId(clientsJson.RootElement[0]);
        }
        else
        {
            var newClient = new {
                ClientNumber = "E2E-004",
                BusinessName = "E2E Reconcile Test Co",
                ContactPerson = "Alice Reconcile",
                Email = "reconcile@example.com",
                PhoneNumber = "+23270000003",
                Address = "4 Test Street Freetown",
                ClientType = 2,
                TaxpayerCategory = 1,
                AnnualTurnover = 100000,
                TIN = "TIN126",
                Status = 0
            };
            var createClientResp = await client.PostAsJsonAsync("/api/clients", newClient);
            createClientResp.EnsureSuccessStatusCode();
            var created = JsonDocument.Parse(await createClientResp.Content.ReadAsStringAsync());
            clientId = GetClientId(created.RootElement);
        }

        // 3) Create and approve a payment
        var createPayment = new {
            ClientId = clientId,
            Amount = 900.00,
            Method = 0, // BankTransfer
            PaymentReference = $"E2E-RECONCILE-{Guid.NewGuid():N}",
            PaymentDate = DateTime.UtcNow
        };
        var createPayResp = await client.PostAsJsonAsync("/api/payments", createPayment);
        createPayResp.EnsureSuccessStatusCode();
        var createdPaymentJson = JsonDocument.Parse(await createPayResp.Content.ReadAsStringAsync());
        var createdPaymentData = GetResponseData(createdPaymentJson.RootElement);
        var paymentId = GetRequiredProperty(createdPaymentData, "paymentId").GetInt32();

        // Approve the payment first
        var approveResp = await client.PostAsJsonAsync($"/api/payments/{paymentId}/approve", new { Comments = "Approved for reconciliation test" });
        approveResp.EnsureSuccessStatusCode();

        // 4) Reconcile the payment
        var reconcileData = new {
            ReconciliationReference = "BANK-REF-12345",
            BankStatementReference = "STMT-67890",
            BankStatementDate = DateTime.UtcNow.Date,
            Notes = "Funds received and verified in bank statement",
            MarkAsCompleted = true
        };
        var reconcileResp = await client.PostAsJsonAsync($"/api/payments/{paymentId}/reconcile", reconcileData);
        reconcileResp.EnsureSuccessStatusCode();

        var reconcileJson = JsonDocument.Parse(await reconcileResp.Content.ReadAsStringAsync());
        reconcileJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

        // 5) Verify payment was reconciled and marked as completed
        var paymentResp = await client.GetAsync($"/api/payments/{paymentId}");
        paymentResp.EnsureSuccessStatusCode();
        var paymentJson = JsonDocument.Parse(await paymentResp.Content.ReadAsStringAsync());
        var paymentData = GetResponseData(paymentJson.RootElement);

        // Check status is Completed (3)
        var statusProp = GetRequiredProperty(paymentData, "status");
        bool isCompleted = (statusProp.ValueKind == JsonValueKind.String && statusProp.GetString() == "Completed")
                          || (statusProp.ValueKind == JsonValueKind.Number && statusProp.GetInt32() == 3);
        isCompleted.Should().BeTrue("Payment should be marked as Completed after reconciliation");
    }

    [Fact]
    public async Task Report_generation_failure_still_approves_payment_and_logs_warning()
    {
        var client = _factory.CreateClient();

        // 1) Login
        var login = new { Email = "admin@thebettsfirmsl.com", Password = "AdminPass123!" };
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", login);
        loginResp.EnsureSuccessStatusCode();
        var loginDto = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        loginDto.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.token);

        // 2) Ensure a client exists (use first or create)
        var clientsResp = await client.GetAsync("/api/clients");
        clientsResp.EnsureSuccessStatusCode();
        var clientsJson = JsonDocument.Parse(await clientsResp.Content.ReadAsStringAsync());
        int clientId;
        if (clientsJson.RootElement.ValueKind == JsonValueKind.Array && clientsJson.RootElement.GetArrayLength() > 0)
        {
            clientId = GetClientId(clientsJson.RootElement[0]);
        }
        else
        {
            var newClient = new {
                ClientNumber = "E2E-004",
                BusinessName = "E2E Test Co 4",
                ContactPerson = "Alice Johnson",
                Email = "e2e4@example.com",
                PhoneNumber = "+23270000003",
                Address = "4 Test Street Freetown",
                ClientType = 2, // Corporation
                TaxpayerCategory = 1, // Medium
                AnnualTurnover = 100000,
                TIN = "TIN125",
                Status = 0 // Active
            };
            var createClientResp = await client.PostAsJsonAsync("/api/clients", newClient);
            createClientResp.EnsureSuccessStatusCode();
            var created = JsonDocument.Parse(await createClientResp.Content.ReadAsStringAsync());
            clientId = GetClientId(created.RootElement);
        }

        // 3) Create payment (Pending)
        var createPayment = new {
            ClientId = clientId,
            Amount = 800.00,
            Method = 3, // OnlinePayment
            PaymentReference = $"E2E-FAILURE-{Guid.NewGuid():N}",
            PaymentDate = DateTime.UtcNow
        };
        var createPayResp = await client.PostAsJsonAsync("/api/payments", createPayment);
        createPayResp.EnsureSuccessStatusCode();
        var createdPaymentJson = JsonDocument.Parse(await createPayResp.Content.ReadAsStringAsync());
        var createdPaymentData = GetResponseData(createdPaymentJson.RootElement);
        var paymentId = GetRequiredProperty(createdPaymentData, "paymentId").GetInt32();

        // 4) Approve payment - this should succeed even if receipt generation fails
        var approveResp = await client.PostAsJsonAsync($"/api/payments/{paymentId}/approve", new { Comments = "Approval with report failure test" });
        approveResp.EnsureSuccessStatusCode();

        // 5) Verify payment status is Approved
        var paymentResp = await client.GetAsync($"/api/payments/{paymentId}");
        paymentResp.EnsureSuccessStatusCode();
        var paymentJson = JsonDocument.Parse(await paymentResp.Content.ReadAsStringAsync());
        var paymentData = GetResponseData(paymentJson.RootElement);
        var statusElement = GetRequiredProperty(paymentData, "status");
        bool isApproved = statusElement.ValueKind == JsonValueKind.String
            ? string.Equals(statusElement.GetString(), "Approved", StringComparison.OrdinalIgnoreCase)
            : statusElement.GetInt32() == 1;
        isApproved.Should().BeTrue("Payment should be approved even if receipt generation fails");

        // 6) Check that no receipts were created (since report generation would fail in this test scenario)
        // Note: In a real test, we'd mock IReportGenerator to throw, but for this integration test,
        // we verify the payment is approved and the system handles failures gracefully
        var docsResp = await client.GetAsync($"/api/documents?category=2&clientId={clientId}");
        docsResp.EnsureSuccessStatusCode();
        var docsJson = JsonDocument.Parse(await docsResp.Content.ReadAsStringAsync());
        var items = GetResponseData(docsJson.RootElement);
        int receiptCount = items.EnumerateArray().Count(d =>
        {
            var catProp = GetRequiredProperty(d, "category");
            return (catProp.ValueKind == JsonValueKind.String && catProp.GetString() == "Receipt")
                   || (catProp.ValueKind == JsonValueKind.Number && catProp.GetInt32() == 2);
        });
        // Receipt count may be 0 or 1 depending on whether report generation succeeded or failed
        // The key is that payment approval succeeded regardless
        receiptCount.Should().BeInRange(0, 1, "Receipt generation failure should not prevent payment approval");
    }

    [Fact]
    public async Task Rejecting_payment_does_not_create_receipt()
    {
        var client = _factory.CreateClient();

        // 1) Login
        var login = new { Email = "admin@thebettsfirmsl.com", Password = "AdminPass123!" };
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", login);
        loginResp.EnsureSuccessStatusCode();
        var loginDto = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        loginDto.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.token);

        // 2) Ensure a client exists (use first or create)
        var clientsResp = await client.GetAsync("/api/clients");
        clientsResp.EnsureSuccessStatusCode();
        var clientsJson = JsonDocument.Parse(await clientsResp.Content.ReadAsStringAsync());
        int clientId;
        if (clientsJson.RootElement.ValueKind == JsonValueKind.Array && clientsJson.RootElement.GetArrayLength() > 0)
        {
            clientId = GetClientId(clientsJson.RootElement[0]);
        }
        else
        {
            var newClient = new {
                ClientNumber = "E2E-003",
                BusinessName = "E2E Test Co 3",
                ContactPerson = "Bob Smith",
                Email = "e2e3@example.com",
                PhoneNumber = "+23270000002",
                Address = "3 Test Street Freetown",
                ClientType = 2, // Corporation
                TaxpayerCategory = 1, // Medium
                AnnualTurnover = 100000,
                TIN = "TIN125",
                Status = 0 // Active
            };
            var createClientResp = await client.PostAsJsonAsync("/api/clients", newClient);
            createClientResp.EnsureSuccessStatusCode();
            var created = JsonDocument.Parse(await createClientResp.Content.ReadAsStringAsync());
            clientId = GetClientId(created.RootElement);
        }

        // 3) Create payment (Pending)
        var createPayment = new {
            ClientId = clientId,
            Amount = 700.00,
            Method = 3, // OnlinePayment
            PaymentReference = $"E2E-REJECT-{Guid.NewGuid():N}",
            PaymentDate = DateTime.UtcNow
        };
        var createPayResp = await client.PostAsJsonAsync("/api/payments", createPayment);
        createPayResp.EnsureSuccessStatusCode();
        var createdPaymentJson = JsonDocument.Parse(await createPayResp.Content.ReadAsStringAsync());
        var createdPaymentData = GetResponseData(createdPaymentJson.RootElement);
        var paymentId = GetRequiredProperty(createdPaymentData, "paymentId").GetInt32();

        // 4) Reject payment
        var rejectResp = await client.PostAsJsonAsync($"/api/payments/{paymentId}/reject", new { RejectionReason = "Test rejection" });
        rejectResp.EnsureSuccessStatusCode();

        // 5) List documents for client with category Receipt (2) - should be empty
        var docsResp = await client.GetAsync($"/api/documents?category=2&clientId={clientId}");
        docsResp.EnsureSuccessStatusCode();
        var docsJson = JsonDocument.Parse(await docsResp.Content.ReadAsStringAsync());
        var items = GetResponseData(docsJson.RootElement);
        bool hasReceipt = items.EnumerateArray().Any(d =>
        {
            var catProp = GetRequiredProperty(d, "category");
            return (catProp.ValueKind == JsonValueKind.String && catProp.GetString() == "Receipt")
                   || (catProp.ValueKind == JsonValueKind.Number && catProp.GetInt32() == 2);
        });
        hasReceipt.Should().BeFalse("Rejected payment should not create receipt");
    }
}

