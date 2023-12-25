using System;
using System.Text;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Application.Services;

public class OutputPathService : IOutputPathService
{
    public string? PathPrefix { get; set; }

    public string GetOutputPath(string path)
    {
        if (PathPrefix == null)
        {
            throw new InvalidOperationException($"{nameof(PathPrefix)} is not set.");
        }

        var builder = new StringBuilder();
        var idx = 0;

        for (; idx < path.Length && IsPathSeparator(path[idx]); idx++)
        {
        }

        for (; idx < path.Length; idx++)
        {
            if (idx <= path.Length - 2
                && path[idx] == '.' && path[idx + 1] == '.'
                && (idx + 2 == path.Length || IsPathSeparator(path[idx + 2])))
            {
                idx += 2;
                continue;
            }

            if (idx <= path.Length - 1
                && path[idx] == '.'
                && (idx + 1 == path.Length || IsPathSeparator(path[idx + 1])))
            {
                idx++;
                continue;
            }

            builder.Append(path[idx]);
        }

        return Path.Join(PathPrefix, builder.ToString());
    }

    private static bool IsPathSeparator(char c)
    {
        return c == '/' || c == '\\';
    }
}
