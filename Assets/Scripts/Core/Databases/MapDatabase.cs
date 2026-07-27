using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// ScriptableObject chứa một danh sách các đối tượng MapData.
    /// MapManager sẽ sử dụng database này để chọn một map và xây dựng.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMapDatabase", menuName = "Kaboom Chaos/Database/Map Database")]
    public class MapDatabase : ScriptableObject
    {
        [Tooltip("Danh sách các đối tượng MapData. MapManager sẽ chọn một từ đây để tạo.")]
        public List<MapData> maps = new();
    }
}
