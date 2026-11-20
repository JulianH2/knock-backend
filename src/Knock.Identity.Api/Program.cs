using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Panel.Cli;

// Modo de herramienta (spec 033, R-25): `Knock.Identity.Api panel <comando>` no levanta el servidor web.
if (PanelCliHost.IsToolMode(args))
{
    return await PanelCliHost.RunAsync(args[1..]);
}

var builder = WebApplication.CreateBuilder(args);
builder.AddKnockApi();

var app = builder.Build();
app.UseKnockApi();
await app.RunAsync();
return 0;
