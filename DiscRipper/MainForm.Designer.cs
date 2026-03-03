using DiscRipper.Controls;

namespace DiscRipper;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        Text = "disc-ripper";
        ClientSize = new Size(1000, 700);
        MinimumSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9);
        BackColor = Color.FromArgb(45, 45, 48);
        ForeColor = Color.White;

        // === Tab Control ===
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f)
        };

        _ripTab = new TabPage("Rip") { BackColor = Color.FromArgb(45, 45, 48) };
        _continueTab = new TabPage("Continue") { BackColor = Color.FromArgb(45, 45, 48) };
        _settingsTab = new TabPage("Settings") { BackColor = Color.FromArgb(45, 45, 48) };
        _tabControl.TabPages.AddRange([_ripTab, _continueTab, _settingsTab]);

        // === Status Bar ===
        _statusBar = new StatusStrip { BackColor = Color.FromArgb(0, 122, 204) };
        _statusLabel = new ToolStripStatusLabel("Ready") { ForeColor = Color.White };
        _durationLabel = new ToolStripStatusLabel("Duration: 00:00:00")
        {
            ForeColor = Color.White,
            Alignment = ToolStripItemAlignment.Right,
            Spring = true,
            TextAlign = ContentAlignment.MiddleRight
        };
        _statusBar.Items.AddRange([_statusLabel, _durationLabel]);

        _durationTimer = new System.Windows.Forms.Timer(components) { Interval = 1000 };
        _durationTimer.Tick += DurationTimer_Tick;

        // ========== RIP TAB ==========
        BuildRipTab();

        // ========== CONTINUE TAB ==========
        BuildContinueTab();

        // ========== SETTINGS TAB ==========
        BuildSettingsTab();

        Controls.Add(_tabControl);
        Controls.Add(_statusBar);
    }

    private void BuildRipTab()
    {
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            BackColor = Color.FromArgb(45, 45, 48),
            Panel1MinSize = 250
        };
        splitContainer.Panel1.BackColor = Color.FromArgb(37, 37, 38);
        splitContainer.Panel2.BackColor = Color.FromArgb(45, 45, 48);

        // === Left Panel ===
        var leftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(12)
        };

        var y = 8;
        AddLabel(leftPanel, "TITLE", ref y);
        _ripTitle = AddTextBox(leftPanel, ref y);
        _ripAutoDiscover = AddCheckBox(leftPanel, "Auto-detect title", ref y);
        _ripAutoDiscover.Checked = true;
        y += 8;

        AddLabel(leftPanel, "TYPE", ref y);
        _ripContentType = AddComboBox(leftPanel, Enum.GetNames<Models.ContentType>(), ref y);
        _ripContentType.SelectedIndexChanged += (_, _) => UpdateRipSeriesVisibility();
        y += 4;

        _ripBluray = AddCheckBox(leftPanel, "Blu-ray", ref y);
        y += 8;

        // Series options group
        _ripSeriesGroup = new GroupBox
        {
            Text = "SERIES OPTIONS",
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(12, y),
            Size = new Size(330, 80),
            Font = new Font("Segoe UI", 8.5f)
        };
        _ripSeason = AddNumericInGroup(_ripSeriesGroup, "Season:", 0, 0, 99, 12, 24);
        _ripStartEpisode = AddNumericInGroup(_ripSeriesGroup, "Start Episode:", 1, 1, 999, 12, 50);
        leftPanel.Controls.Add(_ripSeriesGroup);
        y += 90;

        _ripDisc = AddNumericField(leftPanel, "Disc number:", 1, 1, 99, ref y);
        y += 4;

        AddLabel(leftPanel, "SOURCE DRIVE", ref y);
        _ripDrivePanel = new FlowLayoutPanel
        {
            Location = new Point(24, y),
            Size = new Size(320, 60),
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true
        };
        leftPanel.Controls.Add(_ripDrivePanel);
        y += 64;

        AddLabel(leftPanel, "SAVE TO", ref y);
        _ripOutputDrive = AddComboBox(leftPanel, [], ref y);
        y += 8;

        AddLabel(leftPanel, "OPTIONS", ref y);
        _ripExtras = AddCheckBox(leftPanel, "This is an extras/bonus disc", ref y);
        _ripQueue = AddCheckBox(leftPanel, "Add to queue (encode later)", ref y);
        y += 16;

        _ripStartButton = AddButton(leftPanel, "Start Rip", ref y, primary: true);
        _ripStartButton.Click += RipStartButton_Click;
        _ripStopButton = AddButton(leftPanel, "Stop", ref y, primary: false);
        _ripStopButton.Enabled = false;
        _ripStopButton.Click += StopButton_Click;

        splitContainer.Panel1.Controls.Add(leftPanel);

        // === Right Panel ===
        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(8)
        };
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _ripStepProgress = new StepProgressPanel { Dock = DockStyle.Fill };
        _ripCommandPreview = new CommandPreview { Dock = DockStyle.Fill };
        _ripOutputConsole = new OutputConsole { Dock = DockStyle.Fill };

        rightPanel.Controls.Add(_ripStepProgress, 0, 0);
        rightPanel.Controls.Add(_ripCommandPreview, 0, 1);
        rightPanel.Controls.Add(_ripOutputConsole, 0, 2);

        splitContainer.Panel2.Controls.Add(rightPanel);
        _ripTab.Controls.Add(splitContainer);
        splitContainer.SplitterDistance = 360;
    }

    private void BuildContinueTab()
    {
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            BackColor = Color.FromArgb(45, 45, 48),
            Panel1MinSize = 250
        };
        splitContainer.Panel1.BackColor = Color.FromArgb(37, 37, 38);
        splitContainer.Panel2.BackColor = Color.FromArgb(45, 45, 48);

        // === Left Panel ===
        var leftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(12)
        };

        var y = 8;
        AddLabel(leftPanel, "TITLE", ref y);
        _contTitle = AddTextBox(leftPanel, ref y);
        y += 8;

        AddLabel(leftPanel, "RESUME FROM", ref y);
        _contFromStep = AddComboBox(leftPanel, ["HandBrake", "Organize", "Open"], ref y);
        y += 8;

        AddLabel(leftPanel, "TYPE", ref y);
        _contContentType = AddComboBox(leftPanel, Enum.GetNames<Models.ContentType>(), ref y);
        _contContentType.SelectedIndexChanged += (_, _) => UpdateContinueSeriesVisibility();
        y += 4;

        _contBluray = AddCheckBox(leftPanel, "Blu-ray", ref y);
        y += 8;

        _contSeriesGroup = new GroupBox
        {
            Text = "SERIES OPTIONS",
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(12, y),
            Size = new Size(330, 80),
            Font = new Font("Segoe UI", 8.5f)
        };
        _contSeason = AddNumericInGroup(_contSeriesGroup, "Season:", 0, 0, 99, 12, 24);
        _contStartEpisode = AddNumericInGroup(_contSeriesGroup, "Start Episode:", 1, 1, 999, 12, 50);
        leftPanel.Controls.Add(_contSeriesGroup);
        y += 90;

        _contDisc = AddNumericField(leftPanel, "Disc number:", 1, 1, 99, ref y);

        AddLabel(leftPanel, "SAVE TO", ref y);
        _contOutputDrive = AddComboBox(leftPanel, [], ref y);
        y += 8;

        AddLabel(leftPanel, "OPTIONS", ref y);
        _contExtras = AddCheckBox(leftPanel, "This is an extras/bonus disc", ref y);
        y += 16;

        _contStartButton = AddButton(leftPanel, "Resume", ref y, primary: true);
        _contStartButton.Click += ContinueStartButton_Click;
        _contStopButton = AddButton(leftPanel, "Stop", ref y, primary: false);
        _contStopButton.Enabled = false;
        _contStopButton.Click += StopButton_Click;

        splitContainer.Panel1.Controls.Add(leftPanel);

        // === Right Panel ===
        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(8)
        };
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _contStepProgress = new StepProgressPanel { Dock = DockStyle.Fill };
        _contCommandPreview = new CommandPreview { Dock = DockStyle.Fill };
        _contOutputConsole = new OutputConsole { Dock = DockStyle.Fill };

        rightPanel.Controls.Add(_contStepProgress, 0, 0);
        rightPanel.Controls.Add(_contCommandPreview, 0, 1);
        rightPanel.Controls.Add(_contOutputConsole, 0, 2);

        splitContainer.Panel2.Controls.Add(rightPanel);
        _continueTab.Controls.Add(splitContainer);
        splitContainer.SplitterDistance = 360;
    }

    private void BuildSettingsTab()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var y = 20;
        AddLabel(panel, "SCRIPTS FOLDER", ref y, color: Color.FromArgb(180, 180, 180));
        _settingsScriptDir = AddTextBox(panel, ref y, width: 400);
        _settingsBrowseScript = new Button
        {
            Text = "Browse...",
            Location = new Point(440, _settingsScriptDir.Top),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        _settingsBrowseScript.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = _settingsScriptDir.Text };
            if (dialog.ShowDialog() == DialogResult.OK)
                _settingsScriptDir.Text = dialog.SelectedPath;
        };
        panel.Controls.Add(_settingsBrowseScript);
        y += 12;

        AddLabel(panel, "POWERSHELL PATH", ref y, color: Color.FromArgb(180, 180, 180));
        _settingsPsPath = AddTextBox(panel, ref y, width: 400);
        y += 12;

        AddLabel(panel, "DEFAULT SOURCE DRIVE", ref y, color: Color.FromArgb(180, 180, 180));
        _settingsDefaultDrive = AddTextBox(panel, ref y, width: 100);
        y += 12;

        AddLabel(panel, "DEFAULT DRIVE INDEX", ref y, color: Color.FromArgb(180, 180, 180));
        _settingsDefaultDriveIndex = AddNumericField(panel, "", -1, -1, 99, ref y);
        y += 12;

        AddLabel(panel, "DEFAULT SAVE DRIVE", ref y, color: Color.FromArgb(180, 180, 180));
        _settingsDefaultOutputDrive = AddTextBox(panel, ref y, width: 100);
        y += 24;

        _settingsSaveButton = AddButton(panel, "Save Settings", ref y, primary: true);
        _settingsSaveButton.Click += SettingsSaveButton_Click;

        _settingsTab.Controls.Add(panel);
    }

    // === Helper methods for building controls ===

    private static void AddLabel(Panel parent, string text, ref int y, bool indent = false, Color? color = null)
    {
        var label = new Label
        {
            Text = text,
            Location = new Point(indent ? 24 : 12, y),
            AutoSize = true,
            ForeColor = color ?? Color.FromArgb(0, 122, 204),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };
        parent.Controls.Add(label);
        y += 20;
    }

    private static TextBox AddTextBox(Panel parent, ref int y, int width = 330)
    {
        var tb = new TextBox
        {
            Location = new Point(12, y),
            Size = new Size(width, 24),
            BackColor = Color.FromArgb(51, 51, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f)
        };
        parent.Controls.Add(tb);
        y += 30;
        return tb;
    }

    private static CheckBox AddCheckBox(Panel parent, string text, ref int y)
    {
        var cb = new CheckBox
        {
            Text = text,
            Location = new Point(12, y),
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9)
        };
        parent.Controls.Add(cb);
        y += 24;
        return cb;
    }

    private static ComboBox AddComboBox(Panel parent, string[] items, ref int y)
    {
        var cb = new ComboBox
        {
            Location = new Point(12, y),
            Size = new Size(330, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(51, 51, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9)
        };
        cb.Items.AddRange(items);
        if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        parent.Controls.Add(cb);
        y += 30;
        return cb;
    }

    private static NumericUpDown AddNumericField(Panel parent, string label, int value, int min, int max, ref int y)
    {
        if (!string.IsNullOrEmpty(label))
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(12, y + 3),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            parent.Controls.Add(lbl);
        }

        var nud = new NumericUpDown
        {
            Location = new Point(string.IsNullOrEmpty(label) ? 12 : 120, y),
            Size = new Size(70, 24),
            Minimum = min,
            Maximum = max,
            Value = Math.Max(min, Math.Min(max, value)),
            BackColor = Color.FromArgb(51, 51, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9)
        };
        parent.Controls.Add(nud);
        y += 30;
        return nud;
    }

    private static NumericUpDown AddNumericInGroup(GroupBox group, string label, int value, int min, int max, int x, int y)
    {
        var lbl = new Label
        {
            Text = label,
            Location = new Point(x, y + 3),
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9)
        };
        group.Controls.Add(lbl);

        var nud = new NumericUpDown
        {
            Location = new Point(x + 110, y),
            Size = new Size(70, 24),
            Minimum = min,
            Maximum = max,
            Value = Math.Max(min, Math.Min(max, value)),
            BackColor = Color.FromArgb(51, 51, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9)
        };
        group.Controls.Add(nud);
        return nud;
    }

    private static Button AddButton(Panel parent, string text, ref int y, bool primary)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(12, y),
            Size = new Size(330, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(0, 122, 204) : Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, primary ? FontStyle.Bold : FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = primary ? Color.FromArgb(0, 100, 180) : Color.FromArgb(80, 80, 80);
        parent.Controls.Add(btn);
        y += 40;
        return btn;
    }

    // === Field declarations ===

    private TabControl _tabControl = null!;
    private TabPage _ripTab = null!, _continueTab = null!, _settingsTab = null!;
    private StatusStrip _statusBar = null!;
    private ToolStripStatusLabel _statusLabel = null!, _durationLabel = null!;
    private System.Windows.Forms.Timer _durationTimer = null!;

    // Rip tab
    private TextBox _ripTitle = null!;
    private CheckBox _ripAutoDiscover = null!, _ripBluray = null!, _ripExtras = null!, _ripQueue = null!;
    private ComboBox _ripContentType = null!, _ripOutputDrive = null!;
    private GroupBox _ripSeriesGroup = null!;
    private NumericUpDown _ripSeason = null!, _ripStartEpisode = null!, _ripDisc = null!;
    private FlowLayoutPanel _ripDrivePanel = null!;
    private Button _ripStartButton = null!, _ripStopButton = null!;
    private StepProgressPanel _ripStepProgress = null!;
    private CommandPreview _ripCommandPreview = null!;
    private OutputConsole _ripOutputConsole = null!;

    // Continue tab
    private TextBox _contTitle = null!;
    private CheckBox _contBluray = null!, _contExtras = null!;
    private ComboBox _contContentType = null!, _contFromStep = null!, _contOutputDrive = null!;
    private GroupBox _contSeriesGroup = null!;
    private NumericUpDown _contSeason = null!, _contStartEpisode = null!, _contDisc = null!;
    private Button _contStartButton = null!, _contStopButton = null!;
    private StepProgressPanel _contStepProgress = null!;
    private CommandPreview _contCommandPreview = null!;
    private OutputConsole _contOutputConsole = null!;

    // Settings tab
    private TextBox _settingsScriptDir = null!, _settingsPsPath = null!;
    private TextBox _settingsDefaultDrive = null!, _settingsDefaultOutputDrive = null!;
    private NumericUpDown _settingsDefaultDriveIndex = null!;
    private Button _settingsSaveButton = null!, _settingsBrowseScript = null!;
}
