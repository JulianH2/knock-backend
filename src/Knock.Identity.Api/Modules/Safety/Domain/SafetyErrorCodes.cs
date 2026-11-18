namespace Knock.Identity.Api.Modules.Safety.Domain;

/// <summary>Códigos de error de negocio de los bloqueos (spec 043, §5). En inglés y <c>snake_case</c>, sin datos de nadie.</summary>
internal static class SafetyErrorCodes
{
    public const string UserNotFoundCode = "user_not_found";
    public const string AlreadyBlockedCode = "already_blocked";
    public const string BlockNotFoundCode = "block_not_found";
    public const string InvalidCursorCode = "invalid_cursor";
    public const string ContentRejectedCode = "content_rejected";
    public const string ModerationUnavailableCode = "moderation_unavailable";
    public const string AppealNotAllowedCode = "appeal_not_allowed";
}
