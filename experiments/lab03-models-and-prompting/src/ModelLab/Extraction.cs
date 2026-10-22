using System.Diagnostics;
using System.Text.Json;

// Intencja użytkownika odczytana z wiadomości na czacie
public record Intent(string? Mode, int? RatingMin, int? RatingMax, string? Topic)
{
    public override string ToString() =>
        $"mode={Mode ?? "null"}, rating={RatingMin?.ToString() ?? "null"}..{RatingMax?.ToString() ?? "null"}, " +
        $"topic={Topic ?? "null"}";
}

public static class Extraction
{
    // Schemat dla natywnego trybu structured output w Ollama
    public static readonly object Schema = new
    {
        type = "object",
        properties = new
        {
            mode = new { type = new[] { "string", "null" }, @enum = new object?[] { "solo", "contest", null } },
            rating_min = new { type = new[] { "integer", "null" } },
            rating_max = new { type = new[] { "integer", "null" } },
            topic = new { type = new[] { "string", "null" } }
        },
        required = new[] { "mode", "rating_min", "rating_max", "topic" }
    };

    public static string Prompt(string message, IEnumerable<LabeledMessage>? examples = null)
    {
        var examplesSection = examples is null
            ? ""
            : $"""

                Przykłady:
                {string.Join(Environment.NewLine, examples.Select(e => $"\"{e.Message}\" => {ToJson(e.Expected)}"))}

                """;

        return $$"""
            Jesteś agentem do treningu zadań algorytmicznych na Codeforces.
            Z wiadomości użytkownika wyodrębnij:
            - mode — "solo", gdy użytkownik chce zadanie dla siebie, "contest", gdy chce ułożyć contest, rundę, eliminacje albo trening dla grupy; null, gdy wiadomość nie dotyczy żadnego z nich,
            - rating_min i rating_max — zakres trudności; jeśli użytkownik podaje jedną liczbę, wpisz ją w oba pola; jeśli podaje rangę albo dywizję, użyj progów poniżej; jeśli podaje kilka rang lub dywizji, weź zakres obejmujący wszystkie; null, gdy nie da się ustalić konkretnej wartości,
            - topic — temat zadania jako tag Codeforces z listy poniżej; null, gdy nie podano tematu.

            Nie dopowiadaj brakujących danych.

            Dywizje:
            {{Codeforces.Divisions}}

            Rangi:
            {{Codeforces.Ranks}}

            Tagi: {{string.Join(", ", Codeforces.Tags)}}
            {{examplesSection}}
            Zwróć wyłącznie JSON:
            {
              "mode": "solo" | "contest" | null,
              "rating_min": liczba całkowita | null,
              "rating_max": liczba całkowita | null,
              "topic": tag | null
            }

            Wiadomość: "{{message}}"
            """;
    }

    // Sprawdzamy nie tylko deserializację, ale też wymagane pola i typy
    public static Intent? Parse(string response, out string? error)
    {
        try
        {
            using var json = JsonDocument.Parse(StripCodeFence(response));
            var root = json.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "To nie jest obiekt JSON";
                return null;
            }

            if (!TryGetString(root, "mode", out var mode) || mode is not (null or "solo" or "contest"))
            {
                error = "Brak pola mode lub zła wartość";
                return null;
            }

            if (!TryGetInt(root, "rating_min", out var ratingMin) || !TryGetInt(root, "rating_max", out var ratingMax))
            {
                error = "Brak pola rating_min/rating_max lub zły typ";
                return null;
            }

            if (!TryGetString(root, "topic", out var topic))
            {
                error = "Brak pola topic lub zły typ";
                return null;
            }

            error = null;

            return new Intent(mode, ratingMin, ratingMax, topic?.Trim().ToLowerInvariant());
        }
        catch (JsonException)
        {
            error = "Niepoprawny JSON";
            return null;
        }
    }

    // Uruchamia model dla wszystkich wiadomości i liczy metryki
    public static async Task<Metrics> EvaluateAsync(
        ModelClient client,
        string model,
        IReadOnlyList<LabeledMessage> messages,
        Func<string, string> buildPrompt,
        double temperature = 0,
        object? format = null)
    {
        var metrics = new Metrics();

        foreach (var message in messages)
        {
            var watch = Stopwatch.StartNew();

            var response = await client.GenerateAsync(
                model,
                buildPrompt(message.Message),
                temperature,
                format);

            watch.Stop();

            var result = Parse(response, out var error);

            metrics.Add(message, result, watch.ElapsedMilliseconds);

            Console.WriteLine(result is null
                ? $"✗ {message.Id} \"{message.Message}\" => {error}: {response.ReplaceLineEndings(" ")}"
                : $"{(Metrics.IsExactMatch(message, result) ? "✓" : "✗")} {message.Id} \"{message.Message}\" => {result} " +
                  $"(oczekiwano: {message.Expected})");
        }

        Console.WriteLine(metrics);

        return metrics;
    }

    // Część modeli otacza JSON blokiem ```json ... ``` mimo prośby o sam JSON
    public static string StripCodeFence(string response)
    {
        var text = response.Trim();

        if (!text.StartsWith("```"))
            return text;

        var start = text.IndexOf('\n') + 1;
        var end = text.LastIndexOf("```");

        return end > start ? text[start..end].Trim() : text;
    }

    private static bool TryGetString(JsonElement root, string name, out string? value)
    {
        value = null;

        if (!root.TryGetProperty(name, out var element))
            return false;

        if (element.ValueKind == JsonValueKind.Null)
            return true;

        if (element.ValueKind != JsonValueKind.String)
            return false;

        value = element.GetString();
        return true;
    }

    private static bool TryGetInt(JsonElement root, string name, out int? value)
    {
        value = null;

        if (!root.TryGetProperty(name, out var element))
            return false;

        if (element.ValueKind == JsonValueKind.Null)
            return true;

        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var number))
            return false;

        value = number;
        return true;
    }

    private static string ToJson(Intent intent) =>
        JsonSerializer.Serialize(new
        {
            mode = intent.Mode,
            rating_min = intent.RatingMin,
            rating_max = intent.RatingMax,
            topic = intent.Topic
        });
}
