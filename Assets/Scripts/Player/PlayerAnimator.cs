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
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int ResetTriggerHash = Animator.StringToHash("Reset");
        private static readonly int IsClimbingHash = Animator.StringToHash("IsClimbing");
        private static readonly int ClimbingSpeedHash = Animator.StringToHash("ClimbingSpeed");

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

        #region Public Methods

        /// <summary>
        /// Kích hoạt trigger 'Reset' trên Animator.
        /// Hữu ích để buộc Animator thoát khỏi các trạng thái hiện tại (như leo trèo)
        /// và quay về trạng thái mặc định một cách sạch sẽ, ví dụ như khi đứng dậy từ ragdoll.
        /// </summary>
        public void TriggerReset()
        {
            if (_animator != null) _animator.SetTrigger(ResetTriggerHash);
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
            
            // Cập nhật tốc độ di chuyển để điều khiển tốc độ animation
            _animator.SetFloat(SpeedHash, _player.HorizontalSpeed);

            // Sử dụng cờ 'JustLanded' để kích hoạt trigger Reset một cách đáng tin cậy,
            // ngay cả khi trạng thái Grounded chỉ tồn tại trong một khoảnh khắc.
            if (_player.JustLanded) {
                _animator.SetTrigger(ResetTriggerHash);
            }

            // Cập nhật các tham số liên quan đến leo trèo
            _animator.SetBool(IsClimbingHash, _player.IsClimbing);
            _animator.SetFloat(ClimbingSpeedHash, _player.ClimbingSpeed);
        }

        #endregion
    }
}