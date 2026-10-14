namespace Infrastructure.Conjugation;

/// <summary>Separate resource reader: never change the historical seed or migration.</summary>
public static class ConjugationCandidateData
{
    public const string Version = "candidate-20261013.1";

    public static string ReadContent()
    {
        using var stream = typeof(ConjugationCandidateData).Assembly.GetManifestResourceStream(
            "Infrastructure.Conjugation.Data.candidate-20261013.1.json")
            ?? throw new InvalidOperationException("Missing embedded conjugation candidate.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
