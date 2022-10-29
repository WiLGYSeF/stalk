using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Queries.Jobs;

public class GetJob : IQuery
{
    public long Id { get; }

    public GetJob(long id)
    {
        Id = id;
    }
}
