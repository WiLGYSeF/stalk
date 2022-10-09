using System.Diagnostics;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ProcessServices;

namespace Wilgysef.Stalk.Core.ProcessServices;

public class ProcessService : IProcessService, ITransientDependency
{
    public IProcess? Start(ProcessStartInfo startInfo)
    {
        return new ProcessWrapper(Process.Start(startInfo)!);
    }
}
