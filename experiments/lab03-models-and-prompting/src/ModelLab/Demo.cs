// Pojedyncze wywołanie modelu do pokazania pełnej interakcji w dokumentacji.
// Surowe zapytanie i odpowiedź zapisuje ModelClient, gdy ustawiona jest zmienna MODELLAB_TRACE.
public static class Demo
{
    public static async Task RunAsync(ModelClient client, string task, string model, string id, string? variant)
    {
        if (task == "intent")
        {
            var message = TestData.Messages.Single(m => m.Id == id);
            var prompt = variant == "few-shot"
                ? Extraction.Prompt(message.Message, TestData.PromptExamples)
                : Extraction.Prompt(message.Message);
            var format = variant == "json-schema" ? Extraction.Schema : null;

            var response = await client.GenerateAsync(model, prompt, 0, format);
            var result = Extraction.Parse(response, out var error);

            Console.WriteLine($"Odpowiedź modelu: {response}");
            Console.WriteLine(result is null
                ? $"Błąd: {error}"
                : $"Wynik: {result} | oczekiwano: {message.Expected} | " +
                  (Metrics.IsExactMatch(message, result) ? "poprawnie" : "błędnie"));
        }
        else
        {
            var data = TestData.Contests;
            var scenario = data.Scenarios.Single(s => s.Id == id);
            var (problems, justification, check) = await ContestSelection.RunOneAsync(client, model, data, scenario);

            Console.WriteLine($"Zadania: {string.Join(", ", problems)}");
            Console.WriteLine($"Ocena: {check} | {(check.Good ? "dobry" : "nie spełnia kryteriów")}");
            Console.WriteLine($"Uzasadnienie: {justification}");
        }
    }
}
