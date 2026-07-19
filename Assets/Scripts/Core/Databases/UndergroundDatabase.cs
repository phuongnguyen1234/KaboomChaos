using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// ScriptableObject chứa một danh sách các Underground Profiles.
    /// MapManager sẽ sử dụng database này để chọn một cấu hình và xây dựng thế giới ngầm.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUndergroundDatabase", menuName = "Kaboom Chaos/Database/Underground Database")]
    public class UndergroundDatabase : ScriptableObject
    {
        [Tooltip("Danh sách các profile để tạo thế giới ngầm. MapManager sẽ chọn một từ đây.")]
        [SerializeField] private List<UndergroundData> _undergroundDatas = new();

        /// <summary>
        /// Danh sách chỉ đọc của các profile thế giới ngầm.
        /// </summary>
        public IReadOnlyList<UndergroundData> UndergroundDatas => _undergroundDatas;
    }
}
