using Wilgysef.Stalk.Application.Shared.Dtos;
using Wilgysef.Stalk.Application.Shared.Dtos.JobAppService;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Application.Shared.Services;

public interface IJobAppService : ITransientDependency
{
    Task<JobDto> CreateJobAsync(CreateJobInput input);
}
