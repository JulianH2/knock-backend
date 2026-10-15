// Tipos de prueba para el caso negativo de la regla de frontera (TC-TT-API-01-08).
// Alpha y Beta imitan dos módulos: lo público de Beta está en su raíz y sus internos en subespacios.
namespace Knock.UnitTest.Architecture.Fixtures.Violating.Beta.Domain
{
    internal sealed class BetaEntity
    {
        public int Id { get; init; }
    }
}

namespace Knock.UnitTest.Architecture.Fixtures.Violating.Alpha
{
    using Knock.UnitTest.Architecture.Fixtures.Violating.Beta.Domain;

    internal sealed class AlphaUsesBetaInternal
    {
        public BetaEntity Create() => new() { Id = 1 };
    }
}

namespace Knock.UnitTest.Architecture.Fixtures.Compliant.Beta
{
    internal interface IBetaApi
    {
        int Count();
    }
}

namespace Knock.UnitTest.Architecture.Fixtures.Compliant.Alpha
{
    using Knock.UnitTest.Architecture.Fixtures.Compliant.Beta;

    internal sealed class AlphaUsesBetaApi(IBetaApi beta)
    {
        public int Total() => beta.Count();
    }
}
