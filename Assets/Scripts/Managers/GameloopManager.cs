using UnityEngine;
using Core;
using Core.Enums;
using Core.Interfaces;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Core.Interfaces.UI;

namespace Managers
{
    /// <summary>
    /// Quản lý luồng chơi chính (Game Loop), bao gồm việc khởi tạo, bắt đầu và kết thúc game.
    /// Lớp này đóng vai trò điều phối các Manager khác (như PlayerManager, SpawnManager, v.v.).
    /// </summary>

    public class GameloopManager : MonoBehaviour, IGameloopManager, IGameStateProvider
    {
        private static WaitForSeconds _waitForSeconds3 = new(3f);
        private static readonly WaitForSeconds _waitForSeconds2 = new(2f);
        private static readonly WaitForSeconds _waitForSeconds1 = new(1f);
        #region Properties

        /// <summary>
        /// Thể hiện Singleton của GameloopManager, cho phép truy cập toàn cục.
        /// </summary>
        public static GameloopManager Instance { get; private set; }

        /// <summary>
        /// Trạng thái hiện tại của vòng lặp game.
        /// </summary>
        public GameState CurrentState { get; private set; } = GameState.None;

        /// <summary>
        /// Độ khó hiện tại của round đấu.
        /// </summary>
        public float CurrentIntensity { get; private set; } = 1f;

        /// <summary>
        /// Độ khó dự kiến cho round tiếp theo.
        /// </summary>
        public float NextRoundIntensity => _nextRoundIntensity;

        /// <summary>
        /// Độ khó tối thiểu.
        /// </summary>
        public float MinIntensity => _minIntensity;

        /// <summary>
        /// Độ khó tối đa.
        /// </summary>
        public float MaxIntensity => _maxIntensity;

        /// <summary>
        /// Tổng thời gian (giây) của round đấu hiện tại.
        /// Dùng để tính toán Survival Score theo tỷ lệ thời gian sống sót.
        /// </summary>
        public float CurrentRoundDuration => _roundDuration;

        /// <summary>
        /// Thời gian CÒN LẠI (giây) theo đồng hồ của round đấu hiện tại.
        /// Cập nhật mỗi frame trong RoundActiveStage.
        /// </summary>
        public float CurrentRoundTimeRemaining => Mathf.Max(0f, _roundTimeRemaining);

        /// <summary>
        /// Thời gian ĐÃ TRÔI QUA (giây) theo đồng hồ của round đấu hiện tại.
        /// Dùng để xác định người thua có sống sót TRÊN 30 GIÂY theo thời gian của round hay không.
        /// </summary>
        public float CurrentRoundElapsedTime => Mathf.Max(0f, CurrentRoundDuration - CurrentRoundTimeRemaining);

        #endregion

        #region Fields

        // Tham chiếu đến các manager khác sẽ được lấy thông qua Singleton hoặc Service Locator.
        private IPlayerManager _playerManager;
        private IMapManager _mapManager;
        private IBombSpawnerManager _bombSpawnerManager;
        private IUIManager _uiManager;
        private ICollectiblePoolManager _collectiblePoolManager;

        // Tracks the running game loop so it can be stopped when returning to Home
        // and restarted cleanly when the player presses Play again.
        private bool _gameLoopActive = false;
        private Coroutine _gameLoopCoroutine;
        // Tracks the currently-running stage coroutine so it can be stopped too.
        // Stopping only the main loop left nested stages running, causing parallel
        // coroutines (e.g. two intermission timers) after returning Home and pressing Play again.
        private Coroutine _activeStageCoroutine;

        [Header("Game Loop Settings")]
        [Tooltip("Thời gian (giây) cho giai đoạn chào mừng 'Welcome to Kaboom Chaos!' khi bấm Play. Player đã được spawn, đồng hồ hiển thị 0:00 trong giai đoạn này.")]
        [SerializeField] private float _welcomeDuration = 5f;
        [Tooltip("Thoi gian (giay) cho giai doan nghi giua cac round / intermission.")]
        [FormerlySerializedAs("_votingDuration")]
        [SerializeField] private int _intermissionDuration = 15;
        [Tooltip("Thời gian (giây) hiển thị tên map đã được chọn.")]
        [SerializeField] private int _mapRevealDuration = 3;
        [Tooltip("Thời gian (giây) chờ để ổn định FPS sau khi xây map.")]
        [SerializeField] private int _postBuildStabilizationDuration = 3;
        [Tooltip("Thời gian (giây) chờ sau khi dịch chuyển người chơi vào đấu trường.")]
        [SerializeField] private int _postTeleportWaitDuration = 3;
        [Tooltip("Thời gian (giây) của một round đấu.")]
        [SerializeField] private int _roundDuration = 150; // 2 phút 30 giây
        [Tooltip("Thời gian (giây) chờ sau khi round đấu kết thúc.")]
        [SerializeField] private int _postRoundWaitDuration = 3;
        [Tooltip("Thời gian (giây) chờ để ổn định FPS sau khi dọn dẹp map.")]
        [SerializeField] private int _postCleanupStabilizationDuration = 2;

