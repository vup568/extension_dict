using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Dictionary.Ports;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Conjugation;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Runs the frozen YCO corpus through the real HTTP API, PostgreSQL and the pinned Sudachi sidecar
/// for the accepted dataset and the YCO candidate, so activation is decided from end-to-end evidence.
/// </summary>
public sealed class YcoCorpusEndToEndTests(YcoCorpusFixture fixture) : IClassFixture<YcoCorpusFixture>
{
    private const string InteractionId = "5f0e7c1a-3b2d-4e8f-9a6b-1c2d3e4f5a6b";
    private const string Accepted = ConjugationCandidateData.Version;
    private const string Candidate = YcoConjugationCandidateData.Version;
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CandidateIsInstalledByMigrationAsAnAdditionalImmutableVersion()
    {
        await using var context = fixture.Runtime.CreateContext();
        var repository = new EfConjugationDatasetRepository(context);
        var accepted = await repository.GetAsync(Accepted, Cancellation);
        var candidate = await repository.GetAsync(Candidate, Cancellation);
        using (var expected = JsonDocument.Parse(ConjugationCandidateData.ReadContent()))
        using (var actual = JsonDocument.Parse(accepted!.Content))
            JsonElement.DeepEquals(expected.RootElement, actual.RootElement).Should().BeTrue("the accepted dataset stays the rollback target");
        using (var expected = JsonDocument.Parse(YcoConjugationCandidateData.ReadContent()))
        using (var actual = JsonDocument.Parse(candidate!.Content))
            JsonElement.DeepEquals(expected.RootElement, actual.RootElement).Should().BeTrue();
        _ = ConjugationKnowledge.Parse(candidate);

        foreach (var sql in new[] { $"UPDATE conjugation_datasets SET content = '{{{{}}}}' WHERE version = '{Candidate}'",
            $"DELETE FROM conjugation_datasets WHERE version = '{Candidate}'" })
            await Assert.ThrowsAsync<Npgsql.PostgresException>(() => context.Database.ExecuteSqlRawAsync(sql, Cancellation));
    }

