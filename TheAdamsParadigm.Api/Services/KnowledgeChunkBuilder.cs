using System.Text;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Services;

public record KnowledgeSectionText(string Section, string Content, string[] Keywords);

// Builds the same section/project/FAQ text chunks used by both the keyword-matching
// KnowledgeSearchService and the RAG embedding pipeline (KnowledgeChunkSeedService),
// so the two stay in sync with a single definition of what a "chunk" is.
public class KnowledgeChunkBuilder
{
    public List<KnowledgeSectionText> Build(KnowledgeBase knowledgeBase)
    {
        var chunks = new List<KnowledgeSectionText>();

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

        chunks.Add(new KnowledgeSectionText(
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
            ]));

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

        chunks.Add(new KnowledgeSectionText(
            "About",
            aboutText,
            [
                "about",
                "dean",
                "developer",
                "experience",
                "background",
                "focus"
            ]));

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

        chunks.Add(new KnowledgeSectionText(
            "Philosophy",
            philosophyText.ToString(),
            [
                "philosophy",
                "values",
                "approach",
                "principles",
                "quality"
            ]));

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

        chunks.Add(new KnowledgeSectionText(
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
            ]));

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

        chunks.Add(new KnowledgeSectionText(
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
            ]));

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

        chunks.Add(new KnowledgeSectionText(
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
            ]));

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

        chunks.Add(new KnowledgeSectionText(
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
            ]));

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

        chunks.Add(new KnowledgeSectionText(
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
            ]));

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

            chunks.Add(new KnowledgeSectionText(
                $"Project: {project.Name}",
                projectText,
                [.. projectKeywords]));
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

            chunks.Add(new KnowledgeSectionText(
                $"FAQ: {faq.Question}",
                faqText,
                [
                    "faq",
                    "question",
                    faq.Question.ToLowerInvariant()
                ]));
        }

        return chunks;
    }
}
