using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Jira
{
    internal sealed class JiraProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtUserName;
        private ValidatingTextBox txtBaseUrl;
        private PasswordTextBox txtPassword;
        private ValidatingTextBox txtClosedState;
        private DropDownList ddlApiType;

        public override void BindToForm(ProviderBase extension)
        {
            var provider = (JiraProvider)extension;
            this.txtUserName.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
            this.txtBaseUrl.Text = provider.BaseUrl;
            this.txtClosedState.Text = provider.ClosedState;
            this.ddlApiType.SelectedValue = provider.ApiType.ToString();
        }

        public override ProviderBase CreateFromForm()
        {
            return new JiraProvider
            {
                UserName = txtUserName.Text,
                Password = txtPassword.Text,
                BaseUrl = txtBaseUrl.Text,
                ClosedState = InedoLib.Util.CoalesceStr(this.txtClosedState.Text, "Closed"),
                ApiType = ddlApiType.SelectedValue == JiraApiType.RESTv2.ToString() 
                                ? JiraApiType.RESTv2 
                                : JiraApiType.SOAP
            };
        }

        protected override void CreateChildControls()
        {
            this.txtUserName = new ValidatingTextBox();
            this.txtBaseUrl = new ValidatingTextBox();
            this.txtPassword = new PasswordTextBox();
            this.txtClosedState = new ValidatingTextBox() { DefaultText = "Closed" };
            this.ddlApiType = new DropDownList()
            {
                Items =
                {
                    new ListItem("REST API (for JIRA v6 or later)", JiraApiType.RESTv2.ToString()),
                    new ListItem("SOAP API (for JIRA v5 or earlier)", JiraApiType.SOAP.ToString())
                }
            };

            this.Controls.Add(
                new SlimFormField("JIRA server URL:", this.txtBaseUrl)
                {
                    HelpText = "The base URL of the JIRA server, e.g. http://jira:8080/ for local installs, "
                            + "and https://{your-account}.atlassian.net for Atlassian Cloud instances."
                },
                new SlimFormField("User name:", this.txtUserName)
                {
                    HelpText = HelpText.FromHtml("This user will be used to authenticate to the JIRA web service. This user must be in both the <b>jira-developers</b> and <b>jira-users</b> groups.")
                },
                new SlimFormField("Password:", this.txtPassword),
                new SlimFormField("Closed state:", this.txtClosedState)
                {
                    HelpText = "This is the status the \"Close Open Issues\" action will use as the " 
                                + "target status for the action."
                },
                new SlimFormField("API version:", this.ddlApiType)
            );
        }
    }
}
