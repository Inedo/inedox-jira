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
    [DisplayName("Transition Jira Issues")]
    [Description("Transitions issues in JIRA.")]
    [Tag(Tags.IssueTracking)]
    [ScriptAlias("Transition-Issues")]
    [Example(@"
# closes issues for the HDARS project
Transition-Issues(
    Credentials: Jira7Local,
    Project: HDARS,
    From: QA-InProgress,
    To: Closed
);
")]
    public sealed class TransitionIssuesOperation : JiraOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [Required]
        [ScriptAlias("Project")]
        [DisplayName("Project name")]
        [SuggestibleValue(typeof(JiraProjectNameSuggestionProvider))]
        public string ProjectName { get; set; }

        [ScriptAlias("From")]
        [DisplayName("From")]
        [PlaceholderText("Any status")]
        public string FromStatus { get; set; }
        [Required]
        [ScriptAlias("To")]
        [DisplayName("To")]
        [SuggestibleValue(typeof(JiraTransitionNameSuggestionProvider))]
        public string ToStatus { get; set; }
        [ScriptAlias("Id")]
        [DisplayName("Specific issue ID")]
        [PlaceholderText("Any")]
        public string IssueId { get; set; }
        [ScriptAlias("FixFor")]
        [DisplayName("With fix for version")]
        [PlaceholderText("$ReleaseNumber")]
        public string ReleaseNumber { get; set; }

        public override Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation($"Transitioning JIRA issue status {(this.IssueId != null ? $"of issue ID '{this.IssueId}' " : " ")}from '{(string.IsNullOrEmpty(this.FromStatus) ? "<any status>" : this.FromStatus)}' to '{this.ToStatus}'...");

            var client = JiraClient.Create(this.Api, this.ServerUrl, this.UserName, this.Password.ToUnsecureString(), this);

            var project = this.ResolveProject(client, this.ProjectName);
            if (project == null)
                return Complete;

            var jiraContext = new JiraContext(project.Key, this.ReleaseNumber ?? context.ReleaseNumber, null);

            if (this.IssueId != null)
            {
                client.TransitionIssue(jiraContext, this.IssueId, this.ToStatus);
            }
            else
            {
                client.TransitionIssuesInStatus(jiraContext, this.FromStatus, this.ToStatus);
            }

            this.LogInformation("Issue(s) transitioned.");

            return Complete;
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Transition Jira Issues for project ", config[nameof(this.ProjectName)])
            );
        }
    }
}
