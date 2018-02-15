using System;
using System.Collections.Generic;

namespace Inedo.Extensions.Jira
{
    [Serializable]
    internal sealed class JiraProject
    {
        public JiraProject()
        {
        }
        public JiraProject(string id, string key, string name)
        {
            this.Id = id;
            this.Key = key;
            this.Name = name;
        }
        public JiraProject(Dictionary<string, object> project)
        {
            this.Key = project["key"].ToString();
            this.Name = project["name"].ToString();
            this.Id = project["id"].ToString();
        }

        public string Id { get; }
        public string Key { get; }
        public string Name { get; }
    }
}
