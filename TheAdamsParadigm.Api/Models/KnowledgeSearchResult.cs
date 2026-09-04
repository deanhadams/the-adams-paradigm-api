namespace TheAdamsParadigm.Api.Models;

public class KnowledgeSearchResult
{
    public string Intent { get; set; } = string.Empty;

    public List<string> Sections { get; set; } = [];

    public string Context { get; set; } = string.Empty;
}