    [Theory]
    [InlineData(Accepted)]
    [InlineData(Candidate)]
    public async Task FrozenCorpusThroughAnalysisAndScanApis(string version)
    {
        await using var host = await StartAsync(version);
        using var corpus = ReadCorpus();
        var fixtures = corpus.RootElement.GetProperty("dictionary_fixtures").EnumerateArray()
            .ToDictionary(item => Field(item, "fixture_id"));
        var output = TestContext.Current.TestOutputHelper;
        var asserted = 0;

        foreach (var item in corpus.RootElement.GetProperty("cases").EnumerateArray())
        {
            var caseId = Field(item, "case_id");
            var stage = Field(item, "stage");
            var input = item.GetProperty("input");
            var text = Field(input, "text");
            var expected = item.GetProperty("expected");
            var caseFixtures = input.GetProperty("fixture_ids").EnumerateArray().Select(id => fixtures[id.GetString()!]).ToArray();
            host.Scope.Allowed = caseFixtures.Select(f => Field(f, "canonical_id")).ToHashSet();

            var policy = Field(input, "fixture_policy");
            // The baseline_sentence case freezes accepted-dataset evidence only; SPAN-01-CANDIDATE covers the new pin.
            if (policy == "baseline_sentence" && version != Accepted) continue;

            var occurrences = await AnalyzeAsync(host.Client, text);
            output?.WriteLine($"{version} {caseId}: {Describe(occurrences)}");

            // Forbidden analyses are safety constraints for every stage and dataset version.
            foreach (var rule in expected.TryGetProperty("forbidden", out var forbidden) ? forbidden.EnumerateArray() : [])
            {
                var ruleId = RuleId(Field(rule, "construction"));
                var scoped = occurrences.Where(o => !rule.TryGetProperty("span", out var span) || SameSpan(o.Span, span));
                if (rule.TryGetProperty("fixture_id", out var fixtureId))
                {
                    var form = Field(fixtures[fixtureId.GetString()!], "form");
                    scoped.SelectMany(o => o.Conjugations).Should().NotContain(c => c.RuleId == ruleId && c.BaseForm == form, $"{version} {caseId}");
                }
                else scoped.SelectMany(o => o.Conjugations).Should().NotContain(c => c.RuleId == ruleId, $"{version} {caseId}");
            }

            var applies = stage switch
            {
                "regression" => true,
                "candidate" => version == Candidate,
                _ => false // linguistic_only records evidence without forcing the tokenizer to choose a sense.
            };
            if (!applies)
            {
                if (stage == "candidate")
                    occurrences.SelectMany(o => o.Conjugations).Should().NotContain(c => c.RuleId.Contains("te-shimau", StringComparison.Ordinal),
                        $"{caseId}: the accepted pin must not expose candidate rules");
                continue;
            }

            foreach (var occurrence in expected.GetProperty("occurrences").EnumerateArray())
            {
                var actual = occurrences.Where(o => o.Surface == Field(occurrence, "surface") && SameSpan(o.Span, occurrence.GetProperty("span")))
                    .Should().ContainSingle($"{version} {caseId} occurrence {Field(occurrence, "surface")}").Which;
                actual.Matches.Should().NotBeEmpty();
                foreach (var match in occurrence.GetProperty("matches").EnumerateArray())
                {
                    var fixture = fixtures[Field(match, "fixture_id")];
                    actual.Matches.Should().Contain(Field(fixture, "canonical_id"), $"{version} {caseId}");
                    var construction = Field(match, "construction");
                    if (construction is "exact" or "lemma-only")
                    {
                        actual.Conjugations.Should().BeEmpty($"{version} {caseId} {construction}");
                        continue;
                    }
                    var explanation = actual.Conjugations.Where(c => c.RuleId == RuleId(construction) && c.BaseForm == Field(fixture, "form"))
                        .Should().ContainSingle($"{version} {caseId} {construction}").Which;
                    explanation.DatasetVersion.Should().Be(version);
                    explanation.Segments.Should().Equal(match.GetProperty("segments").EnumerateArray().Select(s =>
                        (Field(s, "surface"), s.GetProperty("span")[0].GetInt32(), s.GetProperty("span")[1].GetInt32())), $"{version} {caseId}");
                }

                // Scanning at the occurrence start must select the same occurrence from the same analysis.
                var start = occurrence.GetProperty("span")[0].GetInt32();
                using var scan = await ScanAsync(host.Client, text, start);
                scan.StatusCode.Should().Be(HttpStatusCode.OK, $"{version} {caseId} scan");
                using var scanJson = JsonDocument.Parse(await scan.Content.ReadAsStringAsync(Cancellation));
                var data = scanJson.RootElement.GetProperty("data");
                SameSpan(Span(data.GetProperty("target_span")), occurrence.GetProperty("span")).Should().BeTrue($"{version} {caseId} scan target");
                Occurrences(data.GetProperty("analysis"), text).Single(o => o.Span == actual.Span)
                    .Conjugations.Should().BeEquivalentTo(actual.Conjugations, $"{version} {caseId} scan conjugations");
            }

            foreach (var probe in expected.TryGetProperty("scan", out var scans) ? scans.EnumerateArray() : [])
            {
                using var scan = await ScanAsync(host.Client, text, probe.GetProperty("offset").GetInt32());
                ((int)scan.StatusCode).Should().Be(probe.GetProperty("status").GetInt32(), $"{version} {caseId} scan probe");
                using var json = JsonDocument.Parse(await scan.Content.ReadAsStringAsync(Cancellation));
                if (probe.TryGetProperty("target_span", out var target))
                    SameSpan(Span(json.RootElement.GetProperty("data").GetProperty("target_span")), target).Should().BeTrue();
                else
                    json.RootElement.GetProperty("error_code").GetString().Should().Be(Field(probe, "error_code"));
            }
            asserted++;
        }

        // 21 cases: 4 linguistic_only, 7 regression (one accepted-only baseline) and 10 candidate-only cases.
        asserted.Should().Be(version == Accepted ? 7 : 16);
    }

