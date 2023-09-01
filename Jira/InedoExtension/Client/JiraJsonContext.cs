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

internal sealed class JiraJsonProject
{
    [JsonConstructor]
    public JiraJsonProject(string id, string key, string name)
    {
        this.Id = id;
        this.Key = key;
        this.Name = name;
    }

    public string Id { get; }
    public string Key { get; }
    public string Name { get; }
}

internal sealed class JiraJsonIssue
{
    [JsonConstructor]
    public JiraJsonIssue(string id, string key, JiraIssueFields fields)
    {
        this.Id = id;
        this.Key = key;
        this.Fields = fields;
    }

    public string Id { get; }
    public string Key { get; }
    public JiraIssueFields Fields { get; }
    public string? Url { get; set; }
}

internal sealed class JiraIssueFields
{
    [JsonConstructor]
    public JiraIssueFields(string created, JiraNamedThing issueType, JiraNamedThing status, string? summary, string? description, JiraUser reporter, string? resolutionDate)
    {
        this.Created = created;
        this.IssueType = issueType;
        this.Status = status;
        this.Summary = summary;
        this.Description = description;
        this.Reporter = reporter;
        this.ResolutionDate = resolutionDate;
    }

    public string Created { get; }
    [JsonPropertyName("issuetype")]
    public JiraNamedThing IssueType { get; }
    public JiraNamedThing Status { get; }
    public string? Summary { get; }
    public string? Description { get; }
    public JiraUser Reporter { get; }
    [JsonPropertyName("resolutiondate")]
    public string? ResolutionDate { get; }
}

internal sealed class JiraNamedThing
{
    [JsonConstructor]
    public JiraNamedThing(string name) => this.Name = name;

    public string Name { get; }
}

internal sealed class JiraUser
{
    [JsonConstructor]
    public JiraUser(string displayName) => this.DisplayName = displayName;

    public string DisplayName { get; }
}

internal sealed class JiraVersion
{
    [JsonConstructor]
    public JiraVersion(string id, string name, bool archived, bool released)
    {
        this.Id = id;
        this.Name = name;
        this.Archived = archived;
        this.Released = released;
    }

    public string Id { get; }
    public string Name { get; }
    public bool Archived { get; }
    public bool Released { get; }
}

internal sealed class JiraTransition
{
    [JsonConstructor]
    public JiraTransition(string id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public string Id { get; }
    public string Name { get; }
}
