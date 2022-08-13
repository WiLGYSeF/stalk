using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Core.Specifications;

public class JobQuery
{
    public string? Name { get; set; }

    public ICollection<JobState> States { get; set; } = new List<JobState>();

    public DateTime? StartedBefore { get; set; }

    public DateTime? StartedAfter { get; set; }

    public DateTime? FinishedBefore { get; set; }

    public DateTime? FinishedAfter { get; set; }

    public JobSortOrder Sort { get; set; } = JobSortOrder.Id;

    public bool SortDescending { get; set; }
}
