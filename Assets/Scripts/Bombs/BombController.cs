using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Core;
using Core.Interfaces;
using DG.Tweening;
using Bombs.Data;
using Bombs.Behaviors;
using Bombs.Explosions;
using Random = UnityEngine.Random;

namespace Bombs // Thay đổi
{
    /// <summary>
    /// MonoBehaviour that brings a BaseBombData ScriptableObject to life.
    /// It handles activation, behavior (fuse/missile), and explosion logic.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(AudioSource))]
    public class BombController : MonoBehaviour, IBombController, IExplosionReactable
    {
        #region Fields

        [Header("Data")]
        [Tooltip("Dữ liệu ScriptableObject định nghĩa hành vi của quả bom này.")]
        [SerializeField] private BaseBombData _bombData;

        [Header("Special Bomb Interactions")]
        [Tooltip("Prefab của khối Obsidian sẽ được tạo ra bởi bom băng khi nổ gần dung nham.")]
        [SerializeField] private GameObject _obsidianBlockPrefab;
        [Tooltip("Bán kính để bom băng tương tác với dung nham.")]
        [SerializeField] private float _lavaInteractionRadius = 3f;
        [Tooltip("Layer của các đối tượng được coi là dung nham (lava). Bạn cần tạo một layer tên 'Lava' và gán nó cho các đối tượng dung nham.")]
        [SerializeField] private LayerMask _lavaLayer;

        [Header("Poison Gas Bomb Settings")]
        [Tooltip("Prefab của vùng khí độc sẽ được tạo ra bởi bom độc. Prefab này PHẢI có component PoisonGasController và LifetimeController.")]
        [SerializeField] private GameObject _poisonGasPrefab;
        [Tooltip("Kích thước (scale) của vùng khí độc sẽ được tạo ra.")]
        [SerializeField] private float _poisonGasScale = 5f;



        [Header("Component References")]
        [Tooltip("Danh sách các bộ phận sẽ nháy màu khi có hiệu ứng pulse. Kéo các GameObject có component ColorTint vào đây.")]
        [SerializeField] private List<MaterialEffectController> _partsToPulse = new();

        // Cached components
        public Rigidbody BombRigidbody { get; private set; }
        public Collider BombCollider { get; private set; }
        private AudioSource _audioSource;
        private IDestructionManager _destructionManager; // Thêm tham chiếu đến interface

        // State
        private bool _isActive;
        private bool _isExploded;
        private float _fuseTimer = 0f;
        private int _currentStageIndex = 0;
        private Coroutine _activeCoroutine;
        private GameObject _persistentFuseVFXInstance;
        private GameObject _landingIndicatorVFXInstance;
        private readonly Dictionary<Transform, Vector3> _originalPartScales = new();

        // Matryoshka state
        private int _currentGeneration = 0;
        private Vector3 _originalLocalScale;
        private float _effectiveRadius;
        private float _effectiveDamage;
        private float _effectiveForce;

        // Reusable array for non-allocating physics queries to avoid garbage collection.
        private const int MAX_EXPLOSION_HITS = 512; // Tăng kích thước bộ đệm để xử lý các vụ nổ phức tạp.
        private readonly Collider[] _explosionHits = new Collider[MAX_EXPLOSION_HITS];

        // Strategy Pattern
        private IBombBehavior _behavior;
        private static readonly Dictionary<Type, Func<IBombBehavior>> _behaviorFactory = new()
        {   // Sử dụng namespace mới
            { typeof(BombData), () => new FuseBombBehavior() }, // Thêm namespace đầy đủ
            { typeof(MissileBombData), () => new MissileBombBehavior() }, // Thêm namespace đầy đủ
            { typeof(DynamiteData), () => new DynamiteBehavior() },
            { typeof(ClusterBombData), () => new MissileBombBehavior() }, // Bom chùm có hành vi di chuyển như tên lửa
            { typeof(MatryoshkaBombData), () => new FuseBombBehavior() }, // Bom Matryoshka có hành vi như bom hẹn giờ
            { typeof(TrackingRocketData), () => new TrackingRocketBehavior() }, // Tên lửa theo dõi mục tiêu
            { typeof(NavalMineData), () => new NavalMineBehavior() } // Thủy lôi
        };

        // Explosion Strategy
        private IExplosionStrategy _explosionStrategy;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current activation state of the bomb.
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// Cung cấp quyền truy cập public vào dữ liệu của bom cho các strategy hành vi.
        /// </summary>
        public IBaseBombData BombData => _bombData; // Thay đổi kiểu trả về thành interface

