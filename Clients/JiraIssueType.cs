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

        public string Id { get; }
        public string Name { get; }
    }
}
