using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

namespace Knock.UnitTest.Hosting;

public sealed class KnockMongoOptionsTests
{
    private const string _maxPoolSizeKey = "Mongo:MaxPoolSize";

    [Fact]
    public void MaxPoolSizeComesFromConfigurationAndIsValidated_TC_TT_BD_06_13()
    {
        var connectionString = MongoTestServices.SyntheticConnectionString();
        MongoUrl.Create(connectionString).MaxConnectionPoolSize.Should().Be(100, "la cadena pide 100 para que se vea quién manda");
        using var provider = MongoTestServices.Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = connectionString,
            [_maxPoolSizeKey] = "7",
        });

        provider.GetRequiredService<IStartupValidator>().Validate();
        var client = provider.GetRequiredService<IMongoClient>();

        client.Settings.MaxConnectionPoolSize.Should().Be(7);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("101")]
    [InlineData("veinte")]
    [InlineData("20.5")]
    [InlineData("")]
    public void MaxPoolSizeMissingOutOfRangeOrNotNumeric_FailsStartupNamingTheKey_TC_TT_BD_06_13(string? value)
    {
        var connectionString = MongoTestServices.SyntheticConnectionString();
        using var provider = MongoTestServices.Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = connectionString,
            [_maxPoolSizeKey] = value,
        });

        var startup = () => provider.GetRequiredService<IStartupValidator>().Validate();

        var message = startup.Should().Throw<OptionsValidationException>().Which.Message;
        message.Should().Contain(_maxPoolSizeKey);
        message.Should().NotContain(connectionString).And.NotContain(MongoUrl.Create(connectionString).Password);
    }

    [Fact]
    public void RepositoryAppSettings_FixesMaxPoolSizeAtTwenty_TC_TT_BD_06_13()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile(MongoTestServices.RepositoryAppSettings(), optional: false).Build();

        configuration[_maxPoolSizeKey].Should().Be("20");
    }
}