    private static async Task<IReadOnlyList<ObservedOccurrence>> AnalyzeAsync(HttpClient client, string text)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/analysis")
        { Content = JsonContent.Create(new { text, interaction_id = InteractionId }) };
        request.Headers.Add("X-Contract-Version", "3");
        using var response = await client.SendAsync(request, Cancellation);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
        return Occurrences(json.RootElement.GetProperty("data"), text);
    }

    private static Task<HttpResponseMessage> ScanAsync(HttpClient client, string text, int offset)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/scan")
        { Content = JsonContent.Create(new { text, offset, interaction_id = InteractionId }) };
        request.Headers.Add("X-Contract-Version", "1");
        return client.SendAsync(request, Cancellation);
    }

    /// <summary>A whole-word lookup has no occurrence list; it is one occurrence over the full text, as scan wraps it.</summary>
    private static IReadOnlyList<ObservedOccurrence> Occurrences(JsonElement analysis, string text)
    {
        if (analysis.TryGetProperty("vocabulary_occurrences", out var list))
            return list.EnumerateArray().Select(o => new ObservedOccurrence(Field(o, "surface"), Span(o.GetProperty("span")),
                Matches(o.GetProperty("matches")), Conjugations(o.GetProperty("conjugations")))).ToArray();
        var matches = Matches(analysis.GetProperty("dictionary_matches"));
        return matches.Count == 0 ? [] : [new(text, (0, text.Length), matches, Conjugations(analysis.GetProperty("conjugations")))];
    }

    private static IReadOnlyList<string> Matches(JsonElement matches) =>
        matches.EnumerateArray().Select(m => Field(m, "canonicalId")).ToArray();

    private static IReadOnlyList<ObservedConjugation> Conjugations(JsonElement conjugations) =>
        conjugations.EnumerateArray().Select(c => new ObservedConjugation(Field(c, "rule_id"), Field(c, "base_form"), Field(c, "dataset_version"),
            c.GetProperty("segments").EnumerateArray().Select(s =>
                (Field(s, "surface"), s.GetProperty("span").GetProperty("start").GetInt32(), s.GetProperty("span").GetProperty("end").GetInt32())).ToArray())).ToArray();

    private static (int Start, int End) Span(JsonElement span) =>
        (span.GetProperty("start").GetInt32(), span.GetProperty("end").GetInt32());

    private static bool SameSpan((int Start, int End) actual, JsonElement expected) =>
        actual == (expected[0].GetInt32(), expected[1].GetInt32());

    private static string RuleId(string construction) => construction switch
    {
        "verb-past" => "conjugation:verb-past",
        "verb-te" => "conjugation:verb-te",
        "te-iru-nonpast" => "conjugation:verb-te-iru",
        "te-iru-past" => "conjugation:verb-te-iru:plain-past",
        "te-iru-negative" => "conjugation:verb-te-iru:plain-negative",
        "te-shimau-nonpast" => "conjugation:verb-te-shimau",
        "te-shimau-past" => "conjugation:verb-te-shimau:plain-past",
        _ => throw new InvalidOperationException($"Unsupported corpus construction: {construction}")
    };

    private static string Describe(IReadOnlyList<ObservedOccurrence> occurrences) => occurrences.Count == 0 ? "(none)" :
        string.Join("; ", occurrences.Select(o => $"{o.Surface}[{o.Span.Start},{o.Span.End}] {string.Join(",", o.Matches)} " +
            (o.Conjugations.Count == 0 ? "lemma/exact" : string.Join(",", o.Conjugations.Select(c => c.RuleId)))));

    private static JsonDocument ReadCorpus() => ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Root,
        ".sdd/specs/005-yomitan-reading-experience/corpus/yco-20261110.1/cases.json"));

    private static string Field(JsonElement item, string property) => item.GetProperty(property).GetString()!;

    private async Task<Host> StartAsync(string version)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ConnectionStrings:DefaultConnection"] = fixture.Runtime.ConnectionString;
        builder.Configuration["GrpcTokenizerAddress"] = fixture.Runtime.Address;
        builder.Configuration["ConjugationDatasetVersion"] = version;
        WebApi.ApiComposition.RegisterServices(builder);
        // Each case sees only its frozen dictionary fixtures; SQL lookup itself stays real.
        var scope = new CaseScope();
        builder.Services.AddScoped<IDictionaryRepository>(sp =>
            new CorpusRepository(new EfDictionaryRepository(sp.GetRequiredService<AppDbContext>()), scope.Allowed));
        var app = builder.Build();
        WebApi.ApiComposition.MapEndpoints(app);
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync(Cancellation);
        return new(app, new HttpClient { BaseAddress = new(app.Urls.Single()) }, scope);
    }

    private sealed class CaseScope { public IReadOnlySet<string> Allowed { get; set; } = new HashSet<string>(); }

    private sealed record Host(WebApplication App, HttpClient Client, CaseScope Scope) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.StopAsync(Cancellation);
            await App.DisposeAsync();
        }
    }

    private sealed record ObservedOccurrence(string Surface, (int Start, int End) Span,
        IReadOnlyList<string> Matches, IReadOnlyList<ObservedConjugation> Conjugations);

    private sealed record ObservedConjugation(string RuleId, string BaseForm, string DatasetVersion,
        IReadOnlyList<(string Surface, int Start, int End)> Segments);
}

