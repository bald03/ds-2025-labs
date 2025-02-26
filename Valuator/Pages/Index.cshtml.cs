using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConnectionMultiplexer _redis;

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug(text);

        if (string.IsNullOrEmpty(text))
        {
            return Redirect($"summary");
        }

        string id = Guid.NewGuid().ToString();

        // Сохраняем текст в Redis
        string textKey = "TEXT-" + id;
        _redis.GetDatabase().StringSet(textKey, text);

        // Рассчитываем rank
        double rank = CalculateRank(text);
        string rankKey = "RANK-" + id;
        _redis.GetDatabase().StringSet(rankKey, rank);

        // Проверяем текст на дубликаты
        int similarity = CheckForDuplicates(text) ? 1 : 0;
        string similarityKey = "SIMILARITY-" + id;
        _redis.GetDatabase().StringSet(similarityKey, similarity);

        return Redirect($"summary?id={id}");
    }

    private static double CalculateRank(string text)
    {
        double notAlphabetCharsCount = text.Aggregate(
            0,
            (i, c) => Char.IsLetter(c) ? i : i + 1
        );

        return notAlphabetCharsCount == 0 
            ? 0 
            : Math.Round(notAlphabetCharsCount / text.Length, 2);
    }

    private bool CheckForDuplicates(string text)
    {
        var db = _redis.GetDatabase();
    
        // Получаем все сохранённые тексты из множества в Redis
        var texts = db.SetMembers("TEXTS");

        foreach (var storedText in texts)
        {
            if (string.Equals(storedText, text, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Если не найден дубликат, добавляем текст в Redis
        db.SetAdd("TEXTS", text);
        return false;
    }

}