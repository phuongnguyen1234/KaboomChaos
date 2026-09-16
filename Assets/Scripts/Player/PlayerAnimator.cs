using UnityEngine;
using Core.Interfaces;
using Core;

namespace Player
{
    /// <summary>
    /// Điều khiển các tham số của Animator dựa trên trạng thái của người chơi.
    /// Đồng thời lắng nghe các sự kiện game để phản ứng với các thay đổi trạng thái (ví dụ: đóng băng).
    /// Lớp này hoạt động như một cầu nối giữa IPlayer và Animator Controller.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour, ISkillAnimationBridge
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

        // Skill-related Animator trigger/bool parameters
        private static readonly int SuperJumpTriggerHash = Animator.StringToHash("SuperJump");
        private static readonly int SummonTriggerHash = Animator.StringToHash("Summon");
        private static readonly int ChargeBoolHash = Animator.StringToHash("Charge");
        private static readonly int IsForcefieldOnBoolHash = Animator.StringToHash("IsForcefieldOn");

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

        private void OnEnable()
        {
            // Đăng ký lắng nghe sự kiện hiệu ứng trạng thái
            GameEvents.OnPlayerStatusEffectApplied += HandleStatusEffectApplied;
            GameEvents.OnPlayerStatusEffectReverted += HandleStatusEffectReverted;
        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh lỗi
            GameEvents.OnPlayerStatusEffectApplied -= HandleStatusEffectApplied;
            GameEvents.OnPlayerStatusEffectReverted -= HandleStatusEffectReverted;
        }

        private void Update()
        {
            if (_player == null) return;

            // Cập nhật các tham số cơ bản
            // Chỉ cập nhật nếu animator đang hoạt động (không bị đóng băng)
            if (_animator != null && _animator.speed > 0)
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

        /// <summary>
        /// Đặt tốc độ của Animator.
        /// Hữu ích để đóng băng hoặc làm chậm animation.
        /// </summary>
        /// <param name="speed">Tốc độ mới (1.0 là bình thường, 0.0 là đóng băng).</param>
        public void SetAnimationSpeed(float speed)
        {
            if (_animator != null) _animator.speed = speed;
        }

        /// <summary>
        /// Kich hoat trigger 'SuperJump' tren Animator khi player dung skill SuperJump.
        /// </summary>
        public void TriggerSuperJumpAnimation()
        {
            if (_animator != null) _animator.SetTrigger(SuperJumpTriggerHash);
        }

        /// <summary>
        /// Kich hoat trigger 'Summon' tren Animator khi player dung skill Platform, BubbleBarrier sau Heal.
        /// </summary>
        public void TriggerSummonAnimation()
        {
            if (_animator != null) _animator.SetTrigger(SummonTriggerHash);
        }

        /// <summary>
        /// Sat bool 'Charge' tren Animator: true in thoi gian duration cua skill Disarm, false khi ket thuc.
        /// </summary>
        public void SetDisarmCharging(bool charging)
        {
            if (_animator != null) _animator.SetBool(ChargeBoolHash, charging);
        }

        /// <summary>
        /// Sat bool 'IsForcefieldOn' tren Animator: true khi skill Forcefield dang hoat dong, false khi ket thuc.
        /// </summary>
        public void SetForcefieldActive(bool active)
        {
            if (_animator != null) _animator.SetBool(IsForcefieldOnBoolHash, active);
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

        #region Event Handlers

        /// <summary>
        /// Xử lý khi một hiệu ứng trạng thái được áp dụng lên người chơi.
        /// </summary>
        private void HandleStatusEffectApplied(IPlayer player, StatusEffectType effect)
        {
            // Chỉ phản hồi nếu sự kiện này dành cho chính người chơi này.
            if (player != _player) return;

            if (effect == StatusEffectType.Frozen)
                SetAnimationSpeed(0f);
        }

        /// <summary>
        /// Xử lý khi một hiệu ứng trạng thái trên người chơi được hoàn tác.
        /// </summary>
        private void HandleStatusEffectReverted(IPlayer player, StatusEffectType effect)
        {
            // Chỉ phản hồi nếu sự kiện này dành cho chính người chơi này.
            if (player != _player) return;

            if (effect == StatusEffectType.Frozen)
                SetAnimationSpeed(1f);
        }
        #endregion
    }
}