using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Jira.Client;
using Inedo.Extensions.Jira.Credentials;
using Inedo.Serialization;
using Inedo.Web;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.Jira.Operations
{
    public abstract class JiraOperation : ExecuteOperation
    {
        private protected JiraOperation()
        {
        }
        [DisplayName("From project")]
        [ScriptAlias("From")]
        [ScriptAlias("Credentials")]
        [SuggestableValue(typeof(SecureResourceSuggestionProvider<JiraProject>))]
        public string ResourceName { get; set; }

        [Category("Connection")]
        [ScriptAlias("Project")]
        [DisplayName("Project name")]
        [PlaceholderText("Use project specified in connection")]
        public string ProjectName { get; set; }

        [Category("Connection")]
        [ScriptAlias("Server")]
        [PlaceholderText("Use server URL from resource")]
        public string ServerUrl { get; set; }

        [Category("Connection")]
        [ScriptAlias("UserName")]
        [PlaceholderText("Use user name from resource's credentials")]
        public string UserName { get; set; }

        [Category("Connection")]
        [DisplayName("API Token")]
        [ScriptAlias("ApiToken")]
        [PlaceholderText("Use API Token from resource's credentials")]
        public SecureString ApiToken { get; set; }

        public sealed override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            try
            {
                var client = this.GetClient(context);
                await this.ExecuteAsync(context, client.Client, client.Project);
            }
            catch (Exception ex)
            {
                this.LogError(ex.Message);
            }
        }

        private protected abstract Task ExecuteAsync(IOperationExecutionContext context, JiraClient client, JiraProject project);

        private (JiraClient Client, JiraProject Project) GetClient(IOperationExecutionContext context)
        {
            JiraServiceCredentials credentials; JiraProject project;
            if (string.IsNullOrEmpty(this.ResourceName))
            {
                if (string.IsNullOrEmpty(this.ServerUrl))
                    throw new ExecutionFailureException($"Server Url must be specified if a Resource is not specified.");
                if (this.UserName == null)
                    throw new ExecutionFailureException($"UserName must be specified when specifying a server url..");
                if (this.ApiToken == null)
                    throw new ExecutionFailureException($"PermanentToken must be specified when specifying a server url..");

                project = new();
                credentials = new() { ServiceUrl = this.ServerUrl, UserName = this.UserName, Password = this.ApiToken };
            }
            else
            {
                var maybeProject = SecureResource.TryCreate(SecureResourceType.IssueTrackerProject, this.ResourceName, context);
                if (maybeProject is JiraProject)
                {
                    project = (JiraProject)maybeProject;
                    credentials = (JiraServiceCredentials)project.GetCredentials(context) ?? new();
                }
                else
                {
                    throw new ExecutionFailureException($"Cannot find the Jira Project {this.ResourceName}.");
                }
            }

            if (this.UserName != null)
                credentials.UserName = this.UserName;
            if (this.ApiToken != null)
                credentials.Password = this.ApiToken;

            if (credentials.Password == null)
                throw new ExecutionFailureException($"An API Token was not specified on the operation or credential.");
            if (string.IsNullOrEmpty(credentials.ServiceUrl))
                throw new ExecutionFailureException($"A ServerUrl was not specified on the operation or credential.");


            project.ProjectName = AH.CoalesceString(this.ProjectName, project.ProjectName);
            if(project.ProjectName == null)
                throw new ExecutionFailureException($"A ProjectName was not specified on the operation or project.");
            return (new JiraClient(credentials.ServiceUrl, credentials.UserName, AH.Unprotect(credentials.Password)), project);
        }

        //protected (JiraSecureResource resource, SecureCredentials secureCredentials) GetResourceAndCredentials(IOperationExecutionContext context)
        //{
        //    JiraSecureResource resource = null;
        //    SecureCredentials credentials = null;

        //    if (!string.IsNullOrEmpty(this.ResourceName))
        //    {
        //        resource = SecureResource.TryCreate(this.ResourceName, context as IResourceResolutionContext) as JiraSecureResource;
        //        if (resource == null)
        //        {
        //            var legacy = ResourceCredentials.TryCreate<JiraLegacyCredentials>(this.ResourceName);
        //            resource = legacy?.ToSecureResource() as JiraSecureResource;
        //            credentials = legacy?.ToSecureCredentials();
        //        }
        //        else
        //        {
        //            credentials = resource.GetCredentials((context as ICredentialResolutionContext));
        //        }
        //    }
        //    if (!string.IsNullOrEmpty(this.UserName) && this.Password != null)
        //    {
        //        credentials = new UsernamePasswordCredentials
        //        {
        //            UserName = this.UserName,
        //            Password = this.Password
        //        };
        //    }

        //    return (new JiraSecureResource()
        //    {
        //        ServerUrl = AH.CoalesceString(this.ServerUrl, resource?.ServerUrl)
        //    }, credentials);
        //}

        //internal async Task<JiraProject> ResolveProjectAsync(JiraClient client, string name)
        //{
        //    this.LogDebug($"Resolving project name '{name}'...");
        //    var project = await client.FindProjectAsync(name);

        //    if (project == null)
        //    {
        //        this.LogError($"Project '{name}' was not found.");
        //        return null;
        //    }

        //    this.LogDebug($"Project resolved to key='{project.Key}', id='{project.Id}'.");

        //    return project;
        //}

        //internal async Task<JiraIssueType> ResolveIssueTypeAsync(JiraClient client, JiraProject project, string issueTypeName)
        //{
        //    this.LogDebug($"Resolving issue type name '{issueTypeName}' for project '{project.Name}'...");
        //    var issueType = await client.FindIssueTypeAsync(project.Id, issueTypeName);

        //    if (issueType == null)
        //    {
        //        this.LogError($"Issue type '{issueTypeName}' was not found in project '{project.Name}'.");
        //        return null;
        //    }

        //    this.LogDebug($"Issue type ID resolved to '{issueType.Id}'.");

        //    return issueType;
        //}
    }
}
