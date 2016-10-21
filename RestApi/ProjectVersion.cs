using System;
using System.Collections.Generic;
using Inedo.BuildMasterExtensions.Jira.JiraApi;

namespace Inedo.BuildMasterExtensions.Jira
{
    internal sealed class ProjectVersion
    {
        public ProjectVersion()
        {
        }

        public static ProjectVersion Parse(Dictionary<string, object> v)
        {
            string id = v?["id"].ToString();
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return new ProjectVersion
            {
                Id = id,
                Name = v["name"].ToString(),
                Released = "true".Equals(v["released"].ToString(), StringComparison.OrdinalIgnoreCase)
            };
        }

        public ProjectVersion(RemoteVersion version)
        {
            this.Id = version.id;
            this.Name = version.name;
            this.Released = version.released;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public bool Released { get; private set; }
    }
}
