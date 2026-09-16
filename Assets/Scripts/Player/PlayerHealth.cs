using UnityEngine;
using Core.Interfaces;
using System;
using Core;
using System.Collections;
using System.Collections.Generic;

namespace Player
{
    /// <summary>
    /// Qu?n l� m�u c?a ngu?i choi v� x? l� vi?c chuy?n d?i sang tr?ng th�i ragdoll khi ch?t.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerAnimator))]
    [RequireComponent(typeof(RagdollController))] // Ph? thu?c v�o RagdollController d? k�ch ho?t hi?u ?ng
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
        [Tooltip("S�t thuong m?i tick t? hi?u ?ng Burning/Electrified.")]
        [SerializeField] private float _statusDamagePerTick = 5f;
        [Tooltip("Kho?ng th?i gian gi?a m?i l?n g�y s�t thuong t? hi?u ?ng (gi�y).")]
        [SerializeField] private float _statusEffectDOTInterval = 0.5f;

        [Header("Contact Damage Immunity")]
        [Tooltip("Th?i gian mi?n nhi?m (giy) sau khi nh?n st thuong t? vi?c ch?m vo m?t d?i tu?ng c hi?u ?ng ho?c mi tru?ng.")]
        [SerializeField] private float _contactDamageImmunityDuration = 0.5f;
        private readonly Dictionary<StatusEffectType, float> _statusEffectContactImmunityTimestamps = new();
        private float _lastEnvironmentalContactDamageTime;

        [Header("VFX & SFX Settings")]
        [Tooltip("Prefab VFX se duoc sinh ra tai vi tri player khi chet.")]
        [SerializeField] private GameObject _deathVfxPrefab;
        [Tooltip("m thanh s? pht khi ngu?i choi ch?t.")]
        [SerializeField] private AudioClip _deathSfx;
        [Tooltip("m thanh s? pht khi ngu?i choi nh?n st thuong (chung).")]
        [SerializeField] private AudioClip _takeDamageSfx;
        [Tooltip("m thanh s? pht khi nh?n st thuong t? hi?u ?ng Burning.")]
        [SerializeField] private AudioClip _burningDamageSfx;
        [Tooltip("�m thanh s? ph�t khi nh?n s�t thuong t? hi?u ?ng Electrified.")]
        [SerializeField] private AudioClip _electrifiedDamageSfx;
        [Tooltip("�m thanh s? ph�t khi nh?n s�t thuong t? hi?u ?ng Poison.")]
        [SerializeField] private AudioClip _poisonDamageSfx;

        // Component d? k�ch ho?t ragdoll
        private RagdollController _ragdollController;
        private AudioSource _audioSource;
        private IPlayer _player;
        private Collider _collider; 
        private IPlayerShieldController _shieldController;
        private StatusEffectReceiver _statusEffectReceiver;

        private Coroutine _statusEffectCoroutine;

        #endregion

        /// <summary>
        /// S? ki?n du?c g?i khi ngu?i choi ch?t.
        /// Cc h? th?ng khc c th? dang k vo s? ki?n ny d? x? l logic khi ngu?i choi ch?t.
        /// </summary>
        public event Action OnDied;

        public bool IsAlive { get; private set; }
        /// <summary>
        /// Mu hi?n t?i c?a ngu?i choi.
        /// </summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>
        /// Mau toi da cua nguoi choi.
        /// </summary>
        public float MaxHealth => _maxHealth;

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
            _statusEffectReceiver = GetComponent<StatusEffectReceiver>();

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

