using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Conjugation;
using Application.Dictionary;
using Domain.Entities;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Grpc.Net.Client;
using Infrastructure.Adapters;
using Infrastructure.Adapters.Grpc;
using Infrastructure.Conjugation;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

/// <summary>Real pinned Sudachi container + PostgreSQL + HTTP. No mocked linguistic provider.</summary>
public sealed class TokenizerDeinflectionIntegrationTests(TokenizerDeinflectionFixture fixture) : IClassFixture<TokenizerDeinflectionFixture>
{
    private string Address => fixture.Address;
    private AppDbContext CreateContext() => fixture.CreateContext();

    [Theory]
    [InlineData("食べます", "食べる", "không quá khứ")]
    [InlineData("食べました", "食べる", "quá khứ")]
    [InlineData("食べない", "食べる", "phủ định")]
    [InlineData("食べなかった", "食べる", "quá khứ")]
    [InlineData("食べた", "食べる", "quá khứ")]
    [InlineData("食べません", "食べる", "lịch sự")]
    [InlineData("高かった", "高い", "quá khứ")]
    [InlineData("高くない", "高い", "phủ định")]
    [InlineData("高くなかった", "高い", "quá khứ")]
    [InlineData("書いた", "書く", "quá khứ")]
    [InlineData("読んだ", "読む", "quá khứ")]
    [InlineData("しました", "する", "quá khứ")]
    [InlineData("来ました", "来る", "quá khứ")]
    public async Task RealProviderCorpusResolvesBaseAndVietnamese(string surface, string baseForm, string semantics)
    {
        await using var context = CreateContext();
        using var channel = GrpcChannel.ForAddress(Address);
        var adapter = new GrpcDictionaryTokenizerAdapter(new(channel), new EfConjugationDatasetRepository(context), ConjugationSeedData.DevelopmentVersion);
        var result = await new LookupWordUseCase(new EfDictionaryRepository(context), adapter).ExecuteAsync(surface, TestContext.Current.CancellationToken);
        result.Matches.Should().ContainSingle();
        result.Matches[0].MatchedWrittenForm.Should().Be(baseForm);
        result.Conjugation!.Detail!.LanguageTag.Should().Be("vi");
        result.Conjugation.Explanation.Should().Contain(semantics);
        result.Conjugation.Detail.Span.End.Should().Be(surface.Length);
        string.Concat(result.Conjugation.Detail.Segments.Select(x => x.Surface)).Should().Be(surface);
    }

    [Theory]
    [InlineData("猫を食べました")]
    [InlineData("食べられる")]
    [InlineData("食べました。")]
    [InlineData("食べ ました")]
    public async Task UnsupportedInputDoesNotReturnFirstVerb(string surface)
    {
        await using var context = CreateContext();
        using var channel = GrpcChannel.ForAddress(Address);
        var adapter = new GrpcDictionaryTokenizerAdapter(new(channel), new EfConjugationDatasetRepository(context), ConjugationSeedData.DevelopmentVersion);
        var result = await new LookupWordUseCase(new EfDictionaryRepository(context), adapter).ExecuteAsync(surface, TestContext.Current.CancellationToken);
        result.Matches.Should().BeEmpty(); result.Conjugation.Should().BeNull();
        result.Capabilities.Conjugation.Should().Be("unavailable");
    }

    [Fact]
    public async Task SupportedRuleWithoutBaseEntryDoesNotInventDictionaryOrConjugation()
    {
        await using var context = CreateContext();
        using var channel = GrpcChannel.ForAddress(Address);
        var adapter = new GrpcDictionaryTokenizerAdapter(new(channel), new EfConjugationDatasetRepository(context), ConjugationSeedData.DevelopmentVersion);
        var result = await new LookupWordUseCase(new EfDictionaryRepository(context), adapter)
            .ExecuteAsync("泳ぎました", TestContext.Current.CancellationToken);
        result.Matches.Should().BeEmpty();
        result.Conjugation.Should().BeNull();
        result.Capabilities.Conjugation.Should().Be("completed");
    }

    [Fact]
    public async Task ExactDictionaryEntryWinsWithoutConjugation()
    {
        await using var context = CreateContext();
        using var channel = GrpcChannel.ForAddress(Address);
        var adapter = new GrpcDictionaryTokenizerAdapter(new(channel), new EfConjugationDatasetRepository(context), ConjugationSeedData.DevelopmentVersion);
        var result = await new LookupWordUseCase(new EfDictionaryRepository(context), adapter)
            .ExecuteAsync("食べる", TestContext.Current.CancellationToken);
        result.Matches.Should().ContainSingle();
        result.Matches[0].MatchedWrittenForm.Should().Be("食べる");
        result.Conjugation.Should().BeNull();
    }

    [Fact]
    public async Task SupplementalSpansUseUtf16AndNoSelectionTextIsLogged()
    {
        using var channel = GrpcChannel.ForAddress(Address);
        var text = "𠮷野で食べました";
        var response = await new TokenizerService.TokenizerServiceClient(channel).TokenizeAsync(new() { Text = text, Mode = "C" }, cancellationToken: TestContext.Current.CancellationToken);
        foreach (var token in response.Tokens) text[token.SpanStart..token.SpanEnd].Should().Be(token.Surface);
        response.Tokens[^1].SpanEnd.Should().Be(text.Length);
        var logs = await fixture.Tokenizer.GetLogsAsync(ct: TestContext.Current.CancellationToken);
        logs.Stdout.Should().NotContain(text); logs.Stderr.Should().NotContain("食べました");
    }

