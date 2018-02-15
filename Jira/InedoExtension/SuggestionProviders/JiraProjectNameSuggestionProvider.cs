using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensions.Jira.Clients;
using Inedo.Extensions.Jira.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraProjectNameSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var empty = new[] { "$ApplicationName" };

            string credentialName = config["CredentialName"];
            if (string.IsNullOrEmpty(credentialName))
                return empty;

            var credential = ResourceCredentials.Create<JiraCredentials>(credentialName);
            if (credential == null)
                return empty;

            var client = JiraClient.Create(credential.ServerUrl, credential.UserName, AH.Unprotect(credential.Password));
            var proj = from p in await client.GetProjectsAsync()
                       select p.Name;

            return empty.Concat(proj);
        }
    }
}
