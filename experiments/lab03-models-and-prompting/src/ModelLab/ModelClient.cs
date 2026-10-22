using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

// Klient modeli: lokalna Ollama albo zdalny serwer zgodny z API OpenAI (np. llama.cpp, vLLM).
// Model z prefiksem "openai/" (np. "openai/gemma-4-31b:Q8_K_XL") idzie pod adres ze zmiennej
// środowiskowej OPENAI_BASE_URL (np. http://host:port/v1), klucz opcjonalnie w OPENAI_API_KEY.
// Gdy ustawiona jest zmienna MODELLAB_TRACE, każde zapytanie i surowa odpowiedź są dopisywane do tego pliku.
public sealed class ModelClient(HttpClient http)
{
    private static readonly JsonSerializerOptions TraceOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private const string OpenAiPrefix = "openai/";

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // format: null (zwykły tekst) albo obiekt JSON Schema
    public Task<string> GenerateAsync(
        string model,
        string prompt,
        double temperature = 0,
        object? format = null) =>
        model.StartsWith(OpenAiPrefix)
            ? GenerateOpenAiAsync(model[OpenAiPrefix.Length..], prompt, temperature, format)
            : GenerateOllamaAsync(model, prompt, temperature, format);

    private async Task<string> GenerateOllamaAsync(
        string model,
        string prompt,
        double temperature,
        object? format)
    {
        var request = new
        {
            model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            stream = false,
            format,
            options = new
            {
                temperature
            }
        };

        const string url = "http://localhost:11434/api/chat";

        using var response = await http.PostAsJsonAsync(url, request, Options);

        response.EnsureSuccessStatusCode();

        using var json = await response.Content
            .ReadFromJsonAsync<JsonDocument>();

        Trace(url, request, json!);

        return json!.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }

    private async Task<string> GenerateOpenAiAsync(
        string model,
        string prompt,
        double temperature,
        object? format)
    {
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL")
            ?? throw new InvalidOperationException("Ustaw OPENAI_BASE_URL, np. http://host:port/v1");

        var request = new
        {
            model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature,
            // Odpowiednik parametru format z Ollama
            response_format = format is null
                ? null
                : new { type = "json_schema", json_schema = new { name = "result", schema = format } }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = JsonContent.Create(request, options: Options)
        };

        if (Environment.GetEnvironmentVariable("OPENAI_API_KEY") is { Length: > 0 } apiKey)
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await http.SendAsync(message);

        response.EnsureSuccessStatusCode();

        using var json = await response.Content
            .ReadFromJsonAsync<JsonDocument>();

        Trace(message.RequestUri!.ToString(), request, json!);

        // Rozumowanie modeli "myślących" przychodzi w osobnym polu reasoning_content i je pomijamy
        return json!.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }

    private static void Trace(string url, object request, JsonDocument response)
    {
        if (Environment.GetEnvironmentVariable("MODELLAB_TRACE") is not { Length: > 0 } path)
            return;

        var requestJson = JsonSerializer.Serialize(
            JsonSerializer.SerializeToElement(request, Options), TraceOptions);
        var responseJson = JsonSerializer.Serialize(response.RootElement, TraceOptions);

        File.AppendAllText(path, $"POST {url}\n{requestJson}\n--- odpowiedź ---\n{responseJson}\n=====\n");
    }
}
