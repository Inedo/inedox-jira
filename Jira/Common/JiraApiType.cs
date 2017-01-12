using System.ComponentModel;

namespace Inedo.BuildMasterExtensions.Jira
{
    public enum JiraApiType
    {
        [Description("Auto-detect API version")]
        AutoDetect,
        [Description("SOAP API (for JIRA v5 or earlier)")]
        SOAP,
        [Description("REST API (for JIRA v6 or later)")]
        RESTv2
    }
}
