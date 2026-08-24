using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho hệ thống quản lý việc sinh (spawn) bom trong game.
    /// </summary>
    public interface IBombSpawnerManager
    {
        /// <summary>
        /// Thể hiện Singleton toàn cục của IBombSpawnerManager.
        /// </summary>
        static IBombSpawnerManager Instance { get; }

        /// <summary> Bắt đầu chu trình sinh bom. </summary>
        void StartSpawning();
        /// <summary> Dừng chu trình sinh bom. </summary>
        void StopSpawning();
        /// <summary> Dọn dẹp tất cả bom đang hoạt động. </summary>
        void ClearAllBombs();
        
        /// <summary>
        /// Đặt vị trí Y cho MẶT TRÊN của khu vực sinh bom.
        /// </summary>
        /// <param name="newTopY">Tọa độ Y mới cho mặt trên của khu vực sinh bom.</param>
        void SetSpawnAreaTopY(float newTopY);

        /// <summary>
        /// Lấy một đối tượng bom từ pool.
        /// </summary>
        /// <returns>Một instance của GameObject từ pool.</returns>
        GameObject GetBombFromPool(GameObject prefab, Vector3 position, Quaternion rotation);

        /// <summary>
        /// Lấy prefab tương ứng với một dữ liệu bom cụ thể.
        /// </summary>
        /// <param name="data">Dữ liệu bom cần tìm prefab.</param>
        /// <returns>Prefab của bom, hoặc null nếu không tìm thấy.</returns>
        GameObject GetPrefabForBombData(IBaseBombData data);

        /// <summary>
        /// Lấy prefab gốc của một instance bom đang hoạt động.
        /// </summary>
        /// <param name="instance">Instance của bom.</param>
        /// <returns>Prefab gốc, hoặc null nếu không tìm thấy.</returns>
        GameObject GetPrefabForInstance(GameObject instance);
    }
}