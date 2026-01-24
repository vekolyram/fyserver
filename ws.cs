using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

namespace fyserver
{
    public class ws
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        public async Task StartWsServerAsync()
        {
            builder.Services.AddOpenApi();
            builder.WebHost.UseUrls("http://localhost" + ":" + config.appconfig.portWs);
            var app = builder.Build();
            app.UseWebSockets();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            app.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    Response(webSocket,context);
                }
                else
                {
                    await next(context);
                }
            });
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("正在启动ws服务器");
            Console.ForegroundColor = ConsoleColor.White;
            await app.RunAsync();
        }
        public static async Task SendString(WebSocket ws, string s)
        {
            byte[] messageBuffer = Encoding.UTF8.GetBytes(s);
            await ws.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public delegate void Processor<T>(ref T item, in WebSocketReceiveResult result);
        public static async Task ProcessBytes(WebSocket ws, byte[] buffer, Processor<byte[]> callback)
        {
            await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            callback(ref buffer, in result);
        }
        private static async Task Response(WebSocket webSocket,HttpContext context)
        {
            var buffer = new byte[1024*4];
            var receiveResult = await webSocket.ReceiveAsync( new ArraySegment<byte>(buffer), CancellationToken.None);
            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                    "客户端请求关闭", CancellationToken.None);
                webSocket.Dispose();
                return;
            }
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            msg msg1=new msg();
            try
            {
                msg1 = (JsonConvert.DeserializeObject<msg>(receivedMessage));
            }
            catch
            {
                Console.WriteLine("fuck");
            }
            
            config.appconfig.users.Get(context.Request.Headers["authorization"].ToString()[4..]);
                buffer = Encoding.UTF8.GetBytes("pong");
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer, 0, buffer.Length),
                receiveResult.MessageType,
                receiveResult.EndOfMessage,
                CancellationToken.None);
            //await webSocket.CloseAsync(
            //    receiveResult.CloseStatus.Value,
            //    receiveResult.CloseStatusDescription,
            //    CancellationToken.None);
        }
    }
}
