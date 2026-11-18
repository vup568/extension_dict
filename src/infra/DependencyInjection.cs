namespace Infrastructure;

using Application.Dictionary.Ports;
using Application.Conjugation.Ports;
using Application.Grammar.Ports;
using Application.Kanji.Ports;
using Application.Speech;
using Application.Translation;
using Grpc.Net.Client;
using Infrastructure.Adapters;
using Infrastructure.Adapters.Grpc;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        // EARS[Unwanted]: Do not register an unusable EF graph or serve requests with missing DB configuration.
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Missing PostgreSQL configuration. Set ConnectionStrings__DefaultConnection before starting the backend.");

        services.AddMemoryCache();
        // EARS[Unwanted]: A provider must never forward content or credentials through a redirect.
        services.AddSingleton<ITranslationProvider>(_ => new GeminiTranslationProvider(
            new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan },
            configuration["Translation:Gemini:ApiKey"]));
        services.AddSingleton<ISpeechProvider>(_ => new GeminiSpeechProvider(
            new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan },
            configuration["Speech:Gemini:ApiKey"], configuration["Translation:Gemini:ApiKey"],
            configuration["Speech:Gemini:Model"], configuration["Speech:Gemini:Voice"]));
        // Analysis capabilities run concurrently and must retain independent EF contexts.
        services.AddDbContext<AppDbContext>(
            options => options.UseNpgsql(connectionString),
            contextLifetime: ServiceLifetime.Transient);

        services.AddScoped<IDictionaryRepository, EfDictionaryRepository>();
        services.AddScoped<IKanjiRepository, EfKanjiRepository>();
        services.AddScoped<IConjugationDatasetRepository, EfConjugationDatasetRepository>();
        var datasetVersion = configuration["ConjugationDatasetVersion"];
        services.AddScoped<ITokenizerAdapter>(sp => string.IsNullOrWhiteSpace(datasetVersion)
            ? new NullTokenizerAdapter()
            : new GrpcDictionaryTokenizerAdapter(
                sp.GetRequiredService<TokenizerService.TokenizerServiceClient>(),
                sp.GetRequiredService<IConjugationDatasetRepository>(), datasetVersion));

        // Register GrpcChannel connection reuse (Singleton) for Python Tokenizer Sidecar
        // Local Compose publishes IPv4 only; avoid localhost's IPv6-first connection delay.
        var sidecarAddress = configuration["GrpcTokenizerAddress"] ?? "http://127.0.0.1:50051";
        services.AddSingleton(sp => GrpcChannel.ForAddress(sidecarAddress));
        services.AddSingleton(sp =>
        {
            var channel = sp.GetRequiredService<GrpcChannel>();
            return new TokenizerService.TokenizerServiceClient(channel);
        });

        services.AddScoped<IGrammarTokenizerPort, GrpcGrammarTokenizerAdapter>();

        services.AddScoped<EfGrammarRepository>();
        services.AddScoped<IGrammarRepository>(sp =>
        {
            var efRepo = sp.GetRequiredService<EfGrammarRepository>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            return new CachedGrammarRepository(efRepo, cache);
        });

        return services;
    }
}
