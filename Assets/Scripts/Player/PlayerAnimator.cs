using UnityEngine;
using Core.Interfaces;

namespace Player
{
    /// <summary>
    /// Điều khiển các tham số của Animator dựa trên trạng thái của người chơi.
    /// Lớp này hoạt động như một cầu nối giữa IPlayer và Animator Controller.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        #region Fields

        private Animator _animator;
        private IPlayer _player; // Interface để lấy trạng thái của người chơi

        // Sử dụng StringToHash để tối ưu hiệu suất khi truy cập tham số Animator
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
        private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            
            // Lấy component triển khai IPlayer (có thể là PlayerController hoặc CMFPlayerAdapter)
            _player = GetComponent<IPlayer>();
            if (_player == null)
            {
                Debug.LogError("Không tìm thấy component nào triển khai IPlayer trên GameObject này!", this);
                enabled = false; // Vô hiệu hóa script nếu không có IPlayer
            }
        }

        private void Update()
        {
            if (_player == null) return;

            // Cập nhật các tham số cơ bản
            UpdateMovementParameters();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Cập nhật các tham số liên quan đến di chuyển, nhảy và rơi.
        /// </summary>
        private void UpdateMovementParameters()
        {
            _animator.SetBool(IsMovingHash, _player.IsMoving);
            _animator.SetBool(IsGroundedHash, _player.IsGrounded);
            
            // Lấy vận tốc theo trục Y từ interface IPlayer để xử lý animation rơi (Fall)
            _animator.SetFloat(VerticalVelocityHash, _player.VerticalVelocity);
        }

        /// <summary>
        /// Kích hoạt trigger cho animation nhảy.
        /// Phương thức này nên được gọi từ bên ngoài (ví dụ: từ PlayerController) khi nhân vật thực hiện nhảy.
        /// </summary>
        public void TriggerJump()
        {
            _animator.SetTrigger(JumpTriggerHash);
        }

        #endregion
    }
}