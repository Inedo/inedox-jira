using System;
using System.Collections.Generic;
using System.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Jira.JiraApi;

namespace Inedo.BuildMasterExtensions.Jira
{
    [ProviderProperties(
       "JIRA",
       "Supports JIRA 4.0 and later.")]
    [CustomEditor(typeof(JiraProviderEditor))]
    public sealed partial class JiraProvider : IssueTrackerConnectionBase, IReleaseManager, IIssueCloser, IIssueCommenter
    {
        private Lazy<JiraSoapServiceService> getService;
        private Lazy<string> getToken;
        private Lazy<Dictionary<string, string>> getIssueStatuses;

        public JiraProvider()
        {
            this.getService = new Lazy<JiraSoapServiceService>(
                () => new JiraSoapServiceService { Url = CombinePaths(this.BaseUrl, this.RelativeServiceUrl) }
            );

            this.getToken = new Lazy<string>(
                () => this.Service.login(this.UserName, this.Password)
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

        [Persistent]
        public string UserName { get; set; }
        [Persistent]
        public string Password { get; set; }
        [Persistent]
        public string BaseUrl { get; set; }
        [Persistent]
        public string RelativeServiceUrl { get; set; }

        private JiraSoapServiceService Service
        {
            get { return this.getService.Value; }
        }
        private string Token
        {
            get { return this.getToken.Value; }
        }
        private Dictionary<string, string> IssueStatuses
        {
            get { return this.getIssueStatuses.Value; }
        }

        public override ExtensionComponentDescription GetDescription()
        {
            return new ExtensionComponentDescription(
                "JIRA at ",
                new Hilite(this.BaseUrl)
            );
        }

        public override bool IsAvailable()
        {
            return true;
        }

        public override void ValidateConnection()
        {
            try
            {
                var token = this.Service.login(this.UserName, this.Password);
                LogOut(this.Service, token);
            }
            catch (Exception ex)
            {
                throw new NotAvailableException(ex.Message, ex);
            }
        }

        public override IEnumerable<IIssueTrackerIssue> EnumerateIssues(IssueTrackerConnectionContext context)
        {
            var filter = this.GetFilter(context);
            var version = this.Service.getVersions(this.Token, filter.ProjectId)
                .FirstOrDefault(v => string.Equals(v.name, context.ReleaseNumber, StringComparison.OrdinalIgnoreCase));

            if (version == null)
                return Enumerable.Empty<IIssueTrackerIssue>();

            var projectFilter = string.Empty;
            if (!string.IsNullOrEmpty(filter.ProjectId))
                projectFilter = " and project = \"" + filter.ProjectId + "\"";

            var issues = this.Service.getIssuesFromJqlSearch(
                this.Token,
                string.Format("fixVersion = \"{0}\" {1}", context.ReleaseNumber, projectFilter),
                int.MaxValue
            );

            if (issues.Length == 0)
                return Enumerable.Empty<IIssueTrackerIssue>();

            return from i in issues
                   select new JiraIssue(i, this.IssueStatuses);
        }

        public override IssueTrackerApplicationConfiguration GetDefaultApplicationConfiguration(int applicationId)
        {
            if (this.legacyFilter != null)
                return this.legacyFilter;

            var application = StoredProcs.Applications_GetApplication(applicationId).Execute().Applications_Extended.First();
            var projects = this.GetProjects();

            return new JiraApplicationFilter
            {
                ProjectId = projects
                    .Where(p => string.Equals(p.Value, application.Application_Name, StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Key)
                    .FirstOrDefault()
            };
        }

        internal Dictionary<string, string> GetProjects()
        {
            var remoteProjects = this.Service.getProjectsNoSchemes(this.Token);
            return remoteProjects
                .GroupBy(p => p.key, p => p.name)
                .ToDictionary(p => p.Key, p => p.First());
        }

        void IReleaseManager.DeployRelease(IssueTrackerConnectionContext context)
        {
            var releaseNumber = context.ReleaseNumber;
            if (string.IsNullOrEmpty(releaseNumber))
                throw new ArgumentNullException("releaseNumber");

            var filter = this.GetFilter(context);

            if (filter == null || string.IsNullOrEmpty(filter.ProjectId))
                throw new InvalidOperationException("Application must be specified in category ID filter to close a release.");

            // Ensure version exists.
            var versions = this.Service.getVersions(this.Token, filter.ProjectId);
            var version = Array.Find(versions, v => releaseNumber.Equals((v.name ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
            if(version == null)
                throw new InvalidOperationException("Version " + releaseNumber + " does not exist.");

            // If version is already released, do nothing.
            if (version.released)
                return;

            // Otherwise release it.
            version.released = true;
            version.releaseDate = DateTime.Now;
            this.Service.releaseVersion(this.Token, filter.ProjectId, version);
        }
        void IReleaseManager.CreateRelease(IssueTrackerConnectionContext context)
        {
            var releaseNumber = context.ReleaseNumber;
            if (string.IsNullOrEmpty(releaseNumber))
                throw new ArgumentNullException("releaseNumber");

            var filter = this.GetFilter(context);

            if (filter == null || string.IsNullOrEmpty(filter.ProjectId))
                throw new InvalidOperationException("Application must be specified in category ID filter to create a release.");

            // If version is already created, do nothing.
            var versions = this.Service.getVersions(this.Token, filter.ProjectId);
            if (Array.Find(versions, v => releaseNumber.Equals((v.name ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) != null)
                return;

            // Otherwise add it.
            this.Service.addVersion(this.Token, filter.ProjectId, new RemoteVersion { name = releaseNumber });
        }

        void IIssueCloser.CloseIssue(IssueTrackerConnectionContext context, string issueId)
        {
            this.CloseIssueInternal(issueId);
        }
        void IIssueCloser.CloseAllIssues(IssueTrackerConnectionContext context)
        {
            foreach (var issue in this.EnumerateIssues(context))
                this.CloseIssueInternal(issue.Id);
        }

        void IIssueCommenter.AddComment(IssueTrackerConnectionContext context, string issueId, string commentText)
        {
            var comment = new RemoteComment { body = commentText };
            this.Service.addComment(this.Token, issueId, comment);
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

        private JiraApplicationFilter GetFilter(IssueTrackerConnectionContext context)
        {
            return (JiraApplicationFilter)context.ApplicationConfiguration ?? this.legacyFilter;
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

        private void CloseIssueInternal(string issueId)
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
    }
}