using UnityEngine;
using DG.Tweening;

/// <summary>
/// Quản lý việc ghi đè các thuộc tính của material (như màu và alpha) một cách linh hoạt.
/// Hỗ trợ các hiệu ứng tạm thời như nháy màu (pulse) thông qua DOTween.
/// </summary>
[ExecuteAlways]
[SelectionBase]
public class MaterialEffectController : MonoBehaviour
{
    [Header("Màu tùy chỉnh")]
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

    private const string BASE_COLOR_PROPERTY_NAME = "_BaseColor";
    private const string DECAL_COLOR_PROPERTY_NAME = "_DecalColor";
    private const string ALPHA_PROPERTY_NAME = "_Alpha";
    private const string EMISSION_INTENSITY_PROPERTY_NAME = "_EmissionIntensity";

    private MaterialPropertyBlock _propBlock;
    private Renderer _myRenderer;

    // Trạng thái runtime cho việc ghi đè màu nền
    private bool _isBaseColorOverridden = false;
    private Color _runtimeBaseColor;
    private Tweener _baseColorTween;

    // Trạng thái runtime cho việc ghi đè màu họa tiết
    private bool _isDecalColorOverridden = false;
    private Color _runtimeDecalColor;
    private Tweener _decalColorTween;

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

    private void Awake()
    {
        if (_myRenderer == null) _myRenderer = GetComponent<Renderer>();
        _propBlock ??= new MaterialPropertyBlock();
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
        if (_myRenderer == null) _myRenderer = GetComponent<Renderer>();
        if (_myRenderer == null) return;
        
        _myRenderer.SetPropertyBlock(null);
    }

    private void OnDestroy()
    {
        // Đảm bảo tween bị hủy khi đối tượng bị phá hủy để tránh lỗi.
        _baseColorTween?.Kill();
        _decalColorTween?.Kill();
    }

    private void OnValidate()
    {
        // Trong Editor, nếu component đang bật, cập nhật màu khi giá trị thay đổi.
        if (this.enabled)
        {
            UpdateColor();
        }
    }

    private void UpdateColor()
    {
        if (_myRenderer == null) _myRenderer = GetComponent<Renderer>();
        if (_myRenderer == null) return;

        _propBlock ??= new MaterialPropertyBlock();

        // Luôn áp dụng cường độ emission tùy chỉnh
        _propBlock.SetFloat(EMISSION_INTENSITY_PROPERTY_NAME, _customEmissionIntensity);

        // Luôn áp dụng alpha tùy chỉnh
        _propBlock.SetFloat(ALPHA_PROPERTY_NAME, _customAlpha);

        // --- Xử lý màu nền ---
        // Nếu có hiệu ứng runtime (pulse), dùng màu của hiệu ứng đó. Nếu không, dùng màu trong Inspector.
        Color finalBaseColor = _isBaseColorOverridden ? _runtimeBaseColor : _customBaseColor;
        _propBlock.SetColor(BASE_COLOR_PROPERTY_NAME, finalBaseColor);

        // --- Xử lý màu họa tiết ---
        Color finalDecalColor = _isDecalColorOverridden ? _runtimeDecalColor : _customDecalColor;
        _propBlock.SetColor(DECAL_COLOR_PROPERTY_NAME, finalDecalColor);

        _myRenderer.SetPropertyBlock(_propBlock);
    }
}