using UnityEngine;
using UnityEngine.InputSystem;
using Core.Interfaces;

namespace Player
{
    /// <summary>
    /// Điều khiển di chuyển và nhảy của nhân vật người chơi.
    /// Sử dụng CharacterController và InputSystem để xử lý đầu vào.
    /// </summary>
    public class PlayerController : MonoBehaviour, IPlayer
    {
        #region Fields

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _acceleration = 10f;
        [SerializeField] private float _rotationSmoothTime = 0.1f;

        [Header("Jump Settings")]
        [SerializeField] private float _jumpHeight = 2f;
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField] private float _groundedOffset = 0.1f;

        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        private CharacterController _characterController;
        private Transform _mainCameraTransform;
        private CameraController _cameraController;

        // Input Action references
        private InputActionMap _playerActionMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;

        // Movement state
        private Vector2 _currentMoveInput;
        private Vector3 _currentVelocity;
        private float _verticalVelocity;
        private float _targetRotation;
        private float _rotationVelocity;
        private bool _isGrounded;

        #region Properties

        /// <summary>
        /// Tham chiếu đến GameObject của người chơi.
        /// </summary>
        public GameObject GO => gameObject;

        /// <summary>
        /// Tốc độ di chuyển hiện tại.
        /// </summary>
        public float CurrentSpeed => _moveSpeed;

        /// <summary>
        /// Nhân vật đang ở trên mặt đất hay không.
        /// </summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>
        /// Nhân vật đang di chuyển hay không.
        /// </summary>
        public bool IsMoving => _currentMoveInput.magnitude > 0.1f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _mainCameraTransform = Camera.main != null ? Camera.main.transform : null;
            _cameraController = GetComponent<CameraController>();
            SetupInputActions();
        }

        private void OnEnable()
        {
            _playerActionMap?.Enable();
        }

        private void OnDisable()
        {
            _playerActionMap?.Disable();
        }

        private void Update()
        {
            HandleGroundedState();
            HandleMovement();
            HandleJump();
            ApplyFinalMovement();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Thiết lập Input Actions từ InputActionAsset.
        /// Lấy action map "Player" và các action Move, Jump.
        /// </summary>
        private void SetupInputActions()
        {
            if (_inputActions == null)
            {
                Debug.LogError("InputActionAsset chưa được gán! Vui lòng gán InputSystem_Actions vào Inspector.", this);
                return;
            }

            _playerActionMap = _inputActions.FindActionMap("Basic", true);
            if (_playerActionMap == null)
            {
                Debug.LogError("Không tìm thấy action map 'Player' trong InputActionAsset!", this);
                return;
            }

            _moveAction = _playerActionMap.FindAction("Move", true);
            _jumpAction = _playerActionMap.FindAction("Jump", true);

        }

        /// <summary>
        /// Kiểm tra trạng thái grounded của nhân vật.
        /// Reset vertical velocity nếu đang grounded.
        /// </summary>
        private void HandleGroundedState()
        {
            _isGrounded = _characterController.isGrounded;

            if (_isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
        }

        /// <summary>
        /// Xử lý di chuyển: đọc input Move và tính toán hướng di chuyển.
        /// Hỗ trợ camera-relative movement cho 3rd person.
        /// </summary>
        private void HandleMovement()
        {
            _currentMoveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

            Vector3 moveDirection = GetCameraRelativeDirection(_currentMoveInput);

            if (_currentMoveInput.magnitude < 0.01f)
            {
                _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, _acceleration * Time.deltaTime);
            }
            else
            {
                Vector3 targetVelocity = moveDirection * CurrentSpeed;
                _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, _acceleration * Time.deltaTime);
            }

            RotateCharacter(moveDirection);
        }

        /// <summary>
        /// Chuyển đổi input Vector2 thành hướng di chuyển 3D dựa trên góc nhìn camera.
        /// </summary>
        private Vector3 GetCameraRelativeDirection(Vector2 moveInput)
        {
            if (_mainCameraTransform == null)
            {
                return new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            }

            Vector3 cameraForward = Vector3.ProjectOnPlane(_mainCameraTransform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(_mainCameraTransform.right, Vector3.up).normalized;

            return (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
        }

        /// <summary>
        /// Xoay nhân vật về hướng di chuyển hoặc theo góc nhìn camera (nếu đang ngắm).
        /// </summary>
        private void RotateCharacter(Vector3 moveDirection)
        {
            bool isAiming = _cameraController != null && (_cameraController.IsFirstPerson || _cameraController.IsShiftLock);

            if (isAiming && _mainCameraTransform != null)
            {
                // Xoay nhân vật chạy theo góc camera (giống game FPS) - xoay lập tức không có độ trễ
                float cameraYaw = _mainCameraTransform.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);
                
                // Reset vận tốc xoay để tránh giật khi chuyển về góc nhìn thứ 3
                _rotationVelocity = 0f;
            }
            else
            {
                // Di chuyển tự do ở góc nhìn thứ 3
                if (moveDirection == Vector3.zero) return;

                _targetRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;

                float rotation = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    _targetRotation,
                    ref _rotationVelocity,
                    _rotationSmoothTime
                );

                transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }
        }

        /// <summary>
        /// Xử lý nhảy: áp dụng vertical velocity khi người chơi nhấn Jump.
        /// Hỗ trợ bunny hop khi giữ phím nhảy.
        /// </summary>
        private void HandleJump()
        {
            bool isJumpPressed = _jumpAction?.IsPressed() == true;

            if (isJumpPressed && _isGrounded)
            {
                _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }

            _verticalVelocity += _gravity * Time.deltaTime;
        }

        /// <summary>
        /// Áp dụng chuyển động cuối cùng lên CharacterController.
        /// Kết hợp horizontal velocity + vertical velocity (gravity + jump).
        /// </summary>
        private void ApplyFinalMovement()
        {
            Vector3 finalVelocity = _currentVelocity;
            finalVelocity.y = _verticalVelocity;

            _characterController.Move(finalVelocity * Time.deltaTime);
        }

        #endregion

        #endregion
    }
}
