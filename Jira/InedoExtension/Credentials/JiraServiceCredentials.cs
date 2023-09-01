using System.ComponentModel;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.IssueTrackers;
using Inedo.Serialization;
using Inedo.Web;

#nullable enable

namespace Inedo.Extensions.Jira.Credentials;

[DisplayName("Jira Service")]
[Description("Provides secure access to YouTrack.")]
public sealed class JiraServiceCredentials : ServiceCredentials, IIssueTrackerServiceCredentials
{
    [Persistent]
    [DisplayName("User email")]
    public string? UserName { get; set; }
    [Persistent(Encrypted = true)]
    [DisplayName("API token")]
    public SecureString? Password { get; set; }

    public override RichDescription GetCredentialDescription() => new("(secret)");
    public override RichDescription GetServiceDescription()
    {
        return this.TryGetServiceUrlHostName(out var hostName)
            ? new("Jira (", new Hilite(hostName), ")")
            : new("Jira");
    }

    public override ValueTask<ValidationResults> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var service = new JiraServiceInfo();
        service.MessageLogged += (_, args) => this.Log(args.Level, args.Message, args.Details, args.Category, args.ContextData, args.Exception);
        return service.ValidateCredentialsAsync(this, cancellationToken);
    }
}
