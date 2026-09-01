namespace Core.Interfaces
{
    /// <summary>
    /// Giao dien cho hanh vi thuc thi cua mot Skill (Strategy Pattern).
    /// </summary>
    public interface ISkillBehavior
    {
        /// <summary>
        /// Kich hoat skill tren nguoi choi.
        /// </summary>
        /// <param name="player">Nguoi choi kich hoat skill.</param>
        void Activate(IPlayer player);

        /// <summary>
        /// Cap nhat logic skill moi frame trong thoi gian hieu luc.
        /// </summary>
        /// <param name="player">Nguoi choi dang duy tri skill.</param>
        /// <param name="deltaTime">Thoi gian troi qua giua cac frame.</param>
        void UpdateBehavior(IPlayer player, float deltaTime);

        /// <summary>
        /// Huy kich hoat va don dep trang thai skill.
        /// </summary>
        /// <param name="player">Nguoi choi het hieu luc skill.</param>
        void Deactivate(IPlayer player);
    }
}
