using System.Text;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Services;

public class KnowledgeSearchService
{
    private readonly KnowledgeBaseService _knowledgeBaseService;

    public KnowledgeSearchService(
        KnowledgeBaseService knowledgeBaseService)
    {
        _knowledgeBaseService = knowledgeBaseService;
    }

    public KnowledgeSearchResult Search(string query)
    {
        var knowledgeBase =
            _knowledgeBaseService.GetKnowledgeBase();

        var normalizedQuery = query.ToLowerInvariant();

        var results = new List<(int Score, string SectionName, string Content)>();

        // ---------------------------------------------------------
        // Detect intent
        // ---------------------------------------------------------

        var intent = DetectIntent(normalizedQuery);

        // ---------------------------------------------------------
        // Business
        // ---------------------------------------------------------

        var businessText = $"""
            Business:
            Name: {knowledgeBase.Business.Name}
            Short Name: {knowledgeBase.Business.ShortName}
            Founder: {knowledgeBase.Business.Founder}
            Role: {knowledgeBase.Business.Role}
            Tagline: {knowledgeBase.Business.Tagline}
            Description: {knowledgeBase.Business.Description}
            Email: {knowledgeBase.Business.Email}
            Availability: {knowledgeBase.Business.Availability}
            Location: {knowledgeBase.Business.Location}
            Currency: {knowledgeBase.Business.Currency}
            Website: {knowledgeBase.Business.Website}
            Socials: {string.Join(", ", knowledgeBase.Business.Socials)}
            """;

        AddResult(
            results,
            normalizedQuery,
            "Business",
            businessText,
            [
                "business",
                "company",
                "founder",
                "dean",
                "who",
                "website",
                "email",
                "contact",
                "location",
                "available"
            ]);

        // ---------------------------------------------------------
        // About
        // ---------------------------------------------------------

        var aboutText = $"""
            About:
            {knowledgeBase.About.Summary}

            Biography:
            {string.Join("\n", knowledgeBase.About.Bio)}

            Focus Areas:
            {string.Join(", ", knowledgeBase.About.FocusAreas)}
            """;

        AddResult(
            results,
            normalizedQuery,
            "About",
            aboutText,
            [
                "about",
                "dean",
                "developer",
                "experience",
                "background",
                "focus"
            ]);

        // ---------------------------------------------------------
        // Philosophy
        // ---------------------------------------------------------

        var philosophyText = new StringBuilder();

        philosophyText.AppendLine("Philosophy:");

        foreach (var principle in knowledgeBase.Philosophy.Principles)
        {
            philosophyText.AppendLine(
                $"{principle.Title}: {principle.Description}");
        }

        AddResult(
            results,
            normalizedQuery,
            "Philosophy",
            philosophyText.ToString(),
            [
                "philosophy",
                "values",
                "approach",
                "principles",
                "quality"
            ]);

        // ---------------------------------------------------------
        // Process
        // ---------------------------------------------------------

        var processText = new StringBuilder();

        processText.AppendLine("Development Process:");
        processText.AppendLine(
            knowledgeBase.Process.Summary);

        foreach (var step in knowledgeBase.Process.Steps)
        {
            processText.AppendLine(
                $"{step.Step}. {step.Title}: {step.Description}");
        }

        AddResult(
            results,
            normalizedQuery,
            "Process",
            processText.ToString(),
            [
                "process",
                "development process",
                "how",
                "build",
                "develop",
                "steps",
                "workflow",
                "project process",
                "how does it work"
            ]);

        // ---------------------------------------------------------
        // Technologies
        // ---------------------------------------------------------

        var technologyText = new StringBuilder();

        technologyText.AppendLine("Technologies:");
        technologyText.AppendLine(
            knowledgeBase.Technologies.Summary);

        foreach (var group in knowledgeBase.Technologies.Groups)
        {
            technologyText.AppendLine(
                $"{group.Category}: {group.Description}");

            technologyText.AppendLine(
                $"Technologies: {string.Join(", ", group.Items)}");
        }

        AddResult(
            results,
            normalizedQuery,
            "Technologies",
            technologyText.ToString(),
            [
                "technology",
                "technologies",
                "tech",
                "react",
                "typescript",
                "javascript",
                "html",
                "css",
                "tailwind",
                "c#",
                ".net",
                "asp.net",
                "api",
                "database",
                "postgres",
                "postgresql",
                "sql",
                "signalr",
                "ai",
                "artificial intelligence",
                "cloud",
                "github",
                "webhook"
            ]);

        // ---------------------------------------------------------
        // Services
        // ---------------------------------------------------------

        var servicesText = new StringBuilder();

        servicesText.AppendLine("Services:");
        servicesText.AppendLine(
            knowledgeBase.Services.Summary);

        servicesText.AppendLine(
            $"Currency: {knowledgeBase.Services.Currency}");

        foreach (var service in knowledgeBase.Services.List)
        {
            servicesText.AppendLine(
                $"""
                Service ID: {service.ServiceId}
                Title: {service.Title}
                Description: {service.Description}
                Cost Per Hour: {service.CostPerHour}
                Setup Fee: {service.SetupFee}
                Bookable: {service.IsBookable}
                """);
        }

        AddResult(
            results,
            normalizedQuery,
            "Services",
            servicesText.ToString(),
            [
                "service",
                "services",
                "price",
                "pricing",
                "cost",
                "rate",
                "rates",
                "hour",
                "hourly",
                "website",
                "software",
                "application",
                "app",
                "api",
                "booking",
                "consultation",
                "consult",
                "development",
                "build",
                "create",
                "develop"
            ]);

        // ---------------------------------------------------------
        // Booking
        // ---------------------------------------------------------

        var bookingText = $"""
            Booking:
            {knowledgeBase.Booking.Summary}

            Payment Provider:
            {knowledgeBase.Booking.PaymentProvider}

            Currency:
            {knowledgeBase.Booking.Currency}

            Booking Steps:
            {string.Join("\n", knowledgeBase.Booking.Steps)}

            View Bookings:
            {knowledgeBase.Booking.ViewBookings}
            """;

        AddResult(
            results,
            normalizedQuery,
            "Booking",
            bookingText,
            [
                "booking",
                "book",
                "appointment",
                "payment",
                "yoco",
                "schedule",
                "scheduling",
                "availability",
                "calendar",
                "service"
            ]);

        // ---------------------------------------------------------
        // Contact
        // ---------------------------------------------------------

        var contactText = new StringBuilder();

        contactText.AppendLine("Contact:");
        contactText.AppendLine(
            knowledgeBase.Contact.Summary);

        contactText.AppendLine(
            $"Email: {knowledgeBase.Contact.Email}");

        contactText.AppendLine("Conversion Paths:");

        foreach (var path in knowledgeBase.Contact.ConversionPaths)
        {
            contactText.AppendLine(
                $"{path.Title}: {path.Description}");
        }

        contactText.AppendLine("Project Types:");
        contactText.AppendLine(
            string.Join(
                ", ",
                knowledgeBase.Contact.ProjectTypeOptions));

        contactText.AppendLine("Budget Options:");
        contactText.AppendLine(
            string.Join(
                ", ",
                knowledgeBase.Contact.BudgetOptions));

        AddResult(
            results,
            normalizedQuery,
            "Contact",
            contactText.ToString(),
            [
                "contact",
                "email",
                "quote",
                "project",
                "budget",
                "consultation",
                "get started",
                "reach",
                "hire"
            ]);

        // ---------------------------------------------------------
        // Projects
        // ---------------------------------------------------------

        foreach (var project in knowledgeBase.Projects)
        {
            var projectText = $"""
                Project:
                Name: {project.Name}
                Category: {project.Category}
                Description: {project.Description}

                Highlights:
                {string.Join("\n", project.Highlights)}

                Tags:
                {string.Join(", ", project.Tags)}

                URL:
                {project.Url}

                Featured:
                {project.Featured}

                Challenge:
                {project.Challenge}

                Solution:
                {project.Solution}

                Features:
                {string.Join("\n", project.Features)}
                """;

            var projectKeywords = new List<string>
            {
                "project",
                "portfolio",
                project.Name.ToLowerInvariant(),
                project.Category.ToLowerInvariant()
            };

            projectKeywords.AddRange(
                project.Tags.Select(x =>
                    x.ToLowerInvariant()));

            AddResult(
                results,
                normalizedQuery,
                $"Project: {project.Name}",
                projectText,
                projectKeywords);
        }

        // ---------------------------------------------------------
        // FAQs
        // ---------------------------------------------------------

        foreach (var faq in knowledgeBase.Faqs)
        {
            var faqText = $"""
                Frequently Asked Question:

                Question:
                {faq.Question}

                Answer:
                {faq.Answer}
                """;

            AddResult(
                results,
                normalizedQuery,
                $"FAQ: {faq.Question}",
                faqText,
                [
                    "faq",
                    "question",
                    faq.Question.ToLowerInvariant()
                ]);
        }

        // ---------------------------------------------------------
        // Intent boosting
        // ---------------------------------------------------------

        BoostIntentResults(
            results,
            intent);

        // ---------------------------------------------------------
        // Select best results
        // ---------------------------------------------------------

        var bestResults = results
            .OrderByDescending(x => x.Score)
            .Take(5)
            .Where(x => x.Score > 0)
            .ToList();

        // ---------------------------------------------------------
        // Fallback
        // ---------------------------------------------------------

        if (bestResults.Count == 0)
        {
            return new KnowledgeSearchResult
            {
                Intent = intent,
                Sections = [],
                Context = """
                    No specific knowledge-base section matched
                    the visitor's question.

                    Do not invent information. If the question
                    cannot be answered accurately, explain that
                    the information is not currently available.
                    """
            };
        }

        return new KnowledgeSearchResult
        {
            Intent = intent,

            Sections = bestResults
                .Select(x => x.SectionName)
                .ToList(),

            Context = string.Join(
                "\n\n----------------------------\n\n",
                bestResults.Select(x => x.Content))
        };
    }

