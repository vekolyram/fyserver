
namespace fyserver
{
    public class http
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        async public Task StartHttpServer()
        {
            builder.Services.AddOpenApi();
            builder.WebHost.UseUrls("http://localhost" + ":" + config.appconfig.portHttp);
            var app = builder.Build();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            app.MapGet("/hello", () => "Hello named route")
               .WithName("hi");

            app.MapGet("/", (LinkGenerator linker) =>
                    $"The link to the hello route is {linker.GetPathByName("hi", values: null)}");
            app.MapGet("/users/{userId}/books/{bookId}",
    (int userId, int bookId) => $"The user id is {userId} and book id is {bookId}");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("正在启动http服务器");
            Console.ForegroundColor = ConsoleColor.White;
            await app.RunAsync();

        }
    }
}
