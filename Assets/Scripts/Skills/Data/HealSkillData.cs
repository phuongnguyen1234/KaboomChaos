using UnityEngine;
using Core.Interfaces;
using Skills.Behaviors;

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

        /// <summary>
        /// Khong cho phep su dung skill Heal khi player dang day mau
        /// (tranh tieu thu energy vo ich khi khong the hoi them HP).
        /// </summary>
        public override bool CanActivate(IPlayer player)
        {
            if (player == null || player.GameObject == null) return true;

            var healable = player.GameObject.GetComponent<IHealable>();
            if (healable == null) return true;

            return !healable.IsHealthFull;
        }
    }
}
