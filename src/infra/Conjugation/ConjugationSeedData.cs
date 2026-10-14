namespace Infrastructure.Conjugation;

public static class ConjugationSeedData
{
    public const string DevelopmentVersion = "development-20261013.1";

    public static string ReadDevelopmentContent()
    {
        using var stream = typeof(ConjugationSeedData).Assembly.GetManifestResourceStream(
            "Infrastructure.Conjugation.Data.development-20261013.1.json")
            ?? throw new InvalidOperationException("Missing embedded conjugation dataset.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
