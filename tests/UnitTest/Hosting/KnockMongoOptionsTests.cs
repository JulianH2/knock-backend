using AwesomeAssertions;
using Knock.Api.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

namespace Knock.UnitTest.Hosting;

public sealed class KnockMongoOptionsTests
{
    private const string _maxPoolSizeKey = "Mongo:MaxPoolSize";

    public static TheoryData<string, string> InvalidTimeouts()
    {
        var data = new TheoryData<string, string>();
        foreach (var key in new[]
        {
            "Mongo:ServerSelectionTimeoutSeconds", "Mongo:ConnectTimeoutSeconds", "Mongo:SocketTimeoutSeconds", "Mongo:QueryMaxTimeSeconds",
        })
        {
            foreach (var value in new[] { "0", "-1", "61", "cinco", "2.5", "" })
            {
                data.Add(key, value);
            }
        }

        return data;
    }

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

    [Fact]
    public void TimeoutsMissingFromConfiguration_UseTheDefaults_TC_TT_BD_06_13()
    {
        using var provider = MongoTestServices.Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = MongoTestServices.SyntheticConnectionString(),
            [_maxPoolSizeKey] = "7",
        });

        provider.GetRequiredService<IStartupValidator>().Validate();
        var settings = provider.GetRequiredService<IMongoClient>().Settings;

        settings.ServerSelectionTimeout.Should().Be(TimeSpan.FromSeconds(5));
        settings.ConnectTimeout.Should().Be(TimeSpan.FromSeconds(5));
        settings.SocketTimeout.Should().Be(TimeSpan.FromSeconds(10));
        provider.GetRequiredService<IOptions<KnockMongoOptions>>().Value.QueryMaxTime.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TimeoutsComeFromConfigurationAndRepositoryAppSettingsFixesThem_TC_TT_BD_06_13()
    {
        using var provider = MongoTestServices.Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = MongoTestServices.SyntheticConnectionString(),
            [_maxPoolSizeKey] = "7",
            ["Mongo:ServerSelectionTimeoutSeconds"] = "3",
            ["Mongo:ConnectTimeoutSeconds"] = "4",
            ["Mongo:SocketTimeoutSeconds"] = "60",
            ["Mongo:QueryMaxTimeSeconds"] = "1",
        });
        var configuration = new ConfigurationBuilder().AddJsonFile(MongoTestServices.RepositoryAppSettings(), optional: false).Build();

        provider.GetRequiredService<IStartupValidator>().Validate();
        var settings = provider.GetRequiredService<IMongoClient>().Settings;

        settings.ServerSelectionTimeout.Should().Be(TimeSpan.FromSeconds(3));
        settings.ConnectTimeout.Should().Be(TimeSpan.FromSeconds(4));
        settings.SocketTimeout.Should().Be(TimeSpan.FromSeconds(60), "60 s es el máximo permitido");
        provider.GetRequiredService<IOptions<KnockMongoOptions>>().Value.QueryMaxTime.Should().Be(TimeSpan.FromSeconds(1));
        configuration["Mongo:ServerSelectionTimeoutSeconds"].Should().Be("5");
        configuration["Mongo:ConnectTimeoutSeconds"].Should().Be("5");
        configuration["Mongo:SocketTimeoutSeconds"].Should().Be("10");
        configuration["Mongo:QueryMaxTimeSeconds"].Should().Be("5");
    }

    [Theory]
    [MemberData(nameof(InvalidTimeouts))]
    public void TimeoutZeroNegativeOverSixtySecondsOrNotInteger_FailsStartupNamingTheKey_TC_TT_BD_06_13(string key, string value)
    {
        var connectionString = MongoTestServices.SyntheticConnectionString();
        using var provider = MongoTestServices.Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = connectionString,
            [_maxPoolSizeKey] = "7",
            [key] = value,
        });

        var startup = () => provider.GetRequiredService<IStartupValidator>().Validate();

        var message = startup.Should().Throw<OptionsValidationException>().Which.Message;
        message.Should().Contain(key);
        message.Should().NotContain(connectionString).And.NotContain(MongoUrl.Create(connectionString).Password);
    }
}
