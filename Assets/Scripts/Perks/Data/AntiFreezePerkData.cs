using UnityEngine;
using Core;
using Core.Interfaces;

namespace Perks.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Perk Anti-Freeze.
    /// Player khong the bi dong bang. Moi lan tiep xuc voi vu no bang se tang Max HP va hoi 10 HP.
    /// Toan bo bien doi chi co hieu luc trong round; khi quay ve lobby Max HP tu reset ve gia tri ban dau (100).
    /// </summary>
    [CreateAssetMenu(fileName = "AntiFreezePerkData", menuName = "Kaboom Chaos/Perks/Anti Freeze")]
    public class AntiFreezePerkData : BasePerkData
    {
        [Header("Thong So Anti Freeze")]
        [Tooltip("Luong Max HP tang them moi lan player tiep xuc voi mot vu no bang.")]
        [SerializeField] private float _maxHealthBonus = 10f;
        public float MaxHealthBonus => _maxHealthBonus;

        [Tooltip("Luong HP duoc hoi (heal) moi lan player tiep xuc voi mot vu no bang.")]
        [SerializeField] private float _healBonus = 10f;
        public float HealBonus => _healBonus;

        private AntiFreezeBehavior _behavior;

        public override IPerkBehavior Behavior => _behavior ??= new AntiFreezeBehavior(this);
    }

    /// <summary>
    /// Hanh vi thuc thi cua Perk Anti-Freeze.
    /// Chan hieu ung dong bang huong vao player. Moi lan player trung dan vao vu no bang
    /// (khong phu thuoc khien hay trang thai bat tu) thi tang Max HP va hoi mau theo cau hinh.
    /// </summary>
    public class AntiFreezeBehavior : IPerkBehavior
    {
        private readonly AntiFreezePerkData _data;

        public AntiFreezeBehavior(AntiFreezePerkData data)
        {
            _data = data;
        }

        public void Apply(IPlayer player)
        {
            // Chan hieu ung dong bang (thay doi rule cua game): player khong the bi frozen.
            GameEvents.OnQueryPlayerStatusEffectBlocked += HandleStatusEffectBlockQuery;
            // Phat hien moi lan trung vu no bang (bat ke co khien hay bat tu) de tang Max HP va heal.
            GameEvents.OnPlayerExplosionHit += HandleExplosionHit;
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Xu ly qua event, khong can tick logic moi frame.
        }

        public void Remove(IPlayer player)
        {
            GameEvents.OnQueryPlayerStatusEffectBlocked -= HandleStatusEffectBlockQuery;
            GameEvents.OnPlayerExplosionHit -= HandleExplosionHit;
        }

        /// <summary>
        /// Tra ve true de chan moi hieu ung dong bang huong vao player (mien nhiem hoan toan).
        /// </summary>
        private bool HandleStatusEffectBlockQuery(IPlayer player, StatusEffectType effect)
        {
            return effect == StatusEffectType.Frozen;
        }

        /// <summary>
        /// Moi lan player trung dan vao vu no bang (khong phu thuoc khien/bat tu) thi tang Max HP va heal.
        /// Max HP chi co hieu luc trong round; PlayerHealth.ResetState se dua Max HP ve ban dau
        /// (100) khi ket thuc round/quay ve lobby.
        /// </summary>
        private void HandleExplosionHit(IPlayer player, StatusEffectType effectContext)
        {
            if (effectContext != StatusEffectType.Frozen) return;
            if (player == null || player.GameObject == null) return;

            var healable = player.GameObject.GetComponent<IHealable>();
            if (healable == null)
            {
                Debug.LogWarning("[AntiFreezeBehavior] Player GameObject khong co component IHealable!");
                return;
            }

            // Tang Max HP va heal 10 HP moi lan trung vu no bang, bat ke khien hay bat tu.
            healable.IncreaseMaxHealth(_data.MaxHealthBonus);
            healable.Heal(_data.HealBonus);
            Debug.Log($"[AntiFreezeBehavior] Trung vu no bang: +{_data.MaxHealthBonus} Max HP va hoi {_data.HealBonus} HP.");
        }
    }
}