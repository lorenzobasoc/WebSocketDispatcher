using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Connections;
using SenderClientServer.DTOs;

namespace SenderClientServer;

public class Program
{
    public async static Task Main(string[] args) {       
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://localhost:6999");
        var app = builder.Build();
        app.UseWebSockets();
        app.Map("/send", async context => {
            if (!context.WebSockets.IsWebSocketRequest) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine("Connection completed.");
            CheckWsClosing(ws);
            while (true) {
                var dto = new LogDto {
                    Time = DateTime.Now,
                    Author = "Lorenzo Basoc",
                    Division = "Technical Division",
                    Type = "Information",
                    Text = "Log Text",
                };
                var dtoString = JsonSerializer.Serialize(dto);
                var bytes  = Encoding.UTF8.GetBytes(dtoString);
                var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
                if (ws.State == WebSocketState.Open) {
                    await ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                    Console.WriteLine("Log sent. ");
                } else if (ws.State == WebSocketState.Closed || ws.State == WebSocketState.Aborted) {
                    Console.WriteLine("Connection closed.");
                    break;
                }
                await Task.Delay(1000);
            }
        });
        await app.RunAsync();
    }

    public static async void CheckWsClosing(WebSocket ws) {
        try {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close) {
                Console.WriteLine("Closing connection...");
                await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            }
        } catch (ConnectionAbortedException) {
            ws.Abort();
        }               
    }
}
