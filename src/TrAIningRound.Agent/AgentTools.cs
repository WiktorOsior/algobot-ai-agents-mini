using System.ComponentModel;

namespace TrAIningRound.Agent;
// Narzędzia, które udostępniamy modelowi. Każda metoda zwraca krótki tekst, który model przeczyta
public class AgentTools(CodeforcesClient cf)
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
}