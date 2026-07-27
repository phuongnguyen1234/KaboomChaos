using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Interfaces;
using DG.Tweening;

namespace Bombs
{
    /// <summary>
    /// MonoBehaviour that brings a BombData ScriptableObject to life.
    /// It handles activation, behavior (fuse/missile), and explosion logic.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(AudioSource))]
    public class BombController : MonoBehaviour, IBombController
    {
        #region Fields

        [Header("Data")]
        [Tooltip("Dữ liệu ScriptableObject định nghĩa hành vi của quả bom này.")]
        [SerializeField] private BaseBombData _bombData;

        [Header("Component References")]
        [Tooltip("Danh sách các bộ phận sẽ nháy màu khi có hiệu ứng pulse. Kéo các GameObject có component ColorTint vào đây.")]
        [SerializeField] private List<MaterialEffectController> _partsToPulse = new();

        // Cached components
        private Rigidbody _rb;
        private Collider _collider;
        private AudioSource _audioSource;

        // State
        private bool _isActive = false;
        private float _fuseTimer = 0f;
        private int _currentStageIndex = 0;
        private Coroutine _activeCoroutine;
        private readonly Dictionary<Transform, Vector3> _originalPartScales = new();

        // Reusable array for non-allocating physics queries to avoid garbage collection.
        private const int MAX_EXPLOSION_HITS = 256; // Tăng kích thước bộ đệm để xử lý các vụ nổ phức tạp.
        private readonly Collider[] _explosionHits = new Collider[MAX_EXPLOSION_HITS];

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current activation state of the bomb.
        /// </summary>
        public bool IsActive => _isActive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>(); // Get any Collider component
            _audioSource = GetComponent<AudioSource>();

            // Cache original scales of parts to pulse for scale pulsing effect.
            _originalPartScales.Clear();
            foreach (var part in _partsToPulse)
            {
                if (part != null)
                {
                    _originalPartScales[part.transform] = part.transform.localScale;
                }
            }
        }

