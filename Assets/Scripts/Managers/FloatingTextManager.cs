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
        [Tooltip("Prefab của đối tượng text nổi. Prefab này phải có component FloatingTextController.")]
        [SerializeField] private GameObject _floatingTextPrefab;
        [Tooltip("Tag của container object bên trong đối tượng cha để chứa text. Nếu để trống hoặc không tìm thấy, text sẽ được gắn trực tiếp vào đối tượng cha.")]
        [SerializeField] private string _containerTag = "FloatingTextContainer";

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

        private void ShowFloatingText(Transform parent, Vector3 offset, string text, Color color)
        {
            if (_floatingTextPrefab == null) return;

            // Sử dụng phương thức GetFromPool của lớp cơ sở.
            GameObject textInstance = GetFromPool(_floatingTextPrefab, Vector3.zero, Quaternion.identity);
            if (textInstance == null) return;

            if (textInstance.TryGetComponent(out IFloatingTextController textController))
            {
                Transform textTransform = textController.GameObject.transform;
                Transform finalContainer = parent; // Mặc định là đối tượng gốc

                // Tìm một container được chỉ định bằng tag trong tất cả các đối tượng con (đệ quy).
                if (!string.IsNullOrEmpty(_containerTag) && parent != null)
                {
                    // Sử dụng GetComponentsInChildren để tìm kiếm trong toàn bộ cây con.
                    // Điều này có thể tốn một chút hiệu năng do cấp phát bộ nhớ, nhưng đảm bảo tìm được container dù nó nằm sâu đến đâu.
                    var allDescendants = parent.GetComponentsInChildren<Transform>(true); // true để tìm cả các object không active
                    foreach (var descendant in allDescendants)
                    {
                        // Bỏ qua chính đối tượng cha.
                        if (descendant == parent) continue;

                        if (descendant.CompareTag(_containerTag))
                        {
                            finalContainer = descendant;
                            break;
                        }
                    }
                }

                // Gắn text vào container và thiết lập vị trí.
                textTransform.SetParent(finalContainer);

                // Nếu container là một object con (không phải là đối tượng gốc), chúng ta cần tính toán lại vị trí local.
                // Điều này đảm bảo text xuất hiện ở đúng vị trí trong không gian thế giới, bất kể nó được gắn vào đâu.
                Vector3 finalLocalPosition = (finalContainer != parent && parent != null)
                    ? finalContainer.InverseTransformPoint(parent.TransformPoint(offset))
                    : offset;

                textTransform.SetLocalPositionAndRotation(finalLocalPosition, Quaternion.identity);
                textController.Trigger(text, color);
            }
            else
            {
                Debug.LogError($"Prefab '{_floatingTextPrefab.name}' does not have a component that implements IFloatingTextController. Returning to pool.", this);
                ReturnToPool(textInstance); // Trả lại pool nếu nó không hợp lệ.
            }
        }
    }
}
