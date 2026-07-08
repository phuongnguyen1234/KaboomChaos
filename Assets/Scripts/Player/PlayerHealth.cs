using UnityEngine;
using Core.Interfaces;
using System;

namespace Player
{
    /// <summary>
    /// Quản lý máu của người chơi và xử lý việc chuyển đổi sang trạng thái ragdoll khi chết.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerAnimator))]
    [RequireComponent(typeof(RagdollController))] // Phụ thuộc vào RagdollController để kích hoạt hiệu ứng
    [RequireComponent(typeof(AudioSource))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        #region Fields

        [Header("Health Settings")]
        [SerializeField] private float _maxHealth = 100f;
        private float _currentHealth;

        [Header("SFX Settings")]
        [Tooltip("Âm thanh sẽ phát khi người chơi chết.")]
        [SerializeField] private AudioClip _deathSfx;

        // Component để kích hoạt ragdoll
        private RagdollController _ragdollController;
        private AudioSource _audioSource;

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

            _currentHealth = _maxHealth;
            IsAlive = true;
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Nhận sát thương và kiểm tra nếu người chơi đã chết.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;

            _currentHealth -= amount;
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
        private void Die()
        {
            if (!IsAlive) return; // Đảm bảo Die() chỉ chạy một lần

            // Phát âm thanh chết, nếu có
            if (_audioSource != null && _deathSfx != null)
            {
                _audioSource.PlayOneShot(_deathSfx);
            }

            IsAlive = false;
            _currentHealth = 0;
            
            // Kích hoạt hiệu ứng chết "vỡ ra" bằng cách phá hủy các khớp.
            _ragdollController?.ShatterAndDie();
            
            // Kích hoạt các sự kiện chết
            OnDied?.Invoke();
            KaboomChaos.GameEvents.TriggerPlayerDied();
        }
        #endregion
    }
}