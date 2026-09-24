using UnityEngine;

namespace Core.Utilities
{
    /// <summary>
    /// Tiện ích tính toán điểm theo quy tắc trong ScoreRules.md:
    /// - Survival Score: dựa trên phân khúc intensity (mỗi phân khúc cách nhau 15, tối đa 150).
    /// - Multiplier: x1.0 mặc định, x1.25 nếu bật Extreme Mode.
    /// - Win Multiplier: x1.0, từ lần thắng thứ 3 liên tiếp trở đi cộng thêm 0.05 mỗi lần thắng.
    /// - Total Credits = Survival Score x Multiplier x Win Multiplier.
    /// </summary>
    public static class ScoreCalculator
    {
        #region Constants

        /// <summary>
        /// Hệ số nhân cơ bản mặc định.
        /// </summary>
        public const float DefaultMultiplier = 1f;

        /// <summary>
        /// Hệ số nhân khi người chơi bật Extreme Mode.
        /// </summary>
        public const float ExtremeModeMultiplier = 1.25f;

        /// <summary>
        /// Điểm Survival tối thiểu (phân khúc intensity 1.0 - 1.99).
        /// </summary>
        public const float BaseSurvivalScore = 75f;

        /// <summary>
        /// Điểm Survival tối đa có thể đạt được (intensity 6.0 trở lên).
        /// </summary>
        public const float MaxSurvivalScore = 150f;

        /// <summary>
        /// Khoảng cách điểm giữa các phân khúc intensity liền kề.
        /// </summary>
        public const float ScorePerIntensitySegment = 15f;

        /// <summary>
        /// Ngưỡng thời gian (giây) tính theo ĐỒNG HỒ CỦA ROUND để người chơi thất bại được tính Survival Score.
        /// Quy tắc hiện tại: người thất bại phải sống sót TRÊN 30 giây theo đồng hồ round mới được tính điểm
        /// (điểm = tỷ lệ thời gian sống / tổng thời gian round) và được hiển thị Score Card cá nhân.
        /// Dưới 30 giây theo đồng hồ round: không đạt điểm, không hiển thị Score Card.
        /// </summary>
        public const float MinRoundSurvivalTimeForScore = 30f; // 30 giây theo đồng hồ round

        /// <summary>
        /// Mức cộng thêm cho Win Multiplier mỗi lần thắng liên tiếp sau chuỗi thắng thứ 3.
        /// </summary>
        public const float WinMultiplierStep = 0.05f;

        #endregion

        /// <summary>
        /// Tính điểm Survival tối đa ứng với phân khúc intensity hiện tại.
        /// Công thức: 75 + (segment - 1) * 15, với segment = floor(intensity), được clamp trong [75, 150].
        /// </summary>
        /// <param name="intensity">Độ khó hiện tại của round (CurrentIntensity).</param>
        /// <returns>Điểm Survival tối đa thuộc phân khúc của intensity đã cho.</returns>
        public static int GetMaxSurvivalScore(float intensity)
        {
            int segment = Mathf.FloorToInt(Mathf.Max(1f, intensity));
            float rawScore = BaseSurvivalScore + (segment - 1) * ScorePerIntensitySegment;
            return Mathf.RoundToInt(Mathf.Clamp(rawScore, BaseSurvivalScore, MaxSurvivalScore));
        }

        /// <summary>
        /// Tính Survival Score theo thời gian sống sót tính bằng ĐỒNG HỒ CỦA ROUND (không phải thời gian thực tế từng người chơi).
        /// - Người thất bại sống sót dưới 30 giây theo đồng hồ round: trả về 0 (không hiển thị Score Card).
        /// - Người thất bại sống TRÊN 30 giây: điểm = tỷ lệ (roundSurvivalTime / roundDuration) * điểm tối đa của round.
        /// Người thắng (sống hết round) tự động đạt 100% điểm tối đa (dùng GetMaxSurvivalScore).
        /// </summary>
        /// <param name="intensity">Độ khó của round.</param>
        /// <param name="roundSurvivalTime">Thời gian sống tính theo đồng hồ round (giây) tính tới thời điểm người chơi thua.</param>
        /// <param name="roundDuration">Tổng thời gian của round (giây).</param>
        /// <returns>Survival Score tính được.</returns>
        public static int GetSurvivalScore(float intensity, float roundSurvivalTime, float roundDuration)
        {
            if (roundSurvivalTime <= MinRoundSurvivalTimeForScore) return 0;

            int maxScore = GetMaxSurvivalScore(intensity);
            if (roundDuration <= 0f) return maxScore;

            float ratio = Mathf.Clamp01(roundSurvivalTime / roundDuration);
            return Mathf.RoundToInt(maxScore * ratio);
        }

        /// <summary>
        /// Hệ số nhân cơ bản dựa trên trạng thái Extreme Mode của người chơi.
        /// </summary>
        /// <param name="isExtremeModeEnabled">True nếu người chơi bật Extreme Mode.</param>
        /// <returns>x1.25 nếu bật Extreme Mode, ngược lại x1.0.</returns>
        public static float GetMultiplier(bool isExtremeModeEnabled)
        {
            return isExtremeModeEnabled ? ExtremeModeMultiplier : DefaultMultiplier;
        }

        /// <summary>
        /// Hệ số nhân (Win Multiplier) dựa trên chuỗi chiến thắng liên tiếp.
        /// x1.0 với 0-2 lần thắng; từ lần thắng thứ 3: 1.0 + (winStreak - 2) * 0.05.
        /// </summary>
        /// <param name="winStreak">Số lần thắng liên tiếp hiện tại.</param>
        /// <returns>Win Multiplier tương ứng.</returns>
        public static float GetWinMultiplier(int winStreak)
        {
            if (winStreak < 3) return DefaultMultiplier;
            return DefaultMultiplier + (winStreak - 2) * WinMultiplierStep;
        }

        /// <summary>
        /// Tính tổng credits: Survival Score x Multiplier x Win Multiplier.
        /// </summary>
        /// <param name="survivalScore">Điểm Survival đã tính.</param>
        /// <param name="baseMultiplier">Hệ số nhân cơ bản.</param>
        /// <param name="winMultiplier">Hệ số nhân thắng (win streak).</param>
        /// <returns>Tổng credits (được làm tròn về số nguyên).</returns>
        public static int GetTotalCredits(int survivalScore, float baseMultiplier, float winMultiplier)
        {
            return Mathf.RoundToInt(survivalScore * baseMultiplier * winMultiplier);
        }
    }
}