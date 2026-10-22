using System.Text;
using System.Text.Json;

public record LabeledMessage(string Id, string Message, Intent Expected);

public static class TestData
{
    // Wiadomości testowe z oczekiwanym wynikiem
    public static IReadOnlyList<LabeledMessage> Messages => Load("messages.csv");

    // Przykłady używane w promptach one-shot i few-shot (inne niż wiadomości testowe)
    public static IReadOnlyList<LabeledMessage> PromptExamples => Load("prompt-examples.csv");

    public static ContestData Contests { get; } = JsonSerializer.Deserialize<ContestData>(
        File.ReadAllText(DataPath("contest-scenarios.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    private static string DataPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "data", fileName);

    private static IReadOnlyList<LabeledMessage> Load(string fileName) =>
        File.ReadLines(DataPath(fileName))
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseLine)
            .Select(fields => new LabeledMessage(
                fields[0],
                fields[1],
                new Intent(
                    NullIfEmpty(fields[2]),
                    fields[3] == "null" ? null : int.Parse(fields[3]),
                    fields[4] == "null" ? null : int.Parse(fields[4]),
                    NullIfEmpty(fields[5]))))
            .ToList();

    private static string? NullIfEmpty(string value) => value == "null" ? null : value;

    // Prosty parser CSV: przecinek jako separator, pola w cudzysłowach mogą zawierać przecinki
    public static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                    quoted = false;
                else
                    field.Append(c);
            }
            else if (c == '"')
                quoted = true;
            else if (c == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
                field.Append(c);
        }

        fields.Add(field.ToString());

        return fields;
    }
}
