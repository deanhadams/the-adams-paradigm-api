namespace TheAdamsParadigm.Api.Configuration;

public class ResendSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "onboarding@resend.dev";
    public string ToEmail { get; set; } = string.Empty;
}
