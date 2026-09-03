using Microsoft.AspNetCore.Mvc;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Services;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly ClaudeService _claudeService;

    public AiController(ClaudeService claudeService)
    {
        _claudeService = claudeService;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat(
        [FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new
            {
                error = "Message cannot be empty."
            });
        }

        var answer = await _claudeService.AskClaudeAsync(
            request.Message);

        return Ok(new ChatResponse
        {
            Answer = answer
        });
    }
}