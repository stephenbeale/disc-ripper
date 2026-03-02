namespace DiscRipper.Models;

public enum StepState
{
    Pending,
    Active,
    Completed,
    Failed,
    Skipped
}

public class StepInfo
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
    public StepState State { get; set; } = StepState.Pending;

    public static StepInfo[] CreatePipeline() =>
    [
        new() { Number = 1, Name = "MakeMKV" },
        new() { Number = 2, Name = "HandBrake" },
        new() { Number = 3, Name = "Organize" },
        new() { Number = 4, Name = "Open" }
    ];
}
