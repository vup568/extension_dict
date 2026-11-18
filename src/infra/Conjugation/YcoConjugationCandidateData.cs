namespace Infrastructure.Conjugation;

/// <summary>
/// Reviewed immutable snapshot that extends candidate-20261013.1 with the YCO rules.
/// The file is compiled by the corpus script and must never be edited after its migration ships.
/// </summary>
public static class YcoConjugationCandidateData
{
    public const string Version = "candidate-20261110.1";

    public static string ReadContent()
    {
        using var stream = typeof(YcoConjugationCandidateData).Assembly.GetManifestResourceStream(
            "Infrastructure.Conjugation.Data.candidate-20261110.1.json")
            ?? throw new InvalidOperationException("Missing embedded YCO conjugation candidate.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
