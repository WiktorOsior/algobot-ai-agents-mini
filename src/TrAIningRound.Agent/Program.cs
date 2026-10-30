using Microsoft.Extensions.AI;
using OllamaSharp;
using TrAIningRound.Agent;

var model = args.Length > 0 ? args[0] : "qwen3:4b-instruct";
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

IChatClient chat = new ChatClientBuilder(ollama)
    .UseFunctionInvocation()
    .Build();
List<ChatMessage> history = [new(ChatRole.System, SystemPrompt.Text)];
var cf = new CodeforcesClient();
var tools = new AgentTools(cf);
var options = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(tools.GetUser, "get_user"),
    ]
};  

while (Console.ReadLine() is { } input && input != "exit")
{
    history.Add(new(ChatRole.User, input));
    var response = await chat.GetResponseAsync(history, options);
    history.AddMessages(response);
    Console.WriteLine($"Agent: {response.Text}");
}