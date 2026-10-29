using Microsoft.Extensions.AI;
using OllamaSharp;
using TrAIningRound.Agent;

var model = args.Length > 0 ? args[0] : "qwen3:4b-instruct";
IChatClient chat = new ChatClientBuilder(new OllamaApiClient(new Uri("http://localhost:11434"), model))
    .UseFunctionInvocation()
    .Build();

List<ChatMessage> history = [new(ChatRole.System, SystemPrompt.Text)];
var options = new ChatOptions { Tools = [] };  

while (Console.ReadLine() is { } input && input != "exit")
{
    history.Add(new(ChatRole.User, input));
    var response = await chat.GetResponseAsync(history, options);
    history.AddMessages(response);
    Console.WriteLine($"Agent: {response.Text}");
}