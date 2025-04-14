using System.Reflection;
using McpTools.Tools;
using Microsoft.AspNetCore.Builder.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithToolsFromAssembly(Assembly.GetAssembly(typeof(EchoTool)));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
// app.MapMcpSse();
app.MapMcp();

app.Run();

