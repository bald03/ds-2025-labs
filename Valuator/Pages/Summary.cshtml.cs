using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IDatabase _db;

    private const string RankPrefix = "RANK-";
    private const string SimilarityPrefix = "SIMILARITY-";

    public SummaryModel(ILogger<SummaryModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = redis.GetDatabase();
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);
        
        if (!_db.KeyExists($"{RankPrefix}{id}"))
        {
            ViewData["StatusMessage"] = "Оценка содержания не завершена";
            Rank = 0;
        }
        else
        {
            string rankKey = RankPrefix + id;
            string rankValue = _db.StringGet(rankKey);
            Rank = ParseDouble(rankValue);
        }
        
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