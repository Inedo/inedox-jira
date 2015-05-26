using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Jira
{
    internal sealed class JiraProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtUserName, txtBaseUrl, txtRelativeServiceUrl;
        private PasswordTextBox txtPassword;
        private CheckBox chkConsiderResolvedStatusAsClosed;

        public override void BindToForm(ProviderBase extension)
        {
            var provider = (JiraProvider)extension;
            this.txtUserName.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
            this.txtRelativeServiceUrl.Text = provider.RelativeServiceUrl;
            this.txtBaseUrl.Text = provider.BaseUrl;
        }

        public override ProviderBase CreateFromForm()
        {
            return new JiraProvider
            {
                UserName = txtUserName.Text,
                Password = txtPassword.Text,
                RelativeServiceUrl = Util.CoalesceStr(txtRelativeServiceUrl.Text, "rpc/soap/jirasoapservice-v2"),
                BaseUrl = txtBaseUrl.Text,
            };
        }

        protected override void CreateChildControls()
        {
            this.txtUserName = new ValidatingTextBox();
            this.txtBaseUrl = new ValidatingTextBox();
            this.txtRelativeServiceUrl = new ValidatingTextBox { DefaultText = "rpc/soap/jirasoapservice-v2" };
            this.txtPassword = new PasswordTextBox();
            this.chkConsiderResolvedStatusAsClosed = new CheckBox { Text = "Treat Resolved status as Closed", Checked = true };

            this.Controls.Add(
                new SlimFormField("JIRA server URL:", this.txtBaseUrl)
                {
                    HelpText = "The base URL of the JIRA server, e.g. http://jira:8080/"
                },
                new SlimFormField("Web service relative path:", this.txtRelativeServiceUrl),
                new SlimFormField("User name:", this.txtUserName)
                {
                    HelpText = HelpText.FromHtml("This user will be used to authenticate to the JIRA web service. This user must be in both the <b>jira-developers</b> and <b>jira-users</b> groups.")
                },
                new SlimFormField("Password:", this.txtPassword),
                new SlimFormField("Options:", this.chkConsiderResolvedStatusAsClosed)
            );
        }
    }
}
