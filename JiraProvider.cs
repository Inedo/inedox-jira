using System;
using System.Collections.Generic;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Jira.JiraApi;

namespace Inedo.BuildMasterExtensions.Jira
{
    [ProviderProperties(
       "JIRA",
       "Supports JIRA 4.0 and later.")]
    [CustomEditor(typeof(JiraProviderEditor))]
    public sealed class JiraProvider : IssueTrackingProviderBase, ICategoryFilterable, IUpdatingProvider, IReleaseNumberCloser, IReleaseNumberCreator
    {
        private const string JiraUrlFormatString = "/browse/{0}";

        private JiraSoapServiceService _Service;
        private string _Token;

        /// <summary>
        /// Initializes a new instance of the <see cref="JiraProvider"/> class.
        /// </summary>
        public JiraProvider()
        {
        }

        [Persistent]
        public string UserName { get; set; }
        [Persistent]
        public string Password { get; set; }
        [Persistent]
        public string BaseUrl { get; set; }
        [Persistent]
        public string RelativeServiceUrl { get; set; }
        [Persistent]
        public bool ConsiderResolvedStatusClosed { get; set; }

        public string[] CategoryIdFilter { get; set; }
        public string[] CategoryTypeNames
        {
            get { return new[] { "Project" }; }
        }

        /// <summary>
        /// Gets an existing service if there is one, otherwise creates a new service.
        /// </summary>
        private JiraSoapServiceService Service
        {
            get { return this._Service ?? (this._Service = new JiraSoapServiceService { Url = CombinePaths(this.BaseUrl, this.RelativeServiceUrl)}); }
        }

        /// <summary>
        /// Gets the authentication token used by the Jira web services, or creates one if it doesn't exist.
        /// </summary>
        private string Token
        {
            get { return this._Token ?? (this._Token = this.Service.login(this.UserName, this.Password)); }
        }

