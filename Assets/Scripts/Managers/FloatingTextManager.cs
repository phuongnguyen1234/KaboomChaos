using UnityEngine;
using System.Collections.Generic;
using Core; // Namespace chứa GameEvents của bạn
using Core.Interfaces;

namespace Managers
{
    /// <summary>
    /// Quản lý việc tạo và tái sử dụng các đối tượng text nổi (damage numbers, etc.) bằng object pool.
    /// Lớp này kế thừa BaseGameObjectPoolManager để có thể được quản lý bởi các hệ thống chung,
    /// mặc dù phương thức hoạt động chính của nó là thông qua hệ thống GameEvents.
    /// </summary>
    public class FloatingTextManager : BaseGameObjectPoolManager
    {
        public static FloatingTextManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private GameObject _floatingTextPrefab;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                PrewarmPool();
            }
        }

        private void OnEnable()
        {
            // Lắng nghe các yêu cầu hiển thị và thu hồi text
            GameEvents.OnFloatingTextRequested += ShowFloatingText;
            GameEvents.OnFloatingTextDespawnRequest += ReturnToPool;
        }

        private void OnDisable()
        {
            GameEvents.OnFloatingTextRequested -= ShowFloatingText;
            GameEvents.OnFloatingTextDespawnRequest -= ReturnToPool;
        }

        /// <summary>
        /// Khởi tạo và làm đầy sẵn pool với các đối tượng text nổi.
        /// </summary>
        private void PrewarmPool()
        {
            if (_floatingTextPrefab == null)
            {
                Debug.LogError("Floating Text Prefab is not assigned in FloatingTextManager.", this);
                return;
            }
            
            // Lấy ra và trả lại ngay lập tức để khởi tạo pool với số lượng ban đầu.
            var instances = new List<GameObject>();
            for (int i = 0; i < _initialPoolSize; i++)
            {
                instances.Add(GetFromPool(_floatingTextPrefab, Vector3.zero, Quaternion.identity));
            }
            foreach (var instance in instances)
            {
                ReturnToPool(instance);
            }
        }

        private void ShowFloatingText(Transform parent, Vector3 offset, string text, Color color, Transform containerOverride = null, bool showIcon = false)
        {
            if (_floatingTextPrefab == null) return;

            // Sử dụng phương thức GetFromPool của lớp cơ sở.
            GameObject textInstance = GetFromPool(_floatingTextPrefab, Vector3.zero, Quaternion.identity);
            if (textInstance == null) return;
            
            if (textInstance.TryGetComponent(out IFloatingTextController textController))
            {
                Transform textTransform = textController.GameObject.transform;
                Transform targetParent = containerOverride != null ? containerOverride : parent;

                textTransform.SetParent(targetParent);
                textTransform.SetLocalPositionAndRotation(offset, Quaternion.identity);
                textController.Trigger(text, color, containerOverride, showIcon);
            }
            else
            {
                Debug.LogError($"Prefab '{_floatingTextPrefab.name}' does not have a component that implements IFloatingTextController. Returning to pool.", this);
                ReturnToPool(textInstance); // Trả lại pool nếu nó không hợp lệ.
            }
        }
    }
}
