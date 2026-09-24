namespace Core
{
    /// <summary>
    /// Dữ liệu theo dõi từng người chơi xuyên suốt các round, phục vụ việc tính điểm Score Card.
    /// Dữ liệu này được quản lý bởi <see cref="Core.Interfaces.IPlayerManager"/> và được cập nhật
    /// trong StartRound, AddPlayerToCurrentRound, HandlePlayerDeath và khi người chơi thắng round.
    /// </summary>
    public class PlayerRoundData
    {
        /// <summary>
        /// Chuỗi chiến thắng liên tiếp hiện tại của người chơi.
        /// Bắt đầu từ 0 và tăng lên 1 mỗi khi thắng một round; reset về 0 khi thua.
        /// </summary>
        public int WinStreak { get; set; }

        /// <summary>
        /// Cho biết người chơi có kích hoạt Extreme Mode trong round hay không.
        /// Nếu bật, Multiplier cơ bản sẽ được nâng từ x1.0 lên x1.25 (theo ScoreRules.md).
        /// </summary>
        public bool IsExtremeModeEnabled { get; set; }
    }
}