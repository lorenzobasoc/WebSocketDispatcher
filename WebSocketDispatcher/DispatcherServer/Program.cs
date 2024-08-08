using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DispatcherServer.DataAccess;
using DispatcherServer.DTOs;
using DispatcherServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispatcherServer;

public class Program
{
    private static readonly List<LogDto> Logs = new();
    private static readonly object _lock = new();
    private static bool Stopped = false;

    public async static Task Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql("Host=localhost;Database=iot_project;Username=postgres;Password=postgres;")
            .UseSnakeCaseNamingConvention());
        builder.WebHost.UseUrls("http://localhost:6000");
        var app = builder.Build();
        app.UseWebSockets();
        app.Lifetime.ApplicationStopping.Register(() => Stopped = true);
        app.Map("/dispatch", async context => {
            await SendToReceiver(app, context);
        });

        await MigrateDatabase(app);

        StartApp(app);

        await ReceiveLogs(app);
    }

    private async static Task SendToReceiver(WebApplication app, HttpContext context) {
        WriteReceiverLine("Connected to receiver!");
        try {
            if (context.WebSockets.IsWebSocketRequest) {
                using var ws = await context.WebSockets.AcceptWebSocketAsync();
                if (Logs.Count > 0) {
                    while (true) {
                        var log = Logs.FirstOrDefault();
                        if (log == null) {
                            break;
                        }
                        var message = JsonSerializer.Serialize(log);
                        var bytes = Encoding.UTF8.GetBytes(message);
                        var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
                        if (ws.State == WebSocketState.Open) {
                            await ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                            WriteReceiverLine("Log sent.");
                        } else if (ws.State == WebSocketState.Closed || ws.State == WebSocketState.Aborted) {
                            WriteReceiverLine("Connection with receiver closed.");
                            break;
                        }
                        lock (_lock) {
                            Logs.Remove(log);
                        }
                        await Task.Delay(500);
                    }
                }
                WriteReceiverLine("No logs in queue, connection closed.");
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "There are no logs in queue.", CancellationToken.None);
            }
        } catch (Exception ex) {
            WriteReceiverLine("Connection with receiver lost. ");
            WriteReceiverLine(ex.GetType().ToString());
            WriteReceiverLine(ex.Message);
        }
    }

    private static async void StartApp(WebApplication app) {
        await app.RunAsync();
    }

    private async static Task ReceiveLogs(WebApplication app) {
        while (true) {
            try {
                if (Stopped) {
                    WriteSenderLine("Connection closed.");
                    break;
                }

                var ws = await ConnectToClient();

                WriteSenderLine("Connected to sender!");
                    
                await Receive(ws, app);
            } catch (Exception ex) {
                WriteSenderLine("");
                WriteSenderLine("Connection with sender lost. ");
                WriteSenderLine(ex.GetType().ToString());
                WriteSenderLine(ex.Message);
            }
        }
    }

    private static async Task<ClientWebSocket> ConnectToClient() {
        var ws = new ClientWebSocket();
        do {
            try {
                ws = new ClientWebSocket();
                WriteSenderLine("Connecting to Sender...");
                await ws.ConnectAsync(new Uri("ws://localhost:6999/send"), CancellationToken.None);
            } catch (WebSocketException) {
                WriteSenderLine("No connection could be made because the target machine actively refused it. New try in 5 seconds.");
                WriteSenderLine("");
                await Task.Delay(5000);
            }
        } while (ws.State != WebSocketState.Open);

        return ws;
    } 

    private static async Task Receive(ClientWebSocket ws, WebApplication app) {
        var buffer = new byte[1024 * 4];
        while (true) {
            if (Stopped) {
                WriteSenderLine("Closing connection...");
                await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                break;
            }
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close) {
                WriteSenderLine("Connection with sender closed.");
                break;
            } else {
                WriteSenderLine("Recived log.");
                var dtoString = Encoding.UTF8.GetString(buffer);
                dtoString = dtoString[..result.Count];
                var dto =  JsonSerializer.Deserialize<LogDto>(dtoString);
                await SaveLog(dto, app);
                lock (_lock) {
                    Logs.Add(dto);
                }
            }
        }
    }

    private async static Task SaveLog(LogDto dto, WebApplication app) {
        var db = CreateDbContext(app);
        var entity = MapToEntity(dto);
        if (db != null) {
            db.Logs.Add(entity);
            await db.SaveChangesAsync();
        }
    }

    private static LogEntity MapToEntity(LogDto dto) {
        return new LogEntity {
            Time = dto.Time.ToUniversalTime(),
            Author = dto.Author,
            Division = dto.Division,
            Type = dto.Type,
            Text = dto.Text,
        };
    }

    private static AppDbContext CreateDbContext(WebApplication app) {
        try {
            var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return db;
        } catch (ObjectDisposedException) {
            return null;
        }
    }

    private async static Task MigrateDatabase(WebApplication app) {
        var db = CreateDbContext(app);
        if (db != null) {
            await db.Database.MigrateAsync();
        }   
    }
    
    private static void WriteSenderLine(string msg) {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(msg);
    }

    private static void WriteReceiverLine(string msg) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(msg);
    }
}
