using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Text;

Console.WriteLine("Starting RankCalculator...");

// Redis connection
using var redis = ConnectionMultiplexer.Connect("localhost");
var db = redis.GetDatabase();

// RabbitMQ connection
var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "guest",
    Password = "guest",
    AutomaticRecoveryEnabled = true
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare(
    queue: "rank_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (_, ea) =>
{
    try
    {
        var id = Encoding.UTF8.GetString(ea.Body.ToArray());
        var text = db.StringGet($"TEXT-{id}");

        if (!text.HasValue)
        {
            channel.BasicNack(ea.DeliveryTag, false, false);
            return;
        }

        double rank = CalculateRank(text!);
        db.StringSet($"RANK-{id}", rank);

        channel.BasicAck(ea.DeliveryTag, false);
        Console.WriteLine($"Processed {id}, rank: {rank}");
    }
    catch
    {
        channel.BasicNack(ea.DeliveryTag, false, true);
    }
};

channel.BasicConsume(
    queue: "rank_queue",
    autoAck: false,
    consumer: consumer);

Console.WriteLine("Waiting for messages...");
Console.ReadLine();

static double CalculateRank(string text)
{
    int nonLetters = text.Count(c => !char.IsLetter(c));
    return nonLetters == 0 ? 0 : Math.Round((double)nonLetters / text.Length, 2);
}