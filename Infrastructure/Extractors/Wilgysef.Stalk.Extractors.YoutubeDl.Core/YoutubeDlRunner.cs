using System;
using System.ComponentModel;
using System.Diagnostics;
using Wilgysef.Stalk.Core.Shared.ProcessServices;

namespace Wilgysef.Stalk.Extractors.YoutubeDl.Core
{
    public class YoutubeDlRunner
    {
        public readonly string[] YouTubeDlDefaultExeNames = new string[]
        {
            "youtube-dl.exe",
            "youtube-dl",
            "yt-dlp.exe",
            "yt-dlp"
        };

        public YoutubeDlConfig Config { get; set; } = new YoutubeDlConfig();

        private readonly IProcessService _processService;

        public YoutubeDlRunner(IProcessService processService)
        {
            _processService = processService;
        }

        public virtual IProcess FindAndStartProcess(
            Uri uri,
            string filename,
            Action<ProcessStartInfo>? configure = null)
        {
            var startInfo = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
            };

            startInfo.ArgumentList.Add("--retries");
            startInfo.ArgumentList.Add(Config.Retries.ToString());

            startInfo.ArgumentList.Add("--file-access-retries");
            startInfo.ArgumentList.Add(Config.FileAccessRetries.ToString());

            startInfo.ArgumentList.Add("--fragment-retries");
            startInfo.ArgumentList.Add(Config.FragmentRetries.ToString());

            foreach (var retry in Config.RetrySleep)
            {
                startInfo.ArgumentList.Add("--retry-sleep");
                startInfo.ArgumentList.Add(retry);
            }

            startInfo.ArgumentList.Add("--buffer-size");
            startInfo.ArgumentList.Add(Config.BufferSize.ToString());

            startInfo.ArgumentList.Add("--progress");
            startInfo.ArgumentList.Add("--newline");

            if (Config.WriteInfoJson)
            {
                startInfo.ArgumentList.Add("--write-info-json");
            }
            if (Config.WriteSubs)
            {
                startInfo.ArgumentList.Add("--write-subs");
            }

            if (Config.CookieString != null)
            {
                startInfo.ArgumentList.Add("--add-header");
                startInfo.ArgumentList.Add($"Cookie:{Config.CookieString}");
            }

            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(filename);

            startInfo.ArgumentList.Add(uri.AbsoluteUri);

            configure?.Invoke(startInfo);

            return FindAndStartProcess(startInfo);
        }

        protected virtual IProcess FindAndStartProcess(ProcessStartInfo startInfo)
        {
            if (Config.ExecutableName != null)
            {
                return StartProcess(Config.ExecutableName.ToString());
            }

            foreach (var possibleName in YouTubeDlDefaultExeNames)
            {
                try
                {
                    return StartProcess(possibleName);
                }
                catch (Win32Exception) { }
            }

            throw new InvalidOperationException("Could not start youtube-dl.");

            IProcess StartProcess(string executableName)
            {
                startInfo.FileName = executableName;
                return _processService.Start(startInfo)
                    ?? throw new InvalidOperationException($"Could not start process: {startInfo.FileName}");
            }
        }
    }
}
