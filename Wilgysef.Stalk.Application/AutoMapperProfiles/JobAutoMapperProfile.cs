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

        MapJobConfig();

        CreateMap<JobTask, JobTaskDto>()
            .ForMember(dto => dto.State, opt => opt.MapFrom(t => t.State.ToString().ToLower()))
            .ForMember(dto => dto.Type, opt => opt.MapFrom(t => t.Type.ToString().ToLower()));

        CreateMap<JobTaskResult, JobTaskResultDto>();

        CreateMap<GetJobs, JobQuery>()
            .ForMember(q => q.States, opt => opt.MapFrom(q => q.States.Select(s => EnumUtils.Parse<JobState>(s, true))))
            .ForMember(q => q.Sort, opt => opt.MapFrom(q => q.Sort != null ? EnumUtils.Parse<JobSortOrder>(q.Sort, true) : JobSortOrder.Id));
    }

    private void MapJobConfig()
    {
        CreateMap<JobConfig, JobConfigDto>();
        CreateMap<JobConfig.Logging, JobConfigDto.LoggingDto>();
        CreateMap<JobConfig.DelayConfig, JobConfigDto.DelayConfigDto>();
        CreateMap<JobConfig.ConfigGroup, JobConfigDto.ConfigGroupDto>();
        CreateMap<JobConfig.Range, JobConfigDto.RangeDto>();

        CreateMap<JobConfigDto, JobConfig>();
        CreateMap<JobConfigDto.LoggingDto, JobConfig.Logging>();
        CreateMap<JobConfigDto.DelayConfigDto, JobConfig.DelayConfig>();
        CreateMap<JobConfigDto.ConfigGroupDto, JobConfig.ConfigGroup>();
        CreateMap<JobConfigDto.RangeDto, JobConfig.Range>();
    }
}
