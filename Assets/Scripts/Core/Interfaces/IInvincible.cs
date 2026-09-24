namespace Core.Interfaces
{
    /// <summary>
    /// Giao dien cho cac doi tuong co the duoc bat/tat trang thai bat tu (invincible).
    /// Duoc su dung boi skill Forcefield de lam player khong nhan sat thuong trong thoi gian hieu luc.

    /// </summary>
    public interface IInvincible
    {
        /// <summary>
        /// Cho biet doi tuong co dang bat tu hay khong.

        /// </summary>
        bool IsInvincible { get; set; }
    }
}