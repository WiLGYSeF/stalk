namespace Wilgysef.Stalk.Core.Shared.IdGenerators
{
    public interface IIdGenerator<T>
    {
        /// <summary>
        /// Creates a new Id.
        /// </summary>
        /// <returns>Id.</returns>
        T CreateId();
    }
}
