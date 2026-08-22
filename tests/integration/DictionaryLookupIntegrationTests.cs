using Application.Dictionary;
using Application.Dictionary.Ports;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Adapters;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntegrationTests;

public sealed class DictionaryLookupIntegrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private DbContextOptions<AppDbContext> CreateDbContextOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
    }

    [Fact]
    public async Task AC001_ExactMatchLookup_ReturnsEntryWithCorrectFormsReadingsAndGlosses()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var entry = new DictionaryEntry
            {
                CanonicalId = $"dictionary:test-{Guid.NewGuid():N}",
                WrittenForms = [new WrittenForm { Form = "日本語", IsCommon = true }],
                Readings = [new Reading { ReadingText = "にほんご", IsCommon = true }],
                Senses = [
                    new DictionarySense
                    {
                        SenseKey = "1",
                        Position = 1,
                        PartOfSpeech = ["noun"],
                        Glosses = [new LocalizedGloss { LanguageTag = "vi", GlossText = "Tiếng Nhật", Position = 1 }]
                    }
                ]
            };

            setupContext.DictionaryEntries.Add(entry);
            await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using var dbContext = new AppDbContext(options);
        IDictionaryRepository repository = new EfDictionaryRepository(dbContext);
        ITokenizerAdapter tokenizer = new NullTokenizerAdapter();
        var useCase = new LookupWordUseCase(repository, tokenizer);

        var result = await useCase.ExecuteAsync("日本語", TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Matches.Should().HaveCount(1);
        var match = result.Matches[0];
        match.CanonicalId.Should().StartWith("dictionary:test-");
        match.MatchedWrittenForm.Should().Be("日本語");
        match.WrittenForms.Should().ContainSingle(w => w.Form == "日本語");
        match.Readings.Should().ContainSingle(r => r.ReadingText == "にほんご");
        match.Senses[0].Glosses[0].LanguageTag.Should().Be("vi");
        match.Senses[0].Glosses[0].GlossText.Should().Be("Tiếng Nhật");
    }

    [Fact]
    public async Task AC002_LanguagePriority_PrioritizesVietnameseGlossOverEnglish()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var entry = new DictionaryEntry
            {
                CanonicalId = $"dictionary:test-{Guid.NewGuid():N}",
                WrittenForms = [new WrittenForm { Form = "猫", IsCommon = true }],
                Readings = [new Reading { ReadingText = "ねこ", IsCommon = true }],
                Senses = [
                    new DictionarySense
                    {
                        SenseKey = "1",
                        Position = 1,
                        Glosses = [
                            new LocalizedGloss { LanguageTag = "en", GlossText = "cat", Position = 1 },
                            new LocalizedGloss { LanguageTag = "vi", GlossText = "con mèo", Position = 1 }
                        ]
                    }
                ]
            };

            setupContext.DictionaryEntries.Add(entry);
            await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using var dbContext = new AppDbContext(options);
        IDictionaryRepository repository = new EfDictionaryRepository(dbContext);
        ITokenizerAdapter tokenizer = new NullTokenizerAdapter();
        var useCase = new LookupWordUseCase(repository, tokenizer);

        var result = await useCase.ExecuteAsync("猫", TestContext.Current.CancellationToken);

        // Assert
        result.Matches.Should().HaveCount(1);
        var sense = result.Matches[0].Senses[0];
        sense.Glosses.Should().HaveCount(1);
        sense.Glosses[0].LanguageTag.Should().Be("vi");
        sense.Glosses[0].GlossText.Should().Be("con mèo");
    }

    [Fact]
    public async Task AC003_EmptyResult_ReturnsEmptyMatchesWhenWordNotFound()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using var dbContext = new AppDbContext(options);
        IDictionaryRepository repository = new EfDictionaryRepository(dbContext);
        ITokenizerAdapter tokenizer = new NullTokenizerAdapter();
        var useCase = new LookupWordUseCase(repository, tokenizer);

        var result = await useCase.ExecuteAsync("asdfghjkl_nonexistent_word", TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Matches.Should().BeEmpty();
        result.Capabilities.Morphology.Should().Be("completed");
    }

    [Fact]
    public async Task AC004_Validation_ThrowsArgumentExceptionForInvalidQueries()
    {
        // Arrange
        var options = CreateDbContextOptions();
        await using var dbContext = new AppDbContext(options);
        IDictionaryRepository repository = new EfDictionaryRepository(dbContext);
        ITokenizerAdapter tokenizer = new NullTokenizerAdapter();
        var useCase = new LookupWordUseCase(repository, tokenizer);

        // Act & Assert
        var emptyAct = () => useCase.ExecuteAsync("", TestContext.Current.CancellationToken);
        await emptyAct.Should().ThrowAsync<ArgumentException>();

        var longAct = () => useCase.ExecuteAsync(new string('あ', 256), TestContext.Current.CancellationToken);
        await longAct.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AC005_RestrictionProvenance_LoadsSenseApplicabilitiesAccurately()
    {
        // Arrange
        var options = CreateDbContextOptions();
        long writtenFormId;
        long readingId;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var wf = new WrittenForm { Form = "表", IsCommon = true };
            var rd = new Reading { ReadingText = "おもて", IsCommon = true };

            var entry = new DictionaryEntry
            {
                CanonicalId = $"dictionary:test-{Guid.NewGuid():N}",
                WrittenForms = [wf],
                Readings = [rd],
                Senses = [
                    new DictionarySense
                    {
                        SenseKey = "1",
                        Position = 1,
                        Glosses = [new LocalizedGloss { LanguageTag = "vi", GlossText = "mặt ngoài, bề mặt", Position = 1 }]
                    }
                ]
            };

            setupContext.DictionaryEntries.Add(entry);
            await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            writtenFormId = wf.Id;
            readingId = rd.Id;

            var sense = entry.Senses.First();
            sense.Applicabilities.Add(new SenseApplicability
            {
                SenseId = sense.Id,
                WrittenFormId = writtenFormId,
                ReadingId = readingId
            });

            await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using var dbContext = new AppDbContext(options);
        IDictionaryRepository repository = new EfDictionaryRepository(dbContext);
        ITokenizerAdapter tokenizer = new NullTokenizerAdapter();
        var useCase = new LookupWordUseCase(repository, tokenizer);

        var result = await useCase.ExecuteAsync("表", TestContext.Current.CancellationToken);

        // Assert
        result.Matches.Should().NotBeNullOrEmpty();
        var match = result.Matches.First(m => m.WrittenForms.Any(w => w.Form == "表"));
        var restrictions = match.Senses[0].Restrictions;

        restrictions.Should().HaveCount(1);
        restrictions[0].WrittenFormId.Should().Be(writtenFormId);
        restrictions[0].ReadingId.Should().Be(readingId);
    }
}
