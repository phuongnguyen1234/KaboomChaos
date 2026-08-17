using UnityEngine;
using System.Collections.Generic;

namespace Core.Interfaces
{
    public interface IPlayerManager
    {
        int GetAlivePlayerCount();
        IPlayer GetCurrentPlayer();
        List<IPlayer> GetAllPlayers();
        void SpawnInitialPlayer();
        IPlayer HandlePlayerSpawn(GameObject playerPrefab, PlayerSpawn spawnPoint);
        void RespawnPlayer(IPlayer player, PlayerSpawn spawnPoint = null);
        void ClearAllPlayers();

        void StartRound();
        List<IPlayer> GetPlayersNotInCurrentRound();
        void AddPlayerToCurrentRound(IPlayer player);
        void EndRound();
        void ReturnRoundSurvivorsToLobby();
        Vector3 GetRandomPlayerPosition();
        List<IPlayer> GetPlayersInRound();

        // BGM Control Methods
        void SetGameplayMusicForRoundPlayers(float intensity);
        void SetLast30sMusicForRoundPlayers(float intensity);
        void SetLobbyMusicForAllPlayers();
    }
}
