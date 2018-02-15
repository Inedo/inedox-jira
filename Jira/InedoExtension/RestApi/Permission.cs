using System;
using System.Collections.Generic;

namespace Inedo.Extensions.Jira.RestApi
{
    internal sealed class Permission
    {
        public Permission(Dictionary<string, object> permission)
        {
            this.Key = permission["key"].ToString();
            this.HasPermission = permission["havePermission"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public string Key { get; }
        public bool HasPermission { get; }
    }
}
