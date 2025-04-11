using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpTools.Tools;

[McpToolType]
public static class EchoTool
{
    [McpTool("echo"), Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}