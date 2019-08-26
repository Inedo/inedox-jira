using System.ComponentModel;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Serialization;
using Inedo.Web;
using Inedo.Web.Plans;

namespace Inedo.Extensions.Jira.Credentials
{
    [ScriptAlias(JiraCredentials.TypeName)]
    [DisplayName("JIRA")]
    [Description("Credentials for JIRA.")]
    [PersistFrom("Inedo.BuildMasterExtensions.Jira.Credentials.JiraCredentials,Jira")]
    [PersistFrom("Inedo.OtterExtensions.Jira.Credentials.JiraCredentials,Jira")]
    public sealed class JiraCredentials : ResourceCredentials
    {
        public const string TypeName = "Jira";

        [Required]
        [Persistent]
        [DisplayName("JIRA server URL")]
        public string ServerUrl { get; set; }

        [Persistent]
        [DisplayName("User name")]
        [PlaceholderText("Anonymous")]
        public string UserName { get; set; }

        [Persistent(Encrypted = true)]
        [DisplayName("Password")]
        [FieldEditMode(FieldEditMode.Password)]
        public SecureString Password { get; set; }

        public override RichDescription GetDescription() => new RichDescription(this.UserName, " @ ", this.ServerUrl);

        internal static JiraCredentials TryCreate(string name, IComponentConfiguration config)
        {
            int? projectId = (config.EditorContext as IOperationEditorContext)?.ProjectId ?? AH.ParseInt(AH.CoalesceString(config["ProjectId"], config["ApplicationId"]));
            int? environmentId = AH.ParseInt(config["EnvironmentId"]);

            return (JiraCredentials)ResourceCredentials.TryCreate(JiraCredentials.TypeName, name, environmentId: environmentId, applicationId: projectId, inheritFromParent: false);
        }
    }
}
