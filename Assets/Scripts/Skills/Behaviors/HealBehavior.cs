using UnityEngine;
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

        public HealBehavior(HealSkillData data)
        {
            _data = data;
        }

        public void Activate(IPlayer player)
        {
            if (player != null)
            {
                var healable = player.GameObject.GetComponent<IHealable>();
                if (healable != null)
                {
                    float healed = healable.Heal(_data.HealAmount);
                    Debug.Log($"[HealBehavior] Player {_data.DisplayName} da hoi phuc {healed} HP (Muc tieu: {_data.HealAmount} HP)");
                }
                else
                {
                    Debug.LogWarning("[HealBehavior] Player GameObject khong co component IHealable!");
                }
            }
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Hanh vi tuc thoi
        }

        public void Deactivate(IPlayer player)
        {
            Debug.Log("[HealBehavior] Skill Heal ket thuc.");
        }
    }
}