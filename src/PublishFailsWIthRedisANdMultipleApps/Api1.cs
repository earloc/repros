#:sdk Microsoft.Net.Sdk.Web

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/test", () => "Hello World!");
await app.RunAsync();