    // =============================================================
    // Intent detection
    // =============================================================

    private static string DetectIntent(string query)
    {
        if (ContainsAny(
                query,
                [
                    "price",
                    "pricing",
                    "cost",
                    "how much",
                    "rate",
                    "hourly",
                    "expensive",
                    "budget"
                ]))
        {
            return "Pricing";
        }

        if (ContainsAny(
                query,
                [
                    "book",
                    "booking",
                    "appointment",
                    "schedule",
                    "availability",
                    "calendar"
                ]))
        {
            return "Booking";
        }

        if (ContainsAny(
                query,
                [
                    "contact",
                    "email",
                    "hire",
                    "reach",
                    "get in touch",
                    "quote"
                ]))
        {
            return "Contact";
        }

        if (ContainsAny(
                query,
                [
                    "project",
                    "portfolio",
                    "built",
                    "created",
                    "case study"
                ]))
        {
            return "Projects";
        }

        if (ContainsAny(
                query,
                [
                    "technology",
                    "technologies",
                    "tech stack",
                    "react",
                    "typescript",
                    "javascript",
                    "c#",
                    ".net",
                    "asp.net",
                    "postgres",
                    "database",
                    "signalr",
                    "ai"
                ]))
        {
            return "Technologies";
        }

        if (ContainsAny(
                query,
                [
                    "process",
                    "workflow",
                    "how do you build",
                    "how do you develop",
                    "development process"
                ]))
        {
            return "Process";
        }

        if (ContainsAny(
                query,
                [
                    "who is dean",
                    "who are you",
                    "about dean",
                    "about the company",
                    "about the business"
                ]))
        {
            return "About";
        }

        if (ContainsAny(
                query,
                [
                    "service",
                    "services",
                    "website",
                    "web app",
                    "application",
                    "software",
                    "api",
                    "build",
                    "develop"
                ]))
        {
            return "Services";
        }

        return "General";
    }

