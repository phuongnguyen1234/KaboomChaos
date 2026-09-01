using UnityEngine;
using Core.Interfaces;

namespace Skills.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Skill Heal.
    /// </summary>
    [CreateAssetMenu(fileName = "HealSkillData", menuName = "Skills/Heal")]
    public class HealSkillData : BaseSkillData
    {
        [Header("Thong So Heal")]
        [Tooltip("Luong HP se hoi phuc khi su dung skill.")]
        [SerializeField] private float _healAmount = 25f;
        public float HealAmount => _healAmount;

        private HealBehavior _behavior;

        public override ISkillBehavior Behavior => _behavior ??= new HealBehavior(this);

        private void OnEnable()
        {
            _type = SkillType.Defensive;
        }
    }

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
