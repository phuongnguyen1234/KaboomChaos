using UnityEngine;
using Core.Interfaces;
using System.Collections;
using Core.Utilities;
using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Xử lý logic gameplay cho các hiệu ứng trạng thái (status effects).
    /// Component này nhận hiệu ứng, xử lý các tương tác, và ủy quyền
    /// việc thay đổi hình ảnh cho MaterialEffectController.
    /// </summary>
    [RequireComponent(typeof(MaterialEffectController))]
    [RequireComponent(typeof(Collider))]
    public class StatusEffectReceiver : MonoBehaviour, IStatusEffectable
    {
        /// <summary>
        /// Profile chứa thông tin về hiệu ứng hình ảnh (VFX) cho một loại hiệu ứng gameplay.
        /// </summary>
        [System.Serializable]
        public class GameplayEffectProfile
        {
            public StatusEffectType effectType;
            [Tooltip("Prefab của hiệu ứng particle sẽ được bật khi hiệu ứng này hoạt động. Sẽ được làm con của object này.")]
            public GameObject effectVFX;
        }

        [Header("Tương tác Hiệu ứng")]
        [Tooltip("Vật liệu vật lý sẽ được áp dụng khi đối tượng bị đóng băng.")]
        [SerializeField] private PhysicsMaterial _slipperyMaterial;

        [Header("Damage on Touch Settings")] // Giữ lại header
        [Tooltip("Sát thương mỗi lần khi người chơi chạm vào đối tượng đang Burning/Electrified.")]
        [SerializeField] private float _contactDamagePerTick = 5f; // Giữ lại trường này

        [Header("Player-Specific Settings")]
        [Tooltip("Thời gian (giây) người chơi bị đóng băng. Ghi đè thời gian mặc định của hiệu ứng.")]
        [SerializeField] private float _playerFrozenDuration = 8.0f;

        [Header("Obsidian Settings")]
        [Tooltip("Độ cứng (toughness) al khốiului Obsidian. Un vụ nổ cu destructionPower mai miciă decât aceasta valoare nu puede phá hủy khối obsidian. Mai mare = mai greu de phá hủy.")]
        [Min(1)]
        [SerializeField] private int _obsidianToughness = 6;

        [Header("Hiệu ứng Hình ảnh Gameplay")]
        [Tooltip("GameObject trực quan (ví dụ: khối băng) sẽ được bật khi người chơi bị đóng băng. Nên là một object con của player.")]
        [SerializeField] private GameObject _frozenBlockVisual;

        [Header("SFX")]
        [Tooltip("Âm thanh phát ra khi người chơi được rã đông.")]
        [SerializeField] private AudioClip _unfreezeSfx;

        [Header("Gameplay Effects VFX")]
        [SerializeField] private List<GameplayEffectProfile> _gameplayEffectProfiles = new();

        [Tooltip("Hiệu ứng trạng thái ban đầu của đối tượng này. Ví dụ: một khối Obsidian sẽ có Initial Effect là Obsidian.")]
        [SerializeField] private StatusEffectType _initialEffect = StatusEffectType.None;

        // Cached components
        private MaterialEffectController _materialEffectController;
        private DestructibleBlock _destructibleBlock;
        private DestructiblePart _destructiblePart;
        private IPlayer _player; // Thay thế PlayerController bằng IPlayer
        private Rigidbody _rigidbody;
        private IDamageable _damageable; // Thêm để kiểm tra trạng thái IsAlive một cách trừu tượng
        private AudioSource _audioSource;
        private IPlayerShieldController _shieldController;
        private Collider _objectCollider;

        // Trạng thái hiệu ứng
        public StatusEffectType CurrentEffect => _currentEffect;
        private StatusEffectType _currentEffect = StatusEffectType.None;

        /// <summary>Kiểm tra xem đối tượng có đang trong trạng thái "nóng" (cháy hoặc trong dung nham) hay không.</summary>
        public bool IsHot => _currentEffect == StatusEffectType.Burning || _isInLavaZone;
        private StatusEffectType _temporaryOverlayEffect = StatusEffectType.None; // Hiệu ứng tạm thời chồng lên hiệu ứng vĩnh viễn (ví dụ: Obsidian bị nhiễm điện)
        private Coroutine _statusEffectCoroutine; // Coroutine đang chạy cho hiệu ứng hiện tại (cả vĩnh viễn và tạm thời)
        
        // Trạng thái tạm thời để biết đối tượng có đang trong vùng dung nham không (chỉ áp dụng cho DestructibleBlock)
        private bool _isInLavaZone = false;

        // Cache lại các thuộc tính gốc để hoàn tác
        private PhysicsMaterial _originalPhysicMaterial;
        private int _originalToughness;
        private int _originalMaxHits;

        // Trạng thái của VFX
        private Dictionary<StatusEffectType, GameObject> _effectVFXMap;
        private GameObject _permanentVFXInstance;
        private GameObject _temporaryVFXInstance;

        private void Awake()
        {
            // Cache các component cần thiết để tối ưu hiệu năng.
            _materialEffectController = GetComponent<MaterialEffectController>();
            _destructibleBlock = GetComponent<DestructibleBlock>();
            _destructiblePart = GetComponent<DestructiblePart>();
            _player = GetComponent<IPlayer>(); // Lấy IPlayer thay vì PlayerController
            _rigidbody = GetComponent<Rigidbody>();
            _damageable = GetComponent<IDamageable>(); // Lấy component IDamageable
            _shieldController = GetComponent<IPlayerShieldController>();
            if (_shieldController == null)
            {
                _shieldController = GetComponentInParent<IPlayerShieldController>();
            }
            _objectCollider = GetComponent<Collider>();

            if (_objectCollider != null)
            {
                _originalPhysicMaterial = _objectCollider.sharedMaterial;
            }

            // Xây dựng dictionary từ các profile VFX để tra cứu nhanh.
            _effectVFXMap = new Dictionary<StatusEffectType, GameObject>();
            foreach (var profile in _gameplayEffectProfiles)
            {
                _effectVFXMap[profile.effectType] = profile.effectVFX;
            }

            // Khởi tạo hiệu ứng ban đầu nếu được thiết lập trong Inspector (ví dụ: một khối Obsidian có sẵn).
            if (_initialEffect != StatusEffectType.None)
            {
                SetPermanentEffect(_initialEffect);
            }

            // Đảm bảo khối băng bị tắt khi bắt đầu.
            if (_frozenBlockVisual != null)
            {
                _frozenBlockVisual.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // TỐI ƯU HÓA: Chỉ đăng ký một sự kiện dọn dẹp duy nhất để tránh gọi ResetState() hai lần.
            // Nếu component này nằm trên một người chơi, nó sẽ được reset bởi sự kiện dành riêng cho người chơi.
            if (_player != null)
            {
                GameEvents.OnRoundEndPlayerReset += ResetState;
            }
            // Nếu không, nó là một đối tượng trong màn chơi (như đá Obsidian) và sẽ được dọn dẹp
            // bởi sự kiện dọn dẹp chung.
            else
            {
                GameEvents.OnRoundEndCleanup += ResetState;
            }
        }

        private void OnDisable()
        {
            // Dừng coroutine khi object bị vô hiệu hóa để tránh lỗi.
            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
                _statusEffectCoroutine = null;
            }

            // QUAN TRỌNG: Việc dọn dẹp VFX (un-parent) không thể được thực hiện một cách an toàn bên trong OnDisable(),
            // vì nó được gọi trong khi parent (đối tượng này) đang bị vô hiệu hóa, gây ra lỗi của Unity.
            // Logic dọn dẹp đã được chuyển hoàn toàn vào phương thức PrepareForDespawn().
            // Bất kỳ hệ thống nào muốn vô hiệu hóa đối tượng này (ví dụ: trả về pool)
            // đều có TRÁCH NHIỆM gọi PrepareForDespawn() TRƯỚC KHI vô hiệu hóa nó.
            // Xem DestructibleBlock.cs để tham khảo cách triển khai đúng.

            // Hủy đăng ký để tránh lỗi
            if (_player != null)
            {
                GameEvents.OnRoundEndPlayerReset -= ResetState;
            }
            else
            {
                GameEvents.OnRoundEndCleanup -= ResetState;
            }
        }

        /// <summary>
        /// Chuẩn bị cho việc đối tượng bị vô hiệu hóa hoặc trả về pool.
        /// Dọn dẹp các tài nguyên (như VFX) một cách an toàn để tránh lỗi race condition.
        /// </summary>
        public void PrepareForDespawn()
        {
            RevertPermanentEffectVFX();
            RevertTemporaryVFX();
        }

        /// <summary>
        /// Đặt trạng thái cho biết đối tượng có đang trong vùng dung nham hay không.
        /// Chỉ có ý nghĩa cho DestructibleBlock để xử lý tương tác với Frozen.
        /// </summary>
        /// <param name="state">True nếu đang trong vùng dung nham, False nếu không.</param>
        public void SetIsInLavaZone(bool state)
        {
            _isInLavaZone = state;
        }
        /// <summary>
        /// Áp dụng một hiệu ứng trạng thái lên đối tượng.
        /// Xử lý các tương tác giữa các hiệu ứng và ghi đè hiệu ứng cũ nếu cần.
        /// </summary>
        /// <param name="newEffect">Loại hiệu ứng mới để áp dụng.</param>
        /// <param name="duration">Thời gian hiệu ứng tồn tại (giây).</param>
        public void ApplyStatusEffect(StatusEffectType newEffect, float duration)
        {
            if (newEffect == StatusEffectType.None) return;

            // --- LOGIC KHIEN ---
            if (_player != null && _shieldController == null)
            {
                _shieldController = _player.GameObject.GetComponent<IPlayerShieldController>() ?? _player.GameObject.GetComponentInParent<IPlayerShieldController>();
            }

            // Neu day la nguoi choi, kiem tra xem khien co chan hieu ung nay khong.
            if (_player != null && _shieldController != null && _shieldController.IsShieldActive)
            {
                if (_shieldController.ProcessStatusEffect(newEffect))
                {
                    return; // Khien da chan hieu ung.
                }
            }

            // Nếu đây là người chơi và hiệu ứng là Đóng băng, sử dụng thời gian đóng băng riêng.
            if (_player != null && newEffect == StatusEffectType.Frozen)
            {
                // PERK INTERCEPTION: Neu perk (vi du Anti-Freeze) chan hieu ung dong bang
                // (tra ve true tu TriggerQueryPlayerStatusEffectBlocked) thi bo qua hoan toan.
                if (GameEvents.TriggerQueryPlayerStatusEffectBlocked(_player, newEffect))
                {
                    return;
                }

                duration = _playerFrozenDuration;
            }


            // Quy tắc 2: Trạng thái Obsidian là vĩnh viễn.
            if (_currentEffect == StatusEffectType.Obsidian)
            {
                // Nếu đã là Obsidian, nó miễn nhiễm với Lửa và Băng.
                if (newEffect == StatusEffectType.Burning || newEffect == StatusEffectType.Frozen)
                {
                    // SỬA LỖI: Mặc dù Obsidian miễn nhiễm với hiệu ứng mới (Lửa/Băng),
                    // chúng ta vẫn cần dọn dẹp bất kỳ hiệu ứng tạm thời nào đang có trên nó
                    // (ví dụ: nếu nó đang bị nhiễm điện).
                    if (_temporaryOverlayEffect != StatusEffectType.None)
                    {
                        // SỬA LỖI: Dừng coroutine đang chạy của hiệu ứng tạm thời TRƯỚC KHI hoàn tác.
                        // Nếu không, coroutine cũ sẽ trở thành "zombie", tiếp tục chạy ngầm và
                        // gọi RevertTemporaryOverlayEffect() một lần nữa khi hết giờ, gây ra
                        // các hành vi không mong muốn và khó lường.
                        if (_statusEffectCoroutine != null)
                        {
                            StopCoroutine(_statusEffectCoroutine);
                        }
                        RevertTemporaryOverlayEffect();
                    }
                    return; // Ignore the new effect
                }
                // Special case: Obsidian can be Electrified visually, but its core state remains Obsidian.
                if (newEffect == StatusEffectType.Electrified)
                {
                    // Apply a temporary visual AND gameplay effect (contact damage)
                    // without changing the permanent Obsidian state.
                    StartTemporaryOverlayEffect(newEffect, duration);
                    return; // Handled, exit
                }
            }

            // --- 1. Xử lý tương tác hiệu ứng ---
            if (_currentEffect != StatusEffectType.None)
            {
                // Tương tác: Lửa + Băng -> Obsidian.
                // Quy tắc 1: Chỉ có DestructibleBlock mới hóa Obsidian. DestructiblePart sẽ bị phá hủy.
                // Cập nhật logic: Nếu là DestructibleBlock, kiểm tra _isInLavaZone thay vì _currentEffect == Burning.
                if ((_currentEffect == StatusEffectType.Burning || (_destructibleBlock != null && _isInLavaZone)) && newEffect == StatusEffectType.Frozen)
                {
                    // Chỉ có khối mới hóa Obsidian. Người chơi sẽ không bị ảnh hưởng bởi tương tác này.
                    if (_destructibleBlock != null && _player == null)
                    {
                        TurnToObsidian();
                    }
                    else
                    {
                        // Nếu là DestructiblePart hoặc đối tượng khác, nó sẽ bị phá hủy.
                        DestroyWithImpact();
                    }
                    return; 
                }
                // Tương tác: Băng + Lửa -> Phá hủy
                // LOGIC MỚI: Nếu người chơi bị đóng băng và trúng hiệu ứng Lửa, họ sẽ được rã đông.
                // Các đối tượng khác sẽ bị phá hủy.
                else if (_currentEffect == StatusEffectType.Frozen && newEffect == StatusEffectType.Burning)
                {
                    if (_player != null) RevertAllEffects(); // Rã đông người chơi
                    else DestroyWithImpact(); // Phá hủy các đối tượng khác
                    return; 
                } else // Nếu không có tương tác đặc biệt, hiệu ứng mới sẽ ghi đè lên hiệu ứng cũ.
                {
                    if (_statusEffectCoroutine != null)
                    {
                        StopCoroutine(_statusEffectCoroutine);

                        // SỬA LỖI: Nếu hiệu ứng mới giống hệt hiệu ứng cũ (ví dụ: liên tục đứng trong dung nham),
                        // chúng ta chỉ muốn "làm mới" thời gian tồn tại của nó. Việc gọi RevertAllEffects()
                        // sẽ xóa hiệu ứng hình ảnh và gây ra hiện tượng nhấp nháy khi nó được áp dụng lại.
                        // Thay vào đó, chúng ta chỉ hoàn tác nếu hiệu ứng mới là một hiệu ứng *khác*.
                        if (_currentEffect != newEffect)
                        {
                            RevertAllEffects();
                        }
                    }
                }
            }

            // --- 2. Áp dụng hiệu ứng mới ---
            _statusEffectCoroutine = StartCoroutine(StatusEffectRoutine(newEffect, duration));
        }

        /// <summary>
        /// Rã đong player imediat: gos bo hiêu ung dogng bang daca player dang dogng bang.
        /// Dung cand player nhan sat thuong sau cand player chit sau khi round ket thuc (het gio),
        /// de garant ca player nu se ramaine blocat si duoc adu sau lai lobby.
        /// </summary>
        /// <returns>True neu hiêu ung dogng bang a bat gos bo (da rã đong), false neu player nu era dogng bang.</returns>
        public bool UnfreezePlayer()
        {
            if (_player == null || _currentEffect != StatusEffectType.Frozen) return false;

            RevertAllEffects();
            return true;
        }
        
        /// <summary>
        /// Bắt đầu một hiệu ứng tạm thời (hình ảnh và gameplay) chồng lên một hiệu ứng vĩnh viễn (như Obsidian).
        /// </summary>
        private void StartTemporaryOverlayEffect(StatusEffectType effect, float duration)
        {
            // Dừng bất kỳ hiệu ứng tạm thời nào đang chạy.
            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
                RevertTemporaryOverlayEffect();
            }
            _statusEffectCoroutine = StartCoroutine(ApplyTemporaryOverlayEffectRoutine(effect, duration));
        }

        /// <summary>
        /// Coroutine chính để áp dụng một hiệu ứng có thời hạn.
        /// </summary>
        private IEnumerator StatusEffectRoutine(StatusEffectType effect, float duration)
        {
            // Chỉ áp dụng lại hình ảnh và thuộc tính gameplay nếu đây là một hiệu ứng mới,
            // hoặc nếu hiệu ứng hiện tại là None.
            bool isEffectChanging = (_currentEffect != effect);
            _currentEffect = effect;

            if (isEffectChanging)
            {
                ApplyGameplayProperties(effect);
                ApplyEffectMaterialWithRandomOffset(effect);
                ApplyPermanentEffectVFX(effect);
            }

            yield return new WaitForSeconds(duration);

            // Nếu coroutine này vẫn còn chạy (chưa bị một hiệu ứng khác ngắt), hoàn tác lại.
            // Chỉ hoàn tác nếu hiệu ứng hiện tại vẫn là hiệu ứng này.
            // Điều này ngăn chặn việc hoàn tác sai nếu một hiệu ứng khác đã được áp dụng trong khi coroutine này đang chạy.
            if (_currentEffect == effect)
            {
                RevertAllEffects();
            }
        }

        /// <summary>
        /// Coroutine chỉ áp dụng hiệu ứng tạm thời (hình ảnh, gameplay) lên một trạng thái vĩnh viễn.
        /// </summary>
        private IEnumerator ApplyTemporaryOverlayEffectRoutine(StatusEffectType effect, float duration)
        {
            _temporaryOverlayEffect = effect;
            ApplyEffectMaterialWithRandomOffset(effect);

            // --- VFX Logic for Temporary Effect ---
            if (_effectVFXMap.TryGetValue(effect, out GameObject vfxPrefab) && vfxPrefab != null)
            {
                _temporaryVFXInstance = GameEvents.TriggerVFXSpawnRequest(vfxPrefab, transform.position, transform.rotation);
                if (_temporaryVFXInstance != null)
                {
                    _temporaryVFXInstance.transform.SetParent(transform, true); // Use world position stay
                    ConfigureParticleSystemShape(_temporaryVFXInstance); // Cấu hình hình dạng phát hạt
                }
            }
            // --- End VFX Logic ---

            yield return new WaitForSeconds(duration);

            RevertTemporaryOverlayEffect();
        }

        /// <summary>
        /// Hoàn tác hiệu ứng tạm thời và khôi phục lại hình ảnh của hiệu ứng vĩnh viễn bên dưới.
        /// </summary>
        private void RevertTemporaryOverlayEffect()
        {
            RevertTemporaryVFX();

            // Hoàn tác lại material của hiệu ứng vĩnh viễn bên dưới.
            ApplyEffectMaterialWithRandomOffset(_currentEffect);
            _temporaryOverlayEffect = StatusEffectType.None;
            _statusEffectCoroutine = null; // Đảm bảo coroutine được xóa khi hoàn tất.
        }

        /// <summary>
        /// Hoàn tác tất cả các hiệu ứng (cả gameplay và hình ảnh) về trạng thái ban đầu.
        /// </summary>
        private void RevertAllEffects()
        {
            if (_currentEffect == StatusEffectType.None) return;

            // Chỉ hoàn tác các thuộc tính gameplay nếu hiệu ứng hiện tại không phải là Obsidian.
            // Obsidian là trạng thái vĩnh viễn và không bị hoàn tác.
            if (_currentEffect != StatusEffectType.Obsidian)
            {
                RevertGameplayProperties();
            }
            
            _materialEffectController.RevertEffectMaterial();
            RevertPermanentEffectVFX();
            RevertTemporaryVFX();
            _statusEffectCoroutine = null;
            _currentEffect = StatusEffectType.None; // Reset current effect for non-permanent effects.
        }

        /// <summary>
        /// Áp dụng các thay đổi về thuộc tính gameplay (vật lý, độ cứng) cho một hiệu ứng.
        /// </summary>
        private void ApplyGameplayProperties(StatusEffectType effect)
        {
            if (effect == StatusEffectType.Frozen)
            {
                if (_objectCollider != null && _slipperyMaterial != null)
                {
                    _objectCollider.sharedMaterial = _slipperyMaterial;
                }

                if (_destructibleBlock != null)
                {
                    _originalToughness = _destructibleBlock.Toughness;
                    _destructibleBlock.Toughness = 99;
                }
                if (_destructiblePart != null)
                {
                    _originalMaxHits = _destructiblePart.MaxHits;
                    _destructiblePart.MaxHits = 99;
                }

                // LOGIC ĐÓNG BĂNG NGƯỜI CHƠI
                // Yêu cầu: Bật khối băng, đóng băng animation, đứng yên, không nhận input.
                if (_player != null)
                {
                    // Trực tiếp đóng băng vật lý và vô hiệu hóa controller
                    // thay vì dựa vào event trong PlayerController.
                    _player.SetMovementEnabled(false);
                    if (_rigidbody != null) _rigidbody.isKinematic = true;

                    // Phát sự kiện để các component khác (như PlayerAnimator) vẫn có thể phản ứng.
                    GameEvents.TriggerPlayerStatusEffectApplied(_player, effect);
                }

                // Bật khối băng trực quan (logic này vẫn do StatusEffectReceiver quản lý).
                if (_frozenBlockVisual != null)
                {
                    _frozenBlockVisual.SetActive(true);
                }
            } else if (effect == StatusEffectType.Obsidian)
            {
                // Obsidian's properties are set permanently.
                // We only set them if it's a DestructibleBlock.
                if (_destructibleBlock != null)
                {
                    _originalToughness = _destructibleBlock.Toughness;
                    _destructibleBlock.Toughness = _obsidianToughness; // Obsidian acum puede fi phá hủy, doar număi cu suficiente sức phá hủy
                }
                if (_destructiblePart != null)
                {
                    // Although pieces shouldn't become Obsidian, this is a safeguard.
                    _originalMaxHits = _destructiblePart.MaxHits;
                    _destructiblePart.MaxHits = 10;
                }
            }
        }

        /// <summary>
        /// Hoàn tác các thay đổi về thuộc tính gameplay về giá trị gốc.
        /// </summary>
        private void RevertGameplayProperties()
        {
            if (_currentEffect == StatusEffectType.Frozen)
            {
                if (_objectCollider != null)
                {
                    _objectCollider.sharedMaterial = _originalPhysicMaterial;
                }

                if (_destructibleBlock != null)
                {
                    _destructibleBlock.Toughness = _originalToughness;
                }
                // Only revert MaxHits if it was a DestructiblePart that became Frozen.
                // This is a bit tricky because DestructiblePart shouldn't become Obsidian.
                // But if it did, we wouldn't revert its MaxHits.
                // Given the new rule, DestructiblePart will be destroyed if Burning + Frozen.
                // So this part for DestructiblePart might not be strictly necessary for Frozen.
                if (_destructiblePart != null)
                {
                    _destructiblePart.MaxHits = _originalMaxHits;
                }

                // LOGIC GIẢI BĂNG NGƯỜI CHƠI
                // Hoàn tác lại các thay đổi của hiệu ứng đóng băng
                if (_player != null)
                {
                    // Phát âm thanh rã đông
                    // CHỈ phát âm thanh nếu đối tượng còn sống.
                    // Tránh phát âm thanh khi người chơi đã chết và đang được reset.
                    // Sử dụng interface IDamageable để tránh phụ thuộc trực tiếp vào PlayerHealth.
                    if (_unfreezeSfx != null && _damageable.IsAlive && SfxService.Instance != null)
                    {
                        SfxService.Instance.PlaySfx(_unfreezeSfx, transform.position);
                    }

                    // Luôn khôi phục trạng thái di chuyển bình thường sau khi rã đông,
                    // bỏ qua bất kỳ hiệu ứng ragdoll nào có thể đã xảy ra trong khi bị đóng băng.
                    if (_rigidbody != null) _rigidbody.isKinematic = false;
                    _player.SetMovementEnabled(true);

                    // Phát sự kiện để các component khác (như PlayerAnimator) tự hoàn tác.
                    GameEvents.TriggerPlayerStatusEffectReverted(_player, _currentEffect); // Animator vẫn cần sự kiện này
                }

                // Tắt khối băng trực quan
                if (_frozenBlockVisual != null)
                {
                    _frozenBlockVisual.SetActive(false);
                }
            }
            // Obsidian không bị hoàn tác theo thời gian.
            // If _currentEffect is Obsidian, we do nothing here as its properties are permanent.
        }

        /// <summary>
        /// Thiết lập một hiệu ứng trạng thái vĩnh viễn cho đối tượng (ví dụ: Obsidian).
        /// </summary>
        public void SetPermanentEffect(StatusEffectType effect)
        {
            _currentEffect = effect;
            ApplyGameplayProperties(effect);
            ApplyEffectMaterialWithRandomOffset(effect);
            ApplyPermanentEffectVFX(effect);
        }

        /// <summary>
        /// Reset lại trạng thái của component này, hoàn tác tất cả các hiệu ứng đang có.
        /// Thường được gọi bởi một hệ thống quản lý bên ngoài (ví dụ: qua SendMessage) khi cần reset toàn bộ đối tượng.
        /// </summary>
        public void ResetState()
        {
            // 1. Dừng mọi coroutine đang chạy để ngăn xung đột hoặc ghi đè trạng thái.
            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
                _statusEffectCoroutine = null;
            }

            // 2. Hoàn tác mọi hiệu ứng tạm thời và thuộc tính đã thay đổi.
            RevertTemporaryOverlayEffect(); // Dọn dẹp hiệu ứng tạm (như Electrified trên Obsidian).
            RevertAllEffects();             // Dọn dẹp hiệu ứng chính (như Burning).

            // 3. Áp dụng lại hiệu ứng ban đầu nếu đối tượng này có một trạng thái mặc định.
            if (_initialEffect != StatusEffectType.None)
            {
                SetPermanentEffect(_initialEffect);
            }
        }

        /// <summary>
        /// Áp dụng hiệu ứng particle (VFX) cho một hiệu ứng vĩnh viễn.
        /// </summary>
        private void ApplyPermanentEffectVFX(StatusEffectType effect)
        {
            // Clean up old VFX if any
            RevertPermanentEffectVFX();

            if (_effectVFXMap.TryGetValue(effect, out GameObject vfxPrefab) && vfxPrefab != null)
            {
                _permanentVFXInstance = GameEvents.TriggerVFXSpawnRequest(vfxPrefab, transform.position, transform.rotation);
                if (_permanentVFXInstance == null && vfxPrefab != null) // Fallback if no pool manager
                {
                    _permanentVFXInstance = Instantiate(vfxPrefab, transform.position, transform.rotation);
                }
                if (_permanentVFXInstance != null)
                {
                    _permanentVFXInstance.transform.SetParent(transform);
                    ConfigureParticleSystemShape(_permanentVFXInstance); // Cấu hình hình dạng phát hạt
                }
            }
        }

        /// <summary>
        /// Dọn dẹp và trả về pool hiệu ứng particle vĩnh viễn đang hoạt động.
        /// </summary>
        private void RevertPermanentEffectVFX()
        {
            if (_permanentVFXInstance != null)
            {
                // Quan trọng: Tách VFX ra khỏi đối tượng cha TRƯỚC KHI yêu cầu trả về pool.
                // Điều này ngăn lỗi "Cannot set the parent" khi đối tượng cha này cũng đang bị vô hiệu hóa trong cùng một frame.
                _permanentVFXInstance.transform.SetParent(null);

            // CẢI TIẾN: Thêm bước kiểm tra để đảm bảo có một pool manager đang lắng nghe.
            // Nếu không, các VFX sẽ bị "mồ côi" trong scene, gây ra lỗi mà người dùng báo cáo
            // (VFX còn active, ở scale 1, tại một vị trí bất kỳ).
            if (GameEvents.IsVFXPoolListening())
            {
                GameEvents.TriggerVFXDespawnRequest(_permanentVFXInstance);
            }
            else
            {
                // Fallback: Nếu không có pool, tự hủy để tránh rò rỉ.
                Destroy(_permanentVFXInstance);
            }
                _permanentVFXInstance = null;
            }
        }

        /// <summary>
        /// Dọn dẹp và trả về pool hiệu ứng particle tạm thời đang hoạt động.
        /// </summary>
        private void RevertTemporaryVFX()
        {
            if (_temporaryVFXInstance == null) return;

            // Tương tự như RevertPermanentEffectVFX, unparent trước khi despawn.
            _temporaryVFXInstance.transform.SetParent(null);
        if (GameEvents.IsVFXPoolListening())
        {
            GameEvents.TriggerVFXDespawnRequest(_temporaryVFXInstance);
        }
        else
        {
            Destroy(_temporaryVFXInstance);
        }
            _temporaryVFXInstance = null;
        }

        /// <summary>
        /// Cấu hình module Shape của Particle System để phát hạt từ bề mặt của object này.
        /// </summary>
        /// <param name="vfxInstance">GameObject chứa ParticleSystem cần cấu hình.</param>
        private void ConfigureParticleSystemShape(GameObject vfxInstance)
        {
            if (vfxInstance == null) return;

            if (!vfxInstance.TryGetComponent<ParticleSystem>(out var ps))
            {
                Debug.LogWarning($"VFX prefab '{vfxInstance.name}' không có component ParticleSystem.", vfxInstance);
                return;
            }

            // Ủy quyền việc cấu hình cho lớp tiện ích.
            // 'gameObject' ở đây chính là đối tượng có StatusEffectReceiver (khối, người chơi, v.v.)
            // và sẽ được dùng làm nguồn hình dạng.
            // Yêu cầu phát hạt từ bề mặt (surface) của mesh/collider.
            ParticleSystemUtils.MatchShapeToSurface(ps, gameObject);
        }

        /// <summary>
        /// Chuyển đối tượng này thành một khối Obsidian.
        /// </summary>
        private void TurnToObsidian()
        {
            // Dừng bất kỳ coroutine hiệu ứng nào đang chạy (ví dụ: coroutine của hiệu ứng Burning).
            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
                _statusEffectCoroutine = null;
            }

            // Hoàn tác các thuộc tính và hình ảnh của hiệu ứng cũ (Burning)
            RevertGameplayProperties();
            _materialEffectController.RevertEffectMaterial();
            // Dọn dẹp VFX TRƯỚC KHI đối tượng được biến đổi để tránh lỗi cha-con.
            RevertPermanentEffectVFX();
            RevertTemporaryVFX();

            // Bây giờ, áp dụng hiệu ứng Obsidian vĩnh viễn tại chỗ.
            // Phương thức này sẽ thay đổi _currentEffect, áp dụng material và thuộc tính gameplay mới.
            SetPermanentEffect(StatusEffectType.Obsidian);
        }

        /// <summary>
        /// Phá hủy đối tượng bằng cách mô phỏng một tác động cực mạnh.
        /// </summary>
        private void DestroyWithImpact()
        {
            // Dọn dẹp VFX TRƯỚC KHI đối tượng bị phá hủy/trả về pool để tránh lỗi cha-con.
            RevertPermanentEffectVFX();
            RevertTemporaryVFX();
            if (_destructibleBlock != null)
            {
                _destructibleBlock.ReceiveImpact(9999);
            }
            else if (_destructiblePart != null)
            {
                gameObject.SetActive(false); // Destroy DestructiblePart
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Trình bao bọc để áp dụng material hiệu ứng đồng thời gán một giá trị ngẫu nhiên cho thuộc tính 'PulseOffset' của shader.
        /// </summary>
        /// <param name="effect">Hiệu ứng để áp dụng.</param>
        private void ApplyEffectMaterialWithRandomOffset(StatusEffectType effect)
        {
            // Chỉ áp dụng offset ngẫu nhiên cho các hiệu ứng có shader 'pulse' (lửa, điện).
            if (effect == StatusEffectType.Burning || effect == StatusEffectType.Electrified)
            {
                // Gán một giá trị ngẫu nhiên lớn để dễ dàng thấy sự khác biệt giữa các khối.
                float randomOffset = Random.Range(0f, 100f);
                _materialEffectController.ApplyEffectMaterial(effect, (materialInstance) => {
                    if (materialInstance.HasProperty("_PulseOffset"))
                    {
                        materialInstance.SetFloat("_PulseOffset", randomOffset);
                    }
                });
            }
            else
            {
                // Đối với các hiệu ứng khác, chỉ cần áp dụng material mà không cần tùy chỉnh.
                _materialEffectController.ApplyEffectMaterial(effect);
            }
        }

        #region Contact Damage

        private void OnCollisionStay(Collision collision)
        {
            HandleContact(collision.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            HandleContact(other.gameObject);
        }

        /// <summary>
        /// Xử lý logic gây sát thương khi có đối tượng va chạm.
        /// </summary>
        /// <param name="contactObject">Đối tượng đã va chạm.</param>
        private void HandleContact(GameObject contactObject)
        {
            // Xác định hiệu ứng nào đang hoạt động và có gây sát thương khi chạm không.
            StatusEffectType damagingEffect = StatusEffectType.None;
            if (_currentEffect == StatusEffectType.Burning || _currentEffect == StatusEffectType.Electrified)
            {
                damagingEffect = _currentEffect;
            }
            // Hiệu ứng tạm thời (ví dụ Obsidian bị nhiễm điện) cũng có thể gây sát thương.
            else if (_temporaryOverlayEffect == StatusEffectType.Burning || _temporaryOverlayEffect == StatusEffectType.Electrified)
            {
                damagingEffect = _temporaryOverlayEffect;
            }

            // Nếu không có hiệu ứng gây sát thương nào đang hoạt động, thoát.
            if (damagingEffect == StatusEffectType.None) return;

            // Tìm component có thể nhận sát thương trên đối tượng va chạm.
            // Sử dụng GetComponentInParent để xử lý trường hợp va chạm với một bộ phận con của player (ví dụ: ragdoll).
            IDamageable damageable = contactObject.GetComponentInParent<IDamageable>();
            if (damageable == null) return;

            // Gọi TakeDamage, truyền cả context của hiệu ứng (damagingEffect).
            // PlayerHealth sẽ sử dụng context này để áp dụng thời gian miễn nhiễm cho đúng loại hiệu ứng,
            // tránh việc nhận sát thương mỗi frame.
            damageable.TakeDamage(_contactDamagePerTick, DamageSourceType.StatusEffectContact, damagingEffect);
        }

        #endregion
    }
}

