using System.Globalization;

public static class ResultsCsv
{
    private const string Header =
        "experiment,model,variant,temperature,total,valid_json,mode_accuracy,rating_accuracy,topic_accuracy,exact_match,avg_ms,stable";

    public static string FilePath { get; } =
        Path.Combine(Directory.GetCurrentDirectory(), "results", "results.csv");

    public static void Append(
        string experiment,
        string model,
        string variant,
        double temperature,
        Metrics metrics,
        int? stable = null)
    {
        AppendLine(FilePath, Header, string.Join(",",
            experiment,
            model,
            variant,
            Format(temperature),
            metrics.Total,
            metrics.ValidJson,
            Format(Math.Round(metrics.ModeAccuracy, 2)),
            Format(Math.Round(metrics.RatingAccuracy, 2)),
            Format(Math.Round(metrics.TopicAccuracy, 2)),
            Format(Math.Round(metrics.ExactMatchRate, 2)),
            Format(Math.Round(metrics.AvgMs)),
            stable?.ToString() ?? ""));
    }

    public static void AppendLine(string path, string header, string line)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (!File.Exists(path))
            File.WriteAllText(path, header + Environment.NewLine);

        File.AppendAllText(path, line + Environment.NewLine);
    }

    public static string Format(double value) =>
        value.ToString(CultureInfo.InvariantCulture);
}
