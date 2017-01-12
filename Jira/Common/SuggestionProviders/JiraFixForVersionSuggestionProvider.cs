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
    public sealed class JiraFixForVersionSuggestionProvider : ISuggestionProvider
    {
        public Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var empty = new[] { "$ReleaseNumber" };
            var emptyResult = Task.FromResult((IEnumerable<string>)empty);

            string credentialName = config["CredentialName"];
            if (string.IsNullOrEmpty(credentialName))
                return emptyResult;

            string projectName = config["ProjectName"];
            if (string.IsNullOrEmpty(projectName))
                return emptyResult;

            var credential = ResourceCredentials.Create<JiraCredentials>(credentialName);
            if (credential == null)
                return emptyResult;

            JiraApiType api;
            api = Enum.TryParse(config["Api"], out api) ? api : JiraApiType.AutoDetect;
            
            var client = JiraClient.Create(credential.ServerUrl, credential.UserName, credential.Password.ToUnsecureString(), apiType: api);
            var proj = client.GetProjects().FirstOrDefault(p => string.Equals(projectName, p.Name, StringComparison.OrdinalIgnoreCase));
            if (proj == null)
                return emptyResult;

            var versions = from v in client.GetProjectVersions(proj.Key)
                           select v.Name;

            return Task.FromResult(empty.Concat(versions));
        }
    }
}
