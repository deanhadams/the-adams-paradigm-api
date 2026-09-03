namespace TheAdamsParadigm.Api.Models;

public class KnowledgeBase
{
    public string LastUpdated { get; set; } = string.Empty;
    public BusinessInfo Business { get; set; } = new();
    public AboutInfo About { get; set; } = new();
    public PhilosophyInfo Philosophy { get; set; } = new();
    public List<WhoWeWorkWithItem> WhoWeWorkWith { get; set; } = [];
    public ProcessInfo Process { get; set; } = new();
    public TechnologiesInfo Technologies { get; set; } = new();
    public ServicesInfo Services { get; set; } = new();
    public BookingInfo Booking { get; set; } = new();
    public ContactInfo Contact { get; set; } = new();
    public List<ProjectInfo> Projects { get; set; } = [];
    public List<FaqItem> Faqs { get; set; } = [];
}

public class BusinessInfo
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Founder { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public List<string> Socials { get; set; } = [];
}

public class AboutInfo
{
    public string Summary { get; set; } = string.Empty;
    public List<string> Bio { get; set; } = [];
    public List<string> FocusAreas { get; set; } = [];
}

public class PhilosophyInfo
{
    public List<PhilosophyPrinciple> Principles { get; set; } = [];
}

public class PhilosophyPrinciple
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class WhoWeWorkWithItem
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ProcessInfo
{
    public string Summary { get; set; } = string.Empty;
    public List<ProcessStep> Steps { get; set; } = [];
}

public class ProcessStep
{
    public int Step { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
public class TechnologiesInfo
{
    public string Summary { get; set; } = string.Empty;
    public List<TechnologyGroup> Groups { get; set; } = [];
}

public class TechnologyGroup
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Items { get; set; } = [];
}

public class ServicesInfo
{
    public string Summary { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public List<ServiceInfo> List { get; set; } = [];
}

public class ServiceInfo
{
    public int ServiceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal CostPerHour { get; set; }
    public decimal SetupFee { get; set; }
    public bool IsBookable { get; set; }
}

public class BookingInfo
{
    public string Summary { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = [];
    public string ViewBookings { get; set; } = string.Empty;
}

public class ContactInfo
{
    public string Summary { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<ConversionPath> ConversionPaths { get; set; } = [];
    public List<string> ProjectTypeOptions { get; set; } = [];
    public List<string> BudgetOptions { get; set; } = [];
}

public class ConversionPath
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
public class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Highlights { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public string Url { get; set; } = string.Empty;
    public bool Featured { get; set; }
    public string Challenge { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public List<string> Features { get; set; } = [];
}

public class FaqItem
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}