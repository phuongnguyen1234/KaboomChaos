using UnityEngine;
using Core.Interfaces;

namespace Skills.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Skill Super Dash.
    /// </summary>
    [CreateAssetMenu(fileName = "SuperDashSkillData", menuName = "Skills/Super Dash")]
    public class SuperDashSkillData : BaseSkillData
    {
        [Header("Thong So Super Dash")]
        [Tooltip("He so nhan toc do chay cua player trong thoi gian dash.")]
        [SerializeField] private float _speedMultiplier = 2.0f;
        public float SpeedMultiplier => _speedMultiplier;

        private SuperDashBehavior _behavior;

        public override ISkillBehavior Behavior => _behavior ??= new SuperDashBehavior(this);

        private void OnEnable()
        {
            _type = SkillType.Movement;
        }
    }

    /// <summary>
    /// Hanh vi thuc thi cua Skill Super Dash.
    /// </summary>
    public class SuperDashBehavior : ISkillBehavior
    {
        private readonly SuperDashSkillData _data;

        public SuperDashBehavior(SuperDashSkillData data)
        {
            _data = data;
        }

        public void Activate(IPlayer player)
        {
            if (player != null)
            {
                // Nhan toc do di chuyen cua player len
                player.ApplySpeedMultiplier(_data.SpeedMultiplier);
                Debug.Log($"[SuperDashBehavior] Tang toc do player len x{_data.SpeedMultiplier}");
            }
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Toc do tu dong duoc duy tri thong qua speed multiplier, khong can tick logic moi frame
        }

        public void Deactivate(IPlayer player)
        {
            if (player != null)
            {
                // Reset lai he so toc do
                player.RemoveSpeedMultiplier(_data.SpeedMultiplier);
                Debug.Log($"[SuperDashBehavior] Reset toc do player tro lai binh thuong");
            }
        }
    }
}
