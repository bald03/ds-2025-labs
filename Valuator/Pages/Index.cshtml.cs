using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Text;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _db;
    private readonly IConnection _rabbitConnection;

    public IndexModel(
        ILogger<IndexModel> logger,
        IConnectionMultiplexer redis,
        IConnection rabbitConnection)
    {
        _logger = logger;
        _db = redis.GetDatabase();
        _rabbitConnection = rabbitConnection;
    }

    public void OnGet() { }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug("Received text: {Text}", text);

        if (string.IsNullOrEmpty(text))
            return RedirectToPage("Summary");

        string id = Guid.NewGuid().ToString();
        _db.StringSet($"TEXT-{id}", text);
        _db.StringSet($"SIMILARITY-{id}", CheckForDuplicates(text) ? 1 : 0);

        SendToRabbitMQ(id);

        return RedirectToPage("Summary", new { id });
    }

    private void SendToRabbitMQ(string id)
    {
        try
        {
            using var channel = _rabbitConnection.CreateModel();
            channel.QueueDeclare(
                queue: "rank_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish(
                exchange: "",
                routingKey: "rank_queue",
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ error");
            throw;
        }
    }

    private bool CheckForDuplicates(string text)
    {
        var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
        foreach (var key in server.Keys(pattern: "TEXT-*"))
        {
            if (_db.StringGet(key) == text)
                return true;
        }
        return false;
    }
}