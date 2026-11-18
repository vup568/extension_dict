using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Xunit;

namespace IntegrationTests;

// Windows-only operational scope was explicitly chosen by the project owner.
public sealed class LocalRecoveryScriptTests
{
    [Fact]
    public async Task WindowsExecutableResolutionSelectsOneExeEvenWithMultiplePathMatches()
    {
        var script = """
            function Get-Command {
                param($Name, $CommandType, $ErrorAction)
                if ($Name -cne 'docker.exe') { throw 'Expected explicit Windows executable.' }
                [pscustomobject]@{Source='first-docker.exe'}
                [pscustomobject]@{Source='second-docker.exe'}
            }
            $resolved=Resolve-LocalRecoveryExecutable -Name docker
            if ($resolved -isnot [string] -or $resolved -cne 'first-docker.exe') { throw 'Ambiguous executable selection.' }
            'PASS'
            """;
        var result = await ShellAsync(script);
        result.Code.Should().Be(0); result.Output.Trim().Should().Be("PASS"); result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task BinaryBackupRoundTripsWithoutTextEncodingAndNeverOverwritesAnExistingFile()
    {
        await InFixtureAsync(async directory => {
            byte[] bytes = [0, 255, 128, 10, 13, 0, 201, 42];
            var input = Path.Combine(directory, "synthetic-input.dump");
            var backup = Path.Combine(directory, "synthetic-backup.dump");
            await File.WriteAllBytesAsync(input, bytes, TestContext.Current.CancellationToken);
            var script = """
                $child=(Get-Command pwsh.exe -CommandType Application).Source
                $code=[Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes('[Console]::OpenStandardInput().CopyTo([Console]::OpenStandardOutput())'))
                $arguments=@('-NoProfile','-EncodedCommand',$code)
                Invoke-LocalRecoveryProcess -FileName $child -Arguments $arguments -WorkingDirectory '__ROOT__' -InputPath '__INPUT__' -OutputPath '__BACKUP__'
                try {
                    Invoke-LocalRecoveryProcess -FileName $child -Arguments $arguments -WorkingDirectory '__ROOT__' -InputPath '__INPUT__' -OutputPath '__BACKUP__'
                    throw 'Existing backup unexpectedly overwritten.'
                } catch {
                    if ($_.Exception.Message -notlike 'Recovery process failed*') { throw }
                }
                'PASS'
                """.Replace("__ROOT__", Escape(ConjugationCorpusFiles.Root), StringComparison.Ordinal)
                .Replace("__INPUT__", Escape(input), StringComparison.Ordinal)
                .Replace("__BACKUP__", Escape(backup), StringComparison.Ordinal);
            var result = await ShellAsync(script);
            result.Code.Should().Be(0); result.Output.Trim().Should().Be("PASS"); result.Errors.Should().BeEmpty();
            (await File.ReadAllBytesAsync(backup, TestContext.Current.CancellationToken)).Should().Equal(bytes);
        });
    }

    [Fact]
    public async Task RowContentHashStreamsPrivatelyAndChildEnvironmentDoesNotChangeTheCaller()
    {
        var sentinel = "synthetic-private-" + Guid.NewGuid().ToString("N");
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sentinel))).ToLowerInvariant();
        var script = """
            $env:LocalRecoveryFixture='prior'
            $child=(Get-Command pwsh.exe -CommandType Application).Source
            $code=[Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes('[Console]::Out.Write($env:LocalRecoveryFixture)'))
            $hash=Invoke-LocalRecoveryProcess -FileName $child -Arguments @('-NoProfile','-EncodedCommand',$code) -WorkingDirectory '__ROOT__' -Environment @{LocalRecoveryFixture='__VALUE__'} -HashOutput
            if ($env:LocalRecoveryFixture -cne 'prior') { throw 'Caller environment changed.' }
            $hash
            """.Replace("__ROOT__", Escape(ConjugationCorpusFiles.Root), StringComparison.Ordinal)
            .Replace("__VALUE__", Escape(sentinel), StringComparison.Ordinal);
        var result = await ShellAsync(script);
        result.Code.Should().Be(0); result.Output.Trim().Should().Be(expected).And.NotContain(sentinel); result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task NativeFailuresReportOnlySafeTypeAndExitCodeWithoutPrivateOutput()
    {
        var sentinel = "synthetic-private-" + Guid.NewGuid().ToString("N");
        var script = """
            $child=(Get-Command pwsh.exe -CommandType Application).Source
            $code=[Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes('[Console]::Out.Write("__VALUE__");[Console]::Error.Write("__VALUE__");exit 9'))
            Invoke-LocalRecoveryProcess -FileName $child -Arguments @('-NoProfile','-EncodedCommand',$code) -WorkingDirectory '__ROOT__' -ReturnOutput
            """.Replace("__ROOT__", Escape(ConjugationCorpusFiles.Root), StringComparison.Ordinal)
            .Replace("__VALUE__", sentinel, StringComparison.Ordinal);
        var result = await ShellAsync(script);
        result.Code.Should().Be(1); result.Output.Should().Contain("exit code 9").And.NotContain(sentinel); result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("jp_recovery_clone_0123456789abcdef0123456789abcdef", true)]
    [InlineData("jp_recovery_reference_0123456789abcdef0123456789abcdef", true)]
    [InlineData("postgres", false)]
    [InlineData("jp_recovery_clone_0123456789abcdef0123456789abcdef;DROP DATABASE", false)]
    public async Task RehearsalNamesAreStrictlyValidated(string name, bool valid)
    {
        var result = await ShellAsync("Assert-LocalRecoveryDatabaseName -Name '" + Escape(name) + "'; 'PASS'");
        result.Code.Should().Be(valid ? 0 : 1);
        if (valid) { result.Output.Trim().Should().Be("PASS"); }
        else { result.Output.Should().Contain("Invalid isolated recovery database name"); }
    }

    [Fact]
    public async Task SnapshotMismatchFailsClosedAndCompositeMembershipsUseTheirActualKey()
    {
        var script = """
            if ((Get-LocalRecoveryOrderBy -Table 'knowledge_release_manifests') -cne 't.release_id, t.manifest_id') { throw 'Composite key ordering is wrong.' }
            if ((Get-LocalRecoveryOrderBy -Table 'dictionary_entries') -cne 't.id') { throw 'Identity key ordering is wrong.' }
            Assert-LocalRecoverySnapshotsEqual -Expected @{dictionary_entries=@{Count=1;Hash='before'}} -Actual @{dictionary_entries=@{Count=1;Hash='different'}}
            """;
        var result = await ShellAsync(script);
        result.Code.Should().Be(1); result.Output.Should().Contain("Recovery data verification failed"); result.Errors.Should().BeEmpty();
    }

    private static async Task<(int Code, string Output, string Errors)> ShellAsync(string body)
    {
        var library = Path.Combine(ConjugationCorpusFiles.Root, "scripts/local-recovery-tools.ps1");
        var script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';try { . '" + Escape(library) + "'; " + body +
            " } catch { [Console]::Out.WriteLine($_.Exception.Message);exit 1 }";
        var start = new ProcessStartInfo("pwsh.exe") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-EncodedCommand");
        start.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(TestContext.Current.CancellationToken); }
        catch { if (!process.HasExited) { process.Kill(entireProcessTree: true); } throw; }
        return (process.ExitCode, await output, await errors);
    }

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
    private static async Task InFixtureAsync(Func<string, Task> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "jp-local-recovery-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { await action(directory); }
        finally {
            var resolved = Path.GetFullPath(directory);
            var prefix = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("jp-local-recovery-tests-", StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected recovery fixture cleanup target.");
            Directory.Delete(resolved, recursive: true);
        }
    }
}
