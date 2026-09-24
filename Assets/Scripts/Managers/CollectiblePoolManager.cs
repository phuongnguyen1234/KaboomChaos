using UnityEngine;
using Core.Interfaces;
using Core;
using System.Collections.Generic;
using System.Linq;

namespace Managers
{
    public class CollectiblePoolManager : BaseGameObjectPoolManager, ICollectiblePoolManager
    {
        public static ICollectiblePoolManager Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private ScriptableObject _collectibleDatabaseAsset;
        private ICollectibleDatabase _collectibleDatabase;

        protected override void Awake()
        {
            base.Awake();
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
            if (_collectibleDatabaseAsset is ICollectibleDatabase db)
            {
                _collectibleDatabase = db;
                PrewarmPools();
            }
            else
            {
                Debug.LogError("[CollectiblePoolManager] Collectible Database is not assigned or does not implement ICollectibleDatabase.", this);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnCollectibleSpawnRequest += GetFromPool;
            GameEvents.OnCollectibleDespawnRequest += ReturnToPool;
        }

        private void OnDisable()
        {
            GameEvents.OnCollectibleSpawnRequest -= GetFromPool;
            GameEvents.OnCollectibleDespawnRequest -= ReturnToPool;
        }

        private void PrewarmPools()
        {
            if (_collectibleDatabase == null) return;

            foreach (var mapping in _collectibleDatabase.Mappings)
            {
                if (mapping.Prefab == null) continue;

                var instances = new List<GameObject>();
                for (int i = 0; i < _initialPoolSize; i++)
                {
                    instances.Add(GetFromPool(mapping.Prefab, Vector3.zero, Quaternion.identity));
                }
                foreach (var instance in instances)
                {
                    ReturnToPool(instance);
                }
            }
        }

        /// <summary>
        /// Trả tất cả các vật phẩm đang hoạt động về lại pool.
        /// </summary>
        public void ClearAllCollectibles()
        {
            // Lớp cơ sở theo dõi các instance đang hoạt động thông qua _instanceToPrefabMap.
            // Chúng ta có thể lặp qua nó để trả về pool.
            Debug.Log($"[CollectiblePoolManager] Clearing all {_instanceToPrefabMap.Count} active collectibles.");
            // Tạo một bản sao của danh sách keys để tránh lỗi "Collection was modified" khi ReturnToPool sửa đổi nó.
            var activeInstances = new List<GameObject>(_instanceToPrefabMap.Keys);
            foreach (var collectibleInstance in activeInstances)
                ReturnToPool(collectibleInstance);
        }
    }
}