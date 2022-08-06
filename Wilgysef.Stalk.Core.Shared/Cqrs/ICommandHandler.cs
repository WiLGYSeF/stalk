using System.Threading.Tasks;

namespace Wilgysef.Stalk.Core.Shared.Cqrs
{
    public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand
    {
        Task<TResult> HandleCommandAsync(TCommand command);
    }
}
