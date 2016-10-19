using System.ComponentModel;

namespace Inedo.BuildMasterExtensions.Jira
{
    public enum JiraApiType
    {
        [Description("SOAP API (for JIRA v5 or earlier)")]
        SOAP,
        [Description("REST API (for JIRA v6 or later)")]
        RESTv2
    }
}
