using System.Threading.Tasks;

namespace Wilgysef.Stalk.Core.Shared.Cqrs
{
    public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery
    {
        Task<TResult> HandleQueryAsync(TQuery query);
    }
}
