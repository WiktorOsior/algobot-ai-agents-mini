using System.Diagnostics;
using System.Text.Json;

public record PoolProblem(string Id, string Name, int Rating, string[] Tags);

public record Participant(string Handle, int Rating, string[] Solved, bool Real);

public record ContestScenario(string Id, string Description, Participant[] Participants);

public record ContestData(int ContestSize, PoolProblem[] Pool, ContestScenario[] Scenarios);

// Wynik sprawdzenia jednego contestu ułożonego przez model
public record ContestCheck(
    bool Valid, bool Unsolved, bool Start, bool End, bool Spread, bool Topics, bool Justified)
{
    public bool Good => Valid && Unsolved && Start && End && Spread && Topics && Justified;

    public override string ToString() =>
        $"poprawny={Mark(Valid)} nierozwiązane={Mark(Unsolved)} start={Mark(Start)} koniec={Mark(End)} " +
        $"rozrzut={Mark(Spread)} tematy={Mark(Topics)} uzasadnienie={Mark(Justified)}";

    private static string Mark(bool value) => value ? "✓" : "✗";
}

// EXP-05: czy model potrafi ułożyć dobry contest dla grupy i go uzasadnić
public static class ContestSelection
{
    public static readonly string[] DefaultModels = ["gemma3:4b", "qwen3:4b-instruct", "qwen2.5-coder:3b", "gemma4:e4b"];

    // Contest zaczyna się trochę poniżej najsłabszego uczestnika i kończy w okolicy najmocniejszego:
    // najłatwiejsze zadanie od (najsłabszy - 500) do najsłabszego,
    // najtrudniejsze od (najmocniejszy - 100) do (najmocniejszy + 300)
    private const int MaxBelowWeakest = 500;
    private const int MaxBelowStrongest = 100;
    private const int MaxAboveStrongest = 300;
    private const int MinSpread = 200;
    private const int MinJustificationLength = 40;

    private const string Header =
        "model,scenario,valid,unsolved,start,end,spread,topics,justified,good,ms,problems,justification";

    private static string FilePath { get; } =
        Path.Combine(Directory.GetCurrentDirectory(), "results", "contest-results.csv");

    private static readonly object Schema = new
    {
        type = "object",
        properties = new
        {
            problems = new { type = "array", items = new { type = "string" } },
            justification = new { type = "string" }
        },
        required = new[] { "problems", "justification" }
    };

    public static async Task RunAsync(ModelClient client, IEnumerable<string> models)
    {
        var data = TestData.Contests;

        foreach (var model in models)
        {
            Console.WriteLine($"\nModel: {model}");
            var good = 0;

            foreach (var scenario in data.Scenarios)
            {
                var watch = Stopwatch.StartNew();
                var response = await client.GenerateAsync(model, Prompt(data, scenario), 0, Schema);
                watch.Stop();

                var (problems, justification) = Parse(response);
                var check = Check(data, scenario, problems, justification);

                if (check.Good)
                    good++;

                Console.WriteLine($"{(check.Good ? "✓" : "✗")} {scenario.Id} ({scenario.Description})");
                Console.WriteLine($"    zadania: {string.Join(", ", problems.Select(id => Describe(data, id)))}");
                Console.WriteLine($"    {check}");
                Console.WriteLine($"    uzasadnienie: {justification}");

                ResultsCsv.AppendLine(FilePath, Header, string.Join(",",
                    model, scenario.Id, check.Valid, check.Unsolved, check.Start, check.End,
                    check.Spread, check.Topics, check.Justified, check.Good, watch.ElapsedMilliseconds,
                    string.Join(" ", problems), Quote(justification)));
            }

            Console.WriteLine($"Dobre contesty: {good}/{data.Scenarios.Length}");
        }
    }

    // Ocenia ponownie zapisane odpowiedzi modeli (np. po zmianie kryteriów), bez pytania modeli od nowa
    public static void Rescore()
    {
        var data = TestData.Contests;
        var scenarios = data.Scenarios.ToDictionary(s => s.Id);
        var lines = File.ReadLines(FilePath).ToList();
        var header = TestData.ParseLine(lines[0]);
        var rows = lines.Skip(1)
            .Select(TestData.ParseLine)
            .Select(fields => header.Zip(fields).ToDictionary(pair => pair.First, pair => pair.Second))
            .ToList();

        File.Delete(FilePath);

        foreach (var model in rows.Select(r => r["model"]).Distinct())
        {
            Console.WriteLine($"\nModel: {model}");
            var good = 0;

            foreach (var row in rows.Where(r => r["model"] == model))
            {
                var (scenario, ms, justification) = (scenarios[row["scenario"]], row["ms"], row["justification"]);
                var problems = row["problems"].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var check = Check(data, scenario, problems, justification);

                if (check.Good)
                    good++;

                Console.WriteLine($"{(check.Good ? "✓" : "✗")} {scenario.Id} " +
                    $"{string.Join(", ", problems.Select(id => Describe(data, id)))}  {check}");

                ResultsCsv.AppendLine(FilePath, Header, string.Join(",",
                    model, scenario.Id, check.Valid, check.Unsolved, check.Start, check.End,
                    check.Spread, check.Topics, check.Justified, check.Good, ms,
                    string.Join(" ", problems), Quote(justification)));
            }

            Console.WriteLine($"Dobre contesty: {good}/{rows.Count(r => r["model"] == model)}");
        }
    }

