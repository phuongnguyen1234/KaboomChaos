using UnityEngine;

namespace Bombs.Data
{
    /// <summary>
    /// Dữ liệu cho bom Matryoshka (búp bê Nga).
    /// Khi nổ, nó sẽ tạo ra một phiên bản nhỏ hơn của chính nó.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMatryoshkaBombData", menuName = "Kaboom Chaos/Bomb Types/Matryoshka Bomb")]
    public class MatryoshkaBombData : BombData // Kế thừa từ BombData vì nó là một dạng bom hẹn giờ
    {
        [Header("Hành vi Matryoshka")]
        [Tooltip("Tổng số lần nổ (số thế hệ). Ví dụ: 3 có nghĩa là bom gốc, một con, và một cháu.")]
        public int maxGenerations = 3;

        [Tooltip("Hệ số nhân kích thước (scale) cho thế hệ tiếp theo. Ví dụ: 0.7 là nhỏ hơn 30%.")]
        [Range(0.1f, 1f)]
        public float scaleMultiplier = 0.7f;

        [Tooltip("Hệ số nhân bán kính nổ cho thế hệ tiếp theo.")]
        [Range(0.1f, 1f)]
        public float radiusMultiplier = 0.7f;

        [Tooltip("Hệ số nhân sát thương cho thế hệ tiếp theo.")]
        [Range(0.1f, 1f)]
        public float damageMultiplier = 0.8f;

        [Tooltip("Hệ số nhân lực đẩy cho thế hệ tiếp theo.")]
        [Range(0.1f, 1f)]
        public float forceMultiplier = 0.7f;
    }
}