using UnityEngine;
using Core.Interfaces;
using Core;
using Core.Enums;
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

        // Trang thai AFK (runtime - KHONG luu tren player data).
        // Mac dinh TAT (false) khi nguoi choi vao game tu Home (player khong AFK, tham gia arena).
        // Nguoi choi bat/tat qua Option Menu. Khi BAT (true): player khong duoc dua vao round / khong bi teleport vao arena.
        private bool _afkEnabled = false;

        // Gia tri AFK dang CHO ap dung khi dang trong round (chi valid neu _hasPendingAfkEnabled = true).
        // Dung de tri hoan thay doi AFK neu nguoi choi bat/tat luc dang trong round,
        // ap dung sau khi round ket thuc (xem RoundStateHelper).
        private bool _pendingAfkEnabled;
        private bool _hasPendingAfkEnabled;
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
                // Gán luôn qua interface để các assembly khác (UI) có thể truy cập qua IPlayerManager.Instance.
                // Nếu không có dòng này, IPlayerManager.Instance (static của interface) sẽ luôn null vì nó không
                // tự động nhận giá trị từ static của lớp PlayerManager.
                IPlayerManager.Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            // Lấy tham chiếu trong Start() để đảm bảo các Singleton khác đã được khởi tạo trong Awake().
            _spawnManager = SpawnManager.Instance;
        }

        private void Update()
        {
            // Ap dung thay đoi AFK (neu dang CHO) khi khong con trong round
            // (luc dang o lobby / giua cac round). Dieu nay dam bao ta cau
            // bat/tat AFK trong round khong ap dung giua chung.
            if (_hasPendingAfkEnabled && !RoundStateHelper.IsInRound())
            {
                bool pendingValue = _pendingAfkEnabled;
                _hasPendingAfkEnabled = false;
                ApplyAfkEnabled(pendingValue);
            }
        }

        private void OnEnable()
        {
            // Đăng ký lắng nghe sự kiện người chơi chết từ GameEvents
            GameEvents.OnPlayerDied += HandlePlayerDeath;
            // Clear active players when returning to Home so "Play" does not spawn a duplicate.
            GameEvents.OnReturnToHomeRequest += HandleReturnToHome;
            // Handles the Roblox-style reset request (no player argument).
            GameEvents.OnResetPlayerRequested += HandleResetPlayerRequested;
            // Xu ly thay doi trang thai AFK tu Option Menu.
            GameEvents.OnAfkEnabledChanged += HandleAfkEnabledChanged;
            // Cung cap trang thai AFK runtime cho cac he thong khac (PlayerAfkIndicator...).
            GameEvents.OnRequestAfkEnabled += GetAfkEnabled;
        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh rò rỉ bộ nhớ khi đối tượng bị hủy
            GameEvents.OnPlayerDied -= HandlePlayerDeath;
            GameEvents.OnReturnToHomeRequest -= HandleReturnToHome;
            GameEvents.OnResetPlayerRequested -= HandleResetPlayerRequested;
            GameEvents.OnAfkEnabledChanged -= HandleAfkEnabledChanged;
            GameEvents.OnRequestAfkEnabled -= GetAfkEnabled;
        }
        #endregion

        #region Public Methods (IPlayerManager Implementation)

        /// <summary>
        /// Bắt đầu quá trình sinh người chơi lần đầu tiên khi game bắt đầu (vào game từ Home).
        /// </summary>
        public void SpawnInitialPlayer()
        {
            // AFK mac dinh TAT (false) khi nguoi choi vao game tu Home: ban dau player KHONG AFK
            // (tham gia arena binh thuong), va phai tu bat AFK trong Option Menu neu muon nghi khong vaoc arena.
            ResetAfkToDefault();

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
            bool isExtremeMode = GameEvents.TriggerRequestExtremeModeEnabled();

            // Lay trang thai AFK (runtime) de loai toan bo player AFK ra khoi round.
            bool anyPlayerAfk = _afkEnabled;

            foreach (var player in _activePlayers)
            {
                if (player != null && player.GameObject != null)
                {
                    if (anyPlayerAfk) continue;

                    player.GameObject.SetActive(true); // Đảm bảo người chơi được kích hoạt
                    _playersInRound.Add(player);

                    // Cap nhat trang thai Extreme Mode cho player khi bat dau round
                    var roundData = GetPlayerRoundData(player);
                    if (roundData != null)
                    {
                        roundData.IsExtremeModeEnabled = isExtremeMode;
                    }
                }
            }
            Debug.Log($"[PlayerManager] Started round with {_playersInRound.Count} players. Extreme Mode: {isExtremeMode}");
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
            var newData = new PlayerRoundData
            {
                // Ghi nhận trạng thái Extreme Mode hiện tại (đã lưu) để tính điểm x1.25 cho round.
                IsExtremeModeEnabled = GameEvents.TriggerRequestExtremeModeEnabled()
            };
            _playerData[player] = newData;
            return newData;
        }

        /// <inheritdoc/>
        public void SetRoundSurvivorsInvincible(bool invincible)
        {
            foreach (var player in _playersInRound)
            {
                if (player != null && player.GameObject != null)
                {
                    if (player.GameObject.TryGetComponent<IInvincible>(out var inv))
                    {
                        inv.IsInvincible = invincible;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void ChargeRoundPlayersSkills()
        {
            foreach (var player in _playersInRound)
            {
                if (player != null && player.GameObject != null)
                {
                    ISkillController skillController = player.GameObject.GetComponentInChildren<ISkillController>();
                    skillController?.ChargeSkill();
                }
            }
        }

        /// <inheritdoc/>
        public void SetRoundPlayersSkillLock(bool locked)
        {
            foreach (var player in _playersInRound)
            {
                if (player != null && player.GameObject != null)
                {
                    ISkillController skillController = player.GameObject.GetComponentInChildren<ISkillController>();
                    skillController?.SetSkillLock(locked);
                }
            }
        }

        public void EndRound()
        {
            // Mo khoa skill cho tat ca active players khi ve lobby
            foreach (var player in _activePlayers)
            {
                if (player?.GameObject != null)
                {
                    ISkillController skillController = player.GameObject.GetComponentInChildren<ISkillController>();
                    skillController?.SetSkillLock(false);
                }
            }

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
        /// Duoc goi khi su kien GameEvents.OnPlayerDied duoc kich hoat.
        /// Huy doi tuong nguoi choi cu va bat dau coroutine hoi sinh.
        /// </summary>
        private void HandlePlayerDeath(IPlayer player)
        {
            if (player == null) return;

            // Neu nguoi choi dang trong round, loai ho ra khoi danh sach nguoi choi con song cua round do.
            bool diedDuringRound = _playersInRound.Contains(player);
            if (diedDuringRound)
            {
                Debug.Log($"[PlayerManager] Player {player.GameObject.name} eliminated from the round.", player.GameObject);

                // ===== SCORE CARD CA NHAN CHO NGUOI THUA CUOC =====
                // Thoi gian song sot duoc tinh theo DONG HO CUA ROUND (CurrentRoundElapsedTime cua GameloopManager).
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
                    survivedRoundTime = isDuringActiveRound ? gameloop.CurrentRoundElapsedTime : 0f;
                }

                // Survival Score: tra ve 0 neu chua song du 30 giay theo dong ho round (ScoreRules.md).
                int survivalScore = ScoreCalculator.GetSurvivalScore(roundIntensity, survivedRoundTime, roundDuration);

                // Multiplier: x1.0 mac dinh, x1.25 neu bat Extreme Mode.
                float baseMultiplier = ScoreCalculator.GetMultiplier(hasRoundRecord && roundData.IsExtremeModeEnabled);

                // Win Multiplier la x1.0 vi nguoi choi da thua.
                float winMultiplier = ScoreCalculator.DefaultMultiplier;

                // Total Credits = Survival Score x Multiplier x Win Multiplier.
                int totalCredits = ScoreCalculator.GetTotalCredits(survivalScore, baseMultiplier, winMultiplier);

                ScoreCardData? defeatScoreCard = null;
                // CHI tao Score Card khi nguoi choi dat diem (song sot tren 30 giay theo dong ho round).
                if (isDuringActiveRound && survivalScore > 0)
                {
                    defeatScoreCard = new ScoreCardData
                    {
                        SurvivalScore = survivalScore,
                        BaseMultiplier = baseMultiplier,
                        WinMultiplier = winMultiplier,
                        TotalCredits = totalCredits,
                        IsWinner = false,
                        WinStreak = 0,
                        IsExtremeMode = hasRoundRecord && roundData.IsExtremeModeEnabled
                    };
                }
                else if (isDuringActiveRound)
                {
                    Debug.Log($"[PlayerManager] Player {player.GameObject.name} died before 30 seconds (round clock). Khong dat diem, khong hien thi Score Card.", player.GameObject);
                }

                // Reset chuoi thang cua nguoi choi ve 0
                _playerData.Remove(player);
                _playersInRound.Remove(player);

                // Bat dau quy trinh chet trong round: cho 2 giay de xem Death VFX va ragdoll -> Transition Screen + Score Card -> ve Lobby
                StartCoroutine(RoundPlayerDeathSequenceCoroutine(player, defeatScoreCard, totalCredits));
            }
            else
            {
                // Chet ngoai round (Reset character tai lobby)
                Debug.Log($"[PlayerManager] Player {player.GameObject.name} died in lobby. Starting respawn process...", player.GameObject);
                StartCoroutine(UnifiedRespawnCoroutine(player, 3f, false));
            }
        }

        /// <summary>
        /// Coroutine xu ly quy trinh khi player chet trong round:
        /// 1. Cho 1 giay tai vi tri chet.
        /// 2. Hien thi Score Card bat dau chay len dong thoi voi Transition Screen bat dau chay.
        /// 3. Khi man hinh duoc che kin (onCovered), teleport player ve diem spawn o lobby.
        /// 4. Huy GameObject player cu va sinh player moi tai lobby khi transition hoan tat.
        /// </summary>
        private IEnumerator RoundPlayerDeathSequenceCoroutine(IPlayer playerToDestroy, ScoreCardData? defeatScoreCard, int totalCredits)
        {
            // B1: Cho 1 giay tai vi tri chet truoc khi bat dau Score Card va Transition Screen
            yield return new WaitForSeconds(1.0f);

            // B2: Hien thi Score Card ca nhan cho nguoi choi (Score Card bat dau truot len)
            if (defeatScoreCard.HasValue)
            {
                GameEvents.TriggerAddCreditsRequest(totalCredits);
                IUIManager.Instance?.ShowScoreCard(defeatScoreCard.Value);
                Debug.Log($"[PlayerManager] Hien thi Score Card ca nhan cho nguoi thua {playerToDestroy?.GameObject.name}: Survival {defeatScoreCard.Value.SurvivalScore}, Credits {totalCredits}.");
            }

            // B3: Kich hoat Transition Screen dong thoi voi Score Card
            bool transitionCovered = false;
            bool transitionCompleted = false;

            if (IUIManager.Instance != null)
            {
                IUIManager.Instance.PlayTransition(
                    onCovered: () =>
                    {
                        transitionCovered = true;

                        // Teleport nguoi choi ve diem spawn o sanh cho khi man hinh da duoc che kin
                        if (playerToDestroy != null)
                        {
                            RespawnPlayer(playerToDestroy);
                        }
                    },
                    onComplete: () =>
                    {
                        transitionCompleted = true;
                    }
                );
            }
            else
            {
                // Fallback neu khong co UIManager
                if (playerToDestroy != null)
                {
                    RespawnPlayer(playerToDestroy);
                }
                transitionCovered = true;
                transitionCompleted = true;
            }

            // Cho den khi transition da che man hinh va teleport xong
            while (!transitionCovered)
            {
                yield return null;
            }

            // Xoa player cu khoi danh sach active
            if (playerToDestroy != null)
            {
                _activePlayers.Remove(playerToDestroy);
            }

            // Cho transition hoan tat truoc khi huy va tao player moi de tranh giat man hinh
            while (!transitionCompleted)
            {
                yield return null;
            }

            if (playerToDestroy != null && playerToDestroy.GameObject != null)
            {
                Destroy(playerToDestroy.GameObject);
            }

            Debug.Log("[PlayerManager] Respawning new player in lobby after round death transition.");
            SpawnPlayer();

            BGMController.Instance?.PlayLobbyMusic();
        }

        /// <summary>
        /// Coroutine xử lý việc hồi sinh người chơi khi chết tại sảnh: phá hủy người chơi cũ, đợi, và tạo người chơi mới.
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
            Quaternion spawnRot = spawnPoint != null ? spawnPoint.SpawnRotation : Quaternion.LookRotation(Vector3.left);
            GameObject spawnedPlayerObject = Instantiate(playerPrefab, finalSpawnPosition, spawnRot);
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
            player.Teleport(spawnPoint.SpawnPoint, spawnPoint.SpawnRotation);
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
        /// Destroys all active players and stops all respawn/death coroutines.
        /// </summary>
        private void HandleReturnToHome()
        {
            Debug.Log("[PlayerManager] Return to Home requested. Stopping coroutines and clearing all active players.");
            StopAllCoroutines();
            _playersInRound.Clear();
            _playerData.Clear();
            ClearAllPlayers();
        }

        /// <summary>
        /// Xu ly khi nguoi choi bat/tat AFK tu Option Menu:
        /// - Neu dang TRONG round: tri hoan ap dung cho toi khi round ket thuc (khong ap dung giua round).
        /// - Neu khong trong round: ap dung lap tuc (cap nhat runtime + thong bao cac he thong khac).
        /// </summary>
        /// <param name="enabled">True neu bat AFK, false neu tat.</param>
        private void HandleAfkEnabledChanged(bool enabled)
        {
            // Neu trung voi trang thai dang ap dung -> khong lam gi (va xoa pending neu co).
            if (enabled == _afkEnabled)
            {
                _hasPendingAfkEnabled = false;
                return;
            }

            // Dang trong round: luu tam, ap dung sau khi round ket thuc.
            if (RoundStateHelper.IsInRound())
            {
                _pendingAfkEnabled = enabled;
                _hasPendingAfkEnabled = true;
                Debug.Log($"[PlayerManager] Dang trong round - luu trang thai AFK = {enabled} tam, se ap dung sau khi round ket thuc.");
                return;
            }

            ApplyAfkEnabled(enabled);
        }

        /// <summary>
        /// Ap dung gia tri AFK moi vao trang thai runtime va bao cac he thong khac (PlayerAfkIndicator...).
        /// Khi TAT AFK (enabled = false): viet lai rang buoc reset HP va max HP ve mac dinh
        /// cho player dang hoat dong, de player vua thoat khoi trang thai AFK
        /// (khong tham gia round truoc do) co day du mau khi quay lai arena.
        /// </summary>
        /// <param name="enabled">True neu bat AFK, false neu tat.</param>
        private void ApplyAfkEnabled(bool enabled)
        {
            _afkEnabled = enabled;
            Debug.Log($"[PlayerManager] AFK da thay doi: {(_afkEnabled ? "BAT" : "TAT")}");

            GameEvents.TriggerAfkStateChanged(_afkEnabled);

            // Khi tat AFK (player bat dau tham gia arena): reset HP va max HP ve mac dinh.
            if (!_afkEnabled)
            {
                GameEvents.TriggerRoundEndPlayerReset();
            }
        }

        /// <summary>
        /// Cap trang thai AFK runtime hien tai cho cac he thong khac (PlayerAfkIndicator...).
        /// </summary>
        /// <returns>True neu AFK dang bat.</returns>
        private bool GetAfkEnabled()
        {
            return _afkEnabled;
        }

        /// <summary>
        /// Reset rang thai AFK ve mac dinh TAT (false) khi nguoi choi vao game tu Home.
        /// Dieu nay dam bao: ban dau, player khong AFK (tham gia arena) va phai tu bat
        /// AFK trong Option Menu neu muon nghi khong vaoc arena.
        /// </summary>
        private void ResetAfkToDefault()
        {
            _hasPendingAfkEnabled = false;

            if (!_afkEnabled) return;

            _afkEnabled = false;
            Debug.Log("[PlayerManager] AFK reset mac dinh TAT (vao game tu Home).");
            GameEvents.TriggerAfkStateChanged(_afkEnabled);
        }
        #endregion
    }
}