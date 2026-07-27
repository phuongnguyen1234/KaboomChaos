using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho một trình quản lý xử lý việc sinh và thu hồi các Hiệu ứng Hình ảnh (VFX).
    /// Thường được triển khai bởi một hệ thống pooling để tối ưu hóa hiệu năng.
    /// </summary>
    public interface IVFXManager
    {
        /// <summary>
        /// Sinh ra một prefab VFX từ pool tại một vị trí và góc quay được chỉ định.
        /// </summary>
        /// <returns>GameObject instance của VFX đã được sinh ra, hoặc null nếu thất bại.</returns>
        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation);
    }
}