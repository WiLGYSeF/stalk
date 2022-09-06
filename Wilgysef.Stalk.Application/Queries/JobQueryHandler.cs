using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Application.Contracts.Queries.Jobs;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Options;
using Wilgysef.Stalk.Core.Specifications;

namespace Wilgysef.Stalk.Application.Queries;

public class JobQueryHandler : Query,
    IQueryHandler<GetJob, JobDto>,
    IQueryHandler<GetJobs, JobListDto>
{
    private readonly IJobManager _jobManager;

    public JobQueryHandler(
        IJobManager jobManager,
        ExtractorsOptions options)
    {
        _jobManager = jobManager;
    }

    public async Task<JobDto> HandleQueryAsync(GetJob query)
    {
        var job = await _jobManager.GetJobAsync(query.Id, true);

        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobListDto> HandleQueryAsync(GetJobs query)
    {
        var jobs = await _jobManager.GetJobsAsync(
            new JobQuerySpecification(Mapper.Map<JobQuery>(query), readOnly: true));

        return new JobListDto
        {
            Jobs = Mapper.Map<ICollection<JobDto>>(jobs)
        };
    }
}
