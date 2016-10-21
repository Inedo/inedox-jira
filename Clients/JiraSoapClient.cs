using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMasterExtensions.Jira.JiraApi;
using Inedo.Diagnostics;

namespace Inedo.BuildMasterExtensions.Jira.Clients
{
    internal sealed class JiraSoapClient : JiraClient
    {
        private Lazy<JiraSoapServiceService> getService;
        private Lazy<string> getToken;
        private Lazy<Dictionary<string, string>> getIssueStatuses;

        public JiraSoapClient(string serverUrl, string userName, string password, ILogger log)
            : base(serverUrl, userName, password, log)
        {
            this.getService = new Lazy<JiraSoapServiceService>(
                () => new JiraSoapServiceService { Url = CombinePaths(serverUrl, "rpc/soap/jirasoapservice-v2") }
            );

            this.getToken = new Lazy<string>(
                () => this.Service.login(this.userName, this.password)
            );

            this.getIssueStatuses = new Lazy<Dictionary<string, string>>(
                () =>
                {
                    return this.Service.getStatuses(this.Token)
                        .GroupBy(s => s.id ?? string.Empty, s => s.name)
                        .ToDictionary(s => s.Key, s => s.First());
                }
            );
        }

        private JiraSoapServiceService Service => this.getService.Value;
        private string Token => this.getToken.Value;
        private Dictionary<string, string> IssueStatuses => this.getIssueStatuses.Value;

        public override void ValidateConnection()
        {
            try
            {
                var token = this.Service.login(this.userName, this.password);
                LogOut(this.Service, token);
            }
            catch (Exception ex)
            {
                throw new NotAvailableException(ex.Message, ex);
            }
        }

        public override IEnumerable<JiraProject> GetProjects()
        {
            var remoteProjects = this.Service.getProjectsNoSchemes(this.Token);

            var result = from p in remoteProjects
                         group p by p.key into g
                         select new JiraProject(g.First());

            return result;
        }

        public override IEnumerable<ProjectVersion> GetProjectVersions(string projectKey)
        {
            var remoteVersions = this.Service.getVersions(this.Token, projectKey);

            var result = from v in remoteVersions
                         select new ProjectVersion();

            return result;
        }

        public override IEnumerable<JiraIssueType> GetIssueTypes(string projectId)
        {
            RemoteIssueType[] issueTypes;

            if (!string.IsNullOrEmpty(projectId))
                issueTypes = this.Service.getIssueTypesForProject(this.Token, projectId);
            else
                issueTypes = this.Service.getIssueTypes(this.Token);

            return issueTypes.Select(t => new JiraIssueType(t));
        }

        public override IEnumerable<Transition> GetTransitions(JiraContext context)
        {
            return Enumerable.Empty<Transition>();
        }

        public override void AddComment(JiraContext context, string issueId, string commentText)
        {
            var comment = new RemoteComment { body = commentText };
            this.Service.addComment(this.Token, issueId, comment);
        }

        public override void TransitionIssue(JiraContext context, string issueId, string issueStatus)
        {
            // verify status name is text
            issueStatus = (issueStatus ?? "").Trim();
            if (string.IsNullOrEmpty(issueStatus) || issueStatus.Length < 2)
                throw new ArgumentException("The status being applied must contain text and be at least 2 characters long", "newStatus");

            // return if the issue is already set to the new status
            var issue = this.Service.getIssue(this.Token, issueId);
            var jiraIssue = new JiraIssue(issue, this.IssueStatuses, this.serverUrl);
            if (jiraIssue.Status == issueStatus)
            {
                this.log.LogDebug($"{jiraIssue.Id} is already in the {issueStatus} status.");
                return;
            }

            this.ChangeIssueStatusInternal(jiraIssue, issueStatus);
        }

        public override void TransitionIssuesInStatus(JiraContext context, string fromStatus, string toStatus)
        {
            // verify status name is text
            toStatus = (toStatus ?? "").Trim();
            if (string.IsNullOrEmpty(toStatus) || toStatus.Length < 2)
                throw new ArgumentException("The status being applied must contain text and be at least 2 characters long", "newStatus");

            foreach (var jiraIssue in this.EnumerateIssues(context))
            {
                if (!string.IsNullOrEmpty(fromStatus) && jiraIssue.Status != fromStatus)
                {
                    this.log.LogDebug($"{jiraIssue.Id} is not in the {fromStatus} status, and will not be changed.");
                    continue;
                }

                this.ChangeIssueStatusInternal((JiraIssue)jiraIssue, toStatus);
            }
        }

        public override void CloseIssue(JiraContext context, string issueId)
        {
            var availableActions = this.Service.getAvailableActions(this.Token, issueId);
            var closeAction = availableActions
                .FirstOrDefault(a => string.Equals(a.name, "Close Issue", StringComparison.OrdinalIgnoreCase));

            this.Service.progressWorkflowAction(
                this.Token,
                issueId,
                closeAction.id,
                new RemoteFieldValue[0]
            );
        }

