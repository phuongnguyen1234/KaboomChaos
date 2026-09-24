using UnityEngine;
using Core;
using Core.Interfaces;
using Perks.Behaviors;

namespace Perks.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Perk Big Saver.
    /// Nhat coin giup hoi HP. Uu tien dung luong BonusHP da cau hinh san trong CoinData.
    /// </summary>
    [CreateAssetMenu(fileName = "BigSaverPerkData", menuName = "Kaboom Chaos/Perks/Big Saver")]
    public class BigSaverPerkData : BasePerkData
    {
        [Header("Thong So Big Saver")]
        [Tooltip("Luong HP hoi phuc fallback neu CoinData.BonusHP bang 0.")]
        [SerializeField] private float _fallbackBonusHp = 5f;
        public float FallbackBonusHp => _fallbackBonusHp;

        private BigSaverBehavior _behavior;

        public override IPerkBehavior Behavior => _behavior ??= new BigSaverBehavior(this);
    }
}