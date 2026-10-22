using System.Reflection;
using AwesomeAssertions;
using Knock.Api.Common.Errors;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Knock.UnitTest.Errors;

public class DomainExceptionMappingTests
{
    public static TheoryData<DomainErrorKind, int> Catalog => new()
    {
        { DomainErrorKind.BadRequest, 400 },
        { DomainErrorKind.Forbidden, 403 },
        { DomainErrorKind.NotFound, 404 },
        { DomainErrorKind.Conflict, 409 },
        { DomainErrorKind.Unprocessable, 422 },
    };

    [Theory]
    [MemberData(nameof(Catalog))]
    public void EachDomainCode_HasAMapping_TC_TT_API_03_13(DomainErrorKind kind, int expectedStatus)
    {
        DomainErrors.ToStatusCode(kind).Should().Be(expectedStatus);
        new DomainException(kind, "synthetic").Kind.Should().Be(kind);
    }

    [Fact]
    public void CatalogCoversEveryKind_AndNeverIncludes401Or429_TC_TT_API_03_13()
    {
        var statuses = Enum.GetValues<DomainErrorKind>().Select(DomainErrors.ToStatusCode).ToList();

        statuses.Should().BeEquivalentTo([400, 403, 404, 409, 422], "el catálogo de dominio es solo ese");
        statuses.Should().NotContain(401).And.NotContain(429);
        Catalog.Select(row => (DomainErrorKind)row[0]).Should().BeEquivalentTo(Enum.GetValues<DomainErrorKind>());
    }

    [Fact]
    public void UndefinedKind_IsRejected_TC_TT_API_03_13()
    {
        var act = () => DomainErrors.ToStatusCode((DomainErrorKind)999);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SynchronousControllerActions_HaveNoTryCatch_TC_TT_API_03_13()
    {
        // Los métodos async llevan try/catch generado por el compilador en su máquina de estados; aquí se revisan los síncronos.
        var controllers = typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
        controllers.Should().NotBeEmpty();

        var actions = controllers
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>() is null)
            .ToList();

        actions.Should().OnlyContain(m => m.GetMethodBody() == null || m.GetMethodBody()!.ExceptionHandlingClauses.Count == 0);
    }
}
