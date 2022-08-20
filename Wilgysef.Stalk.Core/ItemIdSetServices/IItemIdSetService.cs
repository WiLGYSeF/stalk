using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public interface IItemIdSetService : ITransientDependency
{
    IItemIdSet GetItemIdSet(string path);

    int WriteChanges(string path, IItemIdSet itemIds);
}
