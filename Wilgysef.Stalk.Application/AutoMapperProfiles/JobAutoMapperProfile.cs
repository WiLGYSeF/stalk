using AutoMapper;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Application.Contracts.Queries.Jobs;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Specifications;
using Wilgysef.Stalk.Core.Utilities;

namespace Wilgysef.Stalk.Application.AutoMapperProfiles;

public class JobAutoMapperProfile : Profile
{
    public JobAutoMapperProfile()
    {
        CreateMap<Job, JobDto>()
            .ForMember(dto => dto.State, opt => opt.MapFrom(j => j.State.ToString().ToLower()))
            .ForMember(dto => dto.Config, opt => opt.MapFrom(j => j.GetConfig()));

        CreateMap<JobConfig, JobConfigDto>();
        CreateMap<JobConfigDto, JobConfig>();

        CreateMap<JobTask, JobTaskDto>()
            .ForMember(dto => dto.State, opt => opt.MapFrom(t => t.State.ToString().ToLower()))
            .ForMember(dto => dto.Type, opt => opt.MapFrom(t => t.Type.ToString().ToLower()));

        CreateMap<JobTaskResult, JobTaskResultDto>();

        CreateMap<GetJobs, JobQuery>()
            .ForMember(q => q.States, opt => opt.MapFrom(q => q.States.Select(s => EnumUtils.Parse<JobState>(s, true))))
            .ForMember(q => q.Sort, opt => opt.MapFrom(q => q.Sort != null ? EnumUtils.Parse<JobSortOrder>(q.Sort, true) : JobSortOrder.Id));
    }
}