        // Public getters for strategies
        public IBombSpawnerManager BombSpawnerManager { get; private set; }
        public int CurrentGeneration => _currentGeneration;

        /// <summary>
        /// Một đối tượng dữ liệu chung để các strategy hành vi có thể sử dụng cho các nhu cầu khởi tạo cụ thể.
        /// </summary>
        public object BehaviorData { get; set; }


        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            BombRigidbody = GetComponent<Rigidbody>();
            BombCollider = GetComponent<Collider>(); // Get any Collider component
            _audioSource = GetComponent<AudioSource>();
            _destructionManager = Managers.DestructionManager.Instance; // Lấy instance của DestructionManager dưới dạng interface
            BombSpawnerManager = Managers.BombSpawnerManager.Instance;

            if (_bombData != null && _behaviorFactory.TryGetValue(_bombData.GetType(), out var factoryFunc))
            { // _bombData là BaseBombData, nhưng GetType() sẽ trả về BombData hoặc MissileBombData
                _behavior = factoryFunc();
            }
            else if (_bombData != null)
            {
                Debug.LogError($"[BombController] Không có strategy hành vi nào được định nghĩa cho loại bomb data '{_bombData.GetType()}'.", this);
            }

            _explosionStrategy = CreateExplosionStrategy(_bombData);
            // Cache original scales of parts to pulse for scale pulsing effect.
            _originalPartScales.Clear();
            foreach (var part in _partsToPulse)
            {
                if (part != null)
                {
                    _originalPartScales[part.transform] = part.transform.localScale;
                }
            }

            _originalLocalScale = transform.localScale;
            InitializeEffectiveStats();
        }

        /// <summary>
        /// Factory method to create the appropriate explosion strategy based on bomb data.
        /// </summary>
        private static IExplosionStrategy CreateExplosionStrategy(IBaseBombData data)
        {
            if (data == null) return new DefaultExplosionStrategy();

            // Order is important: more specific types first.
            if (data is MatryoshkaBombData)
                return new MatryoshkaExplosionStrategy();
            if (data is ClusterBombData)
                return new ClusterExplosionStrategy();
            if (data.ExplodeMultipleTimes)
                return new MultiExplosionStrategy();
            return new DefaultExplosionStrategy();
        }

