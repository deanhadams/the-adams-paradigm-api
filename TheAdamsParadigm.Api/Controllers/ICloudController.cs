using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheAdamsParadigm.Api.Models.Calendar;
using TheAdamsParadigm.Api.Services.CloudCalendarService;

namespace TheAdamsParadigm.Api.Controllers
{
    [Route("api/icloud")]
    [ApiController]
    public class ICloudController : ControllerBase
    {

        private readonly ICloudCalendarService _iCloudCalendarService;

        public ICloudController(ICloudCalendarService iCloudCalendarService)
        {
            _iCloudCalendarService = iCloudCalendarService;
        }

        [HttpGet("calendars")]
        public async Task<IActionResult> GetCalendars()
        {
            var calendars =
                await _iCloudCalendarService.DiscoverCalendarsAsync();

            return Ok(calendars);
        }

        [HttpGet("events")]
        public async Task<IActionResult> GetEvents(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            var events =
                await _iCloudCalendarService.GetEventsAsync(
                    from,
                    to);

            return Ok(events);
        }

        [HttpPost("events")]
        public async Task<IActionResult> CreateEvent(
            [FromBody] CreateICloudCalendarEventRequest request)
        {
            var uid =
                await _iCloudCalendarService.CreateEventAsync(request);

            return Ok(new
            {
                message = "Event created successfully.",
                uid
            });
        }

        [HttpPut("events/{uid}")]
        public async Task<IActionResult> UpdateEvent(
            string uid,
            [FromBody] UpdateICloudCalendarEventRequest request)
        {
            await _iCloudCalendarService.UpdateEventAsync(
                uid,
                request);

            return Ok(new
            {
                message = "Event updated successfully.",
                uid
            });
        }

        [HttpDelete("events/{uid}")]
        public async Task<IActionResult> DeleteEvent(string uid)
        {
            await _iCloudCalendarService.DeleteEventAsync(uid);

            return Ok(new
            {
                message = "Event deleted successfully.",
                uid
            });
        }
    }
}
