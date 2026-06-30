using UnityEngine;
using Core.Interfaces;

namespace Player
{
    /// <summary>
    /// Lớp Adapter để tích hợp AdvancedWalkerController từ package CMF vào hệ thống của game.
    /// Lớp này kế thừa AdvancedWalkerController, triển khai IPlayer và kết nối với InputSystem của dự án.
    /// </summary>
    [RequireComponent(typeof(Mover))]
    [RequireComponent(typeof(PlayerInputAdapter))] // Thay đổi RequireComponent
    public class PlayerController : AdvancedWalkerController, IPlayer
    {
        #region Fields
        [Header("Adapter Settings")]
        [Tooltip("Độ mượt khi xoay nhân vật theo hướng di chuyển ở góc nhìn thứ ba.")]
        [SerializeField] private float _rotationSmoothTime = 0.1f;

        private float _rotationVelocity;

        private CameraController _cameraController;
        private PlayerAnimator _playerAnimator; // Tham chiếu đến PlayerAnimator
        #endregion

        #region IPlayer Implementation

        /// <summary>
        /// Tham chiếu đến GameObject của người chơi.
        /// </summary>
        public new GameObject gameObject => base.gameObject;

        /// <summary>
        /// Tốc độ di chuyển hiện tại.
        /// </summary>
        public float CurrentSpeed => movementSpeed;

        /// <summary>
        /// Nhân vật đang di chuyển hay không.
        /// </summary>
        public bool IsMoving => GetMovementVelocity().magnitude > 0.1f;

        /// <summary>
        /// Nhân vật có đang ở trên mặt đất hay không.
        /// </summary>
        public new bool IsGrounded => base.IsGrounded();

        /// <summary>
        /// Vận tốc hiện tại theo trục Y.
        /// </summary>
        public float VerticalVelocity => GetVelocity().y;

        #endregion

        /// <summary>
        /// Ghi đè hàm Setup để lấy các component và thiết lập Input Actions.
        /// </summary>
        protected override void Setup()
        {
            base.Setup(); // Gọi hàm Setup của lớp cha

            // Gán cameraTransform từ CameraController để di chuyển theo hướng camera
            _cameraController = GetComponent<CameraController>();
            if (_cameraController != null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            
            // Lấy component PlayerAnimator
            _playerAnimator = GetComponent<PlayerAnimator>();
        }

        /// <summary>
        /// Ghi đè phương thức OnJumpStart để tùy chỉnh hành vi nhảy.
        /// 1. Vô hiệu hóa 'jumpInputIsLocked' để cho phép Bunny Hop (nhảy liên tục khi giữ phím).
        /// 2. Vô hiệu hóa 'jumpDuration' bằng cách chuyển trạng thái ngay lập tức, làm cho cú nhảy có chiều cao cố định.
        /// </summary>
        protected override void OnJumpStart()
        {
            // Gọi phương thức gốc để áp dụng lực nhảy và các sự kiện cơ bản.
            // Tuy nhiên, phương thức gốc sẽ khóa input nhảy, chúng ta sẽ không gọi nó.
            // base.OnJumpStart(); 

            // Tái triển khai logic của OnJumpStart nhưng bỏ qua phần khóa input.
            if (_useLocalMomentum)
                momentum = tr.localToWorldMatrix * momentum;

            momentum += tr.up * _jumpSpeed;

            OnJump?.Invoke(momentum);

            // Kích hoạt animation nhảy
            _playerAnimator?.TriggerJump();

            if (_useLocalMomentum)
                momentum = tr.worldToLocalMatrix * momentum;

            // Ngay lập tức chuyển sang trạng thái Rising để bỏ qua logic giữ nút nhảy.
            currentControllerState = ControllerState.Rising;
        }

        /// <summary>
        /// Ghi đè FixedUpdate của lớp cha để thêm logic xoay nhân vật.
        /// Luôn phải gọi base.FixedUpdate() để đảm bảo logic di chuyển và vật lý gốc được thực thi.
        /// </summary>
        protected override void FixedUpdate()
        {
            base.FixedUpdate(); // Rất quan trọng! Gọi hàm của lớp cha để xử lý di chuyển.
            HandleCharacterRotation();
        }

        /// <summary>
        /// Xử lý việc xoay nhân vật.
        /// - Ở chế độ First Person/Shift Lock: Xoay tức thì theo camera (logic này nằm trong CameraController).
        /// - Ở chế độ Third Person (mặc định): Xoay mượt mà theo hướng di chuyển.
        /// </summary>
        private void HandleCharacterRotation()
        {
            if (_cameraController == null) return;

            bool isShiftLocked = _cameraController.IsShiftLock;

            // Khi bật Shift Lock hoặc ở góc nhìn thứ nhất, nhân vật sẽ xoay theo camera.
            // Logic này được xử lý trong CameraController để đảm bảo thứ tự thực thi đúng.
            if (isShiftLocked || _cameraController.IsFirstPerson)
            {
                // Reset vận tốc xoay mượt để không bị giật khi chuyển về góc nhìn thứ 3.
                _rotationVelocity = 0f;
                // Xử lý xoay nhân vật theo camera đã được chuyển sang CameraController.
            }
            else
            {
                // Ở góc nhìn thứ 3 (không Shift Lock), xoay nhân vật mượt mà theo hướng di chuyển.
                Vector3 horizontalVelocity = GetMovementVelocity();
                horizontalVelocity.y = 0;

                // Chỉ xoay khi có di chuyển
                if (horizontalVelocity.magnitude < 0.1f) return;

                // Tính toán góc xoay dựa trên hướng di chuyển
                float targetAngle = Mathf.Atan2(horizontalVelocity.x, horizontalVelocity.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationVelocity, _rotationSmoothTime);
                
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }
    }
}