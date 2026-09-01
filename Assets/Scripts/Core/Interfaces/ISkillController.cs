namespace Core.Interfaces
{
    /// <summary>
    /// Giao dien cho bo dieu khien Skill tren Player.
    /// </summary>
    public interface ISkillController
    {
        /// <summary>
        /// Skill hien tai dang duoc trang bi.
        /// </summary>
        ISkillData CurrentSkill { get; }

        /// <summary>
        /// Cho biet skill hien tai co dang duoc kich hoat hay khong.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Cho biet skill hien tai co dang trong thoi gian sac hay khong.
        /// </summary>
        bool IsRecharging { get; }

        /// <summary>
        /// Thoi gian hieu luc con lai cua skill.
        /// </summary>
        float DurationRemaining { get; }

        /// <summary>
        /// Thoi gian sac con lai cua skill.
        /// </summary>
        float RechargeRemaining { get; }

        /// <summary>
        /// Trang bi mot skill moi cho Player.
        /// </summary>
        /// <param name="skillData">Du lieu cua skill can trang bi.</param>
        void EquipSkill(ISkillData skillData);

        /// <summary>
        /// Yeu cau su dung/kich hoat skill hien tai.
        /// </summary>
        void UseActiveSkill();
    }
}
