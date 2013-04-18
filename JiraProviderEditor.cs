using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Jira
{
    internal sealed class JiraProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtUserName, txtBaseUrl, txtRelativeServiceUrl;
        private PasswordTextBox txtPassword;
        private DropDownList ddlConsiderResolvedStatusAsClosed;

        /// <summary>
        /// Initializes a new instance of the <see cref="JiraProviderEditor"/> class.
        /// </summary>
        public JiraProviderEditor()
        {
        }

        public override void BindToForm(ProviderBase extension)
        {
            this.EnsureChildControls();

            var provider = (JiraProvider)extension;
            this.txtUserName.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
            if (!string.IsNullOrEmpty(provider.RelativeServiceUrl))
                this.txtRelativeServiceUrl.Text = provider.RelativeServiceUrl;
            this.txtBaseUrl.Text = provider.BaseUrl;
            this.ddlConsiderResolvedStatusAsClosed.SelectedValue = (provider.ConsiderResolvedStatusClosed)
                ? "Yes"
                : "No";
        }
        public override ProviderBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new JiraProvider
            {
                UserName = txtUserName.Text,
                Password = txtPassword.Text,
                RelativeServiceUrl = txtRelativeServiceUrl.Text,
                BaseUrl = txtBaseUrl.Text,
                ConsiderResolvedStatusClosed = ddlConsiderResolvedStatusAsClosed.SelectedValue == "Yes"
            };
        }

        protected override void CreateChildControls()
        {
            this.txtUserName = new ValidatingTextBox();
            
            this.txtBaseUrl = new ValidatingTextBox
            {
                Width = Unit.Pixel(300)
            };
            
            this.txtRelativeServiceUrl = new ValidatingTextBox
            { 
                Text = "rpc/soap/jirasoapservice-v2",
                Width = Unit.Pixel(300)
            };
            
            this.txtPassword = new PasswordTextBox();
            
            this.ddlConsiderResolvedStatusAsClosed = new DropDownList();
            ddlConsiderResolvedStatusAsClosed.Items.Add("Yes");
            ddlConsiderResolvedStatusAsClosed.Items.Add("No");

            this.Controls.Add(
                new FormFieldGroup("Jira Server URL",
                    "The URL of the Jira server, for example: http://jiraserver:8080",
                    false,
                    new StandardFormField(
                        "Server URL:",
                        this.txtBaseUrl)
                    ),
                new FormFieldGroup("Jira Web Service Path",
                    "The relative path to the Jira service URL. Generally, this will be \"rpc/soap/jirasoapservice-v2\"", /*http://jiraserver:8080/rpc/soap/jirasoapservice-v2*/
                    false,
                    new StandardFormField(
                        "Service Path:",
                        this.txtRelativeServiceUrl)
                    ),
                new FormFieldGroup("Authentication",
                    "Provide a username and password to connect to the Jira service. The given user must be in both <b>jira-developers</b> and <b>jira-users</b> groups.",
                    false,
                    new StandardFormField(
                        "User Name:",
                        this.txtUserName),
                    new StandardFormField(
                        "Password:",
                        this.txtPassword),
                    new StandardFormField(
                        "Consider Resolved Status as Closed?",
                        this.ddlConsiderResolvedStatusAsClosed)
                    )
            );

            base.CreateChildControls();
        }
    }
}
