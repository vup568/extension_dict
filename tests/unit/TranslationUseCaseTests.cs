namespace UnitTests;

using Application.Translation;
using NSubstitute;
using Xunit;

public sealed class TranslationUseCaseTests
{
    [Fact]
    public async Task ValidRequest_ForwardsOnlyTrimmedText_AndReturnsTranslation()
    {
        var provider = Substitute.For<ITranslationProvider>();
        provider.TranslateJapaneseToEnglishAsync("猫です。", Arg.Any<CancellationToken>()).Returns("It is a cat.");
        var useCase = new TranslationUseCase(provider);
        Assert.Equal("It is a cat.", await useCase.ExecuteAsync(" 猫です。 ", "en", Guid.NewGuid().ToString(), TestContext.Current.CancellationToken));
        await provider.Received(1).TranslateJapaneseToEnglishAsync("猫です。", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "en", "a1d3b846-c077-4b5a-8b97-1d4d583ab0ad")]
    [InlineData("猫", "vi", "a1d3b846-c077-4b5a-8b97-1d4d583ab0ad")]
    [InlineData("猫", "en", "bad-id")]
    [InlineData("猫\u0000", "en", "a1d3b846-c077-4b5a-8b97-1d4d583ab0ad")]
    public async Task InvalidRequest_DoesNotCallProvider(string text, string language, string id)
    {
        var provider = Substitute.For<ITranslationProvider>();
        await Assert.ThrowsAsync<ArgumentException>(() => new TranslationUseCase(provider).ExecuteAsync(text, language, id, TestContext.Current.CancellationToken));
        await provider.DidNotReceive().TranslateJapaneseToEnglishAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OversizedInput_IsRejected_AndInvalidOutputIsNotReturned()
    {
        var provider = Substitute.For<ITranslationProvider>();
        var useCase = new TranslationUseCase(provider);
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new string('あ', 1001), "en", Guid.NewGuid().ToString(), TestContext.Current.CancellationToken));
        provider.TranslateJapaneseToEnglishAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(" ");
        var exception = await Assert.ThrowsAsync<TranslationProviderException>(() => useCase.ExecuteAsync("猫", "en", Guid.NewGuid().ToString(), TestContext.Current.CancellationToken));
        Assert.Equal("SERVICE_UNAVAILABLE", exception.Code);
    }

    [Fact]
    public async Task LengthLimit_AppliesAfterTrimming()
    {
        var provider = Substitute.For<ITranslationProvider>();
        provider.TranslateJapaneseToEnglishAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("translation");
        var text = new string('あ', 1000);
        var result = await new TranslationUseCase(provider).ExecuteAsync(" " + text + " ", "en", Guid.NewGuid().ToString(), TestContext.Current.CancellationToken);
        Assert.Equal("translation", result);
        await provider.Received(1).TranslateJapaneseToEnglishAsync(text, Arg.Any<CancellationToken>());
    }
}
