using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheAdamsParadigm.Api.Models.Calendar;
using TheAdamsParadigm.Api.Services.CloudCalendarService;

namespace TheAdamsParadigm.Api.Controllers
{
    [Route("api/icloud/{clientApiKey}")]
    [ApiController]
    public class ICloudController : ControllerBase
    {

        private readonly ICloudCalendarService _iCloudCalendarService;

        public ICloudController(ICloudCalendarService iCloudCalendarService)
        {
            _iCloudCalendarService = iCloudCalendarService;
        }

        [HttpGet("calendars")]
        public async Task<IActionResult> GetCalendars(string clientApiKey)
        {
            try
            {
                var calendars =
                    await _iCloudCalendarService.DiscoverCalendarsAsync(clientApiKey);

                return Ok(calendars);
            }
            catch (ClientNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ClientCloudCredentialsMissingException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("events")]
        public async Task<IActionResult> GetEvents(
            string clientApiKey,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            try
            {
                var events =
                    await _iCloudCalendarService.GetEventsAsync(
                        clientApiKey,
                        from,
                        to);

                return Ok(events);
            }
            catch (ClientNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ClientCloudCredentialsMissingException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("events")]
        public async Task<IActionResult> CreateEvent(
            string clientApiKey,
            [FromBody] CreateICloudCalendarEventRequest request)
        {
            try
            {
                var uid =
                    await _iCloudCalendarService.CreateEventAsync(clientApiKey, request);

                return Ok(new
                {
                    message = "Event created successfully.",
                    uid
                });
            }
            catch (ClientNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ClientCloudCredentialsMissingException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("events/{uid}")]
        public async Task<IActionResult> UpdateEvent(
            string clientApiKey,
            string uid,
            [FromBody] UpdateICloudCalendarEventRequest request)
        {
            try
            {
                await _iCloudCalendarService.UpdateEventAsync(
                    clientApiKey,
                    uid,
                    request);

                return Ok(new
                {
                    message = "Event updated successfully.",
                    uid
                });
            }
            catch (ClientNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ClientCloudCredentialsMissingException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("events/{uid}")]
        public async Task<IActionResult> DeleteEvent(string clientApiKey, string uid)
        {
            try
            {
                await _iCloudCalendarService.DeleteEventAsync(clientApiKey, uid);

                return Ok(new
                {
                    message = "Event deleted successfully.",
                    uid
                });
            }
            catch (ClientNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ClientCloudCredentialsMissingException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("availability")]
        public async Task<IActionResult> CheckAvailability(
            string clientApiKey,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end)
        {
            try
            {
                var result =
                    await _iCloudCalendarService.CheckAvailabilityAsync(
                        clientApiKey,
                        start,
                        end);

                return Ok(result);
            }
            catch (ClientNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ClientCloudCredentialsMissingException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("available-slots")]
        public async Task<IActionResult> GetAvailableSlots(
            string clientApiKey,
            [FromQuery] DateTime date,
            [FromQuery] int durationMinutes = 30,
            [FromQuery] int slotIntervalMinutes = 30)
        {
            var request = new BookingAvailabilityRequest
            {
                Date = date,
                DurationMinutes = durationMinutes,
                SlotIntervalMinutes = slotIntervalMinutes,

                // Temporary development hours
                BusinessStart = new TimeSpan(9, 0, 0),
                BusinessEnd = new TimeSpan(17, 0, 0)
            };

            try
            {
                var slots =
                    await _iCloudCalendarService
                        .GetAvailableSlotsAsync(clientApiKey, request);

                return Ok(slots);
            }
            catch (ClientNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ClientCloudCredentialsMissingException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
