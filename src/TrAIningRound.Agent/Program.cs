using Microsoft.Extensions.AI;
using OllamaSharp;
using TrAIningRound.Agent;
using System.ClientModel;
using OpenAI;

var cf = new CodeforcesClient();

if (args.Length > 0 && args[0] == "sync")
{
    await ProblemStore.Sync(cf);
    return;
}

if (!ProblemStore.Exists())
{
    Console.WriteLine("Brak lokalnej bazy zadań. Najpierw uruchom: dotnet run --project src/TrAIningRound.Agent -- sync");
    return;
}

var store = new ProblemStore();
store.Load();

var useRemote = args.Contains("--remote");
var model = args.FirstOrDefault(a => !a.StartsWith("--")) ?? (useRemote ? "bielik_11b" : "qwen3:4b-instruct");

IChatClient baseClient;
if (useRemote)
{
    var apiKey = Environment.GetEnvironmentVariable("LLM_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.WriteLine("Ustaw zmienną środowiskową LLM_API_KEY z kluczem do llm.hpc.psnc.pl.");
        return;
    }

    baseClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri("https://llm.hpc.psnc.pl/v1") })
        .GetChatClient(model)
        .AsIChatClient();
}
else
{
    var ollama = new OllamaApiClient(new Uri("http://localhost:11434"), model);
    try
    {
        var models = await ollama.ListLocalModelsAsync();
        if (!models.Any(m => m.Name == model || m.Name == $"{model}:latest"))
        {
            Console.WriteLine($"Brak modelu {model}. Pobierz go: ollama pull {model}");
            return;
        }
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("Ollama nie działa. Zainstaluj ją z https://ollama.com i uruchom.");
        return;
    }
    baseClient = ollama;
}

Console.WriteLine($"Model: {model}" + (useRemote ? " (PSNC)" : " (Ollama)"));

IChatClient chat = new ChatClientBuilder(baseClient)
    .UseFunctionInvocation()
    .Build();


List<ChatMessage> history = [new(ChatRole.System, SystemPrompt.Text)];


var tools = new AgentTools(cf,store);
var options = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(tools.GetUser, "get_user"),
        AIFunctionFactory.Create(tools.SearchProblems, "search_problems"),
        AIFunctionFactory.Create(tools.GetSolvedProblems, "get_solved_problems"),
    ]
};  

while (Console.ReadLine() is { } input && input != "exit")
{
    history.Add(new(ChatRole.User, input));
    var response = await chat.GetResponseAsync(history, options);
    history.AddMessages(response);
    Console.WriteLine($"Agent: {response.Text}");
}