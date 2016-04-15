using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Jira.Clients;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.Jira
{
    [DisplayName("JIRA")]
    [Description("Supports locally-hosted JIRA installations and instances hosted by Atlassian Cloud.")]
    [CustomEditor(typeof(JiraProviderEditor))]
    public sealed partial class JiraProvider : IssueTrackerConnectionBase, IReleaseManager, IIssueCloser, IIssueCommenter, IIssueStatusUpdater
    {
        private Lazy<CommonJiraClient> getClient;

        public JiraProvider()
        {
            this.getClient = new Lazy<CommonJiraClient>(this.CreateClient);
        }

        [Persistent]
        public string UserName { get; set; }
        [Persistent]
        public string Password { get; set; }
        [Persistent]
        public string BaseUrl { get; set; }
        [Persistent]
        public JiraApiType ApiType { get; set; }
        [Persistent]
        public string ClosedState { get; set; } = "Closed";

        private CommonJiraClient Client => this.getClient.Value;

        public override RichDescription GetDescription()
        {
            return new RichDescription(
                "JIRA at ",
                new Hilite(this.BaseUrl)
            );
        }

        public override bool IsAvailable() => true;

        public override void ValidateConnection()
        {
            this.Client.ValidateConnection();
        }

        public override IEnumerable<IIssueTrackerIssue> EnumerateIssues(IssueTrackerConnectionContext context)
        {
            return this.Client.EnumerateIssues(context);
        }

        public override IssueTrackerApplicationConfigurationBase GetDefaultApplicationConfiguration(int applicationId)
        {
            if (this.legacyFilter != null)
                return this.legacyFilter;

            var application = DB.Applications_GetApplication(applicationId).Applications_Extended.First();
            var projects = this.GetProjects();

            return new JiraApplicationFilter
            {
                ProjectId = projects
                    .Where(p => string.Equals(p.Name, application.Application_Name, StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Id)
                    .FirstOrDefault()
            };
        }

        void IReleaseManager.DeployRelease(IssueTrackerConnectionContext context)
        {
            this.Client.DeployRelease(context);
        }
        void IReleaseManager.CreateRelease(IssueTrackerConnectionContext context)
        {
            this.Client.CreateRelease(context);
        }

        void IIssueCloser.CloseIssue(IssueTrackerConnectionContext context, string issueId)
        {
            this.Client.CloseIssue(context, issueId);
        }
        void IIssueCloser.CloseAllIssues(IssueTrackerConnectionContext context)
        {
            this.Client.CloseAllIssues(context);
        }

        void IIssueCommenter.AddComment(IssueTrackerConnectionContext context, string issueId, string commentText)
        {
            this.Client.AddComment(context, issueId, commentText);
        }

        void IIssueStatusUpdater.ChangeIssueStatus(IssueTrackerConnectionContext context, string issueId, string issueStatus)
        {
            this.Client.ChangeIssueStatus(context, issueId, issueStatus);
        }

        void IIssueStatusUpdater.ChangeStatusForAllIssues(IssueTrackerConnectionContext context, string fromStatus, string toStatus)
        {
            this.Client.ChangeStatusForAllIssues(context, fromStatus, toStatus);
        }

        internal IEnumerable<JiraProject> GetProjects()
        {
            return this.Client.GetProjects();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this.Client.Dispose();

            base.Dispose(disposing);
        }

        private CommonJiraClient CreateClient()
        {
            CommonJiraClient client;
            if (this.ApiType == JiraApiType.SOAP)
                client = new JiraSoapClient(this);
            else if (this.ApiType == JiraApiType.RESTv2)
                client = new JiraRestClient(this);
            else
                throw new InvalidOperationException("Invalid JIRA client version specified: " + this.ApiType);

            this.LogDebug($"Loading JIRA {this.ApiType} client...");

            client.MessageLogged += (s, e) => this.Log(e.Level, e.Message);

            return client;
        }
    }
}