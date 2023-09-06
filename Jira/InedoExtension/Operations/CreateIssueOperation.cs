using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Jira.Client;
using Inedo.Extensions.Jira.Credentials;

namespace Inedo.Extensions.Jira.Operations
{
    [Description("Creates an issue in Jira.")]
    [ScriptAlias("Create-Issue")]
    [Example(@"
# create issue for the HDARS project notifying QA that testing is required
Create-Issue(
    From: Jira7Local,
    Title: QA Testing Required for $ApplicationName,
    Description: This issue was created by BuildMaster on $Date,
    Type: ReadyForQA,
    JiraIssueId => $JiraIssueId
);

Log-Information ""Issue '$JiraIssueId' was created in JIRA."";
")]
    public sealed class CreateIssueOperation : JiraOperation
    {
        [Required]
        [ScriptAlias("Title")]
        [DisplayName("Title")]
        public string Title { get; set; }
        [Required]
        [ScriptAlias("Type")]
        [DisplayName("Type")]
        public string Type { get; set; }
        [ScriptAlias("Description")]
        [DisplayName("Description")]
        public string Description { get; set; }
        [ScriptAlias("FixFor")]
        [DisplayName("Fix for version")]
        [DefaultValue("$ReleaseNumber")]
        public string FixForVersion { get; set; }

        [Output]
        [ScriptAlias("JiraIssueId")]
        [DisplayName("Set issue ID to a variable")]
        [Description("The JIRA issue ID can be output into a runtime variable.")]
        [PlaceholderText("e.g. $JiraIssueId")]
        public string JiraIssueId { get; set; }

        private protected override async Task ExecuteAsync(IOperationExecutionContext context, JiraClient client, JiraProject project)
        {
            var proj = await client.TryGetProjectAsync(project.ProjectName, context.CancellationToken)
                ?? throw new ExecutionFailureException($"Project {project.ProjectName} not found in Jira.");

            this.JiraIssueId = await client.CreateIssueAsync(proj.Id, this.Title, this.Type, this.Description, context.CancellationToken);
            this.LogInformation($"Jira issue {this.JiraIssueId} created.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Create Jira Issue for project ", config[nameof(this.ProjectName)])
            );
        }
    }
}
