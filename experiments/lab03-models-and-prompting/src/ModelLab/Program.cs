using var http = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(5)
};

var client = new ModelClient(http);

var experiment = args.FirstOrDefault();

// Drugi argument (opcjonalny) to model; w EXP-01 i EXP-05 uruchamia tylko ten jeden model
var model = args.Length > 1 ? args[1] : "gemma3:4b";

switch (experiment)
{
    case "EXP-01":
        await ModelComparison.RunAsync(client, args.Length > 1 ? model : null);
        break;

    case "EXP-02":
        await Prompting.RunAsync(client, model);
        break;

    case "EXP-03":
        await Temperature.RunAsync(client, model);
        break;

    case "EXP-04":
        await StructuredOutput.RunAsync(client, model);
        break;

    case "EXP-05":
        await ContestSelection.RunAsync(client, args.Length > 1 ? [model] : ContestSelection.DefaultModels);
        break;

    // Jedno wywołanie modelu tym samym kodem co w eksperymentach, np.
    // DEMO intent qwen2.5-coder:3b MSG-009 [few-shot|json-schema]  albo  DEMO contest gemma4:e4b SC-02
    case "DEMO":
        await Demo.RunAsync(client, args[1], args[2], args[3], args.ElementAtOrDefault(4));
        return;

    // Wypisuje prompt EXP-05 dla wskazanego scenariusza, np. EXP-05-PROMPT SC-01
    case "EXP-05-PROMPT":
        Console.WriteLine(ContestSelection.Prompt(
            TestData.Contests, TestData.Contests.Scenarios.Single(s => s.Id == args[1])));
        return;

    case "EXP-05-RESCORE":
        ContestSelection.Rescore();
        break;

    default:
        Console.WriteLine("Wybierz EXP-01 ... EXP-05 [model]");
        return;
}

Console.WriteLine($"\nWyniki zapisano w {Path.GetDirectoryName(ResultsCsv.FilePath)}");
