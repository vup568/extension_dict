using Application.Dictionary;
using Application.Kanji;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<LookupWordUseCase>();
        services.AddScoped<LookupKanjiUseCase>();
        return services;
    }
}
