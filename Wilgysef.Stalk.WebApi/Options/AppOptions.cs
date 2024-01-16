using Wilgysef.Stalk.Core.Shared.Options;

namespace Wilgysef.Stalk.WebApi.Options;

public class AppOptions : IOptionSection
{
    public string Label => "App";

    public IList<string> Urls { get; set; } = null!;

    public string OutputPath { get; set; } = null!;

    public bool ExceptionsInResponse { get; set; }

    public bool PauseJobsOnStart { get; set; }
}
