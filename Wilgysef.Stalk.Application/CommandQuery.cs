using AutoMapper;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Application;

public abstract class CommandQuery : ITransientDependency
{
    public IMapper Mapper { get; set; } = null!;
}
