using System.Collections.Generic;

namespace Core.Interfaces
{
    /// <summary>
    /// Giao dien cho database chua danh sach tat ca Perk co trong game.
    /// </summary>
    public interface IPerkDatabase
    {
        /// <summary>
        /// Danh sach read-only toan bo cac Perk co trong game.
        /// </summary>
        IReadOnlyList<IPerkData> AllPerks { get; }

        /// <summary>
        /// Tim kiem Perk theo ID.
        /// </summary>
        /// <param name="id">ID cua Perk can tim.</param>
        /// <returns>IPerkData neu tim thay, null neu khong co.</returns>
        IPerkData GetById(string id);
    }
}