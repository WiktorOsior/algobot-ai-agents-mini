// EXP-02: wpływ zero-shot, one-shot i few-shot na jakość odczytu intencji
public static class Prompting
{
    public static async Task RunAsync(ModelClient client, string model)
    {
        var examples = TestData.PromptExamples;

        var variants = new (string Name, Func<string, string> Prompt)[]
        {
            ("zero-shot", message => Extraction.Prompt(message)),
            ("one-shot", message => Extraction.Prompt(message, examples.Take(1))),
            ("few-shot", message => Extraction.Prompt(message, examples))
        };

        foreach (var (name, buildPrompt) in variants)
        {
            Console.WriteLine($"\nWariant: {name}");

            var metrics = await Extraction.EvaluateAsync(
                client,
                model,
                TestData.Messages,
                buildPrompt);

            ResultsCsv.Append("EXP-02", model, name, 0, metrics);
        }
    }
}
