using BettsTax.Core.DTOs.Client;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Web.Services;

/// <summary>
/// Client service implementation
/// </summary>
public class ClientService : IClientService
{
    private readonly ILogger<ClientService> _logger;

    public ClientService(ILogger<ClientService> logger)
    {
        _logger = logger;
    }

    public async Task<List<ClientDto>> GetClientsAsync(
        string? searchTerm = null,
        string? segment = null,
        string? status = null,
        int? clientId = null)
    {
        _logger.LogInformation("Retrieving clients with filters: search={Search}, segment={Segment}, status={Status}, clientId={ClientId}",
            searchTerm, segment, status, clientId);

        await Task.CompletedTask;

        // Mock data - replace with actual database query
        var clients = new List<ClientDto>
        {
            new() { Id = 1, Name = "Sierra Leone Breweries Ltd", Tin = "TIN001234567", Segment = "Large", Industry = "Manufacturing", Status = "Active", ComplianceScore = 95, AssignedTo = "John Kamara" },
            new() { Id = 2, Name = "Standard Chartered Bank SL", Tin = "TIN002345678", Segment = "Large", Industry = "Financial Services", Status = "Active", ComplianceScore = 98, AssignedTo = "Sarah Conteh" },
            new() { Id = 3, Name = "Orange Sierra Leone", Tin = "TIN003456789", Segment = "Large", Industry = "Telecommunications", Status = "Active", ComplianceScore = 92, AssignedTo = "Mohamed Sesay" },
            new() { Id = 4, Name = "Rokel Commercial Bank", Tin = "TIN004567890", Segment = "Medium", Industry = "Financial Services", Status = "Active", ComplianceScore = 88, AssignedTo = "Fatmata Koroma" },
            new() { Id = 5, Name = "Freetown Terminal Ltd", Tin = "TIN005678901", Segment = "Medium", Industry = "Logistics", Status = "Under Review", ComplianceScore = 75, AssignedTo = "Abdul Rahman" }
        };

        // If clientId is specified, return only that client
        if (clientId.HasValue)
        {
            clients = clients.Where(c => c.Id == clientId.Value).ToList();
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            clients = clients.Where(c =>
                c.Name.ToLower().Contains(searchTerm) ||
                c.Tin.ToLower().Contains(searchTerm) ||
                c.Industry.ToLower().Contains(searchTerm) ||
                c.AssignedTo.ToLower().Contains(searchTerm)
            ).ToList();
        }

        if (!string.IsNullOrWhiteSpace(segment) && segment != "all")
        {
            clients = clients.Where(c => c.Segment.Equals(segment, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            clients = clients.Where(c => c.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return clients;
    }

    public async Task<ClientDto?> GetClientByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving client with id {ClientId}", id);

        await Task.CompletedTask;

        // Mock data - replace with actual database query
        var clients = new List<ClientDto>
        {
            new() { Id = 1, Name = "Sierra Leone Breweries Ltd", Tin = "TIN001234567", Segment = "Large", Industry = "Manufacturing", Status = "Active", ComplianceScore = 95, AssignedTo = "John Kamara" },
            new() { Id = 2, Name = "Standard Chartered Bank SL", Tin = "TIN002345678", Segment = "Large", Industry = "Financial Services", Status = "Active", ComplianceScore = 98, AssignedTo = "Sarah Conteh" },
            new() { Id = 3, Name = "Orange Sierra Leone", Tin = "TIN003456789", Segment = "Large", Industry = "Telecommunications", Status = "Active", ComplianceScore = 92, AssignedTo = "Mohamed Sesay" },
            new() { Id = 4, Name = "Rokel Commercial Bank", Tin = "TIN004567890", Segment = "Medium", Industry = "Financial Services", Status = "Active", ComplianceScore = 88, AssignedTo = "Fatmata Koroma" },
            new() { Id = 5, Name = "Freetown Terminal Ltd", Tin = "TIN005678901", Segment = "Medium", Industry = "Logistics", Status = "Under Review", ComplianceScore = 75, AssignedTo = "Abdul Rahman" }
        };

        return clients.FirstOrDefault(c => c.Id == id);
    }

    public async Task<ClientDto> CreateClientAsync(CreateClientDto dto)
    {
        _logger.LogInformation("Creating new client: {ClientName}", dto.Name);

        await Task.CompletedTask;

        // Mock implementation - replace with actual database insert
        var newClient = new ClientDto
        {
            Id = new Random().Next(100, 999),
            Name = dto.Name,
            Tin = dto.Tin,
            Segment = dto.Segment,
            Industry = dto.Industry,
            Status = dto.Status,
            ComplianceScore = 0, // New clients start at 0
            AssignedTo = dto.AssignedTo
        };

        return newClient;
    }

    public async Task<ClientDto?> UpdateClientAsync(int id, UpdateClientDto dto)
    {
        _logger.LogInformation("Updating client {ClientId}", id);

        await Task.CompletedTask;

        // Mock implementation - replace with actual database update
        var existingClient = await GetClientByIdAsync(id);
        if (existingClient == null)
        {
            return null;
        }

        // Apply updates
        if (dto.Name != null) existingClient.Name = dto.Name;
        if (dto.Tin != null) existingClient.Tin = dto.Tin;
        if (dto.Segment != null) existingClient.Segment = dto.Segment;
        if (dto.Industry != null) existingClient.Industry = dto.Industry;
        if (dto.Status != null) existingClient.Status = dto.Status;
        if (dto.AssignedTo != null) existingClient.AssignedTo = dto.AssignedTo;

        return existingClient;
    }
}
