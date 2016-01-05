using System.Collections.Generic;

namespace Inedo.BuildMasterExtensions.Jira.RestApi
{
    internal sealed class Transition
    {
        public Transition(Dictionary<string, object> transition)
        {
            this.Id = transition["id"].ToString();
            this.Name = transition["name"].ToString();
        }

        public string Id { get; }
        public string Name { get; }
    }
}
