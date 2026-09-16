using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Core;
using Core.Interfaces;
using Core.Interfaces.UI;

namespace UI
{
    /// <summary>
    /// Bang diem (Score Card) hien thi ket qua cuoi round rieng cho tung nguoi choi:
    /// Survival Score, Base Multiplier, Win Multiplier va Total Credits.
    /// Score Card xuat hien bang cach truot LEN, sau do nhay scale up/down, phat SFX, chay cac hat ngoi sao,
    /// va tu dong bien mat bang cach truot XUONG sau <see cref="_displayDuration"/> giay.
    /// </summary>
    public class ScoreCard : MonoBehaviour, IScoreCard
    {
        #region Fields

        [Header("Panel")]
        [Tooltip("RectTransform goc cua Score Card (phai la child cua UI Canvas).")]
        [SerializeField] private RectTransform _panelRect;

        [Header("Text Fields")]
        [Tooltip("Text hien thi gia tri Survival Score.")]
        [SerializeField] private TextMeshProUGUI _survivalScoreText;
        [Tooltip("Text hien thi Base Multiplier (vd: x1.0, x1.25).")]
        [SerializeField] private TextMeshProUGUI _baseMultiplierText;
        [Tooltip("Text hien thi Win Multiplier (dua tren win streak).")]
        [SerializeField] private TextMeshProUGUI _winMultiplierText;
        [Tooltip("Text hien thi tong credits nhan duoc.")]
        [SerializeField] private TextMeshProUGUI _totalCreditsText;

        [Header("Extreme Mode Icon")]
        [Tooltip("Icon (GameObject) hien thi khi round vua roi co bat Extreme Mode.")]
        [SerializeField] private GameObject _extremeModeIcon;

        [Header("Animation Settings")]
        [Tooltip("Thoi gian (giay) Score Card di chuyen LEN de xuat hien tren man hinh.")]
        [SerializeField] private float _slideInDuration = 0.5f;
        [Tooltip("Thoi gian (giay) Score Card di chuyen XUONG de bien mat.")]
        [SerializeField] private float _slideOutDuration = 0.4f;
        [Tooltip("Khoang dich chuyen theo truc Y khi truot (duong = len, am = xuong).")]
        [SerializeField] private Vector2 _slideOffset = new(0, -350f);
        [Tooltip("Thoi gian hien thi (giay) truoc khi Score Card tu dong truot xuong an di. Mac dinh 5 giay.")]
        [SerializeField] private float _displayDuration = 5f;
        [Tooltip("Do tre (giay) truoc khi Score Card bat dau xuat hien sau khi Show() duoc goi. Mac dinh 1 giay.")]
        [SerializeField] private float _showDelay = 1f;

        [Header("Pop / Pulse Animation")]
        [Tooltip("He so phong to khi nhay scale (vi du: 1.08).")]
        [SerializeField] private float _popScale = 1.08f;
        [Tooltip("Thoi gian nhay scale up va down (giay).")]
        [SerializeField] private float _popDuration = 0.2f;
        [Tooltip("Ease cho hieu ung nhay scale.")]
        [SerializeField] private Ease _popEase = Ease.OutQuad;
        [Tooltip("SFX phat khi Score Card truot len xong va nhay scale.")]
        [SerializeField] private AudioClip _popSfx;

        [Header("Star Particles")]
        [Tooltip("Danh sach cac object StarUIParticle phat sau khi truot vao xong.")]
        [SerializeField] private List<StarUIParticle> _starParticles = new();

        // Vi tri goc cua panel khi hien thi binh thuong (thuong la giua man hinh).
        private Vector3 _originalLocalPosition;

        // Co danh dau da cache vi tri goc hay chua
        private bool _isOriginalPositionCached;
        private Coroutine _autoHideCoroutine;
        private Coroutine _showDelayCoroutine;
        private Tween _slideTween;
        private Tween _popTween;

        #endregion

        #region Properties

        /// <summary>
        /// Cho biet Score Card co dang hien thi tren man hinh hay khong.
        /// </summary>
        public bool IsVisible { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_panelRect != null)
            {
                _originalLocalPosition = _panelRect.localPosition;
                _isOriginalPositionCached = true;
                _panelRect.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _slideTween?.Kill();
            _popTween?.Kill();
        }

        #endregion

        #region Public Methods (IScoreCard Implementation)

