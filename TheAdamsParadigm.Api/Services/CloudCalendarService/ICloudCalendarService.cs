using System.Xml.Linq;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Models.Calendar;

namespace TheAdamsParadigm.Api.Services.CloudCalendarService
{
    public class ICloudCalendarService
    {
        private readonly HttpClient _httpClient;
        private readonly ICloudSettings _settings;

        public ICloudCalendarService(
            HttpClient httpClient,
            ICloudSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        public async Task<bool> TestConnectionAsync()
        {
            var credentials = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{_settings.Username}:{_settings.Password}"));

            using var request = new HttpRequestMessage(
                new HttpMethod("PROPFIND"),
                _settings.ServerUrl);

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    credentials);

            request.Headers.Add("Depth", "0");

            var xml = """
              <?xml version="1.0" encoding="utf-8" ?>
              <D:propfind xmlns:D="DAV:">
                  <D:prop>
                      <D:current-user-principal />
                  </D:prop>
              </D:propfind>
              """;

            request.Content = new StringContent(
                xml,
                System.Text.Encoding.UTF8,
                "application/xml");

            using var response = await _httpClient.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"iCloud Status: {(int)response.StatusCode}");
            Console.WriteLine($"iCloud Response: {responseBody}");

            return response.IsSuccessStatusCode;
        }

        public async Task<List<ICloudCalendar>> DiscoverCalendarsAsync()
        {
            var credentials = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{_settings.Username}:{_settings.Password}"));

            // =========================================================
            // STEP 1 — Discover current-user-principal
            // =========================================================

            using var principalRequest = new HttpRequestMessage(
                new HttpMethod("PROPFIND"),
                _settings.ServerUrl);

            principalRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    credentials);

            principalRequest.Headers.Add("Depth", "0");

            var principalXml = """
        <?xml version="1.0" encoding="utf-8" ?>
        <D:propfind xmlns:D="DAV:">
            <D:prop>
                <D:current-user-principal />
            </D:prop>
        </D:propfind>
        """;

            principalRequest.Content = new StringContent(
                principalXml,
                System.Text.Encoding.UTF8,
                "application/xml");

            using var principalResponse =
                await _httpClient.SendAsync(principalRequest);

            var principalResponseBody =
                await principalResponse.Content.ReadAsStringAsync();

