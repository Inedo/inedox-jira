using System;
using System.Collections.Generic;

namespace Inedo.BuildMasterExtensions.Jira
{
    [Serializable]
    internal sealed class JiraProject
    {
        public JiraProject(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public JiraProject(Dictionary<string, object> project)
        {
            this.Id = project["key"].ToString();
            this.Name = project["name"].ToString();
        }

        public string Id { get; }
        public string Name { get; }
    }
}
