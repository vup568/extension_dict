using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Dictionary;
using Application.Dictionary.Models;
using Application.Dictionary.Ports;
using Domain.Entities;
using FluentAssertions;
using Grpc.Net.Client;
using Infrastructure.Adapters;
using Infrastructure.Conjugation;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntegrationTests;

public sealed class ConjugationCorpusIntegrationTests(F09CorpusFixture fixture) : IClassFixture<F09CorpusFixture>
{
    [Fact]
    public async Task ApprovedCorpusMatchesRealSudachiAndCanonicalPostgreSqlFixtures()
    {
        using var corpus = ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Packet, "cases.json"));
        using var channel = GrpcChannel.ForAddress(fixture.Runtime.Address);
        int exercised = 0;
        foreach (var c in corpus.RootElement.GetProperty("cases").EnumerateArray().Where(c => !c.TryGetProperty("scenario", out _)))
        {
            await using var context = fixture.Runtime.CreateContext();
            var ids = c.GetProperty("dictionary_fixture_ids").EnumerateArray().Select(x => "fixture:word:" + x.GetString()!.Split(':')[1]).ToHashSet();
            var repo = new CorpusRepository(new EfDictionaryRepository(context), ids);
            var adapter = new GrpcDictionaryTokenizerAdapter(new(channel), new EfConjugationDatasetRepository(context), ConjugationCorpusFiles.CandidateVersion);
            var text = c.GetProperty("input").GetProperty("text").GetString()!;
            var result = await new LookupWordUseCase(repo, adapter).ExecuteAsync(text, TestContext.Current.CancellationToken);
            var expected = c.GetProperty("expected");
            var actual = result.Alternatives ?? (result.Conjugation?.Detail is { } detail ? [detail] : []);
            var cid = c.GetProperty("case_id").GetString();
            actual.Select(a => a.RuleId).Order().Should().Equal(expected.GetProperty("alternatives").EnumerateArray()
                .Select(a => a.GetProperty("rule_id").GetString()).Order(), $"approved case {cid}");
            result.Capabilities.Conjugation.Should().Be(expected.GetProperty("capability").GetString(), $"case {cid}");
            if (expected.GetProperty("outcome").GetString() == "exact_first")
            {
                result.Matches.Should().NotBeEmpty($"exact match for {cid}");
                result.Matches.Should().OnlyContain(m => m.MatchedWrittenForm == text || m.MatchedReading == text);
                result.Conjugation.Should().BeNull();
            }
            foreach (var a in actual)
            {
                var e = expected.GetProperty("alternatives").EnumerateArray().Single(e => e.GetProperty("rule_id").GetString() == a.RuleId);
                a.BaseForm.Should().Be(e.GetProperty("base_form").GetString(), $"case {cid}");
                a.RuleVersion.Should().Be(e.GetProperty("rule_version").GetInt32());
                a.DatasetVersion.Should().Be(ConjugationCorpusFiles.CandidateVersion);
                a.LanguageTag.Should().Be("vi");
                a.Segments.Select(s => s.Surface).Should().Equal(e.GetProperty("segments").EnumerateArray().Select(s => s.GetProperty("surface").GetString()), $"case {cid}");
                // Equivalent duplicate editorial cases can use different accepted VI phrasings.
                var realizations = corpus.RootElement.GetProperty("cases").EnumerateArray().Where(x => x.GetProperty("input").GetProperty("text").GetString() == text)
                    .SelectMany(x => x.GetProperty("expected").GetProperty("alternatives").EnumerateArray()).Where(x =>
                        x.GetProperty("rule_id").GetString() == a.RuleId && x.GetProperty("base_form").GetString() == a.BaseForm).ToArray();
                realizations.Should().Contain(x => x.GetProperty("explanation_vi").GetString() == a.Explanation &&
                    x.GetProperty("segments").GetArrayLength() == a.Segments.Count &&
                    x.GetProperty("segments").EnumerateArray().Zip(a.Segments).All(pair =>
                        pair.First.GetProperty("surface").GetString() == pair.Second.Surface &&
                        pair.First.GetProperty("explanation_vi").GetString() == pair.Second.Explanation &&
                        pair.First.GetProperty("span").GetProperty("start").GetInt32() == pair.Second.Span.Start &&
                        pair.First.GetProperty("span").GetProperty("end").GetInt32() == pair.Second.Span.End && pair.Second.Span.Unit == "utf16"),
                    $"complete independently reviewed VI/components/spans for {cid}");
                a.Span.Start.Should().Be(expected.GetProperty("span").GetProperty("start").GetInt32());
                a.Span.End.Should().Be(expected.GetProperty("span").GetProperty("end").GetInt32());
                string.Concat(a.Segments.Select(s => s.Surface)).Should().Be(text);
                result.Matches.Should().Contain(m => m.MatchedWrittenForm == a.BaseForm || m.MatchedReading == a.BaseForm);
            }
            exercised++;
        }
        exercised.Should().Be(404); // 412 total minus eight explicit failure/async scenarios.
    }

    [Theory]
    [InlineData("食べられる", 3)]
    [InlineData("食べさせられる", 2)]
    [InlineData("食べました", 1)]
    public async Task AnonymousContractsV1V2V3PreserveCardinalityAndDictionaryIdentity(string text, int count)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ConnectionStrings:DefaultConnection"] = fixture.Runtime.ConnectionString;
        builder.Configuration["GrpcTokenizerAddress"] = fixture.Runtime.Address;
        builder.Configuration["ConjugationDatasetVersion"] = ConjugationCorpusFiles.CandidateVersion;
        WebApi.ApiComposition.RegisterServices(builder);
        await using var app = builder.Build(); WebApi.ApiComposition.MapEndpoints(app);
        app.Urls.Add("http://127.0.0.1:0"); await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = new HttpClient { BaseAddress = new(app.Urls.Single()) };
        foreach (var version in new[] { "1", "2", "3" })
        {
            client.DefaultRequestHeaders.Remove("X-Contract-Version"); client.DefaultRequestHeaders.Add("X-Contract-Version", version);
            var id = Guid.NewGuid().ToString();
            var response = await client.PostAsJsonAsync("/api/analysis", new { text, interaction_id = id }, TestContext.Current.CancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(body);
            doc.RootElement.GetProperty("meta").GetProperty("interaction_id").GetString().Should().Be(id);
            var data = doc.RootElement.GetProperty("data");
            int expectedCount = version == "1" || (version == "2" && count > 1) ? 0 : count;
            data.GetProperty("conjugations").GetArrayLength().Should().Be(expectedCount);
            data.GetProperty("capabilities").GetProperty("conjugation").GetString().Should().Be(expectedCount > 0 ? "completed" : "unavailable");
            var start = new System.Diagnostics.ProcessStartInfo("node") { RedirectStandardInput = true, RedirectStandardOutput = true,
                RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add("--experimental-transform-types");
            start.ArgumentList.Add(Path.Combine(ConjugationCorpusFiles.Feature, "scripts/validate-client-response.mjs"));
            start.ArgumentList.Add(id);
            using var process = System.Diagnostics.Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            var errors = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            await process.StandardInput.WriteAsync(body); process.StandardInput.Close();
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            process.ExitCode.Should().Be(0, await errors);
            using var clientValidated = JsonDocument.Parse(await output);
            clientValidated.RootElement.GetProperty("occurrences").GetInt32().Should().Be(expectedCount);
            if (version == "3") data.GetProperty("conjugations").EnumerateArray().Should().OnlyContain(c =>
                c.GetProperty("base_form").GetString() == "食べる" && c.GetProperty("language_tag").GetString() == "vi");
        }
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CandidateIsAdditiveAndOldKnowledgeRemainsUnchanged()
    {
        await using var context = fixture.Runtime.CreateContext();
        var repo = new EfConjugationDatasetRepository(context);
        var old = await repo.GetAsync(ConjugationSeedData.DevelopmentVersion, TestContext.Current.CancellationToken);
        using var expectedOld = JsonDocument.Parse(ConjugationSeedData.ReadDevelopmentContent());
        using var actualOld = JsonDocument.Parse(old!.Content);
        JsonElement.DeepEquals(expectedOld.RootElement, actualOld.RootElement).Should().BeTrue();
        var candidate = await repo.GetAsync(ConjugationCorpusFiles.CandidateVersion, TestContext.Current.CancellationToken);
        using var expected = ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Root, "src/infra/Conjugation/Data/candidate-20261013.1.json"));
        using var actual = JsonDocument.Parse(candidate!.Content);
        JsonElement.DeepEquals(expected.RootElement, actual.RootElement).Should().BeTrue();
        foreach (var sql in new[] { "UPDATE conjugation_datasets SET content = '{{}}' WHERE version = 'candidate-20261013.1'",
            "DELETE FROM conjugation_datasets WHERE version = 'candidate-20261013.1'", "TRUNCATE conjugation_datasets" })
            await Assert.ThrowsAsync<Npgsql.PostgresException>(() => context.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken));
        var logs = await fixture.Runtime.Tokenizer.GetLogsAsync(ct: TestContext.Current.CancellationToken);
        logs.Stdout.Should().NotContain("食べさせられる"); logs.Stderr.Should().NotContain("食べられる");
    }

    [Theory]
    [InlineData("買う", "v5u", "買いました", "買って", "買える")]
    [InlineData("歩く", "v5k", "歩きました", "歩いて", "歩ける")]
    [InlineData("泳ぐ", "v5g", "泳ぎました", "泳いで", "泳げる")]
    [InlineData("話す", "v5s", "話しました", "話して", "話せる")]
    [InlineData("待つ", "v5t", "待ちました", "待って", "待てる")]
    [InlineData("死ぬ", "v5n", "死にました", "死んで", "死ねる")]
    [InlineData("遊ぶ", "v5b", "遊びました", "遊んで", "遊べる")]
    [InlineData("休む", "v5m", "休みました", "休んで", "休める")]
    [InlineData("帰る", "v5r", "帰りました", "帰って", "帰れる")]
    [InlineData("見る", "v1", "見ました", "見て", "見られる")]
    public async Task GeneralizesToUnlistedWordsRatherThanLookingUpCorpusSurfaces(string root, string pos,
        string politePast, string te, string potential)
    {
        await using var context = fixture.Runtime.CreateContext();
        var canonical = "dictionary:generalization-" + root;
        context.DictionaryEntries.Add(new() { CanonicalId = canonical, WrittenForms = [new() { Form = root }],
            Senses = [new() { SenseKey = "1", PartOfSpeech = [pos], Glosses = [new() { LanguageTag = "vi", GlossText = "Mục từ kiểm thử.", ReviewStatus = "demo" }] }] });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var channel = GrpcChannel.ForAddress(fixture.Runtime.Address);
        var adapter = new GrpcDictionaryTokenizerAdapter(new(channel), new EfConjugationDatasetRepository(context), ConjugationCorpusFiles.CandidateVersion);
        var useCase = new LookupWordUseCase(new CorpusRepository(new EfDictionaryRepository(context), new HashSet<string> { canonical }), adapter);
        foreach (var (text, rule) in new[] { (politePast, "conjugation:verb-polite-past"), (te, "conjugation:verb-te"),
            (potential, "conjugation:verb-potential") })
        {
            var result = await useCase.ExecuteAsync(text, TestContext.Current.CancellationToken);
            result.Alternatives.Should().Contain(a => a.BaseForm == root && a.RuleId == rule);
            result.Matches.Should().Contain(m => m.CanonicalId == canonical);
        }
    }

    [Fact]
    public async Task UnreviewedIrregularPoliteStemsAreNotAcceptedAsRegularGodan()
    {
        await using var context = fixture.Runtime.CreateContext();
        var canonical = "dictionary:unreviewed-irregular";
        context.DictionaryEntries.Add(new() { CanonicalId = canonical, WrittenForms = [new() { Form = "なさる" }],
            Senses = [new() { SenseKey = "1", PartOfSpeech = ["v5r-i"] }] });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var channel = GrpcChannel.ForAddress(fixture.Runtime.Address);
        var adapter = new GrpcDictionaryTokenizerAdapter(new(channel), new EfConjugationDatasetRepository(context), ConjugationCorpusFiles.CandidateVersion);
        var result = await new LookupWordUseCase(new CorpusRepository(new EfDictionaryRepository(context), new HashSet<string> { canonical }), adapter)
            .ExecuteAsync("なさります", TestContext.Current.CancellationToken);
        result.Alternatives.Should().BeEmpty(); result.Capabilities.Conjugation.Should().Be("unavailable");
    }

    [Fact]
    public async Task GodanRaRowPassiveAndHonorificAreNotLostToAnIchidanProposal()
    {
        await using var context = fixture.Runtime.CreateContext();
        const string canonical = "dictionary:ra-passive-regression";
        context.DictionaryEntries.Add(new() { CanonicalId = canonical, WrittenForms = [new() { Form = "帰る" }],
            Senses = [new() { SenseKey = "1", PartOfSpeech = ["v5r"] }] });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var channel = GrpcChannel.ForAddress(fixture.Runtime.Address);
        var adapter = new GrpcDictionaryTokenizerAdapter(new(channel), new EfConjugationDatasetRepository(context), ConjugationCorpusFiles.CandidateVersion);
        var result = await new LookupWordUseCase(new CorpusRepository(new EfDictionaryRepository(context), new HashSet<string> { canonical }), adapter)
            .ExecuteAsync("帰られる", TestContext.Current.CancellationToken);
        result.Alternatives!.Select(a => a.RuleId).Should().Equal("conjugation:verb-honorific", "conjugation:verb-passive");
        result.Alternatives.Should().OnlyContain(a => a.BaseForm == "帰る");
        // Same reviewed godan realization as 書かれる, transformed by the versioned る-row.
        result.Alternatives[0].Segments.Select(s => s.Surface).Should().Equal("帰ら", "れ", "る");
    }
}

