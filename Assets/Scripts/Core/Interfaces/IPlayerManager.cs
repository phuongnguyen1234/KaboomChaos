using UnityEngine;
using System.Collections.Generic;
using Core;

namespace Core.Interfaces
{
    public interface IPlayerManager
    {
        /// <summary>
        /// Thể hiện Singleton toàn cục của IPlayerManager.
        /// Được gán bởi lớp cụ thể (PlayerManager) trong Awake(); các assembly khác (ví dụ UI)
        /// chỉ đọc giá trị này qua interface để tránh tham chiếu trực tiếp tới assembly Managers.
        /// </summary>
        static IPlayerManager Instance { get; set; }

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

        /// <summary>
        /// Bat hoac tat trang thai bat tu cho tat ca nguoi choi con song trong round.
        /// Dung khi round ket thuc de tranh sat thuong trong thoi gian chuyen giao ve lobby.
        /// </summary>
        void SetRoundSurvivorsInvincible(bool invincible);

        /// <summary>
        /// Nap day ngay lap tuc nang luong (energy) skill cho tat ca nguoi choi tham gia round.
        /// </summary>
        void ChargeRoundPlayersSkills();

        /// <summary>
        /// Khoa hoac mo khoa su dung skill cho tat ca nguoi choi tham gia round.
        /// </summary>
        /// <param name="locked">True de khoa khong cho dung skill, False de mo khoa.</param>
        void SetRoundPlayersSkillLock(bool locked);
    }
}
