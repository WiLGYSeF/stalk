using Wilgysef.Stalk.Core.Shared.IdGenerators;

namespace Wilgysef.Stalk.Application.IdGenerators;

public class IdGenerator : IIdGenerator<long>
{
    private IdGen.IdGenerator _idGenerator;

    public IdGenerator(IdGen.IdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    public long CreateId()
    {
        return _idGenerator.CreateId();
    }
}
