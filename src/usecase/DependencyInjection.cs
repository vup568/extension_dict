using Application.Analysis;
using Application.Dictionary;
using Application.Grammar;
using Application.Grammar.Helpers;
using Application.Kanji;
using Application.Speech;
using Application.Translation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<LookupWordUseCase>();
        services.AddScoped<SentenceVocabularyUseCase>();
        services.AddScoped<LookupKanjiUseCase>();
        services.AddSingleton<GrammarSequenceMatcher>();
        services.AddScoped<DetectGrammarUseCase>();
        services.AddScoped<AnalyzeTextUseCase>();
        services.AddScoped<TranslationUseCase>();
        services.AddScoped<SpeechUseCase>();
        return services;
    }
}
