using UnityEngine;
using Core;
using Core.Interfaces;

namespace Managers
{
    /// <summary>
    /// Quản lý luồng chơi chính (Game Loop), bao gồm việc khởi tạo, bắt đầu và kết thúc game.
    /// Lớp này đóng vai trò điều phối các Manager khác (như PlayerManager, SpawnManager, v.v.).
    /// </summary>
    public class GameloopManager : MonoBehaviour, IGameloopManager
    {
        #region Properties

        /// <summary>
        /// Thể hiện Singleton của GameloopManager, cho phép truy cập toàn cục.
        /// </summary>
        public static GameloopManager Instance { get; private set; }

        #endregion

        #region Fields

        // Tham chiếu đến các manager khác sẽ được lấy thông qua Singleton hoặc Service Locator.
        private IPlayerManager _playerManager;
        private IMapManager _mapManager;
        private IBombSpawnerManager _bombSpawnerManager;

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

                // Lấy tham chiếu đến các manager khác.
                // Giả định rằng các manager này cũng là Singleton và đã được khởi tạo.
                _playerManager = PlayerManager.Instance;
                _mapManager = MapManager.Instance;
                _bombSpawnerManager = BombSpawnerManager.Instance;
            }
        }
        
        private void Start()
        {
            InitializeGame();
            StartGameLoop(); // Bắt đầu vòng lặp game, sinh người chơi lần đầu.
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Khởi tạo các thành phần cần thiết cho game.
        /// </summary>
        public void InitializeGame()
        {
            Debug.Log("[GameloopManager] Initializing game...");
            // Các manager khác (như SpawnManager, UIManager) sẽ tự khởi tạo trong Awake của chúng.

            if (_mapManager == null)
            {
                Debug.LogError("[GameloopManager] MapManager is not available. Cannot load map.", this);
                return;
            }

            // Lấy số lượng map và underground có sẵn
            int mapCount = _mapManager.GetMapCount();
            int undergroundCount = _mapManager.GetUndergroundDataCount();

            if (mapCount > 0 && undergroundCount > 0)
            {
                // Chọn ngẫu nhiên một chỉ số cho map và underground
                int randomMapIndex = Random.Range(0, mapCount);
                int randomUndergroundIndex = Random.Range(0, undergroundCount);

                Debug.Log($"[GameloopManager] Loading random map. MapIndex: {randomMapIndex}, UndergroundIndex: {randomUndergroundIndex}");
                _mapManager.LoadMapByIndex(randomMapIndex, randomUndergroundIndex);
            }
            else
            {
                Debug.LogWarning("[GameloopManager] Not enough maps or underground profiles in databases to load randomly. Check MapManager databases.", this);
            }
        }

        /// <summary>
        /// Bắt đầu vòng lặp chính của game, bao gồm việc sinh người chơi.
        /// </summary>
        public void StartGameLoop()
        {
            Debug.Log("[GameloopManager] Starting game loop...");
            // Ra lệnh cho PlayerManager sinh người chơi ban đầu.
            _playerManager?.SpawnInitialPlayer();
            _bombSpawnerManager?.StartSpawning();
        }

        /// <summary>
        /// Kết thúc vòng lặp game, thực hiện dọn dẹp hoặc chuyển đổi trạng thái.
        /// </summary>
        public void EndGameLoop()
        {
            Debug.Log("[GameloopManager] Ending game loop...");
            // Logic dọn dẹp hoặc chuyển đổi trạng thái game sẽ được thêm vào đây.
            _bombSpawnerManager?.StopSpawning();
        }

        #endregion
    }
}