using System.Security.Cryptography;
using System.Text.Json;

namespace IntegrationTests;

internal static class ConjugationCorpusFiles
{
    public const string CandidateVersion = "candidate-20261013.1";
    public static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "JPReadingPlatform.sln"))) directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Repository corpus root not found.");
        }
    }
    public static string Feature => Path.Combine(Root, ".sdd/specs/feat-conjugation-explanation");
    public static string Packet => Path.Combine(Feature, "corpus/expanded-20261013.3");
    public static JsonDocument Read(string path) => JsonDocument.Parse(File.ReadAllText(path));
    public static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}
