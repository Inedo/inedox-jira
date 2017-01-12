﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.IssueSources;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Variables;
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
        public string CredentialName { get; set; }
        [Persistent]
        [DisplayName("Project name")]
        [SuggestibleValue(typeof(JiraProjectNameSuggestionProvider))]
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

            if (credentials == null)
                throw new InvalidOperationException("Credentials must be supplied to enumerate JIRA issues.");
            if (string.IsNullOrEmpty(this.ProjectName) && string.IsNullOrEmpty(this.FixForVersion) && string.IsNullOrEmpty(this.CustomJql))
                throw new InvalidOperationException("Cannot enumerate JIRA issues unless either a project name, fix version, or custom JQL is specified.");
            if (credentials.Password == null)
                throw new InvalidOperationException("A credential password is required to enumerate JIRA issues.");

            var apiVersion = GetApiType(context.ExecutionId);

            var client = JiraClient.Create(credentials.ServerUrl, credentials.UserName, credentials.Password.ToUnsecureString(), context.Log, apiVersion);

            var project = client.FindProject(this.ProjectName);

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

        private static JiraApiType GetApiType(int? executionId)
        {
            var exec = DB.Executions_GetExecution(executionId);
            if (exec == null)
                return JiraApiType.AutoDetect;

            var variable = BuildMasterVariable.GetCascadedVariable("JiraApiVersion", new ExecutionContext(exec), includeSystemVariables: true);

            JiraApiType result;
            if (Enum.TryParse(variable?.UnprocessedValue ?? "", out result))
                return result;
            else
                return JiraApiType.AutoDetect;
        }

        private sealed class ExecutionContext : IBuildMasterContext
        {
            private Lazy<int?> getAppGroupId;
            private Tables.Executions_Extended e;
            public ExecutionContext(Tables.Executions_Extended e)
            {
                this.e = e;
                this.getAppGroupId = new Lazy<int?>(
                    () => DB.Applications_GetApplication(e.Application_Id).Applications_Extended.FirstOrDefault()?.ApplicationGroup_Id
                );
            }

            public int? ExecutionId => e.Execution_Id;
            public int? ApplicationId => e.Application_Id;
            public int? ApplicationGroupId => this.getAppGroupId.Value;

            public int? BuildId => null;
            public string BuildNumber => null;
            public int? DeployableId => null;
            public int? EnvironmentId => null;
            public int? PipelineId => null;
            public string PipelineStageName => null;
            public int? PromotionId => null;
            public int? ReleaseId => null;
            public string ReleaseNumber => null;
            public int? ServerId => null;
            public int? ServerRoleId => null;
        }
    }
}