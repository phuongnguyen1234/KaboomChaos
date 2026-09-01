using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Core.Interfaces;

/// <summary>
/// Quản lý việc ghi đè các thuộc tính của material (như màu và alpha) một cách linh hoạt.
/// Hỗ trợ các hiệu ứng tạm thời như nháy màu (pulse) thông qua DOTween.
/// </summary>
[ExecuteAlways]
[SelectionBase]
public class MaterialEffectController : MonoBehaviour
{
    [System.Serializable]
    public class StatusEffectProfile
    {
        public StatusEffectType effectType;
        [Tooltip("Material sử dụng shader hiệu ứng (ví dụ: EffectPulse). Texture gốc sẽ được tự động gán vào thuộc tính '_BaseMap'.")]
        public Material effectMaterial;
        [Tooltip("Bật để sử dụng texture có sẵn trên 'Effect Material' thay vì lấy texture từ material gốc của đối tượng.")]
        public bool UseEffectTexture = false;
    }

    [Header("Màu tùy chỉnh")]
    [Tooltip("Bật để script này ghi đè các thuộc tính cơ bản (màu, alpha, emission) của material. Tắt tính năng này nếu bạn muốn giữ lại các thuộc tính gốc của material nhưng vẫn muốn sử dụng các hiệu ứng trạng thái (status effect) hoặc pulse màu.")]
    [SerializeField] private bool _applyMaterialOverrides = true;

    [SerializeField, ColorUsage(true, true)]
    [Tooltip("Màu nền mặc định. Shader cần có thuộc tính '_BaseColor'.")]
    private Color _customBaseColor = Color.white;

    [SerializeField, ColorUsage(true, true)]
    [Tooltip("Màu họa tiết mặc định. Shader cần có thuộc tính '_DecalColor'.")]
    private Color _customDecalColor = Color.clear;

    [Header("Alpha tùy chỉnh")]
    [SerializeField, Range(0f, 1f)]
    [Tooltip("Giá trị alpha sẽ được áp dụng cho object này.")]
    private float _customAlpha = 1.0f;

    [Header("Emission")]
    [SerializeField, Range(0, 5)]
    [Tooltip("Cường độ phát sáng của material. Yêu cầu shader có thuộc tính '_EmissionIntensity'.")]
    private float _customEmissionIntensity = 0;

    [Header("Fresnel Power")]
    [SerializeField, Range(0, 20)]
    [Tooltip("Độ mạnh hiệu ứng Fresnel")]
    private float _customFresnelPower = 1f;

    [Header("Border thickness")]
    [SerializeField, Range(0, 1)]
    [Tooltip("Độ dày viền")]
    private float _customThickness = 0.6f;

    [Header("Hiệu ứng Trạng thái")]
    [Tooltip("Danh sách các material sẽ được áp dụng cho các hiệu ứng trạng thái.")]
    [SerializeField] private List<StatusEffectProfile> _statusEffectProfiles = new();

    private const string BASE_COLOR_PROPERTY_NAME = "_BaseColor";
    private const string DECAL_COLOR_PROPERTY_NAME = "_DecalColor";
    private const string ALPHA_PROPERTY_NAME = "_Alpha";
    private const string EMISSION_INTENSITY_PROPERTY_NAME = "_EmissionIntensity";
    private const string FRESNEL_POWER_PROPERTY_NAME = "_FresnelPower";
    private const string THICKNESS_PROPERTY_NAME = "_Thickness";
    private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseMap");

    private MaterialPropertyBlock _propBlock;
    private Renderer _myRenderer;

    // Trạng thái runtime cho việc ghi đè màu nền
    private bool _isBaseColorOverridden = false;
    private Color _runtimeBaseColor;
    private Tweener _baseColorTween;

    // Trạng thái runtime cho việc ghi đè màu ngòi nổ (fuse)
    private bool _isFuseColorOverridden = false;
    private Color _runtimeFuseColor;

    // Trạng thái runtime cho việc ghi đè màu họa tiết
    private bool _isDecalColorOverridden = false;
    private Color _runtimeDecalColor;
    private Tweener _decalColorTween;

    // Trạng thái runtime cho hiệu ứng trạng thái
    private Material[] _originalMaterials;
    private Coroutine _statusEffectCoroutine;
    private Dictionary<StatusEffectType, StatusEffectProfile> _statusEffectProfileMap;
    private bool _isEffectActive = false;

    // CẢI TIẾN: Cache các material hiệu ứng đã được tạo ra để tái sử dụng.
    private Dictionary<StatusEffectType, Material[]> _cachedEffectMaterialInstances;

