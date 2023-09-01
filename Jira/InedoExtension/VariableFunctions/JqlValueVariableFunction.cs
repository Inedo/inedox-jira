using System.Text.RegularExpressions;
using Inedo.Extensibility;
using Inedo.Extensibility.VariableFunctions;

#nullable enable

namespace Inedo.Extensions.Jira.VariableFunctions;

[ScriptAlias("JqlValue")]
public sealed class JqlValueVariableFunction : ScalarVariableFunction
{
    [VariableFunctionParameter(0)]
    public string? Value { get; set; }

    protected override object EvaluateScalar(IVariableFunctionContext context)
    {
        return $"\"{Regex.Replace(this.Value ?? string.Empty, @"[""\\]", "\\$1")}\"";
    }
}
