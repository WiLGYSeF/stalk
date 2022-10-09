using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Wilgysef.Stalk.Core.Shared.ProcessServices
{
    public interface IProcess : IDisposable
    {
        DateTime StartTime { get; }

        StreamReader StandardOutput { get; }

        StreamWriter StandardInput { get; }

        StreamReader StandardError { get; }

        ProcessStartInfo StartInfo { get; }

        bool HasExited { get; }

        DateTime ExitTime { get; }

        int ExitCode { get; }

        bool EnableRaisingEvents { get; set; }

        int Id { get; }

        event EventHandler? Exited;

        event DataReceivedEventHandler? ErrorDataReceived;

        event DataReceivedEventHandler? OutputDataReceived;

        void BeginErrorReadLine();

        void BeginOutputReadLine();

        void CancelErrorRead();

        void CancelOutputRead();

        void Close();

        bool CloseMainWindow();

        void Kill(bool entireProcessTree);

        void Kill();

        void Refresh();

        bool Start();

        void WaitForExit();

        bool WaitForExit(int milliseconds);

        Task WaitForExitAsync(CancellationToken cancellationToken = default);

        bool WaitForInputIdle();

        bool WaitForInputIdle(int milliseconds);
    }
}