        [Header("Intensity Settings")]
        [Tooltip("Intensity toi thieu.")]
        [SerializeField] private float _minIntensity = 1f;
        [Tooltip("Intensity toi da.")]
        [SerializeField] private float _maxIntensity = 6f;
        [Tooltip("Intensity khoi tao cho round dau tien cua mot phien choi moi.")]
        [SerializeField] private float _initialIntensity = 1f;
        [Tooltip("Muc tang intensity co dinh cho round ke tiep sau mot round co nguoi song sot.")]
        [SerializeField] private float _intensityIncrementPerRound = 0.25f;
        [Tooltip("Muc giam intensity toi da khi toan bo player that bai trong round.")]
        [SerializeField] private float _intensityMaxDeathPenalty = 0.3f;

        [Header("Dynamic Positioning Offsets")]
        [Tooltip("Offset tọa độ Y cho khu vực spawn người chơi trong đấu trường, tính từ mặt trên của map (MapTopY). Dùng giá trị âm để đặt thấp hơn.")]
        [SerializeField] private float _arenaSpawnAreaYOffset = 0f;
        [Tooltip("Offset tọa độ Y cho khu vực sinh bom, tính từ mặt trên của map (MapTopY). Dùng giá trị âm để đặt thấp hơn.")]
        [SerializeField] private float _bombSpawnerYOffset = 0f;
        [Tooltip("Offset tọa độ Y cho đường biên trên, tính từ mặt trên của map (MapTopY).")]
        [SerializeField] private float _topBorderYOffset = 5f;

        // Scene References (obtained from SceneObjectRegistry)
        private BoxCollider _arenaSpawnArea;
        private Transform _topBorder;

        // Game loop state
        private int _selectedMapIndex = -1;
        private int _selectedUndergroundIndex = -1;
        // Trạng thái độ khó cho round tiếp theo.
        private float _nextRoundIntensity = 1f;

        // Số lượng người chơi đã chết trong round hiện tại, dùng để tính penalty intensity.
        private int _playersWhoDiedInRound = 0;

        // Số lượng người chơi ban đầu tham gia round hiện tại, dùng để chuẩn hóa penalty.
        private int _initialPlayerCountForRound = 0;


