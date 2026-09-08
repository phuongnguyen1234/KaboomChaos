using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Core;
using System;

namespace UI.Animations
{
    /// <summary>
    /// Xử lý các hiệu ứng animation cho màn hình chính (HomeScreen)
    /// </summary>
    public class HomeScreenAnimation : MonoBehaviour
    {
        #region Serialized Fields
        [Header("1. Loading Overlay")]
        [SerializeField] private GameObject _loadingOverlayGroup;
        [SerializeField] private RectTransform _bombImage;
        [SerializeField] private RectTransform _loadingText;

        [Header("2. Home Group")]
        [SerializeField] private GameObject _homeGroup;
        [SerializeField] private RectTransform _exitButton;
        [SerializeField] private RectTransform _settingsButton;
        [SerializeField] private RectTransform _infoButton;
        [SerializeField] private RectTransform _logo;
        [SerializeField] private RectTransform _playButton;

        [Header("3. Transition Screen")]
        [SerializeField] private RectTransform _transitionScreen;
        #endregion

        #region Private Fields
        private Vector2 _exitOriginalPos;
        private Vector2 _settingsOriginalPos;
        private Vector2 _infoOriginalPos;
        private Vector2 _logoOriginalPos;
        private Vector2 _playOriginalPos;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_homeGroup != null) _homeGroup.SetActive(false);
            if (_transitionScreen != null) 
            {
                _transitionScreen.gameObject.SetActive(false);
                _transitionScreen.anchoredPosition = new Vector2(0, 2000); // Đặt ngoài màn hình
            }
        }

        private void OnDestroy()
        {
            // Clean up DOTween
            if (_bombImage != null) _bombImage.DOKill();
            if (_logo != null) _logo.DOKill();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Chuẩn bị các vị trí ban đầu trước khi chạy animation của Home Screen.
        /// </summary>
        public void SetupInitialPositions()
        {
            if (_exitButton == null || _settingsButton == null || _logo == null || _playButton == null) return;

            // Lưu lại vị trí ban đầu
            _exitOriginalPos = _exitButton.anchoredPosition;
            _settingsOriginalPos = _settingsButton.anchoredPosition;
            if (_infoButton != null) _infoOriginalPos = _infoButton.anchoredPosition;
            _logoOriginalPos = _logo.anchoredPosition;
            _playOriginalPos = _playButton.anchoredPosition;

            // Dời ra ngoài màn hình
            _exitButton.anchoredPosition = new Vector2(-1500, _exitOriginalPos.y);
            _settingsButton.anchoredPosition = new Vector2(1500, _settingsOriginalPos.y);
            if (_infoButton != null) _infoButton.anchoredPosition = new Vector2(1500, _infoOriginalPos.y);
            _logo.anchoredPosition = new Vector2(_logoOriginalPos.x, 1500);
            _playButton.anchoredPosition = new Vector2(_playOriginalPos.x, -1500);
        }

        /// <summary>
        /// Chạy hiệu ứng loading (quả bom lắc lư, thu nhỏ dần).
        /// </summary>
        public void StartLoadingAnimation(Action onLoadingComplete)
        {
            if (_loadingOverlayGroup == null || _bombImage == null || _loadingText == null)
            {
                onLoadingComplete?.Invoke();
                return;
            }

            _loadingOverlayGroup.SetActive(true);
            
            // Quả bom tilt qua lại nhẹ nhàng
            _bombImage.DORotate(new Vector3(0, 0, 15f), 0.5f)
                      .SetLoops(-1, LoopType.Yoyo)
                      .SetEase(Ease.InOutSine);

            // Giả lập thời gian loading (1.5s)
            DOVirtual.DelayedCall(1.5f, () => {
                _bombImage.DOKill(); // Dừng hiệu ứng lắc
                
                // Thu nhỏ biến mất
                _bombImage.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
                
                // Text slide xuống
                _loadingText.DOAnchorPosY(-500f, 0.3f)
                            .SetRelative(true)
                            .SetEase(Ease.InBack)
                            .OnComplete(() => {
                                _loadingOverlayGroup.SetActive(false);
                                onLoadingComplete?.Invoke();
                            });
            });
        }

        /// <summary>
        /// Hiển thị Home Screen với hiệu ứng slide từ các phía.
        /// </summary>
        public void PlayHomeEnterAnimation()
        {
            if (_homeGroup == null) return;
            
            _homeGroup.SetActive(true);
            
            if (_exitButton != null) 
                _exitButton.DOAnchorPos(_exitOriginalPos, 0.5f).SetEase(Ease.OutBack);
                
            if (_settingsButton != null) 
                _settingsButton.DOAnchorPos(_settingsOriginalPos, 0.5f).SetEase(Ease.OutBack).SetDelay(0.1f);
                
            if (_infoButton != null) 
                _infoButton.DOAnchorPos(_infoOriginalPos, 0.5f).SetEase(Ease.OutBack).SetDelay(0.2f);
                
            if (_logo != null)
            {
                _logo.DOAnchorPos(_logoOriginalPos, 0.6f).SetEase(Ease.OutBounce).SetDelay(0.3f).OnComplete(() => {
                    // Hiệu ứng idle: Logo scale up/down và tilt nhẹ
                    _logo.DOScale(1.05f, 2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                    _logo.DORotate(new Vector3(0, 0, 3f), 2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                });
            }

            if (_playButton != null) 
                _playButton.DOAnchorPos(_playOriginalPos, 0.5f).SetEase(Ease.OutBack).SetDelay(0.4f);
        }

        /// <summary>
        /// Hiệu ứng chuyển cảnh khi nhấn nút Play.
        /// </summary>
        public void PlayTransitionAnimation(Action onSlideDown, Action onSlideUp)
        {
            if (_transitionScreen == null)
            {
                onSlideDown?.Invoke();
                onSlideUp?.Invoke();
                return;
            }

            _transitionScreen.gameObject.SetActive(true);
            _transitionScreen.anchoredPosition = new Vector2(0, 2000);

            // Slide xuống che màn hình
            _transitionScreen.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutBounce).OnComplete(() => {
                
                // Gọi callback ngay khi vừa slide xuống che xong
                onSlideDown?.Invoke();
                
                // Che lại trong 3 giây
                DOVirtual.DelayedCall(3f, () => {
                    // Bắt đầu slide lên
                    onSlideUp?.Invoke();
                    
                    _transitionScreen.DOAnchorPos(new Vector2(0, 2000), 0.5f).SetEase(Ease.InBack).OnComplete(() => {
                        _transitionScreen.gameObject.SetActive(false);
                    });
                });
            });
        }
        #endregion
    }
}

