namespace DiscRipper.Services;

public record DriveInfo2(string Letter, string Label, bool IsReady);

public static class DriveDetector
{
    public static List<DriveInfo2> GetOpticalDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.CDRom)
            .Select(d => new DriveInfo2(
                d.Name.TrimEnd('\\'),
                d.IsReady ? d.VolumeLabel : "(no disc)",
                d.IsReady))
            .ToList();
    }

    public static List<DriveInfo2> GetFixedDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
            .Select(d => new DriveInfo2(
                d.Name.TrimEnd('\\'),
                d.VolumeLabel,
                true))
            .ToList();
    }
}
