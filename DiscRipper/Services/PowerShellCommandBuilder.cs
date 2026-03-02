using DiscRipper.Models;

namespace DiscRipper.Services;

public static class PowerShellCommandBuilder
{
    public static string BuildRipCommand(RipOptions options, AppSettings settings)
    {
        var scriptPath = Path.Combine(settings.ScriptDirectory, "rip-disc.ps1");
        var args = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.Title))
            args.Add($"-title '{EscapeSingleQuotes(options.Title)}'");

        AddContentTypeArgs(args, options.ContentType);

        if (options.Bluray)
            args.Add("-Bluray");

        if (options.ContentType == ContentType.Series)
        {
            args.Add($"-Season {options.Season}");
            args.Add($"-StartEpisode {options.StartEpisode}");
        }

        args.Add($"-Disc {options.Disc}");
        args.Add($"-Drive '{EscapeSingleQuotes(options.Drive)}'");

        if (options.DriveIndex >= 0)
            args.Add($"-DriveIndex {options.DriveIndex}");

        args.Add($"-OutputDrive '{EscapeSingleQuotes(options.OutputDrive)}'");

        if (options.Extras)
            args.Add("-Extras");

        if (options.Queue)
            args.Add("-Queue");

        return BuildFullCommand(settings.PowerShellPath, scriptPath, args);
    }

    public static string BuildContinueCommand(ContinueOptions options, AppSettings settings)
    {
        var scriptPath = Path.Combine(settings.ScriptDirectory, "continue-rip.ps1");
        var args = new List<string>();

        args.Add($"-title '{EscapeSingleQuotes(options.Title)}'");
        args.Add($"-FromStep '{options.FromStep}'");

        AddContentTypeArgs(args, options.ContentType);

        if (options.Bluray)
            args.Add("-Bluray");

        if (options.ContentType == ContentType.Series)
        {
            args.Add($"-Season {options.Season}");
            args.Add($"-StartEpisode {options.StartEpisode}");
        }

        args.Add($"-Disc {options.Disc}");
        args.Add($"-OutputDrive '{EscapeSingleQuotes(options.OutputDrive)}'");

        if (options.Extras)
            args.Add("-Extras");

        return BuildFullCommand(settings.PowerShellPath, scriptPath, args);
    }

    private static void AddContentTypeArgs(List<string> args, ContentType contentType)
    {
        switch (contentType)
        {
            case ContentType.Series:
                args.Add("-Series");
                break;
            case ContentType.Documentary:
                args.Add("-Documentary");
                break;
            case ContentType.Tutorial:
                args.Add("-Tutorial");
                break;
            case ContentType.Fitness:
                args.Add("-Fitness");
                break;
            case ContentType.Music:
                args.Add("-Music");
                break;
            case ContentType.Surf:
                args.Add("-Surf");
                break;
        }
    }

    private static string BuildFullCommand(string psPath, string scriptPath, List<string> args)
    {
        var scriptArgs = string.Join(" ", args);
        var isPwsh = psPath.Contains("pwsh", StringComparison.OrdinalIgnoreCase);

        if (isPwsh)
        {
            return $"\"{psPath}\" -ExecutionPolicy Bypass -File \"{scriptPath}\" {scriptArgs}";
        }

        // PS 5.1: use -Command with *>&1 to merge all streams into stdout
        var innerCommand = $"& '{EscapeSingleQuotes(scriptPath)}' {scriptArgs}";
        return $"\"{psPath}\" -ExecutionPolicy Bypass -NoProfile -Command \"& {{ {innerCommand} }} *>&1\"";
    }

    private static string EscapeSingleQuotes(string value) => value.Replace("'", "''");
}
