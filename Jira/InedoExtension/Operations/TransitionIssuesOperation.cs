using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.IssueTrackers;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Jira.Client;
using Inedo.Extensions.Jira.Credentials;
using static Inedo.Extensions.Jira.Credentials.JiraProject;

namespace Inedo.Extensions.Jira.Operations
{
    [Description("Transitions issues in JIRA.")]
    [ScriptAlias("Transition-Issues")]
    [Example(@"
# closes issues for the HDARS project for the current release
Transition-Issues(
    From: Jira7Local,
    Project: HDARS,
    From: QA-InProgress,
    To: Closed
);
")]
    public sealed class TransitionIssuesOperation : JiraOperation
    {

        [ScriptAlias("From")]
        [DisplayName("From")]
        [PlaceholderText("Any status")]
        public string FromStatus { get; set; }
        [Required]
        [ScriptAlias("To")]
        [DisplayName("To")]
        public string ToStatus { get; set; }
        [ScriptAlias("FixFor")]
        [DisplayName("With fix for version")]
        [PlaceholderText("$ReleaseNumber")]
        public string FixForVersion { get; set; }
        [ScriptAlias("Id")]
        [DisplayName("Specific issue ID")]
        [PlaceholderText("Any")]
        [Description("If an issue ID is supplied, other filters will be ignored.")]
        public string IssueId { get; set; }
        [ScriptAlias("Comment")]
        [DisplayName("Comment")]
        public string Comment { get; set; }

        private protected override async Task ExecuteAsync(IOperationExecutionContext context, JiraClient client, JiraProject project)
        {
            this.LogInformation($"Transitioning JIRA issue status {(this.IssueId != null ? $"of issue ID '{this.IssueId}' " : " ")}from '{(string.IsNullOrEmpty(this.FromStatus) ? "<any status>" : this.FromStatus)}' to '{this.ToStatus}'...");


            var queryFilter = $"project = \"{project.ProjectName}\" ";
            if (this.FixForVersion != null)
                queryFilter += $"AND fixVersion = {this.FixForVersion}";
            if (this.IssueId != null)
                queryFilter += $"AND id = {this.IssueId}";
            
            await project.TransitionIssuesAsync(this.FromStatus, this.ToStatus, this.Comment, new OperationEnumerationContext(context, queryFilter), context.CancellationToken);
            

            this.LogInformation("Issue(s) transitioned.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Transition Jira Issues for project ", config[nameof(this.ProjectName)])
            );
        }

        private class OperationEnumerationContext : IIssuesEnumerationContext
        {
            public OperationEnumerationContext(IOperationExecutionContext context, string query)
            {
                this.Filter = new JiraIssuesQueryFilter(query);
                this.ApplicationId = context.ApplicationId;
                this.EnvironmentId = context.EnvironmentId;
            }


            public IssuesQueryFilter Filter { get; }

            public int? EnvironmentId { get; }

            public int? ApplicationId { get; }
        }
    }
}
