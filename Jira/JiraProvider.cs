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
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed partial class JiraProvider : IssueTrackerConnectionBase, IReleaseManager, IIssueCloser, IIssueCommenter, IIssueStatusUpdater
    {
        private Lazy<JiraClient> getClient;

        public JiraProvider()
        {
            this.getClient = new Lazy<JiraClient>(this.CreateClient);
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

        private JiraClient Client => this.getClient.Value;

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
            return this.Client.EnumerateIssues(new JiraContext(this, context));
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
                    .Select(p => p.Key)
                    .FirstOrDefault()
            };
        }

        void IReleaseManager.DeployRelease(IssueTrackerConnectionContext context)
        {
            this.Client.DeployRelease(new JiraContext(this, context));
        }
        void IReleaseManager.CreateRelease(IssueTrackerConnectionContext context)
        {
            this.Client.CreateRelease(new JiraContext(this, context));
        }

        void IIssueCloser.CloseIssue(IssueTrackerConnectionContext context, string issueId)
        {
            this.Client.CloseIssue(new JiraContext(this, context), issueId);
        }
        void IIssueCloser.CloseAllIssues(IssueTrackerConnectionContext context)
        {
            this.Client.CloseAllIssues(new JiraContext(this, context));
        }

        void IIssueCommenter.AddComment(IssueTrackerConnectionContext context, string issueId, string commentText)
        {
            this.Client.AddComment(new JiraContext(this, context), issueId, commentText);
        }

        void IIssueStatusUpdater.ChangeIssueStatus(IssueTrackerConnectionContext context, string issueId, string issueStatus)
        {
            this.Client.TransitionIssue(new JiraContext(this, context), issueId, issueStatus);
        }

        void IIssueStatusUpdater.ChangeStatusForAllIssues(IssueTrackerConnectionContext context, string fromStatus, string toStatus)
        {
            this.Client.TransitionIssuesInStatus(new JiraContext(this, context), fromStatus, toStatus);
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

        private JiraClient CreateClient()
        {
            var client = JiraClient.Create(this.BaseUrl, this.UserName, this.Password, this, this.ApiType);
            return client;
        }
    }
}