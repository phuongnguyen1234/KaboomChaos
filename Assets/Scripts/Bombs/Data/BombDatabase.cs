using System.Collections.Generic;
using System.Linq;
using Core.Interfaces;
using UnityEngine;

namespace Bombs.Data
{
    /// <summary>
    /// Ánh xạ giữa dữ liệu bom (ScriptableObject) và prefab (GameObject) của nó.
    /// </summary>
    [System.Serializable]
    public class BombMapping : IBombMapping
    {
        [Tooltip("Dữ liệu ScriptableObject của bom.")]
        [SerializeField] private BaseBombData _data;
        public IBaseBombData Data => _data;

        [Tooltip("Prefab tương ứng với dữ liệu bom. Prefab này phải có BombController.")]
        [SerializeField] private GameObject _prefab;
        public GameObject Prefab => _prefab;
    }

    /// <summary>
    /// ScriptableObject chứa một danh sách các đối tượng BombData.
    /// Các hệ thống như BombSpawner sẽ sử dụng database này để truy cập tất cả các loại bom có sẵn.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBombDatabase", menuName = "Kaboom Chaos/Database/Bomb Database")]
    public class BombDatabase : ScriptableObject, IBombDatabase
    {
        [Tooltip("Danh sách ánh xạ giữa dữ liệu và prefab cho tất cả các loại bom có thể được sinh ra.")]
        [SerializeField] private List<BombMapping> _bombMappings = new();

        // Triển khai interface IBombDatabase
        public IReadOnlyList<IBombMapping> Mappings => _bombMappings.Cast<IBombMapping>().ToList().AsReadOnly();
    }
}