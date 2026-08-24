using UnityEngine;
using Core.Interfaces;
using Core;
using Core.Utilities;
using Core.Interfaces.UI;
using System.Collections;
using System.Collections.Generic;

namespace Managers
{
    /// <summary>
    /// Quản lý vòng đời của người chơi, bao gồm việc sinh (spawn), hồi sinh (respawn),
    /// và cung cấp quyền truy cập vào đối tượng người chơi hiện tại.
    /// </summary>
    public class PlayerManager : MonoBehaviour, IPlayerManager
    {
        #region Singleton
        /// <summary>
        /// Thể hiện Singleton của PlayerManager, cho phép truy cập toàn cục.
        /// </summary>
        public static IPlayerManager Instance { get; private set; }
        #endregion

        #region Fields
        [Header("Player Settings")]
        [Tooltip("Prefab của người chơi sẽ được sinh ra trong game.")]
        [SerializeField] private GameObject _playerPrefab;

        // Phụ thuộc vào các manager khác
        private ISpawnManager _spawnManager;

        // Danh sách những người chơi đang hoạt động trong màn.
        private readonly List<IPlayer> _activePlayers = new();
        
        // Danh sách những người chơi đang tham gia round đấu hiện tại.
        private readonly List<IPlayer> _playersInRound = new();

        // Dữ liệu theo dõi từng người chơi xuyên suốt các round: win streak, thời điểm bắt đầu round, Extreme Mode.
        // Key là IPlayer của người chơi ĐANG TỒN TẠI (chưa bị hồi sinh/phá hủy). Khi người chơi chết, entry sẽ được xóa.
        private readonly Dictionary<IPlayer, PlayerRoundData> _playerData = new();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance as MonoBehaviour != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            // Lấy tham chiếu trong Start() để đảm bảo các Singleton khác đã được khởi tạo trong Awake().
            _spawnManager = SpawnManager.Instance;
        }

        private void OnEnable()
        {
            // Đăng ký lắng nghe sự kiện người chơi chết từ GameEvents
            GameEvents.OnPlayerDied += HandlePlayerDeath;
            // Clear active players when returning to Home so "Play" does not spawn a duplicate.
            GameEvents.OnReturnToHomeRequest += HandleReturnToHome;
            // Handles the Roblox-style reset request (no player argument).
            GameEvents.OnResetPlayerRequested += HandleResetPlayerRequested;
        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh rò rỉ bộ nhớ khi đối tượng bị hủy
            GameEvents.OnPlayerDied -= HandlePlayerDeath;
            GameEvents.OnReturnToHomeRequest -= HandleReturnToHome;
            GameEvents.OnResetPlayerRequested -= HandleResetPlayerRequested;
        }
        #endregion

        #region Public Methods (IPlayerManager Implementation)

        /// <summary>
        /// Bắt đầu quá trình sinh người chơi lần đầu tiên khi game bắt đầu.
        /// </summary>
        public void SpawnInitialPlayer()
        {
            SpawnPlayer();
        }

        /// <inheritdoc/>
        public List<IPlayer> GetAllPlayers()
        {
            // Trả về một bản sao của danh sách để tránh sửa đổi từ bên ngoài
            return new List<IPlayer>(_activePlayers);
        }

        /// <inheritdoc/>
        public List<IPlayer> GetPlayersInRound()
        {
            // Trả về một bản sao của danh sách để tránh sửa đổi từ bên ngoài.
            // Đây là danh sách những người chơi đang thực sự tham gia thi đấu.
            return new List<IPlayer>(_playersInRound);
        }


        public int GetAlivePlayerCount() => _playersInRound.Count;

