using Application.Conjugation.Ports;
using Application.Dictionary.Ports;
using Application.Grammar.Ports;
using Application.Kanji.Ports;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

public sealed class BackendConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingDatabaseConfigurationFailsBeforeAnHttpRequest(string? connection)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = connection }).Build();
        var services = new ServiceCollection();
        var action = () => services.AddInfrastructureServices(configuration);
        action.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings__DefaultConnection*");
    }

    [Fact]
    public void ConfiguredRepositoriesResolveAndContextsRemainTransientWithoutDatabaseAccess()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Database=configuration-test-only" }).Build();
        var services = new ServiceCollection(); services.AddInfrastructureServices(configuration);
        using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Should().NotBeSameAs(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        scope.ServiceProvider.GetRequiredService<IDictionaryRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IKanjiRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IGrammarRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IConjugationDatasetRepository>().Should().NotBeNull();
    }
}
