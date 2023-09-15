using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Extensibility.IssueTrackers;
using Inedo.Extensions.Jira.Client;
using Inedo.Extensions.Jira.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.Jira;

[DisplayName("Jira")]
[Description("Provides integration for Jira issue tracking projects.")]
public sealed class JiraServiceInfo : IssueTrackerService<JiraProject, JiraServiceCredentials>
{
    public override string ServiceName => "Jira";
    public override string DefaultVersionFieldName => "Version";
    public override string PasswordDisplayName => "API token";
    public override string UsernameDisplayName => "User email";
    public override string ApiUrlPlaceholderText => "e.g. https://your-company.atlassian.net/";
    public override string ApiUrlDisplayName => "Instance URL";

    protected override async IAsyncEnumerable<string> GetProjectNamesAsync(JiraServiceCredentials credentials, string serviceNamespace = null, [EnumeratorCancellation]CancellationToken cancellationToken = default)
    {
        var client = new JiraClient(credentials.ServiceUrl, credentials.UserName, AH.Unprotect(credentials.Password), this);
        await foreach (var p in client.GetProjectsAsync(cancellationToken))
            yield return p.Name;
    }

    protected override async ValueTask<ValidationResults> ValidateCredentialsAsync(JiraServiceCredentials credentials, CancellationToken cancellationToken = default)
    {
        var client = new JiraClient(credentials.ServiceUrl, credentials.UserName, AH.Unprotect(credentials.Password), this);
        try
        {
            this.LogInformation($"Validating Connection to {credentials.ServiceUrl}...");

            this.LogDebug("Enumerating projects...");
            int found = 0;
            await foreach (var project in client.GetProjectsAsync(cancellationToken).ConfigureAwait(false))
            {
                this.LogDebug($"Found \"{project.Name} ({project.Id})\".");
                if (found++ > 5)
                    break;
            }

            if (found == 0)
                this.LogWarning("No projects were found.");
            else
                this.LogInformation($"Found {found} projects.");

            return true;
        }
        catch (Exception ex)
        {
            return new ValidationResults(false, "An error occurred during validation: " + ex.Message);
        }


    }
}
