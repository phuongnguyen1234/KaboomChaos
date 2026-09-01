using UnityEngine;
using Core;
using Core.Interfaces;

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

    /// <summary>
    /// Hanh vi thuc thi cua Perk Big Saver.
    /// Lang nghe su kien nhat coin de hoi HP cho player.
    /// </summary>
    public class BigSaverBehavior : IPerkBehavior
    {
        private readonly BigSaverPerkData _data;

        public BigSaverBehavior(BigSaverPerkData data)
        {
            _data = data;
        }

        public void Apply(IPlayer player)
        {
            GameEvents.OnPlayerCoinCollected += HandleCoinCollected;
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Xu ly qua event, khong can tick logic moi frame.
        }

        public void Remove(IPlayer player)
        {
            GameEvents.OnPlayerCoinCollected -= HandleCoinCollected;
        }

        /// <summary>
        /// Khi player nhat coin: hoi HP theo BonusHP cua coin do (fallback neu cau hinh bang 0).
        /// </summary>
        private void HandleCoinCollected(IPlayer player, float bonusHp)
        {
            if (player == null || player.GameObject == null) return;

            float healAmount = bonusHp > 0f ? bonusHp : _data.FallbackBonusHp;
            if (healAmount <= 0f) return; // Khong co luong hoi -> khong lam gi them.

            var healable = player.GameObject.GetComponent<IHealable>();
            if (healable == null)
            {
                Debug.LogWarning("[BigSaverBehavior] Player GameObject khong co component IHealable!");
                return;
            }

            healable.Heal(healAmount);
        }
    }
}