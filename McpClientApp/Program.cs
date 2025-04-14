using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace McpClientApp
{
    static class Program
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private const string BaseUrl = "http://localhost:3001";

        static async Task Main(string[] args)
        {
            Console.WriteLine("MCP Client starting...");
            
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => 
            {
                e.Cancel = true;
                cts.Cancel();
            };
            
            // Start listening for SSE events in a background task
            var sseTask = Task.Run(() => ReceiveSseEventsAsync(cts.Token), cts.Token);
            
            // Wait a moment for SSE connection to establish
            await Task.Delay(1000, cts.Token);
            
            while (!cts.Token.IsCancellationRequested)
            {
                Console.WriteLine("\nChoose an action:");
                Console.WriteLine("1 - Send LLM prompt");
                Console.WriteLine("2 - Call echo tool");
                Console.WriteLine("3 - Exit");
                
                var key = Console.ReadKey(true);
                
                try
                {
                    switch (key.KeyChar)
                    {
                        case '1':
                            Console.Write("Enter your prompt: ");
                            var prompt = Console.ReadLine() ?? "Tell me a joke";
                            await SendLlmPromptAsync(prompt, cts.Token);
                            break;
                        case '2':
                            Console.Write("Enter message for echo tool: ");
                            var message = Console.ReadLine() ?? "Hello, world!";
                            await CallEchoToolAsync(message, cts.Token);
                            break;
                        case '3':
                            cts.Cancel();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            
            try
            {
                await sseTask;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Client shut down.");
            }
        }
        
        static async Task ReceiveSseEventsAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Connecting to SSE endpoint...");
            
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/sse");
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            
            Console.WriteLine("SSE connection established. Waiting for events...");
            
            string? line;
            StringBuilder dataBuilder = new();
            
            while (!cancellationToken.IsCancellationRequested && (line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    // Empty line indicates the end of an event
                    if (dataBuilder.Length > 0)
                    {
                        var eventData = dataBuilder.ToString();
                        ProcessSseEvent(eventData);
                        dataBuilder.Clear();
                    }
                    continue;
                }
                
                if (line.StartsWith("data: "))
                {
                    dataBuilder.AppendLine(line.Substring(6));
                }
            }
        }
        
        static void ProcessSseEvent(string eventData)
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(eventData);
                
                if (jsonElement.TryGetProperty("method", out var methodElement) && 
                    methodElement.GetString() == "tools/response")
                {
                    // Handle tool response
                    if (jsonElement.TryGetProperty("params", out var paramsElement))
                    {
                        Console.WriteLine("\n--- Tool Response ---");
                        var formattedJson = JsonSerializer.Serialize(paramsElement, new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine(formattedJson);
                    }
                }
                else if (jsonElement.TryGetProperty("result", out var resultElement))
                {
                    // Handle LLM response
                    Console.WriteLine("\n--- LLM Response ---");
                    var formattedJson = JsonSerializer.Serialize(resultElement, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(formattedJson);
                }
                else
                {
                    // Generic event handling
                    Console.WriteLine("\n--- Server event ---");
                    var formattedJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(formattedJson);
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing event: {ex.Message}");
                Console.WriteLine($"Raw data: {eventData}");
            }
        }
        
        static async Task SendLlmPromptAsync(string prompt, CancellationToken cancellationToken)
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = Guid.NewGuid().ToString(),
                method = "tools/call",
                @params = new
                {
                    name = "sampleLLM",
                    arguments = new
                    {
                        prompt = prompt,
                        maxTokens = 100
                    }
                }
            };
            
            var response = await HttpClient.PostAsJsonAsync($"{BaseUrl}/message", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            Console.WriteLine("LLM request sent successfully. Waiting for response...");
        }
        
        static async Task CallEchoToolAsync(string message, CancellationToken cancellationToken)
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/call",
                @params = new
                {
                    name = "echo",
                    arguments = new
                    {
                        message = message
                    }
                }
            };
            
            var response = await HttpClient.PostAsJsonAsync($"{BaseUrl}/message", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            Console.WriteLine("Echo tool request sent successfully. Waiting for response...");
        }
    }
}