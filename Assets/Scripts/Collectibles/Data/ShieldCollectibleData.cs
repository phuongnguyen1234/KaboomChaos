using UnityEngine;
using Core.Interfaces;

namespace Collectibles.Data
{
    [CreateAssetMenu(fileName = "ShieldCollectible_New", menuName = "Kaboom Chaos/Collectibles/Shield Collectible")]
    public class ShieldCollectibleData : BaseCollectibleData
    {
        [Header("Shield Specifics")]
        [Tooltip("Dữ liệu cấu hình cho loại khiên này.")]
        [SerializeField] private BaseShieldData _shieldData;
        public IBaseShieldData ShieldData => _shieldData;
    }
}