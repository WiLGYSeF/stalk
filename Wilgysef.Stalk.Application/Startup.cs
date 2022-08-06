using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Application;

public class Startup
{
    private readonly IJobManager _jobManager;

    private bool _started = false;

    public Startup(
        IJobManager jobManager)
    {
        _jobManager = jobManager;
    }

    public async Task StartAsync()
    {
        if (_started)
        {
            throw new InvalidOperationException("Already started.");
        }
        _started = true;

        await _jobManager.DeactivateJobsAsync();
    }
}
