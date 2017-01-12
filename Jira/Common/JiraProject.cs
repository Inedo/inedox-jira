using System;
using System.Collections.Generic;
using Inedo.BuildMasterExtensions.Jira.JiraApi;

namespace Inedo.BuildMasterExtensions.Jira
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

        public JiraProject(RemoteProject proj)
        {
            this.Key = proj.key;
            this.Name = proj.name;
            this.Id = proj.id;
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
