using AutoMapper;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Application.AutoMapperProfiles;

public class JobAutoMapperProfile : Profile
{
    public JobAutoMapperProfile()
    {
        CreateMap<Job, JobDto>()
            .ForMember(dto => dto.State, opt => opt.MapFrom(j => j.State.ToString().ToLower()));

        CreateMap<JobTask, JobTaskDto>();

        CreateMap<JobTaskResult, JobTaskResultDto>();
    }
}
