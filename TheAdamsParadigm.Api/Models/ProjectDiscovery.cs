namespace TheAdamsParadigm.Api.Models;

public class ProjectDiscovery
{
    public bool IsPotentialLead { get; set; }

    public string? ProjectType { get; set; }

    public string? ProjectDescription { get; set; }

    public string? TargetUsers { get; set; }

    public List<string> Features { get; set; } = [];

    public bool? RequiresPayments { get; set; }

    public bool? RequiresAuthentication { get; set; }

    public bool? RequiresDatabase { get; set; }

    public bool? RequiresAdminDashboard { get; set; }

    public string? Budget { get; set; }

    public string? Timeline { get; set; }

    public List<string> MissingInformation { get; set; } = [];

    public int CompletionPercentage { get; set; }
}