using System.ComponentModel;
using System.Security;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensions.Jira.Clients;

#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.Jira.Credentials;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Credentials;
using Inedo.Otter.Extensibility.Operations;
using Inedo.OtterExtensions.Jira.Credentials;
#endif

namespace Inedo.Extensions.Jira.Operations
{
    public abstract class JiraOperation : ExecuteOperation, IHasCredentials<JiraCredentials>
    {
        public abstract string CredentialName { get; set; }

        [Category("Connection")]
        [ScriptAlias("Server")]
        [PlaceholderText("Use server URL from credential")]
        [MappedCredential(nameof(JiraCredentials.ServerUrl))]
        public string ServerUrl { get; set; }

        [Category("Connection")]
        [ScriptAlias("UserName")]
        [PlaceholderText("Use user name from credential")]
        [MappedCredential(nameof(JiraCredentials.UserName))]
        public string UserName { get; set; }

        [Category("Connection")]
        [ScriptAlias("Password")]
        [PlaceholderText("Use password from credential")]
        [MappedCredential(nameof(JiraCredentials.Password))]
        public SecureString Password { get; set; }

        [Category("Connection")]
        [ScriptAlias("Api")]
        [DisplayName("API type")]
        [DefaultValue("$JiraApiVersion")]
        [Description("If not set, BuildMaster will try to find a $JiraApiVersion variable in scope, or otherwise will attempt to auto-detect the API version.")]
        public JiraApiType Api { get; set; }

        internal JiraProject ResolveProject(JiraClient client, string name)
        {
            this.LogDebug($"Resolving project name '{name}'...");
            var project = client.FindProject(name);

            if (project == null)
            {
                this.LogError($"Project '{name}' was not found.");
                return null;
            }

            this.LogDebug($"Project resolved to key='{project.Key}', id='{project.Id}'.");

            return project;
        }

        internal JiraIssueType ResolveIssueType(JiraClient client, JiraProject project, string issueTypeName)
        {
            this.LogDebug($"Resolving issue type name '{issueTypeName}' for project '{project.Name}'...");
            var issueType = client.FindIssueType(project.Id, issueTypeName);

            if (issueType == null)
            {
                this.LogError($"Issue type '{issueTypeName}' was not found in project '{project.Name}'.");
                return null;
            }

            this.LogDebug($"Issue type ID resolved to '{issueType.Id}'.");

            return issueType;
        }
    }
}
