using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace BettsTax.Web.Tests.Integration;

public class DeadlineE2ETests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _factory;
    public DeadlineE2ETests(IntegrationTestFixture factory) => _factory = factory;

    private record LoginResponse(string token, string[] roles);

    private async Task<(System.Net.Http.HttpClient client, int clientId)> AuthAndEnsureClientAsync()
    {
        var client = _factory.CreateClient();
        var login = new { Email = "admin@thebettsfirmsl.com", Password = "AdminPass123!" };
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", login);
        loginResp.EnsureSuccessStatusCode();
        var loginDto = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        loginDto.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.token);

        // ensure client exists
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
                ClientNumber = "DL-E2E-001",
                BusinessName = "Deadlines Test Co",
                ContactPerson = "Test Owner",
                Email = "deadline@example.com",
                PhoneNumber = "+23270000099",
                Address = "1 Test Way Freetown",
                ClientType = 2,
                TaxpayerCategory = 1,
                AnnualTurnover = 100000,
                TIN = "TIN-DL-1",
                Status = 0
            };
            var createClientResp = await client.PostAsJsonAsync("/api/clients", newClient);
            createClientResp.EnsureSuccessStatusCode();
            var created = JsonDocument.Parse(await createClientResp.Content.ReadAsStringAsync());
            clientId = GetClientId(created.RootElement);
        }
        return (client, clientId);
    }

    private static int GetClientId(JsonElement element)
    {
        if (TryGetPropertyCaseInsensitive(element, "clientId", out var value))
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
    public async Task Deadlines_CRUD_and_Complete_flow_works()
    {
        var (client, clientId) = await AuthAndEnsureClientAsync();

        // Create
        var due = DateTime.UtcNow.AddDays(3);
        var createReq = new {
            title = "E2E Filing",
            type = "tax-filing",
            description = "E2E create",
            dueDate = due,
            priority = "high",
            category = "GST",
            clientId = clientId,
            amount = 1000,
            taxYear = due.Year,
            taxType = "GST",
            notes = "integration"
        };
        var createResp = await client.PostAsJsonAsync("/api/deadlines", createReq);
        createResp.EnsureSuccessStatusCode();
        var createJson = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        createJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        var createdIdStr = createJson.RootElement.GetProperty("data").GetProperty("id").GetString();
        int.TryParse(createdIdStr, out var deadlineId).Should().BeTrue();

        // List should include
        var listResp = await client.GetAsync("/api/deadlines?status=all&type=all&priority=all");
        listResp.EnsureSuccessStatusCode();

        // Update
        var updateReq = new { description = "E2E updated", amount = 1200, priority = "medium" };
        var updateResp = await client.PutAsJsonAsync($"/api/deadlines/{deadlineId}", updateReq);
        updateResp.EnsureSuccessStatusCode();
        var updateJson = JsonDocument.Parse(await updateResp.Content.ReadAsStringAsync());
        updateJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

        // Complete
        var completeResp = await client.PutAsJsonAsync($"/api/deadlines/{deadlineId}/complete", new { notes = "done" });
        completeResp.EnsureSuccessStatusCode();
        var completeJson = JsonDocument.Parse(await completeResp.Content.ReadAsStringAsync());
        var status = completeJson.RootElement.GetProperty("data").GetProperty("status").GetString();
        status.Should().Be("completed");

        // Delete
        var deleteResp = await client.DeleteAsync($"/api/deadlines/{deadlineId}");
        deleteResp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Reminders_Calendar_Analytics_Export_smoke()
    {
        var (client, clientId) = await AuthAndEnsureClientAsync();

        // Create a deadline due in ~10 days
        var due = DateTime.UtcNow.AddDays(10);
        var createResp = await client.PostAsJsonAsync("/api/deadlines", new {
            title = "Reminder Test",
            type = "tax-filing",
            description = "with reminders",
            dueDate = due,
            priority = "low",
            category = "GST",
            clientId = clientId,
            taxYear = due.Year,
            taxType = "GST"
        });
        createResp.EnsureSuccessStatusCode();
        var createdJson = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var idStr = createdJson.RootElement.GetProperty("data").GetProperty("id").GetString();
        int.TryParse(idStr, out var deadlineId).Should().BeTrue();

        // Set reminders
        var setRem = await client.PostAsJsonAsync($"/api/deadlines/{deadlineId}/reminders", new { daysBefore = new[] {7,1}, methods = new[] {"sms","email"} });
        setRem.EnsureSuccessStatusCode();

        // Get reminders
        var getRem = await client.GetAsync($"/api/deadlines/{deadlineId}/reminders");
        getRem.EnsureSuccessStatusCode();
        var remJson = JsonDocument.Parse(await getRem.Content.ReadAsStringAsync());
        var remArray = remJson.RootElement.GetProperty("data").EnumerateArray().ToList();
        remArray.Count.Should().BeGreaterThan(0);
        var smsId = remArray.Select(r => r.GetProperty("id").GetString()).FirstOrDefault(x => x!.StartsWith("sms-"));
        smsId.Should().NotBeNull();

        // Update reminder status to sent
        var updRem = await client.PutAsJsonAsync($"/api/deadlines/reminders/{smsId}", new { status = "sent" });
        updRem.EnsureSuccessStatusCode();

        // Calendar (include reminders)
        var cal = await client.GetAsync($"/api/deadlines/calendar?year={due.Year}&month={due.Month}&includeReminders=true");
        cal.EnsureSuccessStatusCode();

        // Analytics
        var analytics = await client.GetAsync("/api/deadlines/analytics?timeRange=6m");
        analytics.EnsureSuccessStatusCode();

        // Export CSV
        var export = await client.GetAsync("/api/deadlines/export?format=csv");
        export.IsSuccessStatusCode.Should().BeTrue();
        export.Content.Headers.ContentType!.MediaType.Should().Contain("csv");

        // Cleanup: delete reminder and deadline
        var delRem = await client.DeleteAsync($"/api/deadlines/reminders/{smsId}");
        delRem.EnsureSuccessStatusCode();
        var delDeadline = await client.DeleteAsync($"/api/deadlines/{deadlineId}");
        delDeadline.EnsureSuccessStatusCode();
    }
}

