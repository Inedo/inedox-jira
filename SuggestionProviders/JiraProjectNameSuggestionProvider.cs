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
    public sealed class JiraProjectNameSuggestionProvider : ISuggestionProvider
    {
        public Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var empty = Task.FromResult(Enumerable.Empty<string>());

            string credentialName = config["CredentialName"];
            if (string.IsNullOrEmpty(credentialName))
                return empty;

            var credential = ResourceCredentials.Create<JiraCredentials>(credentialName);
            if (credential == null)
                return empty;

            JiraApiType api;
            api = Enum.TryParse(config["Api"], out api) ? api : JiraApiType.AutoDetect;

            var client = JiraClient.Create(credential.ServerUrl, credential.UserName, credential.Password.ToUnsecureString(), apiType: api);
            var proj = from p in client.GetProjects()
                       select p.Name;

            return Task.FromResult(proj);
        }
    }
}
