using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.Jira.Clients;
using Inedo.BuildMasterExtensions.Jira.Credentials;

namespace Inedo.BuildMasterExtensions.Jira.SuggestionProviders
{
    public sealed class JiraIssueTypeNameSuggestionProvider : ISuggestionProvider
    {
        public Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var empty = Task.FromResult(Enumerable.Empty<string>());

            string credentialName = config["CredentialName"];
            var credential = ResourceCredentials.Create<JiraCredentials>(credentialName);
            if (credential == null)
                return empty;

            JiraApiType api;
            if (!Enum.TryParse(config["Api"], out api))
                return empty;

            var client = JiraClient.Create(api, credential.ServerUrl, credential.UserName, credential.Password.ToUnsecureString());
            var project = client.FindProject(config["ProjectName"]);

            var types = from t in client.GetIssueTypes(project?.Id)
                        select t.Name;

            return Task.FromResult(types);
        }
    }
}
