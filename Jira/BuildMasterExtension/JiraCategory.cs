using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.Extensions.Jira;

namespace Inedo.BuildMasterExtensions.Jira
{
    [Serializable]
    internal sealed class JiraCategory : IssueTrackerCategory
    {
        public enum CategoryTypes { Project }

        public CategoryTypes CategoryType { get; private set; }

        private JiraCategory(string categoryId, string categoryName, IssueTrackerCategory[] subCategories, CategoryTypes categoryType)
            : base(categoryId, categoryName, subCategories) 
        { 
            CategoryType = categoryType;
        }

        internal static JiraCategory CreateProject(JiraProject project)
        {
            return new JiraCategory(
                project.Key,
                project.Name,
                new JiraCategory[] {},
                CategoryTypes.Project);
        }
    }
}
