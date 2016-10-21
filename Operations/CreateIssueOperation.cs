using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.Jira.Clients;
using Inedo.BuildMasterExtensions.Jira.SuggestionProviders;
using Inedo.Diagnostics;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.Jira.Operations
{
    [DisplayName("Create Jira Issue")]
    [Description("Creates an issue in Jira.")]
    [Tag(Tags.IssueTracking)]
    [ScriptAlias("Create-Issue")]
    [Example(@"
# create issue for the HDARS project notifying QA that testing is required
Create-Issue(
    Credentials: Jira7Local,
    Project: HDARS,
    Title: QA Testing Required for $ApplicationName,
    Description: This issue was created by BuildMaster on $Date,
    Type: ReadyForQA,
    JiraIssueId => $JiraIssueId
);

Log-Information ""Issue '$JiraIssueId' was created in JIRA."";
")]
    public sealed class CreateIssueOperation : JiraOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [Required]
        [ScriptAlias("Project")]
        [DisplayName("Project name")]
        [SuggestibleValue(typeof(JiraProjectNameSuggestionProvider))]
        public string ProjectName { get; set; }
        [Required]
        [ScriptAlias("Title")]
        [DisplayName("Title")]
        public string Title { get; set; }
        [Required]
        [ScriptAlias("Type")]
        [DisplayName("Type")]
        [SuggestibleValue(typeof(JiraIssueTypeNameSuggestionProvider))]
        public string Type { get; set; }
        [ScriptAlias("Description")]
        [DisplayName("Description")]
        public string Description { get; set; }
        [ScriptAlias("FixFor")]
        [DisplayName("Fix for version")]
        [PlaceholderText("$ReleaseNumber")]
        [SuggestibleValue(typeof(JiraFixForVersionSuggestionProvider))]
        public string ReleaseNumber { get; set; }

        [Output]
        [ScriptAlias("JiraIssueId")]
        [DisplayName("Set issue ID to a variable")]
        [Description("The JIRA issue ID can be output into a runtime variable.")]
        [PlaceholderText("e.g. $JiraIssueId")]
        public string JiraIssueId { get; set; }

        public override Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Creating JIRA issue...");

            var client = JiraClient.Create(this.ServerUrl, this.UserName, this.Password.ToUnsecureString(), this, this.Api);

            var project = this.ResolveProject(client, this.ProjectName);
            if (project == null)
                return Complete;

            var type = this.ResolveIssueType(client, project, this.Type);
            if (type == null)
                return Complete;

            var issue = client.CreateIssue(
                new JiraContext(project, this.ReleaseNumber ?? context.ReleaseNumber, null),
                this.Title,
                this.Description,
                type.Id
            );

            this.JiraIssueId = issue.Id;

            this.LogInformation($"JIRA issue '{issue.Id}' created.");

            return Complete;
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Create Jira Issue for project ", config[nameof(this.ProjectName)])
            );
        }
    }
}
