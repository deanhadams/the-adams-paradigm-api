using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Models.Calendar;
using TheAdamsParadigm.Api.Services.CloudCalendarService;

namespace TheAdamsParadigm.Api.Controllers;

// Public-facing booking endpoints. The caller (the frontend, via VITE_CLIENT_API_KEY)
// supplies which client's calendar to check — the API doesn't hold its own copy of that
// key, only the non-secret scheduling defaults (DefaultDurationMinutes/SlotIntervalMinutes).
[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly ICloudCalendarService _iCloudCalendarService;
    private readonly BookingSettings _bookingSettings;

    public BookingsController(
        ICloudCalendarService iCloudCalendarService,
        IOptions<BookingSettings> bookingSettings)
    {
        _iCloudCalendarService = iCloudCalendarService;
        _bookingSettings = bookingSettings.Value;
    }

    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new
        {
            enabled = _bookingSettings.Enabled
        });
    }

    [HttpGet("available-slots")]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] DateTime date,
        [FromQuery] string clientApiKey,
        [FromQuery] int? durationMinutes = null)
    {
        if (string.IsNullOrWhiteSpace(clientApiKey))
        {
            return BadRequest(new { error = "Client API key is required." });
        }

        var request = new BookingAvailabilityRequest
        {
            Date = date,
            DurationMinutes = durationMinutes ?? _bookingSettings.DefaultDurationMinutes,
            SlotIntervalMinutes = _bookingSettings.SlotIntervalMinutes,

            // Temporary development hours
            BusinessStart = new TimeSpan(9, 0, 0),
            BusinessEnd = new TimeSpan(17, 0, 0)
        };

        try
        {
            var slots = await _iCloudCalendarService
                .GetAvailableSlotsAsync(clientApiKey, request);

            // Don't offer slots that have already passed today, in the booking
            // calendar's own timezone (not the server's).
            var nowInBookingTimeZone = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                ICloudCalendarService.BookingTimeZone);

            var futureSlots = slots.Where(slot => slot.Start > nowInBookingTimeZone);

            return Ok(futureSlots);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (
            ex is ClientNotFoundException ||
            ex is ClientCloudCredentialsMissingException)
        {
            return StatusCode(502, new
            {
                error = "Booking calendar is currently unavailable. Please try again shortly."
            });
        }
    }
}
