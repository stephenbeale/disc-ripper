using DiscRipper.Controls;
using DiscRipper.Models;
using DiscRipper.Services;

namespace DiscRipper;

public partial class MainForm : Form
{
    private AppSettings _settings;
    private ProcessRunner? _processRunner;
    private StepInfo[] _currentSteps = StepInfo.CreatePipeline();
    private DateTime _startTime;

    public MainForm()
    {
        InitializeComponent();
        _settings = SettingsService.Load();
        LoadSettingsToUi();
        PopulateDrives();
        WireUpCommandPreview();
        UpdateRipSeriesVisibility();
        UpdateContinueSeriesVisibility();
        UpdateRipCommandPreview();
        UpdateContinueCommandPreview();
    }

    // === Drive population ===

    private void PopulateDrives()
    {
        // Optical drives for source
        _ripDrivePanel.Controls.Clear();
        var opticalDrives = DriveDetector.GetOpticalDrives();
        if (opticalDrives.Count == 0)
        {
            var noDrive = new RadioButton
            {
                Text = $"{_settings.DefaultDrive} (default)",
                Tag = _settings.DefaultDrive,
                Checked = true,
                ForeColor = Color.White,
                AutoSize = true
            };
            noDrive.CheckedChanged += (_, _) => UpdateRipCommandPreview();
            _ripDrivePanel.Controls.Add(noDrive);
        }
        else
        {
            bool first = true;
            foreach (var drive in opticalDrives)
            {
                var rb = new RadioButton
                {
                    Text = $"{drive.Letter} {drive.Label}",
                    Tag = drive.Letter,
                    Checked = first,
                    ForeColor = Color.White,
                    AutoSize = true
                };
                rb.CheckedChanged += (_, _) => UpdateRipCommandPreview();
                _ripDrivePanel.Controls.Add(rb);
                first = false;
            }
        }

        // Fixed drives for output
        var fixedDrives = DriveDetector.GetFixedDrives();
        _ripOutputDrive.Items.Clear();
        _contOutputDrive.Items.Clear();
        foreach (var drive in fixedDrives)
        {
            var display = $"{drive.Letter} ({drive.Label})";
            _ripOutputDrive.Items.Add(display);
            _contOutputDrive.Items.Add(display);
        }

        // Select default output
        SelectOutputDrive(_ripOutputDrive, _settings.DefaultOutputDrive, fixedDrives);
        SelectOutputDrive(_contOutputDrive, _settings.DefaultOutputDrive, fixedDrives);
    }

