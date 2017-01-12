using System.ComponentModel;
using System.Security;
using Inedo.Documentation;
using Inedo.Serialization;

#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web;
namespace Inedo.BuildMasterExtensions.Jira.Credentials
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Credentials;
using Inedo.Otter.Extensions;
namespace Inedo.OtterExtensions.Jira.Credentials
#endif
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
        
        [Persistent]
        [DisplayName("User name")]
        [PlaceholderText("Anonymous")]
        public string UserName { get; set; }

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
