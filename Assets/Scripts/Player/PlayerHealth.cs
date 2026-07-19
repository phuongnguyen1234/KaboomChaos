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
        /// Xử lý sát thương và lực từ một vụ nổ.
        /// </summary>
        public void TakeExplosionDamage(float amount, Vector3 force, Vector3 point)
        {
            if (!IsAlive) return;

            // Nếu sát thương này sẽ gây chết, hãy truyền thông tin về lực cho phương thức Die.
            if (_currentHealth - amount <= 0)
            {
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

            _currentHealth -= amount;
            Debug.Log($"[PlayerHealth] Player took {amount} damage. Current HP: {_currentHealth}/{_maxHealth}", this);

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
            
            // Kích hoạt hiệu ứng chết "vỡ ra" bằng cách phá hủy các khớp.
            _ragdollController?.ShatterAndDie(killingForce, hitPoint);
            
            // Kích hoạt các sự kiện chết
            OnDied?.Invoke();
            GameEvents.TriggerPlayerDied();
        }
        #endregion
    }
}