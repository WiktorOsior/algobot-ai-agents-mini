using System.Text.Json;

namespace TrAIningRound.Agent;

// lokalna baza zadan
public class ProblemStore
{
    public const string FilePath = "data/codeforces/problemset.json";

    public List<CfProblem> Problems { get; private set; } = [];

    // tagi zadan wystepujace w bazie
    public HashSet<string> AllTags => Problems.SelectMany(p => p.Tags).ToHashSet();

    public static bool Exists() => File.Exists(FilePath);

    public void Load()
    {
        var json = File.ReadAllText(FilePath);
        Problems = JsonSerializer.Deserialize<List<CfProblem>>(json) ?? [];
    }

    public static async Task Sync(CodeforcesClient cf)
    {
        Console.WriteLine("Pobieram listę zadań z Codeforces...");
        var problems = await cf.GetAllProblems();

        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        await File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(problems));
        Console.WriteLine($"Zapisano {problems.Count} zadań do {FilePath}");
    }

    // zadania spelniajace kryteria
    public List<CfProblem> Search(int minRating, int maxRating, string[] tags, HashSet<string> excluded)
    {
        return Problems
            .Where(p => p.Rating >= minRating && p.Rating <= maxRating)
            .Where(p => p.Tags.Count > 0 && !p.Tags.Contains("*special")) // bez nietypowych zadań 
            .Where(p => tags.All(tag => p.Tags.Contains(tag)))
            .Where(p => !excluded.Contains(p.Id))
            .ToList();
    }
}