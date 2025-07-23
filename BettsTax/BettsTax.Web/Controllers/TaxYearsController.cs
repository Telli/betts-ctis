using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/clients/{clientId}/[controller]")]
    [Authorize(Roles = "Admin,Associate,SystemAdmin")]
    public class TaxYearsController : ControllerBase
    {
        private readonly ITaxYearService _service;

        public TaxYearsController(ITaxYearService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int clientId)
        {
            var result = await _service.GetClientTaxYearsAsync(clientId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int clientId, CreateTaxYearDto dto)
        {
            dto.ClientId = clientId;
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { clientId }, created);
        }
    }
}
