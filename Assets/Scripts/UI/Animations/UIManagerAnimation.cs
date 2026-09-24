using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Core.Interfaces;

namespace UI.Animations
{
    /// <summary>
    /// Cau hinh mau hien thi cho Text va Icon theo nguong cuong do Intensity.
    /// </summary>
    [Serializable]
    public struct IntensityColorTier
    {
        [Tooltip("Nguong Intensity (neu CurrentIntensity < Nguong nay se dung mau nay).")]
        public float MaxIntensityThreshold;
        [Tooltip("Mau hien thi cho Text.")]
        public Color TextColor;
        [Tooltip("Mau hien thi cho Icon.")]
        public Color IconColor;
    }

    /// <summary>
    /// Component quan ly tat ca animation va hieu ung am thanh lien quan cua UIManager
    /// (Transition screen, thanh cuong do intensity, panel current intensity, va countdown bang anh).
    /// </summary>
    public class UIManagerAnimation : MonoBehaviour
    {
        #region Fields
        private static readonly WaitForSeconds _waitForSeconds1 = new(1f);

        [Header("Transition Screen")]
        [Tooltip("Component xu ly animation chuyen canh TransitionScreen.")]
        [SerializeField] private TransitionScreenAnimation _transitionScreenAnimation;

        [Header("Intensity Bar & SFX")]
        [Tooltip("Panel chua thanh cuong do.")]
        [SerializeField] private GameObject _intensityBarPanel;
        [Tooltip("Mui ten chi bao tren thanh cuong do.")]
        [SerializeField] private RectTransform _intensityArrow;
        [Tooltip("Khu vuc co the di chuyen cua mui ten.")]
        [SerializeField] private RectTransform _intensityRange;
        [Tooltip("SFX phat khi mui ten tren thanh intensity chay.")]
        [SerializeField] private AudioClip _arrowMoveSfx;

        [Header("Intensity Bar Animation Settings")]
        [Tooltip("Vi tri bat dau cua thanh cuong do (ngoai man hinh).")]
        [SerializeField] private Vector2 _intensityBarStartOffset = new(0, 200f);
        [Tooltip("Thoi gian truot vao cua thanh cuong do.")]
        [SerializeField] private float _intensityBarSlideInDuration = 0.5f;
        [Tooltip("Ease type cho hieu ung truot vao.")]
        [SerializeField] private Ease _intensityBarSlideInEase = Ease.OutQuad;
        [Tooltip("Thoi gian chay cua mui ten.")]
        [SerializeField] private float _arrowMoveDuration = 0.8f;
        [Tooltip("Ease type cho mui ten.")]
        [SerializeField] private Ease _arrowMoveEase = Ease.InOutSine;

        [Header("Current Intensity Display & SFX")]
        [Tooltip("Panel hien thi do kho hien tai cua round.")]
        [SerializeField] private GameObject _currentIntensityPanel;
        [Tooltip("Text hien thi gia tri do kho hien tai cua round.")]
        [SerializeField] private TextMeshProUGUI _currentIntensityText;
        [Tooltip("Icon (Image) hien thi tren CurrentIntensityPanel.")]
        [SerializeField] private Image _currentIntensityIcon;

        [Header("Current Intensity Tier Colors")]
        [Tooltip("Mau Text cho Nhom 1 (Intensity < 2.0).")]
        [SerializeField] private Color _textColorGroup1 = Color.white;
        [Tooltip("Mau Icon cho Nhom 1 (Intensity < 2.0).")]
        [SerializeField] private Color _iconColorGroup1 = Color.white;

        [Tooltip("Mau Text cho Nhom 2 (Intensity 2.0 - 4.99).")]
        [SerializeField] private Color _textColorGroup2 = Color.yellow;
        [Tooltip("Mau Icon cho Nhom 2 (Intensity 2.0 - 4.99).")]
        [SerializeField] private Color _iconColorGroup2 = Color.yellow;

        [Tooltip("Mau Text cho Nhom 3 (Intensity 5.0 - 5.99).")]
        [SerializeField] private Color _textColorGroup3 = new(1f, 0.5f, 0f);
        [Tooltip("Mau Icon cho Nhom 3 (Intensity 5.0 - 5.99).")]
        [SerializeField] private Color _iconColorGroup3 = new(1f, 0.5f, 0f);

        [Tooltip("Mau Text cho Nhom 4 (Intensity >= 6.0).")]
        [SerializeField] private Color _textColorGroup4 = Color.red;
        [Tooltip("Mau Icon cho Nhom 4 (Intensity >= 6.0).")]
        [SerializeField] private Color _iconColorGroup4 = Color.red;