/// <summary>Real PostgreSQL + Sudachi runtime seeded with the frozen YCO JMdict fixtures.</summary>
public sealed class YcoCorpusFixture : IAsyncLifetime
{
    public TokenizerDeinflectionFixture Runtime { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await Runtime.InitializeAsync();
        try
        {
            using var corpus = ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Root,
                ".sdd/specs/005-yomitan-reading-experience/corpus/yco-20261110.1/cases.json"));
            await using var context = Runtime.CreateContext();
            foreach (var group in corpus.RootElement.GetProperty("dictionary_fixtures").EnumerateArray()
                .GroupBy(f => f.GetProperty("canonical_id").GetString()!))
            {
                var entry = new DictionaryEntry { CanonicalId = group.Key };
                var ordinal = 0;
                foreach (var fixture in group)
                {
                    ordinal++;
                    var restricted = fixture.TryGetProperty("reading_restricted_to", out var restrictions);
                    var form = new WrittenForm { Form = fixture.GetProperty("form").GetString()! };
                    var reading = new Reading { ReadingText = fixture.GetProperty("reading").GetString()!,
                        RestrictedToForms = restricted ? restrictions.EnumerateArray().Select(r => r.GetString()!).ToArray() : null };
                    var position = fixture.TryGetProperty("sense_ordinal", out var p) ? p.GetInt16() : (short)ordinal;
                    var sense = new DictionarySense { SenseKey = position.ToString(System.Globalization.CultureInfo.InvariantCulture), Position = position,
                        PartOfSpeech = fixture.GetProperty("pos").EnumerateArray().Select(x => x.GetString()!).ToArray(),
                        Glosses = [new() { LanguageTag = "vi", GlossText = "Mục từ kiểm thử YCO.", Position = 1, ReviewStatus = "demo" }] };
                    // Restricted pairs bind each sense to its own written form and reading.
                    if (restricted) sense.Applicabilities = [new() { WrittenForm = form, ReadingEntity = reading }];
                    entry.WrittenForms.Add(form);
                    entry.Readings.Add(reading);
                    entry.Senses.Add(sense);
                }
                context.DictionaryEntries.Add(entry);
            }
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        catch { await Runtime.DisposeAsync(); throw; }
    }

    public ValueTask DisposeAsync() => Runtime.DisposeAsync();
}
