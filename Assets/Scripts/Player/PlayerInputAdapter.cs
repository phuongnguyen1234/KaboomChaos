using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Lớp này kế thừa từ CharacterInput của package CMF để làm cầu nối với InputSystem của dự án.
    /// Nó đọc các giá trị từ InputActionAsset và cung cấp cho các controller như AdvancedWalkerController.
    /// </summary>
    public class PlayerInputAdapter : CharacterInput
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionAsset _inputActions;

        private InputActionMap _basicActionMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;

        private void Awake()
        {
            // Thiết lập Input Actions
            if (_inputActions == null)
            {
                Debug.LogError("InputActionAsset chưa được gán!", this);
                return;
            }

            _basicActionMap = _inputActions.FindActionMap("Basic", true);
            _moveAction = _basicActionMap.FindAction("Move", true);
            _jumpAction = _basicActionMap.FindAction("Jump", true);
        }

        private void OnEnable()
        {
            _basicActionMap?.Enable();
        }

        private void OnDisable()
        {
            _basicActionMap?.Disable();
        }

        public override float GetHorizontalMovementInput()
        {
            return _moveAction?.ReadValue<Vector2>().x ?? 0f;
        }

        public override float GetVerticalMovementInput()
        {
            return _moveAction?.ReadValue<Vector2>().y ?? 0f;
        }

        public override bool IsJumpKeyPressed()
        {
            return _jumpAction?.IsPressed() ?? false;
        }
    }
}