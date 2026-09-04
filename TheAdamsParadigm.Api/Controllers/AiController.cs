using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Services;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly ClaudeService _claudeService;
    private readonly AnthropicSettings _anthropicSettings;

    public AiController(ClaudeService claudeService, IOptions<AnthropicSettings> anthropicSettings)
    {
        _claudeService = claudeService;
        _anthropicSettings = anthropicSettings.Value;
    }

    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new
        {
            status = _anthropicSettings.Status
        });
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
            request.Message,
            request.History);

        return Ok(new ChatResponse
        {
            Answer = answer
        });
    }
}