        /// <summary>
        /// Gets a URL to the specified issue.
        /// </summary>
        /// <param name="issue">The issue whose URL is returned.</param>
        /// <returns>
        /// The URL of the specified issue if applicable; otherwise null.
        /// </returns>
        public override string GetIssueUrl(Issue issue)
        {
            if (issue == null)
                throw new ArgumentNullException("issue");

            return CombinePaths(this.BaseUrl, string.Format(JiraUrlFormatString, issue.IssueId));
        }
        public override Issue[] GetIssues(string releaseNumber)
        {
            string projectFilter = (this.CategoryIdFilter != null && this.CategoryIdFilter.Length > 0) 
                ? string.Format(" and project = \"{0}\"", this.CategoryIdFilter[0])
                : "";

            var issues = this.Service.getIssuesFromJqlSearch(
                this.Token, 
                string.Format(
                    "fixVersion = \"{0}\" {1}", 
                    releaseNumber, 
                    projectFilter), 
                int.MaxValue);

            var issueList = new List<JiraIssue>();
            foreach (var remoteIssue in issues)
            {
                JiraIssue issue = new JiraIssue(this.Service, this.Token, remoteIssue);
                issueList.Add(issue);
            }

            return issueList.ToArray();
        }
        public override bool IsIssueClosed(Issue issue)
        {
            // depending on the configuration of the provider, a Resolved status may be considered closed for promotion purposes
            return issue.IssueStatus == JiraIssue.DefaultStatusNames.Closed 
                || (ConsiderResolvedStatusClosed && issue.IssueStatus == JiraIssue.DefaultStatusNames.Resolved);
        }
        public override bool IsAvailable()
        {
            return true;
        }
        public override void ValidateConnection()
        {
            try
            {
                this.Service.login(this.UserName, this.Password);
            }
            catch (Exception ex)
            {
                throw new NotAvailableException(ex.Message, ex);
            }
        }
        public CategoryBase[] GetCategories()
        {
            var remoteProjects = this.Service.getProjectsNoSchemes(this.Token);

            var categories = new List<JiraCategory>();
            foreach (var project in remoteProjects)
                categories.Add(JiraCategory.CreateProject(project));

            return categories.ToArray();
        }
        public bool CanAppendIssueDescriptions
        {
            get { return true; }
        }
        public bool CanChangeIssueStatuses
        {
            get { return true; }
        }
        public bool CanCloseIssues
        {
            get { return true; }
        }
        public void AppendIssueDescription(string issueId, string textToAppend)
        {
            var comment = new RemoteComment { body = textToAppend };
            this.Service.addComment(this.Token, issueId, comment);
        }
        public void ChangeIssueStatus(string issueId, string newStatus)
        {
            // verify status name is text
            newStatus = (newStatus ?? "").Trim();
            if (string.IsNullOrEmpty(newStatus) || newStatus.Length < 2)
                throw new ArgumentException("The status being applied must contain text and be at least 2 characters long", "newStatus");

            // return if the issue is already set to the new status
            var issue = this.Service.getIssue(this.Token, issueId);
            var jiraIssue = new JiraIssue(this.Service, this.Token, issue);
            if (jiraIssue.IssueStatus == newStatus)
                return;

            // get available actions for the issue (e.g. "Resolve issue" or "Close issue")
            var availableActions = this.Service.getAvailableActions(this.Token, issueId);

            // build a list of permitted action names and grab the id of the action that contains the newStatus (i.e. "Resolve issue" contains all but the last char in "Resolved")
            var permittedActions = new List<string>();
            string actionId = null;
            string newStatusPart = newStatus.Substring(0, newStatus.Length - 1);
            foreach (var action in availableActions)
            {
                permittedActions.Add(action.name);
                if (action.name.Contains(newStatusPart))
                    actionId = action.id;
            }

            if (actionId == null)
                throw new ArgumentException(string.Format("Changing the status to {0} is not permitted in the current workflow. The only permitted operations are: {1}", newStatus, string.Join(", ", permittedActions.ToArray())));

            this.Service.progressWorkflowAction(
                this.Token, 
                issueId, 
                actionId, 
                new RemoteFieldValue[0]
            );
        }
        public void CloseIssue(string issueId)
        {
            this.ChangeIssueStatus(issueId, JiraIssue.DefaultStatusNames.Closed);
        }
        public void CloseReleaseNumber(string releaseNumber)
        {
            if (string.IsNullOrEmpty(releaseNumber))
                throw new ArgumentNullException("releaseNumber");

            if (this.CategoryIdFilter == null || this.CategoryIdFilter.Length == 0)
                throw new InvalidOperationException("Application must be specified in category ID filter to close a release.");

            // Ensure version exists.
            var versions = this.Service.getVersions(this.Token, this.CategoryIdFilter[0]);
            var version = Array.Find(versions, v => releaseNumber.Equals((v.name ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
            if(version == null)
                throw new InvalidOperationException("Version " + releaseNumber + " does not exist.");

            // If version is already released, do nothing.
            if (version.released)
                return;

            // Otherwise release it.
            version.released = true;
            version.releaseDate = DateTime.Now;
            this.Service.releaseVersion(this.Token, this.CategoryIdFilter[0], version);
        }
        public void CreateReleaseNumber(string releaseNumber)
        {
            if (string.IsNullOrEmpty(releaseNumber))
                throw new ArgumentNullException("releaseNumber");

            if (this.CategoryIdFilter == null || this.CategoryIdFilter.Length == 0)
                throw new InvalidOperationException("Application must be specified in category ID filter to create a release.");

            // If version is already created, do nothing.
            var versions = this.Service.getVersions(this.Token, this.CategoryIdFilter[0]);
            if (Array.Find(versions, v => releaseNumber.Equals((v.name ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) == null)
                return;

            // Otherwise add it.
            this.Service.addVersion(this.Token, this.CategoryIdFilter[0], new RemoteVersion { name = releaseNumber });
        }
        public override string ToString()
        {
            return "Connects to the Jira issue tracking system version 4.0 or later.";
        }

        private static string CombinePaths(string baseUrl, string relativeUrl)
        {
            if (baseUrl.EndsWith("/"))
            {
                return relativeUrl.StartsWith("/")
                    ? baseUrl + relativeUrl.Substring(1, relativeUrl.Length - 1)
                    : baseUrl + relativeUrl;
            }
            else
            {
                return relativeUrl.StartsWith("/")
                    ? baseUrl + relativeUrl
                    : baseUrl + "/" + relativeUrl;
            }
        }
    }
}