            // C?p nh?t UI l?n d?u
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
        }

        private void OnEnable()
        {
            // �ang k� l?ng nghe s? ki?n reset cu?i round
            GameEvents.OnRoundEndPlayerReset += ResetState;
            // Listen for the character reset request (Roblox-style), so the player dies itself.
            GameEvents.OnPlayerResetRequested += HandlePlayerResetRequested;
            // Lang nghe thay doi Extreme Mode de gioi han max HP khi bat/tat.
            GameEvents.OnExtremeModeStateChanged += HandleExtremeModeChanged;
        }

        private void OnDisable()
        {
            // D?ng coroutine n?u d?i tu?ng b? v� hi?u h�a
            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
            }

            // H?y dang k� d? tr�nh l?i
            GameEvents.OnRoundEndPlayerReset -= ResetState;
            GameEvents.OnPlayerResetRequested -= HandlePlayerResetRequested;
            GameEvents.OnExtremeModeStateChanged -= HandleExtremeModeChanged;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// X? l� s�t thuong v� l?c t? m?t v? n?.
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
        /// Nh?n s�t thuong v� ki?m tra n?u ngu?i choi d� ch?t.
        /// </summary>
        public void TakeDamage(float amount, DamageSourceType sourceType = DamageSourceType.Generic, StatusEffectType effectContext = StatusEffectType.None)
        {
            if (!IsAlive) return;

            // Bat tu (forcefield): Player khong nhan bat ky sat thuong nao khi dang bat tu.


            if (_isInvincible) return;

            // --- LOGIC KHI�N ---
            // T?t c? s�t thuong d?u ph?i di qua khi�n tru?c.
            float damageAfterShield = amount;
            if (_shieldController != null && _shieldController.IsShieldActive)
            {
                damageAfterShield = _shieldController.ProcessDamage(amount, sourceType, effectContext);
            }

            // KI?M TRA MI?N NHI?M (LOGIC M?I)
            bool isImmune = false;
            if (sourceType == DamageSourceType.StatusEffectContact && effectContext != StatusEffectType.None)
            {
                // Ki?m tra mi?n nhi?m cho t?ng lo?i hi?u ?ng tr?ng th�i ri�ng bi?t.
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
            
            if (isImmune) return; // N?u mi?n nhi?m, kh�ng g�y s�t thuong, kh�ng ph�t �m thanh, kh�ng hi?n text n?i.

            // N?u s�t thuong sau khi qua khi�n <= 0, kh�ng x? l� g� th�m.
            if (damageAfterShield <= 0) return;

            // YEU CAU MOI: Neu player dang dogng bang si nhan sat thuong sau khi qua khiên,
            // gos bo hiêu ung dogng bang de xuca thoat player.
            UnfreezePlayerIfFrozen();

            // --- Logic ch?n v� ph�t �m thanh s�t thuong ---
            AudioClip clipToPlay = _takeDamageSfx; // �m thanh m?c d?nh

            // Ch?n �m thanh c? th? d?a tr�n ng? c?nh hi?u ?ng
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
            // Ph�t �m thanh d� ch?n (prin SfxManager central pentru live volume SFX din Settings)
            if (clipToPlay != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(clipToPlay, transform.position);
            }

            // Hi?n th? s? s�t thuong bay l�n CH? KHI s�t thuong th?c s? du?c �p d?ng
            ShowDamageNumber(damageAfterShield);

            _currentHealth -= damageAfterShield;

            // C?p nh?t UI
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);

            // Th�ng b�o s�t thuong th?c t? d� du?c �p d?ng (d� qua khien v� mi?n nhi?m).
            // D�ng cho perk theo doi sat thuong nhan duoc (Regeneration, Anti-Freeze).
            GameEvents.TriggerPlayerDamageTaken(_player, damageAfterShield, sourceType, effectContext);

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// �p d?ng hi?u ?ng tr?ng th�i l�n ngu?i choi (v� d?: d?t ch�y).
        /// </summary>
        public void ApplyStatusEffect(StatusEffectType effect, float duration)
        {
            // --- LOGIC KHI�N ---
            // Ki?m tra xem khi�n c� ch?n hi?u ?ng n�y kh�ng.
            if (_shieldController != null && _shieldController.IsShieldActive)
            {
                if (_shieldController.ProcessStatusEffect(effect))
                {
                    return; // Khi�n d� ch?n hi?u ?ng.
                }
            }
            // D?ng hi?u ?ng cu n?u c�
            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
                _statusEffectCoroutine = null;
            }

            // B?t d?u hi?u ?ng m?i n?u n� l� lo?i g�y s�t thuong
            if (effect == StatusEffectType.Burning || effect == StatusEffectType.Electrified)
            {
                _statusEffectCoroutine = StartCoroutine(DamageOverTimeRoutine(duration, effect));
            }
        }

        /// <summary>
        /// Coroutine g�y s�t thuong theo th?i gian.
        /// </summary>
        private IEnumerator DamageOverTimeRoutine(float duration, StatusEffectType effectContext) // ��y l� s�t thuong DOT t? hi?u ?ng �p d?ng l�n player
        {
            float timer = 0f;
            while (timer < duration && IsAlive) // Th�m ki?m tra IsAlive d? d?ng khi ch?t
            {
                // G�y s�t thuong v� ch?
                TakeDamage(_statusDamagePerTick, DamageSourceType.StatusEffectDOT, effectContext);
                yield return new WaitForSeconds(_statusEffectDOTInterval);
                timer += _statusEffectDOTInterval;
            }

            // Hi?u ?ng k?t th�c
            _statusEffectCoroutine = null;
        }

        /// <summary>
        /// Reset l?i tr?ng th�i m�u v� c�c hi?u ?ng li�n quan c?a ngu?i choi, thu?ng du?c g?i khi k?t th�c m?t round.
        /// </summary>
        public void ResetState()
        {
            // Ch? reset n?u ngu?i choi c�n s?ng. Ngu?i choi d� ch?t s? du?c x? l� b?i quy tr�nh h?i sinh.
            if (!IsAlive) return;
            UnfreezePlayerIfFrozen(); // Rã đong player dang dogng bang khi round ket thuc (het gio), de xuca thoat lai lobby.

            // D?ng m?i hi?u ?ng s�t thuong theo th?i gian (DOT) dang ch?y tr�n component n�y.
            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
                _statusEffectCoroutine = null;
            }

            // Ph?c h?i m�u v? gi� tr? t?i da.
            _maxHealth = _baseMaxHealth;
            // Neu Extreme Mode dang bat, gioi han max HP xuong 35 khi reset round.
            if (_extremeModeEnabled)
            {
                _maxHealth = ExtremeModeMaxHealth;
            }
            _currentHealth = _maxHealth;

            // X�a l?ch s? mi?n nhi?m s�t thuong d? kh�ng mang sang round m?i.
            _statusEffectContactImmunityTimestamps.Clear();
            _lastEnvironmentalContactDamageTime = 0f;
            // Tat trang thai bat tu khi reset round de khong mang sang round moi.,

            _isInvincible = false;

            // C?p nh?t l?i UI m�u cho ngu?i choi.
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
        }

        /// <summary>
        /// H?i m?t lu?ng m�u cho ngu?i choi.
        /// </summary>
        /// <param name="amount">Lu?ng m�u c?n h?i.</param>
        /// <returns>Lu?ng m�u th?c t? d� du?c h?i.</returns>
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
        /// Tang m�u t?i da c?a ngu?i choi v� h?i m�u b?ng lu?ng tuong ?ng.
        /// </summary>
        /// <param name="amount">Lu?ng m�u t?i da c?n tang.</param>
        public void IncreaseMaxHealth(float amount)
        {
            if (!IsAlive || amount <= 0) return;

            _maxHealth += amount;
            // Kh�ng t? d?ng h?i m�u ? d�y. Vi?c h?i m�u s? do behavior quy?t d?nh.
            // Ch? c?n th�ng b�o cho UI bi?t l� max health d� thay d?i.
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// X? l� khi tr?ng th�i Extreme Mode thay d?i: gioi han max HP xuong 35 khi bat,
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
        /// X? l� khi ngu?i choi ch?t, k�ch ho?t ragdoll.
        /// </summary>
        /// <param name="killingForce">L?c d� g�y ra c�i ch?t, d? �p d?ng cho ragdoll.</param>
        /// <param name="hitPoint">�i?m t�c d?ng c?a l?c.</param>
        private void Die(Vector3 killingForce = default, Vector3 hitPoint = default)
        {
            if (!IsAlive) return; // �?m b?o Die() ch? ch?y m?t l?n

            UnfreezePlayerIfFrozen(); // Rã đong player dang dogng bang inaiteat de xuca thoat lai lobby.

            Debug.Log("[PlayerHealth] Player has died.", this);

            // S?A L?I: G? b? t?t c? cc khin ngay khi ngu?i choi ch?t.
            _shieldController?.RemoveAllShields();

            // Phat am thanh chet, neu co
            if (_deathSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_deathSfx, transform.position);
            }

            // Sinh VFX chet tai vi tri player neu co
            if (_deathVfxPrefab != null)
            {
                GameEvents.TriggerVFXSpawnRequest(_deathVfxPrefab, transform.position, Quaternion.identity);
            }

            IsAlive = false;
            _currentHealth = 0;
            
            GameEvents.TriggerPlayerHealthChanged(_player, _currentHealth, _maxHealth);
            
            // K�ch ho?t hi?u ?ng ch?t "v? ra" b?ng c�ch ph� h?y c�c kh?p.
            // N?u kh�ng c� l?c, ShatterAndDie s? t? x? l�.
            if (killingForce == default)
            {
                _ragdollController?.ShatterAndDie(killingForce, hitPoint);
            }
            
            // K�ch ho?t c�c s? ki?n ch?t
            OnDied?.Invoke();
            GameEvents.TriggerPlayerDied(_player);
        }

        /// <summary>
        /// Rã đong player daca dang dogng bang (prin component StatusEffectReceiver).
        /// Dung la TakeDamage, Die si ResetState de garant ca player nu se tuma blocat.
        /// </summary>
        private void UnfreezePlayerIfFrozen()
        {
            if (_statusEffectReceiver != null) _statusEffectReceiver.UnfreezePlayer();
        }

        /// <summary>
        /// Y�u c?u h? th?ng hi?n th? m?t text n?i cho bi?t lu?ng s�t thuong d� nh?n.
        /// </summary>
        /// <param name="amount">Lu?ng s�t thuong.</param>
        private void ShowDamageNumber(float amount)
        {
            if (amount <= 0) return;

            // T�nh to�n v? tr� offset c?c b? cho text, ? ph�a tr�n d?u c?a player.
            Vector3 offset = Vector3.up * 1.5f; // Gi� tr? m?c d?nh n?u kh�ng c� collider
            if (_collider != null)
            {
                // V? tr� tr�n d?nh c?a collider, chuy?n th�nh offset so v?i transform.position c?a player.
                Vector3 topOfCollider = _collider.bounds.center + Vector3.up * _collider.bounds.extents.y;
                offset = topOfCollider - transform.position;
            }

            // G?i y�u c?u th�ng qua GameEvents.
            // Gi? d?nh r?ng c� m?t FloatingTextManager dang l?ng nghe s? ki?n n�y.
            GameEvents.TriggerFloatingTextRequested(transform, offset, $"-{Mathf.RoundToInt(amount)}", Color.red, _player.HPTextContainer, false); // Kh�ng hi?n th? icon cho HP
        }
        #endregion
    }
}

