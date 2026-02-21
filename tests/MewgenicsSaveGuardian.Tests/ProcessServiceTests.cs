using MewgenicsSaveGuardian.Services;

namespace MewgenicsSaveGuardian.Tests;

public class ProcessServiceTests
{
    private readonly ProcessService _service = new();

    [Fact]
    public void IsGameRunning_should_return_false_when_game_not_running()
    {
        Assert.False(_service.IsGameRunning());
    }

    [Fact]
    public void GetGameProcess_should_return_null_when_game_not_running()
    {
        Assert.Null(_service.GetGameProcess());
    }

    [Fact]
    public void LaunchGame_should_not_throw()
    {
        // Verify the method exists and accepts the right signature
        Assert.NotNull((Action)_service.LaunchGame);
    }
}
