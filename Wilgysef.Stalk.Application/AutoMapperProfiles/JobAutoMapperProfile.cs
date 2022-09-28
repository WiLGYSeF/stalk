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
        CreateMap<JobConfig, JobConfigDto>()
            .ForMember(d => d.ExtractorConfig, opt => opt.MapFrom(c => c.ExtractorConfig.ToList()))
            .ForMember(d => d.DownloaderConfig, opt => opt.MapFrom(c => c.DownloaderConfig.ToList()));
        CreateMap<JobConfig.Logging, JobConfigDto.LoggingDto>();
        CreateMap<JobConfig.DelayConfig, JobConfigDto.DelayConfigDto>();
        CreateMap<JobConfigGroup, JobConfigDto.ConfigGroupDto>();
        CreateMap<JobConfig.Range, JobConfigDto.RangeDto>();

        CreateMap<JobConfigDto, JobConfig>()
            .ForMember(c => c.ExtractorConfig, opt => opt
                .MapFrom(d => new JobConfigGroupCollection(d.ExtractorConfig != null
                    ? d.ExtractorConfig.Select(CreateJobConfigGroup)
                    : null)))
            .ForMember(c => c.DownloaderConfig, opt => opt
                .MapFrom(d => new JobConfigGroupCollection(d.DownloaderConfig != null
                    ? d.DownloaderConfig.Select(CreateJobConfigGroup)
                    : null)));
        CreateMap<JobConfigDto.LoggingDto, JobConfig.Logging>();
        CreateMap<JobConfigDto.DelayConfigDto, JobConfig.DelayConfig>();
        CreateMap<JobConfigDto.ConfigGroupDto, JobConfigGroup>();
        CreateMap<JobConfigDto.RangeDto, JobConfig.Range>();
    }

    private static JobConfigGroup CreateJobConfigGroup(JobConfigDto.ConfigGroupDto group)
    {
        return new JobConfigGroup
        {
            Name = group.Name,
            Config = group.Config,
        };
    }
}
