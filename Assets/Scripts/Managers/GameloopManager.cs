using UnityEngine;
using Core;
using Core.Interfaces;
using System.Collections;

namespace Managers
{
    /// <summary>
    /// Quản lý luồng chơi chính (Game Loop), bao gồm việc khởi tạo, bắt đầu và kết thúc game.
    /// Lớp này đóng vai trò điều phối các Manager khác (như PlayerManager, SpawnManager, v.v.).
    /// </summary>
    
    /// <summary>
    /// Các trạng thái của vòng lặp game.
    /// </summary>
    public enum GameState
    {
        /// <summary>Trạng thái không xác định hoặc khởi tạo.</summary>
        None,
        /// <summary>Giai đoạn người chơi bỏ phiếu cho map tiếp theo.</summary>
        MapVoting,
        /// <summary>Giai đoạn map đang được xây dựng.</summary>
        Building,
        /// <summary>Giai đoạn chuẩn bị trước round đấu (dịch chuyển, đếm ngược).</summary>
        PreRound,
        /// <summary>Giai đoạn round đấu đang diễn ra.</summary>
        RoundActive,
        /// <summary>Giai đoạn kết thúc round đấu (hiển thị kết quả, dọn dẹp).</summary>
        PostRound
    }

    public class GameloopManager : MonoBehaviour, IGameloopManager
    {
        private static WaitForSeconds _waitForSeconds3 = new WaitForSeconds(3f);
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

        #endregion

        #region Fields

        // Tham chiếu đến các manager khác sẽ được lấy thông qua Singleton hoặc Service Locator.
        private IPlayerManager _playerManager;
        private IMapManager _mapManager;
        private IBombSpawnerManager _bombSpawnerManager;
        private IDestructionManager _destructionManager;
        private IUIManager _uiManager;

        [Header("Game Loop Settings")]
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

