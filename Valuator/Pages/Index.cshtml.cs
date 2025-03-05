using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _db; // Поле для работы с Redis

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = redis.GetDatabase(); // Инициализация базы данных Redis
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

        bool isDuplicate = CheckForDuplicates(text);

        string id = Guid.NewGuid().ToString();
        string textKey = "TEXT-" + id;
        _db.StringSet(textKey, text);

        double rank = CalculateRank(text);
        string rankKey = "RANK-" + id;
        _db.StringSet(rankKey, rank);

        int similarity = isDuplicate ? 1 : 0;
        string similarityKey = "SIMILARITY-" + id;
        _db.StringSet(similarityKey, similarity);

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
        // Получаем сервер Redis
        var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());

        // Ищем все ключи с префиксом "TEXT-*"
        var keys = server.Keys(pattern: "TEXT-*");

        // Проходим по всем найденным ключам
        foreach (var key in keys)
        {
            try
            {
                // Получаем текст по ключу
                var storedText = _db.StringGet(key);
                if (storedText == text)
                {
                    return true; // Найден дубликат
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, если что-то пошло не так
                _logger.LogError(ex, "Ошибка при получении текста из Redis по ключу {Key}", key);
                continue;
            }
        }

        // Если дубликатов не найдено, возвращаем false
        return false;
    }
}