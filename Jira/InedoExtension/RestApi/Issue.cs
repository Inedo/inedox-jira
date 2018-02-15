using System;
using System.Collections.Generic;
using Inedo.Extensibility.IssueSources;

namespace Inedo.Extensions.Jira.RestApi
{
    [Serializable]
    internal sealed class Issue : IIssueTrackerIssue
    {
        public Issue(Dictionary<string, object> issue, string hostUrl)
        {
            if (issue == null)
                throw new ArgumentNullException(nameof(issue));
            if (hostUrl == null)
                throw new ArgumentNullException(nameof(hostUrl));

            var fields = (Dictionary<string, object>)issue["fields"];
            var status = (Dictionary<string, object>)fields["status"];
            var reporter = (Dictionary<string, object>)fields["reporter"];
            var type = (Dictionary<string, object>)fields["issuetype"];

            this.Id = issue["key"].ToString();
            this.Description = fields["description"]?.ToString();
            this.Status = status["name"].ToString();
            this.IsClosed = fields["resolution"] != null;
            this.SubmittedDate = DateTime.Parse(fields["created"].ToString());
            this.Submitter = reporter["name"].ToString();
            this.Title = fields["summary"].ToString();
            this.Type = type["name"].ToString();
            this.Url = hostUrl + "/browse/" + this.Id;
        }

        public string Id { get; }
        public string Description { get; }
        public bool IsClosed { get; }
        public string Status { get; }
        public DateTime SubmittedDate { get; }
        public string Submitter { get; }
        public string Title { get; }
        public string Type { get; }
        public string Url { get; }
    }
}
