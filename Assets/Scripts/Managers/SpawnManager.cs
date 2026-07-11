using UnityEngine;
using Core.Interfaces;
using System.Collections.Generic;
using Core;
namespace Managers
{
    /// <summary>
    /// Quản lý tất cả các điểm spawn trong game.
    /// Cung cấp các phương thức để đăng ký, hủy đăng ký và lấy điểm spawn.
    /// </summary>
    public class SpawnManager : MonoBehaviour, ISpawnManager
    {
        #region Singleton
        /// <summary>
        /// Thể hiện Singleton của SpawnManager, cho phép truy cập toàn cục.
        /// </summary>
        public static ISpawnManager Instance { get; private set; }
        #endregion

        #region Fields
        [Tooltip("Danh sách các điểm spawn người chơi. Sẽ tự động tìm và đăng ký khi khởi động.")]
        [SerializeField] private List<PlayerSpawn> _spawnPoints = new();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Triển khai mẫu Singleton để đảm bảo chỉ có một thể hiện của SpawnManager
            if (Instance != null && Instance as MonoBehaviour != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Giữ Manager tồn tại khi chuyển đổi giữa các scene.
                FindAndRegisterAllSpawnPoints();
            }
        }
        #endregion

        #region Public Methods (ISpawnManager Implementation)
        /// <summary>
        /// Lấy một điểm spawn ngẫu nhiên từ danh sách đã đăng ký.
        /// </summary>
        /// <returns>Một đối tượng PlayerSpawn ngẫu nhiên, hoặc null nếu không có điểm spawn nào.</returns>
        public PlayerSpawn GetRandomSpawnPoint()
        {
            if (_spawnPoints == null || _spawnPoints.Count == 0) return null;
            return _spawnPoints[Random.Range(0, _spawnPoints.Count)];
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
        /// Thường được gọi từ OnDestroy() của PlayerSpawn.
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
        /// Tìm và đăng ký tất cả các đối tượng PlayerSpawn có trong scene khi khởi động.
        /// </summary>
        private void FindAndRegisterAllSpawnPoints()
        {
            // Sử dụng FindObjectsByType để lấy tất cả các thể hiện của PlayerSpawn trong scene.
            // FindObjectsSortMode.None có thể nhanh hơn một chút nếu không cần sắp xếp.
            var foundSpawnPoints = FindObjectsByType<PlayerSpawn>();
            _spawnPoints.Clear(); // Xóa danh sách cũ để tránh trùng lặp khi tải lại scene.
            foreach (var spawnPoint in foundSpawnPoints) 
            { 
                RegisterSpawnPoint(spawnPoint); 
            }
            Debug.Log($"[SpawnManager] Found and registered {foundSpawnPoints.Length} spawn points.");
        }
        #endregion
    }
}