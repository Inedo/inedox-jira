using System;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Jira
{
    [Serializable]
    [CustomEditor(typeof(JiraApplicationFilterEditor))]
    public sealed class JiraApplicationFilter : IssueTrackerApplicationConfiguration
    {
        [Persistent]
        public string ProjectId { get; set; }
    }
}
