namespace Valuator;
using StackExchange.Redis;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Добавляем сервисы в контейнер.
        builder.Services.AddRazorPages();

        // Добавляем Redis
        builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
        
        // var redis = ConnectionMultiplexer.Connect("localhost:6379");
        // var db = redis.GetDatabase();
        // db.StringSet("test-key", "Hello, Redis!");
        // string value = db.StringGet("test-key");
        // Console.WriteLine($"Value from Redis: {value}");

        var app = builder.Build();

        // Настраиваем конвейер HTTP-запросов.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}