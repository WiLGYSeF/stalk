using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Wilgysef.Stalk.TestBase.Shared.Mocks
{
    public class MockProcessBuilder
    {
        DateTime StartTime { get; set; }

        StreamReader StandardOutput { get; set; }

        StreamWriter StandardInput { get; set; }

        StreamReader StandardError { get; set; }

        ProcessStartInfo StartInfo { get; set; }

        bool HasExited { get; set; }

        DateTime ExitTime { get; set; }

        int ExitCode { get; set; }

        bool EnableRaisingEvents { get; set; }

        int Id { get; set; }

        public MockProcess Create()
        {
            return new MockProcess
            {
                StartTime = StartTime,
                StandardOutput = StandardOutput,
                StandardInput = StandardInput,
                StandardError = StandardError,
                StartInfo = StartInfo,
                HasExited = HasExited,
                ExitTime = ExitTime,
                ExitCode = ExitCode,
                EnableRaisingEvents = EnableRaisingEvents,
                Id = Id,
            };
        }

        public MockProcessBuilder WithFilename(string filename)
        {
            StartInfo ??= new ProcessStartInfo();
            StartInfo.FileName = filename;
            return this;
        }

        public MockProcessBuilder WithStartInfo(ProcessStartInfo startInfo)
        {
            StartInfo = startInfo;
            return this;
        }

        public MockProcessBuilder WithStandardOutput(StreamReader reader)
        {
            StandardOutput = reader;
            return this;
        }

        public MockProcessBuilder WithStandardError(StreamReader reader)
        {
            StandardError = reader;
            return this;
        }
    }
}
