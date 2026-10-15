using Knock.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddKnockApi();

var app = builder.Build();
app.UseKnockApi();
await app.RunAsync();
