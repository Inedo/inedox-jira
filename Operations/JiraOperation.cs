using System.ComponentModel;
using System.Security;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.Jira.Clients;
using Inedo.BuildMasterExtensions.Jira.Credentials;
using Inedo.Diagnostics;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.Jira.Operations
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
        [Description("Instances of JIRA v5 and earlier require the SOAP API.")]
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
