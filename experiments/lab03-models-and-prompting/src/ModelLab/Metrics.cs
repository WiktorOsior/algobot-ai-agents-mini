public sealed class Metrics
{
    public int Total { get; private set; }
    public int ValidJson { get; private set; }
    public int ModeCorrect { get; private set; }
    public int RatingCorrect { get; private set; }
    public int TopicCorrect { get; private set; }
    public int ExactMatch { get; private set; }
    public long TotalMs { get; private set; }

    // Odsetek poprawnie rozpoznanych trybów (solo / contest / null)
    public double ModeAccuracy => Ratio(ModeCorrect);

    // Odsetek poprawnie rozpoznanych zakresów trudności (oba końce zakresu)
    public double RatingAccuracy => Ratio(RatingCorrect);

    // Odsetek poprawnie rozpoznanych tematów
    public double TopicAccuracy => Ratio(TopicCorrect);

    // Odsetek wiadomości, dla których wszystkie pola są poprawne
    public double ExactMatchRate => Ratio(ExactMatch);

    public double AvgMs => Total == 0 ? 0 : (double)TotalMs / Total;

    public void Add(LabeledMessage expected, Intent? result, long elapsedMs)
    {
        Total++;
        TotalMs += elapsedMs;

        // Niepoprawny JSON liczymy jako błędną odpowiedź
        if (result is null)
            return;

        ValidJson++;

        if (result.Mode == expected.Expected.Mode)
            ModeCorrect++;

        if (IsRatingCorrect(expected, result))
            RatingCorrect++;

        if (result.Topic == expected.Expected.Topic)
            TopicCorrect++;

        if (IsExactMatch(expected, result))
            ExactMatch++;
    }

    public static bool IsExactMatch(LabeledMessage expected, Intent result) =>
        result.Mode == expected.Expected.Mode &&
        IsRatingCorrect(expected, result) &&
        result.Topic == expected.Expected.Topic;

    private static bool IsRatingCorrect(LabeledMessage expected, Intent result) =>
        result.RatingMin == expected.Expected.RatingMin &&
        result.RatingMax == expected.Expected.RatingMax;

    public override string ToString() =>
        $"Mode accuracy: {ModeAccuracy:P0}, " +
        $"Rating accuracy: {RatingAccuracy:P0}, " +
        $"Topic accuracy: {TopicAccuracy:P0}, " +
        $"Exact match: {ExactMatchRate:P0}, " +
        $"poprawny JSON: {ValidJson}/{Total}, " +
        $"średni czas: {AvgMs:F0} ms";

    private double Ratio(int value) => Total == 0 ? 0 : (double)value / Total;
}