    // =============================================================
    // Boost results based on detected intent
    // =============================================================

    private static void BoostIntentResults(
        List<(int Score, string SectionName, string Content)> results,
        string intent)
    {
        var boostSections = intent switch
        {
            "Pricing" =>
                new[] { "Services", "Contact" },

            "Booking" =>
                new[] { "Booking", "Services", "Contact" },

            "Contact" =>
                new[] { "Contact", "Business" },

            "Projects" =>
                new[] { "Project" },

            "Technologies" =>
                new[] { "Technologies" },

            "Process" =>
                new[] { "Process" },

            "About" =>
                new[] { "About", "Business", "Philosophy" },

            "Services" =>
                new[] { "Services", "Technologies" },

            _ =>
                Array.Empty<string>()
        };

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];

            if (boostSections.Any(section =>
                    result.SectionName.StartsWith(
                        section,
                        StringComparison.OrdinalIgnoreCase)))
            {
                results[i] = (
                    result.Score + 5,
                    result.SectionName,
                    result.Content);
            }
        }
    }

    // =============================================================
    // Add search result
    // =============================================================

    private static void AddResult(
        List<(int Score, string SectionName, string Content)> results,
        string query,
        string sectionName,
        string content,
        IEnumerable<string> keywords)
    {
        var score = 0;

        foreach (var keyword in keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (query.Contains(
                    keyword.ToLowerInvariant()))
            {
                score++;
            }
        }

        if (score > 0)
        {
            results.Add(
                (
                    score,
                    sectionName,
                    content
                ));
        }
    }

    // =============================================================
    // Keyword helper
    // =============================================================

    private static bool ContainsAny(
        string query,
        IEnumerable<string> keywords)
    {
        return keywords.Any(keyword =>
            query.Contains(
                keyword.ToLowerInvariant()));
    }
}