using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Knock.Api.Modules.Catalog.ModelBinders;

/// <summary>
/// Enlaza un parámetro de query de un solo valor como <see cref="string"/>. Siempre devuelve un resultado y nunca marca
/// <c>ModelState</c> (eso daría el 400 automático, que repite el valor): <see langword="null"/> si falta, el valor si llega uno y
/// <see cref="RepeatedValue"/> si llega repetido, que no cumple el formato y el validador rechaza con 422. Nunca toma el primero.
/// </summary>
internal sealed class SingleValueQueryModelBinder : IModelBinder
{
    /// <summary>Valor para un parámetro repetido: no es ninguno de los recibidos y no cumple ningún formato numérico.</summary>
    public const string RepeatedValue = "(repeated)";

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);
        var values = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        bindingContext.Result = ModelBindingResult.Success(values.Length switch
        {
            0 => null,
            1 => values.FirstValue,
            _ => RepeatedValue,
        });
        return Task.CompletedTask;
    }
}

/// <summary>Fuente query con el nombre dado y <see cref="SingleValueQueryModelBinder"/>: un solo atributo de enlace por propiedad.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
internal sealed class SingleValueFromQueryAttribute : ModelBinderAttribute
{
    public SingleValueFromQueryAttribute(string name)
        : base(typeof(SingleValueQueryModelBinder))
    {
        Name = name;
        BindingSource = BindingSource.Query;
    }
}
