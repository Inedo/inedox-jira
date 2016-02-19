using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Jira
{
    internal sealed class JiraApplicationFilterEditor : IssueTrackerApplicationConfigurationEditorBase
    {
        private SelectList ctlProject;

        public override void BindToForm(IssueTrackerApplicationConfigurationBase extension)
        {
            var filter = (JiraApplicationFilter)extension;

            this.ctlProject.SelectedValue = filter.ProjectId;
        }
        public override IssueTrackerApplicationConfigurationBase CreateFromForm()
        {
            return new JiraApplicationFilter
            {
                ProjectId = this.ctlProject.SelectedValue
            };
        }

        protected override void CreateChildControls()
        {
            var application = DB.Applications_GetApplication(this.EditorContext.ApplicationId)
                .Applications_Extended
                .First();

            var projects = GetProjects(application);

            this.ctlProject = new SelectList(
                (from p in projects
                 orderby p.Name
                 select new SelectListItem(p.Name, p.Id)).ToList()
            );

            this.Controls.Add(
                new SlimFormField("JIRA project:", ctlProject)
            );
        }

        private static IEnumerable<JiraProject> GetProjects(Tables.Applications_Extended application)
        {
            if (application.IssueTracking_Provider_Id == null)
                return Enumerable.Empty<JiraProject>();

            using (var provider = (JiraProvider)Util.Providers.CreateProviderFromId<IssueTrackerConnectionBase>((int)application.IssueTracking_Provider_Id))
            {
                return provider.GetProjects();
            }
        }
    }
}
