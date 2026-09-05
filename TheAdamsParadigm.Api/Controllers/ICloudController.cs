using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheAdamsParadigm.Api.Models.Calendar;
using TheAdamsParadigm.Api.Services.CloudCalendarService;

namespace TheAdamsParadigm.Api.Controllers
{
    [Route("api/icloud/{clientId:int}")]
    [ApiController]
    public class ICloudController : ControllerBase
    {

        private readonly ICloudCalendarService _iCloudCalendarService;

        public ICloudController(ICloudCalendarService iCloudCalendarService)
        {
            _iCloudCalendarService = iCloudCalendarService;
        }

        [HttpGet("calendars")]
        public async Task<IActionResult> GetCalendars(int clientId)
        {
            try
            {
                var calendars =
                    await _iCloudCalendarService.DiscoverCalendarsAsync(clientId);

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
            int clientId,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            try
            {
                var events =
                    await _iCloudCalendarService.GetEventsAsync(
                        clientId,
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
            int clientId,
            [FromBody] CreateICloudCalendarEventRequest request)
        {
            try
            {
                var uid =
                    await _iCloudCalendarService.CreateEventAsync(clientId, request);

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
            int clientId,
            string uid,
            [FromBody] UpdateICloudCalendarEventRequest request)
        {
            try
            {
                await _iCloudCalendarService.UpdateEventAsync(
                    clientId,
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
        public async Task<IActionResult> DeleteEvent(int clientId, string uid)
        {
            try
            {
                await _iCloudCalendarService.DeleteEventAsync(clientId, uid);

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
            int clientId,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end)
        {
            try
            {
                var result =
                    await _iCloudCalendarService.CheckAvailabilityAsync(
                        clientId,
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
            int clientId,
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
                        .GetAvailableSlotsAsync(clientId, request);

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