        private void Start()
        {
            if (_bombData == null)
            {
                Debug.LogError("BombData is not assigned! Disabling bomb controller.", this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (!_isActive || _rb == null) return;

            // Xử lý di chuyển cho từng loại bom
            if (_bombData is MissileBombData missileData)
            {
                // Tên lửa rơi với tốc độ không đổi
                _rb.linearVelocity = Vector3.down * missileData.fallSpeed;
            }
            else if (_bombData is BombData fuseBombData && fuseBombData.additionalGravity > 0)
            {
                // Bom hẹn giờ có thể có thêm trọng lực để rơi nhanh hơn
                _rb.AddForce(Vector3.down * fuseBombData.additionalGravity, ForceMode.Acceleration);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isActive) return;

            // Logic for non-trigger bombs that explode on contact (i.e. non-missile types with this flag)
            if (_bombData is BombData fuseBombData && fuseBombData.isActivatedOnContact)
            {
                Explode();
            }
            // Missile bombs are triggers and use OnTriggerEnter to explode on contact.
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;

            // Logic for trigger bombs (i.e., missiles) to explode on contact.
            // We check the layer to avoid exploding on other triggers or non-gameplay objects.
            if (_bombData is MissileBombData && (_bombData.affectedLayers.value & (1 << other.gameObject.layer)) != 0)
            {
                Explode();
            }
        }

        #endregion

        #region Public Methods (IBombController)
        /// <summary>
        /// Resets the bomb's internal state so it can be reused by an object pool.
        /// </summary>
        public void ResetState()
        {
            _isActive = false;
            _fuseTimer = 0f;
            _currentStageIndex = 0;
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }

            // Restore physical components
            _collider.enabled = true;
            _collider.isTrigger = false; // Reset to non-trigger by default
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

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
            if (_isActive || _bombData == null) return;

            _isActive = true;

            if (_bombData is BombData fuseBombData)
            {
                _collider.isTrigger = false; // Fuse bombs are solid colliders
                _activeCoroutine = StartCoroutine(FuseBombRoutine(fuseBombData));
            }
            else if (_bombData is MissileBombData missileBombData)
            {
                _collider.isTrigger = true; // Missiles should pass through objects, so they are triggers.
                // Missile movement is handled in FixedUpdate.
                _activeCoroutine = null;
            }
        }

        #endregion

        #region Bomb Behaviors

        private IEnumerator FuseBombRoutine(BombData data)
        {
            _fuseTimer = 0f;
            _currentStageIndex = 0;

            // Sort stages by start time to ensure they trigger in order
            data.fuseStages.Sort((a, b) => a.startTime.CompareTo(b.startTime));

            // Play ticking sound
            if (data.tickingSound != null && _audioSource != null)
            {
                _audioSource.clip = data.tickingSound;
                _audioSource.loop = true;
                _audioSource.Play();
            }

            while (_fuseTimer < data.fuseTime)
            {
                // Kiểm tra các fuse stage.
                // Dùng vòng lặp 'while' để đảm bảo tất cả các stage đã đến lúc đều được kích hoạt trong cùng một frame.
                // Điều này cho phép các hiệu ứng "xếp chồng" lên nhau nếu chúng có startTime gần nhau.
                while (_currentStageIndex < data.fuseStages.Count && _fuseTimer >= data.fuseStages[_currentStageIndex].startTime)
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
            if (stage.stageSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(stage.stageSound);
            }

            // Spawn visual effect
            if (stage.visualEffect != null)
            {
                // Yêu cầu sinh hiệu ứng thông qua hệ thống event thay vì gọi trực tiếp Manager.
                // VFXPoolManager sẽ xử lý việc lấy từ pool hoặc tạo mới nếu cần.
                GameObject vfxInstance = GameEvents.TriggerVFXSpawnRequest(stage.visualEffect, transform.position, Quaternion.identity);

                // Fallback: Nếu không có pool manager nào đang chạy, tự tạo một instance để đảm bảo hiệu ứng luôn hiển thị khi test.
                if (vfxInstance == null)
                {
                    Debug.LogWarning($"VFXPoolManager không hoạt động. Tự tạo VFX instance cho stage '{stage.visualEffect.name}'. Hãy thêm VFXPoolManager vào scene để có hiệu năng tốt nhất.", this);
                    vfxInstance = Instantiate(stage.visualEffect, transform.position, Quaternion.identity);
                }

                // Gắn hiệu ứng vào quả bom để nó di chuyển cùng.
                if (vfxInstance != null)
                {
                    vfxInstance.transform.SetParent(transform, true); // true để giữ nguyên world position ban đầu

                    // Nếu VFX có controller, kích hoạt animation của nó.
                    if (vfxInstance.TryGetComponent<ExplosionEffectController>(out var effectController))
                    {
                        effectController.Trigger(stage.vfxRadius);
                    }
                }
            }

            // Trigger base color pulse
            if (stage.enableBaseColorPulse && _partsToPulse.Count > 0)
            {
                foreach (var part in _partsToPulse)
                {
                    if (part != null)
                    {
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
                    {
                        part.PulseDecalColor(stage.decalPulseColor, stage.decalPulseDuration);
                    }
                }
            }

            // Trigger scale pulse
            if (stage.enableScalePulse && _partsToPulse.Count > 0)
            {
                foreach (var part in _partsToPulse)
                {
                    if (part != null && _originalPartScales.TryGetValue(part.transform, out Vector3 originalScale))
                    {
                        // Kill any existing scale tween to avoid conflicts and reset scale before starting a new pulse.
                        part.transform.DOKill();
                        part.transform.localScale = originalScale;
                        part.transform.DOScale(originalScale * stage.pulseScaleMultiplier, stage.pulseScaleDuration / 2f)
                            .SetEase(Ease.OutQuad)
                            .SetLoops(2, LoopType.Yoyo);
                    }
                }
            }
        }

        #endregion

        #region Explosion

        private void Explode()
        {
            if (!_isActive) return;
            _isActive = false; // Mark as exploded to prevent re-triggering

            // Stop any currently playing sounds (like ticking)
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }

            if (_bombData.explodeMultipleTimes)
            {
                StartCoroutine(MultiExplosionRoutine());
            }
            else
            {
                TriggerSingleExplosion(transform.position);
            }

            // Disable visuals/collider and schedule for destruction. In a real game, this would be pooled.
            foreach (var part in GetComponentsInChildren<Renderer>()) part.enabled = false;
            _collider.enabled = false;
            if (_rb != null) _rb.isKinematic = true;
            // Request to be returned to the pool after a delay
            StartCoroutine(DespawnRoutine(5f));
        }

        private IEnumerator MultiExplosionRoutine()
        {
            for (int i = 0; i < _bombData.numberOfExplosions; i++)
            {
                Vector3 explosionCenter = transform.position + (Random.insideUnitSphere * _bombData.explosionSpreadRadius);
                TriggerSingleExplosion(explosionCenter);

                if (i < _bombData.numberOfExplosions - 1)
                {
                    yield return new WaitForSeconds(_bombData.delayBetweenExplosions);
                }
            }
        }

        private IEnumerator DespawnRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameEvents.TriggerBombDespawnRequest(gameObject);
        }

        private void TriggerSingleExplosion(Vector3 explosionCenter)
        {
            // --- Giai đoạn 0: Hiệu ứng & Âm thanh ---
            if (_bombData.explosionVFX != null)
            {
                GameObject effectInstance = GameEvents.TriggerVFXSpawnRequest(_bombData.explosionVFX, explosionCenter, Quaternion.identity);
                if (effectInstance == null && _bombData.explosionVFX != null)
                {
                    Debug.LogWarning($"VFXPoolManager không hoạt động. Tự tạo VFX instance cho vụ nổ '{_bombData.explosionVFX.name}'. Hãy thêm VFXPoolManager vào scene để có hiệu năng tốt nhất.", this);
                    effectInstance = Instantiate(_bombData.explosionVFX, explosionCenter, Quaternion.identity);
                }
                
                if (effectInstance != null && effectInstance.TryGetComponent<ExplosionEffectController>(out var effectController))
                {
                    effectController.Trigger(_bombData.radius);
                }
            }
            
            if (_bombData.explosionSound != null) AudioSource.PlayClipAtPoint(_bombData.explosionSound, explosionCenter);
            if (_bombData.radius <= 0f) return;

            // --- Giai đoạn 1: Tác động lên các đối tượng động (Players, Props) bằng Physics.OverlapSphere ---
            // Sử dụng HashSet để đảm bảo mỗi đối tượng (như một player có nhiều bộ phận ragdoll) chỉ nhận sát thương một lần.
            HashSet<IDamageable> processedDamageables = new();

            int hitCount = Physics.OverlapSphereNonAlloc(explosionCenter, _bombData.radius, _explosionHits, _bombData.affectedLayers);

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
                float normalizedDistance = (_bombData.radius > 0f) ? Mathf.Clamp01(distance / _bombData.radius) : 0f;

                // Tính toán các giá trị chung
                float damageMultiplier = _bombData.damageFalloff.Evaluate(normalizedDistance);
                float finalDamage = _bombData.damage * damageMultiplier;

                float forceMultiplier = _bombData.forceFalloff.Evaluate(normalizedDistance);
                float forceToApply = _bombData.force;
                Vector3 direction = (closestPoint - explosionCenter).normalized;
                if (direction == Vector3.zero) direction = Random.onUnitSphere;
                direction = (direction + Vector3.up * _bombData.upwardsModifier).normalized;
                Vector3 forceVector = forceMultiplier * forceToApply * direction;
                
                // CẢI TIẾN: Sử dụng GetComponentInParent để tìm component IDamageable,
                // cho phép các bộ phận con (như ragdoll) có thể truyền sát thương lên đối tượng cha.
                var damageableComponent = hit.GetComponentInParent<IDamageable>();

                // Nếu tìm thấy một đối tượng có thể nhận sát thương và nó chưa được xử lý trong vụ nổ này...
                if (damageableComponent != null && processedDamageables.Add(damageableComponent))
                {
                    // Ưu tiên xử lý bằng IExplosionDamageable nếu có.
                    if (damageableComponent is IExplosionDamageable explosionDamageable)
                    {
                        explosionDamageable.TakeExplosionDamage(finalDamage, forceVector, closestPoint);
                    }
                    else // Nếu không, chỉ áp dụng sát thương thông thường và lực riêng biệt.
                    {
                        damageableComponent.TakeDamage(finalDamage);

                        // Áp dụng lực riêng vì đối tượng không có IExplosionDamageable.
                        if (_bombData.applyForce && hit.attachedRigidbody != null && !hit.attachedRigidbody.isKinematic)
                        {
                            if (hit.attachedRigidbody.TryGetComponent<IExplosionReactable>(out var explosionReactable))
                            {
                                explosionReactable.OnExplosionHit(forceVector, closestPoint);
                            }
                            else
                            {
                                hit.attachedRigidbody.AddForceAtPosition(forceVector, closestPoint, _bombData.forceMode);
                            }
                        }
                    }
                }
                // Nếu không tìm thấy đối tượng có thể nhận sát thương (ví dụ: một khối vật lý thông thường),
                // chỉ áp dụng lực.
                else if (damageableComponent == null)
                {
                    if (_bombData.applyForce && hit.attachedRigidbody != null && !hit.attachedRigidbody.isKinematic)
                    {
                        if (hit.attachedRigidbody.TryGetComponent<IExplosionReactable>(out var explosionReactable))
                        {
                            explosionReactable.OnExplosionHit(forceVector, closestPoint);
                        }
                        else
                        {
                            hit.attachedRigidbody.AddForceAtPosition(forceVector, closestPoint, _bombData.forceMode);
                        }
                    }
                }

                // Các hiệu ứng khác (trạng thái, phá hủy địa hình) được áp dụng riêng biệt.
                ApplyOtherExplosionEffects(hit, explosionCenter, Vector3.Distance(explosionCenter, hit.transform.position));
            }
        }

