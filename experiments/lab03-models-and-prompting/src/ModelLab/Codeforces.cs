// Stała wiedza o poziomach na Codeforces, wpisana na sztywno do promptu.
// Model nie musi jej znać z treningu, a my wiemy dokładnie, jakich progów używa.
public static class Codeforces
{
    // Dywizje rozumiemy jako poziom trudności zadań, a nie oficjalne limity ratingu rund
    public const string Divisions = """
        - Div 1: od 1900 wzwyż (rating_max = null)
        - Div 2: 1600–1899
        - Div 3: 1400–1599
        - Div 4: 800–1399
        """;

    public const string Ranks = """
        - Newbie (szary): 800–1199
        - Pupil (zielony): 1200–1399
        - Specialist (turkusowy): 1400–1599
        - Expert (niebieski): 1600–1899
        - Candidate Master (fioletowy): 1900–2099
        - Master (pomarańczowy): 2100–2299
        - International Master (pomarańczowy): 2300–2399
        - Grandmaster (czerwony): 2400–2599
        - International Grandmaster (czerwony): 2600–2999
        - Legendary Grandmaster (czarno-czerwony): 3000–3500
        """;

    public static readonly string[] Tags =
    [
        "implementation", "math", "greedy", "dp", "data structures", "brute force",
        "constructive algorithms", "graphs", "sortings", "binary search", "dfs and similar",
        "trees", "strings", "number theory", "combinatorics", "two pointers", "bitmasks",
        "geometry", "dsu", "shortest paths", "probabilities", "games", "hashing",
        "interactive", "divide and conquer", "matrices", "flows", "graph matchings"
    ];
}
