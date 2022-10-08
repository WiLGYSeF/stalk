﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.ProcessServices;

namespace Wilgysef.Stalk.TestBase.Shared.Mocks
{
    public class MockProcessService : IProcessService
    {
        private readonly List<ProcessMatch> _processes = new List<ProcessMatch>();

        public IProcess? Start(ProcessStartInfo startInfo)
        {
            foreach (var process in _processes)
            {
                if ((process.Filename != null && process.Filename == startInfo.FileName)
                    || (process.Regex != null && process.Regex.IsMatch(startInfo.FileName))
                    || (process.UseStartInfo && process.Process.StartInfo.FileName == startInfo.FileName))
                {
                    return process.Process;
                }
            }

            throw new InvalidOperationException($"Process not mocked: {startInfo.FileName}");
        }

        public MockProcessService For(string filename, Action<MockProcessBuilder> action)
        {
            return AddMockProcess(new MockProcessBuilder().WithFilename(filename), action, filename: filename);
        }

        public MockProcessService For(Regex regex, Action<MockProcessBuilder> action)
        {
            return AddMockProcess(new MockProcessBuilder(), action, regex: regex);
        }

        public MockProcessService For(ProcessStartInfo startInfo, Action<MockProcessBuilder> action)
        {
            return AddMockProcess(new MockProcessBuilder().WithStartInfo(startInfo), action, useStartInfo: true);
        }

        private MockProcessService AddMockProcess(
            MockProcessBuilder builder,
            Action<MockProcessBuilder> action,
            string? filename = null,
            Regex? regex = null,
            bool useStartInfo = false)
        {
            action(builder);

            _processes.Add(new ProcessMatch(
                builder.Create(),
                filename,
                regex,
                useStartInfo));
            return this;
        }

        private class ProcessMatch
        {
            public MockProcess Process { get; }

            public string? Filename { get; }

            public Regex? Regex { get; }

            public bool UseStartInfo { get; }

            public ProcessMatch(
                MockProcess process,
                string? filename,
                Regex? regex,
                bool useStartInfo)
            {
                Process = process;
                Filename = filename;
                Regex = regex;
                UseStartInfo = useStartInfo;
            }
        }
    }
}
