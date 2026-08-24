using UnityEngine;
using Core;
using Core.Interfaces;

namespace Managers
{
    /// <summary>
    /// Quản lý object pooling cho các hiệu ứng hình ảnh (VFX) để tối ưu hóa hiệu năng.
    /// Đây là một singleton, đảm bảo chỉ có một instance tồn tại trong suốt game.
    /// </summary>
    public class VFXPoolManager : BaseGameObjectPoolManager, IVFXManager
    {
        private static IVFXManager _instance;

        protected override void Awake()
        {
            base.Awake(); // Gọi Awake của lớp cơ sở để khởi tạo pool container
            if (_instance != null && _instance as MonoBehaviour != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
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
    }
}