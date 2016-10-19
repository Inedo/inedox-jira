using System.ComponentModel;
using System.Security;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.Jira.Credentials
{
    [ScriptAlias("Jira")]
    [DisplayName("JIRA")]
    [Description("Credentials for JIRA.")]
    public sealed class JiraCredentials : ResourceCredentials
    {
        [Required]
        [Persistent]
        [DisplayName("JIRA server URL")]
        public string ServerUrl { get; set; }

        [Required]
        [Persistent]
        [DisplayName("User name")]
        public string UserName { get; set; }

        [Required]
        [Persistent(Encrypted = true)]
        [DisplayName("Password")]
        [FieldEditMode(FieldEditMode.Password)]
        public SecureString Password { get; set; }

        public override RichDescription GetDescription()
        {
            return new RichDescription(this.UserName, " @ ", this.ServerUrl);
        }
    }
}
