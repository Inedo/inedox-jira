using System;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.Jira
{
    [Serializable]
    [CustomEditor(typeof(JiraApplicationFilterEditor))]
    public sealed class JiraApplicationFilter : IssueTrackerApplicationConfigurationBase
    {
        [Persistent]
        public string ProjectId { get; set; }

        public override RichDescription GetDescription()
        {
            return new RichDescription("Project ID: ", this.ProjectId);
        }
    }
}