        public void StartRound()
        {
            _playersInRound.Clear();
            foreach (var player in _activePlayers)
            {
                if (player != null && player.GameObject != null)
                {
                    // TODO(AFK): Khi triển khai hệ thống AFK, hãy BỎ QUA những người chơi đang AFK
                    // tại thời điểm này (không đưa vào _playersInRound), để:
                    // 1. Họ không bị dịch chuyển vào arena.
                    // 2. Độ khó (initialPlayerCountForRound trong GameloopManager) chỉ tính trên
                    //    danh sách non-AFK tham gia ban đầu.
                    player.GameObject.SetActive(true); // Đảm bảo người chơi được kích hoạt
                    _playersInRound.Add(player);
                }
            }
            Debug.Log($"[PlayerManager] Started round with {_playersInRound.Count} players.");            
        }

        /// <inheritdoc/>
        public List<IPlayer> GetPlayersNotInCurrentRound()
        {
            List<IPlayer> notInRound = new List<IPlayer>();
            foreach (var player in _activePlayers)
            {
                // Nếu người chơi đang hoạt động nhưng chưa có trong danh sách _playersInRound
                if (player != null && player.GameObject != null && !_playersInRound.Contains(player))
                {
                    notInRound.Add(player);
                }
            }
            return notInRound;
        }

        /// <inheritdoc/>
        public void AddPlayerToCurrentRound(IPlayer player)
        {
            if (player != null && player.GameObject != null && !_playersInRound.Contains(player))
            {
                player.GameObject.SetActive(true); // Đảm bảo người chơi được kích hoạt
                _playersInRound.Add(player);
            }
        }

        /// <inheritdoc/>
        public List<IPlayer> GetSurvivors()
        {
            List<IPlayer> survivors = new();
            foreach (var player in _playersInRound)
            {
                // Người chơi vẫn còn GameObject active trong danh sách round được coi là còn sống sót.
                if (player != null && player.GameObject != null)
                {
                    survivors.Add(player);
                }
            }
            return survivors;
        }

        /// <inheritdoc/>
        public PlayerRoundData GetPlayerRoundData(IPlayer player)
        {
            if (player == null) return null;

            if (_playerData.TryGetValue(player, out var existingData))
            {
                return existingData;
            }

            // Tạo dữ liệu mới nếu chưa tồn tại (ví dụ: người chơi vừa được hồi sinh sau khi chết).
            var newData = new PlayerRoundData();
            _playerData[player] = newData;
            return newData;
        }

        public void EndRound()
        {
            _playersInRound.Clear();
            // Kích hoạt lại tất cả người chơi (nếu cần) và reset trạng thái của họ cho vòng mới.
            foreach (var player in _activePlayers)
            {
                if (player?.GameObject == null) continue;

                if (!player.GameObject.activeSelf)
                {
                    player.GameObject.SetActive(true);
                }

            }

            // Phát một sự kiện toàn cục yêu cầu tất cả các component liên quan đến người chơi (như PlayerHealth, StatusEffectReceiver)
            // tự reset lại trạng thái của chúng. Bất kỳ component nào quan tâm sẽ lắng nghe sự kiện này.
            // Đây là cách tiếp cận nhất quán với kiến trúc của dự án.
            GameEvents.TriggerRoundEndPlayerReset();
        }

        /// <summary>
        /// Bắt đầu phát nhạc gameplay cho tất cả người chơi đang trong round.
        /// </summary>
        /// <param name="intensity">Độ khó của round đấu.</param>
        public void SetGameplayMusicForRoundPlayers(float intensity)
        {
            foreach (var player in _playersInRound)
            {
                player?.PlayGameplayMusic(intensity);
            }
        }

        /// <summary>
        /// Phát nhạc 30 giây cuối cho tất cả người chơi đang trong round.
        /// </summary>
        /// <param name="intensity">Độ khó của round đấu.</param>
        public void SetLast30sMusicForRoundPlayers(float intensity)
        {
            foreach (var player in _playersInRound)
            {
                player?.PlayLast30sMusic(intensity);
            }
        }

