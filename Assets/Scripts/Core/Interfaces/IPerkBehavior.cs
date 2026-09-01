namespace Core.Interfaces
{
    /// <summary>
    /// Giao dien cho hanh vi thuc thi cua mot Perk (Strategy Pattern).
    /// Khac voi ISkillBehavior: Perk la hieu ung noi tai luon hoat dong,
    /// Apply kich hoat khi trang bi, UpdateBehavior tick moi frame,
    /// Remove go bo khi unequip hoac player bi huy.
    /// </summary>
    public interface IPerkBehavior
    {
        /// <summary>
        /// Kich hoat hieu ung noi tai cua perk len player (goi khi trang bi perk).
        /// </summary>
        /// <param name="player">Player dang trang bi perk.</param>
        void Apply(IPlayer player);

        /// <summary>
        /// Cap nhat logic perk moi frame khi perk dang duoc trang bi.
        /// </summary>
        /// <param name="player">Player dang trang bi perk.</param>
        /// <param name="deltaTime">Thoi gian troi qua giua cac frame.</param>
        void UpdateBehavior(IPlayer player, float deltaTime);

        /// <summary>
        /// Go bo hieu ung noi tai cua perk va don dep trang thai (goi khi unequip
        /// hoac PlayerPerkController bi huy).
        /// </summary>
        /// <param name="player">Player dang go trang bi perk.</param>
        void Remove(IPlayer player);
    }
}