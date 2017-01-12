using System;
using System.Collections.Generic;

#if BuildMaster
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMasterExtensions.Jira.JiraApi;
#elif Otter
using Inedo.OtterExtensions.Jira;
using Inedo.OtterExtensions.Jira.JiraApi;
#endif

namespace Inedo.Extensions.Jira
{
    [Serializable]
    internal sealed class JiraIssue : IIssueTrackerIssue
    {
        internal RemoteIssue remoteIssue;
        private Dictionary<string, string> statuses;
        private string baseUrl;

        public JiraIssue(RemoteIssue remoteIssue, Dictionary<string, string> statuses, string baseUrl)
        {
            this.remoteIssue = remoteIssue;
            this.statuses = statuses;
            this.baseUrl = baseUrl;
        }

        public string Id => this.remoteIssue.key;
        public string Type => this.remoteIssue.type;
        public string Title => this.remoteIssue.summary;
        public string Description => this.remoteIssue.description;
        public bool IsClosed => !string.IsNullOrEmpty(this.remoteIssue.resolution);
        public string Status => this.statuses.GetValueOrDefault(this.remoteIssue.status, this.remoteIssue.status);
        public DateTime SubmittedDate => this.remoteIssue.created ?? DateTime.UtcNow; 
        public string Submitter => this.remoteIssue.reporter;
        public string Url => this.baseUrl + "/browse/" + Uri.EscapeDataString(this.remoteIssue.key);
    }
}
