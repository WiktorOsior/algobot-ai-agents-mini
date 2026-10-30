using System.ComponentModel;

namespace TrAIningRound.Agent;
// Narzędzia, które udostępniamy modelowi. Każda metoda zwraca krótki tekst, który model przeczyta
public class AgentTools(CodeforcesClient cf, ProblemStore store)
{
    [Description("Zwraca rating, maksymalny rating i rangę użytkownika Codeforces.")]
    public async Task<string> GetUser(
        [Description("Handle użytkownika na Codeforces, np. tourist")] string handle)
    {
        Console.WriteLine($"[tool] get_user({handle})");
        if (string.IsNullOrWhiteSpace(handle))
            return "Nie podano handle'a. Zapytaj użytkownika o jego handle na Codeforces.";

        try
        {
            var user = await cf.GetUserInfo(handle);
            if (user is null)
                return $"Użytkownik {handle} nie istnieje na Codeforces.";

            return $"Handle: {user.Handle}, rating: {user.Rating?.ToString() ?? "brak"}, " +
                   $"max rating: {user.MaxRating?.ToString() ?? "brak"}, ranga: {user.Rank ?? "brak"}";
        }
        catch (HttpRequestException)
        {
            return "Codeforces jest niedostępny, spróbuj później.";
        }
    }
    [Description("Szuka zadań w lokalnej bazie zadań Codeforces. Pomija zadania już rozwiązane przez użytkownika.")]
    public async Task<string> SearchProblems(
        [Description("Minimalny rating zadania, np. 1100")] int minRating,
        [Description("Maksymalny rating zadania, np. 1300")] int maxRating,
        [Description("Tagi Codeforces po angielsku, np. dp, greedy, graphs, math, binary search, strings. Pusta lista = dowolny temat.")] string[] tags,
        [Description("Handle użytkownika podany w rozmowie. Nigdy go nie zgaduj, jeśli go nie znasz, najpierw zapytaj użytkownika.")] string handle)
    {
        Console.WriteLine($"[tool] search_problems({minRating}-{maxRating}, [{string.Join(", ", tags)}], {handle})");
        if (string.IsNullOrWhiteSpace(handle))
            return "Nie podano handle'a. Zapytaj użytkownika o jego handle na Codeforces.";

        // wyhalucynowane tagi
        var unknownTags = tags.Where(t => !store.AllTags.Contains(t)).ToList();
        if (unknownTags.Count > 0)
            return $"Nieznane tagi: {string.Join(", ", unknownTags)}. Dostępne tagi: {string.Join(", ", store.AllTags)}.";

        HashSet<string>? solved;
        try
        {
            solved = await cf.GetSolved(handle);
        }
        catch (HttpRequestException)
        {
            return "Codeforces jest niedostępny, nie mogę sprawdzić rozwiązanych zadań.";
        }

        if (solved is null)
            return $"Użytkownik {handle} nie istnieje na Codeforces.";

        var found = store.Search(minRating, maxRating, tags, solved);
        if (found.Count == 0)
            return "Brak nierozwiązanych zadań dla tych kryteriów. Poszerz zakres ratingu albo zmień tagi.";

        // losowe 5 zadan
        var picked = found.OrderBy(_ => Random.Shared.Next()).Take(5);

        return string.Join("\n", picked.Select(p =>
            $"{p.Id} „{p.Name}”, rating {p.Rating}, tagi: {string.Join(", ", p.Tags)}, link: {p.Url}"));
    }
    
    [Description("Zwraca zadania już rozwiązane przez użytkownika. Umie sortować od najnowszych albo od najtrudniejszych i szukać po nazwie.")]
    public async Task<string> GetSolvedProblems(
        [Description("Handle użytkownika podany w rozmowie. Nigdy go nie zgaduj.")] string handle,
        [Description("Tag Codeforces po angielsku, np. greedy, dp. Pusty = wszystkie tematy.")] string tag = "",
        [Description("Kolejność: 'newest' (ostatnio rozwiązane) albo 'hardest' (najwyższy rating).")] string sortBy = "newest",
        [Description("Fragment nazwy zadania, np. 'mouse trap'. Pusty = bez filtrowania po nazwie.")] string nameContains = "")
    {
        Console.WriteLine($"[tool] get_solved_problems({handle}, \"{tag}\", {sortBy}, \"{nameContains}\")");
        if (string.IsNullOrWhiteSpace(handle))
            return "Nie podano handle'a. Zapytaj użytkownika o jego handle na Codeforces.";

        List<CfSubmission>? submissions;
        try
        {
            submissions = await cf.GetSubmissions(handle);
        }
        catch (HttpRequestException)
        {
            return "Codeforces jest niedostępny, spróbuj później.";
        }

        if (submissions is null)
            return $"Użytkownik {handle} nie istnieje na Codeforces.";

        // zgloszenia od najnowszych
        var solved = submissions
            .Where(s => s.Verdict == "OK")
            .Select(s => s.Problem)
            .Where(p => tag == "" || p.Tags.Contains(tag))
            .Where(p => nameContains == "" || p.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(p => p.Id)
            .ToList();

        if (sortBy == "hardest")
            solved = solved.OrderByDescending(p => p.Rating ?? 0).ToList();

        if (solved.Count == 0)
            return $"{handle} nie ma rozwiązanych zadań spełniających te kryteria.";

        return $"Rozwiązane (spełniające kryteria): {solved.Count}. Pierwsze 10:\n" + string.Join("\n", solved.Take(10).Select(p =>
            $"{p.Id} „{p.Name}”, rating {p.Rating?.ToString() ?? "brak (np. zadanie z gyma)"}, tagi: {string.Join(", ", p.Tags)}, link: {p.Url}"));
    }
}