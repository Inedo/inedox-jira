using System;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Jira
{
    [Serializable]
    [CustomEditor(typeof(JiraApplicationFilterEditor))]
    public sealed class JiraApplicationFilter : IssueTrackerApplicationConfigurationBase
    {
        [Persistent]
        public string ProjectId { get; set; }

        public override ExtensionComponentDescription GetDescription()
        {
            return new ExtensionComponentDescription("Project ID: ", this.ProjectId);
        }
    }
}
