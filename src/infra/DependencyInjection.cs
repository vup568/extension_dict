using Application.Dictionary.Ports;
using Application.Kanji.Ports;
using Infrastructure.Adapters;
using Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IDictionaryRepository, EfDictionaryRepository>();
        services.AddScoped<IKanjiRepository, EfKanjiRepository>();
        services.AddScoped<ITokenizerAdapter, NullTokenizerAdapter>();
        return services;
    }
}
