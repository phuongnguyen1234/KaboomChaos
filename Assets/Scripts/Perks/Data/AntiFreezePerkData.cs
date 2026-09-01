using UnityEngine;
using Core;
using Core.Interfaces;

namespace Perks.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Perk Anti-Freeze.
    /// Mien nhiem dong bang. Neu nhan sat thuong bang thi +Max HP (chi co han trong round).
    /// </summary>
    [CreateAssetMenu(fileName = "AntiFreezePerkData", menuName = "Kaboom Chaos/Perks/Anti Freeze")]
    public class AntiFreezePerkData : BasePerkData
    {
        [Header("Thong So Anti Freeze")]
        [Tooltip("Luong Max HP tang them khi nhan sat thuong bang (chi co han trong round).")]
        [SerializeField] private float _maxHealthBonus = 10f;
        public float MaxHealthBonus => _maxHealthBonus;

        private AntiFreezeBehavior _behavior;

        public override IPerkBehavior Behavior => _behavior ??= new AntiFreezeBehavior(this);
    }

    /// <summary>
    /// Hanh vi thuc thi cua Perk Anti-Freeze.
    /// Chan hieu ung dong bang huong vao player va tang Max HP khi nhan sat thuong bang.
    /// </summary>
    public class AntiFreezeBehavior : IPerkBehavior
    {
        private readonly AntiFreezePerkData _data;

        // Chi tang Max HP mot lan moi round; reset khi round ket thuc.
        private bool _grantedThisRound;

        public AntiFreezeBehavior(AntiFreezePerkData data)
        {
            _data = data;
        }

        public void Apply(IPlayer player)
        {
            _grantedThisRound = false;

            // Chan hieu ung dong bang (thay doi rule cua game).
            GameEvents.OnQueryPlayerStatusEffectBlocked += HandleStatusEffectBlockQuery;
            // Phat hien sat thuong bang de tang Max HP.
            GameEvents.OnPlayerDamageTaken += HandleDamageTaken;
            // Reset trang thai bonus theo round.
            GameEvents.OnRoundEndPlayerReset += HandleRoundEnd;
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Xu ly qua event, khong can tick logic moi frame.
        }

        public void Remove(IPlayer player)
        {
            GameEvents.OnQueryPlayerStatusEffectBlocked -= HandleStatusEffectBlockQuery;
            GameEvents.OnPlayerDamageTaken -= HandleDamageTaken;
            GameEvents.OnRoundEndPlayerReset -= HandleRoundEnd;
        }

        /// <summary>
        /// Reset lai co tang Max HP moi khi bat dau round moi.
        /// </summary>
        private void HandleRoundEnd()
        {
            _grantedThisRound = false;
        }

        /// <summary>
        /// Tra ve true de chan moi hieu ung dong bang huong vao player (mien nhiem hoan toan).
        /// </summary>
        private bool HandleStatusEffectBlockQuery(IPlayer player, StatusEffectType effect)
        {
            return effect == StatusEffectType.Frozen;
        }

        /// <summary>
        /// Neu player nhan sat thuong bang (Frozen context) thi tang 10 Max HP, moi round moi lan.
        /// </summary>
        private void HandleDamageTaken(IPlayer player, float amount, DamageSourceType sourceType, StatusEffectType effectContext)
        {
            if (_grantedThisRound) return;
            if (effectContext != StatusEffectType.Frozen) return;
            if (player == null || player.GameObject == null) return;

            var healable = player.GameObject.GetComponent<IHealable>();
            if (healable == null)
            {
                Debug.LogWarning("[AntiFreezeBehavior] Player GameObject khong co component IHealable!");
                return;
            }

            healable.IncreaseMaxHealth(_data.MaxHealthBonus);
            _grantedThisRound = true;
            Debug.Log($"[AntiFreezeBehavior] Mien nhiem dong bang. Tang +{_data.MaxHealthBonus} Max HP (chi trong round).");
        }
    }
}