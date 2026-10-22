// EXP-01: porównanie modeli w odczytywaniu intencji użytkownika
public static class ModelComparison
{
    private static readonly string[] Models =
    [
        "gemma3:4b",
        "qwen3:4b-instruct",
        "qwen2.5-coder:3b",
        "gemma4:e4b"
    ];

    public static async Task RunAsync(ModelClient client, string? onlyModel = null)
    {
        foreach (var model in onlyModel is null ? Models : [onlyModel])
        {
            Console.WriteLine($"\nModel: {model}");

            var metrics = await Extraction.EvaluateAsync(
                client,
                model,
                TestData.Messages,
                message => Extraction.Prompt(message));

            ResultsCsv.Append("EXP-01", model, "zero-shot", 0, metrics);
        }
    }
}
