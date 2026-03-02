namespace DiscRipper.Controls;

public class OutputConsole : RichTextBox
{
    private static readonly Color StepColor = Color.FromArgb(86, 156, 214);
    private static readonly Color SuccessColor = Color.FromArgb(78, 201, 176);
    private static readonly Color ErrorColor = Color.FromArgb(244, 71, 71);
    private static readonly Color WarningColor = Color.FromArgb(220, 220, 170);
    private static readonly Color DefaultColor = Color.FromArgb(204, 204, 204);

    public OutputConsole()
    {
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = DefaultColor;
        Font = new Font("Cascadia Mono", 9.5f, FontStyle.Regular);
        ReadOnly = true;
        WordWrap = true;
        BorderStyle = BorderStyle.None;
        ScrollBars = RichTextBoxScrollBars.Vertical;
    }

    public void AppendOutput(string line)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendOutput(line));
            return;
        }

        var color = ClassifyLine(line);
        SelectionStart = TextLength;
        SelectionLength = 0;
        SelectionColor = color;
        AppendText(line + Environment.NewLine);
        ScrollToCaret();
    }

    public void ClearOutput()
    {
        if (InvokeRequired)
        {
            Invoke(ClearOutput);
            return;
        }
        Clear();
    }

    private static Color ClassifyLine(string line)
    {
        if (line.Contains("[STEP ") || line.Contains("========"))
            return StepColor;
        if (line.Contains("COMPLETE!") || line.Contains("QUEUED!"))
            return SuccessColor;
        if (line.Contains("FAILED!") || line.Contains("ERROR") || line.Contains("Error"))
            return ErrorColor;
        if (line.Contains("WARNING") || line.Contains("Warning"))
            return WarningColor;
        return DefaultColor;
    }
}