    private static void SelectOutputDrive(ComboBox combo, string defaultDrive, List<DriveInfo2> drives)
    {
        for (int i = 0; i < drives.Count; i++)
        {
            if (drives[i].Letter.Equals(defaultDrive, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

    // === Settings ===

    private void LoadSettingsToUi()
    {
        _settingsScriptDir.Text = _settings.ScriptDirectory;
        _settingsPsPath.Text = _settings.PowerShellPath;
        _settingsDefaultDrive.Text = _settings.DefaultDrive;
        _settingsDefaultDriveIndex.Value = Math.Max(-1, Math.Min(99, _settings.DefaultDriveIndex));
        _settingsDefaultOutputDrive.Text = _settings.DefaultOutputDrive;
    }

    private void SettingsSaveButton_Click(object? sender, EventArgs e)
    {
        _settings.ScriptDirectory = _settingsScriptDir.Text.Trim();
        _settings.PowerShellPath = _settingsPsPath.Text.Trim();
        _settings.DefaultDrive = _settingsDefaultDrive.Text.Trim();
        _settings.DefaultDriveIndex = (int)_settingsDefaultDriveIndex.Value;
        _settings.DefaultOutputDrive = _settingsDefaultOutputDrive.Text.Trim();

        SettingsService.Save(_settings);
        SetStatus("Settings saved");
        PopulateDrives();
        UpdateRipCommandPreview();
        UpdateContinueCommandPreview();
    }

    // === Visibility toggles ===

    private void UpdateRipSeriesVisibility()
    {
        var isSeries = _ripContentType.SelectedItem?.ToString() == nameof(ContentType.Series);
        _ripSeriesGroup.Visible = isSeries;
        UpdateRipCommandPreview();
    }

    private void UpdateContinueSeriesVisibility()
    {
        var isSeries = _contContentType.SelectedItem?.ToString() == nameof(ContentType.Series);
        _contSeriesGroup.Visible = isSeries;
        UpdateContinueCommandPreview();
    }

    // === Command preview wiring ===

    private void WireUpCommandPreview()
    {
        // Rip tab
        _ripTitle.TextChanged += (_, _) => UpdateRipCommandPreview();
        _ripAutoDiscover.CheckedChanged += (_, _) => UpdateRipCommandPreview();
        _ripContentType.SelectedIndexChanged += (_, _) => UpdateRipCommandPreview();
        _ripBluray.CheckedChanged += (_, _) => UpdateRipCommandPreview();
        _ripSeason.ValueChanged += (_, _) => UpdateRipCommandPreview();
        _ripStartEpisode.ValueChanged += (_, _) => UpdateRipCommandPreview();
        _ripDisc.ValueChanged += (_, _) => UpdateRipCommandPreview();
        _ripOutputDrive.SelectedIndexChanged += (_, _) => UpdateRipCommandPreview();
        _ripExtras.CheckedChanged += (_, _) => UpdateRipCommandPreview();
        _ripQueue.CheckedChanged += (_, _) => UpdateRipCommandPreview();

        // Continue tab
        _contTitle.TextChanged += (_, _) => UpdateContinueCommandPreview();
        _contFromStep.SelectedIndexChanged += (_, _) => UpdateContinueCommandPreview();
        _contContentType.SelectedIndexChanged += (_, _) => UpdateContinueCommandPreview();
        _contBluray.CheckedChanged += (_, _) => UpdateContinueCommandPreview();
        _contSeason.ValueChanged += (_, _) => UpdateContinueCommandPreview();
        _contStartEpisode.ValueChanged += (_, _) => UpdateContinueCommandPreview();
        _contDisc.ValueChanged += (_, _) => UpdateContinueCommandPreview();
        _contOutputDrive.SelectedIndexChanged += (_, _) => UpdateContinueCommandPreview();
        _contExtras.CheckedChanged += (_, _) => UpdateContinueCommandPreview();
    }

    // === Build options from UI ===

    private RipOptions BuildRipOptions()
    {
        var selectedDrive = _ripDrivePanel.Controls.OfType<RadioButton>()
            .FirstOrDefault(rb => rb.Checked)?.Tag?.ToString() ?? _settings.DefaultDrive;

        var outputDrive = GetSelectedOutputDrive(_ripOutputDrive);
        Enum.TryParse<ContentType>(_ripContentType.SelectedItem?.ToString(), out var contentType);

        return new RipOptions
        {
            Title = _ripAutoDiscover.Checked ? "" : _ripTitle.Text.Trim(),
            ContentType = contentType,
            Bluray = _ripBluray.Checked,
            Season = (int)_ripSeason.Value,
            StartEpisode = (int)_ripStartEpisode.Value,
            Disc = (int)_ripDisc.Value,
            Drive = selectedDrive,
            DriveIndex = _settings.DefaultDriveIndex,
            OutputDrive = outputDrive,
            Extras = _ripExtras.Checked,
            Queue = _ripQueue.Checked
        };
    }

    private ContinueOptions BuildContinueOptions()
    {
        var outputDrive = GetSelectedOutputDrive(_contOutputDrive);
        Enum.TryParse<ContentType>(_contContentType.SelectedItem?.ToString(), out var contentType);

        return new ContinueOptions
        {
            Title = _contTitle.Text.Trim(),
            FromStep = _contFromStep.SelectedItem?.ToString() ?? "handbrake",
            ContentType = contentType,
            Bluray = _contBluray.Checked,
            Season = (int)_contSeason.Value,
            StartEpisode = (int)_contStartEpisode.Value,
            Disc = (int)_contDisc.Value,
            OutputDrive = outputDrive,
            Extras = _contExtras.Checked
        };
    }

    private string GetSelectedOutputDrive(ComboBox combo)
    {
        var text = combo.SelectedItem?.ToString() ?? "";
        // Extract drive letter from "E: (label)" format
        var spaceIdx = text.IndexOf(' ');
        return spaceIdx > 0 ? text[..spaceIdx] : (text.Length > 0 ? text : _settings.DefaultOutputDrive);
    }

    // === Command preview updates ===

    private void UpdateRipCommandPreview()
    {
        var options = BuildRipOptions();
        var cmd = PowerShellCommandBuilder.BuildRipCommand(options, _settings);
        _ripCommandPreview.SetCommand(cmd);
    }

    private void UpdateContinueCommandPreview()
    {
        var options = BuildContinueOptions();
        var cmd = PowerShellCommandBuilder.BuildContinueCommand(options, _settings);
        _contCommandPreview.SetCommand(cmd);
    }

    // === Start / Stop ===

    private async void RipStartButton_Click(object? sender, EventArgs e)
    {
        var options = BuildRipOptions();
        var command = PowerShellCommandBuilder.BuildRipCommand(options, _settings);

        _currentSteps = StepInfo.CreatePipeline();
        _ripStepProgress.UpdateSteps(_currentSteps);
        _ripOutputConsole.ClearOutput();

        await StartProcess(command, _ripStepProgress, _ripOutputConsole, _ripStartButton, _ripStopButton);
    }

    private async void ContinueStartButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_contTitle.Text))
        {
            MessageBox.Show("Title is required for continue-rip.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var options = BuildContinueOptions();
        var command = PowerShellCommandBuilder.BuildContinueCommand(options, _settings);

        // Mark skipped steps based on FromStep
        _currentSteps = StepInfo.CreatePipeline();
        var fromStepNum = options.FromStep switch
        {
            "handbrake" => 2,
            "organize" => 3,
            "open" => 4,
            _ => 2
        };
        for (int i = 0; i < _currentSteps.Length; i++)
        {
            if (_currentSteps[i].Number < fromStepNum)
                _currentSteps[i].State = StepState.Skipped;
        }
        _contStepProgress.UpdateSteps(_currentSteps);
        _contOutputConsole.ClearOutput();

        await StartProcess(command, _contStepProgress, _contOutputConsole, _contStartButton, _contStopButton);
    }

    private async Task StartProcess(string command, StepProgressPanel stepPanel, OutputConsole console,
        Button startButton, Button stopButton)
    {
        startButton.Enabled = false;
        stopButton.Enabled = true;
        _startTime = DateTime.Now;
        _durationTimer.Start();
        SetStatus("Running...");
        Text = "disc-ripper - Running";

        _processRunner?.Dispose();
        _processRunner = new ProcessRunner();

        _processRunner.OutputReceived += (_, line) =>
        {
            console.AppendOutput(line);
            HandleOutputEvent(line, stepPanel);
        };

        _processRunner.ErrorReceived += (_, line) =>
        {
            console.AppendOutput(line);
        };

        _processRunner.ProcessExited += (_, exitCode) =>
        {
            Invoke(() =>
            {
                _durationTimer.Stop();
                startButton.Enabled = true;
                stopButton.Enabled = false;

                if (exitCode == 0)
                {
                    SetStatus("Completed");
                    Text = "disc-ripper - Complete";
                }
                else
                {
                    SetStatus($"Exited with code {exitCode}");
                    Text = "disc-ripper - Error";
                }
            });
        };

        await _processRunner.StartAsync(command);
    }

    private void StopButton_Click(object? sender, EventArgs e)
    {
        if (_processRunner?.IsRunning == true)
        {
            var result = MessageBox.Show("Stop the current operation?", "Confirm Stop",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _processRunner.Stop();
                SetStatus("Stopped by user");
                Text = "disc-ripper";
            }
        }
    }

    // === Output event handling ===

    private void HandleOutputEvent(string line, StepProgressPanel stepPanel)
    {
        var evt = OutputParser.Parse(line);
        if (evt is null) return;

        Invoke(() =>
        {
            switch (evt.Event)
            {
                case OutputEvent.StepStarted:
                    // Mark previous steps as completed
                    for (int i = 0; i < _currentSteps.Length; i++)
                    {
                        if (_currentSteps[i].Number < evt.StepNumber && _currentSteps[i].State == StepState.Active)
                            _currentSteps[i].State = StepState.Completed;
                    }
                    // Mark current step as active
                    var step = _currentSteps.FirstOrDefault(s => s.Number == evt.StepNumber);
                    if (step != null) step.State = StepState.Active;
                    stepPanel.UpdateSteps(_currentSteps);
                    Text = $"disc-ripper - Step {evt.StepNumber}/4";
                    break;

                case OutputEvent.Complete:
                    foreach (var s in _currentSteps)
                    {
                        if (s.State == StepState.Active || s.State == StepState.Pending)
                            s.State = StepState.Completed;
                    }
                    stepPanel.UpdateSteps(_currentSteps);
                    SetStatus("Complete!");
                    Text = "disc-ripper - Complete!";
                    break;

                case OutputEvent.Queued:
                    SetStatus("Queued!");
                    Text = "disc-ripper - Queued";
                    break;

                case OutputEvent.Failed:
                    var active = _currentSteps.FirstOrDefault(s => s.State == StepState.Active);
                    if (active != null) active.State = StepState.Failed;
                    stepPanel.UpdateSteps(_currentSteps);
                    SetStatus("Failed!");
                    Text = "disc-ripper - FAILED";
                    break;

                case OutputEvent.PromptDetected:
                    HandlePrompt(evt.RawLine);
                    break;
            }
        });
    }

    private void HandlePrompt(string promptLine)
    {
        var response = Microsoft.VisualBasic.Interaction.InputBox(
            promptLine, "disc-ripper - Input Required", "");

        if (_processRunner?.IsRunning == true)
        {
            _processRunner.SendInput(response);
        }
    }

    // === Status bar ===

    private void SetStatus(string text)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatus(text));
            return;
        }
        _statusLabel.Text = text;
    }

    private void DurationTimer_Tick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.Now - _startTime;
        _durationLabel.Text = $"Duration: {elapsed:hh\\:mm\\:ss}";
    }

    // === Form closing guard ===

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_processRunner?.IsRunning == true)
        {
            var result = MessageBox.Show(
                "A process is still running. Stop it and close?",
                "disc-ripper",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }
            _processRunner.Stop();
        }

        _processRunner?.Dispose();
        base.OnFormClosing(e);
    }
}
