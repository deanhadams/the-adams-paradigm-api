using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Controllers
{
    [ApiController]
    [Route("api/services")]
    public class ServicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(ApplicationDbContext context, ILogger<ServicesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("get-all")]
        public async Task<ActionResult<IEnumerable<Service>>> GetAll()
        {
            _logger.LogInformation("Fetching all services");
            var services = await _context.Services.AsNoTracking().ToListAsync();
            return Ok(services);
        }
    }
}
