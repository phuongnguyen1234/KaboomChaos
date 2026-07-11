// Core/Interfaces/IPlayerManager.cs
using UnityEngine;

namespace Core.Interfaces
{
    public interface IPlayerManager
    {
        IPlayer GetCurrentPlayer();
        void SpawnInitialPlayer();
        IPlayer HandlePlayerSpawn(GameObject playerPrefab, PlayerSpawn spawnPoint);
        void RespawnPlayer(IPlayer player, PlayerSpawn spawnPoint = null);
    }
}
