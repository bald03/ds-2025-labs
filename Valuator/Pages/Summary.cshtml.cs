using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IDatabase _db; // Поле для работы с Redis

    private const string RankPrefix = "RANK-";
    private const string SimilarityPrefix = "SIMILARITY-";

    public SummaryModel(ILogger<SummaryModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = redis.GetDatabase(); // Инициализация базы данных Redis
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);

        // Получаем rank из Redis
        string rankKey = RankPrefix + id;
        string rankValue = _db.StringGet(rankKey);
        Rank = ParseDouble(rankValue);

        // Получаем similarity из Redis
        string similarityKey = SimilarityPrefix + id;
        string similarityValue = _db.StringGet(similarityKey);
        Similarity = ParseDouble(similarityValue);
    }

    private double ParseDouble(string? num)
    {
        if (num == null)
        {
            return 0;
        }

        return double.Parse(num, System.Globalization.CultureInfo.InvariantCulture);
    }
}