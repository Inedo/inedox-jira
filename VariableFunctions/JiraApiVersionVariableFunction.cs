using System.ComponentModel;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;
using Inedo.BuildMasterExtensions.Jira.SuggestionProviders;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.Jira.VariableFunctions
{
    [ScriptAlias("JiraApiVersion")]
    [Description("the API version of JIRA to use, either SOAP (for JIRA v5 and earlier) or RESTv2 (for JIRA v6 and later)")]
    [ExtensionConfigurationVariable(Required = false, SuggestionProvider = typeof(JiraApiVersionSuggestionProvider), Type = ExpectedValueDataType.String)]
    [Tag("jira")]
    public sealed class JiraApiVersionVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IGenericBuildMasterContext context)
        {
            return JiraApiType.RESTv2.ToString();
        }
    }
}
