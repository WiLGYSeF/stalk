using AutoMapper;
using Wilgysef.Stalk.Application.Shared.Dtos;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Application.AutoMapperProfiles;

public class JobAutoMapperProfile : Profile
{
    public JobAutoMapperProfile()
    {
        CreateMap<Job, JobDto>();

        CreateMap<JobTask, JobTaskDto>();

        CreateMap<JobTaskResult, JobTaskResultDto>();
    }
}
