using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Services;

public class ProjectDiscoveryService
{
    public ProjectDiscovery Analyze(
        string question,
        List<ChatMessage> history)
    {
        var discovery = new ProjectDiscovery();

        var conversation = string.Join(
            "\n",
            history
                .Where(x => !string.IsNullOrWhiteSpace(x.Content))
                .Select(x => $"{x.Role}: {x.Content}"));

        conversation += $"\nuser: {question}";

        var text = conversation.ToLowerInvariant();

        // ---------------------------------------------------------
        // Detect potential project / lead
        // ---------------------------------------------------------

        discovery.IsPotentialLead =
            ContainsAny(
                text,
                [
                    "i need a website",
                    "i need a web app",
                    "i need an app",
                    "i want a website",
                    "i want an app",
                    "i want to build",
                    "i want to create",
                    "can you build",
                    "build me",
                    "develop",
                    "development",
                    "software",
                    "application",
                    "project",
                    "mvp",
                    "startup",
                    "business website",
                    "online store",
                    "ecommerce",
                    "e-commerce"
                ]);

        if (!discovery.IsPotentialLead)
        {
            return discovery;
        }

        // ---------------------------------------------------------
        // Project type
        // ---------------------------------------------------------

        if (ContainsAny(
                text,
                [
                    "website",
                    "web site"
                ]))
        {
            discovery.ProjectType = "Website";
        }

        if (ContainsAny(
                text,
                [
                    "web app",
                    "web application"
                ]))
        {
            discovery.ProjectType = "Web Application";
        }

        if (ContainsAny(
                text,
                [
                    "mobile app",
                    "android app",
                    "ios app",
                    "iphone app"
                ]))
        {
            discovery.ProjectType = "Application";
        }

        if (ContainsAny(
                text,
                [
                    "api",
                    "integration"
                ]))
        {
            discovery.ProjectType = "API / Integration";
        }

        if (ContainsAny(
                text,
                [
                    "software",
                    "custom software"
                ]))
        {
            discovery.ProjectType = "Custom Software";
        }

        // ---------------------------------------------------------
        // Requirements
        // ---------------------------------------------------------

        AddRequirement(
            discovery,
            text,
            "Booking",
            [
                "booking",
                "bookings",
                "appointment",
                "appointments",
                "schedule",
                "scheduling"
            ]);

        AddRequirement(
            discovery,
            text,
            "Payments",
            [
                "payment",
                "payments",
                "pay online",
                "checkout",
                "yoco",
                "credit card",
                "card payments"
            ]);

        AddRequirement(
            discovery,
            text,
            "Authentication",
            [
                "login",
                "log in",
                "sign in",
                "account",
                "accounts",
                "user authentication"
            ]);

        AddRequirement(
            discovery,
            text,
            "Database",
            [
                "database",
                "store data",
                "save data",
                "customer records"
            ]);

        AddRequirement(
            discovery,
            text,
            "Admin Dashboard",
            [
                "admin dashboard",
                "admin panel",
                "administrator",
                "manage users",
                "manage bookings",
                "dashboard"
            ]);

        AddRequirement(
            discovery,
            text,
            "Email Notifications",
            [
                "email notification",
                "email notifications",
                "confirmation email",
                "send emails",
                "email customers"
            ]);

        // ---------------------------------------------------------
        // Budget
        // ---------------------------------------------------------

        if (ContainsAny(
                text,
                [
                    "under r5,000",
                    "under 5000"
                ]))
        {
            discovery.Budget = "Under R5,000";
        }
        else if (ContainsAny(
                     text,
                     [
                         "r5,000",
                         "r5000",
                         "5,000 – 15,000",
                         "5000 - 15000"
                     ]))
        {
            discovery.Budget = "R5,000 – R15,000";
        }
        else if (ContainsAny(
                     text,
                     [
                         "r15,000",
                         "r15000",
                         "15,000 – 50,000",
                         "15000 - 50000"
                     ]))
        {
            discovery.Budget = "R15,000 – R50,000";
        }
        else if (ContainsAny(
                     text,
                     [
                         "r50,000",
                         "r50000",
                         "50,000+",
                         "50000+"
                     ]))
        {
            discovery.Budget = "R50,000+";
        }

        // ---------------------------------------------------------
        // Calculate missing information
        // ---------------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                discovery.ProjectType))
        {
            discovery.MissingInformation.Add(
                "Project type");
        }

        if (string.IsNullOrWhiteSpace(
                discovery.ProjectDescription))
        {
            discovery.MissingInformation.Add(
                "Project description");
        }

        if (discovery.Features.Count == 0)
        {
            discovery.MissingInformation.Add(
                "Required features");
        }

        if (string.IsNullOrWhiteSpace(discovery.Budget))
        {
            discovery.MissingInformation.Add(
                "Budget");
        }

        discovery.CompletionPercentage =
            CalculateCompletion(discovery);

        return discovery;
    }

    private static void AddRequirement(
        ProjectDiscovery discovery,
        string text,
        string requirement,
        IEnumerable<string> keywords)
    {
        if (!ContainsAny(text, keywords))
        {
            return;
        }

        if (!discovery.Features.Contains(requirement))
        {
            discovery.Features.Add(requirement);
        }

        switch (requirement)
        {
            case "Payments":
                discovery.RequiresPayments = true;
                break;

            case "Authentication":
                discovery.RequiresAuthentication = true;
                break;

            case "Database":
                discovery.RequiresDatabase = true;
                break;

            case "Admin Dashboard":
                discovery.RequiresAdminDashboard = true;
                break;
        }
    }

    private static int CalculateCompletion(
        ProjectDiscovery discovery)
    {
        var total = 5;
        var completed = 0;

        if (!string.IsNullOrWhiteSpace(
                discovery.ProjectType))
        {
            completed++;
        }

        if (!string.IsNullOrWhiteSpace(
                discovery.ProjectDescription))
        {
            completed++;
        }

        if (discovery.Features.Count > 0)
        {
            completed++;
        }

        if (!string.IsNullOrWhiteSpace(
                discovery.TargetUsers))
        {
            completed++;
        }

        if (!string.IsNullOrWhiteSpace(
                discovery.Budget))
        {
            completed++;
        }

        return (int)Math.Round(
            completed / (double)total * 100);
    }

    private static bool ContainsAny(
        string text,
        IEnumerable<string> keywords)
    {
        return keywords.Any(keyword =>
            text.Contains(
                keyword,
                StringComparison.OrdinalIgnoreCase));
    }

    public string BuildDiscoveryPrompt(
        ProjectDiscovery discovery)
    {
        if (!discovery.IsPotentialLead)
        {
            return string.Empty;
        }

        var missingInformation =
            discovery.MissingInformation.Count > 0
                ? string.Join(
                    ", ",
                    discovery.MissingInformation)
                : "None";

        var features =
            discovery.Features.Count > 0
                ? string.Join(
                    ", ",
                    discovery.Features)
                : "None identified";

        return $"""
        PROJECT DISCOVERY

        The visitor appears to be discussing a potential project.

        Detected project type:
        {discovery.ProjectType ?? "Not yet identified"}

        Detected requirements:
        {features}

        Budget:
        {discovery.Budget ?? "Not provided"}

        Information still potentially needed:
        {missingInformation}

        Use the conversation history to determine what the visitor
        has already told us.

        Do not ask for information that the visitor has already provided.

        If important project information is missing, naturally ask
        one or two useful questions to better understand the project.

        Do not turn the conversation into a rigid questionnaire.

        The goal is to understand the visitor's idea while keeping
        the conversation natural.
        """;
    }
}
