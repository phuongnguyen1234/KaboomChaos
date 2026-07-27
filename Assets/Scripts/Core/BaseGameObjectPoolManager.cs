using UnityEngine;
using System.Collections.Generic;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cung cấp logic pooling chung cho các GameObject.
    /// Xử lý việc khởi tạo, mở rộng pool, và quản lý các instance.
    /// Kế thừa từ MonoBehaviour để có thể sử dụng Coroutine và vòng đời của Unity.
    /// </summary>
    public abstract class BaseGameObjectPoolManager : MonoBehaviour, IGameObjectPoolManager
    {
        [Header("Base Pool Settings")]
        [Tooltip("Số lượng đối tượng mỗi loại được tạo sẵn trong pool khi có yêu cầu đầu tiên.")]
        [SerializeField] protected int _initialPoolSize = 10;
        [Tooltip("Số lượng đối tượng sẽ được tạo thêm mỗi khi pool hết và cần mở rộng. Đặt là 0 hoặc 1 để mở rộngทีละ một.")]
        [SerializeField] protected int _poolExpansionChunkSize = 5;

        // Dictionary chính của pool, map từ Prefab gốc sang một hàng đợi các instance không hoạt động.
        protected readonly Dictionary<GameObject, Queue<GameObject>> _poolDictionary = new();
        // Dictionary phụ để tra cứu nhanh prefab gốc từ một instance đang hoạt động.
        protected readonly Dictionary<GameObject, GameObject> _instanceToPrefabMap = new();
        
        private Transform _poolContainer;

        protected virtual void Awake()
        {
            // Tạo một container để chứa các object không hoạt động, giúp Hierarchy gọn gàng.
            _poolContainer = new GameObject($"{GetType().Name}_PoolContainer").transform;
            _poolContainer.SetParent(transform);
        }

        /// <summary>
        /// Lấy một đối tượng từ pool. Nếu pool không tồn tại hoặc đã hết, nó sẽ được tạo hoặc mở rộng.
        /// </summary>
        public virtual GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError($"[{GetType().Name}] Yêu cầu lấy đối tượng từ pool với prefab null.", this);
                return null;
            }

            // Nếu pool cho prefab này chưa tồn tại, hãy tạo nó.
            if (!_poolDictionary.ContainsKey(prefab))
            {
                CreateNewPool(prefab);
            }

            // Nếu pool hết, nới rộng nó ra.
            if (_poolDictionary[prefab].Count == 0)
            {
                ExpandPool(prefab, _poolExpansionChunkSize > 0 ? _poolExpansionChunkSize : 1);
            }

            GameObject instance = _poolDictionary[prefab].Dequeue();
            
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.SetParent(null); // Lấy ra khỏi container.
            instance.SetActive(true);

            // Gọi một phương thức ảo để các lớp con có thể thực hiện logic reset bổ sung.
            OnGetInstance(instance);

            return instance;
        }

        /// <summary>
        /// Trả một đối tượng về lại pool.
        /// </summary>
        public virtual void ReturnToPool(GameObject instance)
        {
            if (instance == null) return;

            if (_instanceToPrefabMap.TryGetValue(instance, out GameObject prefab) && _poolDictionary.TryGetValue(prefab, out var queue))
            {
                instance.SetActive(false);
                instance.transform.SetParent(_poolContainer);
                queue.Enqueue(instance);
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] Nhận được yêu cầu trả về pool một đối tượng không được theo dõi: '{instance.name}'. Đối tượng sẽ bị hủy.", instance);
                Destroy(instance);
            }
        }

        /// <summary>
        /// Được gọi ngay sau khi một instance được lấy ra từ pool và kích hoạt.
        /// Các lớp con có thể ghi đè để reset trạng thái của component.
        /// </summary>
        protected virtual void OnGetInstance(GameObject instance) { }

        private void CreateNewPool(GameObject prefab)
        {
            var queue = new Queue<GameObject>();
            _poolDictionary.Add(prefab, queue);
            ExpandPool(prefab, _initialPoolSize);
        }

        private void ExpandPool(GameObject prefab, int amount)
        {
            if (!_poolDictionary.TryGetValue(prefab, out var queue)) return;

            for (int i = 0; i < amount; i++)
            {
                GameObject instance = Instantiate(prefab, _poolContainer);
                instance.SetActive(false);
                queue.Enqueue(instance);
                _instanceToPrefabMap.Add(instance, prefab);
            }
        }
    }
}