        /// <summary>
        /// Hien thi Score Card voi du lieu diem da tinh toan. Panel se tu an sau _displayDuration giay.
        /// </summary>
        /// <param name="data">Du lieu diem va ket qua thang/thua.</param>
        public void Show(ScoreCardData data)
        {
            if (_panelRect == null)
            {
                Debug.LogWarning("[ScoreCard] Root RectTransform is not assigned! Cannot show Score Card.", this);
                return;
            }

            if (!_isOriginalPositionCached)
            {
                _originalLocalPosition = _panelRect.localPosition;
                _isOriginalPositionCached = true;
            }

            ApplyData(data);
            CancelPendingAnimations();

            IsVisible = false;
            _panelRect.localPosition = _originalLocalPosition + new Vector3(0f, _slideOffset.y, 0f);
            _panelRect.localScale = Vector3.one;
            _panelRect.gameObject.SetActive(true);

            if (_showDelay > 0f)
            {
                _showDelayCoroutine = StartCoroutine(DelayedShowCoroutine(_showDelay));
            }
            else
            {
                BeginSlideIn();
            }
        }

        /// <inheritdoc/>
        public void Hide()
        {
            CancelPendingAnimations();
            IsVisible = false;
            if (_panelRect != null)
            {
                _panelRect.localScale = Vector3.one;
                _panelRect.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Dien du lieu vao cac TextField cua Score Card.
        /// </summary>
        private void ApplyData(ScoreCardData data)
        {
            if (_survivalScoreText != null) _survivalScoreText.text = data.SurvivalScore.ToString();
            if (_baseMultiplierText != null) _baseMultiplierText.text = $"x{data.BaseMultiplier:0.##}";
            if (_winMultiplierText != null) _winMultiplierText.text = $"x{data.WinMultiplier:0.##}";
            if (_totalCreditsText != null) _totalCreditsText.text = data.TotalCredits.ToString();

            if (_extremeModeIcon != null)
            {
                _extremeModeIcon.SetActive(data.IsExtremeMode);
            }
        }

        /// <summary>
        /// Coroutine cho _showDelay giay roi moi cho Score Card xuat hien.
        /// </summary>
        private IEnumerator DelayedShowCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _showDelayCoroutine = null;
            BeginSlideIn();
        }

        /// <summary>
        /// Thuc hien hieu ung truot LEN de hien thi panel, nhay scale, chay VFX/SFX va len lich tu an sau _displayDuration giay.
        /// </summary>
        private void BeginSlideIn()
        {
            _panelRect.localPosition = _originalLocalPosition + new Vector3(0f, _slideOffset.y, 0f);
            _panelRect.localScale = Vector3.one;
            _panelRect.gameObject.SetActive(true);
            IsVisible = true;

            _slideTween?.Kill();
            _popTween?.Kill();

            _slideTween = _panelRect
                .DOLocalMove(_originalLocalPosition, _slideInDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    // 1. Phat SFX khi truot len xong
                    if (_popSfx != null && SfxService.Instance != null)
                    {
                        SfxService.Instance.PlaySfx(_popSfx);
                    }

                    // 2. Chay cac particle sao da gan
                    PlayStarParticles();

                    // 3. Nhay scale up va down 1 lan
                    PlayPopAnimation();
                });

            _autoHideCoroutine = StartCoroutine(AutoHideCoroutine(_displayDuration));
        }

        /// <summary>
        /// Hieu ung nhay phong to roi thu nho ve 1.
        /// </summary>
        private void PlayPopAnimation()
        {
            if (_panelRect == null) return;

            _popTween?.Kill();

            float halfDuration = _popDuration * 0.5f;
            Sequence popSeq = DOTween.Sequence();

            popSeq.Append(_panelRect.DOScale(Vector3.one * _popScale, halfDuration).SetEase(_popEase));
            popSeq.Append(_panelRect.DOScale(Vector3.one, halfDuration).SetEase(Ease.InQuad));

            _popTween = popSeq;
        }

        /// <summary>
        /// Kich hoat va phat tat ca cac doi tuong StarUIParticle da gan.
        /// </summary>
        private void PlayStarParticles()
        {
            if (_starParticles == null || _starParticles.Count == 0) return;

            foreach (var star in _starParticles)
            {
                if (star != null)
                {
                    star.gameObject.SetActive(true);
                    star.Play();
                }
            }
        }

        /// <summary>
        /// Huy moi coroutine/tween dang treo (delay hien thi, auto-hide, tween truot, pop).
        /// </summary>
        private void CancelPendingAnimations()
        {
            if (_showDelayCoroutine != null)
            {
                StopCoroutine(_showDelayCoroutine);
                _showDelayCoroutine = null;
            }
            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = null;
            }
            _slideTween?.Kill();
            _popTween?.Kill();
        }

        /// <summary>
        /// Coroutine cho mot khoang thoi gian roi cho Score Card truot xuong (bien mat).
        /// </summary>
        private IEnumerator AutoHideCoroutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            if (_panelRect == null) yield break;

            _slideTween?.Kill();
            _popTween?.Kill();

            _slideTween = _panelRect
                .DOLocalMove(_originalLocalPosition + new Vector3(0f, _slideOffset.y, 0f), _slideOutDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    _panelRect.localScale = Vector3.one;
                    _panelRect.gameObject.SetActive(false);
                    IsVisible = false;
                });

            _autoHideCoroutine = null;
        }

        #endregion
    }
}