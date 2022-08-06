using AutoMapper;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Application.Commands;

public abstract class ApplicationCommand : ITransientDependency
{
    public IMapper Mapper { get; set; } = null!;
}