        // Đồng hồ round hiện tại (giây còn lại). Dùng để tính Survival Score theo thời gian của round.
        private float _roundTimeRemaining = 0f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                // Gan luon qua interface de cac assembly khac (UI) co the truy cap qua interface.
                IGameloopManager.Instance = this;
                IGameStateProvider.Instance = this;
                DontDestroyOnLoad(gameObject); // Giữ Manager tồn tại khi chuyển đổi giữa các scene.
            }
        }

                private void OnEnable()
        {
            // Đăng ký lắng nghe sự kiện người chơi chết để điều chỉnh độ khó
            GameEvents.OnPlayerDied += HandlePlayerDeath;
            GameEvents.OnStartGameRequest += StartGame;
            GameEvents.OnSpawnInitialPlayerRequest += HandleSpawnInitialPlayer;
            // Stop the running game loop when the player returns to the Home screen.
            GameEvents.OnReturnToHomeRequest += HandleReturnToHome;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDeath;
            GameEvents.OnStartGameRequest -= StartGame;
            GameEvents.OnSpawnInitialPlayerRequest -= HandleSpawnInitialPlayer;
            GameEvents.OnReturnToHomeRequest -= HandleReturnToHome;
        }

        private void Start()
        {
            // Lấy tham chiếu đến các manager khác trong Start() để đảm bảo các Singleton của chúng đã được khởi tạo trong Awake().
            // Việc này được đảm bảo bởi Bootstrapper.
            _playerManager = PlayerManager.Instance;
            _mapManager = MapManager.Instance;
            _bombSpawnerManager = BombSpawnerManager.Instance;
            _uiManager = IUIManager.Instance;
            _collectiblePoolManager = CollectiblePoolManager.Instance;

            // Lấy tham chiếu đến các đối tượng trong scene từ Registry
            var registry = SceneObjectRegistry.Instance;
            if (registry != null)
            {
                _arenaSpawnArea = registry.ArenaSpawnArea;
                _topBorder = registry.TopBorder;
            }
            else Debug.LogError("[GameloopManager] SceneObjectRegistry.Instance is null!", this);

            // Khoi tao va phat event NextRoundIntensity ban dau
            SetNextRoundIntensity(_initialIntensity);
        }

        /// <summary>
        /// Thiet lap gia tri NextRoundIntensity va phat event thong bao cho cac UI/World display.
        /// </summary>
        private void SetNextRoundIntensity(float value)
        {
            _nextRoundIntensity = Mathf.Clamp(value, _minIntensity, _maxIntensity);
            GameEvents.TriggerNextRoundIntensityChanged(_nextRoundIntensity, _minIntensity, _maxIntensity);
        }

        public void StartGame()
        {
            // If a previous game loop is still running (e.g. the player returned to Home
            // without it being stopped), stop it before starting a fresh session so that
            // pressing Play again begins a completely new loop.
            StopGameLoop();

            // Đặt lại hạt giống độ khó về giá trị khởi tạo đã cấu hình trong Inspector,
            // để round đầu tiên của phiên mới không bị ép về luôn _minIntensity mặc định.
            SetNextRoundIntensity(_initialIntensity);

            _gameLoopActive = true;

            // Bắt đầu vòng lặp game chính
            _gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
        }

        private void HandleSpawnInitialPlayer()
        {
            _playerManager?.SpawnInitialPlayer();
        }

        /// <summary>
        /// Stops the currently running game loop and cleans up all active systems.
        /// </summary>
        private void StopGameLoop()
        {
            _gameLoopActive = false;

            // Dung tat ca coroutine dang chay tren GameloopManager
            StopAllCoroutines();
            _activeStageCoroutine = null;
            _gameLoopCoroutine = null;

            // Reset GameState ve None
            CurrentState = GameState.None;
            _selectedMapIndex = -1;

            // Don dep bom va spawner
            _bombSpawnerManager?.StopSpawning();
            _bombSpawnerManager?.ClearAllBombs();

            // Don dep map ngay lap tuc
            _mapManager?.ClearCurrentMap();

            // Don dep vat pham thu thap
            _collectiblePoolManager?.ClearAllCollectibles();

            // Phat event don dep chung (hazard zones, skills, VFX...)
            GameEvents.TriggerRoundEndCleanup();

            // Dung toan bo SFX
            SfxService.Instance?.StopAllSfx();

            // An cac thanh phan UI gameplay
            _uiManager?.HideCountdown();
            _uiManager?.HideScoreCard();

            // Reset do kho
            SetNextRoundIntensity(_initialIntensity);
        }

        /// <summary>
        /// Called when the player returns to the Home screen. Stops the game loop
        /// and performs a complete state reset.
        /// </summary>
        private void HandleReturnToHome()
        {
            Debug.Log("[GameloopManager] Return to Home requested. Stopping game loop and resetting everything.");
            StopGameLoop();
        }

        #endregion

        #region Game Loop Coroutine

        /// <summary>
        /// Coroutine chính điều khiển toàn bộ vòng lặp của game, từ lúc chờ, chọn map, thi đấu, cho đến khi kết thúc và lặp lại.
        /// </summary>
        private IEnumerator GameLoopCoroutine()
        {
            // --- Giai đoạn 0: Chào mừng ---
            // Chay dung mot lan moi phien choi (sau khi bam Play), truoc khi buoc vao vong lap map.
            // Player da duoc spawn trong StartGame() va co the di chuyen binh thuong.
            _activeStageCoroutine = StartCoroutine(WelcomeStage());
            yield return _activeStageCoroutine;
            _activeStageCoroutine = null;

            while (_gameLoopActive)
            {
                // Track each stage so StopGameLoop() can cancel it if the player leaves mid-stage.
                _activeStageCoroutine = StartCoroutine(IntermissionStage());
                yield return _activeStageCoroutine;
                _activeStageCoroutine = null;
                if (!_gameLoopActive) break;

                if (_selectedMapIndex != -1)
                {
                    _activeStageCoroutine = StartCoroutine(BuildingStage());
                    yield return _activeStageCoroutine;
                    _activeStageCoroutine = null;
                    if (!_gameLoopActive) break;

                    _activeStageCoroutine = StartCoroutine(PreRoundStage());
                    yield return _activeStageCoroutine;
                    _activeStageCoroutine = null;
                    if (!_gameLoopActive) break;

                    _activeStageCoroutine = StartCoroutine(RoundActiveStage());
                    yield return _activeStageCoroutine;
                    _activeStageCoroutine = null;
                    if (!_gameLoopActive) break;

                    _activeStageCoroutine = StartCoroutine(PostRoundStage());
                    yield return _activeStageCoroutine;
                    _activeStageCoroutine = null;
                    if (!_gameLoopActive) break;
                }
                else
                {
                    Debug.LogWarning("[GameloopManager] Không có map nào được chọn để xây dựng. Bỏ qua chu kỳ.");
                    yield return _waitForSeconds2; // Chờ một chút trước khi lặp lại
                }

                if (!_gameLoopActive) break;

                Debug.Log("[GameloopManager] Khởi động lại vòng lặp...");
                yield return _waitForSeconds1; // Chờ 1 giây trước khi lặp lại
            }

            _gameLoopCoroutine = null;
        }

        #region Game Loop Stages

        /// <summary>
        /// Giai đoạn chào mừng (chạy 1 lần sau khi bấm Play): hiển thị thông báo
        /// "Welcome to Kaboom Chaos!" và đồng hồ 0:00 trong khoảng _welcomeDuration giây,
        /// sau đó mới bước vào vòng lặp map (intermission/build/round) thực sự.
        /// Player đã được spawn bởi StartGame() trước khi stage này chạy.
        /// </summary>
        /// <returns>IEnumerator để chạy như một coroutine.</returns>
        private IEnumerator WelcomeStage()
        {
            // Hien thi thong bao chao mung (khong tu dong an, dung persistent).
            _uiManager?.ShowPersistentNotification("Welcome to Kaboom Chaos!");

            // Hien thi dong ho 0:00 trong luc chao mung.
            _uiManager?.UpdateTimer(0);

            Debug.Log($"[GameloopManager] Giai đoạn 0: Chào mừng trong {_welcomeDuration} giây.");
            yield return new WaitForSeconds(_welcomeDuration);
        }

        /// <summary>
        /// Giai doan 1: Giai doan nghi giua cac round (Intermission). Hien thi timer, chon map ngau nhien va thong bao ket qua.
        /// </summary>
        /// <returns>IEnumerator để chạy như một coroutine.</returns>
        private IEnumerator IntermissionStage()
        {
            // --- Giai doan 1: Intermission ---
            CurrentState = GameState.Intermission;
            Debug.Log("[GameloopManager] Giai doan 1: Intermission");
            _uiManager?.ShowPersistentNotification("Intermission");

            // Bat dau vong lap dem nguoc cho giai doan intermission, dong thoi hien thi timer.
            float intermissionTimer = _intermissionDuration;
            int lastDisplayedSecondForIntermission = Mathf.CeilToInt(intermissionTimer);
            _uiManager?.UpdateTimer(lastDisplayedSecondForIntermission); // Hien thi timer ngay lap tuc

            while (intermissionTimer > 0f)
            {
                intermissionTimer -= Time.deltaTime;
                int currentSecond = Mathf.CeilToInt(intermissionTimer);

                if (currentSecond < lastDisplayedSecondForIntermission)
                {
                    lastDisplayedSecondForIntermission = currentSecond;
                    _uiManager?.UpdateTimer(lastDisplayedSecondForIntermission);
                }
                yield return null;
            }
            // Chon map ngau nhien
            _selectedMapIndex = -1;
            _selectedUndergroundIndex = -1;
            string selectedMapName = "Unknown Map";

            if (_mapManager != null)
            {
                int mapCount = _mapManager.GetMapCount();
                int undergroundCount = _mapManager.GetUndergroundDataCount();
                if (mapCount > 0 && undergroundCount > 0)
                {
                    _selectedMapIndex = Random.Range(0, mapCount);
                    _selectedUndergroundIndex = Random.Range(0, undergroundCount);
                    // Lay ten map duoc chon de hien thi thong bao.
                    // Can ep kieu vi IMapManager co the chua duoc cap nhat.
                    selectedMapName = (_mapManager as MapManager)?.GetMapNameByIndex(_selectedMapIndex) ?? "Invalid Map";
                }
            }

            // Thong bao map da duoc chon
            _uiManager?.HideTimer(); // An timer cua intermission truoc khi hien thong bao map
            _uiManager?.ShowPersistentNotification($"Map selected: {selectedMapName}");
            yield return new WaitForSeconds(_mapRevealDuration);
        }

        /// <summary>
        /// Giai đoạn 2: Xây dựng map đã chọn. Hiển thị thông báo, gọi MapManager để tạo map, và chờ để ổn định FPS.
        /// </summary>
        /// <returns>IEnumerator để chạy như một coroutine.</returns>
        private IEnumerator BuildingStage()
        {
            // --- Giai đoạn 2: Xây dựng Map ---
            CurrentState = GameState.Building;
            Debug.Log("[GameloopManager] Giai đoạn 2: Xây dựng Map");
            _uiManager?.ShowPersistentNotification("Building map...");

            // Xây dựng map đã chọn một cách bất đồng bộ
            yield return StartCoroutine(BuildSelectedMap(_selectedMapIndex, _selectedUndergroundIndex));

            // SỬA LỖI: Ngay sau khi build xong map, định vị lại tọa độ Y của ArenaSpawnArea/BombSpawner/TopBorder
            // theo MapTopY. Bước này PHẢI tách khỏi TeleportPlayersToArena() và chạy TRƯỚC khi teleport player để
            // physics kịp đồng bộ Collider.bounds của Arena (tránh đọc bounds cũ ngay trong cùng frame khiến player
            // spawn nhầm vị trí, ví dụ nằm trên TopBorder).
            UpdateArenaEnvironmentPosition();

            // Chờ một chút để FPS ổn định sau khi tải nhiều đối tượng
            Debug.Log($"[GameloopManager] Map đã xây xong. Chờ ổn định trong {_postBuildStabilizationDuration} giây.");
            yield return new WaitForSeconds(_postBuildStabilizationDuration);

            // Yeu cau: khong an notification panel, giu nguyen hien thi va doi noi dung thanh Get ready.
            _uiManager?.ShowPersistentNotification("Get ready...");
        }

        /// <summary>
        /// Giai đoạn 3 & 4: Chuẩn bị trước round đấu. Dịch chuyển người chơi vào đấu trường và bắt đầu đếm ngược.
        /// </summary>
        /// <returns>IEnumerator để chạy như một coroutine.</returns>
        private IEnumerator PreRoundStage()
        {
            // --- Giai đoạn 3: Dịch chuyển người chơi ---
            CurrentState = GameState.PreRound;
            Debug.Log("[GameloopManager] Giai đoạn 3: Dịch chuyển người chơi");

            // 1. Hiển thị Timer của round trước khi dịch chuyển người chơi.
            _uiManager?.UpdateTimer(_roundDuration);

            _playerManager?.StartRound(); // Chuẩn bị danh sách người chơi cho round mới

            // Chay TransitionScreen truoc khi Teleport nguoi choi vao arena
            bool transitionDone = false;
            if (_uiManager != null)
            {
                _uiManager.PlayTransition(
                    onCovered: () =>
                    {
                        TeleportPlayersToArena();
                        _playerManager?.ChargeRoundPlayersSkills();
                        _playerManager?.SetRoundPlayersSkillLock(true);
                    },
                    onComplete: () =>
                    {
                        transitionDone = true;
                    }
                );

                while (!transitionDone)
                {
                    yield return null;
                }
            }
            else
            {
                TeleportPlayersToArena();
                _playerManager?.ChargeRoundPlayersSkills();
                _playerManager?.SetRoundPlayersSkillLock(true);
            }

            // Nhạc nền của sảnh chờ đã được dừng bên trong TeleportPlayersToArena().
            // Sẽ có một khoảng lặng cho đến khi round đấu chính thức bắt đầu.
            // Yêu cầu 1: Chờ 3 giây sau khi teleport trước khi hiển thị thanh độ khó.
            yield return _waitForSeconds3;

            // 3. Gán độ khó cho round hiện tại và hiển thị ngay lập tức.
            CurrentIntensity = Mathf.Clamp(_nextRoundIntensity, _minIntensity, _maxIntensity);
            Debug.Log($"[GameloopManager] Round starting. Current intensity locked at: {CurrentIntensity}");
            if (_uiManager != null)
            {
                yield return StartCoroutine(_uiManager.AnimateIntensityBar(CurrentIntensity, _minIntensity, _maxIntensity));
                _uiManager.ShowCurrentIntensity(CurrentIntensity);
            }

            // 4. Ghi nhận snapshot số lượng người chơi sống ban đầu của round này
            _initialPlayerCountForRound = _playerManager?.GetAlivePlayerCount() ?? 0;
            _playersWhoDiedInRound = 0;
            Debug.Log($"[GameloopManager] Round {CurrentState}: {_initialPlayerCountForRound} active players. Intensity tracking initialized.");

            yield return new WaitForSeconds(_postTeleportWaitDuration);

            // --- Giai đoạn 4: Đếm ngược & Bắt đầu ---
            Debug.Log("[GameloopManager] Giai đoạn 4: Đếm ngược và Bắt đầu");
            if (_uiManager != null) yield return StartCoroutine(_uiManager.ShowCountdown());

            // Mo khoa su dung skill cho tat ca nguoi choi sau khi hoan tat dem nguoc 3 2 1 GO
            _playerManager?.SetRoundPlayersSkillLock(false);

            // Bắt đầu phát nhạc gameplay sau khi đếm ngược kết thúc
            BGMController.Instance?.PlayGameplayMusic(CurrentIntensity);
        }

        /// <summary>
        /// Giai đoạn 5: Round đấu chính. Kích hoạt việc sinh bom, chạy timer và kiểm tra điều kiện kết thúc round.
        /// </summary>
        /// <returns>IEnumerator để chạy như một coroutine.</returns>
        private IEnumerator RoundActiveStage()
        {
            // --- Giai đoạn 5: Round đang diễn ra ---
            CurrentState = GameState.RoundActive;
            _bombSpawnerManager?.StartSpawning();
            _uiManager?.ShowPersistentNotification("Survive the bombs!");
            Debug.Log("[GameloopManager] Giai đoạn 5: Round đang diễn ra");

            _roundTimeRemaining = _roundDuration;
            int lastDisplayedSecond = Mathf.CeilToInt(_roundTimeRemaining);

            _uiManager?.SetTimerDangerState(false);

            int endConditionPlayerCount = 0;
            bool last30sMusicTriggered = false;

            while (_roundTimeRemaining > 0f && (_playerManager?.GetAlivePlayerCount() > endConditionPlayerCount))
            {
                _roundTimeRemaining -= Time.deltaTime;
                int currentSecond = Mathf.CeilToInt(_roundTimeRemaining);

                if (currentSecond < lastDisplayedSecond)
                {
                    lastDisplayedSecond = currentSecond;
                    _uiManager?.UpdateTimer(lastDisplayedSecond);
                }

                if (!last30sMusicTriggered && _roundTimeRemaining <= 30f)
                {
                    BGMController.Instance?.PlayLast30sMusic(CurrentIntensity);
                    last30sMusicTriggered = true;
                    _uiManager?.SetTimerDangerState(true);
                    Debug.Log("[GameloopManager] 30 seconds left. Playing final music.");
                }
                yield return null;
            }
            Debug.Log("[GameloopManager] Round kết thúc.");
        }

        /// <summary>
        /// Giai đoạn 6: Kết thúc và dọn dẹp round đấu. Hiển thị kết quả, dừng các hệ thống game và đưa người chơi về sảnh chờ.
        /// </summary>
        /// <returns>IEnumerator để chạy như một coroutine.</returns>
        private IEnumerator PostRoundStage()
        {
            // --- Giai đoạn 6: Dọn dẹp ---
            CurrentState = GameState.PostRound;
            Debug.Log("[GameloopManager] Giai đoạn 6: Dọn dẹp");
            _bombSpawnerManager?.StopSpawning(); // Dừng sinh bom trước
            _uiManager?.HideTimer();
            _uiManager?.HideCurrentIntensity(); // Ẩn panel text độ khó của round vừa kết thúc.

            // Bat trang thai bat tu ngay lap tuc cho tat ca nguoi choi con song de khong nhan bat ky sat thuong nao trong thoi gian chuyen giao
            _playerManager?.SetRoundSurvivorsInvincible(true);

            // Phat SFX tieng coi + tieng chuong khi round ket thuc
            _uiManager?.PlayRoundEndSfx();

            var survivors = _playerManager?.GetSurvivors();

            // 1. Tinh toan diem va hien thi Score Card dong thoi voi Transition Screen
            if (survivors != null && survivors.Count > 0)
            {
                ProcessSurvivorScoresAndShowScoreCard(survivors);
            }

            // 2. Chay Transition Screen va Teleport nguoi choi thang/song sot ve lobby khi man hinh che kin
            if (survivors != null && survivors.Count > 0)
            {
                bool transitionDone = false;
                if (_uiManager != null)
                {
                    _uiManager.PlayTransition(
                        onCovered: () =>
                        {
                            _playerManager?.ReturnRoundSurvivorsToLobby();
                        },
                        onComplete: () =>
                        {
                            transitionDone = true;
                        }
                    );

                    while (!transitionDone)
                    {
                        yield return null;
                    }
                }
                else
                {
                    _playerManager?.ReturnRoundSurvivorsToLobby();
                }
            }
            else
            {
                _playerManager?.ReturnRoundSurvivorsToLobby();
            }

            // Dua nguoi choi ve lobby va reset round trong PlayerManager
            _playerManager?.EndRound();

            // --- Intensity Adjustment Logic ---
            // Công thức: NextIntensity = CurrentIntensity + (có người sống sót ? +0.25 : +0) - (0.3 * số người chết / tổng số người tham gia round ban đầu)
            // Quy tắc:
            // 1. Chỉ cộng +0.25 khi round có ÍT NHẤT MỘT người sống sót (thắng hoặc hòa do hết giờ).
            // 2. Khi toàn bộ người tham gia round đều chết (thất bại toàn bộ): KHÔNG cộng +0.25, chỉ trừ penalty
            //    theo tỷ lệ người chết (all-dead -> trừ đủ -0.3).
            // 3. Player reset khi KHÔNG trong round (lobby/intermission/building) không ảnh hưởng intensity vì
            //    HandlePlayerDeath chỉ đếm khi CurrentState == RoundActive.
            // 4. Nếu round bị hủy (không có ai tham gia ban đầu): giữ nguyên intensity, không tăng/giảm.
            // 5. Kết quả luôn được kẹp trong khoảng [minIntensity, maxIntensity].
            if (_initialPlayerCountForRound <= 0)
            {
                // Round không có ai tham gia: giữ nguyên intensity.
                Debug.Log($"[GameloopManager] Round had no active participants. Intensity unchanged at: {CurrentIntensity}");
            }
            else
            {
                // Quy tắc: chỉ cộng +0.25 khi round có NGƯỜI SỐNG SÓT (thắng hoặc hòa do hết giờ).
                // Thất bại toàn bộ (mọi người tham gia round đều chết) -> KHÔNG cộng, chỉ trừ penalty.
                bool hasSurvivors = _playersWhoDiedInRound < _initialPlayerCountForRound;
                float nextIntensity = CurrentIntensity + (hasSurvivors ? _intensityIncrementPerRound : 0f);

                // Giảm theo tỷ lệ người chết: 0.3 * (deadCount / initialPlayerCount).
                float deathRatio = (float)_playersWhoDiedInRound / _initialPlayerCountForRound;
                nextIntensity -= _intensityMaxDeathPenalty * deathRatio;

                SetNextRoundIntensity(nextIntensity);
                Debug.Log($"[GameloopManager] Round ended. Deaths {_playersWhoDiedInRound}/{_initialPlayerCountForRound}. Next round intensity calculated: {_nextRoundIntensity}");
            }

            // Bật lại nhạc sảnh chờ cho tất cả người chơi.
            // Phát lại nhạc sảnh chờ khi round kết thúc (qua BGMController toàn cục).
            BGMController.Instance?.PlayLobbyMusic();

            // Hiển thị thông báo kết thúc round
            _uiManager?.ShowPersistentNotification("Round Over!");

            yield return new WaitForSeconds(_postRoundWaitDuration);

            // DỌN DẸP HIỆU ỨNG: Kích hoạt sự kiện để các hiệu ứng còn sót lại (khí độc, điện...) tự hủy.
            GameEvents.TriggerRoundEndCleanup();
            Debug.Log("[GameloopManager] Triggered Round End Cleanup event for stray effects.");

            // Hiển thị thông báo đang dọn dẹp
            _uiManager?.ShowPersistentNotification("Cleaning up the play area...");
            Debug.Log("[GameloopManager] Bắt đầu dọn dẹp map.");

            yield return StartCoroutine(CleanupRoundAsync());
            Debug.Log("[GameloopManager] Dọn dẹp map hoàn tất.");

            // Chờ ổn định FPS sau khi dọn dẹp. Thông báo vẫn hiển thị trong lúc này.
            Debug.Log($"[GameloopManager] Chờ ổn định FPS trong {_postCleanupStabilizationDuration} giây.");
            yield return new WaitForSeconds(_postCleanupStabilizationDuration);

            // Yeu cau: giu nguyen notification panel hien thi xuyen suot vong lap game,
            // khong an o day; vong lap moi se tu cap nhat noi dung moi o IntermissionStage.
        }

        #endregion

        /// <summary>
        /// Bắt đầu coroutine để tải map và thế giới ngầm dựa trên các chỉ số đã chọn.
        /// </summary>
        /// <param name="mapIndex">Chỉ số của map để tải.</param>
        /// <param name="undergroundIndex">Chỉ số của cấu hình thế giới ngầm để tải.</param>
        private IEnumerator BuildSelectedMap(int mapIndex, int undergroundIndex)
        {
            if (_mapManager == null)
            {
                Debug.LogError("[GameloopManager] MapManager không tồn tại.", this);
                yield break;
            }
            
            Debug.Log($"[GameloopManager] Đang tải map. MapIndex: {mapIndex}, UndergroundIndex: {undergroundIndex}");
            // Đảm bảo MapManager đã được khởi tạo và có thể truy cập
            if (_mapManager == null) {
                _mapManager = MapManager.Instance; // Lấy instance nếu chưa có
            }
            yield return StartCoroutine(_mapManager.LoadMapByIndexAsync(mapIndex, undergroundIndex));
        }

        /// <summary>
        /// Dịch chuyển một người chơi cụ thể đến một vị trí ngẫu nhiên trong khu vực arena.
        /// </summary>
        /// <param name="player">Người chơi cần dịch chuyển.</param>
        private void TeleportPlayerToArena(IPlayer player)
        {
            if (_arenaSpawnArea == null)
            {
                Debug.LogError("[GameloopManager] Arena Spawn Area is not assigned!", this);
                return;
            }
            if (_mapManager == null) {
                Debug.LogError("[GameloopManager] MapManager is null, cannot get MapTopY for arena spawn.", this);
                return;
            }
            if (player == null || player.GameObject == null) return;

            Bounds bounds = _arenaSpawnArea.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            // Dịch chuyển player đến vị trí ngẫu nhiên trên mặt phẳng của arena
            // Sử dụng đỉnh của arenaSpawnArea làm điểm tham chiếu cho mặt đất của arena.
            player.Teleport(new Vector3(randomX, bounds.center.y + bounds.extents.y, randomZ));
            Debug.Log($"[GameloopManager] Teleported {player.GameObject.name} to arena.");
        }

        /// <summary>
        /// Định vị lại vị trí Y của các đối tượng môi trường (ArenaSpawnArea, BombSpawner, TopBorder)
        /// theo MapTopY của map vừa build xong.
        /// PHẢI được gọi SAU KHI build xong map và TRƯỚC KHI teleport player (xem BuildingStage), để physics
        /// kịp đồng bộ Collider.bounds của Arena, tránh đọc bounds cũ ngay trong cùng frame gây spawn sai chỗ
        /// (ví dụ player bị sinh nằm trên TopBorder).
        /// </summary>
        private void UpdateArenaEnvironmentPosition()
        {
            if (_mapManager == null)
            {
                Debug.LogError("[GameloopManager] MapManager is null, cannot get MapTopY for arena environment.", this);
                return;
            }

            // Lấy chiều cao mặt đất của map làm tham chiếu
            float mapTopY = _mapManager.MapTopY;

            // Cập nhật vị trí của BombSpawner, Arena và TopBorder với các offset tùy chỉnh
            float targetBombTopY = mapTopY + _bombSpawnerYOffset;
            _bombSpawnerManager?.SetSpawnAreaTopY(targetBombTopY);

            // Điều chỉnh vị trí của _arenaSpawnArea để mặt trên của nó nằm ở vị trí mong muốn
            if (_arenaSpawnArea != null)
            {
                float targetArenaTopY = mapTopY + _arenaSpawnAreaYOffset;
                // _arenaSpawnArea.bounds.center.y + _arenaSpawnArea.bounds.extents.y là mặt trên hiện tại của collider.
                float currentTopY = _arenaSpawnArea.bounds.center.y + _arenaSpawnArea.bounds.extents.y;
                float yDifference = targetArenaTopY - currentTopY;
                _arenaSpawnArea.transform.position = new Vector3(_arenaSpawnArea.transform.position.x, _arenaSpawnArea.transform.position.y + yDifference, _arenaSpawnArea.transform.position.z);
            }

            if (_topBorder != null) _topBorder.position = new Vector3(_topBorder.position.x, mapTopY + _topBorderYOffset, _topBorder.position.z);
        }

        /// <summary>
        /// Dịch chuyển tất cả người chơi tham gia round (khong bao gom AFK) đến một vị trí ngẫu nhiên trong khu vực arena.
        /// Lưu ý: vị trí của Arena/BombSpawner/TopBorder đã được định vị ở <see cref="UpdateArenaEnvironmentPosition"/>
        /// (gọi trong BuildingStage ngay sau khi build xong map, trước bước teleport này).
        /// CHI teleport nhung nguoi choi trong _playersInRound (nhuoc gap StartRound da loc AFK),
        /// de player AFK (khong tham gia round) van o lobby va khong bi dua vao arena.
        /// </summary>
        private void TeleportPlayersToArena()
        {
            if (_arenaSpawnArea == null)
            {
                Debug.LogError("[GameloopManager] Arena Spawn Area is not assigned!", this);
                return;
            }
            var players = _playerManager?.GetPlayersInRound();
            if (players == null) return;

            // Fade out nhac nen sanh cho (Lobby BGM) khi teleport vao arena.
            if (BGMController.Instance != null)
                BGMController.Instance.FadeOutMusic(1.0f);

            foreach (var player in players)
            {
                TeleportPlayerToArena(player);
            }
        }

        private IEnumerator CleanupRoundAsync()
        {
            yield return StartCoroutine(_mapManager.ClearCurrentMapAsync());
            _bombSpawnerManager?.ClearAllBombs();
            _collectiblePoolManager?.ClearAllCollectibles();
        }

        private void HandlePlayerDeath(IPlayer player)
        {
            if (CurrentState != GameState.RoundActive) return;

            // Chỉ đếm số người chết trong round hiện tại.
            // Penalty intensity sẽ được tính trong PostRoundStage() dựa trên bộ đếm này.
            _playersWhoDiedInRound++;
            Debug.Log($"[GameloopManager] Player died. Deaths this round: {_playersWhoDiedInRound}");
        }

        /// <summary>
        /// Tinh toan, tang credits va hien thi Score Card doc lap cho tat ca nhung nguoi choi song sot khi round ket thuc.
        /// - Neu chi co 1 nguoi song sot: duoc tinh la nguoi thang, win streak tang 1, ap dung Win Multiplier va IsWinner = true.
        /// - Neu nhieu nguoi song sot (round ket thuc do het gio): khong ai duoc tinh la thang, win streak reset ve 0 va IsWinner = false.
        /// Score Card duoc hien thi truc tiep tren client cua player ma khong phai cho doi tuan tu giua cac player.
        /// </summary>
        /// <param name="survivors">Danh sach nguoi choi con song khi round ket thuc (da lay truoc EndRound).</param>
        private void ProcessSurvivorScoresAndShowScoreCard(List<IPlayer> survivors)
        {
            if (survivors == null || survivors.Count == 0)
            {
                Debug.Log("[GameloopManager] Khong co nguoi choi nao song sot trong round. Khong hien thi Score Card dang thang.");
                return;
            }

            bool isVictory = survivors.Count == 1;
            IPlayer currentPlayer = _playerManager?.GetCurrentPlayer();

            for (int i = 0; i < survivors.Count; i++)
            {
                IPlayer survivor = survivors[i];
                if (survivor == null || survivor.GameObject == null) continue;

                PlayerRoundData roundData = _playerManager?.GetPlayerRoundData(survivor);
                if (roundData == null) continue;

                // a. Cap nhat win streak: tang 1 neu la nguoi thang duy nhat, nguoc lai reset ve 0.
                roundData.WinStreak = isVictory ? roundData.WinStreak + 1 : 0;

                // b. Survival Score: song sot ca round -> diem toi da theo phan khuc intensity.
                int survivalScore = ScoreCalculator.GetMaxSurvivalScore(CurrentIntensity);

                // c. Multiplier: x1.0 mac dinh, x1.25 neu bat Extreme Mode.
                float baseMultiplier = ScoreCalculator.GetMultiplier(roundData.IsExtremeModeEnabled);

                // d. Win Multiplier dua tren win streak (chi ap dung khi co nguoi thang duy nhat).
                float winMultiplier = isVictory ? ScoreCalculator.GetWinMultiplier(roundData.WinStreak) : ScoreCalculator.DefaultMultiplier;

                // e. Tong credits = Survival Score x Multiplier x Win Multiplier.
                int totalCredits = ScoreCalculator.GetTotalCredits(survivalScore, baseMultiplier, winMultiplier);

                // f. Tang credits cho nguoi choi (PlayerDataManager lang nghe va tu luu).
                GameEvents.TriggerAddCreditsRequest(totalCredits);
                Debug.Log($"[GameloopManager] Trao {totalCredits} credits cho {survivor.GameObject.name} (Survival: {survivalScore}, Base: x{baseMultiplier:0.##}, Win: x{winMultiplier:0.##}, Streak: {roundData.WinStreak}).", survivor.GameObject);

                // g. Tao du lieu Score Card cho nguoi choi
                ScoreCardData scoreCard = new()
                {
                    SurvivalScore = survivalScore,
                    BaseMultiplier = baseMultiplier,
                    WinMultiplier = winMultiplier,
                    TotalCredits = totalCredits,
                    IsWinner = isVictory,
                    WinStreak = isVictory ? roundData.WinStreak : 0,
                    IsExtremeMode = roundData.IsExtremeModeEnabled
                };

                // Neu co local/current player, hien thi Score Card cho current player
                if (currentPlayer == null || survivor == currentPlayer)
                {
                    _uiManager?.ShowScoreCard(scoreCard);
                }
            }
        }
        #endregion
    }
}
