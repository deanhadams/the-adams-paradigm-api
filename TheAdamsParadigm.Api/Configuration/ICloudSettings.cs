namespace TheAdamsParadigm.Api.Configuration;

public class ICloudSettings
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ServerUrl { get; set; } = "https://caldav.icloud.com";
}