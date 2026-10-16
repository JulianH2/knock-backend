using Testcontainers.PostgreSql;

namespace Knock.IntegrationTest.Data;

internal sealed class PostgisFixture : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgis/postgis:16-3.4").Build();

    public string ConnectionString => _container.GetConnectionString();

    public static async Task<PostgisFixture> StartAsync()
    {
        var fixture = new PostgisFixture();
        await fixture._container.StartAsync();
        return fixture;
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