    /// <summary>
    /// Cho phép truy cập và thay đổi giá trị alpha tùy chỉnh từ các script khác.
    /// Việc gán giá trị mới sẽ tự động cập nhật material của object.
    /// </summary>
    public float CustomAlpha
    {
        get => _customAlpha;
        set {
            _customAlpha = Mathf.Clamp01(value);
            UpdateColor();
        }
    }

    /// <summary>
    /// Cho phép truy cập và thay đổi màu nền tùy chỉnh từ các script khác.
    /// Việc gán giá trị mới sẽ tự động cập nhật material của object.
    /// </summary>
    public Color CustomBaseColor
    {
        get => _customBaseColor;
        set
        {
            _customBaseColor = value;
            UpdateColor();
        }
    }

    /// <summary>
    /// Tạo hiệu ứng nháy màu nền mượt mà.
    /// </summary>
    /// <param name="targetColor">Màu nền để nháy đến.</param>
    /// <param name="duration">Tổng thời gian của hiệu ứng.</param>
    public void PulseBaseColor(Color targetColor, float duration)
    {
        _baseColorTween?.Kill();

        // Màu sẽ được tween, bắt đầu từ màu gốc của component
        Color animatedColor = _customBaseColor;
        _isBaseColorOverridden = true;

        _baseColorTween = DOTween.To(() => animatedColor, c => animatedColor = c, targetColor, duration / 2f)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .OnUpdate(() =>
            {
                _runtimeBaseColor = animatedColor;
                UpdateColor();
            })
            .OnComplete(() =>
            {
                _isBaseColorOverridden = false;
                UpdateColor();
            });
    }

    /// <summary>
    /// Tạo hiệu ứng nháy màu họa tiết mượt mà.
    /// </summary>
    /// <param name="targetColor">Màu họa tiết để nháy đến.</param>
    /// <param name="duration">Tổng thời gian của hiệu ứng.</param>
    public void PulseDecalColor(Color targetColor, float duration)
    {
        _decalColorTween?.Kill();

        // Màu sẽ được tween, bắt đầu từ màu gốc của component
        Color animatedColor = _customDecalColor;
        _isDecalColorOverridden = true;

        _decalColorTween = DOTween.To(() => animatedColor, c => animatedColor = c, targetColor, duration / 2f)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .OnUpdate(() =>
            {
                _runtimeDecalColor = animatedColor;
                UpdateColor();
            })
            .OnComplete(() =>
            {
                _isDecalColorOverridden = false;
                UpdateColor();
            });
    }

    /// <summary>
    /// Thiết lập một màu nền cố định cho trạng thái ngòi nổ (fuse).
    /// Màu này sẽ ghi đè lên màu tùy chỉnh và các hiệu ứng pulse.
    /// </summary>
    /// <param name="color">Màu để áp dụng.</param>
    public void SetFuseColor(Color color)
    {
        _isFuseColorOverridden = true;
        _runtimeFuseColor = color;
        UpdateColor();
    }

    /// <summary>
    /// Xóa bỏ màu nền cố định của trạng thái ngòi nổ.
    /// </summary>
    public void ClearFuseColor()
    {
        _isFuseColorOverridden = false;
        UpdateColor();
    }

