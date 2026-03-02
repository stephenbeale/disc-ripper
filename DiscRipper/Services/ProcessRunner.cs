using System.Diagnostics;

namespace DiscRipper.Services;

public class ProcessRunner : IDisposable
{
    private Process? _process;
    private CancellationTokenSource? _cts;

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;
    public event EventHandler<int>? ProcessExited;

    public bool IsRunning => _process is not null && !_process.HasExited;

    public async Task StartAsync(string command)
    {
        _cts = new CancellationTokenSource();

        // Parse the command into exe + arguments
        string exe;
        string arguments;
        if (command.StartsWith('"'))
        {
            var closingQuote = command.IndexOf('"', 1);
            exe = command[1..closingQuote];
            arguments = command[(closingQuote + 1)..].TrimStart();
        }
        else
        {
            var spaceIdx = command.IndexOf(' ');
            exe = spaceIdx < 0 ? command : command[..spaceIdx];
            arguments = spaceIdx < 0 ? "" : command[(spaceIdx + 1)..];
        }

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        _process.Exited += (_, _) =>
        {
            var exitCode = 0;
            try { exitCode = _process.ExitCode; } catch { }
            ProcessExited?.Invoke(this, exitCode);
        };

        _process.Start();

        // Read stdout and stderr on background tasks
        _ = ReadStreamAsync(_process.StandardOutput, line => OutputReceived?.Invoke(this, line), _cts.Token);
        _ = ReadStreamAsync(_process.StandardError, line => ErrorReceived?.Invoke(this, line), _cts.Token);

        await Task.CompletedTask;
    }

    public void SendInput(string text)
    {
        if (_process is { HasExited: false })
        {
            _process.StandardInput.WriteLine(text);
            _process.StandardInput.Flush();
        }
    }

    public void Stop()
    {
        _cts?.Cancel();

        if (_process is { HasExited: false })
        {
            try
            {
                // Kill the entire process tree
                _process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Process may have already exited
            }
        }
    }

    private static async Task ReadStreamAsync(StreamReader reader, Action<string> handler, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) break;
                handler(line);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    public void Dispose()
    {
        Stop();
        _process?.Dispose();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
