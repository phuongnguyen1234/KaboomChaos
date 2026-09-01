using UnityEngine;
using Core;
using Core.Enums;
using Core.Interfaces;
using Core.Utilities;
using System.Collections;
using System.Collections.Generic;
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
        // Khoảng thời gian chờ giữa các Score Card cá nhân liên tiếp (thời gian hiển thị 5s + thời gian trượt xuống 0.4s).
        private static readonly WaitForSeconds _scoreCardDisplayInterval = new(5.5f);
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
        // coroutines (e.g. two voting timers) after returning Home and pressing Play again.
        private Coroutine _activeStageCoroutine;

        [Header("Game Loop Settings")]
        [Tooltip("Thời gian (giây) cho giai đoạn chào mừng 'Welcome to Kaboom Chaos!' khi bấm Play. Player đã được spawn, đồng hồ hiển thị 0:00 trong giai đoạn này.")]
        [SerializeField] private float _welcomeDuration = 5f;
        [Tooltip("Thời gian (giây) cho giai đoạn bỏ phiếu map.")]
        [SerializeField] private int _votingDuration = 15;
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

        [Header("Difficulty Settings")]
        [Tooltip("Độ khó tối thiểu.")]
        [SerializeField] private float _minIntensity = 1f;
        [Tooltip("Độ khó tối đa.")]
        [SerializeField] private float _maxIntensity = 6f;
        [Tooltip("Độ khó khởi tạo cho round đấu đầu tiên của một phiên chơi mới (thay vì luôn bắt đầu từ minIntensity).")]
        [SerializeField] private float _initialIntensity = 1f;
        [Tooltip("Mức tăng độ khó cố định cho round kế tiếp sau một round đã diễn ra (không phụ thuộc số player).")]
        [SerializeField] private float _intensityIncrementPerRound = 0.25f;
        [Tooltip("Mức giảm độ khó tối đa khi toàn bộ player thất bại trong round. Giảm thực tế = giá trị này * (số người chết / tổng số người tham gia round).")]
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
                // Gan luon qua interface de cac assembly khac (UI) co the truy cap qua IGameStateProvider.Instance.
                IGameStateProvider.Instance = this;
                DontDestroyOnLoad(gameObject); // Giữ Manager tồn tại khi chuyển đổi giữa các scene.
            }
        }

                private void OnEnable()
        {
            // Đăng ký lắng nghe sự kiện người chơi chết để điều chỉnh độ khó
            GameEvents.OnPlayerDied += HandlePlayerDeath;
            GameEvents.OnStartGameRequest += StartGame;
            // Stop the running game loop when the player returns to the Home screen.
            GameEvents.OnReturnToHomeRequest += HandleReturnToHome;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDeath;
            GameEvents.OnStartGameRequest -= StartGame;
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
        }

        public void StartGame()
        {
            // If a previous game loop is still running (e.g. the player returned to Home
            // without it being stopped), stop it before starting a fresh session so that
            // pressing Play again begins a completely new loop.
            StopGameLoop();

            // Đặt lại hạt giống độ khó về giá trị khởi tạo đã cấu hình trong Inspector,
            // để round đầu tiên của phiên mới không bị ép về luôn _minIntensity mặc định.
            _nextRoundIntensity = Mathf.Clamp(_initialIntensity, _minIntensity, _maxIntensity);

            _gameLoopActive = true;

            // Spawn người chơi lần đầu tiên khi game bắt đầu.
            _playerManager?.SpawnInitialPlayer();

            // Bắt đầu vòng lặp game chính
            _gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
        }

        /// <summary>
        /// Stops the currently running game loop, if any.
        /// </summary>
        private void StopGameLoop()
        {
            _gameLoopActive = false;
            // Stop the active stage coroutine first (it may run independently of the main loop).
            if (_activeStageCoroutine != null)
            {
                StopCoroutine(_activeStageCoroutine);
                _activeStageCoroutine = null;
            }
            if (_gameLoopCoroutine != null)
            {
                StopCoroutine(_gameLoopCoroutine);
                _gameLoopCoroutine = null;
            }
        }

        /// <summary>
        /// Called when the player returns to the Home screen. Stops the game loop
        /// so it does not keep running in the background.
        /// </summary>
        private void HandleReturnToHome()
        {
            Debug.Log("[GameloopManager] Return to Home requested. Stopping the game loop.");
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
                _activeStageCoroutine = StartCoroutine(MapVotingStage());
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
        /// sau đó mới bước vào vòng lặp map (voting/build/round) thực sự.
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
        /// Giai đoạn 1: Xử lý việc bỏ phiếu cho map. Hiển thị timer, chọn map ngẫu nhiên và thông báo kết quả.
        /// </summary>
        /// <returns>IEnumerator để chạy như một coroutine.</returns>
        private IEnumerator MapVotingStage()
        {
            // --- Giai đoạn 1: Bỏ phiếu Map ---
            CurrentState = GameState.MapVoting;
            Debug.Log("[GameloopManager] Giai đoạn 1: Bỏ phiếu Map");
            _uiManager?.ShowPersistentNotification("Voting for the next map...");

            // Bắt đầu vòng lặp đếm ngược cho giai đoạn voting, đồng thời hiển thị timer.
            float votingTimer = _votingDuration;
            int lastDisplayedSecondForVote = Mathf.CeilToInt(votingTimer);
            _uiManager?.UpdateTimer(lastDisplayedSecondForVote); // Hiển thị timer ngay lập tức

            while (votingTimer > 0f)
            {
                votingTimer -= Time.deltaTime;
                int currentSecond = Mathf.CeilToInt(votingTimer);

                if (currentSecond < lastDisplayedSecondForVote)
                {
                    lastDisplayedSecondForVote = currentSecond;
                    _uiManager?.UpdateTimer(lastDisplayedSecondForVote);
                }
                yield return null;
            }
            // Chọn map ngẫu nhiên (logic voting thực tế sẽ được thêm vào sau)
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
                    // Lấy tên map được chọn để hiển thị thông báo.
                    // Cần ép kiểu vì IMapManager có thể chưa được cập nhật.
                    selectedMapName = (_mapManager as MapManager)?.GetMapNameByIndex(_selectedMapIndex) ?? "Invalid Map";
                }
            }

            // Thông báo map đã được chọn
            _uiManager?.HideTimer(); // Ẩn timer của voting trước khi hiện thông báo map
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
            TeleportPlayersToArena();

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

            // 4. Ghi nhận snapshot số lượng người chơi sống ban đầu của round này,
            // để dùng cho công thức giảm intensity dựa trên tỉ lệ số người chết.
            // Ngụ ý: "danh sách tham gia round" = những người chơi KHÔNG bị AFK ngay tại thời điểm round bắt đầu.
            // TODO(AFK): Khi triển khai hệ thống AFK, hãy LỌC người chơi AFK khỏi danh sách tham gia
            // trước khi snapshot, để độ khó chỉ được tính dựa trên danh sách non-AFK ban đầu này.
            _initialPlayerCountForRound = _playerManager?.GetAlivePlayerCount() ?? 0;
            _playersWhoDiedInRound = 0;
            Debug.Log($"[GameloopManager] Round {CurrentState}: {_initialPlayerCountForRound} active players. Intensity tracking initialized.");

            yield return new WaitForSeconds(_postTeleportWaitDuration);

            // --- Giai đoạn 4: Đếm ngược & Bắt đầu ---
            Debug.Log("[GameloopManager] Giai đoạn 4: Đếm ngược và Bắt đầu");
            // Sử dụng coroutine đếm ngược mới từ UIManager
            if (_uiManager != null) yield return StartCoroutine(_uiManager.ShowCountdown());

            // Bắt đầu phát nhạc gameplay sau đây đếm ngược kết thúc (qua BGMController toàn cục).
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
            // Yeu cau: sau khi dem nguoc ket thuc va round bat dau, doi noi dung notification thanh Survive the bombs.
            _uiManager?.ShowPersistentNotification("Survive the bombs!");
            Debug.Log("[GameloopManager] Giai đoạn 5: Round đang diễn ra");

            // Khởi tạo đồng hồ round (giây còn lại). Đồng hồ này là nguồn tính Survival Score theo thời gian của round.
            _roundTimeRemaining = _roundDuration;
            int lastDisplayedSecond = Mathf.CeilToInt(_roundTimeRemaining);

            // Reset trang thai cam bao cua timer ve mau binh thuong truoc khi chay round moi.
            _uiManager?.SetTimerDangerState(false);

            // Lấy số người chơi lúc bắt đầu round để xác định điều kiện thắng.
            // Điều kiện kết thúc round: khi không còn người chơi nào sống sót (số người chơi còn lại là 0).
            int endConditionPlayerCount = 0;
            bool last30sMusicTriggered = false;

            // Vòng lặp chính của round đấu.
            // Điều kiện kết thúc: hết giờ, hoặc số người chơi còn lại đạt ngưỡng kết thúc.
            while (_roundTimeRemaining > 0f && (_playerManager?.GetAlivePlayerCount() > endConditionPlayerCount))
            {
                _roundTimeRemaining -= Time.deltaTime;
                int currentSecond = Mathf.CeilToInt(_roundTimeRemaining);

                if (currentSecond < lastDisplayedSecond)
                {
                    lastDisplayedSecond = currentSecond;
                    _uiManager?.UpdateTimer(lastDisplayedSecond);
                }

                // Kích hoạt nhạc 30 giây cuối
                if (!last30sMusicTriggered && _roundTimeRemaining <= 30f)
                {
                                        // Kích hoạt nhạc 30 giây cuối (qua BGMController toàn cục)
                    BGMController.Instance?.PlayLast30sMusic(CurrentIntensity);
                    last30sMusicTriggered = true;

                    // Doi mau text + icon TimerPanel thanh do va phat SFX canh bao song song.
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

            // ===== SCORE CARD CHO NGƯỜI SỐNG SÓT KẾT THÚC ROUND =====
            // Lấy danh sách người chơi còn sống sót TRƯỚC khi EndRound() xóa danh sách người chơi trong round.
            var survivors = _playerManager?.GetSurvivors();

            // B1. Đưa (những) người chơi còn sống sót trong round về sảnh chờ TRƯỚC.
            // Phương thức này phải được gọi TRƯỚC EndRound(), vì EndRound() sẽ xóa danh sách người chơi trong round.
            _playerManager?.ReturnRoundSurvivorsToLobby();

            // SỬA LỖI: Reset trạng thái người chơi (máu, khiên, buff) ngay khi round kết thúc, không chờ đợi.
            _playerManager?.EndRound();

            // B2. Tính điểm, tặng credits và hiển thị Score Card cá nhân cho TỪNG người chơi sống sót.
            // KHÔNG chặn game loop: dùng StartCoroutine (không yield) để cleanup + vòng lặp mới
            // diễn ra bình thường trong lúc các Score Card lần lượt xuất hiện và tự ẩn (mỗi card 5 giây).
            StartCoroutine(ShowScoreCardForSurvivors(survivors));

            // --- Intensity Adjustment Logic ---
            // Công thức: NextIntensity = CurrentIntensity + (có người sống sót ? +0.25 : +0) - (0.3 * số người chết / tổng số người tham gia round ban đầu)
            // Quy tắc:
            // 1. Chỉ cộng +0.25 khi round có ÍT NHẤT MỘT người sống sót (thắng hoặc hòa do hết giờ).
            // 2. Khi toàn bộ người tham gia round đều chết (thất bại toàn bộ): KHÔNG cộng +0.25, chỉ trừ penalty
            //    theo tỷ lệ người chết (all-dead -> trừ đủ -0.3).
            // 3. Player reset khi KHÔNG trong round (lobby/voting/building) không ảnh hưởng intensity vì
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

                _nextRoundIntensity = Mathf.Clamp(nextIntensity, _minIntensity, _maxIntensity);
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
            // khong an o day; vong lap moi se tu cap nhat noi dung moi o MapVotingStage.
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
        /// Dịch chuyển tất cả người chơi hiện đang hoạt động đến một vị trí ngẫu nhiên trong khu vực arena.
        /// Lưu ý: vị trí của Arena/BombSpawner/TopBorder đã được định vị ở <see cref="UpdateArenaEnvironmentPosition"/>
        /// (gọi trong BuildingStage ngay sau khi build xong map, trước bước teleport này).
        /// </summary>
        private void TeleportPlayersToArena()
        {
            if (_arenaSpawnArea == null)
            {
                Debug.LogError("[GameloopManager] Arena Spawn Area is not assigned!", this);
                return;
            }
            var players = _playerManager?.GetAllPlayers();
            if (players == null) return;

            // Sau khi môi trường đã được định vị ở bước riêng (sau khi build map), tiến hành dịch chuyển người chơi.
            foreach (var player in players)
            {
                // Dừng nhạc nền toàn cục (nhạc sảnh chờ) trước khi dịch chuyển vào arena.
                BGMController.Instance?.StopMusic();
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
        /// Tính điểm, tặng credits và hiển thị Score Card CÁ NHÂN cho TỪNG người chơi sống sót khi round kết thúc.
        /// Áp dụng đúng quy tắc ScoreRules.md:
        /// - Nếu chỉ có 1 người sống sót (round thắng): tăng win streak, áp dụng Win Multiplier, IsWinner = true.
        /// - Nếu nhiều người sống sót (round kết thúc do hết giờ): không ai được tính là người thắng,
        ///   win streak được reset thành 0 và IsWinner = false.
        /// Mỗi người chơi có một Score Card RIÊNG và được hiển thị tuần tự để không chồng lấn UI.
        /// </summary>
        /// <param name="survivors">Danh sách người chơi còn sống khi round kết thúc (đã lấy trước EndRound).</param>
        private IEnumerator ShowScoreCardForSurvivors(List<IPlayer> survivors)
        {
            if (survivors == null || survivors.Count == 0)
            {
                Debug.Log("[GameloopManager] Không có người chơi nào sống sót trong round. Không hiển thị Score Card dạng thắng.");
                yield break;
            }

            bool isVictory = survivors.Count == 1;

            for (int i = 0; i < survivors.Count; i++)
            {
                IPlayer survivor = survivors[i];
                if (survivor == null || survivor.GameObject == null) continue;

                PlayerRoundData roundData = _playerManager?.GetPlayerRoundData(survivor);
                if (roundData == null) continue;

                // a. Cập nhật win streak: tăng 1 nếu là người thắng duy nhất, ngược lại reset về 0.
                roundData.WinStreak = isVictory ? roundData.WinStreak + 1 : 0;

                // b. Survival Score: sống sót cả round → điểm tối đa theo phân khúc intensity.
                int survivalScore = ScoreCalculator.GetMaxSurvivalScore(CurrentIntensity);

                // c. Multiplier: x1.0 mặc định, x1.25 nếu bật Extreme Mode.
                float baseMultiplier = ScoreCalculator.GetMultiplier(roundData.IsExtremeModeEnabled);

                // d. Win Multiplier dựa trên win streak (chỉ áp dụng khi có người thắng duy nhất).
                float winMultiplier = isVictory ? ScoreCalculator.GetWinMultiplier(roundData.WinStreak) : ScoreCalculator.DefaultMultiplier;

                // e. Tổng credits = Survival Score x Multiplier x Win Multiplier.
                int totalCredits = ScoreCalculator.GetTotalCredits(survivalScore, baseMultiplier, winMultiplier);

                // f. Tặng credits cho người chơi (PlayerDataManager lắng nghe và tự lưu).
                GameEvents.TriggerAddCreditsRequest(totalCredits);
                Debug.Log($"[GameloopManager] Trao {totalCredits} credits cho {survivor.GameObject.name} (Survival: {survivalScore}, Base: x{baseMultiplier:0.##}, Win: x{winMultiplier:0.##}).", survivor.GameObject);

                // g. Hiển thị Score Card CÁ NHÂN của từng người chơi, lần lượt (mỗi card tự ẩn trong 5 giây).
                ScoreCardData scoreCard = new()
                {
                    SurvivalScore = survivalScore,
                    BaseMultiplier = baseMultiplier,
                    WinMultiplier = winMultiplier,
                    TotalCredits = totalCredits,
                    IsWinner = isVictory,
                };
                _uiManager?.ShowScoreCard(scoreCard);

                // Chờ Score Card hiện tại hiển thị đủ rồi mới chuyển sang Score Card của người tiếp theo.
                yield return _scoreCardDisplayInterval;
            }
        }
        #endregion
    }
}
