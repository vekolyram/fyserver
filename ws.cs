using Microsoft.AspNetCore.Hosting.Server;
using System.Net;
using System.Net.WebSockets;
using System.Text;
namespace fyserver
{
    public class ws
    {
        private WebApplication? app;
        public async Task StartWsServerAsync()
        {
            var builder = WebApplication.CreateSlimBuilder();
            app = builder.Build();

            app.UseWebSockets();
            app.Use(async (context, next) => 
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                     var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var clientId = Guid.NewGuid().ToString();
                    // 【原事件触发位置】OnClientConnected?.Invoke(null, new ConnectionEventArgs(clientId, webSocket));
                    ClientConnected(clientId, webSocket);
                    await HandleWebSocketAsync(webSocket, clientId);
                    ClientDisconnected(clientId);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await next();
                }
            });

            await app.RunAsync(config.appconfig.getAddressWs());
        }

        private static async Task HandleWebSocketAsync(WebSocket webSocket, string clientId)
        {
            var buffer = new byte[4096];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;

                    try
                    {
                        result = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            CancellationToken.None
                        );
                    }
                    catch (WebSocketException ex)
                    {
                        // 👇 这里就是你现在遇到的异常
                        Console.WriteLine($"[{clientId}] WebSocket closed during receive: {ex.Message}");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived(clientId, webSocket, message);
                }
            }
            finally
            {
                // 统一关闭（不要在多个地方 Close）
                if (webSocket.State == WebSocketState.Open ||
                    webSocket.State == WebSocketState.CloseReceived)
                {
                    try
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Server closing",
                            CancellationToken.None
                        );
                    }
                    catch { }
                }

                ClientDisconnected(clientId);
            }
        }

        private static void ClientConnected(string clientId, WebSocket webSocket)
        {
            Console.WriteLine($"客户端已连接: {clientId}");
            // 在这里添加连接处理逻辑
        }

        private static void ClientDisconnected(string clientId)
        {
            Console.WriteLine($"客户端已断开: {clientId}");
            // 在这里添加断开处理逻辑
        }

        private static void MessageReceived(string clientId, WebSocket webSocket, string message)
        {
            Console.WriteLine($"收到消息 [{clientId}]: {message}");
            // 在这里添加消息处理逻辑
            // 例如：回显消息
            _ = SendString(webSocket, $"服务器收到: {message}");
        }

        public static async Task SendString(WebSocket ws, string s)
        {
            if (ws.State == WebSocketState.Open)
            {
                byte[] messageBuffer = Encoding.UTF8.GetBytes(s);
                await ws.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public delegate void Processor<T>(ref T item, in WebSocketReceiveResult result);

        public static async Task ProcessBytes(WebSocket ws, byte[] buffer, Processor<byte[]> callback)
        {
            WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            callback(ref buffer, in result);
        }
    }
}