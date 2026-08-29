using Application.Dictionary;
using Application.Grammar;
using Application.Grammar.Helpers;
using Application.Kanji;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<LookupWordUseCase>();
        services.AddScoped<LookupKanjiUseCase>();
        services.AddSingleton<GrammarSequenceMatcher>();
        services.AddScoped<DetectGrammarUseCase>();
        return services;
    }
}
