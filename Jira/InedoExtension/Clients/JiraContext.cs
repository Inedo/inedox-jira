﻿namespace Inedo.Extensions.Jira.Clients
{
    internal sealed class JiraContext
    {
        public JiraContext(JiraProject project, string fixForVersion, string customJql)
        {
            this.Project = project ?? new JiraProject();
            this.FixForVersion = fixForVersion;
            this.CustomJql = AH.NullIf(customJql, string.Empty);
        }
#if BuildMaster
        public JiraContext(JiraProvider provider, IssueTrackerConnectionContext c)
        {
            this.FixForVersion = c.ReleaseNumber;
            this.Project = new JiraProject(null, ((JiraApplicationFilter)c.ApplicationConfiguration ?? provider.legacyFilter)?.ProjectId, null);
            this.ClosedState = provider.ClosedState;
        }
#endif

        public JiraProject Project { get; }
        public string FixForVersion { get; }
        public string ClosedState { get; }
        public string CustomJql { get; }
        
        public string GetJql()
        {
            if (!string.IsNullOrEmpty(this.CustomJql))
                return this.CustomJql;

            string jql = $"project='{AH.CoalesceString(this.Project.Key, this.Project.Name)}'";
            if (!string.IsNullOrEmpty(this.FixForVersion))
                jql += $" and fixVersion='{this.FixForVersion}'";

            return jql;
        }
    }
}
