using Application.DictionaryImport;
using NSubstitute;
using Xunit;

namespace UnitTests;

public sealed class ExecuteDictionaryImportUseCaseTests
{
    [Theory]
    [InlineData("import")]
    [InlineData("resume")]
    public async Task ApprovedInputsDispatchOnlyExplicitOperation(string verb)
    {
        var port = Substitute.For<IDictionaryImportStore>();
        var input = Inputs();
        var command = new DictionaryImportInvocation(verb, "lock.json", "run", "jp_dict_real_fixture");
        port.ImportAsync(command, input, Arg.Any<CancellationToken>()).Returns(Report());
        var result = await new ExecuteDictionaryImportUseCase(port).ExecuteAsync(command, input, TestContext.Current.CancellationToken);
        Assert.Equal(0, result.ExitCode);
        await port.Received(1).ImportAsync(command, input, TestContext.Current.CancellationToken);
        await port.DidNotReceiveWithAnyArgs().ValidateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpgradeDispatchesOnlySchemaUpgrade()
    {
        var port = Substitute.For<IDictionaryImportStore>();
        var input = Inputs();
        var command = new DictionaryImportInvocation("upgrade", "lock.json", "run", "jp_dict_real_fixture");
        port.UpgradeSchemaAsync(command, input, Arg.Any<CancellationToken>()).Returns(Report());
        var result = await new ExecuteDictionaryImportUseCase(port).ExecuteAsync(command, input, TestContext.Current.CancellationToken);
        Assert.Equal(0, result.ExitCode);
        await port.Received(1).UpgradeSchemaAsync(command, input, TestContext.Current.CancellationToken);
        await port.DidNotReceiveWithAnyArgs().ImportAsync(default!, default!, TestContext.Current.CancellationToken);
        await port.DidNotReceiveWithAnyArgs().ValidateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MechanismValidationDoesNotGrantActivation()
    {
        var port = Substitute.For<IDictionaryImportStore>();
        var input = Inputs();
        var command = new DictionaryImportInvocation("validate", "lock.json", "run", null);
        port.ValidateAsync(command, input, Arg.Any<CancellationToken>()).Returns(Report());
        var result = await new ExecuteDictionaryImportUseCase(port).ExecuteAsync(command, input, TestContext.Current.CancellationToken);
        Assert.Equal(5, result.ExitCode);
    }

    [Theory]
    [InlineData("blocked")]
    [InlineData("missing-run")]
    [InlineData("batch")]
    [InlineData("unknown")]
    public async Task InvalidCommandsNeverCrossPersistenceBoundary(string fault)
    {
        var port = Substitute.For<IDictionaryImportStore>();
        var input = fault == "blocked" ? Inputs() with { Blockers = [new("license_unapproved", "Synthetic pending decision")] } : Inputs();
        var command = new DictionaryImportInvocation(fault == "unknown" ? "activate" : "import", "lock.json",
            fault == "missing-run" ? null : "run", "jp_dict_real_fixture", fault == "batch" ? 2001 : 500);
        await Assert.ThrowsAnyAsync<Exception>(() => new ExecuteDictionaryImportUseCase(port).ExecuteAsync(command, input, TestContext.Current.CancellationToken));
        Assert.Empty(port.ReceivedCalls());
    }

    private static SourceLockInspection Inputs() => new(new DictionarySourceLock(), new string('a', 64), new string('b', 64), []);
    private static ImportMechanismReport Report() => new("run", "jp_dict_real_fixture", "imported", "lock", "inputs", "facts", 3, 0, false, false, ["owner-review"]);
}