        /// <summary>
        /// Phát nhạc lobby cho tất cả người chơi đang hoạt động.
        /// </summary>
        public void SetLobbyMusicForAllPlayers()
        {
            foreach (var player in _activePlayers)
            {
                player?.PlayLobbyMusic();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Tìm một điểm spawn ngẫu nhiên và sinh ra người chơi tại đó.
        /// </summary>
        private void SpawnPlayer()
        {
            if (_spawnManager == null)
            {
                Debug.LogError("SpawnManager instance not found! Cannot spawn player. Make sure a SpawnManager exists in the scene.", this);
                return;
            }

            PlayerSpawn spawnPoint = _spawnManager.GetRandomSpawnPoint();
            if (spawnPoint == null)
            {
                Debug.LogError("No PlayerSpawn found in the scene! Cannot spawn player.", this);
                return;
            }

            IPlayer newPlayer = HandlePlayerSpawn(_playerPrefab, spawnPoint);
            if (newPlayer != null)
            {
                _activePlayers.Add(newPlayer);
                Debug.Log($"[PlayerManager] Player spawned and added to active list. Total players: {_activePlayers.Count}", newPlayer.GameObject);

                // LƯU Ý: KHÔNG phát nhạc lobby tại đây nữa. Việc phát nhạc được điều phối tập trung:
                // - Boot game / Home screen   -> BGMController.Start() phát HomeMusic.
                // - Nhấn Play từ Home         -> BGMController xử lý qua GameEvents.OnStartGameRequest.
                // - Chết trong round, hồi sinh về lobby -> UnifiedRespawnCoroutine (diedDuringRound = true).
                // - Reset character ngay tại lobby -> không phát lại, giữ nguyên nhạc hiện tại.
            }
        }

        /// <summary>
        /// Được gọi khi sự kiện GameEvents.OnPlayerDied được kích hoạt.
        /// Hủy đối tượng người chơi cũ và bắt đầu coroutine hồi sinh.
        /// </summary>
        private void HandlePlayerDeath(IPlayer player)
        {
            if (player == null) return;

            // Nếu người chơi đang trong round, loại họ ra khỏi danh sách người chơi còn sống của round đó.
            // Lưu lại cờ này để quyết định việc phát lại nhạc lobby sau khi hồi sinh:
            // chỉ phát khi chết TRONG round; chết ngay tại lobby (Reset character) thì giữ nguyên nhạc hiện tại.
            bool diedDuringRound = _playersInRound.Contains(player);
            if (diedDuringRound)
            {
                Debug.Log($"[PlayerManager] Player {player.GameObject.name} eliminated from the round.", player.GameObject);

                // ===== SCORE CARD CÁ NHÂN CHO NGƯỜI THUA CUỘC =====
                // Thời gian sống sót được tính theo ĐỒNG HỒ CỦA ROUND (CurrentRoundElapsedTime của GameloopManager),
                // KHÔNG phải thời gian thực tế người chơi sống (Time.time - ...). Đúng theo yêu cầu:
                // chỉ được tính score nếu đã sống sót trên 30 giây theo thời gian tính giờ của round.
                var gameloop = GameloopManager.Instance;
                bool isDuringActiveRound = gameloop != null && gameloop.CurrentState == GameState.RoundActive;
                bool hasRoundRecord = _playerData.TryGetValue(player, out var roundData);

                float roundIntensity = 1f;
                float roundDuration = 150f;
                float survivedRoundTime = 0f;
                if (gameloop != null)
                {
                    roundIntensity = gameloop.CurrentIntensity;
                    roundDuration = gameloop.CurrentRoundDuration;
                    // Thời gian đã trôi qua theo đồng hồ của round tại thời điểm người chơi chết.
                    survivedRoundTime = isDuringActiveRound ? gameloop.CurrentRoundElapsedTime : 0f;
                }

                // Survival Score: trả về 0 nếu chưa sống đủ 30 giây theo đồng hồ round (ScoreRules.md).
                int survivalScore = ScoreCalculator.GetSurvivalScore(roundIntensity, survivedRoundTime, roundDuration);

                // Multiplier: x1.0 mặc định, x1.25 nếu bật Extreme Mode.
                float baseMultiplier = ScoreCalculator.GetMultiplier(hasRoundRecord && roundData.IsExtremeModeEnabled);

                // Win Multiplier là x1.0 vì người chơi đã thua.
                float winMultiplier = ScoreCalculator.DefaultMultiplier;

                // Total Credits = Survival Score x Multiplier x Win Multiplier.
                int totalCredits = ScoreCalculator.GetTotalCredits(survivalScore, baseMultiplier, winMultiplier);

                // CHỈ trao credits và hiển thị Score Card CÁ NHÂN khi người chơi đạt điểm
                // (tức là đã sống sót TRÊN 30 giây theo đồng hồ round -> survivalScore > 0).
                // Nếu chết trước 30 giây: không được điểm và KHÔNG hiển thị Score Card.
                if (isDuringActiveRound && survivalScore > 0)
                {
                    // B1. Đưa người chơi về Lobby TRƯỚC (teleport tới điểm spawn của sảnh chờ).
                    // Yêu cầu: "player về lobby rồi mới hiện score card".
                    RespawnPlayer(player);
                    Debug.Log($"[PlayerManager] Player {player.GameObject.name} về Lobby (chết trong round).", player.GameObject);

                    // B2. Trao credits và hiển thị Score Card dạng thua.
                    GameEvents.TriggerAddCreditsRequest(totalCredits);

                    ScoreCardData defeatScoreCard = new ScoreCardData
                    {
                        SurvivalScore = survivalScore,
                        BaseMultiplier = baseMultiplier,
                        WinMultiplier = winMultiplier,
                        TotalCredits = totalCredits,
                        IsWinner = false,
                    };
                    IUIManager.Instance?.ShowScoreCard(defeatScoreCard);
                    Debug.Log($"[PlayerManager] Hiển thị Score Card cá nhân cho người thua {player.GameObject.name}: Survival {survivalScore}, Credits {totalCredits}.", player.GameObject);
                }
                else if (isDuringActiveRound)
                {
                    Debug.Log($"[PlayerManager] Player {player.GameObject.name} died before 30 seconds (round clock). Không đạt điểm, không hiển thị Score Card.", player.GameObject);
                }

                // Reset chuỗi thắng của người chơi về 0 (bằng cách xóa record; round sau sẽ tạo mới với WinStreak = 0).
                _playerData.Remove(player);

                _playersInRound.Remove(player);
                // KHÔNG vô hiệu hóa GameObject ngay lập tức để hiệu ứng ragdoll có thể diễn ra.
            }

            // Bất kể chết trong round hay ở lobby, bắt đầu cùng một quy trình hồi sinh.
            // Quy trình này sẽ cho phép ragdoll hiển thị, sau đó phá hủy và tạo lại người chơi.
            Debug.Log($"[PlayerManager] Player {player.GameObject.name} died. Starting universal respawn process...", player.GameObject);
            StartCoroutine(UnifiedRespawnCoroutine(player, 3f, diedDuringRound)); // 3 giây là thời gian chờ hồi sinh
        }

        /// <summary>
        /// Coroutine xử lý việc hồi sinh người chơi: phá hủy người chơi cũ, đợi, và tạo người chơi mới.
        /// </summary>
        /// <param name="playerToDestroy">Người chơi cũ cần phá hủy.</param>
        /// <param name="respawnDelay">Thời gian chờ trước khi hồi sinh.</param>
        /// <param name="diedDuringRound">
        /// True nếu người chơi chết TRONG round (được hồi sinh về lobby -> phát lại nhạc lobby).
        /// False nếu chết ngay tại lobby (Reset character) -> giữ nguyên nhạc hiện tại.
        /// </param>
        private IEnumerator UnifiedRespawnCoroutine(IPlayer playerToDestroy, float respawnDelay, bool diedDuringRound)
        {
            // Xóa người chơi cũ khỏi danh sách quản lý chính.
            if (playerToDestroy != null)
            {
                _activePlayers.Remove(playerToDestroy);
            }

            // Đợi một khoảng thời gian để hiệu ứng "vỡ tung" (shatter) có thời gian diễn ra.
            yield return new WaitForSeconds(respawnDelay);

            // Sau khi đợi, phá hủy đối tượng người chơi cũ.
            if (playerToDestroy != null && playerToDestroy.GameObject != null)
            {
                Destroy(playerToDestroy.GameObject);
            }

            // Hồi sinh một người chơi hoàn toàn mới ngay lập tức tại một điểm spawn ở lobby.
            Debug.Log("[PlayerManager] Respawning new player in lobby.");
            SpawnPlayer(); // SpawnPlayer sẽ tự tìm điểm spawn ngẫu nhiên.

            // CHỈ phát lại nhạc sảnh chờ khi người chơi chết TRONG round và được hồi sinh về lobby.
            // Nếu chết/reset ngay tại lobby thì giữ nguyên nhạc đang phát (thường là nhạc lobby),
            // tránh việc nhạc bị phát lại từ đầu mỗi lần người chơi Reset character.
            if (diedDuringRound)
            {
                BGMController.Instance?.PlayLobbyMusic();
                Debug.Log("[PlayerManager] Player respawned into lobby after dying in round. Playing lobby music.");
            }
        }

        /// <summary>
        /// Xử lý việc sinh ra đối tượng người chơi tại một điểm spawn được chỉ định.
        /// </summary>
        /// <param name="playerPrefab">Prefab của người chơi để khởi tạo.</param>
        /// <param name="spawnPoint">Điểm spawn nơi người chơi sẽ xuất hiện.</param>
        /// <returns>Một interface IPlayer của đối tượng người chơi vừa được tạo, hoặc null nếu thất bại.</returns>
        public IPlayer HandlePlayerSpawn(GameObject playerPrefab, PlayerSpawn spawnPoint)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player Prefab is not assigned in PlayerManager! Cannot spawn player.", this);
                return null;
            }
            if (spawnPoint == null)
            {
                Debug.LogError("Invalid PlayerSpawn point provided! Cannot spawn player.", this);
                return null;
            }

            Vector3 finalSpawnPosition = spawnPoint.SpawnPoint;

            // Cố gắng lấy CapsuleCollider từ prefab để tính toán vị trí spawn chính xác.
            // Điều này sẽ đặt phần đáy của collider vật lý của người chơi ngay tại điểm spawn.
            if (playerPrefab.TryGetComponent<CapsuleCollider>(out var capsule))
            {
                // Tính toán độ dời theo chiều dọc để đảm bảo chân của CapsuleCollider (điểm thấp nhất)
                // được đặt chính xác trên SpawnPoint, thay vì pivot của GameObject.
                // Công thức: pivot.y = ground.y + (nửa chiều cao - vị trí tâm của collider theo trục y).
                float verticalOffset = (capsule.height / 2f) - capsule.center.y;
                // Áp dụng độ dời vào vị trí spawn cuối cùng.
                finalSpawnPosition += new Vector3(0, verticalOffset, 0);
            }

            // Sinh người chơi tại vị trí SpawnPoint đã xác định
            GameObject spawnedPlayerObject = Instantiate(playerPrefab, finalSpawnPosition, Quaternion.identity);
            Debug.Log($"[PlayerManager] Player spawned at: {finalSpawnPosition} (Base ground: {spawnPoint.SpawnPoint})", spawnedPlayerObject);

            // Kiểm tra xem prefab có triển khai interface IPlayer hay không.
            if (!spawnedPlayerObject.TryGetComponent<IPlayer>(out var playerInterface))
            {
                Debug.LogError($"Player Prefab '{playerPrefab.name}' does not have a component that implements IPlayer! Destroying spawned object.", spawnedPlayerObject);
                Destroy(spawnedPlayerObject);
                return null;
            }

            return playerInterface;
        }

