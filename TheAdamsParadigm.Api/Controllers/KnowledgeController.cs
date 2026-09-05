using Microsoft.AspNetCore.Mvc;
using TheAdamsParadigm.Api.Services;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/knowledge")]
public class KnowledgeController : ControllerBase
{
    private readonly KnowledgeChunkSeedService _seedService;

    public KnowledgeController(KnowledgeChunkSeedService seedService)
    {
        _seedService = seedService;
    }

    [HttpPost("reseed")]
    public async Task<IActionResult> Reseed()
    {
        try
        {
            var count = await _seedService.ReseedAsync();
            return Ok(new { success = true, chunksInserted = count });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new
            {
                error = "Failed to reseed knowledge chunks.",
                details = ex.Message
            });
        }
    }
}
