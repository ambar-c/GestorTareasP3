var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/salud", () => "OK");

app.Run();
