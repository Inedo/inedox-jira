using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Inedo.Extensibility.IssueTrackers;
using Inedo.Extensions.Jira.Client;
using Inedo.Extensions.Jira.Credentials;

namespace Inedo.Extensions.Jira;

[DisplayName("Jira")]
[Description("Provides integration for Jira issue tracking projects.")]
public sealed class JiraServiceInfo : IssueTrackerService<JiraProject, JiraServiceCredentials>
{
    public override string ServiceName => "Jira";
    public override string DefaultVersionFieldName => "Version";

    protected override async IAsyncEnumerable<string> GetProjectNamesAsync(JiraServiceCredentials credentials, string serviceNamespace = null, [EnumeratorCancellation]CancellationToken cancellationToken = default)
    {
        var client = new JiraClient(credentials.ServiceUrl, credentials.UserName, AH.Unprotect(credentials.Password));
        await foreach (var p in client.GetProjectsAsync(cancellationToken))
            yield return p.Name;
    }
}
