using UnityEngine;
using Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Collectibles
{
    /// <summary>
    /// Maps collectible data (ScriptableObject) to its prefab (GameObject).
    /// </summary>
    [System.Serializable]
    public class CollectibleMapping : ICollectibleMapping
    {
        [Tooltip("The ScriptableObject data for the collectible.")]
        [SerializeField] private Data.BaseCollectibleData _data;
        public ICollectibleData Data => _data;

        [Tooltip("The corresponding prefab for the collectible. This prefab must have a CollectibleController.")]
        [SerializeField] private GameObject _prefab;
        public GameObject Prefab => _prefab;
    }

    [CreateAssetMenu(fileName = "CollectibleDatabase", menuName = "Kaboom Chaos/Database/Collectible Database")]
    public class CollectibleDatabase : ScriptableObject, ICollectibleDatabase
    {
        [Tooltip("The list of mappings between data and prefabs for all spawnable collectibles.")]
        [SerializeField] private List<CollectibleMapping> _collectibleMappings = new();

        public IReadOnlyList<ICollectibleMapping> Mappings => _collectibleMappings.Cast<ICollectibleMapping>().ToList().AsReadOnly();
    }
}
