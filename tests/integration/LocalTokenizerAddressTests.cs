using System.Text.Json;
using FluentAssertions;
using Grpc.Net.Client;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

public sealed class LocalTokenizerAddressTests
{
    [Fact]
    public void InfrastructureDefaultsToTheIpv4AddressPublishedByComposeWithoutNetworkAccess()
    {
        using var provider = CreateProvider(null);
        using var expected = GrpcChannel.ForAddress("http://127.0.0.1:50051");

        provider.GetRequiredService<GrpcChannel>().Target.Should().Be(expected.Target);
    }

    [Theory]
    [InlineData("http://tokenizer:50051")]
    [InlineData("https://nlp.example.invalid:5443")]
    [InlineData("http://[::1]:50051")]
    [InlineData("http://localhost:50051")]
    public void ExplicitSidecarAddressIsNotRewritten(string address)
    {
        using var provider = CreateProvider(address);
        using var expected = GrpcChannel.ForAddress(address);

        provider.GetRequiredService<GrpcChannel>().Target.Should().Be(expected.Target);
    }

    [Fact]
    public void AppSettingsDefaultMatchesTheExplicitIpv4ComposePortBinding()
    {
        using var settings = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(ConjugationCorpusFiles.Root, "src/interface/appsettings.json")));
        settings.RootElement.GetProperty("GrpcTokenizerAddress").GetString()
            .Should().Be("http://127.0.0.1:50051");

        File.ReadAllText(Path.Combine(ConjugationCorpusFiles.Root, "compose.yaml"))
            .Should().Contain("\"127.0.0.1:50051:50051\"");
    }

    private static ServiceProvider CreateProvider(string? address)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Database=configuration-test-only"
        };
        if (address is not null) values["GrpcTokenizerAddress"] = address;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddInfrastructureServices(configuration);
        return services.BuildServiceProvider();
    }
}
