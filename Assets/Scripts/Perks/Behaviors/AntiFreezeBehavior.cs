using UnityEngine;
using Core;
using Core.Interfaces;
using Perks.Data;

namespace Perks.Behaviors
{
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
            float healed = healable.Heal(_data.HealBonus);
            if (healed > 0f)
            {
                GameEvents.TriggerFloatingTextRequested(player.GameObject.transform, Vector3.up * 2.2f, $"+{Mathf.RoundToInt(healed)}", Color.green, player.HPTextContainer, false);
            }
            Debug.Log($"[AntiFreezeBehavior] Trung vu no bang: +{_data.MaxHealthBonus} Max HP va hoi {_data.HealBonus} HP.");
        }
    }
}