            if (!principalResponse.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Principal discovery failed: " +
                    $"{principalResponse.StatusCode}\n" +
                    principalResponseBody);
            }

            var principalDocument =
                XDocument.Parse(principalResponseBody);

            XNamespace dav = "DAV:";

            var principalHref =
                principalDocument
                    .Descendants(dav + "current-user-principal")
                    .Descendants(dav + "href")
                    .FirstOrDefault()
                    ?.Value;

            if (string.IsNullOrWhiteSpace(principalHref))
            {
                throw new Exception(
                    "Could not find current-user-principal href.");
            }

            var principalUrl =
                new Uri(
                    new Uri(_settings.ServerUrl),
                    principalHref);

            Console.WriteLine(
                $"Principal URL: {principalUrl}");

            // =========================================================
            // STEP 2 — Discover calendar-home-set
            // =========================================================

            using var homeRequest = new HttpRequestMessage(
                new HttpMethod("PROPFIND"),
                principalUrl);

            homeRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    credentials);

            homeRequest.Headers.Add("Depth", "0");

            var homeXml = """
        <?xml version="1.0" encoding="utf-8" ?>
        <D:propfind xmlns:D="DAV:"
                    xmlns:C="urn:ietf:params:xml:ns:caldav">
            <D:prop>
                <C:calendar-home-set />
            </D:prop>
        </D:propfind>
        """;

            homeRequest.Content = new StringContent(
                homeXml,
                System.Text.Encoding.UTF8,
                "application/xml");

            using var homeResponse =
                await _httpClient.SendAsync(homeRequest);

            var homeResponseBody =
                await homeResponse.Content.ReadAsStringAsync();

            if (!homeResponse.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Calendar home discovery failed: " +
                    $"{homeResponse.StatusCode}\n" +
                    homeResponseBody);
            }

            var homeDocument =
                XDocument.Parse(homeResponseBody);

            XNamespace caldav =
                "urn:ietf:params:xml:ns:caldav";

            var calendarHomeHref =
                homeDocument
                    .Descendants(caldav + "calendar-home-set")
                    .Descendants(dav + "href")
                    .FirstOrDefault()
                    ?.Value;

            if (string.IsNullOrWhiteSpace(calendarHomeHref))
            {
                throw new Exception(
                    "Could not find calendar-home-set href.");
            }

            var calendarHomeUrl =
                new Uri(
                    new Uri(_settings.ServerUrl),
                    calendarHomeHref);

            Console.WriteLine(
                $"Calendar Home URL: {calendarHomeUrl}");

            // =========================================================
            // STEP 3 — Retrieve calendars
            // =========================================================

            using var calendarsRequest = new HttpRequestMessage(
                new HttpMethod("PROPFIND"),
                calendarHomeUrl);

            calendarsRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    credentials);

            calendarsRequest.Headers.Add("Depth", "1");

            var calendarsXml = """
    <?xml version="1.0" encoding="utf-8" ?>
    <D:propfind xmlns:D="DAV:"
                xmlns:C="urn:ietf:params:xml:ns:caldav">
        <D:prop>
            <D:displayname />
            <D:resourcetype />
            <C:supported-calendar-component-set />
        </D:prop>
    </D:propfind>
    """;

            calendarsRequest.Content = new StringContent(
                calendarsXml,
                System.Text.Encoding.UTF8,
                "application/xml");

            using var calendarsResponse =
                await _httpClient.SendAsync(calendarsRequest);

            var calendarsResponseBody =
                await calendarsResponse.Content.ReadAsStringAsync();

            if (!calendarsResponse.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Calendar discovery failed: " +
                    $"{calendarsResponse.StatusCode}\n" +
                    calendarsResponseBody);
            }

            // =========================================================
            // STEP 4 — Parse XML
            // =========================================================

            var calendarsDocument =
                XDocument.Parse(calendarsResponseBody);

            var calendars = new List<ICloudCalendar>();

            foreach (var response in calendarsDocument
                         .Descendants(dav + "response"))
            {
                var href =
                    response
                        .Element(dav + "href")
                        ?.Value
                        ?.Trim();

                var prop =
                    response
                        .Descendants(dav + "prop")
                        .FirstOrDefault();

                if (prop == null || string.IsNullOrWhiteSpace(href))
                {
                    continue;
                }

                var displayName =
                    prop
                        .Element(dav + "displayname")
                        ?.Value
                        ?.Trim();

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                // Determine whether this resource is actually a calendar.
                var resourceType =
                    prop.Element(dav + "resourcetype");

                var isCalendar =
                    resourceType?
                        .Elements()
                        .Any(x => x.Name.LocalName == "calendar") == true;

                if (!isCalendar)
                {
                    continue;
                }

                // ---------------------------------------------------------
                // Supported calendar components
                // ---------------------------------------------------------

                var components =
                    prop
                        .Element(caldav + "supported-calendar-component-set")?
                        .Elements(caldav + "comp")
                        .Select(x => (string?)x.Attribute("name"))
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Cast<string>()
                        .ToList()
                        ?? [];

                var calendarUrl =
                    new Uri(
                        new Uri(_settings.ServerUrl),
                        href);

                calendars.Add(new ICloudCalendar
                {
                    Name = displayName,
                    Url = calendarUrl.ToString(),
                    IsCalendar = true,
                    SupportedComponents = components
                });
            }

            Console.WriteLine(
                $"Discovered {calendars.Count} iCloud calendars.");

            foreach (var calendar in calendars)
            {
                Console.WriteLine(
                    $"Calendar: {calendar.Name}");

                Console.WriteLine(
                    $"URL: {calendar.Url}");

                Console.WriteLine(
                    $"Components: " +
                    $"{string.Join(", ", calendar.SupportedComponents)}");
            }

            return calendars;
        }

        public async Task<List<ICloudCalendarEvent>> GetEventsAsync(
            DateTime from,
            DateTime to)
        {
            var credentials = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{_settings.Username}:{_settings.Password}"));

            // =========================================================
            // STEP 1 — Discover the Booking calendar
            // =========================================================

            var calendars = await DiscoverCalendarsAsync();

            var bookingCalendar = calendars
                .FirstOrDefault(x =>
                    x.Name.Equals(
                        "Bookings",
                        StringComparison.OrdinalIgnoreCase));

            if (bookingCalendar == null)
            {
                throw new Exception(
                    "The 'Booking' calendar could not be found.");
            }

            Console.WriteLine(
                $"Using Booking calendar: {bookingCalendar.Url}");

            // =========================================================
            // STEP 2 — Create CalDAV calendar-query REPORT
            // =========================================================

            var calendarQueryXml = $"""
        <?xml version="1.0" encoding="utf-8" ?>
        <C:calendar-query
            xmlns:D="DAV:"
            xmlns:C="urn:ietf:params:xml:ns:caldav">

            <D:prop>
                <D:getetag />
                <C:calendar-data />
            </D:prop>

            <C:filter>
                <C:comp-filter name="VCALENDAR">
                    <C:comp-filter name="VEVENT">
                        <C:time-range
                            start="{ToCalDavDateTime(from)}"
                            end="{ToCalDavDateTime(to)}" />
                    </C:comp-filter>
                </C:comp-filter>
            </C:filter>

        </C:calendar-query>
        """;

            using var request = new HttpRequestMessage(
                new HttpMethod("REPORT"),
                bookingCalendar.Url);

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    credentials);

            request.Headers.Add("Depth", "1");

            request.Content = new StringContent(
                calendarQueryXml,
                System.Text.Encoding.UTF8,
                "application/xml");

            // =========================================================
            // STEP 3 — Send request to iCloud
            // =========================================================

            using var response =
                await _httpClient.SendAsync(request);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"Events Status: {(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Event retrieval failed: " +
                    $"{response.StatusCode}\n" +
                    responseBody);
            }

            // =========================================================
            // STEP 4 — Parse CalDAV XML
            // =========================================================

            var document =
                XDocument.Parse(responseBody);

            XNamespace dav = "DAV:";

            XNamespace caldav =
                "urn:ietf:params:xml:ns:caldav";

            var events =
                new List<ICloudCalendarEvent>();

            foreach (var responseElement in
                     document.Descendants(dav + "response"))
            {
                var calendarData =
                    responseElement
                        .Descendants(caldav + "calendar-data")
                        .FirstOrDefault()
                        ?.Value;

                if (string.IsNullOrWhiteSpace(calendarData))
                {
                    continue;
                }

                var calendarEvent =
                    ParseCalendarEvent(calendarData);

                if (calendarEvent != null)
                {
                    events.Add(calendarEvent);
                }
            }

            Console.WriteLine(
                $"Retrieved {events.Count} Booking events.");

            return events;
        }

        public async Task<string> CreateEventAsync(
            CreateICloudCalendarEventRequest request)
        {
            var calendars = await DiscoverCalendarsAsync();

            var bookingCalendar = calendars
                .FirstOrDefault(x =>
                    x.Name.Equals(
                        "Bookings",
                        StringComparison.OrdinalIgnoreCase));

            if (bookingCalendar == null)
                throw new InvalidOperationException(
                    "iCloud calendar 'Booking' was not found.");

            var uid = $"{Guid.NewGuid()}@theadamsparadigm";

            var eventUrl =
                $"{bookingCalendar.Url.TrimEnd('/')}/{uid}.ics";

            var calendarData = $"""
BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//The Adams Paradigm//Booking System//EN
BEGIN:VEVENT
UID:{uid}
DTSTAMP:{ToCalDavDateTime(DateTime.UtcNow)}
DTSTART:{ToCalDavDateTime(request.Start)}
DTEND:{ToCalDavDateTime(request.End)}
SUMMARY:{EscapeICalendarText(request.Summary)}
DESCRIPTION:{EscapeICalendarText(request.Description)}
LOCATION:{EscapeICalendarText(request.Location)}
END:VEVENT
END:VCALENDAR
""";

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Put,
                eventUrl);

            var credentials = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{_settings.Username}:{_settings.Password}"));

            httpRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    credentials);

            httpRequest.Content = new StringContent(
                calendarData,
                System.Text.Encoding.UTF8,
                "text/calendar");

            var response = await _httpClient.SendAsync(httpRequest);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Failed to create iCloud event. " +
                    $"Status: {(int)response.StatusCode} " +
                    $"{response.StatusCode}. " +
                    $"Response: {responseBody}");
            }

            return uid;
        }

        public async Task UpdateEventAsync(
            string uid,
            UpdateICloudCalendarEventRequest request)
        {
            var calendars = await DiscoverCalendarsAsync();

            var bookingCalendar = calendars
                .FirstOrDefault(x =>
                    x.Name.Equals(
                        "Bookings",
                        StringComparison.OrdinalIgnoreCase));

            if (bookingCalendar == null)
            {
                throw new InvalidOperationException(
                    "iCloud calendar 'Booking' was not found.");
            }

            var eventUrl =
                $"{bookingCalendar.Url.TrimEnd('/')}/{uid}.ics";

            var calendarData = $"""
BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//The Adams Paradigm//Booking System//EN
BEGIN:VEVENT
UID:{uid}
DTSTAMP:{ToCalDavDateTime(DateTime.UtcNow)}
DTSTART:{ToCalDavDateTime(request.Start)}
DTEND:{ToCalDavDateTime(request.End)}
SUMMARY:{EscapeICalendarText(request.Summary)}
DESCRIPTION:{EscapeICalendarText(request.Description)}
LOCATION:{EscapeICalendarText(request.Location)}
END:VEVENT
END:VCALENDAR
""";

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Put,
                eventUrl);

            var credentials = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{_settings.Username}:{_settings.Password}"));

            httpRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    credentials);

            httpRequest.Content = new StringContent(
                calendarData,
                System.Text.Encoding.UTF8,
                "text/calendar");

            var response =
                await _httpClient.SendAsync(httpRequest);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Failed to update iCloud event. " +
                    $"Status: {(int)response.StatusCode} " +
                    $"{response.StatusCode}. " +
                    $"Response: {responseBody}");
            }
        }

        public async Task DeleteEventAsync(string uid)
        {
            var calendars = await DiscoverCalendarsAsync();

            var bookingCalendar = calendars
                .FirstOrDefault(x =>
                    x.Name.Equals(
                        "Bookings",
                        StringComparison.OrdinalIgnoreCase));

            if (bookingCalendar == null)
            {
                throw new InvalidOperationException(
                    "iCloud calendar 'Booking' was not found.");
            }

            var eventUrl =
                $"{bookingCalendar.Url.TrimEnd('/')}/{uid}.ics";

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Delete,
                eventUrl);

            var credentials = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{_settings.Username}:{_settings.Password}"));

            httpRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    credentials);

            var response =
                await _httpClient.SendAsync(httpRequest);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Failed to delete iCloud event. " +
                    $"Status: {(int)response.StatusCode} " +
                    $"{response.StatusCode}. " +
                    $"Response: {responseBody}");
            }
        }

        private static string ToCalDavDateTime(DateTime dateTime)
        {
            return dateTime
                .ToUniversalTime()
                .ToString("yyyyMMdd'T'HHmmss'Z'");
        }

        private static ICloudCalendarEvent? ParseCalendarEvent(
            string calendarData)
        {
            var lines = calendarData
                .Replace("\r\n ", "")
                .Replace("\n ", "")
                .Split(
                    new[] { "\r\n", "\n" },
                    StringSplitOptions.RemoveEmptyEntries);

            string GetValue(string propertyName)
            {
                var line = lines.FirstOrDefault(x =>
                    x.StartsWith(
                        propertyName + ":",
                        StringComparison.OrdinalIgnoreCase) ||
                    x.StartsWith(
                        propertyName + ";",
                        StringComparison.OrdinalIgnoreCase));

                if (line == null)
                {
                    return string.Empty;
                }

                var colonIndex = line.IndexOf(':');

                if (colonIndex < 0)
                {
                    return string.Empty;
                }

                return line[(colonIndex + 1)..].Trim();
            }

            var uid = GetValue("UID");

            if (string.IsNullOrWhiteSpace(uid))
            {
                return null;
            }

            var startValue = GetValue("DTSTART");
            var endValue = GetValue("DTEND");

            if (!TryParseICalendarDate(startValue, out var start))
            {
                return null;
            }

            if (!TryParseICalendarDate(endValue, out var end))
            {
                return null;
            }

            return new ICloudCalendarEvent
            {
                Uid = uid,
                Summary = GetValue("SUMMARY"),
                Description = GetValue("DESCRIPTION"),
                Location = GetValue("LOCATION"),
                Start = start,
                End = end
            };
        }

        private static bool TryParseICalendarDate(
            string value,
            out DateTime result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string[] formats =
            [
                "yyyyMMdd'T'HHmmss'Z'",
        "yyyyMMdd'T'HHmmss",
        "yyyyMMdd"
            ];

            return DateTime.TryParseExact(
                value,
                formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal,
                out result);
        }

        private static string EscapeICalendarText(string value) 
        { 
            return value
                .Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace(",", "\\,")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n"); 
         }
    }
}
