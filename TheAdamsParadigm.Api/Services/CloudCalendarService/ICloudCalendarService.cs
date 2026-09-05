using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Xml.Linq;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models.Calendar;

namespace TheAdamsParadigm.Api.Services.CloudCalendarService
{
    public class ICloudCalendarService
    {
        private readonly HttpClient _httpClient;
        private readonly ICloudSettings _settings;
        private readonly ApplicationDbContext _dbContext;
        private readonly ClientCredentialProtector _credentialProtector;

        // Booking calendar discovery is cached per client (each client has their own
        // iCloud account and therefore their own calendar set), keyed by ClientApiKey.
        // One discovery lock per client too, so concurrent requests for different
        // clients don't serialize behind each other.
        private readonly ConcurrentDictionary<string, ICloudCalendar> _bookingCalendars = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _bookingCalendarLocks = new();

        public static readonly TimeZoneInfo BookingTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows()
                    ? "South Africa Standard Time"
                    : "Africa/Johannesburg");

        public ICloudCalendarService(
            HttpClient httpClient,
            ICloudSettings settings,
            ApplicationDbContext dbContext,
            ClientCredentialProtector credentialProtector)
        {
            _httpClient = httpClient;
            _settings = settings;
            _dbContext = dbContext;
            _credentialProtector = credentialProtector;
        }

        // Resolves a client's iCloud username/password (decrypting the stored password)
        // and Base64-encodes them for a Basic auth header — clients are identified by
        // their ClientApiKey rather than their internal numeric ClientId.
        private async Task<string> GetBasicAuthCredentialsAsync(string clientApiKey)
        {
            var client = await _dbContext.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientApiKey == clientApiKey);

            if (client == null)
            {
                throw new ClientNotFoundException(clientApiKey);
            }

            if (string.IsNullOrWhiteSpace(client.ICloudEmail) || string.IsNullOrWhiteSpace(client.ICloudPassword))
            {
                throw new ClientCloudCredentialsMissingException(clientApiKey);
            }

            var password = _credentialProtector.Unprotect(client.ICloudPassword);