    #region Status Effects
    /// <summary>
    /// Áp dụng một hiệu ứng trạng thái lên đối tượng bằng cách thay thế material.
    /// Chỉ xử lý phần hình ảnh.
    /// </summary>
    public void ApplyEffectMaterial(StatusEffectType effect, System.Action<Material> onMaterialInstanced = null)
    {
        if (_myRenderer == null) return;

        // Dừng hiệu ứng hình ảnh cũ nếu có
        if (_statusEffectCoroutine != null)
        {
            StopCoroutine(_statusEffectCoroutine);
            RevertEffectMaterial();
        }

        // Nếu hiệu ứng là None, chỉ cần hoàn tác và dừng lại.
        if (effect == StatusEffectType.None)
        {
            RevertEffectMaterial();
            return;
        }

        if (!_statusEffectProfileMap.TryGetValue(effect, out StatusEffectProfile effectProfile) || effectProfile.effectMaterial == null)
        {
            // Bỏ qua nếu hiệu ứng không có profile (ví dụ: Obsidian là kết quả, không phải hiệu ứng được áp dụng)
            if (effect != StatusEffectType.Obsidian)
                Debug.LogWarning($"Không tìm thấy profile cho hiệu ứng '{effect}' trên '{gameObject.name}'.", this);
            return;
        }

        _isEffectActive = true;
        _myRenderer.SetPropertyBlock(null); // Xóa property block để material mới hiển thị đúng

        Material[] effectMaterialsToApply;

        // CẢI TIẾN: Kiểm tra xem đã cache material cho hiệu ứng này chưa.
        if (_cachedEffectMaterialInstances.TryGetValue(effect, out effectMaterialsToApply))
        {
            // Đã có, tái sử dụng trực tiếp.
            // Gọi callback để cho phép tùy chỉnh (ví dụ: gán lại offset ngẫu nhiên).
            // Vì chúng ta đang dùng lại cùng một instance, việc gọi callback này rất quan trọng.
            if (onMaterialInstanced != null)
            {
                foreach (var mat in effectMaterialsToApply)
                {
                    onMaterialInstanced.Invoke(mat);
                }
            }
        }
        else // Chưa có trong cache, tạo mới.
        {
            effectMaterialsToApply = new Material[_originalMaterials.Length];
        for (int i = 0; i < _originalMaterials.Length; i++)
        {
            // Tạo một instance mới của material hiệu ứng
            var effectInstance = new Material(effectProfile.effectMaterial)
            {
                name = $"{effectProfile.effectMaterial.name} (Instance)"
            };

            // Nếu không được cấu hình để dùng texture của hiệu ứng, thì mới lấy texture gốc của object.
            if (!effectProfile.UseEffectTexture)
            {
                // Lấy texture gốc từ material gốc tương ứng
                Texture originalTexture = null;
                // CẢI TIẾN: Thay vì dùng .mainTexture (chỉ tìm _MainTex),
                // ta chủ động tìm các tên phổ biến để tương thích với nhiều shader hơn.
                // "_BaseMap" là tên mặc định trong URP Lit Shader Graph.
                if (_originalMaterials[i].HasProperty(BaseMapProperty))
                {
                    originalTexture = _originalMaterials[i].GetTexture(BaseMapProperty);
                }
                // "_MainTex" là tên phổ biến trong các shader cũ và Built-in RP.
                else if (_originalMaterials[i].HasProperty("_MainTex"))
                {
                    originalTexture = _originalMaterials[i].GetTexture("_MainTex");
                }

                // Gán texture gốc vào thuộc tính '_BaseMap' của shader hiệu ứng
                if (originalTexture != null)
                {
                    effectInstance.SetTexture(BaseMapProperty, originalTexture);
                }
            }
            // Nếu UseEffectTexture là true, material sẽ tự động dùng texture đã được gán sẵn trên nó.

            // Gọi callback để cho phép tùy chỉnh material instance trước khi áp dụng.
            onMaterialInstanced?.Invoke(effectInstance);

                effectMaterialsToApply[i] = effectInstance;
        }

            // Lưu vào cache để dùng cho các lần sau.
            _cachedEffectMaterialInstances[effect] = effectMaterialsToApply;
        }

        // Áp dụng các material hiệu ứng
        _myRenderer.materials = effectMaterialsToApply;
    }

    /// <summary>
    /// Hoàn tác material trực quan về trạng thái gốc.
    /// </summary>
    public void RevertEffectMaterial()
    {
        if (!_isEffectActive || _myRenderer == null) return;

        // Hoàn tác lại material gốc
        // CẢI TIẾN: Sử dụng sharedMaterials để hoàn tác.
        // Điều này tránh việc Unity tự động tạo ra các bản sao material không cần thiết
        // và đảm bảo renderer quay về đúng trạng thái sử dụng material gốc (asset).
        _myRenderer.sharedMaterials = _originalMaterials;

        // Việc dọn dẹp các material đã được cache sẽ được thực hiện trong OnDestroy.

        _isEffectActive = false;
        UpdateColor(); // Áp dụng lại các giá trị từ MaterialPropertyBlock
    }

    #endregion

    private void Awake()
    {
        if (_myRenderer == null) _myRenderer = GetComponent<Renderer>();
        _propBlock ??= new MaterialPropertyBlock();

        // Khởi tạo cache cho các material hiệu ứng.
        _cachedEffectMaterialInstances ??= new Dictionary<StatusEffectType, Material[]>();

        // Cache the original materials for status effect reversion
        if (_myRenderer != null)
        {
            // Sử dụng sharedMaterials để tránh tạo instance material mới trong Editor,
            // vốn là nguyên nhân gây ra lỗi "leaking materials".
            // sharedMaterials trả về các asset material gốc, không phải bản sao.
            _originalMaterials = _myRenderer.sharedMaterials;
        }

        // Build the status effect dictionary for quick lookups
        _statusEffectProfileMap = new Dictionary<StatusEffectType, StatusEffectProfile>();
        foreach (var profile in _statusEffectProfiles)
        {
            _statusEffectProfileMap[profile.effectType] = profile;
        }
    }

    private void OnEnable()
    {
        // Khi component được bật, áp dụng màu.
        UpdateColor();
    }

