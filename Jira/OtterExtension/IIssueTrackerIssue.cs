using System;

namespace Inedo.OtterExtensions.Jira
{
    public interface IIssueTrackerIssue
    {
        string Description { get; }
        string Id { get; }
        bool IsClosed { get; }
        string Status { get; }
        DateTime SubmittedDate { get; }
        string Submitter { get; }
        string Title { get; }
        string Type { get; }
        string Url { get; }
    }
}