        [Tooltip("Thời gian (giây) cho phép người chơi tham gia muộn vào round.")]
        [SerializeField] private float _lateJoinDuration = 30f;
        [Header("Difficulty Settings")]
        [Tooltip("Độ khó tăng thêm cho mỗi người chơi tham gia round.")]
        [SerializeField] private float _intensityPerPlayer = 0.3f;
        [Tooltip("Độ khó giảm đi cho mỗi người chơi bị loại.")]
        [SerializeField] private float _intensityReductionOnDeath = 0.1f;
        [Tooltip("Độ khó tối thiểu.")]
        [SerializeField] private float _minIntensity = 1f;
        [Tooltip("Độ khó tối đa.")]
        [SerializeField] private float _maxIntensity = 5f;

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
        private float _nextRoundIntensity = 1f;
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
                DontDestroyOnLoad(gameObject); // Giữ Manager tồn tại khi chuyển đổi giữa các scene.
            }
        }

        private void OnEnable()
        {
            // Đăng ký lắng nghe sự kiện người chơi chết để điều chỉnh độ khó
            GameEvents.OnPlayerDied += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDeath;
        }

        private void Start()
        {
            // Lấy tham chiếu đến các manager khác trong Start() để đảm bảo các Singleton của chúng đã được khởi tạo trong Awake().
            // Việc này được đảm bảo bởi Bootstrapper.
            _playerManager = PlayerManager.Instance;
            _mapManager = MapManager.Instance;
            _bombSpawnerManager = BombSpawnerManager.Instance;
            _destructionManager = DestructionManager.Instance;
            _uiManager = IUIManager.Instance;

            // Lấy tham chiếu đến các đối tượng trong scene từ Registry
            var registry = SceneObjectRegistry.Instance;
            if (registry != null)
            {
                _arenaSpawnArea = registry.ArenaSpawnArea;
                _topBorder = registry.TopBorder;
            }
            else Debug.LogError("[GameloopManager] SceneObjectRegistry.Instance is null!", this);

            // Spawn người chơi lần đầu tiên khi game khởi chạy.
            _playerManager?.SpawnInitialPlayer();

            // Bắt đầu vòng lặp game chính
            StartCoroutine(GameLoopCoroutine());
        }

        #endregion

        #region Game Loop Coroutine

        /// <summary>
        /// Coroutine chính điều khiển toàn bộ vòng lặp của game, từ lúc chờ, chọn map, thi đấu, cho đến khi kết thúc và lặp lại.
        /// </summary>
        private IEnumerator GameLoopCoroutine()
        {
            while (true)
            {
                yield return StartCoroutine(MapVotingStage());

                if (_selectedMapIndex != -1)
                {
                    yield return StartCoroutine(BuildingStage());
                    yield return StartCoroutine(PreRoundStage());
                    yield return StartCoroutine(RoundActiveStage());
                    yield return StartCoroutine(PostRoundStage());
                }
                else
                {
                    Debug.LogWarning("[GameloopManager] Không có map nào được chọn để xây dựng. Bỏ qua chu kỳ.");
                    yield return _waitForSeconds2; // Chờ một chút trước khi lặp lại
                }

                Debug.Log("[GameloopManager] Khởi động lại vòng lặp...");
                yield return _waitForSeconds1; // Chờ 1 giây trước khi lặp lại
            }
        }

        #region Game Loop Stages

        /// <summary>
        /// Giai đoạn 1: Xử lý việc bỏ phiếu cho map. Hiển thị timer, chọn map ngẫu nhiên và thông báo kết quả.
        /// </summary>
        /// <returns>IEnumerator để chạy như một coroutine.</returns>
        private IEnumerator MapVotingStage()
        {
            // --- Giai đoạn 1: Bỏ phiếu Map ---
            CurrentState = GameState.MapVoting;
            Debug.Log("[GameloopManager] Giai đoạn 1: Bỏ phiếu Map");
            _uiManager?.ShowNotification("Voting for the next map...", _votingDuration);

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
            _uiManager?.ShowNotification($"Map selected: {selectedMapName}", _mapRevealDuration);
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
            _uiManager?.ShowNotification("Building map...", 1000f); // Hiển thị lâu, sẽ bị ẩn sau khi build xong

            // Xây dựng map đã chọn một cách bất đồng bộ
            yield return StartCoroutine(BuildSelectedMap(_selectedMapIndex, _selectedUndergroundIndex));

            // Chờ một chút để FPS ổn định sau khi tải nhiều đối tượng
            Debug.Log($"[GameloopManager] Map đã xây xong. Chờ ổn định trong {_postBuildStabilizationDuration} giây.");
            yield return new WaitForSeconds(_postBuildStabilizationDuration);

            _uiManager?.HideNotification();
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

            // 2. Gán độ khó cho round hiện tại và hiển thị nó ngay lập tức.
            CurrentIntensity = Mathf.Clamp(_nextRoundIntensity, _minIntensity, _maxIntensity);
            Debug.Log($"[GameloopManager] Round starting. Current intensity locked at: {CurrentIntensity}");
            if (_uiManager != null)
            {
                yield return StartCoroutine(_uiManager.AnimateIntensityBar(CurrentIntensity, _minIntensity, _maxIntensity));
                _uiManager.ShowCurrentIntensity(CurrentIntensity);
            }
            
            // Bây giờ, tính toán lại độ khó CƠ BẢN cho round TIẾP THEO dựa trên số người chơi của round này.
            // Giá trị này sẽ được điều chỉnh giảm xuống khi có người chơi chết trong round hiện tại.
            // Theo yêu cầu: +0.3 cho 100% danh sách player, không phải mỗi player.
            int playerCount = _playerManager.GetAlivePlayerCount();
            _nextRoundIntensity = _minIntensity; // Bắt đầu với độ khó tối thiểu
            if (playerCount > 0) // Nếu có ít nhất một người chơi, thêm bonus cho cả nhóm
            {
                _nextRoundIntensity += _intensityPerPlayer;
            }
            Debug.Log($"[GameloopManager] Base intensity for NEXT round calculated based on {playerCount} players: {_nextRoundIntensity}");

            yield return new WaitForSeconds(_postTeleportWaitDuration);

            // --- Giai đoạn 4: Đếm ngược & Bắt đầu ---
            Debug.Log("[GameloopManager] Giai đoạn 4: Đếm ngược và Bắt đầu");
            // Sử dụng coroutine đếm ngược mới từ UIManager
            if (_uiManager != null) yield return StartCoroutine(_uiManager.ShowCountdown());

            // Bắt đầu phát nhạc gameplay sau khi đếm ngược kết thúc.
            _playerManager?.SetGameplayMusicForRoundPlayers(CurrentIntensity);
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
            Debug.Log("[GameloopManager] Giai đoạn 5: Round đang diễn ra");

            float roundTimer = _roundDuration;
            int lastDisplayedSecond = Mathf.CeilToInt(roundTimer);

            // Lấy số người chơi lúc bắt đầu round để xác định điều kiện thắng.
            // Điều kiện kết thúc round: khi không còn người chơi nào sống sót (số người chơi còn lại là 0).
            int endConditionPlayerCount = 0;
            
            float lateJoinTimer = _lateJoinDuration;
            bool lateJoinPeriodActive = true;
            bool last30sMusicTriggered = false;

            // Vòng lặp chính của round đấu.
            // Điều kiện kết thúc: hết giờ, hoặc số người chơi còn lại đạt ngưỡng kết thúc.
            while (roundTimer > 0f && (_playerManager?.GetAlivePlayerCount() > endConditionPlayerCount))
            {
                // Xử lý người chơi tham gia muộn
                if (lateJoinPeriodActive)
                {
                    lateJoinTimer -= Time.deltaTime;
                    if (lateJoinTimer <= 0f)
                    {
                        lateJoinPeriodActive = false;
                        Debug.Log("[GameloopManager] Late join period ended.");
                    }
                    else
                    {
                        var playersToJoin = _playerManager.GetPlayersNotInCurrentRound();
                        foreach (var player in playersToJoin)
                        {
                            TeleportPlayerToArena(player); // Dịch chuyển người chơi mới vào arena
                            _playerManager.AddPlayerToCurrentRound(player); // Thêm vào danh sách người chơi trong round
                            // Theo yêu cầu: tăng độ khó theo tỷ lệ _intensityPerPlayer chia cho tổng số người chơi mới (số người chơi hiện tại + 1).
                            // Lưu ý: _playerManager.GetAlivePlayerCount() ở đây trả về số người chơi TRƯỚC KHI người chơi hiện tại được thêm vào danh sách _playersInRound.
                            float intensityIncrease = _intensityPerPlayer / (_playerManager.GetAlivePlayerCount() + 1);
                            _nextRoundIntensity = Mathf.Clamp(_nextRoundIntensity + intensityIncrease, _minIntensity, _maxIntensity);
                            Debug.Log($"[GameloopManager] Late joiner {player.GameObject.name} added. Next round intensity adjusted to: {_nextRoundIntensity}");
                        }
                    }
                }
                roundTimer -= Time.deltaTime;
                int currentSecond = Mathf.CeilToInt(roundTimer);

                if (currentSecond < lastDisplayedSecond)
                {
                    lastDisplayedSecond = currentSecond;
                    _uiManager?.UpdateTimer(lastDisplayedSecond);
                }

                // Kích hoạt nhạc 30 giây cuối
                if (!last30sMusicTriggered && roundTimer <= 30f)
                {
                    _playerManager?.SetLast30sMusicForRoundPlayers(CurrentIntensity);
                    last30sMusicTriggered = true;
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

            // Dịch chuyển những người chơi còn sống sót trong round về sảnh chờ ngay lập tức.
            // Phương thức này phải được gọi TRƯỚC EndRound(), vì EndRound() sẽ xóa danh sách người chơi trong round.
            _playerManager?.ReturnRoundSurvivorsToLobby();

            // Bật lại nhạc sảnh chờ cho tất cả người chơi.
            _playerManager?.SetLobbyMusicForAllPlayers();

            // Hiển thị thông báo kết thúc round
            _uiManager?.ShowNotification("Round Over!", _postRoundWaitDuration);

            yield return new WaitForSeconds(_postRoundWaitDuration);

            // Kích hoạt lại các player đã chết và dọn dẹp danh sách round
            _playerManager?.EndRound(); 

            // DỌN DẸP HIỆU ỨNG: Kích hoạt sự kiện để các hiệu ứng còn sót lại (khí độc, điện...) tự hủy.
            GameEvents.TriggerRoundEndCleanup();
            Debug.Log("[GameloopManager] Triggered Round End Cleanup event for stray effects.");

            // Hiển thị thông báo đang dọn dẹp
            _uiManager?.ShowNotification("Cleaning up the play area...", 1000f); // Hiển thị lâu, sẽ bị ẩn sau khi dọn xong
            Debug.Log("[GameloopManager] Bắt đầu dọn dẹp map.");

            yield return StartCoroutine(CleanupRoundAsync());
            Debug.Log("[GameloopManager] Dọn dẹp map hoàn tất.");

            // Chờ ổn định FPS sau khi dọn dẹp. Thông báo vẫn hiển thị trong lúc này.
            Debug.Log($"[GameloopManager] Chờ ổn định FPS trong {_postCleanupStabilizationDuration} giây.");
            yield return new WaitForSeconds(_postCleanupStabilizationDuration);

            // Ẩn thông báo sau khi đã ổn định
            _uiManager?.HideNotification();
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
        /// Dịch chuyển tất cả người chơi hiện đang hoạt động đến một vị trí ngẫu nhiên trong khu vực arena.
        /// </summary>
        private void TeleportPlayersToArena()
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
            var players = _playerManager?.GetAllPlayers();
            if (players == null) return;

            // SỬA LỖI: Phải cập nhật vị trí của các đối tượng môi trường TRƯỚC KHI dịch chuyển người chơi vào.
            // Điều này đảm bảo người chơi luôn được spawn bên trong các ranh giới đã được cập nhật của round mới.
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

            // Sau khi môi trường đã được định vị, tiến hành dịch chuyển người chơi.
            foreach (var player in players)
            {
                player.StopMusic(); // Dừng nhạc lobby của người chơi
                TeleportPlayerToArena(player);
            }
        }

        private IEnumerator CleanupRoundAsync()
        {
            yield return StartCoroutine(_mapManager.ClearCurrentMapAsync());
            _bombSpawnerManager?.ClearAllBombs();
        }

        private void HandlePlayerDeath(IPlayer player)
        {
            if (CurrentState != GameState.RoundActive) return;

            _nextRoundIntensity = Mathf.Max(_minIntensity, _nextRoundIntensity - _intensityReductionOnDeath);
            Debug.Log($"[GameloopManager] Player died. Next round intensity adjusted to: {_nextRoundIntensity}");
        }
        #endregion
    }
}