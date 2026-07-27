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
        private int toughness = 1;

        /// <summary>
        /// Reset trạng thái của khối khi được lấy ra từ pool.
        /// </summary>
        public void ResetState()
        {
            // Hiện tại không có trạng thái nào cần reset (như máu),
            // nhưng phương thức này cần tồn tại để tương thích với pool manager.
        }

        /// <summary>
        /// Nhận một tác động từ một nguồn phá hủy (ví dụ: bom).
        /// </summary>
        /// <param name="destructionPower">Sức mạnh phá hủy của tác động.</param>
        public void ReceiveImpact(int destructionPower)
        {
            if (destructionPower < toughness) return;

            // Gửi yêu cầu trả về pool thông qua hệ thống event.
            // Điều này giúp DestructibleBlock không cần biết về sự tồn tại của BlockPoolManager.
            if (GameEvents.IsBlockPoolListening())
            {
                GameEvents.TriggerBlockDespawnRequest(gameObject);
            }
            else
            {
                // Fallback: Nếu không có pool manager nào đang lắng nghe, chỉ vô hiệu hóa đối tượng.
                Debug.LogWarning($"Không tìm thấy BlockPoolManager đang lắng nghe. Vô hiệu hóa khối '{gameObject.name}' thay vì trả về pool.", this);
                gameObject.SetActive(false);
            }
        }
    }
}