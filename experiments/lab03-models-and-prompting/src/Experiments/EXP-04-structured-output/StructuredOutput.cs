// EXP-04: odczyt intencji do JSON i walidacja
public static class StructuredOutput
{
    public static async Task RunAsync(ModelClient client, string model)
    {
        // Format JSON opisany tylko w prompcie vs natywny tryb JSON Schema w Ollama
        var variants = new (string Name, object? Format)[]
        {
            ("prompt", null),
            ("json-schema", Extraction.Schema)
        };

        foreach (var (name, format) in variants)
        {
            Console.WriteLine($"\nWariant: {name}");

            var metrics = await Extraction.EvaluateAsync(
                client,
                model,
                TestData.Messages,
                message => Extraction.Prompt(message),
                format: format);

            ResultsCsv.Append("EXP-04", model, name, 0, metrics);
        }
    }
}
