namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho hệ thống quản lý việc sinh (spawn) bom trong game.
    /// </summary>
    public interface IBombSpawnerManager
    {
        /// <summary> Bắt đầu chu trình sinh bom. </summary>
        void StartSpawning();
        /// <summary> Dừng chu trình sinh bom. </summary>
        void StopSpawning();
    }
}