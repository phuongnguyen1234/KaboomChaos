namespace Core.Interfaces
{
    /// <summary>
    /// Interface for a database of collectibles.
    /// </summary>
    public interface ICollectibleDatabase : ISpawnableDatabase<ICollectibleMapping, ICollectibleData>
    {
    }
}