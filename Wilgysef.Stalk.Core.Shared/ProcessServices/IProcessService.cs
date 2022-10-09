using System.Diagnostics;

namespace Wilgysef.Stalk.Core.Shared.ProcessServices
{
    public interface IProcessService
    {
        IProcess? Start(ProcessStartInfo startInfo);
    }
}
