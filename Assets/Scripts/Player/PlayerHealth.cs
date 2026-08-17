using UnityEngine;
using Core.Interfaces;
using System;
using Core;
using System.Collections;
using System.Collections.Generic;

namespace Player
{
    /// <summary>
    /// Quản lý máu của người chơi và xử lý việc chuyển đổi sang trạng thái ragdoll khi chết.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerAnimator))]
    [RequireComponent(typeof(RagdollController))] // Phụ thuộc vào RagdollController để kích hoạt hiệu ứng
    [RequireComponent(typeof(AudioSource))]
    public class PlayerHealth : MonoBehaviour, IExplosionDamageable, IStatusEffectable
    {
        #region Fields

        [Header("Health Settings")]
        [SerializeField] private float _maxHealth = 100f;
        private float _currentHealth;

        [Header("Status Effect Settings")]
        [Tooltip("Sát thương mỗi tick từ hiệu ứng Burning/Electrified.")]
        [SerializeField] private float _statusDamagePerTick = 5f;
        [Tooltip("Khoảng thời gian giữa mỗi lần gây sát thương từ hiệu ứng (giây).")]
        [SerializeField] private float _statusEffectDOTInterval = 0.5f;

        [Header("Contact Damage Immunity")]
        [Tooltip("Thời gian miễn nhiễm (giây) sau khi nhận sát thương từ việc chạm vào một đối tượng có hiệu ứng hoặc môi trường.")]
        [SerializeField] private float _contactDamageImmunityDuration = 0.5f;
        private readonly Dictionary<StatusEffectType, float> _statusEffectContactImmunityTimestamps = new();
        private float _lastEnvironmentalContactDamageTime;

        [Header("SFX Settings")]
        [Tooltip("Âm thanh sẽ phát khi người chơi chết.")]
        [SerializeField] private AudioClip _deathSfx;
        [Tooltip("Âm thanh sẽ phát khi người chơi nhận sát thương (chung).")]
        [SerializeField] private AudioClip _takeDamageSfx;
        [Tooltip("Âm thanh sẽ phát khi nhận sát thương từ hiệu ứng Burning.")]
        [SerializeField] private AudioClip _burningDamageSfx;
        [Tooltip("Âm thanh sẽ phát khi nhận sát thương từ hiệu ứng Electrified.")]
        [SerializeField] private AudioClip _electrifiedDamageSfx;
        [Tooltip("Âm thanh sẽ phát khi nhận sát thương từ hiệu ứng Poison.")]
        [SerializeField] private AudioClip _poisonDamageSfx;

        // Component để kích hoạt ragdoll
        private RagdollController _ragdollController;
        private AudioSource _audioSource;
        private IPlayer _player;
        private Collider _collider; // Thêm để lấy vị trí hiển thị text sát thương

        private Coroutine _statusEffectCoroutine;

        #endregion

        /// <summary>
        /// Sự kiện được gọi khi người chơi chết.
        /// Các hệ thống khác có thể đăng ký vào sự kiện này để xử lý logic khi người chơi chết.
        /// </summary>
        public event Action OnDied;

        public bool IsAlive { get; private set; }
        /// <summary>
        /// Máu hiện tại của người chơi.
        /// </summary>
        public float CurrentHealth => _currentHealth;


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

        private void OnEnable()
        {
            // Đăng ký lắng nghe sự kiện reset cuối round
            GameEvents.OnRoundEndPlayerReset += ResetState;
        }

        private void OnDisable()
        {
            // Dừng coroutine nếu đối tượng bị vô hiệu hóa
            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
            }

            // Hủy đăng ký để tránh lỗi
            GameEvents.OnRoundEndPlayerReset -= ResetState;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Xử lý sát thương và lực từ một vụ nổ.
        /// </summary>
        public void TakeExplosionDamage(float amount, Vector3 force, Vector3 point, IBaseBombData bombData)
        {
            if (!IsAlive) return;

            // Nếu sát thương này sẽ gây chết, hãy truyền thông tin về lực cho phương thức Die.
            if (_currentHealth - amount <= 0)
            {
                // Hiển thị số sát thương bay lên trước khi chết
                ShowDamageNumber(amount);
                _currentHealth = 0;
                Die(force, point); // Die() không cần biết về bombData, nó chỉ cần lực.
            }
            else // Nếu không, xử lý sát thương và lực một cách riêng biệt.
            {
                TakeDamage(amount, DamageSourceType.Explosion);
                // Yêu cầu RagdollController xử lý lực tác động.
                _ragdollController?.OnExplosionHit(force, point, bombData);
            }
        }

