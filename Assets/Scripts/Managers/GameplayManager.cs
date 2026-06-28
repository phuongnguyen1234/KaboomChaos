using UnityEngine;
using System.Collections.Generic;
using Core;
using Core.Interfaces;

namespace Managers
{
    /// <summary>
    /// Quản lý luồng chơi chính, bao gồm việc khởi tạo game, sinh người chơi và quản lý vòng lặp game.
    /// Đây là một lớp partial, với logic xử lý người chơi được tách ra trong file GameplayManager.PlayerHandler.cs.
    /// </summary>
    public partial class GameplayManager : MonoBehaviour, IGameplayManager
    {
        #region Properties

        /// <summary>
        /// Thể hiện Singleton của GameplayManager, cho phép truy cập toàn cục.
        /// </summary>
        public static GameplayManager Instance { get; private set; }

        #endregion

        #region Fields

        [Header("Player Settings")]
        [Tooltip("Prefab của người chơi sẽ được sinh ra trong game.")]
        [SerializeField] private GameObject _playerPrefab;
        
        [Tooltip("Danh sách các điểm spawn người chơi. Sẽ tự động tìm và đăng ký nếu bỏ trống.")]
        [SerializeField] private List<PlayerSpawn> _spawnPoints = new(); // Danh sách các điểm spawn đã được đăng ký.

        private IPlayer _currentPlayer; // Tham chiếu đến đối tượng người chơi hiện tại đã được sinh ra.

        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // Triển khai mẫu Singleton để đảm bảo chỉ có một thể hiện của GameplayManager
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

        private void Start()
        {
            InitializeGame();
            StartGameLoop();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Khởi tạo các thành phần cần thiết cho game.
        /// </summary>
        public void InitializeGame()
        {
            Debug.Log("GameplayManager: Initializing game...");
            FindAndRegisterAllSpawnPoints();
            // Các thiết lập khởi tạo khác cho game có thể được thêm vào đây.
        }

        /// <summary>
        /// Bắt đầu vòng lặp chính của game, bao gồm việc sinh người chơi.
        /// </summary>
        public void StartGameLoop()
        {
            Debug.Log("GameplayManager: Starting game loop...");

            PlayerSpawn spawnPoint = GetRandomSpawnPoint();
            if (spawnPoint == null)
            {
                Debug.LogError("No PlayerSpawn found in the scene! Cannot spawn player.", this);
                return;
            }

            // Gọi phương thức partial từ file GameplayManager.PlayerHandler.cs để xử lý việc sinh người chơi.
            _currentPlayer = HandlePlayerSpawn(_playerPrefab, spawnPoint);
        }

        /// <summary>
        /// Kết thúc vòng lặp game, thực hiện dọn dẹp hoặc chuyển đổi trạng thái.
        /// </summary>
        public void EndGameLoop()
        {
            Debug.Log("GameplayManager: Ending game loop...");
            // Logic dọn dẹp hoặc chuyển đổi trạng thái game sẽ được thêm vào đây.
        }

        /// <summary>
        /// Đăng ký một điểm spawn vào danh sách quản lý.
        /// </summary>
        /// <param name="spawnPoint">Điểm spawn cần đăng ký.</param>
        public void RegisterSpawnPoint(PlayerSpawn spawnPoint)
        {
            if (spawnPoint != null && !_spawnPoints.Contains(spawnPoint))
            {
                _spawnPoints.Add(spawnPoint);
            }
        }

        /// <summary>
        /// Hủy đăng ký một điểm spawn khỏi danh sách.
        /// </summary>
        /// <param name="spawnPoint">Điểm spawn cần hủy đăng ký.</param>
        public void UnregisterSpawnPoint(PlayerSpawn spawnPoint)
        {
            if (spawnPoint != null)
            {
                _spawnPoints.Remove(spawnPoint);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Lấy một điểm spawn ngẫu nhiên từ danh sách đã đăng ký.
        /// </summary>
        /// <returns>Một đối tượng PlayerSpawn ngẫu nhiên, hoặc null nếu không có điểm spawn nào.</returns>
        private PlayerSpawn GetRandomSpawnPoint()
        {
            if (_spawnPoints == null || _spawnPoints.Count == 0) return null;
            return _spawnPoints[Random.Range(0, _spawnPoints.Count)];
        }

        /// <summary>
        /// Tìm và đăng ký tất cả các đối tượng PlayerSpawn có trong scene.
        /// </summary>
        private void FindAndRegisterAllSpawnPoints()
        {
            var foundSpawnPoints = FindObjectsByType<PlayerSpawn>();
            _spawnPoints.Clear(); // Xóa danh sách cũ để tránh trùng lặp khi tải lại scene.
            foreach (var spawnPoint in foundSpawnPoints)
            {
                RegisterSpawnPoint(spawnPoint);
            }
            Debug.Log($"Found and registered {foundSpawnPoints.Length} spawn points.");
        }
    }

    #endregion
}