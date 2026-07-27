using UnityEngine;
using Core.Interfaces;
using System;
using Core;

namespace Player
{
    /// <summary>
    /// Quản lý máu của người chơi và xử lý việc chuyển đổi sang trạng thái ragdoll khi chết.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerAnimator))]
    [RequireComponent(typeof(RagdollController))] // Phụ thuộc vào RagdollController để kích hoạt hiệu ứng
    [RequireComponent(typeof(AudioSource))]
    public class PlayerHealth : MonoBehaviour, IExplosionDamageable
    {
        #region Fields

        [Header("Health Settings")]
        [SerializeField] private float _maxHealth = 100f;
        private float _currentHealth;

        [Header("SFX Settings")]
        [Tooltip("Âm thanh sẽ phát khi người chơi chết.")]
        [SerializeField] private AudioClip _deathSfx;
        [Tooltip("Âm thanh sẽ phát khi người chơi nhận sát thương.")]
        [SerializeField] private AudioClip _takeDamageSfx;

        // Component để kích hoạt ragdoll
        private RagdollController _ragdollController;
        private AudioSource _audioSource;
        private IPlayer _player;
        private Collider _collider; // Thêm để lấy vị trí hiển thị text sát thương

        #endregion

        /// <summary>
        /// Sự kiện được gọi khi người chơi chết.
        /// Các hệ thống khác có thể đăng ký vào sự kiện này để xử lý logic khi người chơi chết.
        /// </summary>
        public event Action OnDied;

        public bool IsAlive { get; private set; }

        #region Unity Lifecycle

        private void Awake()
        {
            _ragdollController = GetComponent<RagdollController>();
            _audioSource = GetComponent<AudioSource>();
            _player = GetComponent<IPlayer>();
            _collider = GetComponent<Collider>();

            _currentHealth = _maxHealth;
            IsAlive = true;

            // Cập nhật UI lần đầu
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Xử lý sát thương và lực từ một vụ nổ.
        /// </summary>
        public void TakeExplosionDamage(float amount, Vector3 force, Vector3 point)
        {
            if (!IsAlive) return;

            // Nếu sát thương này sẽ gây chết, hãy truyền thông tin về lực cho phương thức Die.
            if (_currentHealth - amount <= 0)
            {
                // Hiển thị số sát thương bay lên trước khi chết
                ShowDamageNumber(amount);
                _currentHealth = 0;
                Die(force, point);
            }
            else // Nếu không, xử lý sát thương và lực một cách riêng biệt.
            {
                TakeDamage(amount);
                // Yêu cầu RagdollController xử lý lực tác động.
                _ragdollController?.OnExplosionHit(force, point);
            }
        }

        /// <summary>
        /// Nhận sát thương và kiểm tra nếu người chơi đã chết.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;

            // Phát âm thanh nhận sát thương
            if (_audioSource != null && _takeDamageSfx != null)
            {
                _audioSource.PlayOneShot(_takeDamageSfx);
            }

            // Hiển thị số sát thương bay lên
            ShowDamageNumber(amount);

            _currentHealth -= amount;

            // Cập nhật UI
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Xử lý khi người chơi chết, kích hoạt ragdoll.
        /// </summary>
        /// <param name="killingForce">Lực đã gây ra cái chết, để áp dụng cho ragdoll.</param>
        /// <param name="hitPoint">Điểm tác động của lực.</param>
        private void Die(Vector3 killingForce = default, Vector3 hitPoint = default)
        {
            if (!IsAlive) return; // Đảm bảo Die() chỉ chạy một lần

            Debug.Log("[PlayerHealth] Player has died.", this);

            // Phát âm thanh chết, nếu có
            if (_audioSource != null && _deathSfx != null)
            {
                _audioSource.PlayOneShot(_deathSfx);
            }

            IsAlive = false;
            _currentHealth = 0;
            
            // Cập nhật UI lần cuối để đảm bảo nó hiển thị giá trị 0.
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
            
            // Kích hoạt hiệu ứng chết "vỡ ra" bằng cách phá hủy các khớp.
            _ragdollController?.ShatterAndDie(killingForce, hitPoint);
            
            // Kích hoạt các sự kiện chết
            OnDied?.Invoke();
            GameEvents.TriggerPlayerDied(_player);
        }

        /// <summary>
        /// Yêu cầu hệ thống hiển thị một text nổi cho biết lượng sát thương đã nhận.
        /// </summary>
        /// <param name="amount">Lượng sát thương.</param>
        private void ShowDamageNumber(float amount)
        {
            if (amount <= 0) return;

            // Tính toán vị trí offset cục bộ cho text, ở phía trên đầu của player.
            Vector3 offset = Vector3.up * 1.5f; // Giá trị mặc định nếu không có collider
            if (_collider != null)
            {
                // Vị trí trên đỉnh của collider, chuyển thành offset so với transform.position của player.
                Vector3 topOfCollider = _collider.bounds.center + Vector3.up * _collider.bounds.extents.y;
                offset = topOfCollider - transform.position;
            }

            // Gửi yêu cầu thông qua GameEvents.
            // Giả định rằng có một FloatingTextManager đang lắng nghe sự kiện này.
            GameEvents.TriggerFloatingTextRequested(transform, offset, $"-{Mathf.RoundToInt(amount)}", Color.red);
        }
        #endregion
    }
}