        /// <summary>
        /// Nhận sát thương và kiểm tra nếu người chơi đã chết.
        /// </summary>
        public void TakeDamage(float amount, DamageSourceType sourceType = DamageSourceType.Generic, StatusEffectType effectContext = StatusEffectType.None)
        {
            if (!IsAlive) return;

            // KIỂM TRA MIỄN NHIỄM (LOGIC MỚI)
            bool isImmune = false;
            if (sourceType == DamageSourceType.StatusEffectContact && effectContext != StatusEffectType.None)
            {
                // Kiểm tra miễn nhiễm cho từng loại hiệu ứng trạng thái riêng biệt.
                if (_statusEffectContactImmunityTimestamps.TryGetValue(effectContext, out float lastDamageTime))
                {
                    if (Time.time < lastDamageTime + _contactDamageImmunityDuration) isImmune = true;
                }
                if (!isImmune) _statusEffectContactImmunityTimestamps[effectContext] = Time.time;
            }
            else if (sourceType == DamageSourceType.EnvironmentalContact)
            {
                if (Time.time < _lastEnvironmentalContactDamageTime + _contactDamageImmunityDuration) isImmune = true;
                if (!isImmune) _lastEnvironmentalContactDamageTime = Time.time;
            }
            
            if (isImmune) return; // Nếu miễn nhiễm, không gây sát thương, không phát âm thanh, không hiện text nổi.

            // Yêu cầu 2: Nếu sát thương bằng 0, không xử lý gì thêm (không phát âm thanh, không hiện text).
            if (amount <= 0) return;

            // --- Logic chọn và phát âm thanh sát thương ---
            AudioClip clipToPlay = _takeDamageSfx; // Âm thanh mặc định

            // Chọn âm thanh cụ thể dựa trên ngữ cảnh hiệu ứng
            if (sourceType == DamageSourceType.StatusEffectContact || sourceType == DamageSourceType.StatusEffectDOT || sourceType == DamageSourceType.EnvironmentalContact)
            {
                switch (effectContext)
                {
                    case StatusEffectType.Burning:
                        if (_burningDamageSfx != null) clipToPlay = _burningDamageSfx;
                        break;
                    case StatusEffectType.Electrified:
                        if (_electrifiedDamageSfx != null) clipToPlay = _electrifiedDamageSfx;
                        break;
                    case StatusEffectType.Poison:
                        if (_poisonDamageSfx != null) clipToPlay = _poisonDamageSfx;
                        break;
                }
            }
            // Phát âm thanh đã chọn
            if (_audioSource != null && clipToPlay != null)
            {
                _audioSource.PlayOneShot(clipToPlay);
            }

            // Hiển thị số sát thương bay lên CHỈ KHI sát thương thực sự được áp dụng
            ShowDamageNumber(amount);

            _currentHealth -= amount;

            // Cập nhật UI
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Áp dụng hiệu ứng trạng thái lên người chơi (ví dụ: đốt cháy).
        /// </summary>
        public void ApplyStatusEffect(StatusEffectType effect, float duration)
        {
            // Dừng hiệu ứng cũ nếu có
            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
                _statusEffectCoroutine = null;
            }

            // Bắt đầu hiệu ứng mới nếu nó là loại gây sát thương
            if (effect == StatusEffectType.Burning || effect == StatusEffectType.Electrified)
            {
                _statusEffectCoroutine = StartCoroutine(DamageOverTimeRoutine(duration, effect));
            }
        }

        /// <summary>
        /// Coroutine gây sát thương theo thời gian.
        /// </summary>
        private IEnumerator DamageOverTimeRoutine(float duration, StatusEffectType effectContext) // Đây là sát thương DOT từ hiệu ứng áp dụng lên player
        {
            float timer = 0f;
            while (timer < duration && IsAlive) // Thêm kiểm tra IsAlive để dừng khi chết
            {
                // Gây sát thương và chờ
                TakeDamage(_statusDamagePerTick, DamageSourceType.StatusEffectDOT, effectContext);
                yield return new WaitForSeconds(_statusEffectDOTInterval);
                timer += _statusEffectDOTInterval;
            }

            // Hiệu ứng kết thúc
            _statusEffectCoroutine = null;
        }

        /// <summary>
        /// Reset lại trạng thái máu và các hiệu ứng liên quan của người chơi, thường được gọi khi kết thúc một round.
        /// </summary>
        public void ResetState()
        {
            // Chỉ reset nếu người chơi còn sống. Người chơi đã chết sẽ được xử lý bởi quy trình hồi sinh.
            if (!IsAlive) return;

            // Dừng mọi hiệu ứng sát thương theo thời gian (DOT) đang chạy trên component này.
            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
                _statusEffectCoroutine = null;
            }

            // Phục hồi máu về giá trị tối đa.
            _currentHealth = _maxHealth;

            // Xóa lịch sử miễn nhiễm sát thương để không mang sang round mới.
            _statusEffectContactImmunityTimestamps.Clear();
            _lastEnvironmentalContactDamageTime = 0f;

            // Cập nhật lại UI máu cho người chơi.
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
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