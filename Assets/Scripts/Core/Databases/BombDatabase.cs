using System.Collections.Generic;
using UnityEngine;
using Core;

namespace Bombs
{
    /// <summary>
    /// ScriptableObject chứa một danh sách các đối tượng BombData.
    /// Các hệ thống như BombSpawner sẽ sử dụng database này để truy cập tất cả các loại bom có sẵn.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBombDatabase", menuName = "Kaboom Chaos/Database/Bomb Database")]
    public class BombDatabase : ScriptableObject
    {
        [Tooltip("Danh sách tất cả các loại bom có trong game.")]
        public List<BaseBombData> bombs = new();
    }
}