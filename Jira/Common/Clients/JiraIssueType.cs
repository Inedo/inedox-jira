using System.Collections.Generic;
using Inedo.BuildMasterExtensions.Jira.JiraApi;

namespace Inedo.BuildMasterExtensions.Jira.Clients
{
    internal sealed class JiraIssueType
    {
        public JiraIssueType(RemoteIssueType type)
        {
            this.Id = type.id;
            this.Name = type.name;
        }

        public JiraIssueType(Dictionary<string, object> type)
        {
            this.Id = type["id"].ToString();
            this.Name = type["name"].ToString();
        }

        public string Id { get; }
        public string Name { get; }
    }
}
