using System;
using System.Collections.Generic;
using System.Linq;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMasterExtensions.Jira.RestApi;
using Inedo.Diagnostics;

namespace Inedo.BuildMasterExtensions.Jira.Clients
{
    internal sealed class JiraRestClient : CommonJiraClient
    {
        private RestApiClient restClient;

        public JiraRestClient(JiraProvider provider)
            : base(provider)
        {
            string hostAndPort = new Uri(provider.BaseUrl).GetLeftPart(UriPartial.Authority);
            this.restClient = new RestApiClient(hostAndPort)
            {
                UserName = provider.UserName,
                Password = provider.Password
            };
        }

        public override void AddComment(IssueTrackerConnectionContext context, string issueId, string commentText)
        {
            this.restClient.AddComment(issueId, commentText);
        }

        public override void ChangeIssueStatus(IssueTrackerConnectionContext context, string issueId, string issueStatus)
        {
            if (string.IsNullOrWhiteSpace(issueStatus))
                throw new ArgumentException("The status being applied must contain text.", nameof(issueStatus));

            var issue = this.restClient.GetIssue(issueId);
            if (issue.Status == issueStatus)
            {
                this.LogDebug($"{issue.Id} is already in the {issueStatus} status.");
                return;
            }

            this.ChangeIssueStatusInternal(issue, issueStatus);
        }

        private void ChangeIssueStatusInternal(IIssueTrackerIssue issue, string toStatus)
        {
            this.LogDebug($"Changing {issue.Id} to {toStatus} status...");
            var validTransitions = this.restClient.GetTransitions(issue.Id);
            var transition = validTransitions.FirstOrDefault(t => t.Name.Equals(toStatus, StringComparison.OrdinalIgnoreCase));
            if (transition == null)
                this.LogError($"Changing the status to {toStatus} is not permitted in the current workflow. The only permitted statuses are: {string.Join(", ", validTransitions.Select(t => t.Name))}");
            else
                this.restClient.TransitionIssue(issue.Id, transition.Id);
        }

        public override void ChangeStatusForAllIssues(IssueTrackerConnectionContext context, string fromStatus, string toStatus)
        {
            if (string.IsNullOrWhiteSpace(toStatus))
                throw new ArgumentException("The status being applied must contain text.", nameof(toStatus));

            foreach (var issue in this.EnumerateIssues(context))
            {
                if (!string.IsNullOrEmpty(fromStatus) && issue.Status != fromStatus)
                {
                    this.LogDebug($"{issue.Id} is not in the {fromStatus} status, and will not be changed.");
                    continue;
                }

                this.ChangeIssueStatusInternal(issue, toStatus);
            }
        }

        public override void CloseIssue(IssueTrackerConnectionContext context, string issueId)
        {
            var validTransitions = this.restClient.GetTransitions(issueId);
            var closeTransition = validTransitions.FirstOrDefault(t => t.Name.Equals(this.Provider.ClosedState, StringComparison.OrdinalIgnoreCase));
            if (closeTransition == null)
                this.LogError($"Cannot close issue {issueId} because the issue cannot be transitioned to the configured closed state on the provider: {this.Provider.ClosedState}");
            else
                this.restClient.TransitionIssue(issueId, closeTransition.Id);
        }

        public override void CreateRelease(IssueTrackerConnectionContext context)
        {
            var filter = this.GetFilter(context);
            var version = this.TryGetVersion(context, filter);
            if (version != null)
                return;

            this.restClient.CreateVersion(filter.ProjectId, context.ReleaseNumber);
        }

        public override void DeployRelease(IssueTrackerConnectionContext context)
        {
            var filter = this.GetFilter(context);
            var version = this.TryGetVersion(context, filter);
            if (version == null)
                throw new InvalidOperationException("Version " + context.ReleaseNumber + " does not exist.");

            if (version.Released)
                return;

            this.restClient.ReleaseVersion(filter.ProjectId, version.Id);
        }

        public override IEnumerable<IIssueTrackerIssue> EnumerateIssues(IssueTrackerConnectionContext context)
        {
            var filter = this.GetFilter(context);
            var version = this.TryGetVersion(context, filter);
            if (version == null)
                return Enumerable.Empty<IIssueTrackerIssue>();

            return this.restClient.GetIssues(filter.ProjectId, version.Name);
        }

        public override IEnumerable<JiraProject> GetProjects()
        {
            return this.restClient.GetProjects();
        }

        public override void ValidateConnection()
        {
            try
            {
                var browsePermission = this.restClient
                    .GetPermissions()
                    .FirstOrDefault(p => p.Key.Equals("BROWSE_PROJECTS", StringComparison.OrdinalIgnoreCase));

                if (browsePermission == null || !browsePermission.HasPermission)
                    throw new Exception("The specified account cannot browse projects, therefore no issues can be viewed.");
            }
            catch (Exception ex)
            {
                throw new NotAvailableException(ex.Message, ex);
            }
        }

        private ProjectVersion TryGetVersion(IssueTrackerConnectionContext context, JiraApplicationFilter filter)
        {
            if (string.IsNullOrEmpty(context.ReleaseNumber))
                throw new ArgumentException(nameof(context.ReleaseNumber));
            if (string.IsNullOrEmpty(filter?.ProjectId))
                throw new InvalidOperationException("Application must be specified in category ID filter to create a release.");

            return this.restClient.GetVersions(filter.ProjectId).FirstOrDefault(v => v.Name == context.ReleaseNumber);
        }
    }
}
