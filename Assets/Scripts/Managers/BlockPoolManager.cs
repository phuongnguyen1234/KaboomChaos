using UnityEngine;
using Core.Interfaces;
using Core; // Để có thể tham chiếu đến DestructibleBlock

namespace Managers
{
    /// <summary>
    /// Quản lý object pooling cho các khối địa hình (DestructibleBlock) để tối ưu hóa hiệu năng.
    /// </summary>
    public class BlockPoolManager : BaseGameObjectPoolManager, IBlockPoolManager
    {
        private static BlockPoolManager _instance;

        protected override void Awake()
        {
            base.Awake(); // Gọi Awake của lớp cơ sở
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        
        private void OnEnable()
        {
            GameEvents.OnBlockDespawnRequest += ReturnToPool;
            GameEvents.OnBlockSpawnRequest += GetFromPool;
        }

        private void OnDisable()
        {
            GameEvents.OnBlockDespawnRequest -= ReturnToPool;
            GameEvents.OnBlockSpawnRequest -= GetFromPool;
        }

        /// <summary>
        /// Ghi đè phương thức hook để reset trạng thái của khối khi nó được lấy ra từ pool.
        /// </summary>
        /// <param name="instance">Instance của khối vừa được lấy ra.</param>
        protected override void OnGetInstance(GameObject instance)
        {
            base.OnGetInstance(instance);
            // Reset trạng thái của khối để nó như mới
            if (instance.TryGetComponent<DestructibleBlock>(out var destructibleBlock))
            {
                destructibleBlock.ResetState();
            }
            // Yêu cầu 1: Reset lại hiệu ứng trạng thái của khối khi nó được lấy ra từ pool.
            if (instance.TryGetComponent<StatusEffectReceiver>(out var statusReceiver))
            {
                statusReceiver.ResetState();
            }
        }

    }
}