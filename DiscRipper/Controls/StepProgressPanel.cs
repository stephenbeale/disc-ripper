using DiscRipper.Models;

namespace DiscRipper.Controls;

public class StepProgressPanel : Panel
{
    private StepInfo[] _steps = StepInfo.CreatePipeline();

    private static readonly Color PendingColor = Color.FromArgb(100, 100, 100);
    private static readonly Color ActiveColor = Color.FromArgb(0, 120, 215);
    private static readonly Color CompletedColor = Color.FromArgb(0, 180, 80);
    private static readonly Color FailedColor = Color.FromArgb(220, 50, 50);
    private static readonly Color SkippedColor = Color.FromArgb(160, 160, 160);

    public StepProgressPanel()
    {
        DoubleBuffered = true;
        Height = 60;
    }

    public void UpdateSteps(StepInfo[] steps)
    {
        _steps = steps;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var circleSize = 28;
        var totalWidth = Width - 40;
        var stepSpacing = totalWidth / (_steps.Length - 1);
        var startX = 20;
        var centerY = Height / 2;

        // Draw connecting lines first
        for (int i = 0; i < _steps.Length - 1; i++)
        {
            var x1 = startX + (i * stepSpacing) + circleSize / 2;
            var x2 = startX + ((i + 1) * stepSpacing) - circleSize / 2;
            var lineColor = _steps[i].State == StepState.Completed ? CompletedColor : PendingColor;
            using var pen = new Pen(lineColor, 2);
            g.DrawLine(pen, x1, centerY, x2, centerY);
        }

        // Draw circles and labels
        for (int i = 0; i < _steps.Length; i++)
        {
            var x = startX + (i * stepSpacing) - circleSize / 2;
            var rect = new Rectangle(x, centerY - circleSize / 2, circleSize, circleSize);
            var color = GetColor(_steps[i].State);

            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, rect);

            // Step number
            using var font = new Font("Segoe UI", 10, FontStyle.Bold);
            var numText = _steps[i].Number.ToString();
            var numSize = g.MeasureString(numText, font);
            g.DrawString(numText, font, Brushes.White,
                rect.X + (rect.Width - numSize.Width) / 2,
                rect.Y + (rect.Height - numSize.Height) / 2);

            // Step name below
            using var labelFont = new Font("Segoe UI", 8);
            var labelSize = g.MeasureString(_steps[i].Name, labelFont);
            using var labelBrush = new SolidBrush(color);
            g.DrawString(_steps[i].Name, labelFont, labelBrush,
                rect.X + (rect.Width - labelSize.Width) / 2,
                rect.Bottom + 2);
        }
    }

    private static Color GetColor(StepState state) => state switch
    {
        StepState.Pending => PendingColor,
        StepState.Active => ActiveColor,
        StepState.Completed => CompletedColor,
        StepState.Failed => FailedColor,
        StepState.Skipped => SkippedColor,
        _ => PendingColor
    };
}
