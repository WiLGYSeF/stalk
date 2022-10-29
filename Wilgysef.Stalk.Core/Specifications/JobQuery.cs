using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Core.Specifications;

public class JobQuery
{
    /// <summary>
    /// Filter job names that contain this string.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by job state.
    /// </summary>
    public ICollection<JobState> States { get; set; } = new List<JobState>();

    /// <summary>
    /// Filter jobs started before this time.
    /// </summary>
    public DateTime? StartedBefore { get; set; }

    /// <summary>
    /// Filter jobs started after this time.
    /// </summary>
    public DateTime? StartedAfter { get; set; }

    /// <summary>
    /// Filter jobs finished before this time.
    /// </summary>
    public DateTime? FinishedBefore { get; set; }

    /// <summary>
    /// Filter jobs finished after this time.
    /// </summary>
    public DateTime? FinishedAfter { get; set; }

    /// <summary>
    /// Job sort order.
    /// </summary>
    public JobSortOrder Sort { get; set; } = JobSortOrder.Id;

    /// <summary>
    /// Whether to sort <see cref="Sort"/> by descending.
    /// </summary>
    public bool SortDescending { get; set; }
}