        public override void CreateRelease(JiraContext context)
        {
            var releaseNumber = context.FixForVersion;
            if (string.IsNullOrEmpty(releaseNumber))
                throw new ArgumentNullException("releaseNumber");

            if (string.IsNullOrEmpty(context.Project.Key))
                throw new InvalidOperationException("Application must be specified in category ID filter to create a release.");

            // If version is already created, do nothing.
            var versions = this.Service.getVersions(this.Token, context.Project.Key);
            if (Array.Find(versions, v => releaseNumber.Equals((v.name ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) != null)
                return;

            // Otherwise add it.
            this.Service.addVersion(this.Token, context.Project.Key, new RemoteVersion { name = releaseNumber });

        }

        public override void DeployRelease(JiraContext context)
        {
            var releaseNumber = context.FixForVersion;
            if (string.IsNullOrEmpty(releaseNumber))
                throw new ArgumentNullException("releaseNumber");

            if (string.IsNullOrEmpty(context.Project.Key))
                throw new InvalidOperationException("Application must be specified in category ID filter to close a release.");

            // Ensure version exists.
            var versions = this.Service.getVersions(this.Token, context.Project.Key);
            var version = Array.Find(versions, v => releaseNumber.Equals((v.name ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
            if (version == null)
                throw new InvalidOperationException("Version " + releaseNumber + " does not exist.");

            // If version is already released, do nothing.
            if (version.released)
                return;

            // Otherwise release it.
            version.released = true;
            version.releaseDate = DateTime.Now;
            this.Service.releaseVersion(this.Token, context.Project.Key, version);
        }

        public override IEnumerable<IIssueTrackerIssue> EnumerateIssues(JiraContext context)
        {
            var version = this.Service.getVersions(this.Token, context.Project.Key)
                .FirstOrDefault(v => string.Equals(v.name, context.FixForVersion, StringComparison.OrdinalIgnoreCase));

            if (version == null)
                return Enumerable.Empty<IIssueTrackerIssue>();

            var projectFilter = string.Empty;
            if (!string.IsNullOrEmpty(context.Project.Key))
                projectFilter = " and project = \"" + context.Project.Key + "\"";

            var issues = this.Service.getIssuesFromJqlSearch(
                this.Token,
                string.Format("fixVersion = \"{0}\" {1}", context.FixForVersion, projectFilter),
                int.MaxValue
            );

            if (issues.Length == 0)
                return Enumerable.Empty<IIssueTrackerIssue>();

            var baseUrl = this.serverUrl.TrimEnd('/');

            return from i in issues
                   select new JiraIssue(i, this.IssueStatuses, baseUrl);
        }

        public override Task<IEnumerable<IIssueTrackerIssue>> EnumerateIssuesAsync(JiraContext context)
        {
            var issues = this.EnumerateIssues(context);
            return Task.FromResult(issues);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.getToken.IsValueCreated)
                    LogOut(this.Service, this.Token);
            }

            base.Dispose(disposing);
        }

        private void ChangeIssueStatusInternal(JiraIssue issue, string toStatus)
        {
            this.log.LogDebug($"Changing {issue.Id} to {toStatus} status...");

            // get available actions for the issue (e.g. "Resolve issue" or "Close issue")
            var availableActions = this.Service.getAvailableActions(this.Token, issue.Id);

            // build a list of permitted action names and grab the id of the action that contains the newStatus (i.e. "Resolve issue" contains all but the last char in "Resolved")
            var permittedActions = new List<string>();
            string actionId = null;
            string newStatusPart = toStatus.Substring(0, toStatus.Length - 1);
            foreach (var action in availableActions)
            {
                permittedActions.Add(action.name);
                if (action.name.Contains(newStatusPart))
                    actionId = action.id;
            }

            if (actionId == null)
            {
                this.log.LogError($"Changing the status to {toStatus} is not permitted in the current workflow. The only permitted statuses are: {string.Join(", ", availableActions.Select(a => a.name))}");
            }
            else
            {
                this.Service.progressWorkflowAction(
                    this.Token,
                    issue.Id,
                    actionId,
                    new RemoteFieldValue[0]
                );
            }
        }

        public override IIssueTrackerIssue CreateIssue(JiraContext context, string title, string description, string type)
        {
            var versions = this.Service.getVersions(this.Token, context.Project.Key)
                .Where(v => string.Equals(v.name, context.FixForVersion, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (context.FixForVersion != null && versions.Length == 0)
                this.log.LogWarning($"Could not set Fix For version to '{context.FixForVersion}' because it was not found in JIRA for project key '{context.Project.Key}'.");

            var issue = this.Service.createIssue(
                this.Token,
                new RemoteIssue
                {
                    project = context.Project.Key,
                    summary = title,
                    description = description,
                    type = type,
                    fixVersions = versions
                }
            );

            return new JiraIssue(issue, this.IssueStatuses, this.serverUrl);
        }

        private static void LogOut(JiraSoapServiceService service, string token)
        {
            try
            {
                service.logout(token);
            }
            catch
            {
            }
        }

        private static string CombinePaths(string baseUrl, string relativeUrl)
        {
            if (baseUrl.EndsWith("/"))
            {
                return relativeUrl.StartsWith("/")
                    ? baseUrl + relativeUrl.Substring(1, relativeUrl.Length - 1)
                    : baseUrl + relativeUrl;
            }
            else
            {
                return relativeUrl.StartsWith("/")
                    ? baseUrl + relativeUrl
                    : baseUrl + "/" + relativeUrl;
            }
        }
    }
}
