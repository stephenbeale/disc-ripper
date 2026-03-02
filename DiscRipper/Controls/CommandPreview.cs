namespace DiscRipper.Controls;

public class CommandPreview : Panel
{
    private readonly TextBox _textBox;
    private readonly Button _copyButton;

    public CommandPreview()
    {
        Height = 50;
        Padding = new Padding(4);

        _textBox = new TextBox
        {
            ReadOnly = true,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.FromArgb(180, 220, 180),
            Font = new Font("Cascadia Mono", 9f),
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill
        };

        _copyButton = new Button
        {
            Text = "Copy",
            Width = 50,
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8)
        };
        _copyButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        _copyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrEmpty(_textBox.Text))
                Clipboard.SetText(_textBox.Text);
        };

        Controls.Add(_textBox);
        Controls.Add(_copyButton);
    }

    public void SetCommand(string command)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetCommand(command));
            return;
        }
        _textBox.Text = command;
    }
}
