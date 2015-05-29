using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.ClientResources;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Jira
{
    internal sealed class JiraApplicationFilterEditor : IssueTrackerApplicationConfigurationEditorBase
    {
        private HiddenField ctlProject;

        public override void BindToForm(IssueTrackerApplicationConfigurationBase extension)
        {
            var filter = (JiraApplicationFilter)extension;

            this.ctlProject.Value = filter.ProjectId;
        }
        public override IssueTrackerApplicationConfigurationBase CreateFromForm()
        {
            return new JiraApplicationFilter
            {
                ProjectId = this.ctlProject.Value
            };
        }

        protected override void OnPreRender(EventArgs e)
        {
            this.IncludeClientResourceInPage(
                new JavascriptResource
                {
                    ResourcePath = "~/extension-resources/Jira/JiraApplicationFilterEditor.js?" + typeof(JiraApplicationFilterEditor).Assembly.GetName().Version,
                    CompatibleVersions = { InedoLibCR.Versions.jq171 },
                    Dependencies = { InedoLibCR.select2.select2_js }
                }
            );

            base.OnPreRender(e);
        }
        protected override void CreateChildControls()
        {
            var application = StoredProcs.Applications_GetApplication(this.EditorContext.ApplicationId)
                .Execute()
                .Applications_Extended
                .First();

            var projects = GetProjects(application);

            this.ctlProject = new HiddenField
            {
                ID = "ctlProject"
            };

            this.Controls.Add(
                new SlimFormField("JIRA project:", ctlProject),
                new RenderJQueryDocReadyDelegator(
                    w =>
                    {
                        w.Write("BmInitJiraApplicationFilterEditor(");
                        InedoLib.Util.JavaScript.WriteJson(
                            w,
                            new
                            {
                                ctlProject = ctlProject.ClientID,
                                data = from p in projects
                                       orderby p.Value
                                       select new
                                       {
                                           id = p.Key,
                                           text = p.Value
                                       }
                            }
                        );
                        w.Write(");");
                    }
                )
            );
        }

        private static Dictionary<string, string> GetProjects(Tables.Applications_Extended application)
        {
            if (application.IssueTracking_Provider_Id == null)
                return new Dictionary<string, string>(0);

            using (var provider = (JiraProvider)Util.Providers.CreateProviderFromId<IssueTrackerConnectionBase>((int)application.IssueTracking_Provider_Id))
            {
                return provider.GetProjects();
            }
        }
    }
}
