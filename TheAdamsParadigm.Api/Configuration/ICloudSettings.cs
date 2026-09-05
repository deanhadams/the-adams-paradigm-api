namespace TheAdamsParadigm.Api.Configuration;

// Per-client Username/Password used to live here; ICloudCalendarService now looks those
// up per request from the Clients table (ICloudEmail/ICloudPassword) instead. ServerUrl
// stays global — it's the same CalDAV endpoint for every iCloud account.
public class ICloudSettings
{
    public string ServerUrl { get; set; } = "https://caldav.icloud.com";
}