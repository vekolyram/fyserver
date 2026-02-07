using Fleck;
using System.Net.WebSockets;
using System.Text;

namespace fyserver
{
    public class ws
    {
        //        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        public async Task StartWsServerAsync()
        {
            var server = new WebSocketServer(config.appconfig.getAddressWs());
            server.RestartAfterListenError = true;
            FleckLog.Level = Fleck.LogLevel.Debug;
            // 调用 server 实例的 Start 方法启动服务器。
            // Start 方法接受一个 lambda 表达式作为参数，该表达式定义了如何处理新的 WebSocket 连接。
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("正在启动ws服务器");
            Console.ForegroundColor = ConsoleColor.White;
            server.Start(socket =>
            {
                // 当 WebSocket 连接打开时，触发 OnOpen 事件，并输出 "Open!" 到控制台。
                socket.OnOpen = () =>
                {
                };
                // 当 WebSocket 连接关闭时，触发 OnClose 事件，并输出 "Close!" 到控制台。
                socket.OnClose = () => GlobalState.users.Record2();
                // 当服务器接收到来自客户端的消息时，触发 OnMessage 事件。
                // 这个事件的处理程序接收一个参数 message，它包含了从客户端接收到的消息。
                // 然后，使用 socket.Send 方法将接收到的消息发送回客户端。
                socket.OnMessage = message => Response(socket,message);
            });
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
        public static async Task Response(IWebSocketConnection sk,string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
//        private static async Task Response(WebSocket webSocket,HttpContext context)
//        {
//            var buffer = new byte[1024*4];
//            var receiveResult = await webSocket.ReceiveAsync( new ArraySegment<byte>(buffer), CancellationToken.None);
//            if (receiveResult.MessageType == WebSocketMessageType.Close)
//            {
//                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
//                    "客户端请求关闭", CancellationToken.None);
//                webSocket.Dispose();
//                return;
//            }
//            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
//            msg msg1=new msg();
//            try
//            {
//                msg1 = (JsonConvert.DeserializeObject<msg>(receivedMessage));
//            }
//            catch
//            {
//                Console.WriteLine("fuck");
//            }

//            config.appconfig.users.Get(context.Request.Headers["authorization"].ToString()[4..]);
//                buffer = Encoding.UTF8.GetBytes("pong");
//            await webSocket.SendAsync(
//                new ArraySegment<byte>(buffer, 0, buffer.Length),
//                receiveResult.MessageType,
//                receiveResult.EndOfMessage,
//                CancellationToken.None);
//            //await webSocket.CloseAsync(
//            //    receiveResult.CloseStatus.Value,
//            //    receiveResult.CloseStatusDescription,
//            //    CancellationToken.None);
//        }
//    }
//}

