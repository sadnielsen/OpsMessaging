// CONCEPT (Clean Architecture): Program.cs is the composition root: the single place where
// ports (Application) are wired to adapters (Infrastructure). Endpoints, auth and the
// AddInfrastructure() registration are added in later phases.
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Run();
