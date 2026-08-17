using System.Collections.Generic;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho database chứa tất cả các loại bom có trong game.
    /// </summary>
    public interface IBombDatabase
    {
        /// <summary>
        /// Lấy danh sách tất cả các dữ liệu bom.
        /// </summary>
        IReadOnlyList<IBaseBombData> Bombs { get; }
    }
}