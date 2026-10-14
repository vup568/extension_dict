using Application.Conjugation;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Conjugation;
using Xunit;

namespace IntegrationTests;

public sealed class ConjugationBaselineGapTests(ITestOutputHelper output)
{
    [Fact]
    public void LegacyPinnedDatasetDoesNotPretendToCoverTheApprovedExpandedCorpus()
    {
        var knowledge = ConjugationKnowledge.Parse(new ConjugationDataset {
            Version = ConjugationSeedData.DevelopmentVersion, Content = ConjugationSeedData.ReadDevelopmentContent() });
        using var corpus = ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Packet, "cases.json"));
        using var observations = ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Feature,
            "evidence/expanded-sudachi-observations-20261013.3.json"));
        var observed = observations.RootElement.GetProperty("observations").EnumerateArray().ToDictionary(o => o.GetProperty("text").GetString()!);
        int supported = 0, missing = 0;
        foreach (var c in corpus.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("expected").GetProperty("outcome").GetString() == "supported"))
        {
            var text = c.GetProperty("input").GetProperty("text").GetString()!;
            var ts = observed[text].GetProperty("tokens").EnumerateArray().Select(t => knowledge.Normalize(
                t.GetProperty("surface").GetString()!, t.GetProperty("base_form").GetString()!, t.GetProperty("provider_pos").GetString()!.Split(','),
                t.GetProperty("span").GetProperty("start").GetInt32(), t.GetProperty("span").GetProperty("end").GetInt32())).ToArray();
            var result = ConjugationMatcher.Match(text, ts, knowledge.Rules, ConjugationSeedData.DevelopmentVersion);
            supported++; if (result.Explanation is null) missing++;
        }
        supported.Should().Be(303); missing.Should().BeGreaterThan(0);
        output.WriteLine($"Legacy corpus triage: {supported} supported cases; {missing} have no baseline explanation. No expected answers rewritten.");
    }
}
