using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensions.Jira.Clients;

#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.Jira.Credentials;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Credentials;
using Inedo.Otter.Web.Controls;
using Inedo.OtterExtensions.Jira.Credentials;
#endif

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraIssueTypeNameSuggestionProvider : ISuggestionProvider
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
            var project = client.FindProject(config["ProjectName"]);

            var types = from t in client.GetIssueTypes(project?.Id)
                        select t.Name;

            return Task.FromResult(types);
        }
    }
}
