using Application.Kanji;
using Application.Kanji.Ports;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntegrationTests;

public sealed class KanjiLookupIntegrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private DbContextOptions<AppDbContext> CreateDbContextOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
    }

    [Fact]
    public async Task AC001_AC002_KanjiLookup_ReturnsCanonicalInfo_InFirstOccurrenceOrder()
    {
        // Arrange
        var options = CreateDbContextOptions();
        var char1 = "食";
        var char2 = "日";

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var k1 = new KanjiRecord
            {
                CanonicalId = $"kanji:{Guid.NewGuid():N}",
                Character = char1,
                StrokeCount = 9,
                Grade = 2,
                JlptLevel = 5,
                JlptProvenance = "derived",
                HanViet = "THỰC",
                UnicodeCodepoint = "U+98DF",
                OnReadings = ["ショク", "ジキ"],
                KunReadings = ["く.う", "た.べる"],
                MeaningsEn = ["eat", "food"],
                MeaningsVi = ["ăn", "thực phẩm"]
            };

            var k2 = new KanjiRecord
            {
                CanonicalId = $"kanji:{Guid.NewGuid():N}",
                Character = char2,
                StrokeCount = 4,
                Grade = 1,
                JlptLevel = 5,
                JlptProvenance = "derived",
                HanViet = "NHẬT",
                UnicodeCodepoint = "U+65E5",
                OnReadings = ["ニチ", "ジツ"],
                KunReadings = ["ひ", "-び"],
                MeaningsEn = ["day", "sun"],
                MeaningsVi = ["ngày", "mặt trời"]
            };

            // Add if not existing
            if (!await setupContext.KanjiRecords.AnyAsync(k => k.Character == char1, TestContext.Current.CancellationToken))
            {
                setupContext.KanjiRecords.Add(k1);
            }
            if (!await setupContext.KanjiRecords.AnyAsync(k => k.Character == char2, TestContext.Current.CancellationToken))
            {
                setupContext.KanjiRecords.Add(k2);
            }

            await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using var dbContext = new AppDbContext(options);
        IKanjiRepository repository = new EfKanjiRepository(dbContext);
        var useCase = new LookupKanjiUseCase(repository);

        var result = await useCase.ExecuteAsync("食べる日", TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Matches.Should().HaveCount(2);

        // Order check: 食 before 日
        result.Matches[0].Character.Should().Be("食");
        result.Matches[0].HanViet.Should().Be("THỰC");
        result.Matches[0].OnReadings.Should().Contain("ショク");
        result.Matches[0].JlptLevel.Should().Be(5);
        result.Matches[0].JlptProvenance.Should().Be("derived");

        result.Matches[1].Character.Should().Be("日");
        result.Matches[1].HanViet.Should().Be("NHẬT");
    }

    [Fact]
    public async Task AC010_EmptyResult_ReturnsEmptyMatches_WhenNoKanjiInDatabaseOrInput()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using var dbContext = new AppDbContext(options);
        IKanjiRepository repository = new EfKanjiRepository(dbContext);
        var useCase = new LookupKanjiUseCase(repository);

        var result = await useCase.ExecuteAsync("たべる", TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Matches.Should().BeEmpty();
    }
}
