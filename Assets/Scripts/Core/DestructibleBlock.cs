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
        private int _initialToughness = 1;

        /// <summary>
        /// Public property để các hệ thống khác có thể đọc và thay đổi độ cứng.
        /// </summary>
        public int Toughness { get; set; }

        private void Awake()
        {
            // Khởi tạo độ cứng runtime bằng giá trị ban đầu
            Toughness = _initialToughness;
        }

        /// <summary>
        /// Reset trạng thái của khối khi được lấy ra từ pool.
        /// </summary>
        public void ResetState()
        {
            Toughness = _initialToughness;
        }

        /// <summary>
        /// Nhận một tác động từ một nguồn phá hủy (ví dụ: bom).
        /// </summary>
        /// <param name="destructionPower">Sức mạnh phá hủy của tác động.</param>
        public void ReceiveImpact(int destructionPower)
        {
            if (destructionPower < Toughness) return;

            // Yêu cầu các component khác (như StatusEffectReceiver) dọn dẹp trước khi bị vô hiệu hóa.
            // Điều này ngăn chặn lỗi race condition khi un-parent các particle effect.
            if (TryGetComponent<StatusEffectReceiver>(out var receiver))
            {
                receiver.PrepareForDespawn();
            }

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