public sealed class F09CorpusFixture : IAsyncLifetime
{
    public TokenizerDeinflectionFixture Runtime { get; } = new();
    public async ValueTask InitializeAsync()
    {
        await Runtime.InitializeAsync();
        try
        {
            using var corpus = ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Packet, "cases.json"));
            await using var context = Runtime.CreateContext();
            foreach (var f in corpus.RootElement.GetProperty("dictionary_fixtures").EnumerateArray())
            {
                var form = f.GetProperty("preferred_form").GetString()!;
                var group = f.GetProperty("word_group").GetString();
                var cls = f.GetProperty("conjugation_class");
                var pos = group == "i_adjective" ? "adj-i" : group == "na_adjective" ? "adj-na" : group == "noun_copula" ? "n" :
                    cls.ValueKind == JsonValueKind.Null ? "derived_entry" : cls.GetString() == "ichidan" ? "v1" :
                    cls.GetString() == "irregular" ? form == "する" ? "vs-i" : "vk" : form == "行く" ? "v5k-s" : form.EndsWith("く", StringComparison.Ordinal) ? "v5k" : "v5m";
                context.DictionaryEntries.Add(new() { CanonicalId = f.GetProperty("canonical_id").GetString()!,
                    WrittenForms = f.GetProperty("forms").EnumerateArray().Select(x => new WrittenForm { Form = x.GetString()! }).ToList(),
                    Readings = f.GetProperty("readings").EnumerateArray().Select(x => new Reading { ReadingText = x.GetString()! }).ToList(),
                    Senses = [new() { SenseKey = "1", Position = 1, PartOfSpeech = [pos],
                        Glosses = [new() { LanguageTag = "vi", GlossText = f.GetProperty("gloss_vi").GetString()!, Position = 1, ReviewStatus = "demo" }] }] });
            }
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        catch { await Runtime.DisposeAsync(); throw; }
    }
    public ValueTask DisposeAsync() => Runtime.DisposeAsync();
}

/// <summary>Test-only fixture availability; actual SQL lookup is retained and no expected explanation is supplied.</summary>
internal sealed class CorpusRepository(IDictionaryRepository inner, IReadOnlySet<string> allowed) : IDictionaryRepository
{
    public async Task<IReadOnlyList<DictionaryEntry>> SearchByFormOrReadingAsync(string query, CancellationToken ct = default) =>
        (await inner.SearchByFormOrReadingAsync(query, ct)).Where(e => allowed.Contains(e.CanonicalId)).ToArray();
    public Task<IReadOnlyList<DictionaryEntry>> GetByCanonicalIdsAsync(IEnumerable<string> ids, CancellationToken ct = default) =>
        inner.GetByCanonicalIdsAsync(ids.Where(allowed.Contains), ct);
}
