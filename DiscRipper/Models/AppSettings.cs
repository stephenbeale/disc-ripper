namespace DiscRipper.Models;

public class AppSettings
{
    public string ScriptDirectory { get; set; } = @"C:\Users\sjbeale\source\repos\ripdisc";
    public string PowerShellPath { get; set; } = "powershell.exe";
    public string DefaultDrive { get; set; } = "D:";
    public int DefaultDriveIndex { get; set; } = -1;
    public string DefaultOutputDrive { get; set; } = "E:";
}
