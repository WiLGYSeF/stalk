using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Wilgysef.Stalk.Core.Shared.ProcessServices;

namespace Wilgysef.Stalk.TestBase.Shared.Mocks
{
    // TODO: the rest of the owl
    public class MockProcess : IProcess
    {
        public DateTime StartTime { get; set; }

        public StreamReader StandardOutput { get; set; }

        public StreamWriter StandardInput { get; set; }

        public StreamReader StandardError { get; set; }

        public ProcessStartInfo StartInfo { get; set; }

        public bool HasExited { get; set; }

        public DateTime ExitTime { get; set; }

        public int ExitCode { get; set; }

        public bool EnableRaisingEvents { get; set; }

        public int Id { get; set; }

        public event EventHandler? Exited;
        public event DataReceivedEventHandler? ErrorDataReceived;
        public event DataReceivedEventHandler? OutputDataReceived;

        private Task? _errorReadTask;
        private Task? _outputReadTask;
        private CancellationTokenSource _readTokenSource = new CancellationTokenSource();

        private static readonly ConstructorInfo? DataReceivedEventArgsConstructor = typeof(DataReceivedEventArgs)
            .GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string) },
                null);

        public void BeginErrorReadLine()
        {
            if (StandardError == null)
            {
                return;
            }

            _errorReadTask = Task.Run(async () =>
            {
                while (!HasExited && !StandardError.EndOfStream)
                {
                    var line = await StandardError.ReadLineAsync();
                    ErrorDataReceived?.Invoke(this, CreateDataReceivedEventArgs(line));
                }
            }, _readTokenSource.Token);
        }

        public void BeginOutputReadLine()
        {
            if (StandardOutput == null)
            {
                return;
            }

            _outputReadTask = Task.Run(async () =>
            {
                while (!HasExited && !StandardOutput.EndOfStream)
                {
                    var line = await StandardOutput.ReadLineAsync();
                    OutputDataReceived?.Invoke(this, CreateDataReceivedEventArgs(line));
                }
            }, _readTokenSource.Token);
        }

        public void CancelErrorRead()
        {
            throw new NotImplementedException();
        }

        public void CancelOutputRead()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public bool CloseMainWindow()
        {
            throw new NotImplementedException();
        }

        public void Kill(bool entireProcessTree)
        {
            throw new NotImplementedException();
        }

        public void Kill()
        {
            throw new NotImplementedException();
        }

        public void Refresh()
        {
            throw new NotImplementedException();
        }

        public bool Start()
        {
            throw new NotImplementedException();
        }

        public void WaitForExit()
        {
            throw new NotImplementedException();
        }

        public bool WaitForExit(int milliseconds)
        {
            throw new NotImplementedException();
        }

        public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            if (_errorReadTask != null)
            {
                await _errorReadTask;
            }
            if (_outputReadTask != null)
            {
                await _outputReadTask;
            }
        }

        public bool WaitForInputIdle()
        {
            throw new NotImplementedException();
        }

        public bool WaitForInputIdle(int milliseconds)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _readTokenSource.Cancel();
        }

        private static DataReceivedEventArgs CreateDataReceivedEventArgs(string data)
        {
            return (DataReceivedEventArgs)DataReceivedEventArgsConstructor!.Invoke(new[] { data });
        }
    }
}
