﻿namespace Wilgysef.Stalk.Application.Contracts.Queries.Jobs;

public class GetJobs : GetJobsBase
{
    public GetJobs(
        string? name = null,
        ICollection<string>? states = null,
        DateTime? startedBefore = null,
        DateTime? startedAfter = null,
        DateTime? finishedBefore = null,
        DateTime? finishedAfter = null,
        string? sort = null,
        bool sortDescending = false)
        : base(
            name,
            states,
            startedBefore,
            startedAfter,
            finishedBefore,
            finishedAfter,
            sort,
            sortDescending) { }
}
