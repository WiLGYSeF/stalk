using IdGen;
using Wilgysef.Stalk.Application.Shared.Dtos;
using Wilgysef.Stalk.Application.Shared.Dtos.JobAppService;
using Wilgysef.Stalk.Application.Shared.Services;
using Wilgysef.Stalk.Core.JobWorkerManagers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Application.Services;

public class JobAppService : ApplicationService, IJobAppService
{
    private readonly IJobManager _jobManager;
    private readonly IJobStateManager _jobStateManager;
    private readonly IJobWorkerManager _jobWorkerManager;
    private readonly IIdGenerator<long> _idGenerator;

    public JobAppService(
        IJobManager jobManager,
        IJobStateManager jobStateManager,
        IJobWorkerManager jobWorkerManager,
        IIdGenerator<long> idGenerator)
    {
        _jobManager = jobManager;
        _jobStateManager = jobStateManager;
        _jobWorkerManager = jobWorkerManager;
        _idGenerator = idGenerator;
    }

    public async Task<JobDto> CreateJobAsync(CreateJobInput input)
    {
        var job = await _jobManager.CreateJobAsync(Job.Create(
            _idGenerator.CreateId(),
            input.Name));

        _jobWorkerManager.StartJobWorker(job);

        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> StopJobAsync(StopJobInput input)
    {
        var job = await _jobManager.GetJobAsync(input.Id);

        await _jobStateManager.StopJobAsync(job);

        return Mapper.Map<JobDto>(job);
    }
}
