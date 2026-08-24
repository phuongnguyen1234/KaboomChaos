using UnityEngine;
using System.Collections.Generic;
using Core;

namespace Core.Interfaces
{
    public interface IPlayerManager
    {
        /// <summary>
        /// Thể hiện Singleton toàn cục của IPlayerManager.
        /// </summary>
        static IPlayerManager Instance { get; }

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

        /// <summary>
        /// Lấy danh sách người chơi còn sống sót trong round hiện tại.
        /// Danh sách này phải được lấy TRƯỚC khi gọi EndRound() vì EndRound() xóa danh sách người chơi trong round.
        /// </summary>
        List<IPlayer> GetSurvivors();

        /// <summary>
        /// Lấy (hoặc tạo mới) dữ liệu round của người chơi để theo dõi win streak,
        /// thời điểm bắt đầu round và trạng thái Extreme Mode.
        /// </summary>
        PlayerRoundData GetPlayerRoundData(IPlayer player);

        // BGM Control Methods
        void SetGameplayMusicForRoundPlayers(float intensity);
        void SetLast30sMusicForRoundPlayers(float intensity);
        void SetLobbyMusicForAllPlayers();
    }
}
