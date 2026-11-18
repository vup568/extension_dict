using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IntegrationTests;

public sealed class DatabaseRecoveryCommandTests
{
    [Fact]
    public async Task RealOperatorSwitchIsHandledBeforeNormalStartup()
    {
        var root = ConjugationCorpusFiles.Root;
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") {
            WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(Path.Combine(root, "src/interface/bin/Release/net10.0/WebApi.dll"));
        start.ArgumentList.Add("--recover-legacy-schema");
        start.Environment.Remove("LocalDatabaseRecoveryConfirmed");
        start.Environment.Remove("LegacySchemaReferenceConnection");
        start.Environment.Remove("ConnectionStrings__DefaultConnection");
        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var errors = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        process.ExitCode.Should().Be(1);
        (await output).Should().Contain("Local database recovery refused");
        (await errors).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "127.0.0.1", "jp_recovery_reference_0123456789abcdef0123456789abcdef")]
    [InlineData("1", "remote.invalid", "jp_recovery_reference_0123456789abcdef0123456789abcdef")]
    [InlineData("1", "127.0.0.1", "untrusted_reference")]
    [InlineData("1", "127.0.0.1", "original")]
    public async Task UnconfirmedRemoteOrInvalidReferenceCannotReachTheDatabase(string? confirmation, string host, string database)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["LocalDatabaseRecoveryConfirmed"] = confirmation,
            ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Port=1;Database=original",
            ["LegacySchemaReferenceConnection"] = $"Host={host};Port=1;Database={database}"
        }).Build();
        using var output = new StringWriter();
        (await WebApi.DatabaseRecoveryCommand.ExecuteAsync(configuration, output, TestContext.Current.CancellationToken)).Should().Be(1);
        output.ToString().Should().Contain("InvalidOperationException").And.NotContain(host).And.NotContain("Host=");
    }
}