        private void Start()
        {
            if (_bombData == null)
            {
                Debug.LogError("BombData is not assigned! Disabling bomb controller.", this);
                enabled = false;
                return;
            }

            // Thiết lập ban đầu dựa trên hành vi
            _behavior?.OnSetup(this);

            // Play spawn sound if available
            if (_bombData.SpawnSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_bombData.SpawnSound);
            }
        }

        private void FixedUpdate()
        {
            if (BombRigidbody == null || _behavior == null) return;
            _behavior.OnFixedUpdate(this);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_behavior == null) return;
            _behavior.OnCollisionEnter(this, collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (_behavior == null) return;
            _behavior.OnCollisionStay(this, collision);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_behavior == null) return;
            _behavior.OnTriggerEnter(this, other);
        }

        #endregion

        #region Public Methods (IBombController)
        /// <summary>
        /// Resets the bomb's internal state so it can be reused by an object pool.
        /// </summary>
        public void ResetState()
        {
            _isExploded = false;
            _isActive = false;
            _fuseTimer = 0f;
            _currentStageIndex = 0;
            _currentGeneration = 0;
            transform.localScale = _originalLocalScale;
            InitializeEffectiveStats();
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }

            // Dọn dẹp hiệu ứng ngòi nổ (nếu có)
            if (_persistentFuseVFXInstance != null)
            {
                GameEvents.TriggerVFXDespawnRequest(_persistentFuseVFXInstance);
                _persistentFuseVFXInstance = null;
            }

            // Dọn dẹp hiệu ứng chỉ báo điểm rơi (nếu có)
            if (_landingIndicatorVFXInstance != null)
            {
                GameEvents.TriggerVFXDespawnRequest(_landingIndicatorVFXInstance);
                _landingIndicatorVFXInstance = null;
            }

            // Hoàn tác lại màu sắc
            foreach (var part in _partsToPulse)
            {
                if (part != null) part.ClearFuseColor();
            }



            // Restore physical components
            BombCollider.enabled = true;
            if (BombRigidbody != null)
            {
                BombRigidbody.isKinematic = false;
                BombRigidbody.linearVelocity = Vector3.zero;
                BombRigidbody.angularVelocity = Vector3.zero;
            }

            // Reset lại các thuộc tính của collider dựa trên hành vi
            _behavior?.OnSetup(this);

            // Restore visuals
            foreach (var part in GetComponentsInChildren<Renderer>(true)) // true to include inactive
            {
                part.enabled = true;
            }

            // Reset pulsed parts scale
            if (_originalPartScales != null)
            {
                foreach(var entry in _originalPartScales)
                {
                    if (entry.Key != null)
                    {
                        entry.Key.DOKill();
                        entry.Key.localScale = entry.Value;
                    }
                }
            }
        }
        /// <summary>
        /// Activates the bomb's primary function (e.g., starts the fuse, begins falling).
        /// </summary>
        public void Activate()
        {
            if (_isActive || _bombData == null || _behavior == null) return;
            
            // Việc đặt _isActive = true giờ đây là trách nhiệm của từng strategy hành vi cụ thể
            // trong phương thức OnActivate() của chúng. Điều này cho phép các loại bom như Dynamite
            // có thể "được kích hoạt" (tức là được thả vào thế giới) mà không thực sự "active" (chờ được kích nổ).
            _behavior.OnActivate(this);
        }

        /// <summary>
        /// Cho phép một strategy hành vi thiết lập trạng thái kích hoạt của bom.
        /// </summary>
        /// <param name="state">True nếu bom đang hoạt động, ngược lại là false.</param>
        public void SetActivationState(bool state) => _isActive = state;

        /// <summary>
        /// Cho phép một strategy hành vi thiết lập coroutine đang hoạt động.
        /// </summary>
        public void SetActiveCoroutine(Coroutine coroutine)
        {
            _activeCoroutine = coroutine;
        }

        /// <summary>
        /// Cho phép một strategy hành vi thiết lập VFX chỉ báo điểm rơi.
        /// Controller sẽ chịu trách nhiệm dọn dẹp nó khi reset.
        /// </summary>
        /// <param name="instance">GameObject instance của VFX chỉ báo.</param>
        public void SetLandingIndicator(GameObject instance)
        {
            // Dọn dẹp indicator cũ nếu có (trường hợp hiếm, ví dụ kích hoạt lại bom)
            if (_landingIndicatorVFXInstance != null && _landingIndicatorVFXInstance != instance)
            {
                GameEvents.TriggerVFXDespawnRequest(_landingIndicatorVFXInstance);
            }
            _landingIndicatorVFXInstance = instance;
        }

        /// <summary>
        /// Public method to manually start the fuse routine.
        /// Can be called by behaviors like DynamiteBehavior.
        /// </summary>
        public void StartFuse()
        {
            if (_isExploded || _isActive || !(_bombData is BombData data)) return;

            // Mark as active to prevent re-triggering
            _isActive = true;

            // --- ÁP DỤNG HIỆU ỨNG NGÒI NỔ CỐ ĐỊNH ---
            if (data.changeColorOnFuse)
            {
                foreach (var part in _partsToPulse)
                {
                    if (part != null) part.SetFuseColor(data.fuseColor);
                }
            }
            if (data.persistentFuseVFX != null)
            {
                // Dọn dẹp instance cũ nếu có (trường hợp hiếm)
                if (_persistentFuseVFXInstance != null) GameEvents.TriggerVFXDespawnRequest(_persistentFuseVFXInstance);

                _persistentFuseVFXInstance = GameEvents.TriggerVFXSpawnRequest(data.persistentFuseVFX, transform.position, transform.rotation);
                if (_persistentFuseVFXInstance != null)
                {
                    _persistentFuseVFXInstance.transform.SetParent(transform, true); // true để giữ world position
                }
            }

            // Start the fuse coroutine
            SetActiveCoroutine(StartCoroutine(FuseBombRoutine(data)));
        }

        /// <summary>
        /// Initializes the bomb as a specific generation of a Matryoshka bomb.
        /// </summary>
        public void InitializeMatryoshka(int generation)
        {
            _currentGeneration = generation;
            if (_bombData is MatryoshkaBombData mData)
            {
                transform.localScale = _originalLocalScale * Mathf.Pow(mData.scaleMultiplier, _currentGeneration);
                _effectiveRadius = _bombData.Radius * Mathf.Pow(mData.radiusMultiplier, _currentGeneration);
                _effectiveDamage = _bombData.Damage * Mathf.Pow(mData.damageMultiplier, _currentGeneration);
                _effectiveForce = _bombData.Force * Mathf.Pow(mData.forceMultiplier, _currentGeneration);
            }
        }

        private void InitializeEffectiveStats()
        {
            if (_bombData == null) return;
            _effectiveRadius = _bombData.Radius;
            _effectiveDamage = _bombData.Damage;
            _effectiveForce = _bombData.Force;
        }

        #endregion

        #region Bomb Behaviors

        public IEnumerator FuseBombRoutine(Bombs.Data.BombData data) // Vẫn giữ BombData cụ thể vì đây là logic nội bộ của FuseBomb
        {
            _fuseTimer = 0f;
            _currentStageIndex = 0;

            // Sort stages by start time to ensure they trigger in order
            data.fuseStages.Sort((a, b) => a.startTime.CompareTo(b.startTime));

            // Play ticking sound (sử dụng thuộc tính từ interface)
            if (data.TickingSound != null && _audioSource != null)
            {
                _audioSource.clip = data.TickingSound;
                _audioSource.pitch = data.TickingSoundPitch;
                _audioSource.loop = true;
                _audioSource.Play();
            }

            while (_fuseTimer < data.fuseTime)
            {
                // Kiểm tra các fuse stage.
                // Dùng vòng lặp 'while' để đảm bảo tất cả các stage đã đến lúc đều được kích hoạt trong cùng một frame.
                // Điều này cho phép các hiệu ứng "xếp chồng" lên nhau nếu chúng có startTime gần nhau.
                while (_currentStageIndex < data.FuseStages.Count && _fuseTimer >= data.FuseStages[_currentStageIndex].startTime)
                {
                    TriggerFuseStage(data.fuseStages[_currentStageIndex]);
                    _currentStageIndex++;
                }

                _fuseTimer += Time.deltaTime;
                yield return null;
            }

            Explode();
        }

        private void TriggerFuseStage(FuseStage stage)
        {
            // Play sound
            if (stage.StageSound != null)
            {
                _audioSource.PlayOneShot(stage.StageSound);
            }

            // Spawn visual effect
            if (stage.VisualEffect != null)
            {
                // Yêu cầu sinh hiệu ứng thông qua hệ thống event thay vì gọi trực tiếp Manager.
                // VFXPoolManager sẽ xử lý việc lấy từ pool hoặc tạo mới nếu cần.
                GameObject vfxInstance = GameEvents.TriggerVFXSpawnRequest(stage.visualEffect, transform.position, Quaternion.identity);

                // Fallback: Nếu không có pool manager nào đang chạy, tự tạo một instance để đảm bảo hiệu ứng luôn hiển thị khi test.
                if (vfxInstance == null)
                { // Sử dụng VisualEffect từ interface
                    Debug.LogWarning($"VFXPoolManager không hoạt động. Tự tạo VFX instance cho stage '{stage.VisualEffect.name}'. Hãy thêm VFXPoolManager vào scene để có hiệu năng tốt nhất.", this);
                    vfxInstance = Instantiate(stage.VisualEffect, transform.position, Quaternion.identity);
                }

                // Gắn hiệu ứng vào quả bom để nó di chuyển cùng.
                if (vfxInstance != null)
                { // Sử dụng VfxRadius từ interface
                    vfxInstance.transform.SetParent(transform, true); // true để giữ nguyên world position ban đầu

                    // Nếu VFX có controller, kích hoạt animation của nó.
                    if (vfxInstance.TryGetComponent<ExplosionEffectController>(out var effectController))
                    {
                        effectController.Trigger(stage.vfxRadius);
                    }
                }
            }

            // Trigger base color pulse
            if (stage.EnableBaseColorPulse && _partsToPulse.Count > 0)
            {
                foreach (var part in _partsToPulse)
                {
                    if (part != null)
                    { // Sử dụng BasePulseColor và BasePulseDuration từ interface
                        part.PulseBaseColor(stage.basePulseColor, stage.basePulseDuration);
                    }
                }
            }

            // Trigger decal color pulse
            if (stage.enableDecalColorPulse && _partsToPulse.Count > 0)
            {
                foreach (var part in _partsToPulse)
                {
                    if (part != null)
                    { // Sử dụng DecalPulseColor và DecalPulseDuration từ interface
                        part.PulseDecalColor(stage.decalPulseColor, stage.decalPulseDuration);
                    }
                }
            }

            // Trigger scale pulse
            if (stage.EnableScalePulse && _partsToPulse.Count > 0)
            {
                foreach (var part in _partsToPulse)
                {
                    if (part != null && _originalPartScales.TryGetValue(part.transform, out Vector3 originalScale))
                    {
                        // Kill any existing scale tween to avoid conflicts and reset scale before starting a new pulse.
                        part.transform.DOKill();
                        part.transform.localScale = originalScale; // Sử dụng PulseScaleMultiplier và PulseScaleDuration từ interface
                        part.transform.DOScale(originalScale * stage.pulseScaleMultiplier, stage.pulseScaleDuration / 2f)
                            .SetEase(Ease.OutQuad)
                            .SetLoops(2, LoopType.Yoyo);
                    }
                }
            }
        }

        #endregion

        #region Explosion

        public void Explode()
        {
            // Sử dụng cờ _isExploded làm cơ chế bảo vệ chính để ngăn chặn mọi hình thức nổ lại.
            if (_isExploded) return;
            _isExploded = true;

            // Đặt _isActive thành false để dừng các hành vi đang chạy (như FixedUpdate) và ngăn việc kích hoạt lại ngòi nổ.
            _isActive = false;

            // Dọn dẹp hiệu ứng ngòi nổ ngay lập tức
            if (_persistentFuseVFXInstance != null)
            {
                GameEvents.TriggerVFXDespawnRequest(_persistentFuseVFXInstance);
                _persistentFuseVFXInstance = null;
            }

            // Dọn dẹp hiệu ứng chỉ báo điểm rơi ngay khi nổ, vì nó không còn cần thiết.
            if (_landingIndicatorVFXInstance != null)
            {
                GameEvents.TriggerVFXDespawnRequest(_landingIndicatorVFXInstance);
                _landingIndicatorVFXInstance = null;
            }

            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            // Delegate the explosion logic to the selected strategy
            _explosionStrategy?.Execute(this);

            // Disable visuals/collider and schedule for destruction. In a real game, this would be pooled.
            foreach (var part in GetComponentsInChildren<Renderer>()) part.enabled = false;
            BombCollider.enabled = false;
            if (BombRigidbody != null) BombRigidbody.isKinematic = true;
            StartCoroutine(DespawnRoutine(5f));
        }
        
        public void HandleSpecialInteractionsForExplosion(Vector3 explosionCenter)
        {
                if (_bombData.Effect == StatusEffectType.Frozen)
                {
                    HandleIceBombSpecialInteractions(explosionCenter);
                }
                if (_bombData.Effect == StatusEffectType.Poison)
                {
                    HandlePoisonExplosion(explosionCenter);
                }
        }

        private IEnumerator DespawnRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameEvents.TriggerBombDespawnRequest(gameObject);
        }

        public void TriggerSingleExplosion(Vector3 explosionCenter, bool isSubExplosion = false)
        {
            // --- Giai đoạn 0: Hiệu ứng & Âm thanh --- (Sử dụng thuộc tính từ interface)
            if (_bombData.ExplosionVFX != null)
            {
                GameObject effectInstance = GameEvents.TriggerVFXSpawnRequest(_bombData.ExplosionVFX, explosionCenter, Quaternion.identity);
                if (effectInstance == null && _bombData.ExplosionVFX != null)
                {
                    Debug.LogWarning($"VFXPoolManager không hoạt động. Tự tạo VFX instance cho vụ nổ '{_bombData.ExplosionVFX.name}'. Hãy thêm VFXPoolManager vào scene để có hiệu năng tốt nhất.", this);
                    effectInstance = Instantiate(_bombData.ExplosionVFX, explosionCenter, Quaternion.identity);
                }
                
                if (effectInstance != null && effectInstance.TryGetComponent<ExplosionEffectController>(out var effectController))
                {
                    effectController.Trigger(_effectiveRadius);
                }
            }
            
            if (_bombData.ExplosionSound != null)
            {
                PlayClipAtPointWithPitch(_bombData.ExplosionSound, explosionCenter, 1f, _bombData.ExplosionSoundPitch);
            }

            // --- GIAI ĐOẠN MỚI: Tác động lên kiến trúc (Destructible Parts) ---
            // Gọi DestructionManager để xử lý việc sụp đổ các công trình. (Sử dụng thuộc tính từ interface)
            // Đây là hệ thống riêng biệt với việc phá hủy các khối địa hình (DestructibleBlock).
            // CHỈ xử lý phá hủy kiến trúc nếu bom được cấu hình để áp dụng lực.
            // Điều này cho phép tạo ra các loại bom chỉ gây hiệu ứng mà không làm sập công trình.
            if (_bombData.applyForce && _destructionManager != null)
            {
                _destructionManager.HandleExplosion(explosionCenter, _effectiveRadius, _effectiveForce);
            }
            else
            {
                Debug.LogWarning("[BombController] DestructionManager.Instance không được tìm thấy. Bỏ qua xử lý phá hủy kiến trúc.");
            }

            if (_effectiveRadius <= 0f) return;

            // --- Giai đoạn 1: Tác động lên các đối tượng động (Players, Props) bằng Physics.OverlapSphere ---
            // Sử dụng HashSet để đảm bảo mỗi đối tượng (như một player có nhiều bộ phận ragdoll) chỉ nhận sát thương một lần.
            HashSet<IDamageable> processedDamageables = new();
            // Sử dụng Radius và AffectedLayers từ interface
            int hitCount = Physics.OverlapSphereNonAlloc(explosionCenter, _effectiveRadius, _explosionHits, _bombData.AffectedLayers);

            // CẢNH BÁO: Nếu số lượng va chạm bằng kích thước tối đa của mảng, có khả năng một số đối tượng đã bị bỏ sót.
            if (hitCount == MAX_EXPLOSION_HITS)
            {
                Debug.LogWarning($"[BombController] Vụ nổ đã đạt đến giới hạn va chạm ({MAX_EXPLOSION_HITS}). Một số đối tượng có thể đã bị bỏ qua. Hãy cân nhắc tăng hằng số MAX_EXPLOSION_HITS.", this);
            }

            // CẢI TIẾN: Sắp xếp lại mảng các collider va chạm để ưu tiên những collider
            // có component IPrimaryExplosionTarget. Điều này đảm bảo rằng nếu một đối tượng
            // có một "hurtbox" chính, nó sẽ được dùng để tính toán sát thương và lực,
            // thay vì một collider ngẫu nhiên khác trên cùng đối tượng đó.
            System.Array.Sort(_explosionHits, 0, hitCount, Comparer<Collider>.Create((a, b) =>
            {
                if (a == null || b == null) return 0;
                bool aIsPrimary = a.GetComponent<IPrimaryExplosionTarget>() != null;
                bool bIsPrimary = b.GetComponent<IPrimaryExplosionTarget>() != null;
                if (aIsPrimary && !bIsPrimary) return -1; // a comes first
                if (!aIsPrimary && bIsPrimary) return 1;  // b comes first
                return 0;
            }));

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _explosionHits[i];
                Vector3 closestPoint;

                if (hit is BoxCollider || hit is SphereCollider || hit is CapsuleCollider || (hit is MeshCollider mc && mc.convex))
                {
                    closestPoint = hit.ClosestPoint(explosionCenter);
                }
                else
                {
                    closestPoint = hit.transform.position;
                }
                float distance = Vector3.Distance(explosionCenter, closestPoint);
                float normalizedDistance = (_effectiveRadius > 0f) ? Mathf.Clamp01(distance / _effectiveRadius) : 0f;

                // Chọn thuộc tính sát thương dựa trên đây là vụ nổ chính hay phụ
                float damageToUse = isSubExplosion ? _bombData.SubExplosionDamage : _effectiveDamage;
                AnimationCurve falloffToUse = isSubExplosion ? _bombData.SubExplosionDamageFalloff : _bombData.DamageFalloff;

                float damageMultiplier = falloffToUse.Evaluate(normalizedDistance);
                float finalDamage = damageToUse * damageMultiplier;
                finalDamage = Mathf.Round(finalDamage / 5.0f) * 5.0f; // Làm tròn đến bội số của 5

                float forceMultiplier = _bombData.ForceFalloff.Evaluate(normalizedDistance);
                float forceToApply = _effectiveForce;
                Vector3 direction = (closestPoint - explosionCenter).normalized;
                if (direction == Vector3.zero) direction = Random.onUnitSphere;
                // Sử dụng UpwardsModifier từ interface
                direction = (direction + Vector3.up * _bombData.UpwardsModifier).normalized;
                Vector3 forceVector = forceMultiplier * forceToApply * direction;

                // --- 1. Xử lý Sát thương (Damage) ---
                var damageableComponent = hit.GetComponentInParent<IDamageable>();
                if (damageableComponent != null && processedDamageables.Add(damageableComponent))
                {
                    // Ưu tiên xử lý bằng IExplosionDamageable nếu có.
                    if (damageableComponent is IExplosionDamageable explosionDamageable)
                    {
                        explosionDamageable.TakeExplosionDamage(finalDamage, forceVector, closestPoint, _bombData);
                    }
                    else // Nếu không, chỉ áp dụng sát thương thông thường.
                    {
                        damageableComponent.TakeDamage(finalDamage, DamageSourceType.Explosion);
                    }
                }

                // --- 2. Xử lý Vật lý & Phản ứng (Physics & Reactions) ---
                // Logic này được tách biệt khỏi sát thương để đảm bảo các đối tượng không có IDamageable (như bom khác) vẫn có thể bị tác động.
                if (_bombData.ApplyForce && hit.attachedRigidbody != null && !hit.attachedRigidbody.isKinematic)
                {
                    if (hit.attachedRigidbody.TryGetComponent<IExplosionReactable>(out var explosionReactable))
                    {
                        // Để đối tượng tự xử lý phản ứng với vụ nổ.
                        // Đây là cách Dynamite được kích hoạt.
                        explosionReactable.OnExplosionHit(forceVector, closestPoint, _bombData);
                    }
                    else
                    {
                        // Nếu không có phản ứng đặc biệt, chỉ áp dụng lực thông thường.
                        hit.attachedRigidbody.AddForceAtPosition(forceVector, closestPoint, _bombData.forceMode);
                    }
                }

                // --- 3. Xử lý các hiệu ứng khác ---
                // Các hiệu ứng khác (trạng thái, phá hủy địa hình) được áp dụng riêng biệt cho mọi đối tượng va chạm.
                ApplyOtherExplosionEffects(hit, Vector3.Distance(explosionCenter, hit.transform.position));
            }
        }

        /// <summary>
        /// Implementation of IExplosionReactable.
        /// This is called when this bomb is hit by another explosion.
        /// It delegates the handling to the current behavior strategy.
        /// </summary>
        public void OnExplosionHit(Vector3 force, Vector3 point, IBaseBombData bombData)
        {
            _behavior?.OnExplosionHit(this, force, point, bombData);
        }

        /// <summary>
        /// Áp dụng các hiệu ứng phụ của vụ nổ như hiệu ứng trạng thái và phá hủy địa hình.
        /// Tách ra để giữ cho logic chính trong TriggerSingleExplosion gọn gàng hơn.
        /// </summary>
        private void ApplyOtherExplosionEffects(Collider hit, float distanceToBlockCenter)
        {
            // Hiệu ứng trạng thái cho đối tượng động
            if (_bombData.Effect != StatusEffectType.None && (_bombData.StatusEffectLayers.value & (1 << hit.gameObject.layer)) != 0)
            {
                // Lấy TẤT CẢ các component có thể nhận hiệu ứng trên đối tượng.
                // Điều này cho phép một đối tượng (như Player) có nhiều component xử lý các khía cạnh khác nhau của một hiệu ứng (ví dụ: PlayerHealth xử lý DOT, StatusEffectReceiver xử lý đóng băng).
                var effectables = hit.GetComponentsInParent<IStatusEffectable>();
                foreach (var effectable in effectables)
                {
                    // --- LOGIC MỚI: Lọc hiệu ứng cho Player ---
                    // Kiểm tra xem đối tượng có phải là người chơi không.
                    bool isPlayer = (effectable as MonoBehaviour)?.GetComponentInParent<IPlayer>() != null;

                    if (isPlayer)
                    {
                        // Người chơi chỉ có thể bị ảnh hưởng bởi hiệu ứng Frozen từ các vụ nổ.
                        if (_bombData.Effect == StatusEffectType.Frozen)
                        {
                            effectable.ApplyStatusEffect(_bombData.Effect, _bombData.EffectDuration);
                        }
                        // Bỏ qua tất cả các hiệu ứng khác (Burning, Electrified, Poison...) cho người chơi.
                    }
                    else
                    {
                        // Các đối tượng không phải người chơi (như block, part) nhận tất cả các hiệu ứng.
                        effectable.ApplyStatusEffect(_bombData.Effect, _bombData.EffectDuration);
                    }
                }
            }

            // --- PHÁ HỦY ĐỊA HÌNH ---
            // Nó sẽ tìm component DestructibleBlock trên các collider va chạm trong OverlapSphere.
            if (_bombData.CanDestroyTerrain && hit.TryGetComponent<DestructibleBlock>(out var block))
            { // Sử dụng CanDestroyTerrain từ interface
                // Chỉ phá hủy khối nếu TÂM của nó nằm trong bán kính vụ nổ.
                if (distanceToBlockCenter <= _effectiveRadius)
                { // Sử dụng Radius và TerrainDestructionPowerFalloff từ interface
                    // Tính toán falloff dựa trên khoảng cách từ tâm vụ nổ đến tâm khối.
                    float normalizedDistanceToCenter = (_effectiveRadius > 0f) ? Mathf.Clamp01(distanceToBlockCenter / _effectiveRadius) : 0f;
                    float powerFloat = _bombData.TerrainDestructionPowerFalloff.Evaluate(normalizedDistanceToCenter);
                    int powerInt = Mathf.RoundToInt(powerFloat);

                    block.ReceiveImpact(powerInt);
                }
            }
        }

        /// <summary>
        /// Phát một AudioClip tại một vị trí cụ thể trong thế giới với âm lượng và cao độ (pitch) được chỉ định.
        /// Tạo một GameObject tạm thời với AudioSource để thực hiện việc này.
        /// </summary>
        /// <param name="clip">AudioClip cần phát.</param>
        /// <param name="position">Vị trí trong thế giới để phát âm thanh.</param>
        /// <param name="volume">Âm lượng (0.0 đến 1.0).</param>
        /// <param name="pitch">Cao độ (pitch) của âm thanh.</param>
        private static void PlayClipAtPointWithPitch(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            if (clip == null) return;

            GameObject tempGO = new("TempAudio"); // Tạo một GameObject tạm thời
            tempGO.transform.position = position;
            AudioSource aSource = tempGO.AddComponent<AudioSource>();
            aSource.clip = clip;
            aSource.volume = volume;
            aSource.pitch = pitch;
            aSource.spatialBlend = 1.0f; // Âm thanh 3D
            aSource.Play();
            Destroy(tempGO, clip.length); // Hủy GameObject sau khi âm thanh phát xong
        }

        /// <summary>
        /// Xử lý các tương tác đặc biệt của bom băng, chẳng hạn như tạo obsidian khi nổ gần dung nham.
        /// </summary>
        /// <param name="explosionCenter">Tâm của vụ nổ.</param>
        private void HandleIceBombSpecialInteractions(Vector3 explosionCenter)
        {
            if (_obsidianBlockPrefab == null || _lavaLayer.value == 0) return;

            // Dùng OverlapSphere để tìm các collider thuộc layer dung nham trong bán kính tương tác.
            // Sử dụng lại mảng _explosionHits để tránh cấp phát bộ nhớ mới.
            int hitCount = Physics.OverlapSphereNonAlloc(explosionCenter, _lavaInteractionRadius, _explosionHits, _lavaLayer);

            if (hitCount > 0)
            {
                // Chỉ cần tìm thấy một vùng dung nham là đủ.
                // Lấy collider đầu tiên tìm được.
                Collider lavaCollider = _explosionHits[0];

                // Tìm điểm gần nhất trên bề mặt dung nham so với tâm nổ.
                Vector3 spawnPoint = lavaCollider.ClosestPoint(explosionCenter);

                // Nâng vị trí spawn lên một chút để khối obsidian không bị kẹt vào trong dung nham.
                // Giả sử khối obsidian có kích thước 1x1x1.
                spawnPoint.y += 0.5f;

                // Tạo khối obsidian. Trong một hệ thống thực tế, nên sử dụng object pool.
                Instantiate(_obsidianBlockPrefab, spawnPoint, Quaternion.identity);
            }
        }

        /// <summary>
        /// Xử lý việc tạo ra một vùng khí độc tại vị trí nổ.
        /// </summary>
        /// <param name="explosionCenter">Tâm của vụ nổ.</param>
        private void HandlePoisonExplosion(Vector3 explosionCenter)
        {
            if (_poisonGasPrefab == null)
            {
                Debug.LogWarning("[BombController] _poisonGasPrefab chưa được gán. Không thể tạo khí độc.", this);
                return;
            }

            // Yêu cầu tạo một instance khí độc thông qua hệ thống event (để tận dụng pool).
            GameObject gasInstance = GameEvents.TriggerVFXSpawnRequest(_poisonGasPrefab, explosionCenter, Quaternion.identity);

            // Đặt kích thước cho vùng khí độc.
            // PoisonGasController bên trong sẽ tự động điều chỉnh trigger và particle system theo scale này.
            if (gasInstance != null)
            {
                gasInstance.transform.localScale = Vector3.one * _poisonGasScale;
            }
        }
        #endregion

        #region Editor Gizmos

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_bombData != null && _effectiveRadius > 0)
            { // Sử dụng Radius từ interface
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Sử dụng Radius từ interface
                Gizmos.DrawSphere(transform.position, _effectiveRadius);
            }
        }
        #endif
        #endregion
    }
}