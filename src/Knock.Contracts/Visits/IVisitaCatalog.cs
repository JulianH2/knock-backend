namespace Knock.Contracts.Visits;

/// <summary>Resultado de comprobar si una persona pertenece a una visita (spec 026, R-06 y R-07). Nunca se asume pertenencia cuando no se pudo comprobar.</summary>
public enum VisitMembershipStatus
{
    /// <summary>La visita existe y la persona es el cliente dueño o el técnico elegido.</summary>
    Member,

    /// <summary>La visita no existe o no es de esa persona; no se distingue una cosa de la otra.</summary>
    NotMember,

    /// <summary>El servicio dueño de la visita no respondió a tiempo o contestó algo que no se entiende.</summary>
    Unavailable,
}

/// <summary>Lo único que Mensajería conserva de la vista de una visita: el rol de quien pregunta y el estado de la visita (nunca el código ni la dirección).</summary>
/// <param name="Status">Si la persona es parte de la visita.</param>
/// <param name="Role"><c>cliente</c> o <c>tecnico</c> cuando <paramref name="Status"/> es <see cref="VisitMembershipStatus.Member"/>.</param>
/// <param name="State">Estado de la visita (<c>confirmada</c>, <c>en_curso</c>, <c>terminada</c>…) cuando es miembro.</param>
public sealed record VisitMembership(VisitMembershipStatus Status, string? Role = null, string? State = null);

/// <summary>
/// Pregunta a Mercado si una persona es parte de una visita, reenviando su sesión (spec 026, decisión «la pertenencia se pregunta, no se copia»). Se registra como
/// <c>scoped</c> y nunca se captura en un singleton.
/// </summary>
public interface IVisitaCatalog
{
    /// <summary>Comprueba la pertenencia de quien tiene <paramref name="bearerToken"/> a la visita <paramref name="visitId"/>.</summary>
    Task<VisitMembership> CheckAsync(string visitId, string bearerToken, CancellationToken cancellationToken);
}
