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
    public sealed class JiraIssueTypeNameSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var empty = Enumerable.Empty<string>();

            string credentialName = config["CredentialName"];
            if (string.IsNullOrEmpty(credentialName))
                return empty;

            var credential = ResourceCredentials.Create<JiraCredentials>(credentialName);
            if (credential == null)
                return empty;

            var client = JiraClient.Create(credential.ServerUrl, credential.UserName, AH.Unprotect(credential.Password));
            var project = await client.FindProjectAsync(config["ProjectName"]);

            var types = from t in await client.GetIssueTypesAsync(project?.Id)
                        select t.Name;

            return types;
        }
    }
}
