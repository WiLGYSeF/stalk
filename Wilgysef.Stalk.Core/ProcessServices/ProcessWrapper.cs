using System.Diagnostics;
using Wilgysef.Stalk.Core.Shared.ProcessServices;

namespace Wilgysef.Stalk.Core.ProcessServices;

public class ProcessWrapper : IProcess
{
    public DateTime StartTime => _process.StartTime;

    public StreamReader StandardOutput => _process.StandardOutput;

    public StreamWriter StandardInput => _process.StandardInput;

    public StreamReader StandardError => _process.StandardError;

    public ProcessStartInfo StartInfo => _process.StartInfo;

    public bool HasExited => _process.HasExited;

    public DateTime ExitTime => _process.ExitTime;

    public int ExitCode => _process.ExitCode;

    public bool EnableRaisingEvents
    {
        get => _process.EnableRaisingEvents;
        set => _process.EnableRaisingEvents = value;
    }

    public int Id => _process.Id;

    public event EventHandler? Exited;
    public event DataReceivedEventHandler? ErrorDataReceived;
    public event DataReceivedEventHandler? OutputDataReceived;

    private readonly Process _process;

    public ProcessWrapper(Process process)
    {
        _process = process;

        _process.Exited += Process_Exited;
        _process.ErrorDataReceived += Process_ErrorDataReceived;
        _process.OutputDataReceived += Process_OutputDataReceived;
    }

    private void Process_Exited(object? sender, EventArgs e)
    {
        Exited?.Invoke(sender, e);
    }

    private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        ErrorDataReceived?.Invoke(sender, e);
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        OutputDataReceived?.Invoke(sender, e);
    }

    public void BeginErrorReadLine()
    {
        _process.BeginErrorReadLine();
    }

    public void BeginOutputReadLine()
    {
        _process.BeginOutputReadLine();
    }

    public void CancelErrorRead()
    {
        _process.CancelErrorRead();
    }

    public void CancelOutputRead()
    {
        _process.CancelOutputRead();
    }

    public void Close()
    {
        _process.Close();
    }

    public bool CloseMainWindow()
    {
        return _process.CloseMainWindow();
    }

    public void Kill(bool entireProcessTree)
    {
        _process.Kill(entireProcessTree);
    }

    public void Kill()
    {
        _process.Kill();
    }

    public void Refresh()
    {
        _process.Refresh();
    }

    public bool Start()
    {
        return _process.Start();
    }

    public void WaitForExit()
    {
        _process.WaitForExit();
    }

    public bool WaitForExit(int milliseconds)
    {
        return _process.WaitForExit(milliseconds);
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        return _process.WaitForExitAsync(cancellationToken);
    }

    public bool WaitForInputIdle()
    {
        return _process.WaitForInputIdle();
    }

    public bool WaitForInputIdle(int milliseconds)
    {
        return _process.WaitForInputIdle(milliseconds);
    }

    public void Dispose()
    {
        _process.Dispose();
    }
}
