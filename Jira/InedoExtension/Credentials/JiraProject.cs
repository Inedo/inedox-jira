using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.ExecutionEngine.Variables;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.IssueTrackers;
using Inedo.Extensions.Jira.Client;
using Inedo.Serialization;

#nullable enable

namespace Inedo.Extensions.Jira.Credentials;

[DisplayName("Jira Project")]
[Description("Work with issues on a Jira project.")]
public sealed class JiraProject : IssueTrackerProject<JiraServiceCredentials>
{
    [Persistent]
    [Category("Advanced Mapping")]
    [DisplayName("Issue mapping query")]
    [PlaceholderText("fixVersion: $JqlValue(«release-mapping-expression»)")]
    [Description("""
                 JQL query used to map issues to a release in a project. Note that this does not need to
                 include project filtering, as only issues in the current project are considered. The $JqlValue
                 function is available to produce properly escaped quoted values.
                 """)]
    public string? CustomMappingQuery { get; set; }

    public override async Task<IssuesQueryFilter> CreateQueryFilterAsync(IVariableEvaluationContext context)
    {
        if (!string.IsNullOrWhiteSpace(this.CustomMappingQuery))
        {
            try
            {
                var query = (await ProcessedString.Parse(this.CustomMappingQuery).EvaluateValueAsync(context).ConfigureAwait(false)).AsString();
                if (string.IsNullOrEmpty(query))
                    throw new InvalidOperationException("Resulting query is an empty string.");

                return new JiraIssuesQueryFilter($"project = \"{this.ProjectName}\" AND ({query})");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not parse the Issue mapping query \"{this.CustomMappingQuery}\": {ex.Message}");
            }
        }

        try
        {
            var expression = (await ProcessedString.Parse($"$JqlValue({AH.CoalesceString(this.SimpleVersionMappingExpression, "$ReleaseNumber")})").EvaluateValueAsync(context).ConfigureAwait(false)).AsString();
            if (string.IsNullOrEmpty(expression))
                throw new InvalidOperationException("Resulting query is an empty string.");

            return new JiraIssuesQueryFilter($"project = \"{this.ProjectName}\" AND fixVersion = {expression}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not parse the simple mapping expression \"{this.SimpleVersionMappingExpression}\": {ex.Message}");
        }
    }
    public override async Task EnsureVersionAsync(IssueTrackerVersion version, ICredentialResolutionContext context, CancellationToken cancellationToken = default)
    {
        var client = this.CreateClient(context);
        var project = await client.GetProjectAsync(this.ProjectName!, cancellationToken).ConfigureAwait(false);
        await client.EnsureVersionAsync(project.Id, version.Version, version.IsClosed, version.IsClosed, cancellationToken).ConfigureAwait(false);
    }

    public override async IAsyncEnumerable<IssueTrackerIssue> EnumerateIssuesAsync(IIssuesEnumerationContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = (JiraIssuesQueryFilter)context.Filter;
        var client = this.CreateClient(context, out var url);
        var rootUrl = url.TrimEnd('/');

        await foreach (var i in client.GetIssuesAsync(query.Jql, cancellationToken).ConfigureAwait(false))
        {
            yield return new IssueTrackerIssue(
                Id: i.Key,
                Status: i.Fields.Status.Name,
                Type: i.Fields.IssueType.Name,
                Title: i.Fields.Summary ?? string.Empty,
                Description: i.Fields.Description ?? string.Empty,
                Submitter: i.Fields.Reporter.DisplayName,
                SubmittedDate: DateTimeOffset.Parse(i.Fields.Created).UtcDateTime,
                IsClosed: i.Fields.ResolutionDate != null,
                Url: $"{rootUrl}/browse/{i.Key}"
            );
        }
    }

    public override async IAsyncEnumerable<IssueTrackerVersion> EnumerateVersionsAsync(ICredentialResolutionContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = this.CreateClient(context);
        var project = await client.GetProjectAsync(this.ProjectName!, cancellationToken).ConfigureAwait(false);

        await foreach (var v in client.GetVersionsAsync(project.Id, cancellationToken).ConfigureAwait(false))
            yield return new IssueTrackerVersion(v.Name, v.Released);
    }

    public override RichDescription GetDescription() => new(this.ProjectName);

    public override async Task TransitionIssuesAsync(string? fromStatus, string toStatus, string? comment, IIssuesEnumerationContext context, CancellationToken cancellationToken = default)
    {
        var query = (JiraIssuesQueryFilter)context.Filter;
        var client = this.CreateClient(context, out var url);

        await foreach (var i in client.GetIssuesAsync(query.Jql, cancellationToken).ConfigureAwait(false))
        {
            if (fromStatus == null || string.Equals(i.Fields.Status.Name, fromStatus, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(i.Fields.Status.Name, toStatus, StringComparison.OrdinalIgnoreCase))
                {
                    this.LogInformation($"Issue {i.Key} status is already {toStatus}.");
                    continue;
                }

                var transitions = await client.GetIssueTransitions(i.Id, cancellationToken).ConfigureAwait(false);
                var match = transitions.FirstOrDefault(t => string.Equals(t.Name, toStatus, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Cannot transition issue {i.Key} from {i.Fields.Status.Name} to {toStatus}. There is no transition defined in Jira.");

                await client.TransitionIssueAsync(i.Id, match.Id, comment, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public override async Task ValidateAsync(ILogSink log, ICredentialResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(this.ProjectName))
        {
            log.LogError("Project has not been specified.");
            return;
        }

        var client = this.CreateClient(context);
        var project = await client.TryGetProjectAsync(this.ProjectName);
        if (project == null)
        {
            log.LogError($"Project {this.ProjectName} was not found in Jira.");
            return;
        }

        if (!await client.ProjectHasReleasesEnabled(project.Id))
            log.LogWarning($"Project {this.ProjectName} does not have the Releases feature enabled in Jira.");
    }

    private JiraClient CreateClient(ICredentialResolutionContext context) => this.CreateClient(context, out _);
    private JiraClient CreateClient(ICredentialResolutionContext context, out string url)
    {
        var creds = this.GetCredentials(context) as JiraServiceCredentials
            ?? throw new InvalidOperationException("Credentials are required to query Jira API.");

        url = creds.ServiceUrl!;
        return new JiraClient(creds.ServiceUrl!, creds.UserName!, AH.Unprotect(creds.Password!));
    }

    internal sealed class JiraIssuesQueryFilter : IssuesQueryFilter
    {
        public JiraIssuesQueryFilter(string jql) => this.Jql = jql;

        public string Jql { get; }
    }

}
