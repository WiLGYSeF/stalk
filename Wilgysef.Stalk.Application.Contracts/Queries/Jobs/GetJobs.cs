using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Queries.Jobs;

public class GetJobs : IQuery
{
    public string? Name { get; set; }

    public ICollection<string> States { get; set; }

    public string? Sort { get; set; }

    public bool SortDescending { get; set; }

    public GetJobs(
        string? name = null,
        ICollection<string>? states = null,
        string? sort = null,
        bool sortDescending = false)
    {
        Name = name;
        States = states ?? new List<string>();
        Sort = sort;
        SortDescending = sortDescending;
    }
}
