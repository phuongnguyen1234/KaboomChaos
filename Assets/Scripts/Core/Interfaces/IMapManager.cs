namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho hệ thống quản lý việc tải và dọn dẹp map.
    /// </summary>
    public interface IMapManager
    {
        /// <summary>
        /// Tải một map hoàn chỉnh dựa trên các chỉ số từ database.
        /// </summary>
        /// <param name="mapIndex">Chỉ số của map trong MapDatabase.</param>
        /// <param name="undergroundIndex">Chỉ số của underground profile trong UndergroundDatabase.</param>
        void LoadMapByIndex(int mapIndex, int undergroundIndex);

        /// <summary>
        /// Dọn dẹp tất cả các đối tượng map đã được tạo (map, underground, lava).
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
        int GetUndergroundProfileCount();
    }
}