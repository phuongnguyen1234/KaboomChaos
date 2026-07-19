using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface chung cho các hệ thống quản lý object pool của GameObject.
    /// Điều này giúp thống nhất cách các pool manager hoạt động và cho phép chúng có thể thay thế cho nhau.
    /// </summary>
    public interface IGameObjectPoolManager
    {
        /// <summary>
        /// Lấy một đối tượng từ pool dựa trên prefab của nó.
        /// </summary>
        /// <returns>Một instance của GameObject từ pool.</returns>
        GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation);

        /// <summary>
        /// Trả một đối tượng về lại pool để tái sử dụng.
        /// </summary>
        void ReturnToPool(GameObject instance);
    }
}