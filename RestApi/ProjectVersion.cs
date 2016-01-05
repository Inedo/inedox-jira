using System;
using System.Collections.Generic;

namespace Inedo.BuildMasterExtensions.Jira.RestApi
{
    internal sealed class ProjectVersion
    {
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

        private ProjectVersion()
        {
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public bool Released { get; private set; }
    }
}
