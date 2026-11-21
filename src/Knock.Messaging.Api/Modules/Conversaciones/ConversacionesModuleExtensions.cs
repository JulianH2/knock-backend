namespace Knock.Messaging.Api.Modules.Conversaciones;

/// <summary>Registro del módulo Conversaciones (spec 030). Solo esta clase es pública; el resto del módulo es internal.</summary>
public static class ConversacionesModuleExtensions
{
    /// <summary>Registra el manejador que traduce <c>DomainException</c> a ProblemDetails con el <c>code</c> de su catálogo.</summary>
    public static IServiceCollection AddConversacionesModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddExceptionHandler<DomainExceptionHandler>();
        return services;
    }
}
