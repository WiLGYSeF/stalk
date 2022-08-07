using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Core.Specifications;

public class JobQuery
{
    public string? Name { get; set; }

    public ICollection<JobState> States { get; set; } = new List<JobState>();

    public JobSortOrder Sort { get; set; } = JobSortOrder.Id;

    public bool SortDescending { get; set; }
}
