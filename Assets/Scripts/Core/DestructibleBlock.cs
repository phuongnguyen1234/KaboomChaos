using UnityEngine;

namespace Core
{
    /// <summary>
    /// Gắn component này vào các khối địa hình có thể bị phá hủy.
    /// </summary>
    public class DestructibleBlock : MonoBehaviour
    {
        /// <summary>
        /// Độ cứng của khối. Bom có 'destructionPower' lớn hơn hoặc bằng giá trị này mới có thể phá hủy nó.
        /// </summary>
        [SerializeField]
        public int toughness = 1;

        /// <summary>
        /// Nhận một tác động từ một nguồn phá hủy (ví dụ: bom).
        /// LƯU Ý: Trong hệ thống hiện tại, logic phá hủy bom sẽ gọi trực tiếp WorldGridManager.DamageBlock
        /// để tra cứu và phá hủy khối một cách hiệu quả. Phương thức này được giữ lại để tương thích
        /// với các hệ thống sát thương khác không thông qua grid (ví dụ: một loại đạn đặc biệt).
        /// </summary>
        /// <param name="destructionPower">Sức mạnh phá hủy của tác động.</param>
        public void ReceiveImpact(int destructionPower)
        {
            if (destructionPower < toughness) return;
            Destroy(gameObject);
        }
    }
}