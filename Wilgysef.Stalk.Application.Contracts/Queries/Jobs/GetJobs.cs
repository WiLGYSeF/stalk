using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Queries.Jobs;

public class GetJobs : IQuery
{
    public string? Name { get; set; }

    public ICollection<string> States { get; set; }

    public DateTime? StartedBefore { get; set; }

    public DateTime? StartedAfter { get; set; }

    public DateTime? FinishedBefore { get; set; }

    public DateTime? FinishedAfter { get; set; }

    public string? Sort { get; set; }

    public bool SortDescending { get; set; }

    public GetJobs(
        string? name = null,
        ICollection<string>? states = null,
        DateTime? startedBefore = null,
        DateTime? startedAfter = null,
        DateTime? finishedBefore = null,
        DateTime? finishedAfter = null,
        string? sort = null,
        bool sortDescending = false)
    {
        Name = name;
        States = states ?? new List<string>();
        StartedBefore = startedBefore;
        StartedAfter = startedAfter;
        FinishedBefore = finishedBefore;
        FinishedAfter = finishedAfter;
        Sort = sort;
        SortDescending = sortDescending;
    }
}
