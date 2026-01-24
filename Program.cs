using fyserver;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
new http().StartHttpServer();
 new ws().StartWsServerAsync();
ClientWebSocket webSocket = new ClientWebSocket();
await webSocket.ConnectAsync(new Uri("ws://localhost:5232/"), CancellationToken.None);
string message = JsonConvert.SerializeObject(new msg() { message = "", channel = "ping" });
for (int i =0;i<100;i++)
ws.SendString(webSocket, message);
var buffer = new byte[1024];
List<byte> bs = new List<byte>();
WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
//文本消息
    bs.AddRange(buffer.Take(result.Count));
    //消息是否已接收完全
    if (result.EndOfMessage)
{
        //发送过来的消息
        string userMsg = Encoding.UTF8.GetString(bs.ToArray(), 0, bs.Count);
        Console.WriteLine(userMsg);
        //清空消息容器
        bs = new List<byte>();
    }
await webSocket.CloseAsync(WebSocketCloseStatus.Empty,"",CancellationToken.None);
webSocket.Dispose();
Console.ReadLine();