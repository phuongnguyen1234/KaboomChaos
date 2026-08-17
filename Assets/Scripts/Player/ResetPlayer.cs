using UnityEngine;
using Player;
using UnityEngine.InputSystem;

namespace Debugging
{
    /// <summary>
    /// Lớp test để reset người chơi bằng một phím bấm.
    /// </summary>
    public class ResetPlayer : MonoBehaviour
    {
        [Tooltip("Hành động để reset người chơi. Mặc định là phím 'Q'.")]
        [SerializeField] private InputAction _resetAction = new(type: InputActionType.Button, binding: "<Keyboard>/q");

        private void OnEnable()
        {
            _resetAction.Enable();
        }

        private void OnDisable()
        {
            _resetAction.Disable();
        }

        private void Update()
        {
            if (_resetAction.WasPressedThisFrame())
            {
                // Tìm đối tượng người chơi hiện tại trong scene.
                var player = FindAnyObjectByType<PlayerHealth>();
                if (player != null && player.IsAlive)
                {
                    // Gây sát thương cực lớn để đảm bảo người chơi chết.
                    player.TakeDamage(player.CurrentHealth);
                }
            }
        }
    }
}