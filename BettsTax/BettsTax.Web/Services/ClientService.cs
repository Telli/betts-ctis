using BettsTax.Core.DTOs.Client;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Web.Services;

/// <summary>
/// Client service implementation
/// </summary>
public class ClientService : IClientService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClientService> _logger;

    public ClientService(ApplicationDbContext context, ILogger<ClientService> logger)
    {
        _context = context;
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

        var query = _context.Clients.AsQueryable();

        // If clientId is specified, return only that client
        if (clientId.HasValue)
        {
            query = query.Where(c => c.Id == clientId.Value);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(searchTerm) ||
                c.Tin.ToLower().Contains(searchTerm) ||
                c.Industry.ToLower().Contains(searchTerm) ||
                c.AssignedTo.ToLower().Contains(searchTerm)
            );
        }

        // Apply segment filter
        if (!string.IsNullOrWhiteSpace(segment) && segment != "all")
        {
            query = query.Where(c => c.Segment == segment);
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            query = query.Where(c => c.Status == status);
        }

        var clients = await query
            .Select(c => new ClientDto
            {
                Id = c.Id,
                Name = c.Name,
                Tin = c.Tin,
                Segment = c.Segment,
                Industry = c.Industry,
                Status = c.Status,
                ComplianceScore = c.ComplianceScore,
                AssignedTo = c.AssignedTo
            })
            .ToListAsync();

        return clients;
    }

    public async Task<ClientDto?> GetClientByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving client with id {ClientId}", id);

        var client = await _context.Clients
            .Where(c => c.Id == id)
            .Select(c => new ClientDto
            {
                Id = c.Id,
                Name = c.Name,
                Tin = c.Tin,
                Segment = c.Segment,
                Industry = c.Industry,
                Status = c.Status,
                ComplianceScore = c.ComplianceScore,
                AssignedTo = c.AssignedTo
            })
            .FirstOrDefaultAsync();

        return client;
    }

    public async Task<ClientDto> CreateClientAsync(CreateClientDto dto)
    {
        _logger.LogInformation("Creating new client: {ClientName}", dto.Name);

        var client = new Models.Entities.Client
        {
            Name = dto.Name,
            Tin = dto.Tin,
            Segment = dto.Segment,
            Industry = dto.Industry,
            Status = dto.Status,
            ComplianceScore = 0, // New clients start at 0
            AssignedTo = dto.AssignedTo,
            IsDemo = false, // Real clients are not demo data
            CreatedAt = DateTime.UtcNow
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        return new ClientDto
        {
            Id = client.Id,
            Name = client.Name,
            Tin = client.Tin,
            Segment = client.Segment,
            Industry = client.Industry,
            Status = client.Status,
            ComplianceScore = client.ComplianceScore,
            AssignedTo = client.AssignedTo
        };
    }

    public async Task<ClientDto?> UpdateClientAsync(int id, UpdateClientDto dto)
    {
        _logger.LogInformation("Updating client {ClientId}", id);

        var client = await _context.Clients.FindAsync(id);
        if (client == null)
        {
            return null;
        }

        // Apply updates
        if (dto.Name != null) client.Name = dto.Name;
        if (dto.Tin != null) client.Tin = dto.Tin;
        if (dto.Segment != null) client.Segment = dto.Segment;
        if (dto.Industry != null) client.Industry = dto.Industry;
        if (dto.Status != null) client.Status = dto.Status;
        if (dto.AssignedTo != null) client.AssignedTo = dto.AssignedTo;

        client.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ClientDto
        {
            Id = client.Id,
            Name = client.Name,
            Tin = client.Tin,
            Segment = client.Segment,
            Industry = client.Industry,
            Status = client.Status,
            ComplianceScore = client.ComplianceScore,
            AssignedTo = client.AssignedTo
        };
    }
}