    private void OnDisable()
    {
        // Khi component bị tắt, xóa mọi ghi đè màu để quay về màu gốc của material.
        _baseColorTween?.Kill();
        _decalColorTween?.Kill();
        _isFuseColorOverridden = false; // Xóa ghi đè màu ngòi nổ

        if (_statusEffectCoroutine != null)
        {
            StopCoroutine(_statusEffectCoroutine);
            RevertEffectMaterial();
            _statusEffectCoroutine = null;
        }

        if (_myRenderer == null) _myRenderer = GetComponent<Renderer>();
        if (_myRenderer == null) return;
        
        _myRenderer.SetPropertyBlock(null);
    }

    private void OnDestroy()
    {
        // Đảm bảo tween bị hủy khi đối tượng bị phá hủy để tránh lỗi.
        _baseColorTween?.Kill();
        _decalColorTween?.Kill();

        // CẢI TIẾN: Dọn dẹp tất cả các material đã được cache.
        if (_cachedEffectMaterialInstances != null)
        {
            foreach (var materialArray in _cachedEffectMaterialInstances.Values)
            {
                foreach (var mat in materialArray)
                {
                    if (mat == null) continue;

                    if (Application.isPlaying)
                    {
                        Destroy(mat);
                    }
                    else
                    {
                        DestroyImmediate(mat);
                    }
                }
            }
        }
    }

    private void OnValidate()
    {
        // Trong Editor, nếu component đang bật, cập nhật màu khi giá trị thay đổi.
        if (enabled)
        {
            UpdateColor();
        }
    }

    private void UpdateColor()
    {
        // 1. Không áp dụng property block nếu một hiệu ứng trạng thái (thay thế material) đang hoạt động
        if (_isEffectActive) return;

        if (_myRenderer == null) _myRenderer = GetComponent<Renderer>();
        if (_myRenderer == null) return;

        // 2. Nếu việc ghi đè bị tắt và không có hiệu ứng tạm thời (pulse) nào đang chạy,
        //    hãy đảm bảo xóa mọi property block đã áp dụng trước đó và dừng lại.
        //    Điều này sẽ khiến renderer sử dụng các thuộc tính gốc từ material asset.
        if (!_applyMaterialOverrides && !_isBaseColorOverridden && !_isDecalColorOverridden)
        {
            _myRenderer.SetPropertyBlock(null);
            return;
        }

        _propBlock ??= new MaterialPropertyBlock();

        // 3. Xác định các giá trị cuối cùng sẽ được áp dụng
        Color finalBaseColor;
        if (_isFuseColorOverridden)
        {
            // Ưu tiên cao nhất: màu của ngòi nổ
            finalBaseColor = _runtimeFuseColor;
        }
        else if (_isBaseColorOverridden)
        {
            // Ưu tiên thứ hai: màu của hiệu ứng pulse
            finalBaseColor = _runtimeBaseColor;
        }
        else
        {
            // Mặc định: màu tùy chỉnh từ Inspector
            finalBaseColor = _customBaseColor;
        }

        _propBlock.SetColor(BASE_COLOR_PROPERTY_NAME, finalBaseColor);

        Color finalDecalColor = _isDecalColorOverridden ? _runtimeDecalColor : _customDecalColor;
        
        // 4. Xây dựng property block dựa trên trạng thái hiện tại
        if (_applyMaterialOverrides)
        {
            // Nếu ghi đè được bật, áp dụng tất cả các giá trị tùy chỉnh từ Inspector.
            // Các giá trị từ pulse (nếu có) sẽ được dùng thay thế.
            _propBlock.SetFloat(EMISSION_INTENSITY_PROPERTY_NAME, _customEmissionIntensity);
            _propBlock.SetFloat(ALPHA_PROPERTY_NAME, _customAlpha);
            _propBlock.SetColor(BASE_COLOR_PROPERTY_NAME, finalBaseColor);
            _propBlock.SetFloat(FRESNEL_POWER_PROPERTY_NAME, _customFresnelPower);
            _propBlock.SetFloat(THICKNESS_PROPERTY_NAME, _customThickness);
            _propBlock.SetColor(DECAL_COLOR_PROPERTY_NAME, finalDecalColor);
        }
        else
        {
            // Nếu ghi đè bị tắt, chỉ áp dụng các giá trị từ hiệu ứng pulse đang hoạt động.
            // Xóa block cũ để đảm bảo không còn giá trị rác.
            _propBlock.Clear();
            if (_isFuseColorOverridden) _propBlock.SetColor(BASE_COLOR_PROPERTY_NAME, finalBaseColor);
            if (_isBaseColorOverridden) _propBlock.SetColor(BASE_COLOR_PROPERTY_NAME, finalBaseColor);
            if (_isDecalColorOverridden) _propBlock.SetColor(DECAL_COLOR_PROPERTY_NAME, finalDecalColor);
        }

        // 5. Áp dụng property block đã được xây dựng lên renderer.
        _myRenderer.SetPropertyBlock(_propBlock);

    }
}