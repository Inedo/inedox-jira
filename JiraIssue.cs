using System;
using System.Collections.Generic;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMasterExtensions.Jira.JiraApi;

namespace Inedo.BuildMasterExtensions.Jira
{
    [Serializable]
    internal sealed class JiraIssue : Issue
    {
        /// <summary>
        /// A collection of static strings describing default workflow statuses for Jira
        /// </summary>
        internal static class DefaultStatusNames
        {
            public static string Open = "Open";
            public static string InProgress = "In Progress";
            public static string Reopened = "Reopened";
            public static string Resolved = "Resolved";
            public static string Closed = "Closed";
        }

        internal string[] AvailableStatusNames { get; private set; }

        internal string IssueStatusId { get; private set; }

        internal JiraIssue(JiraSoapServiceService service, string authToken, RemoteIssue remoteIssue)
        {
            // get list of statuses
            List<string> availableStatusNames = new List<string>();
            RemoteStatus[] remoteStatuses = service.getStatuses(authToken);
            foreach (RemoteStatus remoteStatus in remoteStatuses)
            {
                availableStatusNames.Add(remoteStatus.name);
                if (remoteStatus.id == remoteIssue.status) this.IssueStatus = remoteStatus.name; // set the IssueStatus property to the name of the status, not the id
            }
            AvailableStatusNames = availableStatusNames.ToArray();

            // set the base properties of this issue
            this.IssueDescription = remoteIssue.description;
            this.IssueId = remoteIssue.key;
            this.IssueTitle = remoteIssue.summary;
            this.IssueStatusId = remoteIssue.status; // BuildMaster uses IssueStatus to represent the name of the status, Jira's property "status" represents the id

            // grab the first fix version if there are multiple
            RemoteVersion[] fixVersions = remoteIssue.fixVersions;
            if (fixVersions != null && fixVersions.Length > 0)
            {
                this.ReleaseNumber = fixVersions[0].id; 
            }
        }

        internal bool StatusExists(string status)
        {
            foreach (string availableStatus in AvailableStatusNames)
            {
                if (availableStatus == status) return true;
            }
            return false;
        }
    }
}
