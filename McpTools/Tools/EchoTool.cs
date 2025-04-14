using System.ComponentModel;
using System.ComponentModel.Design;
using ModelContextProtocol.Server;

namespace McpTools.Tools;

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool(Destructive = false, Idempotent = true, Name= "echo", OpenWorld = false), 
     Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}