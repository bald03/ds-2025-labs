namespace Valuator;
using StackExchange.Redis;
using RabbitMQ.Client;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Добавляем сервисы в контейнер.
        builder.Services.AddRazorPages();

        // Добавляем Redis
        builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
        
        // Добавляем RabbitMQ connection как singleton
        builder.Services.AddSingleton<IConnection>(sp => 
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };
            return factory.CreateConnection();
        });

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