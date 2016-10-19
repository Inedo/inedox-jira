using System;
using System.Collections.Generic;
using System.Linq;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.Diagnostics;

namespace Inedo.BuildMasterExtensions.Jira.Clients
{
    internal abstract class CommonJiraClient : IDisposable
    {
        protected string userName;
        protected string password;
        protected string serverUrl;
        protected ILogger log;

        protected CommonJiraClient(string serverUrl, string userName, string password, ILogger log)
        {
            if (string.IsNullOrEmpty(serverUrl))
                throw new ArgumentNullException(nameof(serverUrl));
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));
            
            this.serverUrl = serverUrl;
            this.userName = userName;
            this.password = password;
            this.log = log ?? Logger.Null;
        }

        public static CommonJiraClient Create(JiraApiType apiType, string serverUrl, string userName, string password, ILogger log = null)
        {
            CommonJiraClient client;
            if (apiType == JiraApiType.SOAP)
                client = new JiraSoapClient(serverUrl, userName, password, log);
            else if (apiType == JiraApiType.RESTv2)
                client = new JiraRestClient(serverUrl, userName, password, log);
            else
                throw new InvalidOperationException("Invalid JIRA client version specified: " + apiType);

            log?.LogDebug($"Loading JIRA {apiType} client...");

            return client;
        }

        public abstract IEnumerable<JiraProject> GetProjects();
        public abstract IEnumerable<Transition> GetTransitions(JiraContext context);

        public abstract void ValidateConnection();

        public abstract IEnumerable<IIssueTrackerIssue> EnumerateIssues(JiraContext context);
        public abstract void AddComment(JiraContext context, string issueId, string commentText);
        public abstract void TransitionIssue(JiraContext context, string issueId, string issueStatus);
        public abstract void TransitionIssuesInStatus(JiraContext context, string fromStatus, string toStatus);
        public abstract void CloseIssue(JiraContext context, string issueId);
        public abstract void CreateRelease(JiraContext context);
        public abstract void DeployRelease(JiraContext context);
        public abstract IIssueTrackerIssue CreateIssue(JiraContext context, string title, string description, string type);

        public abstract IEnumerable<JiraIssueType> GetIssueTypes(string projectId);

        public void CloseAllIssues(JiraContext context)
        {
            foreach (var issue in this.EnumerateIssues(context))
            {
                if (issue.IsClosed)
                {
                    this.log.LogDebug($"{issue.Id} is already closed, skipping...");
                    continue;
                }
                this.CloseIssue(context, issue.Id);
            }
        }

        public JiraProject FindProject(string projectName)
        {
            return this.GetProjects().FirstOrDefault(p => string.Equals(projectName, p.Name, StringComparison.OrdinalIgnoreCase));
        }

        public JiraIssueType FindIssueType(string projectId, string issueTypeName)
        {
            return this.GetIssueTypes(projectId).FirstOrDefault(p => string.Equals(issueTypeName, p.Name, StringComparison.OrdinalIgnoreCase));
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose() => this.Dispose(true);
    }
}
