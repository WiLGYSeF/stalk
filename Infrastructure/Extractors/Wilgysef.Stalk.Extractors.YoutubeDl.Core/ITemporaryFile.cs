using System;

namespace Wilgysef.Stalk.Extractors.YoutubeDl.Core
{
    public interface ITemporaryFile : IDisposable
    {
        string Filename { get; }
    }
}
