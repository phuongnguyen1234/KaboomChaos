using System.Collections;
namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho hệ thống quản lý việc tải và dọn dẹp map.
    /// </summary>
    public interface IMapManager
    {
        float MapTopY {get;}

        /// <summary>
        /// Tải một map hoàn chỉnh dựa trên các chỉ số từ database.
        /// </summary>
        /// <param name="mapIndex">Chỉ số của map trong MapDatabase.</param>
        /// <param name="undergroundIndex">Chỉ số của underground profile trong UndergroundDatabase.</param>
        IEnumerator LoadMapByIndexAsync(int mapIndex, int undergroundIndex);

        /// <summary>
        /// Dọn dẹp tất cả các đối tượng map đã được tạo một cách bất đồng bộ.
        /// </summary>
        IEnumerator ClearCurrentMapAsync();

        /// <summary>
        /// Don dep ngay lap tuc tat ca cac doi tuong map (dung khi thoat ve Home).
        /// </summary>
        void ClearCurrentMap();

        /// <summary>
        /// Lấy tổng số map có sẵn trong database.
        /// </summary>
        /// <returns>Số lượng map.</returns>
        int GetMapCount();

        /// <summary>
        /// Lấy tổng số cấu hình underground có sẵn trong database.
        /// </summary>
        /// <returns>Số lượng cấu hình underground.</returns>
        int GetUndergroundDataCount();
    }
}