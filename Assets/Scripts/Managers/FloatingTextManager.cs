using UnityEngine;
using System.Collections.Generic;
using Core; 
using Core.Interfaces;
using Core.Interfaces.UI;

namespace Managers
{
    /// <summary>
    /// Quản lý việc tạo và tái sử dụng các đối tượng text nổi (damage numbers, etc.) bằng object pool.
    /// Hệ thống hoạt động hoàn toàn dựa trên uGUI, đặt text làm con của Transform UI đích.
    /// </summary>
    public class FloatingTextManager : BaseGameObjectPoolManager
    {
        public static FloatingTextManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Prefab uGUI chữ nổi (chứa RectTransform và TextMeshProUGUI).")]
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
                DontDestroyOnLoad(gameObject);
                PrewarmPool();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnFloatingTextRequested += ShowFloatingText;
            GameEvents.OnFloatingTextDespawnRequest += ReturnToPool;
        }

        private void OnDisable()
        {
            GameEvents.OnFloatingTextRequested -= ShowFloatingText;
            GameEvents.OnFloatingTextDespawnRequest -= ReturnToPool;
        }

        private void PrewarmPool()
        {
            if (_floatingTextPrefab == null)
            {
                Debug.LogError("Floating Text Prefab is not assigned in FloatingTextManager.", this);
                return;
            }
            
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
            
            GameObject textInstance = GetFromPool(_floatingTextPrefab, Vector3.zero, Quaternion.identity);
            if (textInstance == null) return;
            
            if (textInstance.TryGetComponent(out IFloatingTextController textController))
            {
                RectTransform rectTransform = textController.GameObject.GetComponent<RectTransform>();
                
                // Thao tac uGUI: Lay Transform cha tren UI.
                // Uu tien: containerOverride -> UIManager container theo loai (Collectible neu showIcon, HP neu khong showIcon) -> container mac dinh -> parent goc
                RectTransform defaultContainer = showIcon
                    ? IUIManager.Instance?.CollectibleFloatingTextContainer
                    : IUIManager.Instance?.HpFloatingTextContainer;

                Transform targetParent = containerOverride != null
                    ? containerOverride
                    : (defaultContainer != null ? defaultContainer : (IUIManager.Instance?.FloatingTextContainer != null ? IUIManager.Instance.FloatingTextContainer : parent));

                // Đặt làm con của UI Transform, 'false' để giữ nguyên toạ độ cục bộ (tránh sai lệch scale của uGUI)
                rectTransform.SetParent(targetParent, false);
                
                // Đặt vị trí cục bộ dựa theo offset truyền vào
                rectTransform.localPosition = offset;

                // Kích hoạt animation chạy trên UI
                textController.Trigger(text, color, showIcon);
            }
            else
            {
                Debug.LogError($"Prefab '{_floatingTextPrefab.name}' không implement IFloatingTextController. Returning to pool.", this);
                ReturnToPool(textInstance);
            }
        }
    }
}
