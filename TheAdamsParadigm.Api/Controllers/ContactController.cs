using Microsoft.AspNetCore.Mvc;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Services;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly ResendService _resendService;

    public ContactController(ResendService resendService)
    {
        _resendService = resendService;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(ContactRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.ProjectType)
            || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Name, email, project type and message are required." });
        }

        try
        {
            await _resendService.SendContactMessageAsync(request);
            return Ok(new { success = true });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = "Resend API error", details = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
