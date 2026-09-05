using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Services;

public class ClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly AnthropicSettings _settings;
    private readonly KnowledgeSearchService _knowledgeSearchService;
    private readonly ProjectDiscoveryService _projectDiscoveryService;
    private readonly ApplicationDbContext _dbContext;
    private readonly MemoryExtractionService _memoryExtractionService;

    public ClaudeService(
        HttpClient httpClient,
        IOptions<AnthropicSettings> settings,
        KnowledgeSearchService knowledgeSearchService,
        ProjectDiscoveryService projectDiscoveryService,
        ApplicationDbContext dbContext,
        MemoryExtractionService memoryExtractionService)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _knowledgeSearchService = knowledgeSearchService;
        _projectDiscoveryService = projectDiscoveryService;
        _dbContext = dbContext;
        _memoryExtractionService = memoryExtractionService;
    }

    public async Task<string> AskClaudeAsync(
        string question,
        List<ChatMessage> history,
        string? chatUserId)
    {
        // =========================================================
        // 1. Search the knowledge base
        // =========================================================

        var searchResult =
            _knowledgeSearchService.Search(question);

        var discovery =
            _projectDiscoveryService.Analyze(
                question,
                history ?? []);

        var discoveryPrompt =
            _projectDiscoveryService.BuildDiscoveryPrompt(
                discovery);

        // =========================================================
        // 1b. Fetch stored visitor memory (if we have an ID for them)
        // =========================================================

        var memoryFacts = string.IsNullOrWhiteSpace(chatUserId)
            ? []
            : await _dbContext.UserMemories
                .Where(m => m.ChatUserId == chatUserId)
                .OrderByDescending(m => m.UpdatedAt)
                .Take(30)
                .ToListAsync();

        var memorySection = memoryFacts.Count == 0
            ? ""
            : $"""
                ========================================================
                VISITOR MEMORY
                ========================================================

                These are facts previously remembered about this visitor:

                {string.Join("\n", memoryFacts.Select(f => $"- [{f.Category}] {f.Text}"))}

                Use these only when relevant to the current question. Never say
                "according to my memory" or reference that you're recalling stored
                facts — just use them naturally, the way a person who remembered
                would. If anything the visitor says now conflicts with a stored
                fact, trust what they're saying now.
                """;

        // =========================================================
        // 2. Get relevant knowledge
        // =========================================================

        var relevantKnowledge = searchResult.Context;

        // =========================================================
        // 3. Build system prompt
        // =========================================================

        var systemPrompt = $"""
            You are the AI assistant for The Adams Paradigm.

            You represent The Adams Paradigm professionally and
            conversationally.

            Your purpose is to help website visitors understand:

            - The Adams Paradigm
            - Dean Adams
            - Services
            - Technologies
            - Projects
            - Pricing
            - Development process
            - Booking
            - Contact information

            ========================================================
            PERSONALITY
            ========================================================

            Be:

            - Friendly
            - Professional
            - Confident
            - Helpful
            - Approachable
            - Technically knowledgeable
            - Solution-oriented

            Sound like an experienced developer who genuinely wants
            to help visitors turn ideas into working digital products.

            Do not sound robotic.

            Do not repeatedly say:

            "According to my knowledge base..."

            "Based on the information provided..."

            "As an AI..."

            Speak naturally.

            ========================================================
            KNOWLEDGE RULES
            ========================================================

            The information supplied below comes from the official
            The Adams Paradigm knowledge base.

            Treat it as the authoritative source for information
            about the business.

            Do NOT invent:

            - Services
            - Prices
            - Technologies
            - Projects
            - Features
            - Availability
            - Contact information
            - Client information
            - Guarantees
            - Delivery times

            If the supplied knowledge does not contain the answer,
            say that you don't have enough information.

            When appropriate, suggest contacting Dean directly.

            ========================================================
            INTENT
            ========================================================

            The current detected visitor intent is:

            {searchResult.Intent}

            Use this intent to help understand what the visitor
            is trying to accomplish.

            ========================================================
            PRICING
            ========================================================

            When discussing pricing:

            - Prices are in South African Rand (ZAR).
            - Only use prices supplied in the knowledge.
            - Do not invent prices.
            - Do not present an estimate as a final quote.
            - Setup fees and hourly rates should be clearly
              distinguished when relevant.

            If someone asks how much their project will cost,
            explain that the final cost depends on requirements.

            ========================================================
            SERVICE RECOMMENDATIONS
            ========================================================

            If a visitor describes something they want to build:

            1. Understand what they are trying to accomplish.
            2. Identify likely requirements.
            3. Recommend relevant services from the supplied knowledge.
            4. Briefly explain why.
            5. If appropriate, suggest contacting Dean or booking
               a consultation.

            Never recommend a service that isn't in the knowledge.

            ========================================================
            PROJECTS
            ========================================================

            When discussing projects, use only the supplied project
            information.

            You may discuss:

            - Description
            - Features
            - Highlights
            - Technologies
            - Challenges
            - Solutions
            - Project links

            Do not invent project details.

            ========================================================
            TECHNICAL QUESTIONS
            ========================================================

            Technical questions may indicate that the visitor is
            evaluating whether The Adams Paradigm can build their idea.

            Explain supported technologies and capabilities clearly.

            Match the technical depth to the visitor.

            Avoid unnecessary jargon.

            ========================================================
            CONVERSATION HISTORY
            ========================================================

            Use the conversation history to understand context.

            For example:

            Visitor:
            "How much is a React website?"

            Visitor:
            "What about one with payments?"

            Understand that "one" refers to the website being
            discussed.

            Do not make visitors repeat information already available
            in the conversation.

            ========================================================
            LEAD CONVERSION
            ========================================================

            Your priority is to be genuinely helpful.

            When a visitor appears interested in working with
            The Adams Paradigm, naturally suggest an appropriate
            next step.

            Possible next steps include:

            - Booking a service
            - Booking a consultation
            - Contacting Dean
            - Discussing project requirements

            Do not pressure the visitor.

            Do not use aggressive sales language.

            ========================================================
            PROJECT DISCOVERY
            ========================================================

            If someone gives a vague project idea, help clarify it.

            For example:

            Visitor:
            "I want to build an app."

            You may ask useful questions such as:

            - What should the application do?
            - Who will use it?
            - Does it need user accounts?
            - Does it need payments?
            - Does it need a database?
            - Does it need an admin dashboard?

            Ask only a few relevant questions at a time.

            Do not interrogate the visitor.

            ========================================================
            UNKNOWN INFORMATION
            ========================================================

            If you don't know something, be honest.

            For example:

            "I don't have that information available right now.
            You can contact Dean directly and he can help you
            with that."

            Never invent an answer.

            ========================================================
            ACTION LIMITATIONS
            ========================================================

            You are currently an informational assistant.

            Do not claim that you have:

            - Created a booking
            - Sent an email
            - Processed a payment
            - Checked calendar availability
            - Changed a database record

            unless an actual tool has been provided to perform
            that action.

            ========================================================
            RESPONSE STYLE
            ========================================================

            Keep normal responses concise.

            Prefer:

            - Short paragraphs
            - Bullet points when useful
            - Clear explanations
            - Direct answers

            Avoid:

            - Huge walls of text
            - Repeating information
            - Excessive headings
            - Unnecessary disclaimers
            - Generic AI language

            ========================================================
            RELEVANT KNOWLEDGE
            ========================================================

            The following is the knowledge retrieved specifically
            for the visitor's question.

            Use this information when answering.

            {relevantKnowledge}

            PROJECT DISCOVERY

            {discoveryPrompt}

            {memorySection}

            ========================================================
            FINAL INSTRUCTION
            ========================================================

            Answer the visitor's current question naturally and
            accurately using the relevant knowledge above.

            If the relevant knowledge does not contain the answer,
            do not guess.
            """;

        // =========================================================
        // 4. Limit conversation history
        // =========================================================

        var recentHistory = (history ?? [])
            .Where(message =>
                !string.IsNullOrWhiteSpace(message.Role) &&
                !string.IsNullOrWhiteSpace(message.Content))
            .TakeLast(20)
            .ToList();

        // =========================================================
        // 5. Build Claude messages
        // =========================================================

        var messages = new List<object>();

        foreach (var message in recentHistory)
        {
            var role = message.Role.ToLowerInvariant();

            if (role != "user" && role != "assistant")
            {
                continue;
            }

            messages.Add(new
            {
                role,
                content = message.Content
            });
        }

        // Add current question
        messages.Add(new
        {
            role = "user",
            content = question
        });

        // =========================================================
        // 6. Create Claude API request
        // =========================================================

        var request = new
        {
            model = "claude-sonnet-5",
            max_tokens = 1024,
            // Sonnet 5 runs adaptive thinking by default when this is omitted, which can
            // consume the entire max_tokens budget on thinking before any visible text is
            // produced (observed in practice: HTTP 200, content = [thinking block only],
            // stop_reason "max_tokens"). This service makes no tool calls, so the one
            // documented failure mode of disabling thinking (a tool call leaking into
            // visible text) doesn't apply here.
            thinking = new { type = "disabled" },
            system = systemPrompt,
            messages
        };

        var json = JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        // =========================================================
        // 7. Configure Anthropic headers
        // =========================================================

        _httpClient.DefaultRequestHeaders.Remove("x-api-key");
        _httpClient.DefaultRequestHeaders.Remove("anthropic-version");

        _httpClient.DefaultRequestHeaders.Add(
            "x-api-key",
            _settings.ApiKey);

        _httpClient.DefaultRequestHeaders.Add(
            "anthropic-version",
            "2023-06-01");

        // =========================================================
        // 8. Send request
        // =========================================================

        var response = await _httpClient.PostAsync(
            "v1/messages",
            content);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        Console.WriteLine("========== CLAUDE RESPONSE ==========");
        Console.WriteLine(responseBody);
        Console.WriteLine("=====================================");

        // =========================================================
        // 9. Handle HTTP errors
        // =========================================================

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Claude API request failed: " +
                $"{response.StatusCode} - {responseBody}");
        }

        // =========================================================
        // 10. Parse Claude response
        // =========================================================

        try
        {
            using var document =
                JsonDocument.Parse(responseBody);

            var root = document.RootElement;

            // Log the raw response if the structure isn't what we expect.
            if (!root.TryGetProperty("content", out var contentElement))
            {
                throw new InvalidOperationException(
                    $"Claude response did not contain a 'content' property. " +
                    $"Raw response: {responseBody}");
            }

            if (contentElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    $"Claude response 'content' was not an array. " +
                    $"Raw response: {responseBody}");
            }

            if (contentElement.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    $"Claude response contained an empty 'content' array. " +
                    $"Raw response: {responseBody}");
            }

            var textBuilder = new StringBuilder();

            foreach (var block in contentElement.EnumerateArray())
            {
                if (block.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                // Claude normally returns:
                //
                // {
                //     "type": "text",
                //     "text": "..."
                // }

                if (block.TryGetProperty("type", out var typeElement))
                {
                    var type = typeElement.GetString();

                    if (type == "text" &&
                        block.TryGetProperty("text", out var textElement))
                    {
                        var text = textElement.GetString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            textBuilder.Append(text);
                        }
                    }
                }
            }

            var answer = textBuilder
                .ToString()
                .Trim();

            if (!string.IsNullOrWhiteSpace(answer))
            {
                if (!string.IsNullOrWhiteSpace(chatUserId))
                {
                    var conversationForExtraction = new List<ChatMessage>(recentHistory)
                    {
                        new() { Role = "user", Content = question },
                        new() { Role = "assistant", Content = answer },
                    };

                    if ((conversationForExtraction.Count) % 6 == 0)
                    {
                        _ = _memoryExtractionService.ExtractAndStoreAsync(chatUserId, conversationForExtraction);
                    }
                }

                return answer;
            }

            // If we reach this point, Claude returned HTTP 200 but
            // the response wasn't something we expected.
            throw new InvalidOperationException(
                $"Claude returned HTTP 200 but no usable text was found. " +
                $"Raw response: {responseBody}");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Could not parse Claude's JSON response. " +
                $"Raw response: {responseBody}",
                ex);
        }
    }
}