using System.Collections.Generic;

namespace Inedo.Extensions.Jira.Clients
{
    internal sealed class JiraIssueType
    {
        public JiraIssueType(Dictionary<string, object> type)
        {
            this.Id = type["id"].ToString();
            this.Name = type["name"].ToString();
        }

        public string Id { get; }
        public string Name { get; }
    }
}
