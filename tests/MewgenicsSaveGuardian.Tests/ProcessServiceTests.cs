using System.Diagnostics;
using MewgenicsSaveGuardian.Services;

namespace MewgenicsSaveGuardian.Tests;

public class ProcessServiceTests
{
    private readonly ProcessService _service = new();

    [Fact]
    public void IsGameRunning_should_return_bool()
    {
        // Just verify it returns without throwing; result depends on environment
        var result = _service.IsGameRunning();
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void GetGameProcess_should_return_process_or_null()
    {
        var process = _service.GetGameProcess();
        if (process is not null)
        {
            Assert.Equal("Mewgenics", process.ProcessName);
        }
        // null is also valid when game is not running
    }

    [Fact]
    public void IsGameRunning_and_GetGameProcess_should_be_consistent()
    {
        var isRunning = _service.IsGameRunning();
        var process = _service.GetGameProcess();
        Assert.Equal(isRunning, process is not null);
    }

    [Fact]
    public void LaunchGame_method_should_exist()
    {
        Assert.NotNull((Action)_service.LaunchGame);
    }

    [Fact]
    public void CloseGame_method_should_exist()
    {
        Assert.NotNull((Func<bool>)_service.CloseGame);
    }
}
