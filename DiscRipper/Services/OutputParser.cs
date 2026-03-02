using System.Text.RegularExpressions;

namespace DiscRipper.Services;

public enum OutputEvent
{
    StepStarted,
    Complete,
    Queued,
    Failed,
    PromptDetected
}

public class OutputEventArgs : EventArgs
{
    public OutputEvent Event { get; init; }
    public int StepNumber { get; init; }
    public string RawLine { get; init; } = "";
}

public static partial class OutputParser
{
    private static readonly Regex StepPattern = StepRegex();
    private static readonly Regex PromptPattern = PromptRegex();

    public static OutputEventArgs? Parse(string line)
    {
        if (string.IsNullOrEmpty(line))
            return null;

        // Step transitions: [STEP 1/4], [STEP 2/4], etc.
        var stepMatch = StepPattern.Match(line);
        if (stepMatch.Success)
        {
            return new OutputEventArgs
            {
                Event = OutputEvent.StepStarted,
                StepNumber = int.Parse(stepMatch.Groups[1].Value),
                RawLine = line
            };
        }

        // Completion markers
        if (line.Contains("COMPLETE!"))
            return new OutputEventArgs { Event = OutputEvent.Complete, RawLine = line };

        if (line.Contains("QUEUED!"))
            return new OutputEventArgs { Event = OutputEvent.Queued, RawLine = line };

        if (line.Contains("FAILED!"))
            return new OutputEventArgs { Event = OutputEvent.Failed, RawLine = line };

        // Prompt detection — Read-Host patterns
        if (PromptPattern.IsMatch(line))
            return new OutputEventArgs { Event = OutputEvent.PromptDetected, RawLine = line };

        return null;
    }

    [GeneratedRegex(@"\[STEP (\d)/4\]")]
    private static partial Regex StepRegex();

    [GeneratedRegex(@"(Select \(0-|Accept.*Edit.*Abort|Enter title|Is this a TV series|Season number|Enter title manually|Continue with this title|Press Enter to continue|Enter 1 or 2)", RegexOptions.IgnoreCase)]
    private static partial Regex PromptRegex();
}
