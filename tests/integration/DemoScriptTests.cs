using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WebApi;
using Xunit;

namespace IntegrationTests;

// Local operator tooling is intentionally Windows-only, matching the user's approved scope.
public sealed class DemoScriptTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    public async Task SeedCommandRequiresExplicitConfirmationAndDoesNotPrintConfiguration(string? confirmation)
    {
        var sentinel = "synthetic-private-" + Guid.NewGuid().ToString("N");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["LocalDemoConfirmed"] = confirmation,
            ["ConnectionStrings:DefaultConnection"] = sentinel,
            ["DemoCorpusPath"] = sentinel
        }).Build();
        using var output = new StringWriter();
        (await DemoDictionaryCommand.ExecuteAsync(configuration, output, TestContext.Current.CancellationToken)).Should().Be(1);
        output.ToString().Should().Contain("refused or failed").And.NotContain(sentinel);
    }

    [Fact]
    public async Task SeedCommandRejectsOriginalDatabaseBeforeConnectionAndDoesNotPrintSecrets()
    {
        var sentinel = "synthetic-private-" + Guid.NewGuid().ToString("N");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["LocalDemoConfirmed"] = "1",
            ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Port=1;Database=original;Password=" + sentinel,
            ["DemoCorpusPath"] = Path.Combine(ConjugationCorpusFiles.Packet, "cases.json")
        }).Build();
        using var output = new StringWriter();
        (await DemoDictionaryCommand.ExecuteAsync(configuration, output, TestContext.Current.CancellationToken)).Should().Be(1);
        output.ToString().Should().Contain("InvalidOperationException").And.NotContain(sentinel).And.NotContain("Password=");
    }

    [Theory]
    [InlineData("prepare-demo.ps1")]
    [InlineData("start-demo.ps1")]
    public async Task ValidationDoesNotStartServicesOrChangeCallerEnvironment(string scriptName)
    {
        await InFixtureAsync(async directory =>
        {
            var script = """
                $env:ConnectionStrings__DefaultConnection='synthetic-original';$env:POSTGRES_DB='caller-original'
                & '__SCRIPT__' -EnvFile '__ENV__' -ValidateOnly
                if ($global:Calls.Count -ne 0) { throw 'ValidateOnly started a process.' }
                if ($env:ConnectionStrings__DefaultConnection -cne 'synthetic-original' -or $env:POSTGRES_DB -cne 'caller-original') { throw 'Caller changed.' }
                'PASS'
                """.Replace("__SCRIPT__", Escape(Path.Combine(directory, "scripts", scriptName)), StringComparison.Ordinal)
                .Replace("__ENV__", Escape(Path.Combine(directory, ".env")), StringComparison.Ordinal);
            var result = await ShellAsync(directory, script);
            result.Code.Should().Be(0, "synthetic fixture output: {0}", result.Output + result.Errors); result.Output.Should().Contain("PASS").And.NotContain("synthetic-password"); result.Errors.Should().BeEmpty();
        });
    }

    [Theory]
    [InlineData("f", 4)]
    [InlineData("t", 3)]
    public async Task PreparationUsesOnlyDemoConnectionAndCreatesOnlyWhenAbsent(string exists, int processCount)
    {
        await InFixtureAsync(async directory =>
        {
            var script = """
                $global:DemoExists='__EXISTS__'
                $env:ConnectionStrings__DefaultConnection='synthetic-original'
                & '__SCRIPT__' -EnvFile '__ENV__' -SkipBuild
                if ($global:Calls.Count -ne __COUNT__) { throw 'Unexpected calls.' }
                $seed=$global:Calls[-1]
                $builder=[System.Data.Common.DbConnectionStringBuilder]::new();$builder.set_ConnectionString($seed.Environment.ConnectionStrings__DefaultConnection)
                if ($builder['Database'] -cne 'jp_dict_demo' -or $builder['Host'] -cne '127.0.0.1') { throw 'Wrong seed target.' }
                if ($seed.Environment.LocalDemoConfirmed -cne '1' -or $seed.Arguments[-1] -cne '--seed-demo-dictionary') { throw 'Missing explicit command.' }
                if ($global:Calls[0].Arguments -notcontains 'never') { throw 'Implicit image pull.' }
                if ($env:ConnectionStrings__DefaultConnection -cne 'synthetic-original') { throw 'Caller changed.' }
                $creates=@($global:Calls | Where-Object InputText -eq 'CREATE DATABASE jp_dict_demo;')
                if (($global:DemoExists -ceq 'f' -and $creates.Count -ne 1) -or ($global:DemoExists -ceq 't' -and $creates.Count -ne 0)) { throw 'Wrong create target/count.' }
                'PASS'
                """.Replace("__EXISTS__", exists, StringComparison.Ordinal)
                .Replace("__COUNT__", processCount.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("__SCRIPT__", Escape(Path.Combine(directory, "scripts/prepare-demo.ps1")), StringComparison.Ordinal)
                .Replace("__ENV__", Escape(Path.Combine(directory, ".env")), StringComparison.Ordinal);
            var result = await ShellAsync(directory, script);
            result.Code.Should().Be(0, "synthetic fixture output: {0}", result.Output + result.Errors); result.Output.Should().Contain("PASS").And.NotContain("synthetic-password"); result.Errors.Should().BeEmpty();
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LauncherRestoresOriginalConnectionOnReturnOrBackendFailure(bool fail)
    {
        await InFixtureAsync(async directory =>
        {
            var script = """
                $global:DemoExists='t';$global:BackendFail=__FAIL__
                $env:ConnectionStrings__DefaultConnection='synthetic-original'
                try { & '__SCRIPT__' -EnvFile '__ENV__' } catch { if (-not $global:BackendFail) { throw }; $global:Caught=$true }
                if (-not $global:BackendChecked) { throw 'Backend not called.' }
                if ($global:BackendFail -and -not $global:Caught) { throw 'Backend failure hidden.' }
                if ($env:ConnectionStrings__DefaultConnection -cne 'synthetic-original') { throw 'Caller connection not restored.' }
                'PASS'
                """.Replace("__FAIL__", fail ? "$true" : "$false", StringComparison.Ordinal)
                .Replace("__SCRIPT__", Escape(Path.Combine(directory, "scripts/start-demo.ps1")), StringComparison.Ordinal)
                .Replace("__ENV__", Escape(Path.Combine(directory, ".env")), StringComparison.Ordinal);
            var result = await ShellAsync(directory, script);
            result.Code.Should().Be(0, "synthetic fixture output: {0}", result.Output + result.Errors); result.Output.Should().Contain("PASS").And.NotContain("synthetic-password"); result.Errors.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task OriginalEnvCannotAlreadyNameDemoDatabase()
    {
        await InFixtureAsync(async directory =>
        {
            var path = Path.Combine(directory, ".env");
            await File.WriteAllTextAsync(path, (await File.ReadAllTextAsync(path)).Replace("original", "jp_dict_demo", StringComparison.Ordinal));
            var result = await ShellAsync(directory, "& '" + Escape(Path.Combine(directory, "scripts/prepare-demo.ps1")) + "' -EnvFile '" + Escape(path) + "' -ValidateOnly");
            result.Code.Should().Be(1); result.Output.Should().Contain("must differ").And.NotContain("synthetic-password");
        });
    }

    private static async Task<(int Code, string Output, string Errors)> ShellAsync(string directory, string body)
    {
        var script = "$ErrorActionPreference='Stop';$global:Calls=[Collections.Generic.List[object]]::new();$global:DemoExists='t';$global:BackendChecked=$false;$global:BackendFail=$false;$global:Caught=$false;try { " + body + " } catch { [Console]::Out.WriteLine($_.Exception.Message);exit 1 }";
        var start = new ProcessStartInfo("pwsh.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = directory };
        start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-EncodedCommand");
        start.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(TestContext.Current.CancellationToken); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        return (process.ExitCode, await output, await errors);
    }

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static async Task InFixtureAsync(Func<string, Task> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "jp-demo-script-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "scripts"));
        try
        {
            foreach (var file in new[] { "prepare-demo.ps1", "start-demo.ps1", "local-backend-config.ps1" })
                File.Copy(Path.Combine(ConjugationCorpusFiles.Root, "scripts", file), Path.Combine(directory, "scripts", file));
            var corpusDir = Path.Combine(directory, ".sdd/specs/feat-conjugation-explanation/corpus/expanded-20261013.3");
            Directory.CreateDirectory(corpusDir);
            File.Copy(Path.Combine(ConjugationCorpusFiles.Packet, "cases.json"), Path.Combine(corpusDir, "cases.json"));
            await File.WriteAllTextAsync(Path.Combine(directory, ".env"), "POSTGRES_DB=original\nPOSTGRES_USER=synthetic-user\nPOSTGRES_PASSWORD=synthetic-password\nPOSTGRES_PORT=5432\n");
            await File.WriteAllTextAsync(Path.Combine(directory, "scripts/local-recovery-tools.ps1"), """
                function Resolve-LocalRecoveryExecutable { param($Name) $Name + '.exe' }
                function Invoke-LocalRecoveryProcess {
                    param($FileName,$Arguments,$WorkingDirectory,$Environment=@{},$InputText,[switch]$ReturnOutput)
                    $global:Calls.Add([pscustomobject]@{FileName=$FileName;Arguments=$Arguments;Environment=$Environment;InputText=$InputText})
                    if ($ReturnOutput) { $global:DemoExists }
                }
                """);
            await File.WriteAllTextAsync(Path.Combine(directory, "scripts/start-local.ps1"), """
                param($EnvFile,$DatasetVersion,[switch]$SkipDependencies)
                $builder=[System.Data.Common.DbConnectionStringBuilder]::new();$builder.set_ConnectionString($env:ConnectionStrings__DefaultConnection)
                if ($builder['Database'] -cne 'jp_dict_demo' -or $DatasetVersion -cne 'candidate-20261110.1' -or -not $SkipDependencies) { throw 'Wrong backend target.' }
                $global:BackendChecked=$true
                if ($global:BackendFail) { throw 'Synthetic backend failure.' }
                """);
            await action(directory);
        }
        finally
        {
            var resolved = Path.GetFullPath(directory);
            var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("jp-demo-script-tests-", StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected demo fixture cleanup target.");
            Directory.Delete(resolved, recursive: true);
        }
    }
}
