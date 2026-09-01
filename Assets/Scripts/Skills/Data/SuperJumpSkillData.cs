using UnityEngine;
using Core.Interfaces;

namespace Skills.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Skill Super Jump.
    /// </summary>
    [CreateAssetMenu(fileName = "SuperJumpSkillData", menuName = "Skills/Super Jump")]
    public class SuperJumpSkillData : BaseSkillData
    {
        [Header("Thong So Super Jump")]
        [Tooltip("Luc nhay giup player bay len.")]
        [SerializeField] private float _jumpForce = 25f;
        public float JumpForce => _jumpForce;

        private SuperJumpBehavior _behavior;

        public override ISkillBehavior Behavior => _behavior ??= new SuperJumpBehavior(this);

        private void OnEnable()
        {
            _type = SkillType.Movement;
        }
    }

    /// <summary>
    /// Hanh vi thuc thi cua Skill Super Jump.
    /// </summary>
    public class SuperJumpBehavior : ISkillBehavior
    {
        private readonly SuperJumpSkillData _data;

        public SuperJumpBehavior(SuperJumpSkillData data)
        {
            _data = data;
        }

        public void Activate(IPlayer player)
        {
            if (player != null)
            {
                // Ap dung luc bay len theo truc dung Y
                player.AddMomentum(Vector3.up * _data.JumpForce);
                Debug.Log($"[SuperJumpBehavior] Player {_data.DisplayName} da nhay cao voi luc: {_data.JumpForce}");
            }
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Skill tuc thoi, khong can tick logic trong duration
        }

        public void Deactivate(IPlayer player)
        {
            Debug.Log("[SuperJumpBehavior] Super Jump ket thuc.");
        }
    }
}
