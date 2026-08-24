using UnityEngine;
using Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Collectibles.Data
{
    /// <summary>
    /// Định nghĩa một biến thể của vật phẩm và xác suất xuất hiện của nó.
    /// </summary>
    [System.Serializable]
    public class CollectibleVariant : ICollectibleVariant
    {
        [Tooltip("Dữ liệu của vật phẩm biến thể sẽ được sinh ra. Kéo ScriptableObject của biến thể vào đây.")]
        public BaseCollectibleData variantCollectibleData;
        public ICollectibleData VariantCollectibleData => variantCollectibleData;

        [Tooltip("Đường cong xác suất (0-100) mà biến thể này sẽ xuất hiện thay cho vật phẩm gốc, dựa trên độ khó.")]
        public AnimationCurve spawnChanceByDifficulty = AnimationCurve.Linear(1, 5, 6, 20);
        public AnimationCurve SpawnChanceByDifficulty => spawnChanceByDifficulty;
    }
    
    public abstract class BaseCollectibleData : ScriptableObject, ICollectibleData
    {
        [Header("Base Info")]
        [SerializeField] private string _id = "default-collectible";
        public string Id => _id;

        [SerializeField] private string _displayName = "Default Collectible";
        public string DisplayName => _displayName;

        [Header("Behavior")]
        [Tooltip("Time in seconds before the collectible disappears. 0 = infinite.")]
        [SerializeField] private float _lifespan = 15f;
        public float Lifespan => _lifespan;

        [Header("Effects")]
        [Tooltip("Âm thanh sẽ phát khi vật phẩm được thu thập.")]
        [SerializeField] private AudioClip _collectionSFX;
        public AudioClip CollectionSFX => _collectionSFX;
        [Tooltip("Hiệu ứng hình ảnh khi vật phẩm được thu thập.")]
        [SerializeField] private GameObject _collectionVFX;
        public GameObject CollectionVFX => _collectionVFX;
        [Tooltip("Màu của floating text.")]
        [SerializeField] private Color _floatingTextColor = Color.black;
        public Color FloatingTextColor => _floatingTextColor;

        [Header("Variants (Các biến thể)")]
        [Tooltip("Danh sách các biến thể có thể thay thế cho vật phẩm này khi được sinh ra.")]
        public List<CollectibleVariant> possibleVariants = new();
        public IReadOnlyList<ICollectibleVariant> PossibleVariants => possibleVariants.Cast<ICollectibleVariant>().ToList();

        [Header("Spawning")]
        [Tooltip("Chance (0-100) to spawn this collectible in a wave, based on difficulty. X-axis: Difficulty, Y-axis: Chance %")]
        [SerializeField] private AnimationCurve _rarityByDifficulty = AnimationCurve.Linear(1, 10, 6, 30);
        public AnimationCurve RarityByDifficulty => _rarityByDifficulty;
    }
}