using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Component tao hieu ung hat ngoi sao cho giao dien UI bang DOTween.
    /// Gom 1 ngoi sao lon o tam (scale up xuat hien, sau do scale up tiep va fade out)
    /// va cac ngoi sao nho xung quanh ban ra tu tam (scale up roi scale down).
    /// </summary>
    public class StarUIParticle : MonoBehaviour
    {
        #region Fields

        [Header("General Settings")]
        [Tooltip("Tu dong phat hieu ung khi GameObject duoc Enable.")]
        [SerializeField] private bool _playOnEnable = true;

        [Tooltip("Tu dong lap lai hieu ung lien tuc.")]
        [SerializeField] private bool _loop = false;

        [Tooltip("Thoi gian cho (giay) giua cac lan lap neu bat Loop.")]
        [SerializeField] private float _loopInterval = 0.5f;

        [Tooltip("Su dung unscaled time de animation van chay khi Time.timeScale = 0.")]
        [SerializeField] private bool _ignoreTimeScale = true;

        [Header("Center Big Star")]
        [Tooltip("RectTransform cua ngoi sao lon o trung tam.")]
        [SerializeField] private RectTransform _centerStar;

        [Tooltip("CanvasGroup cua ngoi sao lon de fade out. Neu de trong, script se tu them vao.")]
        [SerializeField] private CanvasGroup _centerStarCanvasGroup;

        [Tooltip("Scale toi da cua ngoi sao lon khi xuat hien.")]
        [SerializeField] private float _centerStarAppearScale = 1.2f;

        [Tooltip("Scale cua ngoi sao lon khi bien mat (phong to truoc khi bien mat).")]
        [SerializeField] private float _centerStarDisappearScale = 1.8f;

        [Tooltip("Thoi gian (giay) ngoi sao lon scale up xuat hien.")]
        [SerializeField] private float _centerAppearDuration = 0.35f;

        [Tooltip("Thoi gian (giay) giu ngoi sao lon o kich thuoc toi da truoc khi bien mat.")]
        [SerializeField] private float _centerHoldDuration = 0.1f;

        [Tooltip("Thoi gian (giay) ngoi sao lon phong to va fade out bien mat.")]
        [SerializeField] private float _centerDisappearDuration = 0.3f;

        [Tooltip("Ease cho ngoi sao lon khi xuat hien.")]
        [SerializeField] private Ease _centerAppearEase = Ease.OutBack;

        [Tooltip("Ease cho ngoi sao lon khi bien mat.")]
        [SerializeField] private Ease _centerDisappearEase = Ease.InQuad;

        [Header("Surrounding Small Stars")]
        [Tooltip("Danh sach RectTransform cac ngoi sao nho xung quanh. Neu de trong, script tu tim cac object con.")]
        [SerializeField] private List<RectTransform> _smallStars = new();

        [Tooltip("Prefab ngoi sao nho (tuy chon, neu muon tu dong tao them ngoi sao runtime).")]
        [SerializeField] private GameObject _smallStarPrefab;

        [Tooltip("So luong ngoi sao nho muon tao tu prefab neu duoc chi dinh.")]
        [SerializeField] private int _spawnSmallStarCount = 6;

        [Tooltip("Khoang cach toi thieu ngoi sao nho ban ra tu tam (pixel).")]
        [SerializeField] private float _minBurstDistance = 80f;

        [Tooltip("Khoang cach toi da ngoi sao nho ban ra tu tam (pixel).")]
        [SerializeField] private float _maxBurstDistance = 160f;

        [Tooltip("Scale toi da cua cac ngoi sao nho.")]
        [SerializeField] private float _smallStarMaxScale = 1f;

        [Tooltip("Tong thoi gian bay (giay) cua cac ngoi sao nho.")]
        [SerializeField] private float _smallStarDuration = 0.6f;

        [Tooltip("Ti le thoi gian scale up so voi tong thoi gian bay (0 den 1, vi du 0.35 la 35% thoi gian dau).")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float _smallStarScaleUpRatio = 0.35f;

        [Tooltip("Ease di chuyen cua ngoi sao nho khi ban ra tu tam.")]
        [SerializeField] private Ease _smallStarMoveEase = Ease.OutCubic;

        [Tooltip("Goc xoay ngau nhien (do) cua cac ngoi sao nho trong qua trinh bay.")]
        [SerializeField] private float _smallStarMaxRotation = 180f;

        [Tooltip("Do lech thoi gian bat dau ban ra cua cac ngoi sao nho so voi ngoi sao lon (giay).")]
        [SerializeField] private float _smallStarsDelay = 0.05f;

        [Header("Audio")]
        [Tooltip("SFX phat khi bat dau hieu ung sao.")]
        [SerializeField] private AudioClip _playSfx;

        // Runtime state
        private Sequence _mainSequence;
        private readonly List<RectTransform> _allSmallStars = new();
        private readonly List<Vector2> _smallStarInitialPositions = new();
        private Vector2 _centerStarInitialPos;

        #endregion

        #region Properties

        /// <summary>
        /// Cho biet hieu ung co dang chay hay khong.
        /// </summary>
        public bool IsPlaying => _mainSequence != null && _mainSequence.IsActive() && _mainSequence.IsPlaying();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupComponents();
        }

        private void OnEnable()
        {
            if (_playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            Stop();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Phat hieu ung sao UI tu vi tri hien tai.
        /// </summary>
        public void Play()
        {
            Stop();

            if (_centerStar == null && _allSmallStars.Count == 0)
            {
                SetupComponents();
            }

            // Phat SFX neu co
            if (_playSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_playSfx);
            }

            // Khoi tao lai trang thai ban dau
            ResetToInitialState();

            // Tao DOTween Sequence tong the
            _mainSequence = DOTween.Sequence();
            if (_ignoreTimeScale)
            {
                _mainSequence.SetUpdate(true);
            }

            // 1. ANIMATION NGOI SAO LON O TAM
            if (_centerStar != null)
            {
                // Giai doan 1: Xuat hien (Scale 0 -> AppearScale)
                _mainSequence.Append(
                    _centerStar.DOScale(Vector3.one * _centerStarAppearScale, _centerAppearDuration)
                               .SetEase(_centerAppearEase)
                );

                // Giai doan 2: Giu mot chut
                if (_centerHoldDuration > 0f)
                {
                    _mainSequence.AppendInterval(_centerHoldDuration);
                }

                // Giai doan 3: Bien mat (Phong to tiep + Fade out ve 0)
                _mainSequence.Append(
                    _centerStar.DOScale(Vector3.one * _centerStarDisappearScale, _centerDisappearDuration)
                               .SetEase(_centerDisappearEase)
                );

                if (_centerStarCanvasGroup != null)
                {
                    _mainSequence.Join(
                        _centerStarCanvasGroup.DOFade(0f, _centerDisappearDuration)
                                              .SetEase(Ease.InQuad)
                    );
                }
            }

            // 2. ANIMATION CAC NGOI SAO NHO BAN RA TU TAM
            int starCount = _allSmallStars.Count;
            if (starCount > 0)
            {
                float angleStep = 360f / starCount;

                for (int i = 0; i < starCount; i++)
                {
                    var star = _allSmallStars[i];
                    if (star == null) continue;

                    // Tinh huong ban va khoang cach
                    float baseAngle = i * angleStep;
                    float randomAngleOffset = UnityEngine.Random.Range(-angleStep * 0.3f, angleStep * 0.3f);
                    float finalAngleRad = (baseAngle + randomAngleOffset) * Mathf.Deg2Rad;

                    float distance = UnityEngine.Random.Range(_minBurstDistance, _maxBurstDistance);
                    Vector2 targetPos = _centerStarInitialPos + new Vector2(Mathf.Cos(finalAngleRad), Mathf.Sin(finalAngleRad)) * distance;

                    float scaleUpTime = _smallStarDuration * _smallStarScaleUpRatio;
                    float scaleDownTime = _smallStarDuration * (1f - _smallStarScaleUpRatio);
                    float targetScale = _smallStarMaxScale * UnityEngine.Random.Range(0.8f, 1.2f);
                    float randomRotation = UnityEngine.Random.Range(-_smallStarMaxRotation, _smallStarMaxRotation);

                    // Di chuyen tu tam ra ngoai
                    Tween moveTween = star.DOAnchorPos(targetPos, _smallStarDuration).SetEase(_smallStarMoveEase);

                    // Scale up roi scale down
                    Sequence starScaleSeq = DOTween.Sequence();
                    starScaleSeq.Append(star.DOScale(Vector3.one * targetScale, scaleUpTime).SetEase(Ease.OutQuad));
                    starScaleSeq.Append(star.DOScale(Vector3.zero, scaleDownTime).SetEase(Ease.InQuad));

                    // Xoay sao
                    Tween rotateTween = star.DORotate(new Vector3(0, 0, randomRotation), _smallStarDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad);

                    // Chen vao sequence chinh tai thoi diem delay
                    _mainSequence.Insert(_smallStarsDelay, moveTween);
                    _mainSequence.Insert(_smallStarsDelay, starScaleSeq);
                    _mainSequence.Insert(_smallStarsDelay, rotateTween);
                }
            }

            // Xu ly Loop hoac an sau khi ket thuc
            _mainSequence.OnComplete(() =>
            {
                if (_loop && gameObject.activeInHierarchy)
                {
                    _mainSequence = DOTween.Sequence();
                    if (_ignoreTimeScale) _mainSequence.SetUpdate(true);
                    _mainSequence.AppendInterval(_loopInterval);
                    _mainSequence.OnComplete(Play);
                }
            });
        }

        /// <summary>
        /// Dung hieu ung va an cac ngoi sao.
        /// </summary>
        public void Stop()
        {
            if (_mainSequence != null)
            {
                _mainSequence.Kill();
                _mainSequence = null;
            }

            ResetToInitialState();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Thiet lap cac component va khoi tao danh sach ngoi sao.
        /// </summary>
        private void SetupComponents()
        {
            // 1. Thiet lap Center Star
            if (_centerStar == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                if (images.Length > 0)
                {
                    _centerStar = images[0].rectTransform;
                }
            }

            if (_centerStar != null)
            {
                _centerStarInitialPos = _centerStar.anchoredPosition;
                if (_centerStarCanvasGroup == null)
                {
                    _centerStarCanvasGroup = _centerStar.GetComponent<CanvasGroup>();
                    if (_centerStarCanvasGroup == null)
                    {
                        _centerStarCanvasGroup = _centerStar.gameObject.AddComponent<CanvasGroup>();
                    }
                }
            }

            // 2. Thiet lap Small Stars
            _allSmallStars.Clear();
            _smallStarInitialPositions.Clear();

            // Neu co san trong list
            if (_smallStars != null && _smallStars.Count > 0)
            {
                foreach (var s in _smallStars)
                {
                    if (s != null && s != _centerStar)
                    {
                        _allSmallStars.Add(s);
                        _smallStarInitialPositions.Add(s.anchoredPosition);
                    }
                }
            }
            // Neu khong co, spawn tu prefab neu duoc cung cap
            else if (_smallStarPrefab != null && _spawnSmallStarCount > 0)
            {
                for (int i = 0; i < _spawnSmallStarCount; i++)
                {
                    GameObject obj = Instantiate(_smallStarPrefab, transform);
                    if (obj.TryGetComponent<RectTransform>(out var rect))
                    {
                        _allSmallStars.Add(rect);
                        _smallStarInitialPositions.Add(_centerStarInitialPos);
                    }
                }
            }
            // Neu khong co prefab, tu tim cac RectTransform con con lai
            else
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i) as RectTransform;
                    if (child != null && child != _centerStar)
                    {
                        _allSmallStars.Add(child);
                        _smallStarInitialPositions.Add(child.anchoredPosition);
                    }
                }
            }

            ResetToInitialState();
        }

        /// <summary>
        /// Dat lai vi tri, scale va do trong suot cua cac ngoi sao ve trang thai an ban dau.
        /// </summary>
        private void ResetToInitialState()
        {
            if (_centerStar != null)
            {
                _centerStar.DOKill();
                _centerStar.anchoredPosition = _centerStarInitialPos;
                _centerStar.localScale = Vector3.zero;
                _centerStar.localRotation = Quaternion.identity;

                if (_centerStarCanvasGroup != null)
                {
                    _centerStarCanvasGroup.DOKill();
                    _centerStarCanvasGroup.alpha = 1f;
                }
            }

            for (int i = 0; i < _allSmallStars.Count; i++)
            {
                var star = _allSmallStars[i];
                if (star == null) continue;

                star.DOKill();
                star.anchoredPosition = _centerStarInitialPos;
                star.localScale = Vector3.zero;
                star.localRotation = Quaternion.identity;
            }
        }

        #endregion
    }
}
