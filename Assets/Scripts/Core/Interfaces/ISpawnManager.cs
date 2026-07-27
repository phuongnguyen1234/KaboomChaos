namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho hệ thống quản lý các điểm spawn trong game.
    /// </summary>
    public interface ISpawnManager
    {
        PlayerSpawn GetRandomSpawnPoint();
        void RegisterSpawnPoint(PlayerSpawn spawnPoint);
        void UnregisterSpawnPoint(PlayerSpawn spawnPoint);
    }
}