        /// <summary>
        /// Lấy tham chiếu đến người chơi hiện tại đang được quản lý.
        /// </summary>
        /// <returns>Interface IPlayer của người chơi hiện tại, hoặc null nếu chưa có.</returns>
        public IPlayer GetCurrentPlayer()
        {
            return _activePlayers.Count > 0 ? _activePlayers[0] : null;
        }

        /// <summary>
        /// Lấy vị trí của một người chơi ngẫu nhiên đang hoạt động.
        /// </summary>
        /// <returns>Vị trí của người chơi ngẫu nhiên, hoặc Vector3.zero nếu không có người chơi nào.</returns>
        public Vector3 GetRandomPlayerPosition()
        {
            if (_activePlayers.Count == 0) return Vector3.zero;

            IPlayer randomPlayer = _activePlayers[Random.Range(0, _activePlayers.Count)];
            return randomPlayer.GameObject.transform.position;
        }

        /// <summary>
        /// Hồi sinh một người chơi tại một điểm spawn được chỉ định.
        /// </summary>
        /// <param name="player">Người chơi cần hồi sinh.</param>
        /// <param name="spawnPoint">Điểm spawn để hồi sinh người chơi. Nếu null, sẽ tìm một điểm ngẫu nhiên.</param>
        public void RespawnPlayer(IPlayer player, PlayerSpawn spawnPoint = null)
        {
            if (player == null)
            {
                Debug.LogError("Cannot respawn null player.", this);
                return;
            }
            if (spawnPoint == null)
            {
                // Nếu không có điểm spawn cụ thể, hãy lấy một điểm ngẫu nhiên.
                spawnPoint = _spawnManager?.GetRandomSpawnPoint();
                if (spawnPoint == null)
                {
                    Debug.LogError("Cannot respawn player, no valid spawn point found.", this);
                    return;
                }
            }

            // Di chuyển người chơi đến vị trí của điểm spawn.
            player.Teleport(spawnPoint.SpawnPoint);
        }

