using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.IssueSources;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Web;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.Jira.Clients;
using Inedo.BuildMasterExtensions.Jira.Credentials;
using Inedo.BuildMasterExtensions.Jira.SuggestionProviders;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.Jira.IssueSources
{
    [DisplayName("JIRA Issue Source")]
    [Description("Issue source for JIRA.")]
    public sealed class JiraIssueSource : IssueSource, IHasCredentials<JiraCredentials>
    {
        [Persistent]
        [DisplayName("Credentials")]
        [TriggerPostBackOnChange]
        public string CredentialName { get; set; }
        [Persistent]
        [DisplayName("Project name")]
        [SuggestibleValue(typeof(JiraProjectNameSuggestionProvider))]
        [TriggerPostBackOnChange]
        public string ProjectName { get; set; }
        [Persistent]
        [DisplayName("Fix for version")]
        [SuggestibleValue(typeof(JiraFixForVersionSuggestionProvider))]
        public string FixForVersion { get; set; }
        [Persistent]
        [DisplayName("Custom JQL")]
        [PlaceholderText("Use above fields")]
        [FieldEditMode(FieldEditMode.Multiline)]
        [Description("Custom JQL will ignore the project name and 'fix for' versions if supplied. "
            +"See the <a href=\"https://confluence.atlassian.com/jirasoftwarecloud/advanced-searching-764478330.html\">JIRA Advanced searching documentation</a> "
            +"for more information.")]
        public string CustomJql { get; set; }

        public async override Task<IEnumerable<IIssueTrackerIssue>> EnumerateIssuesAsync(IIssueSourceEnumerationContext context)
        {
            var credentials = this.TryGetCredentials<JiraCredentials>();

            var client = JiraClient.Create(credentials.ServerUrl, credentials.UserName, credentials.Password.ToUnsecureString(), context.Log);

            var project = new JiraProject(null, null, this.ProjectName);
            var issueContext = new JiraContext(project, this.FixForVersion, this.CustomJql);

            var issues = await client.EnumerateIssuesAsync(issueContext).ConfigureAwait(false);
            return issues;
        }

        public override RichDescription GetDescription()
        {
            if (!string.IsNullOrEmpty(this.CustomJql))
                return new RichDescription("Get Issues from JIRA Using Custom JQL");
            else
                return new RichDescription(
                    "Get Issues from ", new Hilite(this.ProjectName), " in JIRA for version ", new Hilite(this.FixForVersion)
                );
        }
    }
}
