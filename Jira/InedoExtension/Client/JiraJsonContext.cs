using System.Text.Json.Serialization;

#nullable enable

namespace Inedo.Extensions.Jira.Client;

[JsonSerializable(typeof(JiraJsonProject[]))]
[JsonSerializable(typeof(JiraJsonIssue[]))]
[JsonSerializable(typeof(JiraVersion[]))]
[JsonSerializable(typeof(JiraTransition[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class JiraJsonContext : JsonSerializerContext
{
}

internal sealed record JiraJsonProject(string Id, string Key, string Name);
internal sealed record JiraJsonIssue(string Id, string Key, JiraIssueFields Fields, JiraIssueRenderedFields RenderedFields);

internal sealed record JiraIssueFields
{
    public static readonly string FieldNames = "created,issuetype,status,summary,description,reporter,resolutiondate";

    public required string Created { get; init; }
    [JsonPropertyName("issuetype")]
    public required JiraNamedThing IssueType { get; init; }
    public required JiraNamedThing Status { get; init; }
    public string? Summary { get; init; }
    public required JiraUser Reporter { get; init; }
    [JsonPropertyName("resolutiondate")]
    public string? ResolutionDate { get; init; }
}
internal sealed record JiraIssueRenderedFields(string? Description);
internal sealed record JiraNamedThing(string Name);
internal sealed record JiraUser(string DisplayName);
internal sealed record JiraVersion(string Id, string Name, bool Archived, bool Released);
internal sealed record JiraTransition(string Id, string Name);
