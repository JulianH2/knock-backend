using AwesomeAssertions;
using Knock.Market.Api.Modules.Catalog.Dtos;
using Knock.Market.Api.Modules.Catalog.Services;
using Knock.Market.Api.Modules.Catalog.Validators;
using Xunit;

namespace Knock.UnitTest.Catalog;

public sealed class Spec023ValidacionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("１２３４５")]
    [InlineData(".*")]
    [InlineData("42800\0")]
    public void Codigo_Ruta_Mal_Formado_CA_023_03(string cp)
    {
        var result = new PostalCodeRouteValidator().Validate(new PostalCodeRoute { Cp = cp });
        result.Errors.Should().Contain(error => error.PropertyName == "Cp");
    }

    [Theory]
    [InlineData("01234")]
    [InlineData("99999")]
    public void Codigo_Ruta_Valido_CA_023_03(string cp) =>
        new PostalCodeRouteValidator().Validate(new PostalCodeRoute { Cp = cp }).IsValid.Should().BeTrue();
    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    [InlineData("１２３４５")]
    [InlineData("123456")]
    [InlineData("12$45")]
    public void Codigo_Mal_Formado_CA_023_03(string cp) =>
        new WaitlistRequestValidator().Validate(new WaitlistRequest(cp, null)).Errors.Should().Contain(e => e.PropertyName == "Cp");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Tula1")]
    [InlineData("<script>")]
    [InlineData("Tula\n")]
    public void Municipio_Invalido_CA_023_08(string municipio) =>
        new WaitlistRequestValidator().Validate(new WaitlistRequest("91000", municipio)).Errors.Should().Contain(e => e.PropertyName == "Municipio");

    [Fact]
    public void Municipio_Excede_Cien_Caracteres_CA_023_08() =>
        new WaitlistRequestValidator().Validate(new WaitlistRequest("91000", new string('a', 101))).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(null)]
    [InlineData("Tula de Allende")]
    [InlineData("San Agustín Tlaxiaca")]
    [InlineData("Ixmiquilpan")]
    public void Municipios_Validos_CA_023_09(string? municipio) =>
        new WaitlistRequestValidator().Validate(new WaitlistRequest("01234", municipio)).IsValid.Should().BeTrue();
}

public sealed class Spec023ListaEsperaTests
{
    [Fact]
    public async Task Registra_Lista_De_Espera_CA_023_05()
    {
        var store = new FakeWaitlistStore();
        var result = await new WaitlistService(store).JoinAsync("user-a", new WaitlistRequest("91000", "Tula"), default);
        result.AlreadyRegistered.Should().BeFalse();
        result.Entry.Cp.Should().Be("91000");
        store.LastUser.Should().Be("user-a");
    }

    [Fact]
    public async Task Idempotente_Por_Persona_Y_Codigo_CA_023_06()
    {
        var service = new WaitlistService(new FakeWaitlistStore());
        var first = await service.JoinAsync("user-a", new WaitlistRequest("91000", "Tula"), default);
        var second = await service.JoinAsync("user-a", new WaitlistRequest("91000", "Otro"), default);
        second.AlreadyRegistered.Should().BeTrue();
        second.Entry.Should().Be(first.Entry);
    }

    [Fact]
    public async Task Dos_Personas_O_Dos_Codigos_CA_023_07()
    {
        var service = new WaitlistService(new FakeWaitlistStore());
        (await service.JoinAsync("a", new WaitlistRequest("91000", null), default)).AlreadyRegistered.Should().BeFalse();
        (await service.JoinAsync("b", new WaitlistRequest("91000", null), default)).AlreadyRegistered.Should().BeFalse();
        (await service.JoinAsync("a", new WaitlistRequest("91001", null), default)).AlreadyRegistered.Should().BeFalse();
    }

    private sealed class FakeWaitlistStore : IWaitlistStore
    {
        private readonly Dictionary<(string, string), WaitlistEntry> _entries = new();
        public string? LastUser { get; private set; }

        public Task<WaitlistResult> JoinAsync(string user, WaitlistRequest request, CancellationToken cancellationToken)
        {
            LastUser = user;
            var key = (user, request.Cp!);
            if (_entries.TryGetValue(key, out var existing))
            {
                return Task.FromResult(new WaitlistResult(existing, true));
            }

            var entry = new WaitlistEntry(Guid.NewGuid().ToString(), request.Cp!, request.Municipio, DateTime.UtcNow);
            _entries.Add(key, entry);
            return Task.FromResult(new WaitlistResult(entry, false));
        }
    }
}
