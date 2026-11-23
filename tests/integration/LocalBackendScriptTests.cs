using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace IntegrationTests;

public sealed class LocalBackendScriptTests
{
    [Theory]
    [InlineData("app_db")]
    [InlineData("postgres")]
    [InlineData("jp_dict_real_UPPER")]
    [InlineData("jp_dict_real_x;DROP_DATABASE")]
    public async Task RealDictionaryLauncherRejectsNonCandidateTargetsBeforeReadingInputs(string target)
    {
        var path = Escape(Path.Combine(ConjugationCorpusFiles.Root, "scripts/start-real-dictionary.ps1"));
        var result = await ShellAsync("pwsh.exe", "& '" + path + "' -Target '" + Escape(target)
            + "' -EnvFile 'does-not-exist' -SourceLock 'does-not-exist' -SkipDependencies");
        result.Code.Should().Be(1);
        result.Output.Should().Contain("dedicated jp_dict_real_ target database is required");
    }

    [Theory]
    [InlineData("powershell.exe")]
    [InlineData("pwsh.exe")]
    public async Task LiteralValuesRoundTripAndValidateOnlyNeverStartsServices(string shell)
    {
        var password = "fixture-" + Guid.NewGuid().ToString("N") + "; quote\" and # space";
        var content = "POSTGRES_DB=fixture_db\nPOSTGRES_USER=fixture_user\nPOSTGRES_PORT=5432\nPOSTGRES_PASSWORD='" + password + "'\n";
        await InFixtureAsync(content, async path =>
        {
            var script = "$v=Read-LocalPostgresEnvironment -Path '" + Escape(path) + "'; New-LocalBackendConnectionString -Values $v";
            var result = await ShellAsync(shell, script);
            result.Code.Should().Be(0);
            var parsed = new NpgsqlConnectionStringBuilder(result.Output.Trim());
            parsed.Password.Should().Be(password); parsed.Username.Should().Be("fixture_user"); parsed.Database.Should().Be("fixture_db");
            parsed.Port.Should().Be(5432); parsed.Host.Should().Be("127.0.0.1");
            var validate = await ShellAsync(shell, "& '" + Escape(Path.Combine(ConjugationCorpusFiles.Root, "scripts/start-local.ps1")) + "' -EnvFile '" + Escape(path) + "' -ValidateOnly");
            validate.Code.Should().Be(0); validate.Output.Should().Contain("No services started").And.NotContain(password);
            validate.Errors.Should().BeEmpty();
        });
    }

    [Theory]
    [InlineData("POSTGRES_PORT=0", "POSTGRES_PORT")]
    [InlineData("POSTGRES_PORT=65536", "POSTGRES_PORT")]
    [InlineData("POSTGRES_PORT=bad", "POSTGRES_PORT")]
    [InlineData("POSTGRES_PASSWORD=", "POSTGRES_PASSWORD")]
    [InlineData("POSTGRES_PASSWORD=${OTHER}", "Interpolation")]
    [InlineData("POSTGRES_PASSWORD=\"fixture\\nvalue\"", "single-quoted")]
    public async Task RejectsMissingInvalidOrInterpolatedDotenvWithoutEchoingValues(string replacement, string reason)
    {
        var values = "POSTGRES_DB=fixture_db\nPOSTGRES_USER=fixture_user\nPOSTGRES_PORT=5432\nPOSTGRES_PASSWORD=fixture-value\n";
        var key = replacement.Split('=')[0];
        values = string.Join('\n', values.Split('\n').Where(l => !l.StartsWith(key + "=", StringComparison.Ordinal))) + replacement + "\n";
        await InFixtureAsync(values, async path =>
        {
            var result = await ShellAsync("powershell.exe", "Read-LocalPostgresEnvironment -Path '" + Escape(path) + "' | Out-Null");
            result.Code.Should().Be(1); result.Output.Should().Contain(reason).And.NotContain("fixture-value").And.NotContain("${OTHER}");
        });
    }

    [Theory]
    [InlineData("powershell.exe")]
    [InlineData("pwsh.exe")]
    public async Task ExplicitConnectionWinsAndTemporaryEnvironmentIsRestoredEvenWhenBackendFails(string shell)
    {
        await InFixtureAsync("POSTGRES_DB=fixture_db\nPOSTGRES_USER=fixture_user\nPOSTGRES_PORT=5432\nPOSTGRES_PASSWORD=fixture-value\n", async path =>
        {
            var script = """
                $env:ConnectionStrings__DefaultConnection='Host=explicit-host;Database=explicit-fixture'
                $env:POSTGRES_DB='prior-db';$env:GrpcTokenizerAddress='http://prior-fixture:1';$env:ConjugationDatasetVersion='prior-pin'
                function global:docker { $global:LASTEXITCODE=0 }
                function global:dotnet {
                    $global:seenConnection=$env:ConnectionStrings__DefaultConnection
                    $global:seenDatabase=$env:POSTGRES_DB
                    $global:seenPin=$env:ConjugationDatasetVersion
                    $global:seenAddress=$env:GrpcTokenizerAddress
                    $global:LASTEXITCODE=7
                }
                try { & '__START__' -EnvFile '__ENV__' } catch { $script:stopped=$true }
                [pscustomobject]@{Stopped=$script:stopped;ExplicitPreserved=$global:seenConnection -eq 'Host=explicit-host;Database=explicit-fixture';AlignedDatabase=$global:seenDatabase -eq 'fixture_db';CandidatePin=$global:seenPin -eq 'candidate-20261110.1';Ipv4Address=$global:seenAddress -eq 'http://127.0.0.1:50051';RestoredDatabase=$env:POSTGRES_DB -eq 'prior-db';RestoredAddress=$env:GrpcTokenizerAddress -eq 'http://prior-fixture:1';RestoredPin=$env:ConjugationDatasetVersion -eq 'prior-pin'} | ConvertTo-Json -Compress
                """.Replace("__START__", Escape(Path.Combine(ConjugationCorpusFiles.Root, "scripts/start-local.ps1")), StringComparison.Ordinal)
                .Replace("__ENV__", Escape(path), StringComparison.Ordinal);
            var result = await ShellAsync(shell, script);
            result.Code.Should().Be(0);
            using var doc = JsonDocument.Parse(result.Output.Split('\n').Last(l => l.TrimStart().StartsWith('{')));
            doc.RootElement.EnumerateObject().Should().OnlyContain(p => p.Value.GetBoolean());
        });
    }

    private static async Task<(int Code, string Output, string Errors)> ShellAsync(string shell, string body)
    {
        var library = Path.Combine(ConjugationCorpusFiles.Root, "scripts/local-backend-config.ps1");
        var script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';try { . '" + Escape(library) + "'; " + body +
            " } catch { [Console]::Out.WriteLine($_.Exception.Message);exit 1 }";
        var start = new ProcessStartInfo(shell) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-ExecutionPolicy"); start.ArgumentList.Add("Bypass");
        start.ArgumentList.Add("-EncodedCommand"); start.ArgumentList.Add(Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script)));
        start.Environment.Remove("ConnectionStrings__DefaultConnection");
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        return (process.ExitCode, await output, await errors);
    }

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
    private static async Task InFixtureAsync(string content, Func<string, Task> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "jp-local-config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { var path = Path.Combine(directory, ".env"); await File.WriteAllTextAsync(path, content); await action(path); }
        finally
        {
            var resolved = Path.GetFullPath(directory);
            if (!resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("jp-local-config-", StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected fixture cleanup target.");
            Directory.Delete(resolved, true);
        }
    }
}
