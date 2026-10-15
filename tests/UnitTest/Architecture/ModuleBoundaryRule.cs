using System.Reflection;
using System.Text.RegularExpressions;
using NetArchTest.Rules;

namespace Knock.UnitTest.Architecture;

internal sealed record BoundaryResult(bool IsSuccessful, IReadOnlyList<string> FailingTypes);

/// <summary>
/// Regla de frontera: los tipos de un módulo no dependen de los internos de otro. Lo público de un módulo vive
/// en la raíz de su espacio de nombres (<c>Modules.X</c>); el resto va en subespacios (<c>Modules.X.Domain</c>…).
/// </summary>
internal static class ModuleBoundaryRule
{
    public static BoundaryResult Evaluate(Assembly assembly, string modulesNamespace, IReadOnlyList<string> modules)
    {
        var failing = new List<string>();
        foreach (var module in modules)
        {
            // Internos de otro módulo = cada subespacio de su espacio de nombres (Modules.X.Domain, Modules.X.Services…).
            // La búsqueda de dependencias de NetArchTest no acepta el punto final como límite, por eso se listan los subespacios reales.
            var otherInternals = assembly.GetTypes()
                .Select(type => type.Namespace)
                .OfType<string>()
                .Where(ns => modules.Any(other => other != module && ns.StartsWith($"{modulesNamespace}.{other}.", StringComparison.Ordinal)))
                .Distinct()
                .ToArray();

            if (otherInternals.Length == 0)
            {
                continue;
            }

            var result = Types.InAssembly(assembly)
                .That().ResideInNamespaceMatching($"^{Regex.Escape($"{modulesNamespace}.{module}")}([.]|$)")
                .ShouldNot().HaveDependencyOnAny(otherInternals)
                .GetResult();

            if (!result.IsSuccessful)
            {
                failing.AddRange(result.FailingTypes?.Select(t => t.FullName) ?? []);
            }
        }

        return new BoundaryResult(failing.Count == 0, failing);
    }
}
