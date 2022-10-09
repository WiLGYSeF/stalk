using Wilgysef.Stalk.Core.Shared.Options;

namespace Wilgysef.Stalk.WebApi.Options;

public class AppOptions : IOptionSection
{
    public string Label => "App";

    public bool ExceptionsInResponse { get; set; }

    public bool PauseJobsOnStart { get; set; }
}
