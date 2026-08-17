using UnityEngine;

namespace Bombs.Data
{
    /// <summary>
    /// Dữ liệu cho loại bom chùm.
    /// Khi nổ, nó sẽ giải phóng ra nhiều quả bom con.
    /// </summary>
    [CreateAssetMenu(fileName = "NewClusterBombData", menuName = "Kaboom Chaos/Bomb Types/Cluster Bomb")]
    public class ClusterBombData : MissileBombData
    {
        [Header("Hành vi bom chùm")]
        [Tooltip("Dữ liệu bom cho các quả bom con sẽ được sinh ra.")]
        public BaseBombData submunitionBombData;

        [Tooltip("Số lượng bom con tối thiểu sẽ sinh ra.")]
        public int minSubmunitions = 3;

        [Tooltip("Số lượng bom con tối đa sẽ sinh ra.")]
        public int maxSubmunitions = 5;

        [Tooltip("Lực dùng để đẩy các quả bom con ra từ điểm nổ.")]
        public float ejectionForce = 200f;

        [Tooltip("Bán kính phân tán khi sinh ra các quả bom con.")]
        public float ejectionSpreadRadius = 1f;
    }
}