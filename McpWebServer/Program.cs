using System.Reflection;
using AspNetCoreSseServer;
using McpTools.Tools;
using ModelContextProtocol;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithToolsFromAssembly(Assembly.GetAssembly(typeof(EchoTool)));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapMcpSse();

app.Run();

