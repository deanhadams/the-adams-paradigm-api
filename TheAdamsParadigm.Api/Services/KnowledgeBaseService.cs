using System.Text.Json;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Services;

public class KnowledgeBaseService
{
    private readonly KnowledgeBase _knowledgeBase;

    public KnowledgeBaseService(IWebHostEnvironment environment)
    {
        var filePath = Path.Combine(
            environment.ContentRootPath,
            "Data",
            "knowledge-base.json");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Knowledge base file was not found.",
                filePath);
        }

        var json = File.ReadAllText(filePath);

        _knowledgeBase = JsonSerializer.Deserialize<KnowledgeBase>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
            ?? throw new InvalidOperationException(
                "Failed to deserialize the knowledge base.");
    }

    public KnowledgeBase GetKnowledgeBase()
    {
        return _knowledgeBase;
    }
}