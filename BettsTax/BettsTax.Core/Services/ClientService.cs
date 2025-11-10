using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Core.Services
{
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public ClientService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ClientDto>> GetAllAsync()
        {
            var clients = await _db.Clients.AsNoTracking().ToListAsync();
            return _mapper.Map<IEnumerable<ClientDto>>(clients);
        }

        public async Task<ClientDto?> GetByIdAsync(int id)
        {
            var client = await _db.Clients.FindAsync(id);
            return client is null ? null : _mapper.Map<ClientDto>(client);
        }

        public async Task<ClientDto> CreateAsync(ClientDto dto)
        {
            var entity = _mapper.Map<Client>(dto);
            // Ensure optional FKs are not set to empty strings which violate FKs in SQLite
            if (string.IsNullOrWhiteSpace(entity.UserId)) entity.UserId = null;
            if (string.IsNullOrWhiteSpace(entity.AssignedAssociateId)) entity.AssignedAssociateId = null;
            entity.CreatedDate = DateTime.UtcNow;
            entity.UpdatedDate = DateTime.UtcNow;
            entity.ClientNumber = $"CT-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            _db.Clients.Add(entity);
            await _db.SaveChangesAsync();
            return _mapper.Map<ClientDto>(entity);
        }

        public async Task<ClientDto?> UpdateAsync(int id, ClientDto dto)
        {
            var entity = await _db.Clients.FindAsync(id);
            if (entity == null) return null;
            _mapper.Map(dto, entity);
            entity.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return _mapper.Map<ClientDto>(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _db.Clients.FindAsync(id);
            if (entity == null) return false;
            _db.Clients.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
