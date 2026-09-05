using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Models.Calendar;
using TheAdamsParadigm.Api.Services.CloudCalendarService;

namespace TheAdamsParadigm.Api.Controllers;

// Public-facing booking endpoints for this site's own calendar. Unlike ICloudController,
// these don't take a clientApiKey from the caller — the site only ever books against its
// own calendar, so the key stays server-side (BookingSettings) instead of round-tripping
// through the browser.
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

    [HttpGet("available-slots")]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] DateTime date,
        [FromQuery] int? durationMinutes = null)
    {
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
                .GetAvailableSlotsAsync(_bookingSettings.ClientApiKey, request);

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
