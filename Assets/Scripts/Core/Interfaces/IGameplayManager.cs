using UnityEngine;

namespace Core
{
    public interface IGameplayManager
    {
        void InitializeGame();
        void StartGameLoop();
        void EndGameLoop();

        void RegisterSpawnPoint(PlayerSpawn spawnPoint);
        void UnregisterSpawnPoint(PlayerSpawn spawnPoint);
        // Có thể thêm các phương thức khác liên quan đến quản lý gameplay chung ở đây
    }
}