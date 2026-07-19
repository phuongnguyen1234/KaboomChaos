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
    public class BombSpawnerManager : MonoBehaviour, IBombSpawnerManager, IGameObjectPoolManager
    {
        public static IBombSpawnerManager Instance { get; private set; }

        [Header("Dependencies")]
        [Tooltip("Database chứa tất cả các loại bom có trong game.")]
        [SerializeField] private BombDatabase _bombDatabase;
        [Tooltip("Khu vực (dạng BoxCollider) nơi bom sẽ được sinh ra. Bom sẽ xuất hiện trên bề mặt trên cùng của box này.")]
        [SerializeField] private BoxCollider _spawnArea;

        [Header("Settings")]
        [Tooltip("Thời gian (giây) giữa mỗi lần sinh bom.")]
        [SerializeField] private float _spawnInterval = 3f;
        [Tooltip("Số lượng bom mỗi loại được tạo sẵn trong pool.")]
        [SerializeField] private int _poolInitialSize = 5;
        [Tooltip("Số lượng bom sẽ được tạo thêm mỗi khi pool hết và cần mở rộng.")]
        [SerializeField] private int _poolExpansionChunkSize = 3;
        [Tooltip("Độ khó hiện tại của game, dùng để tính toán xác suất xuất hiện của bom.")]
        [SerializeField] private float _currentDifficulty = 1f; // Giá trị này có thể được cập nhật bởi GameloopManager

        private Coroutine _spawnCoroutine;

        // Object Pooling
        private Dictionary<string, (Queue<GameObject> queue, Transform container)> _bombPools = new();
        private Dictionary<GameObject, string> _activeBombInstances = new();

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
            CreatePools();
        }

        private void OnEnable()
        {
            GameEvents.OnBombDespawnRequest += ReturnToPool;
        }

        private void OnDisable()
        {
            GameEvents.OnBombDespawnRequest -= ReturnToPool;
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

        private void CreatePools()
        {
            if (_bombDatabase == null) return;

            foreach (var bombData in _bombDatabase.bombs.Where(b => b.bombPrefab != null))
            {
                string key = bombData.bombPrefab.name;
                if (_bombPools.ContainsKey(key)) continue;

                var queue = new Queue<GameObject>();
                var poolContainer = new GameObject($"Pool - {key}");
                poolContainer.transform.SetParent(transform);

                for (int i = 0; i < _poolInitialSize; i++)
                {
                    var bombInstance = Instantiate(bombData.bombPrefab, poolContainer.transform);
                    bombInstance.SetActive(false);
                    queue.Enqueue(bombInstance);
                }
                _bombPools.Add(key, (queue, poolContainer.transform));
            }
        }

        /// <summary>
        /// Lấy một đối tượng bom từ pool. Triển khai từ IGameObjectPoolManager.
        /// </summary>
        public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("[BombSpawnerManager] Yêu cầu lấy đối tượng từ pool với prefab null.", this);
                return null;
            }

            string key = prefab.name;
            if (!_bombPools.TryGetValue(key, out var pool))
            {
                Debug.LogError($"[BombSpawnerManager] Pool for '{key}' does not exist.", this);
                return null;
            }

            // Nếu pool hết, nới rộng nó ra.
            if (pool.queue.Count == 0)
            {
                int amountToCreate = _poolExpansionChunkSize > 0 ? _poolExpansionChunkSize : 1;
                Debug.LogWarning($"[BombSpawnerManager] Pool for '{key}' is empty. Expanding by {amountToCreate} instance(s).", this);
                for (int i = 0; i < amountToCreate; i++)
                {
                    var newInstance = Instantiate(prefab, pool.container);
                    newInstance.SetActive(false);
                    pool.queue.Enqueue(newInstance);
                }
            }

            // Bây giờ, lấy một instance ra khỏi pool.
            GameObject bombInstance = pool.queue.Dequeue();
            bombInstance.transform.SetParent(null); // Lấy ra khỏi container
            bombInstance.transform.SetPositionAndRotation(position, rotation);
            bombInstance.SetActive(true);
            bombInstance.GetComponent<IBombController>()?.ResetState();
            _activeBombInstances.Add(bombInstance, key);
            return bombInstance;
        }

        /// <summary>
        /// Trả một đối tượng bom về lại pool. Triển khai từ IGameObjectPoolManager.
        /// </summary>
        public void ReturnToPool(GameObject bombInstance)
        {
            if (_activeBombInstances.TryGetValue(bombInstance, out string key) && _bombPools.TryGetValue(key, out var pool))
            {
                bombInstance.SetActive(false);
                bombInstance.transform.SetParent(pool.container);
                pool.queue.Enqueue(bombInstance);
                _activeBombInstances.Remove(bombInstance);
            }
            else
            {
                Debug.LogWarning($"[BombSpawnerManager] Received a despawn request for an untracked or unknown bomb '{bombInstance.name}'. Destroying it.", bombInstance);
                Destroy(bombInstance);
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
    }
}