            return Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{client.ICloudEmail}:{password}"));
        }

        // Which calendar to book against is per client now, not a hardcoded "Bookings".
        private async Task<string> GetBookingCalendarNameAsync(string clientApiKey)
        {
            var calendarName = await _dbContext.Clients
                .AsNoTracking()
                .Where(c => c.ClientApiKey == clientApiKey)
                .Select(c => (string?)c.ICloudCalendar)
                .FirstOrDefaultAsync();

            if (calendarName == null)
            {
                throw new ClientNotFoundException(clientApiKey);
            }

            return string.IsNullOrWhiteSpace(calendarName) ? "Bookings" : calendarName;
        }

        private async Task<ICloudCalendar> GetBookingCalendarAsync(string clientApiKey)
        {
            // Fast path:
            // The calendar has already been discovered.
            if (_bookingCalendars.TryGetValue(clientApiKey, out var cachedCalendar))
            {
                return cachedCalendar;
            }

            // Only one request per client is allowed to perform discovery.
            var calendarLock = _bookingCalendarLocks.GetOrAdd(clientApiKey, _ => new SemaphoreSlim(1, 1));
            await calendarLock.WaitAsync();

            try
            {
                // Check again after acquiring the lock.
                //
                // Another request may have discovered and cached
                // the calendar while this request was waiting.
                if (_bookingCalendars.TryGetValue(clientApiKey, out cachedCalendar))
                {
                    return cachedCalendar;
                }

                Console.WriteLine(
                    "Booking calendar not cached. Discovering calendars...");

                var calendars = await DiscoverCalendarsAsync(clientApiKey);
                var calendarName = await GetBookingCalendarNameAsync(clientApiKey);

                var bookingCalendar = calendars
                    .FirstOrDefault(x =>
                        x.Name.Equals(
                            calendarName,
                            StringComparison.OrdinalIgnoreCase));

                if (bookingCalendar == null)
                {
                    throw new InvalidOperationException(
                        $"iCloud calendar '{calendarName}' was not found for the requesting client.");
                }

                _bookingCalendars[clientApiKey] = bookingCalendar;

                Console.WriteLine(
                    $"Booking calendar cached: {bookingCalendar.Url}");

                return bookingCalendar;
            }
            finally
            {
                calendarLock.Release();
            }
        }

        public async Task<List<ICloudCalendar>> DiscoverCalendarsAsync(string clientApiKey)
        {
            var credentials = await GetBasicAuthCredentialsAsync(clientApiKey);

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
            string clientApiKey,
            DateTime from,
            DateTime to)
        {
            var credentials = await GetBasicAuthCredentialsAsync(clientApiKey);

            // =========================================================
            // STEP 1 — Discover the Booking calendar
            // =========================================================

            var bookingCalendar = await GetBookingCalendarAsync(clientApiKey);

            Console.WriteLine($"Using Booking calendar: {bookingCalendar.Url}");

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
            string clientApiKey,
            CreateICloudCalendarEventRequest request)
        {
            var bookingCalendar = await GetBookingCalendarAsync(clientApiKey);

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

            var credentials = await GetBasicAuthCredentialsAsync(clientApiKey);

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
            string clientApiKey,
            string uid,
            UpdateICloudCalendarEventRequest request)
        {
            var bookingCalendar = await GetBookingCalendarAsync(clientApiKey);

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

            var credentials = await GetBasicAuthCredentialsAsync(clientApiKey);

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

        public async Task DeleteEventAsync(string clientApiKey, string uid)
        {
            var bookingCalendar = await GetBookingCalendarAsync(clientApiKey);

            var eventUrl =
                $"{bookingCalendar.Url.TrimEnd('/')}/{uid}.ics";

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Delete,
                eventUrl);

            var credentials = await GetBasicAuthCredentialsAsync(clientApiKey);

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

        public async Task<ICloudAvailabilityResponse> CheckAvailabilityAsync(
            string clientApiKey,
            DateTime start,
            DateTime end)
        {
            if (end <= start)
            {
                throw new ArgumentException(
                    "End time must be after start time.");
            }

            // Get the events that occur during the requested period.
            var events = await GetEventsAsync(clientApiKey, start, end);

            // Check for overlapping events.
            var conflicts = events
                .Where(existingEvent =>
                    existingEvent.Start < end &&
                    existingEvent.End > start)
                .ToList();

            return new ICloudAvailabilityResponse
            {
                Available = conflicts.Count == 0,
                Start = start,
                End = end,
                Conflicts = conflicts
            };
        }

        public async Task<List<AvailableBookingSlot>>
            GetAvailableSlotsAsync(string clientApiKey, BookingAvailabilityRequest request)
        {
            if (request.DurationMinutes <= 0)
            {
                throw new ArgumentException(
                    "Duration must be greater than zero.");
            }

            if (request.SlotIntervalMinutes <= 0)
            {
                throw new ArgumentException(
                    "Slot interval must be greater than zero.");
            }

            if (request.BusinessEnd <= request.BusinessStart)
            {
                throw new ArgumentException(
                    "Business end time must be after business start time.");
            }

            // ---------------------------------------------------------
            // Build the start/end of the working day
            // ---------------------------------------------------------

            var dayStart =
                request.Date.Date +
                request.BusinessStart;

            var dayEnd =
                request.Date.Date +
                request.BusinessEnd;

            // ---------------------------------------------------------
            // Retrieve all iCloud events for the working day
            // ---------------------------------------------------------

            var events = await GetEventsAsync(
                clientApiKey,
                dayStart,
                dayEnd);

            var availableSlots =
                new List<AvailableBookingSlot>();

            // ---------------------------------------------------------
            // Generate possible slots
            // ---------------------------------------------------------

            var slotStart = dayStart;

            while (slotStart.AddMinutes(
                       request.DurationMinutes) <= dayEnd)
            {
                var slotEnd =
                    slotStart.AddMinutes(
                        request.DurationMinutes);

                // -----------------------------------------------------
                // Check whether this slot overlaps an existing event
                // -----------------------------------------------------

                var hasConflict = events.Any(existingEvent =>
                    existingEvent.Start < slotEnd &&
                    existingEvent.End > slotStart);

                if (!hasConflict)
                {
                    availableSlots.Add(
                        new AvailableBookingSlot
                        {
                            Start = slotStart,
                            End = slotEnd
                        });
                }

                slotStart =
                    slotStart.AddMinutes(
                        request.SlotIntervalMinutes);
            }

            return availableSlots;
        }

        private static string ToCalDavDateTime(DateTime dateTime)
        {
            var localTime = DateTime.SpecifyKind(
                dateTime,
                DateTimeKind.Unspecified);

            var utcTime = TimeZoneInfo.ConvertTimeToUtc(
                localTime,
                BookingTimeZone);

            return utcTime.ToString(
                "yyyyMMdd'T'HHmmss'Z'");
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

            if (!DateTime.TryParseExact(
                    value,
                    formats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var parsed))
            {
                return false;
            }

            // iCloud value ending in Z = UTC
            if (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            {
                result = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(
                        parsed,
                        DateTimeKind.Utc),
                    BookingTimeZone);

                return true;
            }

            // Floating/local calendar time.
            result = DateTime.SpecifyKind(
                parsed,
                DateTimeKind.Unspecified);

            return true;
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
