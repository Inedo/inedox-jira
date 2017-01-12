using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Web.Controls;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Web.Controls;
#endif

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraApiVersionSuggestionProvider : ISuggestionProvider
    {
        public Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            return Task.FromResult(Enum.GetNames(typeof(JiraApiType)).AsEnumerable());
        }
    }
}
