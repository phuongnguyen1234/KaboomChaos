using UnityEngine;
using TMPro;
using DG.Tweening;
using Core;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Component dong bo va hien thi Next Round Intensity trong World space.
    /// Gom mui ten (arrow) di chuyen tren thanh range va text hien thi chi so do kho.
    /// Cap nhat vi tri arrow va text moi khi Next Round Intensity thay doi.
    /// </summary>
    public class WorldNextRoundIntensityDisplay : MonoBehaviour
    {
        #region Fields
        [Header("UI Components")]
        [Tooltip("Mui ten chi bao tren thanh cuong do trong World space.")]
        [SerializeField] private RectTransform _arrow;

        [Tooltip("Khu vuc range de mui ten di chuyen (chieu rong thanh intensity).")]
        [SerializeField] private RectTransform _range;

        [Tooltip("Text Mesh Pro hien thi chi so do kho (Next Round Intensity).")]
        [SerializeField] private TMP_Text _intensityText;

        [Header("Animation Settings")]
        [Tooltip("Thoi gian di chuyen mui ten khi cap nhat intensity.")]
        [SerializeField] private float _arrowMoveDuration = 0.5f;

        [Tooltip("Ease type cho hieu ung di chuyen mui ten.")]
        [SerializeField] private Ease _arrowMoveEase = Ease.OutQuad;

        [Tooltip("Format dinh dang hien thi so intensity tren text (vi du: 0.## hoac {0:0.##}).")]
        [SerializeField] private string _textFormat = "{0:0.##}";

        [Header("Range Fallback")]
        [Tooltip("Do kho toi thieu mac dinh (neu khong dung duoc tu GameloopManager).")]
        [SerializeField] private float _defaultMinIntensity = 1f;

        [Tooltip("Do kho toi da mac dinh (neu khong dung duoc tu GameloopManager).")]
        [SerializeField] private float _defaultMaxIntensity = 6f;

        [Header("Difficulty Tier Colors")]
        [Tooltip("Bat/tat doi mau text va arrow theo phan khuc do kho.")]
        [SerializeField] private bool _enableDifficultyColors = false;

        [Tooltip("Mau Text va Arrow cho Nhom 1 (Intensity < 2.0).")]
        [SerializeField] private Color _colorGroup1 = Color.white;

        [Tooltip("Mau Text va Arrow cho Nhom 2 (Intensity 2.0 - 4.99).")]
        [SerializeField] private Color _colorGroup2 = Color.yellow;

        [Tooltip("Mau Text va Arrow cho Nhom 3 (Intensity 5.0 - 5.99).")]
        [SerializeField] private Color _colorGroup3 = new Color(1f, 0.5f, 0f);

        [Tooltip("Mau Text va Arrow cho Nhom 4 (Intensity >= 6.0).")]
        [SerializeField] private Color _colorGroup4 = Color.red;

        private Tween _arrowTween;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            GameEvents.OnNextRoundIntensityChanged += HandleNextRoundIntensityChanged;
            RefreshDisplay(animate: false);
        }

        private void Start()
        {
            RefreshDisplay(animate: false);
        }

        private void OnDisable()
        {
            GameEvents.OnNextRoundIntensityChanged -= HandleNextRoundIntensityChanged;
            _arrowTween?.Kill();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Cap nhat hien thi chi so Next Round Intensity va di chuyen mui ten.
        /// </summary>
        /// <param name="intensity">Gia tri Next Round Intensity moi.</param>
        /// <param name="minIntensity">Intensity toi thieu.</param>
        /// <param name="maxIntensity">Intensity toi da.</param>
        /// <param name="animate">True de chay animation DOTween, False de dat ngay vi tri.</param>
        public void UpdateDisplay(float intensity, float minIntensity, float maxIntensity, bool animate = true)
        {
            // Cap nhat text hien thi
            if (_intensityText != null)
            {
                _intensityText.text = FormatIntensity(intensity);
            }

            if (_enableDifficultyColors)
            {
                Color targetColor = intensity < 2.0f ? _colorGroup1 :
                                   intensity < 5.0f ? _colorGroup2 :
                                   intensity < 6.0f ? _colorGroup3 : _colorGroup4;

                if (_intensityText != null)
                {
                    _intensityText.color = targetColor;
                }

                if (_arrow != null && _arrow.TryGetComponent<UnityEngine.UI.Image>(out var arrowImage))
                {
                    arrowImage.color = targetColor;
                }
            }

            // Cap nhat vi tri mui ten
            if (_arrow != null && _range != null)
            {
                float normalizedValue;
                if (Mathf.Approximately(minIntensity, maxIntensity))
                {
                    normalizedValue = intensity >= maxIntensity ? 1f : 0f;
                }
                else
                {
                    normalizedValue = Mathf.Clamp01((intensity - minIntensity) / (maxIntensity - minIntensity));
                }

                float rangeWidth = _range.rect.width;
                float targetX;
                if (_arrow.parent == _range)
                {
                    float anchorX = (_arrow.anchorMin.x + _arrow.anchorMax.x) * 0.5f;
                    targetX = (normalizedValue - anchorX) * rangeWidth;
                }
                else
                {
                    float rangeLeft = _range.anchoredPosition.x - (_range.pivot.x * rangeWidth);
                    targetX = rangeLeft + (normalizedValue * rangeWidth);
                }

                _arrowTween?.Kill();

                if (animate && Application.isPlaying && _arrowMoveDuration > 0f)
                {
                    _arrowTween = _arrow.DOAnchorPosX(targetX, _arrowMoveDuration)
                        .SetEase(_arrowMoveEase)
                        .SetUpdate(true);
                }
                else
                {
                    Vector2 pos = _arrow.anchoredPosition;
                    pos.x = targetX;
                    _arrow.anchoredPosition = pos;
                }
            }
        }

        /// <summary>
        /// Doc du lieu Next Round Intensity hien tai tu GameloopManager va refresh giao dien.
        /// </summary>
        public void RefreshDisplay(bool animate = false)
        {
            IGameloopManager gameloop = IGameloopManager.Instance;
            if (gameloop != null)
            {
                UpdateDisplay(gameloop.NextRoundIntensity, gameloop.MinIntensity, gameloop.MaxIntensity, animate);
            }
            else
            {
                UpdateDisplay(_defaultMinIntensity, _defaultMinIntensity, _defaultMaxIntensity, animate);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Format gia tri intensity theo dinh dang 0.## (loai bo cac so 0 vo nghia o phan thap phan).
        /// </summary>
        private string FormatIntensity(float intensity)
        {
            if (string.IsNullOrEmpty(_textFormat))
            {
                return intensity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            }

            try
            {
                if (_textFormat.Contains("{0"))
                {
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture, _textFormat, intensity);
                }

                // Xu ly truong hop nguoi dung nhap 0:## thay vi 0.## hoac truyen truc tiep format specifier
                string normalizedFormat = _textFormat.Replace(':', '.');
                return intensity.ToString(normalizedFormat, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return intensity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        #endregion

        #region Event Handlers
        private void HandleNextRoundIntensityChanged(float intensity, float minIntensity, float maxIntensity)
        {
            UpdateDisplay(intensity, minIntensity, maxIntensity, animate: true);
        }
        #endregion
    }
}

