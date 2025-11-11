using BettsTax.Core.DTOs.Client;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Client service interface
/// </summary>
public interface IClientService
{
    Task<List<ClientDto>> GetClientsAsync(string? searchTerm = null, string? segment = null, string? status = null, int? clientId = null);
    Task<ClientDto?> GetClientByIdAsync(int id);
    Task<ClientDto> CreateClientAsync(CreateClientDto dto);
    Task<ClientDto?> UpdateClientAsync(int id, UpdateClientDto dto);
}
