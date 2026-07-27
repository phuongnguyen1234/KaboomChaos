using UnityEngine;
using Player; // Cần tham chiếu đến assembly của Player
using UnityEngine.InputSystem;

namespace Debugging
{
    /// <summary>
    /// Lớp test để kích hoạt ragdoll cho người chơi bằng một phím bấm.
    /// LƯU Ý KIẾN TRÚC: Lớp này được đặt trong một thư mục và namespace 'Debugging' riêng
    /// để không vi phạm quy tắc phụ thuộc của assembly 'Core'.
    /// </summary>
    public class RagdollTestTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Kéo Prefab hoặc GameObject của người chơi vào đây.")]
        [SerializeField] private RagdollController _playerRagdollController;

        [Tooltip("Hành động để bật/tắt ragdoll. Mặc định là phím 'R'.")]
        [SerializeField] private InputAction _ragdollAction = new(type: InputActionType.Button, binding: "<Keyboard>/r");

        private void OnEnable()
        {
            _ragdollAction.Enable();
        }

        private void OnDisable()
        {
            _ragdollAction.Disable();
        }

        private void Update()
        {
            // Tự động tìm controller nếu chưa được gán, hữu ích cho việc test nhanh.
            if (_playerRagdollController == null)
            {
                _playerRagdollController = FindAnyObjectByType<RagdollController>();
                if (_playerRagdollController == null) return; // Vẫn không tìm thấy, thoát.
            }

            // Khi ấn phím được chỉ định, bật/tắt trạng thái ragdoll.
            if (_ragdollAction.WasPressedThisFrame())
            {
                // Chuyển đổi trạng thái ragdoll.
                _playerRagdollController.SetRagdollState(!_playerRagdollController.IsRagdollActive);
            }
        }
    }
}