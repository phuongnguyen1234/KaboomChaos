namespace Core.Enums
{
    /// <summary>
    /// Các trạng thái của vòng lặp game.
    /// </summary>
    public enum GameState
    {
        /// <summary>Trạng thái không xác định hoặc khởi tạo.</summary>
        None,
        /// <summary>Giai doan nghi giua cac round / chon map tiep theo.</summary>
        Intermission,
        /// <summary>Giai đoạn map đang được xây dựng.</summary>
        Building,
        /// <summary>Giai đoạn chuẩn bị trước round đấu (dịch chuyển, đếm ngược).</summary>
        PreRound,
        /// <summary>Giai đoạn round đấu đang diễn ra.</summary>
        RoundActive,
        /// <summary>Giai đoạn kết thúc round đấu (hiển thị kết quả, dọn dẹp).</summary>
        PostRound
    }
}