using System.Collections.Generic;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.Jira
{
    partial class JiraProvider : ICategoryFilterable
    {
        private JiraApplicationFilter legacyFilter;

        string[] ICategoryFilterable.CategoryIdFilter
        {
            get
            {
                if (this.legacyFilter != null)
                    return new[] { this.legacyFilter.ProjectId };
                else
                    return null;
            }
            set
            {
                if (value != null && value.Length > 0)
                    this.legacyFilter = new JiraApplicationFilter { ProjectId = value[0] };
                else
                    this.legacyFilter = null;
            }
        }
        string[] ICategoryFilterable.CategoryTypeNames
        {
            get { return new[] { "Project" }; }
        }

        IssueTrackerCategory[] ICategoryFilterable.GetCategories()
        {
            var remoteProjects = this.Service.getProjectsNoSchemes(this.Token);

            var categories = new List<JiraCategory>();
            foreach (var project in remoteProjects)
                categories.Add(JiraCategory.CreateProject(project));

            return categories.ToArray();
        }
    }
}
