using System.Collections.Generic;

#if BuildMaster
using Inedo.BuildMasterExtensions.Jira.JiraApi;
#elif Otter
using Inedo.OtterExtensions.Jira.JiraApi;
#endif

namespace Inedo.Extensions.Jira.Clients
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
