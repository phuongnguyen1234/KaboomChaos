using UnityEngine;
using Core.Interfaces;
using Core;
using System.Collections;
using System.Collections.Generic;
using Bombs;
using System.Linq;

namespace Managers
{
    /// <summary>
    /// Quản lý việc sinh các loại bom ngẫu nhiên trong một khu vực được chỉ định.
    /// </summary>
    public class BombSpawnerManager : BaseGameObjectPoolManager, IBombSpawnerManager
    {
        public static IBombSpawnerManager Instance { get; private set; }

        [Header("Dependencies")]
        [Tooltip("Database chứa tất cả các loại bom có trong game.")]
        [SerializeField] private BombDatabase _bombDatabase;
        [Tooltip("Khu vực (dạng BoxCollider) nơi bom sẽ được sinh ra. Bom sẽ xuất hiện trên bề mặt trên cùng của box này.")]
        [SerializeField] private BoxCollider _spawnArea;

        [Header("Spawning Settings")]
        [Tooltip("Thời gian (giây) giữa mỗi lần sinh bom.")]
        [SerializeField] private float _spawnInterval = 3f;
        [Tooltip("Độ khó hiện tại của game, dùng để tính toán xác suất xuất hiện của bom.")]
        [SerializeField] private float _currentDifficulty = 1f; // Giá trị này có thể được cập nhật bởi GameloopManager

        private Coroutine _spawnCoroutine;

        protected override void Awake()
        {
            base.Awake(); // Gọi Awake của lớp cơ sở
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
            PrewarmPools();
        }

        private void OnEnable()
        {
            GameEvents.OnBombDespawnRequest += ReturnToPool;
        }

        private void OnDisable()
        {
            GameEvents.OnBombDespawnRequest -= ReturnToPool;
        }

        /// <summary>
        /// Cập nhật độ khó hiện tại của game, ảnh hưởng đến xác suất sinh bom.
        /// </summary>
        /// <param name="difficulty">Giá trị độ khó mới.</param>
        public void SetDifficulty(float difficulty)
        {
            _currentDifficulty = difficulty;
            Debug.Log($"[BombSpawnerManager] Difficulty set to {difficulty}.");
        }

        public void StartSpawning()
        {
            if (_spawnCoroutine != null) StopSpawning();
            _spawnCoroutine = StartCoroutine(SpawnBombRoutine());
            Debug.Log("[BombSpawnerManager] Started spawning bombs.");
        }

        public void StopSpawning()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
                Debug.Log("[BombSpawnerManager] Stopped spawning bombs.");
            }
        }

        private IEnumerator SpawnBombRoutine()
        {
            if (_bombDatabase == null || _bombDatabase.bombs.Count == 0)
            {
                Debug.LogError("[BombSpawnerManager] BombDatabase is not assigned or is empty!", this);
                yield break;
            }
            if (_spawnArea == null)
            {
                Debug.LogError("[BombSpawnerManager] Spawn Area (BoxCollider) is not assigned!", this);
                yield break;
            }

            while (true)
            {
                yield return new WaitForSeconds(_spawnInterval);

                BaseBombData selectedBombData = GetRandomBombData();
                if (selectedBombData == null)
                {
                    Debug.LogWarning("[BombSpawnerManager] Could not select a valid bomb to spawn (check weights for current difficulty). Skipping cycle.");
                    continue;
                }

                GameObject bombInstance = GetFromPool(selectedBombData.bombPrefab, GetRandomSpawnPosition(), Quaternion.identity);
                if (bombInstance == null) continue;

                if (bombInstance.TryGetComponent<IBombController>(out var bombController))
                {
                    bombController.Activate();
                }
            }
        }

        /// <summary>
        /// Khởi tạo và làm đầy sẵn các pool bom dựa trên BombDatabase.
        /// </summary>
        private void PrewarmPools()
        {
            if (_bombDatabase == null) return;

            foreach (var bombData in _bombDatabase.bombs.Where(b => b.bombPrefab != null))
            {
                // Lấy ra và trả lại ngay lập tức để khởi tạo pool với số lượng ban đầu.
                var instances = new List<GameObject>();
                for (int i = 0; i < _initialPoolSize; i++)
                {
                    instances.Add(GetFromPool(bombData.bombPrefab, Vector3.zero, Quaternion.identity));
                }
                foreach (var instance in instances)
                {
                    ReturnToPool(instance);
                }
            }
        }

        private BaseBombData GetRandomBombData()
        {
            var weightedList = new List<(BaseBombData data, float weight)>();
            float totalWeight = 0f;

            foreach (var bombData in _bombDatabase.bombs)
            {
                if (bombData.bombPrefab == null) continue;

                float weight = bombData.spawnWeightByDifficulty.Evaluate(_currentDifficulty);
                if (weight > 0)
                {
                    weightedList.Add((bombData, weight));
                    totalWeight += weight;
                }
            }

            if (totalWeight == 0) return null;

            float randomValue = Random.Range(0, totalWeight);
            foreach (var item in weightedList)
            {
                if (randomValue < item.weight) return item.data;
                randomValue -= item.weight;
            }
            return null; // Should not be reached
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Bounds bounds = _spawnArea.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            return new Vector3(randomX, bounds.max.y, randomZ);
        }

        /// <summary>
        /// Trả tất cả các quả bom đang hoạt động về lại pool.
        /// </summary>
        public void ClearAllBombs()
        {
            // Lớp cơ sở theo dõi các instance đang hoạt động thông qua _instanceToPrefabMap.
            // Chúng ta có thể lặp qua nó để trả về pool.
            Debug.Log($"[BombSpawnerManager] Clearing all {_instanceToPrefabMap.Count} active bombs.");
            // Tạo một bản sao của danh sách keys để tránh lỗi "Collection was modified" khi ReturnToPool sửa đổi nó.
            var activeInstances = new List<GameObject>(_instanceToPrefabMap.Keys);
            foreach (var bombInstance in activeInstances)
            {
                ReturnToPool(bombInstance);
            }
        }

        protected override void OnGetInstance(GameObject instance)
        {
            base.OnGetInstance(instance);
            // Reset trạng thái của bom khi nó được lấy ra từ pool.
            instance.GetComponent<IBombController>()?.ResetState();
        }
    }
}