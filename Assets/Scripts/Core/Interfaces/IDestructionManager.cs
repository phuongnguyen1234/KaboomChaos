using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho trình quản lý hệ thống phá hủy kiến trúc.
    /// Chịu trách nhiệm "bake" đồ thị kết cấu và xử lý các sự kiện nổ.
    /// </summary>
    public interface IDestructionManager
    {
        /// <summary>
        /// Thể hiện Singleton toàn cục của IDestructionManager.
        /// </summary>
        static IDestructionManager Instance { get; }

        /// <summary>
        /// Xử lý một vụ nổ tại một vị trí, ảnh hưởng đến các mảnh vỡ trong bán kính.
        /// Đây là điểm khởi đầu của logic sụp đổ.
        /// </summary>
        void HandleExplosion(Vector3 position, float radius, float force);
    }
}