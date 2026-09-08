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
    public class PlayerHealth : MonoBehaviour, IExplosionDamageable, IStatusEffectable, IHealable, IInvincible
    {
        #region Fields

        /// <summary>
        /// Max HP cua player khi bat Extreme Mode (Player chi co 35 HP).
        /// </summary>
        private const float ExtremeModeMaxHealth = 35f;

        [Header("Health Settings")]
        [SerializeField] private float _baseMaxHealth = 100f;
        private float _currentHealth;
        private float _maxHealth;
        private bool _isInvincible;

        // Trang thai Extreme Mode cua player (neu bat: max HP = 35).
        private bool _extremeModeEnabled;

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
        private Collider _collider; 
        private IPlayerShieldController _shieldController;

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

        /// <summary>
        /// Cho biet player co dang bat tu hay khong. (Duoc dung boi skill Forcefield.)
        /// </summary>
        public bool IsInvincible { get => _isInvincible; set => _isInvincible = value; }

        /// <summary>
        /// Cho biet player co dang day mau (mau hien tai dat toi da) hay khong.
        /// Dung de chan skill Heal khi khong can thiet.
        /// </summary>
        public bool IsHealthFull => _currentHealth >= _maxHealth;


        #region Unity Lifecycle

        private void Awake()
        {
            _ragdollController = GetComponent<RagdollController>();
            _audioSource = GetComponent<AudioSource>();
            _player = GetComponent<IPlayer>();
            _collider = GetComponent<Collider>();
            _shieldController = GetComponent<IPlayerShieldController>();

            _maxHealth = _baseMaxHealth;
            _currentHealth = _maxHealth;
            IsAlive = true;

            // Khoi phuc trang thai Extreme Mode: neu dang bat thi gioi han max HP xuong 35.
            _extremeModeEnabled = GameEvents.TriggerRequestExtremeModeEnabled();
            if (_extremeModeEnabled)
            {
                _maxHealth = ExtremeModeMaxHealth;
                _currentHealth = _maxHealth;
            }

            // Cập nhật UI lần đầu
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
        }

        private void OnEnable()
        {
            // Đăng ký lắng nghe sự kiện reset cuối round
            GameEvents.OnRoundEndPlayerReset += ResetState;
            // Listen for the character reset request (Roblox-style), so the player dies itself.
            GameEvents.OnPlayerResetRequested += HandlePlayerResetRequested;
            // Lang nghe thay doi Extreme Mode de gioi han max HP khi bat/tat.
            GameEvents.OnExtremeModeStateChanged += HandleExtremeModeChanged;
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
            GameEvents.OnPlayerResetRequested -= HandlePlayerResetRequested;
            GameEvents.OnExtremeModeStateChanged -= HandleExtremeModeChanged;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Xử lý sát thương và lực từ một vụ nổ.
        /// </summary>
        public void TakeExplosionDamage(float amount, Vector3 force, Vector3 point, IBaseBombData bombData)
        {
            if (!IsAlive) return;

            // Thong bao player da trung dan vao vu no (bat ke co khien hay dang bat tu).
            // Cac rule/perk (vi du Anti-Freeze tang Max HP moi lan trung vu no bang) lang nghe event
            // nay de phan ung ngay khi trung vu no, khong phu thuoc vao viec co that su nhan sat thuong.
            GameEvents.TriggerPlayerExplosionHit(_player, bombData.Effect);

            bool wasAlive = IsAlive;
            TakeDamage(amount, DamageSourceType.Explosion, bombData.Effect);
            bool isNowDead = wasAlive && !IsAlive;

            if (isNowDead) _ragdollController?.ShatterAndDie(force, point);
            else if (IsAlive) _ragdollController?.OnExplosionHit(force, point, bombData);
        }

        /// <summary>
        /// Nhận sát thương và kiểm tra nếu người chơi đã chết.
        /// </summary>
        public void TakeDamage(float amount, DamageSourceType sourceType = DamageSourceType.Generic, StatusEffectType effectContext = StatusEffectType.None)
        {
            if (!IsAlive) return;

            // Bat tu (forcefield): Player khong nhan bat ky sat thuong nao khi dang bat tu.


            if (_isInvincible) return;

            // --- LOGIC KHIÊN ---
            // Tất cả sát thương đều phải đi qua khiên trước.
            float damageAfterShield = amount;
            if (_shieldController != null && _shieldController.IsShieldActive)
            {
                damageAfterShield = _shieldController.ProcessDamage(amount, sourceType, effectContext);
            }

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

            // Nếu sát thương sau khi qua khiên <= 0, không xử lý gì thêm.
            if (damageAfterShield <= 0) return;

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
            ShowDamageNumber(damageAfterShield);

            _currentHealth -= damageAfterShield;

            // Cập nhật UI
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);

            // Thông báo sát thương thực tế đã được áp dụng (đã qua khien và miễn nhiễm).
            // Dùng cho perk theo doi sat thuong nhan duoc (Regeneration, Anti-Freeze).
            GameEvents.TriggerPlayerDamageTaken(_player, damageAfterShield, sourceType, effectContext);

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
            // --- LOGIC KHIÊN ---
            // Kiểm tra xem khiên có chặn hiệu ứng này không.
            if (_shieldController != null && _shieldController.IsShieldActive)
            {
                if (_shieldController.ProcessStatusEffect(effect))
                {
                    return; // Khiên đã chặn hiệu ứng.
                }
            }
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
            _maxHealth = _baseMaxHealth;
            // Neu Extreme Mode dang bat, gioi han max HP xuong 35 khi reset round.
            if (_extremeModeEnabled)
            {
                _maxHealth = ExtremeModeMaxHealth;
            }
            _currentHealth = _maxHealth;

            // Xóa lịch sử miễn nhiễm sát thương để không mang sang round mới.
            _statusEffectContactImmunityTimestamps.Clear();
            _lastEnvironmentalContactDamageTime = 0f;
            // Tat trang thai bat tu khi reset round de khong mang sang round moi.,

            _isInvincible = false;

            // Cập nhật lại UI máu cho người chơi.
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
        }

        /// <summary>
        /// Hồi một lượng máu cho người chơi.
        /// </summary>
        /// <param name="amount">Lượng máu cần hồi.</param>
        /// <returns>Lượng máu thực tế đã được hồi.</returns>
        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0) return 0f;

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);

            float healedAmount = _currentHealth - previousHealth;
            if (healedAmount > 0)
            {
                GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
            }

            return healedAmount;
        }

        /// <summary>
        /// Tăng máu tối đa của người chơi và hồi máu bằng lượng tương ứng.
        /// </summary>
        /// <param name="amount">Lượng máu tối đa cần tăng.</param>
        public void IncreaseMaxHealth(float amount)
        {
            if (!IsAlive || amount <= 0) return;

            _maxHealth += amount;
            // Không tự động hồi máu ở đây. Việc hồi máu sẽ do behavior quyết định.
            // Chỉ cần thông báo cho UI biết là max health đã thay đổi.
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Xử lý khi trạng thái Extreme Mode thay đổi: gioi han max HP xuong 35 khi bat,
        /// khoi phuc ve max HP co ban khi tat. Khi giam max HP (bat extreme), mau hien tai
        /// duoc clamp lai de khong vuot qua max moi. Khi TAT extreme, reset day mau ve MaxHP moi
        /// (truoc day chi clamp nen current HP van nam o gia tri cuoi cung cua extreme = 35).
        /// </summary>
        /// <param name="enabled">True neu Extreme Mode dang bat, false neu tat.</param>
        private void HandleExtremeModeChanged(bool enabled)
        {
            _extremeModeEnabled = enabled;

            float newMax = enabled ? ExtremeModeMaxHealth : _baseMaxHealth;
            if (Mathf.Approximately(_maxHealth, newMax)) return;

            _maxHealth = newMax;

            if (enabled)
            {
                // Bat extreme: gioi han max HP ve 35 va clamp mau hien tai xuong khong vuot qua 35.
                _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            }
            else
            {
                // Tat extreme: max HP ve 100 va reset day mau ve MaxHP moi (sua loi "current HP van = 35").
                _currentHealth = _maxHealth;
            }

            if (IsAlive)
            {
                GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
            }
        }

        /// <summary>
        /// Handle the character reset request (Roblox-style reset): the player dies itself.
        /// </summary>
        /// <param name="player">The player that is being reset.</param>
        private void HandlePlayerResetRequested(IPlayer player)
        {
            if (!IsAlive || player == null) return;
            if (player != _player) return;

            Die();
        }

        /// <summary>
        /// Xử lý khi người chơi chết, kích hoạt ragdoll.
        /// </summary>
        /// <param name="killingForce">Lực đã gây ra cái chết, để áp dụng cho ragdoll.</param>
        /// <param name="hitPoint">Điểm tác động của lực.</param>
        private void Die(Vector3 killingForce = default, Vector3 hitPoint = default)
        {
            if (!IsAlive) return; // Đảm bảo Die() chỉ chạy một lần

            Debug.Log("[PlayerHealth] Player has died.", this);

            // SỬA LỖI: Gỡ bỏ tất cả các khiên ngay khi người chơi chết.
            _shieldController?.RemoveAllShields();

            // Phát âm thanh chết, nếu có
            if (_audioSource != null && _deathSfx != null)
            {
                _audioSource.PlayOneShot(_deathSfx); // This will be called before ShatterAndDie
            }

            IsAlive = false;
            _currentHealth = 0;
            
            // Cập nhật UI lần cuối để đảm bảo nó hiển thị giá trị 0.
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
            
            // Kích hoạt hiệu ứng chết "vỡ ra" bằng cách phá hủy các khớp.
            // Nếu không có lực, ShatterAndDie sẽ tự xử lý.
            if (killingForce == default)
            {
                _ragdollController?.ShatterAndDie(killingForce, hitPoint);
            }
            
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
            GameEvents.TriggerFloatingTextRequested(transform, offset, $"-{Mathf.RoundToInt(amount)}", Color.red, _player.HPTextContainer, false); // Không hiển thị icon cho HP
        }
        #endregion
    }
}