using System.Diagnostics;

// EXP-03: wpływ temperatury na poprawność i powtarzalność
public static class Temperature
{
    private const int Repetitions = 3;

    private static readonly double[] Temperatures = [0.0, 0.5, 1.0];

    // Ten sam model i prompt - zmieniamy tylko temperaturę
    public static async Task RunAsync(ModelClient client, string model)
    {
        var messages = TestData.Messages;

        foreach (var temperature in Temperatures)
        {
            Console.WriteLine($"\nTemperature: {temperature}");

            var metrics = new Metrics();
            var stableMessages = 0;

            foreach (var message in messages)
            {
                var answers = new List<string>();

                for (var i = 0; i < Repetitions; i++)
                {
                    var watch = Stopwatch.StartNew();

                    var response = await client.GenerateAsync(
                        model,
                        Extraction.Prompt(message.Message),
                        temperature);

                    watch.Stop();

                    var result = Extraction.Parse(response, out var error);

                    metrics.Add(message, result, watch.ElapsedMilliseconds);
                    answers.Add(result?.ToString() ?? error!);
                }

                var distinct = answers.Distinct().Count();

                if (distinct == 1)
                    stableMessages++;

                Console.WriteLine(
                    $"{message.Id} \"{message.Message}\" => różnych odpowiedzi: {distinct}" +
                    $" (oczekiwano: {message.Expected})");

                foreach (var answer in answers.Distinct())
                    Console.WriteLine($"    {answers.Count(a => a == answer)}x {answer}");
            }

            Console.WriteLine(metrics);
            Console.WriteLine($"Powtarzalne wiadomości: {stableMessages}/{messages.Count}");

            ResultsCsv.Append("EXP-03", model, $"x{Repetitions}", temperature, metrics, stableMessages);
        }
    }
}
