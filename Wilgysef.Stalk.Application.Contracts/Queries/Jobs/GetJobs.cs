using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Application.Contracts.Queries.Jobs;

public class GetJobs : IQuery
{
    public string? Name { get; set; }

    public ICollection<JobState> States { get; set; }

    public JobSortOrder Sort { get; set; }

    public bool SortDescending { get; set; }

    public GetJobs(
        string? name = null,
        ICollection<JobState>? states = null,
        JobSortOrder sort = JobSortOrder.Id,
        bool sortDescending = false)
    {
        Name = name;
        States = states ?? new List<JobState>();
        Sort = sort;
        SortDescending = sortDescending;
    }
}
