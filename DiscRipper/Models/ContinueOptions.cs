namespace DiscRipper.Models;

public class ContinueOptions
{
    public string Title { get; set; } = "";
    public string FromStep { get; set; } = "handbrake";
    public ContentType ContentType { get; set; } = ContentType.Movie;
    public bool Bluray { get; set; }
    public int Season { get; set; }
    public int StartEpisode { get; set; } = 1;
    public int Disc { get; set; } = 1;
    public string OutputDrive { get; set; } = "E:";
    public bool Extras { get; set; }
}
