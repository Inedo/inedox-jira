using System;
using System.Collections.Generic;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.Diagnostics;

namespace Inedo.BuildMasterExtensions.Jira.Clients
{
    internal abstract class CommonJiraClient : IReleaseManager, IIssueCloser, IIssueCommenter, IIssueStatusUpdater, ILogger, IDisposable
    {
        protected CommonJiraClient(JiraProvider provider)
        {
            this.Provider = provider;
        }

        public JiraProvider Provider { get; }

        public JiraApplicationFilter GetFilter(IssueTrackerConnectionContext context)
        {
            return (JiraApplicationFilter)context.ApplicationConfiguration ?? this.Provider.legacyFilter;
        }

        public event EventHandler<LogMessageEventArgs> MessageLogged;

        public abstract IEnumerable<JiraProject> GetProjects();

        public abstract void ValidateConnection();

        public abstract IEnumerable<IIssueTrackerIssue> EnumerateIssues(IssueTrackerConnectionContext context);
        public abstract void AddComment(IssueTrackerConnectionContext context, string issueId, string commentText);
        public abstract void ChangeIssueStatus(IssueTrackerConnectionContext context, string issueId, string issueStatus);
        public abstract void ChangeStatusForAllIssues(IssueTrackerConnectionContext context, string fromStatus, string toStatus);
        public abstract void CloseIssue(IssueTrackerConnectionContext context, string issueId);
        public abstract void CreateRelease(IssueTrackerConnectionContext context);
        public abstract void DeployRelease(IssueTrackerConnectionContext context);

        public void CloseAllIssues(IssueTrackerConnectionContext context)
        {
            foreach (var issue in this.EnumerateIssues(context))
            {
                if (issue.IsClosed)
                {
                    this.LogDebug($"{issue.Id} is already closed, skipping...");
                    continue;
                }
                this.CloseIssue(context, issue.Id);
            }
        }

        public void Log(MessageLevel logLevel, string message)
        {
            this.MessageLogged?.Invoke(this, new LogMessageEventArgs(logLevel, message));
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose() => this.Dispose(true);
    }
}
