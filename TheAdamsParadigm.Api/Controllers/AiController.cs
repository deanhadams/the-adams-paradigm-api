using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Services;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly ClaudeService _claudeService;
    private readonly AnthropicSettings _anthropicSettings;
    private readonly ApplicationDbContext _dbContext;

    public AiController(
        ClaudeService claudeService,
        IOptions<AnthropicSettings> anthropicSettings,
        ApplicationDbContext dbContext)
    {
        _claudeService = claudeService;
        _anthropicSettings = anthropicSettings.Value;
        _dbContext = dbContext;
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
        [FromBody] ChatRequest request,
        [FromHeader(Name = "X-Chat-User-Id")] string? chatUserId)
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
            request.History,
            chatUserId);

        return Ok(new ChatResponse
        {
            Answer = answer
        });
    }

    [HttpGet("memory")]
    public async Task<ActionResult<IEnumerable<UserMemorySummary>>> GetMemory(
        [FromHeader(Name = "X-Chat-User-Id")] string? chatUserId)
    {
        if (string.IsNullOrWhiteSpace(chatUserId))
        {
            return Ok(Array.Empty<UserMemorySummary>());
        }

        var memories = await _dbContext.UserMemories
            .Where(m => m.ChatUserId == chatUserId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new UserMemorySummary
            {
                Category = m.Category,
                Text = m.Text,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return Ok(memories);
    }

    [HttpDelete("memory")]
    public async Task<IActionResult> DeleteMemory(
        [FromHeader(Name = "X-Chat-User-Id")] string? chatUserId)
    {
        if (string.IsNullOrWhiteSpace(chatUserId))
        {
            return BadRequest(new
            {
                error = "X-Chat-User-Id header is required."
            });
        }

        var deletedCount = await _dbContext.UserMemories
            .Where(m => m.ChatUserId == chatUserId)
            .ExecuteDeleteAsync();

        return Ok(new
        {
            success = true,
            deletedCount
        });
    }
}