    public static async Task<(string[] Problems, string Justification, ContestCheck Check)> RunOneAsync(
        ModelClient client, string model, ContestData data, ContestScenario scenario)
    {
        var response = await client.GenerateAsync(model, Prompt(data, scenario), 0, Schema);
        var (problems, justification) = Parse(response);

        return (problems, justification, Check(data, scenario, problems, justification));
    }

    public static string Prompt(ContestData data, ContestScenario scenario)
    {
        var participants = string.Join(Environment.NewLine, scenario.Participants.Select(p =>
            $"- {p.Handle}: rating {p.Rating}, rozwiązane zadania z puli: " +
            (p.Solved.Length == 0 ? "brak" : string.Join(", ", p.Solved))));

        var pool = string.Join(Environment.NewLine, data.Pool.Select(p =>
            $"- {p.Id} | rating {p.Rating} | {string.Join(", ", p.Tags)} | {p.Name}"));

        return $$"""
            Jesteś agentem, który układa contesty treningowe z zadań Codeforces dla grupy zawodników.
            Wybierz dokładnie {{data.ContestSize}} zadania z puli tak, żeby contest był uczciwy:
            - żaden uczestnik nie rozwiązał wcześniej żadnego z wybranych zadań,
            - trudność pasuje do poziomu grupy: najłatwiejsze zadanie da się zrobić dla najsłabszego uczestnika, a najtrudniejsze jest wyzwaniem dla najmocniejszego,
            - zadania są ułożone od najłatwiejszego do najtrudniejszego,
            - tematy są możliwie różne.
            Uzasadnij wybór w 2–4 zdaniach.

            Uczestnicy:
            {{participants}}

            Pula zadań (id | rating | tagi | nazwa):
            {{pool}}

            Zwróć wyłącznie JSON:
            {
              "problems": ["id", ...],
              "justification": "uzasadnienie"
            }
            """;
    }

    private static (string[] Problems, string Justification) Parse(string response)
    {
        try
        {
            using var json = JsonDocument.Parse(Extraction.StripCodeFence(response));
            var root = json.RootElement;

            var problems = root.TryGetProperty("problems", out var array) && array.ValueKind == JsonValueKind.Array
                ? array.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!.Trim())
                    .ToArray()
                : [];

            var justification = root.TryGetProperty("justification", out var text) && text.ValueKind == JsonValueKind.String
                ? text.GetString()!.Trim()
                : "";

            return (problems, justification);
        }
        catch (JsonException)
        {
            return ([], "");
        }
    }

    public static ContestCheck Check(ContestData data, ContestScenario scenario, string[] ids, string justification)
    {
        var pool = data.Pool.ToDictionary(p => p.Id);

        var valid = ids.Length == data.ContestSize &&
                    ids.Distinct().Count() == ids.Length &&
                    ids.All(pool.ContainsKey);

        if (!valid)
            return new ContestCheck(false, false, false, false, false, false, justification.Length >= MinJustificationLength);

        var problems = ids.Select(id => pool[id]).ToList();
        var solved = scenario.Participants.SelectMany(p => p.Solved).ToHashSet();
        var weakest = scenario.Participants.Min(p => p.Rating);
        var strongest = scenario.Participants.Max(p => p.Rating);
        var easiest = problems.Min(p => p.Rating);
        var hardest = problems.Max(p => p.Rating);

        // Gdy w puli nie ma nierozwiązanego zadania łatwiejszego od najsłabszego (albo trudniejszego
        // od najmocniejszego) uczestnika, contest zaczyna się (kończy) na najbliższym dostępnym
        var available = data.Pool.Where(p => !solved.Contains(p.Id)).ToList();
        var startTarget = Math.Max(weakest, available.Min(p => p.Rating));
        var endTarget = Math.Min(strongest - MaxBelowStrongest, available.Max(p => p.Rating));

        return new ContestCheck(
            Valid: true,
            Unsolved: problems.All(p => !solved.Contains(p.Id)),
            Start: easiest >= weakest - MaxBelowWeakest && easiest <= startTarget,
            End: hardest >= endTarget && hardest <= strongest + MaxAboveStrongest,
            // Kolejność nie ma znaczenia (zadania i tak można posortować), liczy się rozrzut trudności
            Spread: hardest - easiest >= MinSpread,
            // Każde zadanie wnosi co najmniej jeden temat, którego nie mają pozostałe
            Topics: problems.All(p => p.Tags.Except(problems.Where(q => q != p).SelectMany(q => q.Tags)).Any()),
            Justified: justification.Length >= MinJustificationLength);
    }

    private static string Describe(ContestData data, string id) =>
        data.Pool.FirstOrDefault(p => p.Id == id) is { } p ? $"{id} ({p.Rating})" : $"{id} (spoza puli)";

    private static string Quote(string value) => $"\"{value.Replace("\"", "\"\"").ReplaceLineEndings(" ")}\"";
}
