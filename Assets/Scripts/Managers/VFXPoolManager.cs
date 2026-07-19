using UnityEngine;
using System.Collections.Generic;
using Core;
using Core.Interfaces;

namespace Managers
{
    /// <summary>
    /// Quản lý object pooling cho các hiệu ứng hình ảnh (VFX) để tối ưu hóa hiệu năng.
    /// Đây là một singleton, đảm bảo chỉ có một instance tồn tại trong suốt game.
    /// </summary>
    public class VFXPoolManager : MonoBehaviour, IVFXManager, IGameObjectPoolManager
    {
        private static IVFXManager _instance;

        // Dictionary chính của pool, map từ Prefab gốc sang một hàng đợi các instance không hoạt động.
        private Dictionary<GameObject, Queue<GameObject>> _poolDictionary;
        // Dictionary phụ để tra cứu nhanh prefab gốc từ một instance đang hoạt động.
        private Dictionary<GameObject, GameObject> _instanceToPrefabMap;

        private void Awake()
        {
            if (_instance != null && _instance as MonoBehaviour != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                _poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
                _instanceToPrefabMap = new Dictionary<GameObject, GameObject>();
                DontDestroyOnLoad(gameObject); // Giữ manager tồn tại khi chuyển scene.
            }
        }

        private void OnEnable()
        {
            // Đăng ký lắng nghe sự kiện yêu cầu despawn từ các hiệu ứng.
            GameEvents.OnVFXDespawnRequest += ReturnToPool;
            GameEvents.OnVFXSpawnRequest += GetFromPool;
        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh lỗi.
            GameEvents.OnVFXDespawnRequest -= ReturnToPool;
            GameEvents.OnVFXSpawnRequest -= GetFromPool;
        }

        /// <summary>
        /// Triển khai phương thức Spawn từ interface IVFXManager.
        /// Nó chỉ đơn giản là một alias cho GetFromPool.
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return GetFromPool(prefab, position, rotation);
        }

        /// <summary>
        /// Lấy một hiệu ứng từ pool hoặc tạo mới nếu cần. Triển khai từ IGameObjectPoolManager.
        /// </summary>
        public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Yêu cầu spawn một prefab VFX null.");
                return null;
            }

            if (!_poolDictionary.ContainsKey(prefab))
            {
                _poolDictionary.Add(prefab, new Queue<GameObject>());
            }

            GameObject effectInstance;
            if (_poolDictionary[prefab].Count > 0)
            {
                effectInstance = _poolDictionary[prefab].Dequeue();
            }
            else
            {
                effectInstance = Instantiate(prefab);
                _instanceToPrefabMap.Add(effectInstance, prefab); // Map instance với prefab gốc của nó.
            }

            effectInstance.transform.SetPositionAndRotation(position, rotation);
            effectInstance.SetActive(true);

            return effectInstance;
        }

        /// <summary>
        /// Trả một hiệu ứng về lại pool để tái sử dụng. Triển khai từ IGameObjectPoolManager.
        /// </summary>
        public void ReturnToPool(GameObject effectInstance)
        {
            if (effectInstance == null) return;

            if (_instanceToPrefabMap.ContainsKey(effectInstance))
            {
                effectInstance.SetActive(false);
                _poolDictionary[_instanceToPrefabMap[effectInstance]].Enqueue(effectInstance);
            }
            // Không cần cảnh báo ở đây vì event có thể được gọi bởi các object không thuộc pool.
        }
    }
}