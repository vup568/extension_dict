using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace IntegrationTests;

public sealed class ConjugationEvidenceValidationTests
{
    [Fact]
    public async Task CandidateCanBeRecompiledExactlyFromTheApprovedPacketAndVersionedRows()
    {
        var start = new System.Diagnostics.ProcessStartInfo("node") { WorkingDirectory = ConjugationCorpusFiles.Root,
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(Path.Combine(ConjugationCorpusFiles.Feature, "scripts/compile-candidate.mjs"));
        start.ArgumentList.Add("full");
        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var errors = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        process.ExitCode.Should().Be(0, await errors);
        using var compiled = System.Text.Json.JsonDocument.Parse(await output);
        using var resource = System.Text.Json.JsonDocument.Parse(Infrastructure.Conjugation.ConjugationCandidateData.ReadContent());
        System.Text.Json.JsonElement.DeepEquals(compiled.RootElement, resource.RootElement).Should().BeTrue();
        resource.RootElement.GetProperty("paradigms").GetArrayLength().Should().Be(909);
        resource.RootElement.GetProperty("paradigms").EnumerateArray().Select(p => p.GetProperty("rule_id").GetString())
            .Distinct().Should().HaveCount(115);
        Infrastructure.Conjugation.ConjugationCandidateData.ReadContent().Should().NotContain("fixture:").And.NotContain("v5r-i");
    }

    private static JsonObject Receipt() => JsonNode.Parse(File.ReadAllText(Path.Combine(ConjugationCorpusFiles.Feature,
        "reviews/user-review-receipt-20261013.3.json")))!.AsObject();

    private static bool Valid(JsonObject receipt)
    {
        using var manifest = ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Packet, "manifest.json"));
        var m = manifest.RootElement;
        return receipt["g1_scope"]?.GetValue<string>() == "approved" &&
            receipt["g2_answers"]?.GetValue<string>() == "accepted_by_user_batch_approval" &&
            receipt["answer_verbatim"]?.GetValue<string>() == "ok tôi duyệt nhé" &&
            receipt["corpus_version"]?.GetValue<string>() == m.GetProperty("corpus_version").GetString() &&
            receipt["cases_sha256"]?.GetValue<string>() == ConjugationCorpusFiles.Hash(Path.Combine(ConjugationCorpusFiles.Packet, "cases.json")) &&
            receipt["sources_sha256"]?.GetValue<string>() == ConjugationCorpusFiles.Hash(Path.Combine(ConjugationCorpusFiles.Packet, "sources.json")) &&
            receipt["scope_material_sha256"]?.GetValue<string>() == ConjugationCorpusFiles.Hash(Path.Combine(ConjugationCorpusFiles.Packet, "scope-material.json"));
    }

    [Fact]
    public void ApprovalBindsTheActualImmutableAnswersAndSources() => Valid(Receipt()).Should().BeTrue();

    [Theory]
    [InlineData("g1_scope", "pending")]
    [InlineData("g2_answers", "pending")]
    [InlineData("cases_sha256", "wrong")]
    [InlineData("sources_sha256", "wrong")]
    [InlineData("scope_material_sha256", "wrong")]
    [InlineData("corpus_version", "other")]
    [InlineData("answer_verbatim", "")]
    public void RejectsMissingApprovalOrChangedEvidence(string field, string value)
    {
        var receipt = Receipt(); receipt[field] = value; Valid(receipt).Should().BeFalse();
    }

    [Fact]
    public void AllFrozenFilesAndPerRuleCoverageRemainBoundAndTheManifestHasNoSelfHash()
    {
        using var material = ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Packet, "scope-material.json"));
        foreach (var f in material.RootElement.GetProperty("files").EnumerateArray())
        {
            var path = Path.GetFullPath(Path.Combine(ConjugationCorpusFiles.Packet, f.GetProperty("path").GetString()!));
            path.Should().NotBe(Path.Combine(ConjugationCorpusFiles.Packet, "scope-material.json"));
            ConjugationCorpusFiles.Hash(path).Should().Be(f.GetProperty("sha256").GetString());
        }
        using var manifest = ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Packet, "manifest.json"));
        foreach (var r in manifest.RootElement.GetProperty("rules").EnumerateArray())
        {
            r.GetProperty("positive_case_ids").GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
            r.GetProperty("negative_case_ids").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        }
        using var sources = ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Packet, "sources.json"));
        sources.RootElement.GetProperty("sources").EnumerateArray().Should().OnlyContain(s =>
            s.GetProperty("source_id").GetString() != null && s.GetProperty("license_status").GetString() != "pending_review");
    }
}
