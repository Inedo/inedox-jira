using System;
using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensions.Jira;
using Inedo.Extensions.Jira.SuggestionProviders;

#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;
#elif Otter
using Inedo.Otter;
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.VariableFunctions;
#endif

namespace Inedo.BuildMasterExtensions.Jira.VariableFunctions
{
    [ScriptAlias("JiraApiVersion")]
    [Description("the API version of JIRA to use, either SOAP (for JIRA v5 and earlier) or RESTv2 (for JIRA v6 and later)")]
    [ExtensionConfigurationVariable(Required = false, SuggestionProvider = typeof(JiraApiVersionSuggestionProvider), Type = ExpectedValueDataType.String)]
    [Tag("jira")]
    [DefaultValue(JiraApiType.AutoDetect)]
    public sealed class JiraApiVersionVariableFunction : ScalarVariableFunction
    {
#if BuildMaster
        protected override object EvaluateScalar(IGenericBuildMasterContext context)
        {
            return JiraApiType.AutoDetect;
        }
#elif Otter
        protected override object EvaluateScalar(IOtterContext context)
        {
            return JiraApiType.AutoDetect;
        }
#endif
    }
}
