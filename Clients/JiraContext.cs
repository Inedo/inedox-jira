using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;

namespace Inedo.BuildMasterExtensions.Jira.Clients
{
    internal sealed class JiraContext
    {
        public JiraContext(string projectKey, string releaseNumber, string closedState)
        {
            this.ProjectKey = projectKey;
            this.ReleaseNumber = releaseNumber;
            this.ClosedState = closedState;
        }

        public JiraContext(JiraProvider provider, IssueTrackerConnectionContext c)
        {
            this.ReleaseNumber = c.ReleaseNumber;
            this.ProjectKey = ((JiraApplicationFilter)c.ApplicationConfiguration ?? provider.legacyFilter)?.ProjectId;
            this.ClosedState = provider.ClosedState;
        }
        
        public string ProjectKey { get; set; }
        public string ReleaseNumber { get; set; }
        public string ClosedState { get; set; }
    }
}
