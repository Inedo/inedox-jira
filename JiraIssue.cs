using System;
using System.Collections.Generic;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMasterExtensions.Jira.JiraApi;

namespace Inedo.BuildMasterExtensions.Jira
{
    [Serializable]
    internal sealed class JiraIssue : IIssueTrackerIssue
    {
        private RemoteIssue remoteIssue;
        private Dictionary<string, string> statuses;
        private string baseUrl;

        public JiraIssue(RemoteIssue remoteIssue, Dictionary<string, string> statuses, string baseUrl)
        {
            this.remoteIssue = remoteIssue;
            this.statuses = statuses;
            this.baseUrl = baseUrl;
        }

        public string Id
        {
            get { return this.remoteIssue.key; }
        }
        public string Title
        {
            get { return this.remoteIssue.summary; }
        }
        public string Description
        {
            get { return this.remoteIssue.description; }
        }
        public bool IsClosed
        {
            get { return !string.IsNullOrEmpty(this.remoteIssue.resolution); }
        }
        public string Status
        {
            get { return this.statuses.GetValueOrDefault(this.remoteIssue.status, this.remoteIssue.status); }
        }
        public DateTime SubmittedDate
        {
            get { return this.remoteIssue.created ?? DateTime.UtcNow; }
        }
        public string Submitter
        {
            get { return this.remoteIssue.reporter; }
        }
        public string Url
        {
            get { return this.baseUrl + "/browse/" + Uri.EscapeDataString(this.remoteIssue.key); }
        }
    }
}
