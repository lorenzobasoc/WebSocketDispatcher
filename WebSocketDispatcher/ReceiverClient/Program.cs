using System.Net.WebSockets;
using System.Text;

namespace ReceiverClient;

public class Program
{
    public async static Task Main() {
        try {
            var ws = new ClientWebSocket();
            do {
                try {
                    ws = new ClientWebSocket();
                    Console.WriteLine("Connecting to Dispatcher Server...");
                    await ws.ConnectAsync(new Uri("ws://localhost:6000/dispatch"), CancellationToken.None);
                } catch (WebSocketException) {
                    Console.WriteLine("No connection could be made because the target machine actively refused it. New try in 5 seconds.");
                    Console.WriteLine();
                    await Task.Delay(5000);
                }
            } while (ws.State != WebSocketState.Open);
            Console.WriteLine("Connected!");
            var buffer = new byte[1024 * 4];
            while (true) {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) {
                    Console.WriteLine();
                    Console.WriteLine("Connection close. Closing message: " + result.CloseStatusDescription);
                    Console.WriteLine();
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    await Task.Delay(5000);
                    await Main();
                } else {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine("Recived: " + message);
                }
            }
        } catch (WebSocketException) {
            Console.WriteLine("Connection lost.");
            Console.WriteLine("Connection with sender lost. ");
            Console.WriteLine();
            await Main();
        }
    } 
}
