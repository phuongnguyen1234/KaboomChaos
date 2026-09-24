using UnityEngine;

namespace Collectibles.Data
{
    [CreateAssetMenu(fileName = "Collectible_Healing", menuName = "Kaboom Chaos/Collectibles/Healing")]
    public class HealingCollectibleData : BaseCollectibleData
    {
        [Header("Healing Properties")]
        [Tooltip("Lượng máu hồi phục. Phải là bội số của 5.")]
        [SerializeField] private int _healAmount = 25;
        public int HealAmount => _healAmount;

        [Tooltip("Lượng máu tối đa cộng thêm. Phải là bội số của 5.")]
        [SerializeField] private int _bonusMaxHPAmount = 0;
        public int BonusMaxHPAmount => _bonusMaxHPAmount;

        
    }
}