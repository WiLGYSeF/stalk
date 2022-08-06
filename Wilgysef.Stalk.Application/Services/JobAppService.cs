using IdGen;
using Wilgysef.Stalk.Application.Shared.Dtos;
using Wilgysef.Stalk.Application.Shared.Dtos.JobAppService;
using Wilgysef.Stalk.Application.Shared.Services;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Application.Services;

public class JobAppService : ApplicationService, IJobAppService
{
    private readonly IJobManager _jobManager;
    private readonly IIdGenerator<long> _idGenerator;

    public JobAppService(
        IJobManager jobManager,
        IIdGenerator<long> idGenerator)
    {
        _jobManager = jobManager;
        _idGenerator = idGenerator;
    }

    public async Task<JobDto> CreateJobAsync(CreateJobInput input)
    {
        var job = await _jobManager.CreateJobAsync(Job.Create(
            _idGenerator.CreateId(),
            input.Name));

        return Mapper.Map<JobDto>(job);
    }
}