        /// <summary>
        /// Áp dụng các hiệu ứng phụ của vụ nổ như hiệu ứng trạng thái và phá hủy địa hình.
        /// Tách ra để giữ cho logic chính trong TriggerSingleExplosion gọn gàng hơn.
        /// </summary>
        private void ApplyOtherExplosionEffects(Collider hit, Vector3 explosionCenter, float distanceToBlockCenter)
        {
            // Hiệu ứng trạng thái cho đối tượng động
            if (_bombData.effect != StatusEffectType.None && (_bombData.statusEffectLayers.value & (1 << hit.gameObject.layer)) != 0)
            {
                if (hit.TryGetComponent<IStatusEffectable>(out var effectable))
                {
                    effectable.ApplyStatusEffect(_bombData.effect, _bombData.effectDuration);
                }
            }

            // --- PHÁ HỦY ĐỊA HÌNH ---
            // Nó sẽ tìm component DestructibleBlock trên các collider va chạm trong OverlapSphere.
            if (_bombData.canDestroyTerrain && hit.TryGetComponent<DestructibleBlock>(out var block))
            {
                // Chỉ phá hủy khối nếu TÂM của nó nằm trong bán kính vụ nổ.
                if (distanceToBlockCenter <= _bombData.radius)
                {
                    // Tính toán falloff dựa trên khoảng cách từ tâm vụ nổ đến tâm khối.
                    float normalizedDistanceToCenter = (_bombData.radius > 0f) ? Mathf.Clamp01(distanceToBlockCenter / _bombData.radius) : 0f;
                    float powerFloat = _bombData.terrainDestructionPowerFalloff.Evaluate(normalizedDistanceToCenter);
                    int powerInt = Mathf.RoundToInt(powerFloat);

                    block.ReceiveImpact(powerInt);
                }
            }
        }

        #endregion

        #region Editor Gizmos

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_bombData != null && _bombData.radius > 0)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawSphere(transform.position, _bombData.radius);
            }
        }
        #endif
        #endregion
    }
}