        [Tooltip("Danh sach custom mau theo nguong intensity. Neu de trong se dung 4 nhom mau mac dinh ben tren.")]
        [SerializeField] private IntensityColorTier[] _customColorTiers;

        [Tooltip("SFX phat khi CurrentIntensityPanel hien thi voi intensity 1 - 1.99.")]
        [SerializeField] private AudioClip _intensitySfxGroup1;
        [Tooltip("SFX phat khi CurrentIntensityPanel hien thi voi intensity 2.0 - 4.99.")]
        [SerializeField] private AudioClip _intensitySfxGroup2;
        [Tooltip("SFX phat khi CurrentIntensityPanel hien thi voi intensity 5.0 - 5.99.")]
        [SerializeField] private AudioClip _intensitySfxGroup3;
        [Tooltip("SFX phat khi CurrentIntensityPanel hien thi voi intensity 6.0.")]
        [SerializeField] private AudioClip _intensitySfxGroup4;

        [Header("Countdown UI & SFX")]
        [Tooltip("Panel chua hinh anh dem nguoc dau round.")]
        [SerializeField] private GameObject _countdownPanel;
        [Tooltip("Image hien thi anh dem nguoc.")]
        [SerializeField] private Image _countdownImage;
        [Tooltip("Image hien thi chu GO rieng biệt.")]
        [SerializeField] private Image _goImage;
        [Tooltip("Danh sach anh dem nguoc tuong ung: Index 0 = '3', Index 1 = '2', Index 2 = '1', Index 3 = 'GO'.")]
        [SerializeField] private Sprite[] _countdownSprites;
        [Tooltip("SFX phat khi moi nhip dem nguoc dien ra.")]
        [SerializeField] private AudioClip _countdownTickSfx;
        [Tooltip("SFX tieng fuse chay ngam loop trong suot qua trinh dem nguoc.")]
        [SerializeField] private AudioClip _fuseLoopSfx;

        [Header("Round Start & End SFX")]
        [Tooltip("SFX tieng coi khi round bat dau va ket thuc.")]
        [SerializeField] private AudioClip _whistleSfx;
        [Tooltip("SFX tieng no phat dong thoi voi tieng coi khi round bat dau (chu GO).")]
        [SerializeField] private AudioClip _roundStartExplosionSfx;
        [Tooltip("SFX tieng chuong khi round ket thuc.")]
        [SerializeField] private AudioClip _bellSfx;

        [Header("Sidebar Tooltips")]
        [Tooltip("Component tooltip cho nut Shop tren Sidebar.")]
        [SerializeField] private UISidebarTooltip _shopTooltip;
        [Tooltip("Component tooltip cho nut Inventory tren Sidebar.")]
        [SerializeField] private UISidebarTooltip _inventoryTooltip;
        [Tooltip("Component tooltip cho nut Option tren Sidebar.")]
        [SerializeField] private UISidebarTooltip _optionTooltip;

        private Vector2 _intensityBarOriginalPosition;
        #endregion

        #region Properties
        /// <summary>
        /// Tooltip cho nut Shop.
        /// </summary>
        public UISidebarTooltip ShopTooltip => _shopTooltip;

        /// <summary>
        /// Tooltip cho nut Inventory.
        /// </summary>
        public UISidebarTooltip InventoryTooltip => _inventoryTooltip;

        /// <summary>
        /// Tooltip cho nut Option.
        /// </summary>
        public UISidebarTooltip OptionTooltip => _optionTooltip;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_intensityBarPanel != null)
            {
                var rect = _intensityBarPanel.GetComponent<RectTransform>();
                if (rect != null)
                {
                    _intensityBarOriginalPosition = rect.anchoredPosition;
                }
                _intensityBarPanel.SetActive(false);
            }

