﻿using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobScopeServices;

public class JobScopeService : IJobScopeService, ITransientDependency
{
    private readonly IJobScopeCollectionService _jobScopeCollectionService;
    private readonly IServiceLocator _serviceLocator;

    public JobScopeService(
        IJobScopeCollectionService jobScopeCollectionService,
        IServiceLocator serviceLocator)
    {
        _jobScopeCollectionService = jobScopeCollectionService;
        _serviceLocator = serviceLocator;
    }

    public IServiceLifetimeScope GetJobScope(long jobId)
    {
        return _jobScopeCollectionService.GetJobScope(jobId, _serviceLocator);
    }

    public bool RemoveJobScope(long jobId)
    {
        return _jobScopeCollectionService.RemoveJobScope(jobId);
    }
}
