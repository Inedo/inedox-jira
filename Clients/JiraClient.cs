using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.Diagnostics;

namespace Inedo.BuildMasterExtensions.Jira.Clients
{
    internal abstract class JiraClient : IDisposable
    {
        private static ConcurrentDictionary<string, JiraApiType> recommendedApiType = new ConcurrentDictionary<string, JiraApiType>(2, 16, StringComparer.OrdinalIgnoreCase);

        protected string userName;
        protected string password;
        protected string serverUrl;
        protected ILogger log;

        protected JiraClient(string serverUrl, string userName, string password, ILogger log)
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

        public static JiraClient Create(string serverUrl, string userName, string password, ILogger log = null, JiraApiType apiType = JiraApiType.AutoDetect)
        {
            if (apiType == JiraApiType.SOAP)
            {
                log?.LogDebug($"Loading specified JIRA SOAP client...");
                return new JiraSoapClient(serverUrl, userName, password, log);
            }
            else if (apiType == JiraApiType.RESTv2)
            {
                log?.LogDebug($"Loading specified JIRA REST client...");
                return new JiraRestClient(serverUrl, userName, password, log);
            }
            else
            {
                log?.LogDebug($"Auto-detecting JIRA client type...");

                JiraClient client;

                var result = recommendedApiType.GetOrAdd(serverUrl, DetermineApiType);
                if (result == JiraApiType.AutoDetect)
                    throw new InvalidOperationException("JIRA API type could not be automatically determined, try specifying the exact version by overriding the $JiraApiVersion variable function.");
                else if (result == JiraApiType.RESTv2)
                    client = new JiraRestClient(serverUrl, userName, password, log);
                else
                    client = new JiraSoapClient(serverUrl, userName, password, log);

                log?.LogDebug($"Using JIRA {result} client.");

                return client;
            }
        }
        
        public abstract IEnumerable<JiraProject> GetProjects();
        public abstract IEnumerable<ProjectVersion> GetProjectVersions(string projectKey);
        public abstract IEnumerable<Transition> GetTransitions(JiraContext context);

        public abstract void ValidateConnection();

        public abstract IEnumerable<IIssueTrackerIssue> EnumerateIssues(JiraContext context);
        public abstract Task<IEnumerable<IIssueTrackerIssue>> EnumerateIssuesAsync(JiraContext context);
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

        private static JiraApiType DetermineApiType(string serverUrl)
        {
            using (var client = new HttpClient())
            {
                string testRestUrl = serverUrl.TrimEnd('/') + "/rest/api/2/";
                var restResult = Task.Run(() => client.GetAsync(testRestUrl)).Result();
                if ((int)restResult.StatusCode != 404)
                    return JiraApiType.RESTv2;

                string testSoapUrl = serverUrl.TrimEnd('/') + "/rpc/soap/jirasoapservice-v2";
                var soapResult = Task.Run(() => client.GetAsync(testSoapUrl)).Result();
                if ((int)soapResult.StatusCode != 404)
                    return JiraApiType.SOAP;

                return JiraApiType.AutoDetect;
            }
        }
    }
}
