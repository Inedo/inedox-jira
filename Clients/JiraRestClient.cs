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

        public JiraRestClient(string serverUrl, string userName, string password, ILogger log)
            : base(serverUrl, userName, password, log)
        {
            string host = new Uri(serverUrl, UriKind.Absolute).GetLeftPart(UriPartial.Authority);
            this.restClient = new RestApiClient(host)
            {
                UserName = userName,
                Password = password
            };
        }

        public override void AddComment(JiraContext context, string issueId, string commentText)
        {
            this.restClient.AddComment(issueId, commentText);
        }

        public override void TransitionIssue(JiraContext context, string issueId, string issueStatus)
        {
            if (string.IsNullOrWhiteSpace(issueStatus))
                throw new ArgumentException("The status being applied must contain text.", nameof(issueStatus));

            var issue = this.restClient.GetIssue(issueId);
            if (issue.Status == issueStatus)
            {
                this.log.LogDebug($"{issue.Id} is already in the {issueStatus} status.");
                return;
            }

            this.ChangeIssueStatusInternal(issue, issueStatus);
        }

        private void ChangeIssueStatusInternal(IIssueTrackerIssue issue, string toStatus)
        {
            this.log.LogDebug($"Changing {issue.Id} to {toStatus} status...");
            var validTransitions = this.restClient.GetTransitions(issue.Id);
            var transition = validTransitions.FirstOrDefault(t => t.Name.Equals(toStatus, StringComparison.OrdinalIgnoreCase));
            if (transition == null)
                this.log.LogError($"Changing the status to {toStatus} is not permitted in the current workflow. The only permitted statuses are: {string.Join(", ", validTransitions.Select(t => t.Name))}");
            else
                this.restClient.TransitionIssue(issue.Id, transition.Id);
        }

        public override void TransitionIssuesInStatus(JiraContext context, string fromStatus, string toStatus)
        {
            if (string.IsNullOrWhiteSpace(toStatus))
                throw new ArgumentException("The status being applied must contain text.", nameof(toStatus));

            foreach (var issue in this.EnumerateIssues(context))
            {
                if (!string.IsNullOrEmpty(fromStatus) && issue.Status != fromStatus)
                {
                    this.log.LogDebug($"{issue.Id} is not in the {fromStatus} status, and will not be changed.");
                    continue;
                }

                this.ChangeIssueStatusInternal(issue, toStatus);
            }
        }

        public override void CloseIssue(JiraContext context, string issueId)
        {
            var validTransitions = this.restClient.GetTransitions(issueId);
            var closeTransition = validTransitions.FirstOrDefault(t => t.Name.Equals(context.ClosedState, StringComparison.OrdinalIgnoreCase));
            if (closeTransition == null)
                this.log.LogError($"Cannot close issue {issueId} because the issue cannot be transitioned to the configured closed state on the provider: {context.ClosedState}");
            else
                this.restClient.TransitionIssue(issueId, closeTransition.Id);
        }

        public override void CreateRelease(JiraContext context)
        {    
            var version = this.TryGetVersion(context);
            if (version != null)
                return;

            this.restClient.CreateVersion(context.ProjectKey, context.ReleaseNumber);
        }

        public override void DeployRelease(JiraContext context)
        {
            var version = this.TryGetVersion(context);
            if (version == null)
                throw new InvalidOperationException("Version " + context.ReleaseNumber + " does not exist.");

            if (version.Released)
                return;

            this.restClient.ReleaseVersion(context.ProjectKey, version.Id);
        }

        public override IEnumerable<IIssueTrackerIssue> EnumerateIssues(JiraContext context)
        {
            var version = this.TryGetVersion(context);
            if (version == null)
                return Enumerable.Empty<IIssueTrackerIssue>();

            return this.restClient.GetIssues(context.ProjectKey, version.Name);
        }

        public override IEnumerable<JiraProject> GetProjects()
        {
            return this.restClient.GetProjects();
        }

        public override IEnumerable<Transition> GetTransitions(JiraContext context)
        {
            var issue = this.EnumerateIssues(context).LastOrDefault();

            if (issue == null)
                return Enumerable.Empty<Transition>();

            var transitions = this.restClient.GetTransitions(issue.Id);
            return transitions;
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

        public override IIssueTrackerIssue CreateIssue(JiraContext context, string title, string description, string type)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<JiraIssueType> GetIssueTypes(string projectName)
        {
            throw new NotImplementedException();
        }

        private ProjectVersion TryGetVersion(JiraContext context)
        {
            if (string.IsNullOrEmpty(context.ReleaseNumber))
                throw new ArgumentException(nameof(context.ReleaseNumber));
            if (string.IsNullOrEmpty(context?.ProjectKey))
                throw new InvalidOperationException("Application must be specified in category ID filter to create a release.");

            return this.restClient.GetVersions(context.ProjectKey).FirstOrDefault(v => v.Name == context.ReleaseNumber);
        }
    }
}
