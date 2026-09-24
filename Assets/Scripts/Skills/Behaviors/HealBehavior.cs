using UnityEngine;
using Core;
using Core.Interfaces;
using Skills.Data;

namespace Skills.Behaviors
{
    /// <summary>
    /// Hanh vi thuc thi cua Skill Heal.
    /// </summary>
    public class HealBehavior : ISkillBehavior
    {
        private readonly HealSkillData _data;
        private float _appliedSlowMultiplier;
        private bool _isSlowApplied;

        public HealBehavior(HealSkillData data)
        {
            _data = data;
        }

        public void Activate(IPlayer player)
        {
            if (player == null || player.GameObject == null) return;

            // Kich hoat animation Summon tren player khi active skill.
            var animBridge = player.GameObject.GetComponent<ISkillAnimationBridge>();
            if (animBridge != null) animBridge.TriggerSummonAnimation();

            var healable = player.GameObject.GetComponent<IHealable>();
            if (healable != null)
            {
                float healed = healable.Heal(_data.HealAmount);
                if (healed > 0f)
                {
                    GameEvents.TriggerFloatingTextRequested(player.GameObject.transform, Vector3.up * 2.2f, $"+{Mathf.RoundToInt(healed)}", Color.green, player.HPTextContainer, false);
                }
                Debug.Log($"[HealBehavior] Player {_data.DisplayName} da hoi phuc {healed} HP (Muc tieu: {_data.HealAmount} HP)");
            }
            else
            {
                Debug.LogWarning("[HealBehavior] Player GameObject khong co component IHealable!");
            }

            // Ap dung hieu ung di cham (slow) cho player trong thoi gian duration (neu duration > 0 va slowMultiplier > 0)
            if (_data.Duration > 0f && _data.SlowMultiplier > 0f)
            {
                _appliedSlowMultiplier = _data.SlowMultiplier;
                player.ApplySpeedMultiplier(_appliedSlowMultiplier);
                _isSlowApplied = true;
                Debug.Log($"[HealBehavior] Player bi lam slow voi he so {_appliedSlowMultiplier} trong {_data.Duration}s.");
            }
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Logic thoi gian duration duoc PlayerSkillController quan ly.
        }

        public void Deactivate(IPlayer player)
        {
            if (_isSlowApplied && player != null)
            {
                player.RemoveSpeedMultiplier(_appliedSlowMultiplier);
                _isSlowApplied = false;
                Debug.Log("[HealBehavior] Go bo hieu ung slow, player khoi phuc toc do binh thuong.");
            }

            Debug.Log("[HealBehavior] Skill Heal ket thuc.");
        }
    }
}