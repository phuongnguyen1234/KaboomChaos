using System.Collections.Generic;
using System.Linq;
using Core.Interfaces;
using UnityEngine;

namespace Bombs.Data
{
    /// <summary>
    /// ScriptableObject chứa một danh sách các đối tượng BombData.
    /// Các hệ thống như BombSpawner sẽ sử dụng database này để truy cập tất cả các loại bom có sẵn.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBombDatabase", menuName = "Kaboom Chaos/Database/Bomb Database")]
    public class BombDatabase : ScriptableObject, IBombDatabase
    {
        [Tooltip("Danh sách tất cả các loại bom có thể được sinh ra.")]
        public List<BaseBombData> bombs = new();

        // Triển khai interface IBombDatabase
        IReadOnlyList<IBaseBombData> IBombDatabase.Bombs => bombs.Cast<IBaseBombData>().ToList().AsReadOnly();
    }
}