namespace Core.Interfaces.UI
{
    /// <summary>
    /// Interface cho Score Card (bảng điểm hiển thị kết quả sau khi kết thúc round).
    /// Được dùng để hiển thị chi tiết điểm (Survival Score, Multiplier, Win Multiplier, Total Credits)
    /// và trạng thái thắng/thua cho người chơi.
    /// </summary>
    public interface IScoreCard
    {
        /// <summary>
        /// Hiển thị Score Card với dữ liệu đã được tính toán.
        /// Score Card sẽ tự động ẩn sau một khoảng thời gian cấu hình (mặc định 5 giây).
        /// </summary>
        /// <param name="data">Dữ liệu điểm và kết quả thắng/thua cần hiển thị.</param>
        void Show(Core.ScoreCardData data);

        /// <summary>
        /// Ẩn Score Card ngay lập tức.
        /// </summary>
        void Hide();

        /// <summary>
        /// Trạng thái hiển thị hiện tại của Score Card.
        /// </summary>
        bool IsVisible { get; }
    }
}