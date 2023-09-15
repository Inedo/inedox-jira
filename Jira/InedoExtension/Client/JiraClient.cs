using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;

#nullable enable

namespace Inedo.Extensions.Jira.Client;

internal sealed class JiraClient
{
    private readonly ILogSink? log;
    private readonly HttpClient httpClient;

    public JiraClient(string url, string userName, string password, ILogSink? log = null)
    {
        if (!url.EndsWith('/'))
            url += "/";

        url += "rest/api/2/";

        this.httpClient = SDK.CreateHttpClient();
        this.httpClient.BaseAddress = new Uri(url);
        this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(InedoLib.UTF8Encoding.GetBytes($"{userName}:{password}")));
        this.log = log;
    }

    public async Task<JiraJsonProject?> TryGetProjectAsync(string name, CancellationToken cancellationToken = default)
    {
        await foreach (var p in this.GetPaginatedListAsync($"project/search?query={Uri.EscapeDataString(name)}", JiraJsonContext.Default.JiraJsonProjectArray, cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                return p;
        }

        return null;
    }
    public async Task<JiraJsonProject> GetProjectAsync(string name, CancellationToken cancellationToken = default)
    {
        return (await this.TryGetProjectAsync(name, cancellationToken).ConfigureAwait(false))
            ?? throw new ArgumentException($"Project {name} not found in Jira.");
    }

    public IAsyncEnumerable<JiraJsonProject> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        return this.GetPaginatedListAsync("project/search", JiraJsonContext.Default.JiraJsonProjectArray, cancellationToken);
    }
    public IAsyncEnumerable<JiraJsonIssue> GetIssuesAsync(string? jql = null, CancellationToken cancellationToken = default)
    {
        var url = "search";
        if (!string.IsNullOrWhiteSpace(jql))
            url = $"{url}?jql={Uri.EscapeDataString(jql)}";

        return this.GetPaginatedListAsync(url, JiraJsonContext.Default.JiraJsonIssueArray, "issues", true, cancellationToken);
    }

    public IAsyncEnumerable<JiraVersion> GetVersionsAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var url = $"project/{Uri.EscapeDataString(projectId)}/version?orderBy=-name";

        return this.GetPaginatedListAsync(url, JiraJsonContext.Default.JiraVersionArray, cancellationToken);
    }
    public async Task EnsureVersionAsync(string projectId, string version, bool? released, bool? archived, CancellationToken cancellationToken = default)
    {
        var url = $"project/{Uri.EscapeDataString(projectId)}/version?query={Uri.EscapeDataString(version)}";

        JiraVersion? current = null;
        await foreach (var v in this.GetPaginatedListAsync(url, JiraJsonContext.Default.JiraVersionArray, cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(v.Name, version, StringComparison.OrdinalIgnoreCase))
            {
                current = v;
                break;
            }
        }

        if (current == null) // create
        {
            var obj = new JsonObject
            {
                ["projectId"] = projectId,
                ["name"] = version,
                ["released"] = released.GetValueOrDefault(),
                ["archived"] = archived.GetValueOrDefault()
            };

            using var response = await this.httpClient.PostAsync("version", new StringContent(obj.ToJsonString(), InedoLib.UTF8Encoding, "application/json"), cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        else if (current != null && (current.Released != released || current.Archived != archived)) // update
        {
            var obj = new JsonObject
            {
                ["released"] = released.GetValueOrDefault(),
                ["archived"] = archived.GetValueOrDefault()
            };

            using var response = await this.httpClient.PutAsync($"version/{Uri.EscapeDataString(current.Id)}", new StringContent(obj.ToJsonString(), InedoLib.UTF8Encoding, "application/json"), cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        else
        {
            // nothing to do
        }
    }

    public async Task<JiraTransition[]> GetIssueTransitions(string issueIdOrKey, CancellationToken cancellationToken = default)
    {
        using var stream = await this.httpClient.GetStreamAsync($"issue/{issueIdOrKey}/transitions", cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var element = doc.RootElement.GetProperty("transitions");
        if (element.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize(element, JiraJsonContext.Default.JiraTransitionArray)!;
        else
            return Array.Empty<JiraTransition>();
    }
    public async Task TransitionIssueAsync(string issueIdOrKey, string transitionId, string? comment = null, CancellationToken cancellationToken = default)
    {
        var obj = new JsonObject
        {
            ["transition"] = new JsonObject
            {
                ["id"] = transitionId
            }
        };

        if (!string.IsNullOrWhiteSpace(comment))
        {
            obj["update"] = new JsonObject
            {
                ["comment"] = new JsonArray(
                    new JsonObject
                    {
                        ["add"] = new JsonObject
                        {
                            ["body"] = comment
                        }
                    }
                )
            };
        }

        using var response = await this.httpClient.PostAsync(
            $"issue/{issueIdOrKey}/transitions",
            new StringContent(obj.ToJsonString(), InedoLib.UTF8Encoding, "application/json"),
            cancellationToken
        ).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }

    public async Task<string> CreateIssueAsync(string projectId, string summary, string type, string? description = null, CancellationToken cancellationToken = default)
    {
        var obj = new JsonObject
        {
            ["summary"] = summary,
            ["description"] = description,
            ["project"] = new JsonObject
            {
                ["id"] = projectId
            },
            ["issuetype"] = new JsonObject
            {
                ["name"] = type
            }
        };

        using var response = await this.httpClient.PostAsync(
            "issue",
            new StringContent(obj.ToJsonString(), InedoLib.UTF8Encoding, "application/json"),
            cancellationToken
        ).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (doc.RootElement.TryGetProperty("key", out var id) && id.ValueKind == JsonValueKind.String)
            return id.GetString()!;
        else
            return string.Empty;
    }

    public async Task<bool> ProjectHasReleasesEnabled(string projectId, CancellationToken cancellationToken = default)
    {
        using var stream = await this.httpClient.GetStreamAsync($"project/{projectId}/features", cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var element = doc.RootElement.GetProperty("features");
        if (element.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var featureElement in element.EnumerateArray())
        {
            if (featureElement.TryGetProperty("feature", out var featureName) && featureName.ValueKind == JsonValueKind.String && featureName.ValueEquals("jsw.agility.releases"))
                return featureElement.TryGetProperty("state", out var state) && state.ValueKind == JsonValueKind.String && state.ValueEquals("ENABLED");
        }

        return false;
    }

    private IAsyncEnumerable<TEntity> GetPaginatedListAsync<TEntity>(string url, JsonTypeInfo<TEntity[]> jsonTypeInfo, CancellationToken cancellationToken) => this.GetPaginatedListAsync(url, jsonTypeInfo, "values", cancellationToken: cancellationToken);
    private async IAsyncEnumerable<TEntity> GetPaginatedListAsync<TEntity>(string url, JsonTypeInfo<TEntity[]> jsonTypeInfo, string propertyName, bool emptyOn404 = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int startAt = 0;

        while (true)
        {
            var (last, items) = await getPageAsync(startAt).ConfigureAwait(false);
            foreach (var i in items)
                yield return i;

            if (last)
                break;
        }

        async Task<(bool isLast, TEntity[] items)> getPageAsync(int startAt)
        {
            char separator = url.Contains('?') ? '&' : '?';
            this.log?.LogDebug($"Querying: {this.httpClient.BaseAddress}{url}{separator}startAt={startAt}");
            using var response = await this.httpClient.GetAsync($"{url}{separator}startAt={startAt}", HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (emptyOn404 && response.StatusCode == HttpStatusCode.NotFound)
            {
                this.log?.LogDebug("Received 404 response.");
                return (true, Array.Empty<TEntity>());
            }

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (doc.RootElement.TryGetProperty(propertyName, out var values) && values.ValueKind == JsonValueKind.Array)
            {
                int total = doc.RootElement.GetProperty("total").GetInt32();
                var items = JsonSerializer.Deserialize(values, jsonTypeInfo) ?? Array.Empty<TEntity>();
                return (startAt + items.Length >= total, items);
            }
            else
            {
                return (true, Array.Empty<TEntity>());
            }
        }
    }
}