        /// <summary>
        /// Dịch chuyển những người chơi còn sống trong round về sảnh chờ.
        /// </summary>
        public void ReturnRoundSurvivorsToLobby()
        {
            foreach (var player in _playersInRound)
            {
                if (player != null && player.GameObject != null)
                {
                    // Người chơi còn sống thì đã active, chỉ cần dịch chuyển họ.
                    RespawnPlayer(player); // Respawn sẽ tìm một điểm spawn ngẫu nhiên ở sảnh và dịch chuyển.
                }
            }
        }

        /// <summary>
        /// Xóa tất cả các đối tượng người chơi đang hoạt động.
        /// </summary>
        public void ClearAllPlayers()
        {
            // Tạo một bản sao của danh sách để lặp qua, vì việc hủy đối tượng có thể kích hoạt OnDisable và sửa đổi danh sách gốc.
            var playersToClear = new List<IPlayer>(_activePlayers);
            foreach (var player in playersToClear) Destroy(player.GameObject);
            _activePlayers.Clear();
        }

        /// <summary>
        /// Handles the Roblox-style character reset request: the current player is told to die.
        /// </summary>
        private void HandleResetPlayerRequested()
        {
            var currentPlayer = GetCurrentPlayer();
            if (currentPlayer == null)
            {
                Debug.LogWarning("[PlayerManager] No current player is available to reset.");
                return;
            }
            GameEvents.TriggerPlayerResetRequested(currentPlayer);
        }

        /// <summary>
        /// Called when the player returns to the Home screen (Back to Home).
        /// Destroys all active players so pressing Play again does not spawn a duplicate.
        /// </summary>
        private void HandleReturnToHome()
        {
            Debug.Log("[PlayerManager] Return to Home requested. Clearing all active players.");
            ClearAllPlayers();
        }
        #endregion
    }
}