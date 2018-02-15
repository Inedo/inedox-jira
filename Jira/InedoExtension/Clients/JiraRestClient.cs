using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Extensibility.IssueSources;
using Inedo.Extensions.Jira.RestApi;

namespace Inedo.Extensions.Jira.Clients
{
    internal sealed class JiraRestClient : JiraClient
    {
        private RestApiClient restClient;

        public JiraRestClient(string serverUrl, string userName, string password, ILogSink log)
            : base(serverUrl, userName, password, log)
        {
            string host = new Uri(serverUrl, UriKind.Absolute).GetLeftPart(UriPartial.Authority);
            this.restClient = new RestApiClient(host)
            {
                UserName = userName,
                Password = password
            };
        }

        public override Task AddCommentAsync(JiraContext context, string issueId, string commentText) => this.restClient.AddCommentAsync(issueId, commentText);

        public override async Task TransitionIssueAsync(JiraContext context, string issueId, string issueStatus)
        {
            if (string.IsNullOrWhiteSpace(issueStatus))
                throw new ArgumentException("The status being applied must contain text.", nameof(issueStatus));

            var issue = await this.restClient.GetIssueAsync(issueId);
            if (issue.Status == issueStatus)
            {
                this.log?.LogDebug($"{issue.Id} is already in the {issueStatus} status.");
                return;
            }

            await this.ChangeIssueStatusInternalAsync(issue, issueStatus);
        }

        private async Task ChangeIssueStatusInternalAsync(IIssueTrackerIssue issue, string toStatus)
        {
            this.log?.LogDebug($"Changing {issue.Id} to {toStatus} status...");
            var validTransitions = await this.restClient.GetTransitionsAsync(issue.Id);
            var transition = validTransitions.FirstOrDefault(t => t.Name.Equals(toStatus, StringComparison.OrdinalIgnoreCase));
            if (transition == null)
                this.log?.LogError($"Changing the status to {toStatus} is not permitted in the current workflow. The only permitted statuses are: {string.Join(", ", validTransitions.Select(t => t.Name))}");
            else
                await this.restClient.TransitionIssueAsync(issue.Id, transition.Id);
        }

        public override async Task TransitionIssuesInStatusAsync(JiraContext context, string fromStatus, string toStatus)
        {
            if (string.IsNullOrWhiteSpace(toStatus))
                throw new ArgumentException("The status being applied must contain text.", nameof(toStatus));

            foreach (var issue in await this.EnumerateIssuesAsync(context))
            {
                if (!string.IsNullOrEmpty(fromStatus) && !string.Equals(issue.Status, fromStatus, StringComparison.OrdinalIgnoreCase))
                {
                    this.log?.LogDebug($"{issue.Id} ({issue.Status}) is not in the {fromStatus} status, and will not be changed.");
                    continue;
                }

                await this.ChangeIssueStatusInternalAsync(issue, toStatus);
            }
        }

        public override async Task CloseIssueAsync(JiraContext context, string issueId)
        {
            var validTransitions = await this.restClient.GetTransitionsAsync(issueId);
            var closeTransition = validTransitions.FirstOrDefault(t => t.Name.Equals(context.ClosedState, StringComparison.OrdinalIgnoreCase));
            if (closeTransition == null)
                this.log?.LogError($"Cannot close issue {issueId} because the issue cannot be transitioned to the configured closed state on the provider: {context.ClosedState}");
            else
                await this.restClient.TransitionIssueAsync(issueId, closeTransition.Id);
        }

        public override async Task CreateReleaseAsync(JiraContext context)
        {    
            var version = await this.TryGetVersionAsync(context);
            if (version != null)
                return;

            await this.restClient.CreateVersionAsync(context.Project.Key, context.FixForVersion);
        }

        public override async Task DeployReleaseAsync(JiraContext context)
        {
            var version = await this.TryGetVersionAsync(context);
            if (version == null)
                throw new InvalidOperationException("Version " + context.FixForVersion + " does not exist.");

            if (version.Released)
                return;

            await this.restClient.ReleaseVersionAsync(context.Project.Key, version.Id);
        }

        public override Task<IEnumerable<IIssueTrackerIssue>> EnumerateIssuesAsync(JiraContext context)
        {   
            return this.restClient.GetIssuesAsync(context.GetJql());
        }

        public override Task<IEnumerable<JiraProject>> GetProjectsAsync() => this.restClient.GetProjectsAsync();

        public override Task<IEnumerable<ProjectVersion>> GetProjectVersionsAsync(string projectKey) => this.restClient.GetVersionsAsync(projectKey);

        public override async Task<IEnumerable<Transition>> GetTransitionsAsync(JiraContext context)
        {
            var issue = (await this.EnumerateIssuesAsync(context)).LastOrDefault();

            if (issue == null)
                return Enumerable.Empty<Transition>();

            var transitions = await this.restClient.GetTransitionsAsync(issue.Id);
            return transitions;
        }

        public override async Task ValidateConnectionAsync()
        {
            var browsePermission = (await this.restClient.GetPermissionsAsync())
                .FirstOrDefault(p => p.Key.Equals("BROWSE_PROJECTS", StringComparison.OrdinalIgnoreCase));

            if (browsePermission == null || !browsePermission.HasPermission)
                throw new Exception("The specified account cannot browse projects, therefore no issues can be viewed.");
        }

        public override async Task<IIssueTrackerIssue> CreateIssueAsync(JiraContext context, string title, string description, string type)
        {
            return await this.restClient.CreateIssueAsync(context.Project.Key, title, description, type, context.FixForVersion);
        }

        public override Task<IEnumerable<JiraIssueType>> GetIssueTypesAsync(string projectId) => this.restClient.GetIssueTypes(projectId);

        private async Task<ProjectVersion> TryGetVersionAsync(JiraContext context)
        {
            if (string.IsNullOrEmpty(context.FixForVersion))
                throw new ArgumentException(nameof(context.FixForVersion));
            if (string.IsNullOrEmpty(context.Project.Key))
                throw new InvalidOperationException("Application must be specified in category ID filter to create a release.");

            return (await this.restClient.GetVersionsAsync(context.Project.Key)).FirstOrDefault(v => v.Name == context.FixForVersion);
        }
    }
}
