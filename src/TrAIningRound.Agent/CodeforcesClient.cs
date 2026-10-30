using System.Net.Http.Json;

namespace TrAIningRound.Agent;

public class CodeforcesClient
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri("https://codeforces.com/api/") };

    private readonly Dictionary<string, List<CfSubmission>> _submissionsCache = new();

    public async Task<CfUser?> GetUserInfo(string handle)
    {
        var response = await _http.GetAsync($"user.info?handles={Uri.EscapeDataString(handle)}");
        var body = await response.Content.ReadFromJsonAsync<CfResponse<List<CfUser>>>();

        if (body is null || body.Status != "OK" || body.Result is null)
            return null;

        return body.Result[0];
    }

    public async Task<List<CfSubmission>?> GetSubmissions(string handle)
    {
        var key = handle.ToLower();
        if (_submissionsCache.TryGetValue(key, out var cached))
            return cached;

        var response = await _http.GetAsync($"user.status?handle={Uri.EscapeDataString(handle)}");
        var body = await response.Content.ReadFromJsonAsync<CfResponse<List<CfSubmission>>>();

        if (body is null || body.Status != "OK" || body.Result is null)
            return null;

        _submissionsCache[key] = body.Result;
        return body.Result;
    }

    public async Task<HashSet<string>?> GetSolved(string handle)
    {
        var submissions = await GetSubmissions(handle);
        if (submissions is null)
            return null;

        return submissions
            .Where(s => s.Verdict == "OK")
            .Select(s => s.Problem.Id)
            .ToHashSet();
    }

    public async Task<List<CfProblem>> GetAllProblems()
    {
        var body = await _http.GetFromJsonAsync<CfResponse<CfProblemset>>("problemset.problems");

        if (body is null || body.Status != "OK" || body.Result is null)
            throw new HttpRequestException("Codeforces nie zwrócił listy zadań.");

        return body.Result.Problems;
    }
}

public record CfResponse<T>(string Status, string? Comment, T? Result);
public record CfUser(string Handle, int? Rating, int? MaxRating, string? Rank);
public record CfSubmission(CfProblem Problem, string? Verdict);
public record CfProblemset(List<CfProblem> Problems);

public record CfProblem(int? ContestId, string Index, string Name, int? Rating, List<string> Tags)
{
    public string Id => $"{ContestId}{Index}";
    public string Url => $"https://codeforces.com/problemset/problem/{ContestId}/{Index}";
}