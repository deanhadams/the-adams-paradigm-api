using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Services;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ClientCredentialProtector _credentialProtector;

    public ClientsController(ApplicationDbContext context, ClientCredentialProtector credentialProtector)
    {
        _context = context;
        _credentialProtector = credentialProtector;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientResponse>>> GetAll()
    {
        var clients = await _context.Clients
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(clients.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientResponse>> GetById(int id)
    {
        var client = await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClientId == id);

        if (client == null)
        {
            return NotFound(new { error = "Client not found.", id });
        }

        return Ok(ToResponse(client));
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> Create(ClientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "Name and email are required." });
        }

        var client = new Client
        {
            Name = request.Name.Trim(),
            Website = request.Website.Trim(),
            Email = request.Email.Trim(),
            ICloudEmail = request.ICloudEmail.Trim(),
            ICloudCalendar = string.IsNullOrWhiteSpace(request.ICloudCalendar)
                ? "Bookings"
                : request.ICloudCalendar.Trim(),
            ICloudPassword = string.IsNullOrEmpty(request.ICloudPassword)
                ? string.Empty
                : _credentialProtector.Protect(request.ICloudPassword),
            ClientApiKey = GenerateApiKey(),
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = client.ClientId }, ToResponse(client));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClientResponse>> Update(int id, ClientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "Name and email are required." });
        }

        var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == id);

        if (client == null)
        {
            return NotFound(new { error = "Client not found.", id });
        }

        client.Name = request.Name.Trim();
        client.Website = request.Website.Trim();
        client.Email = request.Email.Trim();
        client.ICloudEmail = request.ICloudEmail.Trim();
        client.ICloudCalendar = string.IsNullOrWhiteSpace(request.ICloudCalendar)
            ? "Bookings"
            : request.ICloudCalendar.Trim();

        // Only touch the stored password if a new one was actually provided —
        // an empty/omitted value means "leave it as is," not "clear it."
        if (!string.IsNullOrEmpty(request.ICloudPassword))
        {
            client.ICloudPassword = _credentialProtector.Protect(request.ICloudPassword);
        }

        await _context.SaveChangesAsync();

        return Ok(ToResponse(client));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deletedCount = await _context.Clients
            .Where(c => c.ClientId == id)
            .ExecuteDeleteAsync();

        if (deletedCount == 0)
        {
            return NotFound(new { error = "Client not found.", id });
        }

        return NoContent();
    }

    private static string GenerateApiKey() =>
        "tap_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

    private static ClientResponse ToResponse(Client client) => new()
    {
        ClientId = client.ClientId,
        Name = client.Name,
        Website = client.Website,
        Email = client.Email,
        ICloudEmail = client.ICloudEmail,
        ICloudCalendar = client.ICloudCalendar,
        ClientApiKey = client.ClientApiKey,
    };
}