    [Fact]
    public async Task DatasetRejectsMutationAndNewVersionChangesDataWithoutEngineEdits()
    {
        await using var context = CreateContext();
        var original = (await new EfConjugationDatasetRepository(context).GetAsync(ConjugationSeedData.DevelopmentVersion, TestContext.Current.CancellationToken))!;
        foreach (var sql in new[] { "UPDATE conjugation_datasets SET content = '{{}}'", "DELETE FROM conjugation_datasets", "TRUNCATE conjugation_datasets" })
            await Assert.ThrowsAsync<Npgsql.PostgresException>(() => context.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken));
        var updated = original.Content.Replace(ConjugationSeedData.DevelopmentVersion, "test-new-version", StringComparison.Ordinal)
            .Replace("Dạng lịch sự, quá khứ của", "Nội dung kiểm thử mới cho", StringComparison.Ordinal);
        context.ConjugationDatasets.Add(new() { Version = "test-new-version", Content = updated });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        using var channel = GrpcChannel.ForAddress(Address);
        var result = await new GrpcDictionaryTokenizerAdapter(new(channel), new EfConjugationDatasetRepository(context), "test-new-version")
            .DeinflectAsync("食べました", TestContext.Current.CancellationToken);
        result.RuleExplanation.Should().StartWith("Nội dung kiểm thử mới");
        result.Detail!.DatasetVersion.Should().Be("test-new-version");
    }

    [Fact]
    public async Task AnonymousHttpContractV1V2AndDictionaryAreConsistent()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ConnectionStrings:DefaultConnection"] = fixture.ConnectionString;
        builder.Configuration["GrpcTokenizerAddress"] = Address;
        builder.Configuration["ConjugationDatasetVersion"] = ConjugationSeedData.DevelopmentVersion;
        WebApi.ApiComposition.RegisterServices(builder);
        await using var app = builder.Build(); WebApi.ApiComposition.MapEndpoints(app);
        app.Urls.Add("http://127.0.0.1:0"); await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var id = Guid.NewGuid().ToString();
        var legacy = await client.PostAsJsonAsync("/api/analysis", new { text = "食べました", interaction_id = id }, TestContext.Current.CancellationToken);
        legacy.StatusCode.Should().Be(HttpStatusCode.OK);
        using var legacyJson = JsonDocument.Parse(await legacy.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        legacyJson.RootElement.GetProperty("data").GetProperty("conjugations").GetArrayLength().Should().Be(0);
        client.DefaultRequestHeaders.Add("X-Contract-Version", "2");
        var current = await client.PostAsJsonAsync("/api/analysis", new { text = "食べました", interaction_id = id }, TestContext.Current.CancellationToken);
        current.StatusCode.Should().Be(HttpStatusCode.OK);
        using var currentJson = JsonDocument.Parse(await current.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        currentJson.RootElement.GetProperty("meta").GetProperty("contract_version").GetString().Should().Be("2");
        currentJson.RootElement.GetProperty("data").GetProperty("conjugations")[0].GetProperty("base_form").GetString().Should().Be("食べる");
        var dictionary = await client.GetAsync("/api/dictionary/lookup?q=" + Uri.EscapeDataString("食べました"), TestContext.Current.CancellationToken);
        dictionary.StatusCode.Should().Be(HttpStatusCode.OK);
        client.DefaultRequestHeaders.Remove("X-Contract-Version"); client.DefaultRequestHeaders.Add("X-Contract-Version", "99");
        (await client.PostAsJsonAsync("/api/analysis", new { text = "食べました" }, TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await app.StopAsync(TestContext.Current.CancellationToken);
    }
}

/// <summary>One provider/database pair per corpus; xUnit test instances do not own shared setup.</summary>
public sealed class TokenizerDeinflectionFixture : IAsyncLifetime
{
    private readonly PostgreSqlFixture postgres = new();
    public IContainer Tokenizer { get; } = new ContainerBuilder("jp-reading-tokenizer:verified-20261013")
        .WithPortBinding(50051, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(50051)).Build();
    public string Address => $"http://{Tokenizer.Hostname}:{Tokenizer.GetMappedPublicPort(50051)}";
    public string ConnectionString => postgres.ConnectionString;
    public AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    public async ValueTask InitializeAsync()
    {
        try
        {
            await postgres.InitializeAsync();
            await Tokenizer.StartAsync(TestContext.Current.CancellationToken);
            await using var context = CreateContext();
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
            foreach (var (form, reading) in new[] { ("食べる", "たべる"), ("高い", "たかい"), ("書く", "かく"), ("読む", "よむ"), ("する", "する"), ("来る", "くる") })
                context.DictionaryEntries.Add(new DictionaryEntry
                {
                    CanonicalId = $"dictionary:live-{form}", WrittenForms = [new() { Form = form }],
                    Readings = [new() { ReadingText = reading }],
                    Senses = [new() { SenseKey = "1", Position = 1, Glosses = [new() { LanguageTag = "vi", GlossText = "Nghĩa kiểm thử", Position = 1, ReviewStatus = "demo" }] }]
                });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { await Tokenizer.DisposeAsync(); }
        finally { await postgres.DisposeAsync(); }
    }
}
