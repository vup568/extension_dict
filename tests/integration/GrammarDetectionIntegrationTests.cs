namespace IntegrationTests;

using Application.Grammar;
using Application.Grammar.Helpers;
using Application.Grammar.Models;
using Application.Grammar.Ports;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

public sealed class GrammarDetectionIntegrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private DbContextOptions<AppDbContext> CreateDbContextOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
    }

    [Fact]
    public async Task DetectGrammar_WithDatabaseRules_ReturnsMatchedOccurrences()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var rule = new GrammarRule
            {
                CanonicalId = $"grammar:{Guid.NewGuid():N}",
                Pattern = "〜ている",
                JlptLevel = "N5",
                MeaningVi = "Đang diễn ra",
                MatcherMetadata = """[ { "surface": "て" }, { "base": "いる" } ]""",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (!await setupContext.GrammarRules.AnyAsync(r => r.Pattern == "〜ている", TestContext.Current.CancellationToken))
            {
                setupContext.GrammarRules.Add(rule);
                await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
        }

        // Mock Tokenizer Sidecar output
        var mockTokenizer = Substitute.For<IGrammarTokenizerPort>();
        var tokens = new List<NormalizedTokenDto>
        {
            new("雨", "雨", "名詞,一般,*,*,*,*", 0, 1),
            new("が", "が", "助詞,格助詞,*,*,*,*", 1, 2),
            new("降っ", "降る", "動詞,一般,*,*,*,*", 2, 4),
            new("て", "て", "助詞,接続助詞,*,*,*,*", 4, 5),
            new("いる", "いる", "動詞,非自立可能,*,*,*,*", 5, 7)
        };
        mockTokenizer.TokenizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(tokens);

        await using var dbContext = new AppDbContext(options);
        IGrammarRepository repository = new EfGrammarRepository(dbContext);
        var matcher = new GrammarSequenceMatcher();
        var useCase = new DetectGrammarUseCase(mockTokenizer, repository, matcher);

        // Act
        var request = new GrammarDetectionRequestDto("雨が降っている");
        var result = await useCase.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("completed");
        result.Occurrences.Should().HaveCountGreaterThanOrEqualTo(1);
        result.Occurrences.Should().Contain(o => o.Pattern == "〜ている");
    }

    [Fact]
    public async Task DetectGrammar_SidecarFailure_ReturnsGracefulDegradationFailedStatus()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var mockTokenizer = Substitute.For<IGrammarTokenizerPort>();
        mockTokenizer.TokenizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .ThrowsAsync(new InvalidOperationException("gRPC Sidecar connection error"));

        await using var dbContext = new AppDbContext(options);
        IGrammarRepository repository = new EfGrammarRepository(dbContext);
        var matcher = new GrammarSequenceMatcher();
        var useCase = new DetectGrammarUseCase(mockTokenizer, repository, matcher);

        // Act
        var request = new GrammarDetectionRequestDto("雨が降っている");
        var result = await useCase.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("failed");
        result.Occurrences.Should().BeEmpty();
    }
}
