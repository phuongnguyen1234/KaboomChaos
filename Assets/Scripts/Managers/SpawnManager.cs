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
        private readonly List<PlayerSpawn> _spawnPoints = new();
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
            }
        }

        private void Start()
        {
            // Lấy danh sách các điểm spawn từ registry trung tâm.
            var registry = SceneObjectRegistry.Instance;
            if (registry != null && registry.PlayerSpawnPoints != null)
            {
                _spawnPoints.AddRange(registry.PlayerSpawnPoints);
                Debug.Log($"[SpawnManager] Registered {_spawnPoints.Count} spawn points from registry.");
            }
            else Debug.LogError("[SpawnManager] SceneObjectRegistry.Instance is null or has no spawn points assigned!", this);
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
        /// Hữu ích nếu các điểm spawn được tạo ra một cách linh động trong quá trình chơi.
        /// </summary>
        /// <param name="spawnPoint">Điểm spawn cần đăng ký.</param>
        public void RegisterSpawnPoint(PlayerSpawn spawnPoint)
        {
            if (spawnPoint != null && !_spawnPoints.Contains(spawnPoint))
            {
                _spawnPoints.Add(spawnPoint);
                Debug.Log($"[SpawnManager] Dynamically registered spawn point: {spawnPoint.name}", spawnPoint);
            }
        }

        /// <summary>
        /// Hủy đăng ký một điểm spawn khỏi danh sách.
        /// </summary>
        /// <param name="spawnPoint">Điểm spawn cần hủy đăng ký.</param>
        public void UnregisterSpawnPoint(PlayerSpawn spawnPoint)
        {
            if (spawnPoint != null && _spawnPoints.Remove(spawnPoint))
            {
                Debug.Log($"[SpawnManager] Unregistered spawn point: {spawnPoint.name}", spawnPoint);
            }
        }
        #endregion
    }
}