            if (_countdownPanel != null) _countdownPanel.SetActive(false);
            if (_countdownImage != null) _countdownImage.gameObject.SetActive(false);
            if (_goImage != null) _goImage.gameObject.SetActive(false);
            if (_currentIntensityPanel != null) _currentIntensityPanel.SetActive(false);
        }
        #endregion

        #region Public Methods - Animations & Effects
        /// <summary>
        /// Animation thanh cuong do intensity truot vao va mui ten chay den gia tri tuong ung.
        /// </summary>
        public IEnumerator AnimateIntensityBar(float currentIntensity, float minIntensity, float maxIntensity)
        {
            if (_intensityBarPanel == null || _intensityArrow == null || _intensityRange == null)
            {
                Debug.LogWarning("[UIManagerAnimation] Intensity Bar components are not fully assigned.", this);
                yield break;
            }

            var intensityBarRect = _intensityBarPanel.GetComponent<RectTransform>();
            intensityBarRect.anchoredPosition = _intensityBarOriginalPosition + _intensityBarStartOffset;
            _intensityBarPanel.SetActive(true);

            DOTween.To(() => intensityBarRect.anchoredPosition, x => intensityBarRect.anchoredPosition = x, _intensityBarOriginalPosition, _intensityBarSlideInDuration).SetEase(_intensityBarSlideInEase);
            yield return new WaitForSeconds(_intensityBarSlideInDuration);

            float normalizedValue;
            if (Mathf.Approximately(minIntensity, maxIntensity))
            {
                normalizedValue = currentIntensity >= maxIntensity ? 1f : 0f;
            }
            else
            {
                normalizedValue = Mathf.Clamp01((currentIntensity - minIntensity) / (maxIntensity - minIntensity));
            }
            float rangeWidth = _intensityRange.rect.width;
            float arrowTargetX = normalizedValue * rangeWidth;

            if (_arrowMoveSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_arrowMoveSfx);
            }

            DOTween.To(() => _intensityArrow.anchoredPosition, pos => _intensityArrow.anchoredPosition = pos, new Vector2(arrowTargetX, _intensityArrow.anchoredPosition.y), _arrowMoveDuration).SetEase(_arrowMoveEase);
            yield return new WaitForSeconds(_arrowMoveDuration);

            yield return _waitForSeconds1;

            _intensityBarPanel.SetActive(false);
            _intensityArrow.anchoredPosition = new Vector2(0, _intensityArrow.anchoredPosition.y);
        }

        /// <summary>
        /// Hien thi CurrentIntensityPanel voi hieu ung scale up, doi mau Text + Icon theo phan khuc va phat SFX theo nhom intensity.
        /// </summary>
        public void ShowCurrentIntensity(float currentIntensity)
        {
            if (_currentIntensityPanel != null)
            {
                if (_currentIntensityText != null)
                {
                    _currentIntensityText.text = currentIntensity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                }

                Color targetTextColor = Color.white;
                Color targetIconColor = Color.white;

                if (_customColorTiers != null && _customColorTiers.Length > 0)
                {
                    bool foundCustom = false;
                    for (int i = 0; i < _customColorTiers.Length; i++)
                    {
                        if (currentIntensity < _customColorTiers[i].MaxIntensityThreshold)
                        {
                            targetTextColor = _customColorTiers[i].TextColor;
                            targetIconColor = _customColorTiers[i].IconColor;
                            foundCustom = true;
                            break;
                        }
                    }
                    if (!foundCustom)
                    {
                        targetTextColor = _customColorTiers[_customColorTiers.Length - 1].TextColor;
                        targetIconColor = _customColorTiers[_customColorTiers.Length - 1].IconColor;
                    }
                }
                else
                {
                    if (currentIntensity < 2.0f)
                    {
                        targetTextColor = _textColorGroup1;
                        targetIconColor = _iconColorGroup1;
                    }
                    else if (currentIntensity < 5.0f)
                    {
                        targetTextColor = _textColorGroup2;
                        targetIconColor = _iconColorGroup2;
                    }
                    else if (currentIntensity < 6.0f)
                    {
                        targetTextColor = _textColorGroup3;
                        targetIconColor = _iconColorGroup3;
                    }
                    else
                    {
                        targetTextColor = _textColorGroup4;
                        targetIconColor = _iconColorGroup4;
                    }
                }

                if (_currentIntensityText != null)
                {
                    _currentIntensityText.color = targetTextColor;
                }

                if (_currentIntensityIcon != null)
                {
                    _currentIntensityIcon.color = targetIconColor;
                }

                _currentIntensityPanel.transform.DOKill();
                _currentIntensityPanel.transform.localScale = Vector3.zero;
                _currentIntensityPanel.SetActive(true);
                _currentIntensityPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);

                if (SfxService.Instance != null)
                {
                    AudioClip clipToPlay = null;
                    if (currentIntensity < 2.0f) clipToPlay = _intensitySfxGroup1;
                    else if (currentIntensity < 5.0f) clipToPlay = _intensitySfxGroup2;
                    else if (currentIntensity < 6.0f) clipToPlay = _intensitySfxGroup3;
                    else clipToPlay = _intensitySfxGroup4;

                    if (clipToPlay != null)
                    {
                        SfxService.Instance.PlaySfx(clipToPlay);
                    }
                }
            }
        }

        /// <summary>
        /// An thanh cuong do.
        /// </summary>
        public void HideIntensityBar()
        {
            if (_intensityBarPanel != null) _intensityBarPanel.SetActive(false);
        }

        /// <summary>
        /// An panel hien thi do kho hien tai.
        /// </summary>
        public void HideCurrentIntensity()
        {
            if (_currentIntensityPanel != null) _currentIntensityPanel.SetActive(false);
        }

        /// <summary>
        /// Coroutine chay dem nguoc bang hinh anh (3 2 1 GO) kem hieu ung animation va SFX.
        /// </summary>
        public IEnumerator ShowCountdown()
        {
            if (_countdownPanel == null || _countdownImage == null || _countdownSprites == null || _countdownSprites.Length < 4)
            {
                Debug.LogWarning("[UIManagerAnimation] Countdown components are not fully assigned.", this);
                yield break;
            }

            _countdownPanel.SetActive(true);

            ISfxLoopHandle fuseLoop = null;
            if (_fuseLoopSfx != null && SfxService.Instance != null)
            {
                fuseLoop = SfxService.Instance.PlaySfxLoop(_fuseLoopSfx, transform, 1f, 1f, false);
            }

            for (int i = 0; i < 4; i++)
            {
                if (i < 3 && _countdownTickSfx != null && SfxService.Instance != null)
                {
                    SfxService.Instance.PlaySfx(_countdownTickSfx);
                }

                if (i == 3)
                {
                    fuseLoop?.Stop();
                    fuseLoop = null;
                    PlayRoundStartSfx();
                }

                Image targetImage = (i == 3 && _goImage != null) ? _goImage : _countdownImage;

                _countdownImage.gameObject.SetActive(targetImage == _countdownImage);
                if (_goImage != null)
                {
                    _goImage.gameObject.SetActive(targetImage == _goImage);
                }

                if (_countdownSprites[i] != null)
                {
                    targetImage.sprite = _countdownSprites[i];
                }

                if (!targetImage.TryGetComponent<CanvasGroup>(out var cg)) cg = targetImage.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 1f;

                targetImage.transform.DOKill();
                cg.DOKill();

                // Appear: Scale up + Quay (Rotate)
                targetImage.transform.localScale = Vector3.zero;
                targetImage.transform.localRotation = Quaternion.Euler(0, 0, -180f);

                Sequence appearSeq = DOTween.Sequence().SetUpdate(true);
                appearSeq.Join(targetImage.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
                appearSeq.Join(targetImage.transform.DORotate(Vector3.zero, 0.35f).SetEase(Ease.OutBack));

                yield return new WaitForSecondsRealtime(0.6f);

                // Disappear: Fade Out + Scale Up
                Sequence disappearSeq = DOTween.Sequence().SetUpdate(true);
                disappearSeq.Join(targetImage.transform.DOScale(Vector3.one * 1.5f, 0.35f).SetEase(Ease.InQuad));
                disappearSeq.Join(cg.DOFade(0f, 0.35f).SetEase(Ease.InQuad));

                yield return new WaitForSecondsRealtime(0.4f);
            }

            fuseLoop?.Stop();
            _countdownImage.gameObject.SetActive(false);
            if (_goImage != null) _goImage.gameObject.SetActive(false);
            _countdownPanel.SetActive(false);
        }

        /// <summary>
        /// Chay hieu ung transition chuyen canh.
        /// </summary>
        public void PlayTransition(Action onCovered, Action onComplete = null, float? holdDurationOverride = null)
        {
            if (_transitionScreenAnimation != null)
            {
                _transitionScreenAnimation.PlayTransition(onCovered, onComplete, holdDurationOverride);
            }
            else
            {
                onCovered?.Invoke();
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Phat coi va tieng no khi round bat dau (chu GO).
        /// </summary>
        public void PlayRoundStartSfx()
        {
            if (SfxService.Instance != null)
            {
                if (_whistleSfx != null)
                {
                    SfxService.Instance.PlaySfx(_whistleSfx);
                }

                if (_roundStartExplosionSfx != null)
                {
                    SfxService.Instance.PlaySfx(_roundStartExplosionSfx);
                }
            }
        }

        /// <summary>
        /// Phat coi + chuong khi round ket thuc.
        /// </summary>
        public void PlayRoundEndSfx()
        {
            if (SfxService.Instance != null)
            {
                if (_whistleSfx != null) SfxService.Instance.PlaySfx(_whistleSfx);
                if (_bellSfx != null) SfxService.Instance.PlaySfx(_bellSfx);
            }
        }
        #endregion
    }
}

